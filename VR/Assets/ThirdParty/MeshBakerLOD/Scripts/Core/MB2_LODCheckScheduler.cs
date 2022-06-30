using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DigitalOpus.MB.Lod{
	/**
	 * The purpose of this class is to efficiently schedule when LODs check if the LOD needs to change.
	 * All LODs in a cluster should be checked at the same time. This will result in one bake that changes
	 * a lot of LODs rather than many bakes that change a few LODs.
	 * Also distant clusters should be checked less frequently than close clusters.
	 * 
	 * This class also triggers the checking on the assigned schedules.
	 * 
	 * HOW IT WORKS
	 *    each cluster has a variables numFramesBetweenChecks and numFramesBetweenChecksOffset that determine
	 *    when it is checked to see if its LODs need to change. These variables are assigned in such a way
	 *    to distribute baking evenly across frames. 
	 * 
	 *    GridClusters close to the camera are assigned a low value for numFramesBetweenChecks. GridClusters far from
	 *    the camera are assigned a high value. SimpleClusters are all have the same "numFramesBetweenChecks".
	 * 
	 *    Since the camera(s) may be moving then the cluster shedules will need to be updated periodically. This
	 *    is done by recording the camera position(s) of the last schedule update. When the camera has moved
	 *    a certain distance from its last update position then the schedules are re-calculated.
	 * 
	 *    The LOD_Manager has one instance of the LODCheckScheduler. In LOD_Manager.Update, the LOD_Manager calls
	 *    
	 * 		checkScheduler.CheckIfLODsNeedToChange();
	 *    
	 * 		CheckIfLODsNeedToChange:
	 * 			Updates the cluster schedules if the camera has moved.
	 * 			Checks each cluster to see if it needs to be checked this frame.
	 * 			
	 * */
	
	public class LODCheckScheduler {
		public bool FORCE_CHECK_EVERY_FRAME = false; //used by test harness
		Vector3[] lastCameraPositions;
		bool containsMultipleCells;
		bool containsMovingClusters;
		float sqrDistThreashold;
		float minGridSize;
		MB2_LODManager manager;
		int nextFrameCheckOffset;
		float lastSheduleUpdateTime;
		
		public int GetNextFrameCheckOffset(){
			if (nextFrameCheckOffset >= 1000) nextFrameCheckOffset = 0;
			return nextFrameCheckOffset++;
		}
		
		public void Init(MB2_LODManager m){
			manager = m;
			if (manager.LOG_LEVEL >= DigitalOpus.MB.Core.MB2_LogLevel.debug) Debug.Log("Init called for LODCheckScheduler.");
			
			containsMultipleCells = false;
			containsMovingClusters = false;
			minGridSize = float.PositiveInfinity;
			for (int i = 0; i < manager.bakers.Length; i++){
				if (manager.bakers[i].clusterType != MB2_LODManager.BakerPrototype.CombinerType.simple){
					containsMultipleCells = true;
					if (manager.bakers[i].gridSize < minGridSize){ //todo check baker type
						minGridSize = manager.bakers[i].gridSize;
					}
				}
				if (manager.bakers[i].clusterType == MB2_LODManager.BakerPrototype.CombinerType.moving){
					containsMovingClusters = true;	
				}
			}
			if (containsMultipleCells){
				sqrDistThreashold = (minGridSize / 1.5f)*(minGridSize / 1.5f);
				InitializeLastCameraPositions(manager.GetCameras());
			}
		}
		
		void InitializeLastCameraPositions(MB2_LODCamera[] cams){
			lastCameraPositions = new Vector3[cams.Length];
			for (int i = 0; i < lastCameraPositions.Length; i++){
				lastCameraPositions[i] = new Vector3(10e15f,10e15f,10e15f);	
			}			
		}
		
		void UpdateClusterSchedules(){
			if (manager.LOG_LEVEL >= DigitalOpus.MB.Core.MB2_LogLevel.debug) Debug.Log("Updating cluster lodcheck schedules.");
			for (int i = 0; i < manager.bakers.Length; i++){
				LODClusterManager cm = manager.bakers[i].baker;
				for (int j = 0; j < cm.clusters.Count; j++){
					LODCluster cell = cm.clusters[j];
					
					int numBetweenFrameChecks = GetNumFramesBetweenChecks(cell);
					
					int framesUntilNextFrameToCheck = int.MaxValue;
					List<LODCombinedMesh> cls = cell.GetCombiners();

					for (int k = 0; k < cls.Count; k++){
						if (cls[k].numFramesBetweenChecksOffset == -1) {
							//this is a fresh cluster, check it
							if (manager.LOG_LEVEL >= DigitalOpus.MB.Core.MB2_LogLevel.trace) Debug.Log("fm=" + Time.frameCount + " calling cluster Update");
							cls[k].Update();
						}
						cls[k].numFramesBetweenChecks = numBetweenFrameChecks;
						cls[k].numFramesBetweenChecksOffset = GetNextFrameCheckOffset();
						int considerFramesUntilNextFrameToCheck = cls[k].numFramesBetweenChecks - ((Time.frameCount + cls[k].numFramesBetweenChecksOffset) % cls[k].numFramesBetweenChecks);
						if (considerFramesUntilNextFrameToCheck < framesUntilNextFrameToCheck) framesUntilNextFrameToCheck = considerFramesUntilNextFrameToCheck;
//						Debug.Log(String.Format("f={0} framesBet={1} offset={2} consider{3}",Time.frameCount,cls[k].numFramesBetweenChecks,
//							cls[k].numFramesBetweenChecksOffset, considerFramesUntilNextFrameToCheck));
					}
					cell.nextCheckFrame = Time.frameCount + framesUntilNextFrameToCheck;
//					Debug.Log("cell nextframetocheck=" + cell.nextCheckFrame + " framesUntil=" + framesUntilNextFrameToCheck + " ");
				}
			}
			lastSheduleUpdateTime = Time.time;
		}
		
		//Called for a cluster to generate a new value for numFramesBetweenChecks for that cluster.
		public int GetNumFramesBetweenChecks(LODCluster cell){
			int numBetweenFrameChecks = -1;
			if (cell is LODClusterGrid || cell is LODClusterMoving){
				//clusters closer than gridSize are checked numBetweenFrameChecks.
				//clusters greater than one gridSize are checked 2*numBetweenFrameChecks.
				//clusters greater than two gridSize are checked 4*numBetweenFrameChecks.
			
				float distToClosestCamera = Mathf.Sqrt(manager.GetDistanceSqrToClosestPerspectiveCamera(cell.Center()));
				MB2_LODManager.BakerPrototype bakerPrototype = cell.GetClusterManager().GetBakerPrototype();
				numBetweenFrameChecks = bakerPrototype.numFramesBetweenLODChecks;
				int factor = Mathf.FloorToInt(distToClosestCamera / (bakerPrototype.gridSize * .5f)) + 1;
				numBetweenFrameChecks *= (factor);
			} else if (cell is LODClusterSimple) {
				//simple cell
				MB2_LODManager.BakerPrototype bakerPrototype = cell.GetClusterManager().GetBakerPrototype();
				numBetweenFrameChecks = bakerPrototype.numFramesBetweenLODChecks;				
			} else {
				Debug.LogError("Should never get here.");	
			}
			return numBetweenFrameChecks;
		}	
		
		void _UpdateClusterSchedulesIfCameraHasMoved(){
			//if any camera has moved more than .75 smallest grid size then
			//it is time to refresh the check times of the clusters				
			MB2_LODCamera[] cams = manager.GetCameras();
			if (cams.Length != lastCameraPositions.Length){
				InitializeLastCameraPositions(cams);
			}
			bool didUpdate = false;
			for (int i = 0; i < lastCameraPositions.Length; i++){
				Vector3 camPos = cams[i].transform.position;
				Vector3 delta = camPos - lastCameraPositions[i];
				if (Vector3.Dot(delta ,delta ) > sqrDistThreashold){
					//a camera has moved more than .75 smallest grid size
					//update the scheduling on the clusters
					UpdateClusterSchedules();
					didUpdate = true;
					for (int j = 0; j < cams.Length; j++){
						lastCameraPositions[j] = cams[j].transform.position;	
					}
					break;
				}
			}
			if (containsMovingClusters && !didUpdate){
				// if there are moving clusters then just checking how far the camera has moved is
				// not good enough. This will force schedules to recalcule
				// todo change this to better check based on whether cluster distances to camera have changed.
				if (Time.time - lastSheduleUpdateTime > 1f){
					UpdateClusterSchedules();
					didUpdate = true;
					for (int j = 0; j < cams.Length; j++){
						lastCameraPositions[j] = cams[j].transform.position;	
					}						
				}
			}
		}
		
		public void CheckIfLODsNeedToChange() {
			float tStamp = Time.realtimeSinceStartup;
			//Profile.StartProfile("CheckIfLODsNeedToChange_Total");
			//Profile.StartProfile("CheckIfLODsNeedToChange_A");
			if (containsMultipleCells){
				_UpdateClusterSchedulesIfCameraHasMoved();
			}
			
			//Profile.EndProfile("CheckIfLODsNeedToChange_A");
			//Profile.StartProfile("CheckIfLODsNeedToChange_B");
			//check clusters that are scheduled for this frame
			for (int i = 0; i < manager.bakers.Length; i++){
				LODClusterManager cm = manager.bakers[i].baker;
				if (!(cm is LODClusterManagerSimple)) containsMultipleCells = true;
				if (cm is LODClusterManagerMoving) containsMovingClusters = true;
				for (int j = 0; j < cm.clusters.Count; j++){
					LODCluster cell = cm.clusters[j];
					if (FORCE_CHECK_EVERY_FRAME || cell.nextCheckFrame == Time.frameCount){
						if (cell is LODClusterMoving){
							((LODClusterMoving) cell).UpdateBounds();	
						}
						int framesUntilNextFrameToCheck = int.MaxValue;
						List<LODCombinedMesh> cls = cell.GetCombiners();
						for (int k = 0; k < cls.Count; k++){
							LODCombinedMesh cl = cls[k];
							bool forceCheck = false;
							if (cl.numFramesBetweenChecks == -1){
								// this is a fresh combiner and needs a schedule to be assigned
								cl.numFramesBetweenChecks = GetNumFramesBetweenChecks(cell);
								cl.numFramesBetweenChecksOffset = GetNextFrameCheckOffset();
								forceCheck = true;
							} 
							
							//Do we check this frame?
							int considerFramesUntilNextFrameToCheck = cl.numFramesBetweenChecks - ((Time.frameCount + cl.numFramesBetweenChecksOffset) % cl.numFramesBetweenChecks);
							if (FORCE_CHECK_EVERY_FRAME || forceCheck || considerFramesUntilNextFrameToCheck == cl.numFramesBetweenChecks){
								if (manager.LOG_LEVEL >= DigitalOpus.MB.Core.MB2_LogLevel.trace) Debug.Log("fm=" + Time.frameCount + " calling cluster Update");
								cl.Update();
							} 
							if (considerFramesUntilNextFrameToCheck < framesUntilNextFrameToCheck){
								framesUntilNextFrameToCheck = considerFramesUntilNextFrameToCheck;
							}
						}
						cell.nextCheckFrame = Time.frameCount + framesUntilNextFrameToCheck;
					}
					if (cell.nextCheckFrame < Time.frameCount) Debug.LogError(Time.frameCount + " Error somehow bypassed a frame when checking. " + cell.nextCheckFrame);
				}
			}
			//Profile.EndProfile("CheckIfLODsNeedToChange_B");
			//Profile.EndProfile("CheckIfLODsNeedToChange_Total");
			manager.statLastCheckLODNeedToChangeTime = Time.realtimeSinceStartup - tStamp;
			manager.statTotalCheckLODNeedToChangeTime += manager.statLastCheckLODNeedToChangeTime;
		}

		public void ForceCheckIfLODsNeedToChange() {
			//float tStamp = Time.realtimeSinceStartup;
			//Profile.StartProfile("CheckIfLODsNeedToChange_Total");
			//Profile.StartProfile("CheckIfLODsNeedToChange_A");
			if (containsMultipleCells){
				_UpdateClusterSchedulesIfCameraHasMoved();
			}
			
			//Profile.EndProfile("CheckIfLODsNeedToChange_A");
			//Profile.StartProfile("CheckIfLODsNeedToChange_B");
			//check clusters that are scheduled for this frame
			for (int i = 0; i < manager.bakers.Length; i++){
				LODClusterManager cm = manager.bakers[i].baker;
				for (int j = 0; j < cm.clusters.Count; j++){
					LODCluster cell = cm.clusters[j];
					List<LODCombinedMesh> cls = cell.GetCombiners();
					for (int k = 0; k < cls.Count; k++){
						LODCombinedMesh cl = cls[k];
						cl.Update();
					}
				}
			}
			//Profile.EndProfile("CheckIfLODsNeedToChange_B");
			//Profile.EndProfile("CheckIfLODsNeedToChange_Total");
		}		
	}
}
