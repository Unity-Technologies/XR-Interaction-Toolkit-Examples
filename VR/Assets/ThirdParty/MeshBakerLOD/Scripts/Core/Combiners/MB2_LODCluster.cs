using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;

namespace DigitalOpus.MB.Lod{

	public interface LODCluster{
		bool Contains(Vector3 p);
		bool Intersects(Bounds b);
		bool Intersects(Plane[][] fustrum);
		Vector3 Center();
		void Destroy();
		void Clear();
		bool IsVisible();
		float DistSquaredToPlayer();
		List<LODCombinedMesh> GetCombiners();
		void CheckIntegrity();
		void DrawGizmos();
		LODClusterManager GetClusterManager();
		void RemoveAndRecycleCombiner(LODCombinedMesh cl);
		void AddCombiner(LODCombinedMesh cl);
		LODCombinedMesh SuggestCombiner();
		void AssignLODToCombiner(MB2_LOD l);
		void UpdateSkinnedMeshApproximateBounds();
		void PrePrioritize(Plane[][] fustrum, Vector3[] cameraPositions);
		int nextCheckFrame { get;  set; }
		HashSet<LODCombinedMesh> AdjustForMaxAllowedPerLevel();
		void ForceCheckIfLODsChanged();
	}
	
	public abstract class LODClusterBase: LODCluster{
		public LODClusterManager manager;
		int _nextCheckFrame;
		int _lastAdjustForMaxAllowedFrame;
		public int nextCheckFrame{
			get {return _nextCheckFrame;}
			set {_nextCheckFrame = value;}
		}
		protected List<LODCombinedMesh> combinedMeshes = new List<LODCombinedMesh>();
		public List<LODCombinedMesh> GetCombiners(){return new List<LODCombinedMesh>(combinedMeshes);}
		public abstract bool Contains(Vector3 v);
		public abstract bool Intersects(Bounds b);
		public abstract bool Intersects(Plane[][] fustrum);
		public abstract Vector3 Center();
		public abstract void DrawGizmos();
		public abstract bool IsVisible();
		public abstract float DistSquaredToPlayer();
		public LODClusterBase(LODClusterManager m){
			manager = m;
			_lastAdjustForMaxAllowedFrame = -1;
			manager.GetFreshCombiner(this);
		}	
		public virtual void Destroy(){
			for (int i = combinedMeshes.Count - 1; i >= 0; i--){
				combinedMeshes[i].Destroy();
			}
		}
		public virtual void Clear(){
			for (int i = combinedMeshes.Count - 1; i >= 0; i--){
				combinedMeshes[i].Clear();
				combinedMeshes[i].combinedMesh.resultSceneObject.name = combinedMeshes[i].combinedMesh.resultSceneObject.name + "-recycled";
				manager.RecycleCluster(combinedMeshes[i]);
			}			
		}
		public virtual void CheckIntegrity(){
			for (int i = 0; i < combinedMeshes.Count; i++){
				combinedMeshes[i].CheckIntegrity();
				if (combinedMeshes[i].GetLODCluster() != this) Debug.LogError("Cluster was a child of this cell "+i+" but its parent was another cell. num " + combinedMeshes.Count + " " + combinedMeshes[i].GetLODCluster());
				for (int j = 0; j < combinedMeshes.Count; j++){
					if (i != j && combinedMeshes[i] == combinedMeshes[j]) Debug.LogError("same cluster has been added twice.");	
				}
			}
			//todo check all LODs in hierarchy are in same cluster
		}
		public virtual LODClusterManager GetClusterManager(){
			return manager;
		}
		
		public virtual void RemoveAndRecycleCombiner(LODCombinedMesh cl){
			combinedMeshes.Remove (cl);
			if (combinedMeshes.Contains(cl)) Debug.LogError("removed but still contains.");
			manager.RecycleCluster(cl);
		}
		
		public virtual void AddCombiner(LODCombinedMesh cl){
			if (!combinedMeshes.Contains(cl)) combinedMeshes.Add(cl);
			else Debug.LogError("error in AddCombiner");
		}
		
		public virtual LODCombinedMesh SuggestCombiner(){
			LODCombinedMesh c = combinedMeshes[0];
			int numInC = c.GetNumVertsInMesh() + c.GetApproxNetVertsInQs();
			for (int i = 1; i < combinedMeshes.Count; i++){
				int newNumInC = combinedMeshes[i].GetNumVertsInMesh() + combinedMeshes[i].GetApproxNetVertsInQs();
				if (numInC > newNumInC){
					numInC = newNumInC;
					c = combinedMeshes[i];
				}
			}
			return c;
		}
		public virtual void AssignLODToCombiner(MB2_LOD l){
			if (MB2_LODManager.CHECK_INTEGRITY && !combinedMeshes.Contains(l.GetCombiner())) Debug.LogError("Error in AssignLODToCombiner " + l + " combiner " + l.GetCombiner() + " is not in this LODCluster this=" + this + " other=" + l.GetCombiner().GetLODCluster());
			l.GetCombiner().AssignToCombiner(l);
		}
		public virtual void UpdateSkinnedMeshApproximateBounds(){
			Debug.LogError("Grid combinedMeshes cannot be used for skinned meshes");	
		}
		public virtual void PrePrioritize(Plane[][] fustrum, Vector3[] cameraPositions){}

		public virtual HashSet<LODCombinedMesh> AdjustForMaxAllowedPerLevel(){
			if (_lastAdjustForMaxAllowedFrame == Time.frameCount) return null;
			int[] maxNumberPerLevel = manager.GetBakerPrototype().maxNumberPerLevel;
			if (maxNumberPerLevel == null || maxNumberPerLevel.Length == 0) return null;
			HashSet<LODCombinedMesh> dirtyClusters = new HashSet<LODCombinedMesh>();
			
			List<MB2_LOD> objsThatWillBeInMesh = new List<MB2_LOD>();
			
			for (int i = 0; i < combinedMeshes.Count; i++){
				combinedMeshes[i].GetObjectsThatWillBeInMesh(objsThatWillBeInMesh);
			}
			
			// should now have a list of everything that will be in the mesh after the bake
			// sort this by distance from the player
			objsThatWillBeInMesh.Sort(new MB2_LOD.MB2_LODDistToCamComparer());
			
			// separate list into buckets by level of detail to a maximum number allowed per bucket. 
			// create buckets 
			HashSet<MB2_LOD>[] buckets = new HashSet<MB2_LOD>[maxNumberPerLevel.Length];
			int totalBucketCapacity = 0;
			for (int i = 0; i < buckets.Length; i++){
				totalBucketCapacity += maxNumberPerLevel[i];
				buckets[i] = new HashSet<MB2_LOD>();
			}
			HashSet<MB2_LOD> bucketLeftovers = new HashSet<MB2_LOD>();
			
			//todo consider how this will interact with forceLevel
			//Put objects in the highest LOD bucket that is not full.
			int oIdx;
			int numAddedToBuckets = 0;
			for (oIdx = 0; oIdx < objsThatWillBeInMesh.Count; oIdx++){
				int desiredBIdx = objsThatWillBeInMesh[oIdx].nextLevelIdx;
				bool addedToBucket = false;
				if (desiredBIdx < buckets.Length && numAddedToBuckets < totalBucketCapacity){
					for (int j = desiredBIdx; j < buckets.Length; j++){
						if (buckets[j].Count < maxNumberPerLevel[j]){
							buckets[j].Add(objsThatWillBeInMesh[oIdx]);
							numAddedToBuckets++;
							addedToBucket = true;
							break;
						}
					}
				}
				if (!addedToBucket){
					bucketLeftovers.Add(objsThatWillBeInMesh[oIdx]);
				}
			}
			
			if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug){
				String s = String.Format("AdjustForMaxAllowedPerLevel objsThatWillBeInMesh={0}\n", objsThatWillBeInMesh.Count);
				for (int i = 0; i < buckets.Length; i++){
					s += String.Format("b{0} capacity={1} contains={2}\n",i, maxNumberPerLevel[i], buckets[i].Count);
				}
				s += String.Format("b[leftovers] contains={0}\n", bucketLeftovers.Count);
				MB2_Log.Log(MB2_LogLevel.info,s,GetClusterManager().LOG_LEVEL);
			}
			
			// adjust the nextLODlevelIdx in each lod down.
			// can skip first because only objs in and wanting to be in levelIdx 0 could get in there
			// objs in buckets must be in one of: gosToAdd, gosToUpdate in mesh.
			// todo what about force into level
			for (int bIdx = 1; bIdx < buckets.Length; bIdx++){
				foreach(MB2_LOD l in buckets[bIdx]){
					if (l.nextLevelIdx == bIdx) continue; //we didn't change anything
					if (bIdx >= l.levels.Length){//Past last level are hiding these LODs
						if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,String.Format("A Demoting obj in bucket={0} obj={1}",bIdx,l),GetClusterManager().LOG_LEVEL);
						l.AdjustNextLevelIndex(bIdx);
						dirtyClusters.Add(l.GetCombiner());
					} else {
						if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,String.Format("B Demoting obj in bucket={0} obj={1}",bIdx,l),GetClusterManager().LOG_LEVEL);						
						l.AdjustNextLevelIndex(bIdx);
						dirtyClusters.Add(l.GetCombiner());
					}
				}
			}
			
			// put leftover lods in higher lod states
			int maxBIdx = buckets.Length - 1;
			foreach(MB2_LOD l in bucketLeftovers){
				if (l.nextLevelIdx <= maxBIdx){
					if (maxBIdx >= l.levels.Length-1){//Past last level are hiding these LODs
						if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,String.Format("C Demoting obj in bucket={0} obj={1}",maxBIdx+1,l),GetClusterManager().LOG_LEVEL);
						l.AdjustNextLevelIndex(maxBIdx+1);
						dirtyClusters.Add(l.GetCombiner());
					} else {
						if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,String.Format("D Demoting obj in bucket={0} obj={1}",maxBIdx+1,l),GetClusterManager().LOG_LEVEL);						
						l.AdjustNextLevelIndex(maxBIdx+1);
						dirtyClusters.Add(l.GetCombiner());
					}
				}
			}
			_lastAdjustForMaxAllowedFrame = Time.frameCount;
			return dirtyClusters;
		}
	
		public virtual void ForceCheckIfLODsChanged(){
			for (int i = 0; i < combinedMeshes.Count; i++){
				combinedMeshes[i].Update();
			}
		}
	}	
}
