using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;

namespace DigitalOpus.MB.Lod{
	
	//todo
		// add check time to total time calculated in LODManager
	
	public class LODCombinedMesh{
		
		public struct Transaction{
			public MB2_LODOperation action;
			public MB2_LOD lod;
			public int toIdx;
			public int inMeshGameObjectID;
			public int inMeshNumVerts;
		}
		
#region LODCombinedMesh

		protected static float splitCombinerThreshold = 2f;
		protected static float mergeCombinerThreshold = .3f;
		public MB3_MultiMeshCombiner combinedMesh;
		protected Dictionary<int,Transaction> lodTransactions;
		
		protected HashSet<MB2_LOD> gosInCombiner;
		protected HashSet<MB2_LOD> gosAssignedToMe;
		
		//public int frameCheckOffset;
		//todo do I still need these?
		public int numFramesBetweenChecks;
		public int numFramesBetweenChecksOffset;
		protected int numBakeImmediately;
		public LODCluster cluster;
		
		//tries to track number of vertices in mesh
		//may not be acurate due to updates but OK for appoximate measure
		public int numVertsInMesh;
		public int numApproxVertsInQ;

		protected bool wasTranslated = false; //was this combiner translated from 0,0,0
		
		public virtual LODClusterManager GetClusterManager(){
			return cluster.GetClusterManager();	
		}
		
		public void UpdateSkinnedMeshApproximateBounds(){
			if (combinedMesh.renderType != MB_RenderType.skinnedMeshRenderer){
				Debug.LogWarning("Should not call UpdateSkinnedMeshApproximateBounds on a non skinned combined mesh");
				return;
			}
			
			//todo use this when exists
			//combinedMesh.UpdateSkinnedMeshApproximateBounds();
			for (int i = 0; i < combinedMesh.meshCombiners.Count; i++){
				MB3_MeshCombiner c = combinedMesh.meshCombiners[i].combinedMesh;
				if (c != null) combinedMesh.meshCombiners[i].combinedMesh.UpdateSkinnedMeshApproximateBounds();			
			}
		}

		public virtual void ForceBakeImmediately(){
			if (numBakeImmediately == 0) numBakeImmediately = 1;	
		}
		
		public virtual int GetNumVertsInMesh(){
			return numVertsInMesh;
		}
		
		public virtual int GetApproxNetVertsInQs(){
			return numApproxVertsInQ;	
		}
		
		public virtual void SetLODCluster(LODCluster c){
			cluster = c;	
		}
		
		public virtual LODCluster GetLODCluster(){
			return cluster;
		}
		
		public virtual bool IsVisible(){
			return cluster.IsVisible();
		}
		
		public virtual int NumDirty(){
			return lodTransactions.Count;
		}
		
		public virtual int NumBakeImmediately(){
			return numBakeImmediately;	
		}
		
		public virtual float DistSquaredToPlayer(){
			return cluster.DistSquaredToPlayer();
		}
		
		public void AssignToCombiner(MB2_LOD lod){
			if (lod.GetCombiner() != this){
				Debug.LogError("LOD was assigned to a different cluster.");
				return;
			}
			gosAssignedToMe.Add(lod);	
		}
		
		public void UnassignFromCombiner(MB2_LOD lod){
			gosAssignedToMe.Remove(lod);
		}
		
		public bool IsAssignedToThis(MB2_LOD lod){
			return gosAssignedToMe.Contains(lod);
		}

		void _CancelTransaction(MB2_LOD lod){
			if (!lod.isInQueue) {
				Debug.LogError("Cancel transaction should never be called for LODs not in Q");
			}
			Transaction t = new Transaction();
			t.action = lod.action;
			t.toIdx = lod.nextLevelIdx;
			t.lod = lod;
			Transaction oldT;
			if (lodTransactions.TryGetValue (lod.gameObject.GetInstanceID (), out oldT)) {
				if (oldT.action == MB2_LODOperation.toAdd)
						numApproxVertsInQ -= lod.GetNumVerts (oldT.toIdx);
				if (oldT.action == MB2_LODOperation.update) {
						numApproxVertsInQ -= lod.GetNumVerts (oldT.toIdx);
						numApproxVertsInQ += lod.GetNumVerts (lod.currentLevelIdx);
				}
				if (oldT.action == MB2_LODOperation.delete)
						numApproxVertsInQ += oldT.inMeshNumVerts;
				lodTransactions.Remove (lod.gameObject.GetInstanceID ());
				lod.OnRemoveFromQueue ();
			} else {
				Debug.LogError("An LOD thought it was in the Q but it wasn't");
			}
		}

		Transaction _AddTransaction(MB2_LOD lod){
			Transaction t = new Transaction();
			t.action = lod.action;
			t.toIdx = lod.nextLevelIdx;
			t.lod = lod;
			if (lod.isInCombined && lod.action == MB2_LODOperation.delete){
				t.inMeshGameObjectID = lod.GetGameObjectID(lod.currentLevelIdx);
				t.inMeshNumVerts = lod.GetNumVerts(lod.currentLevelIdx);
			}
			//adjust num verts
			//undo changes of previous transaction if there was one
			Transaction oldT;
			if (lodTransactions.TryGetValue(lod.gameObject.GetInstanceID(),out oldT)){
				if (oldT.action == MB2_LODOperation.toAdd) numApproxVertsInQ -= lod.GetNumVerts(oldT.toIdx);
				if (oldT.action == MB2_LODOperation.update){
					numApproxVertsInQ -= lod.GetNumVerts(oldT.toIdx);
					numApproxVertsInQ += lod.GetNumVerts(lod.currentLevelIdx);
				}
				if (oldT.action == MB2_LODOperation.delete) numApproxVertsInQ += oldT.inMeshNumVerts;
			}
			
			if (lod.action == MB2_LODOperation.toAdd) numApproxVertsInQ += lod.GetNumVerts(lod.nextLevelIdx);
			if (lod.action == MB2_LODOperation.update){
				numApproxVertsInQ -= lod.GetNumVerts(lod.currentLevelIdx);
				numApproxVertsInQ += lod.GetNumVerts(lod.nextLevelIdx);
			}
			if (lod.action == MB2_LODOperation.delete) numApproxVertsInQ -= lod.GetNumVerts(lod.currentLevelIdx);
			
			if (lod.action == MB2_LODOperation.delete && !lod.isInCombined){
				lodTransactions.Remove(lod.gameObject.GetInstanceID());
				lod.OnRemoveFromQueue();				
			}else{
				lodTransactions[lod.gameObject.GetInstanceID()] = t;
				lod.OnAddToQueue();
			}
			return t;
		}

		public void LODCancelTransaction(MB2_LOD lod){
			if (lod.GetCombiner() != this) Debug.LogError("Wrong combiner");
			if (cluster.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,"LODManager.LODCancelTransaction " + lod + " action " + lod.action, MB2_LogLevel.trace);
			_CancelTransaction (lod);
		}

		public void LODChanged(MB2_LOD lod, bool immediate){			
			//Profile.StartProfile("MB2_LODManager.LODChanged");
			if (lod.GetCombiner() != this) Debug.LogError("Wrong combiner");
			if (cluster.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,"LODManager.LODChanged " + lod + " action " + lod.action, MB2_LogLevel.trace);
			_AddTransaction(lod);
			MB2_LODManager.Manager().AddDirtyCombinedMesh(this);
			//Profile.EndProfile("MB2_LODManager.LODChanged");
		}

		//called when lod is disabled or destroyed
		public void RemoveLOD(MB2_LOD lod, bool immediate=true){
			//Profile.StartProfile("MB2_LODManager.RemoveLOD");
			if (cluster.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace, "LODManager.RemoveLOD " + lod, MB2_LogLevel.trace);
			if (!lod.isInQueue && !lod.isInCombined){
				Debug.LogError("RemoveLOD: lod is not in combined or is in queue " + lod.isInQueue + " " + lod.isInCombined);
				return;
			}
			if (lod.action != MB2_LODOperation.delete){
				Debug.LogError("Action must be delete");
				return;
			}
			
			if (lod.isInQueue || lod.isInCombined){
				_AddTransaction(lod);
			}
			MB2_LODManager.Manager().AddDirtyCombinedMesh(this);
			//Profile.EndProfile("MB2_LODManager.RemoveLOD");
		}		

		public void _TranslateLODs(Vector3 translation){
			foreach(MB2_LOD lod in gosAssignedToMe){
				lod._ResetPositionMarker();
			}
			if (combinedMesh.resultSceneObject != null){
				Vector3 pos = combinedMesh.resultSceneObject.transform.position;
				combinedMesh.resultSceneObject.transform.position = pos + translation;
			}
			wasTranslated = true;
		}

		float GetFullRatio(){
			int netVerts = GetNumVertsInMesh() + GetApproxNetVertsInQs();
			return ((float) netVerts) / ((float) combinedMesh.maxVertsInMesh);				
		}

		public virtual void Bake(){
			//Profile.StartProfile("MB2_LODManager.Bake");
//				_BakeWithoutSplitAndMerge();
				_BakeWithSplitAndMerge();
			//Profile.EndProfile("MB2_LODManager.Bake");
		}

		void _BakeWithoutSplitAndMerge(){
			HashSet<LODCombinedMesh> dirtyCombiners = cluster.AdjustForMaxAllowedPerLevel();
			//ordinary bake
			BakeClusterCombiner();
			//bake the other combiners in this cluster that may have had combiners adjusted
			if (dirtyCombiners != null){
				foreach (LODCombinedMesh cl in dirtyCombiners){
					if (cl.cluster == this){ //cluster may have been merged
						cl.BakeClusterCombiner();
					}
				}
			}
		}

		void _BakeWithSplitAndMerge(){
			HashSet<LODCombinedMesh> dirtyCombiners = cluster.AdjustForMaxAllowedPerLevel();
			
			bool didSplitOrMergeBake = false;
			//if more than splitCombinerThreshold full then split cluster
			float ratio = GetFullRatio();
			if (ratio > splitCombinerThreshold){
				LODCombinerSplitterMerger.SplitCombiner(this);
				didSplitOrMergeBake = true;
				MB2_LODManager.Manager().statNumSplit ++;
			} else if (ratio < mergeCombinerThreshold){
				//see if there is more than one combiner empty enough to merge
				List<LODCombinedMesh> combiners = cluster.GetCombiners();
				//Debug.Log ("Merging me ratio " + ratio.ToString ("F5"));
				for (int i = 0; i < combiners.Count; i++){
					//Debug.Log ("Merging combiners " + i +" ratio " + combiners[i].GetFullRatio().ToString ("F5"));
					if (combiners[i] != this && combiners[i].GetFullRatio() < mergeCombinerThreshold){
						LODCombinerSplitterMerger.MergeCombiner(cluster);
						didSplitOrMergeBake = true;
						MB2_LODManager.Manager().statNumMerge++;
						break;
					}
					ratio = GetFullRatio();
					if (ratio > mergeCombinerThreshold) break;
				}
			} 
			//ordinary bake
			if (!didSplitOrMergeBake){
				BakeClusterCombiner();
			}
			//bake the other combiners in this cluster that may have had combiners adjusted
			if (dirtyCombiners != null){
				foreach (LODCombinedMesh cl in dirtyCombiners){
					if (cl.cluster == this){ //cluster may have been merged
						cl.BakeClusterCombiner();
					}
				}
			}
		}
		
		public LODCombinedMesh(MB3_MeshBaker meshBaker, LODCluster cell){
			cluster = cell;
			combinedMesh = new MB3_MultiMeshCombiner();
			combinedMesh.maxVertsInMesh = cell.GetClusterManager().GetBakerPrototype().maxVerticesPerCombinedMesh; //c. maxVertsInMesh;
			SetMBValues(meshBaker);
			lodTransactions = new Dictionary<int,Transaction>();
			gosInCombiner = new HashSet<MB2_LOD>();
			gosAssignedToMe = new HashSet<MB2_LOD>();
			numVertsInMesh = 0;
			numApproxVertsInQ = 0;
		}

		public virtual void SetMBValues(MB3_MeshBaker mb){
			MB3_MeshCombiner source = mb.meshCombiner;
			combinedMesh.renderType = source.renderType;
			combinedMesh.outputOption = MB2_OutputOptions.bakeIntoSceneObject;
			combinedMesh.lightmapOption = source.lightmapOption;
			combinedMesh.textureBakeResults = source.textureBakeResults;
			combinedMesh.doNorm = source.doNorm;
			combinedMesh.doTan = source.doTan;
			combinedMesh.doCol = source.doCol;	
			combinedMesh.doUV = source.doUV;
			combinedMesh.doUV1 = source.doUV1;		
		}		
		
		public virtual bool IsDirty(){
			if (lodTransactions.Count > 0) return true;
			return false;
		}
		
		public virtual void PrePrioritize(Plane[][] fustrum, Vector3[] cameraPositions){
			if (cluster == null) Debug.LogError("cluster is null");
			if (fustrum == null) Debug.LogError("fustrum is null");
			if (cameraPositions == null) Debug.LogError("camPositions null");
			cluster.PrePrioritize(fustrum,cameraPositions);
		}
		
		public virtual Bounds CalcBounds(){
			Bounds b = new Bounds(Vector3.zero, Vector3.one);
			if (gosAssignedToMe.Count > 0){
				bool firstFound = false;
				foreach (MB2_LOD lod in gosAssignedToMe){
					if (lod != null && MB2_Version.GetActive(lod.gameObject)){
						if (firstFound){
							b.Encapsulate(lod.transform.position);	
						} else {
							firstFound = true;
							float dim = lod.levels[0].dimension;
							b = new Bounds(lod.transform.position,new Vector3(dim,dim,dim));
						}
					}
				}
				if (firstFound == false){
					if (cluster.GetClusterManager()._LOG_LEVEL >= MB2_LogLevel.info){
						Debug.Log ("CalcBounds called on a CombinedMesh that contained no valid LODs");	
					}
				}
			} else {
				if (cluster.GetClusterManager()._LOG_LEVEL >= MB2_LogLevel.info){
					Debug.Log ("CalcBounds called on a CombinedMesh that contained no valid LODs");	
				}					
			}
			return b;
		}
		
		//removes everything without callbacks
		void ClearBake(){
			lodTransactions.Clear();
			gosInCombiner.Clear();
			combinedMesh.AddDeleteGameObjects(null,combinedMesh.GetObjectsInCombined().ToArray());			
			combinedMesh.Apply();
			numApproxVertsInQ = 0;
			numVertsInMesh = 0;
			numBakeImmediately = 0;
		}
		
		void BakeClusterCombiner(){
			//if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) 
			MB2_Log.Log(MB2_LogLevel.debug, String.Format("Bake called on cluster numTransactions={0}",lodTransactions.Count),GetClusterManager().LOG_LEVEL);
			if (lodTransactions.Count > 0){
				List<int> toDelete = new List<int>();
				List<GameObject> toAdd = new List<GameObject>();
				//need to build lists for callbacks
				//can't use list of transactions because callbacks can cause it to be modified if nested LODs
				List<MB2_LOD> toAddLODs = new List<MB2_LOD>();
				List<MB2_LOD> toDeleteLODs = new List<MB2_LOD>();
				List<MB2_LOD> toUpdateLODs = new List<MB2_LOD>();
				
				foreach(int lodInstID in lodTransactions.Keys){
					Transaction t = lodTransactions[lodInstID];
					if (t.action == MB2_LODOperation.toAdd){
						toAdd.Add(t.lod.GetRendererGameObject(t.toIdx));
						toAddLODs.Add(t.lod);
					} else if (t.action == MB2_LODOperation.update){
						toDelete.Add(t.lod.GetGameObjectID(t.lod.currentLevelIdx));
						toAdd.Add(t.lod.GetRendererGameObject(t.toIdx)); 
						toUpdateLODs.Add(t.lod);
					} else if (t.action == MB2_LODOperation.delete){
						toDelete.Add(t.inMeshGameObjectID);
						if (t.lod != null) toDeleteLODs.Add(t.lod);
					}
				}

				if (wasTranslated){
					if (combinedMesh.resultSceneObject != null) combinedMesh.resultSceneObject.transform.position = Vector3.zero;
					foreach (MB2_LOD lod in gosInCombiner){
						//todo searching these lists could be slow if a lot of LODs
						if (lod.isInCombined && !toAddLODs.Contains(lod) && !toDeleteLODs.Contains(lod) && !toUpdateLODs.Contains(lod)){
							toAdd.Add (lod.GetRendererGameObject(lod.currentLevelIdx));
							toDelete.Add (lod.GetRendererGameObject(lod.currentLevelIdx).GetInstanceID());
						}
					}
					wasTranslated = false;
				}

				combinedMesh.AddDeleteGameObjectsByID(toAdd.ToArray(),toDelete.ToArray(),true);
				combinedMesh.Apply();
				numApproxVertsInQ = 0;
				numVertsInMesh = 0;
				for (int i = 0; i < combinedMesh.meshCombiners.Count; i++){
					numVertsInMesh += combinedMesh.meshCombiners[i].combinedMesh.GetMesh().vertexCount;	
				}			
				
				if (combinedMesh.resultSceneObject != null){
					MB2_LODManager.BakerPrototype bp = GetClusterManager().GetBakerPrototype();
					Transform resultSceneObject = combinedMesh.resultSceneObject.transform;
					for (int j = 0; j < resultSceneObject.childCount; j++){
						GameObject go = resultSceneObject.GetChild(j).gameObject;
						go.layer = bp.layer;
						Renderer r = go.GetComponent<Renderer>();
						r.shadowCastingMode =  bp.castShadow;
						r.receiveShadows = bp.receiveShadow;
					}
				}
				
				MB2_LODManager man = MB2_LODManager.Manager();
				man.statTotalNumBakes++;
				man.statLastNumBakes++;
				man.statLastBakeFrame = Time.frameCount;
				
				//fix so that updating bounds will work.
				if (combinedMesh.renderType == MB_RenderType.skinnedMeshRenderer){
					for (int j = 0; j < combinedMesh.meshCombiners.Count; j++){
						MB3_MeshCombiner c = combinedMesh.meshCombiners[j].combinedMesh;
						if (c != null){
							SkinnedMeshRenderer smr = (SkinnedMeshRenderer) c.targetRenderer;			
							bool ee = smr.updateWhenOffscreen;
							smr.updateWhenOffscreen = true;
							smr.updateWhenOffscreen = ee;
						}
					}
				}
				
				lodTransactions.Clear();
				
				numBakeImmediately = 0;
				//now that baking is complete fire events on the objects that were added
				//it is important to do this after all baking is done because enabling and 
				//disabling objects can enable/disable LODs in children which can cause objects to be added
				//and deleted from the add and delete queues on this cluster
				
				//todo these callbacks could be simpler
				for (int i = 0; i < toDeleteLODs.Count; i++){
					toDeleteLODs[i].OnBakeRemoved();
					gosInCombiner.Remove(toDeleteLODs[i]);
				}
				for (int i = 0; i < toAddLODs.Count; i++){ 
					toAddLODs[i].OnBakeAdded();
					gosInCombiner.Add(toAddLODs[i]);
				}
				for (int i = 0; i < toUpdateLODs.Count; i++){
					toUpdateLODs[i].OnBakeUpdated();
					gosInCombiner.Add(toUpdateLODs[i]); //todo do I need this? will it blow up?
				}
				if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace,"Bake complete, num in combined " + combinedMesh.GetNumObjectsInCombined() + " fullRatio:" + GetFullRatio().ToString("F5"),GetClusterManager().LOG_LEVEL);
			}
		}

		public virtual void Destroy(){
			combinedMesh.DestroyMesh();
			if (combinedMesh.resultSceneObject != null){
				GameObject.Destroy(combinedMesh.resultSceneObject);
				combinedMesh.resultSceneObject = null;
			}
			cluster = null;
		}
		
		public virtual void GetObjectsThatWillBeInMesh(List<MB2_LOD> objsThatWillBeInMesh){
			// collect all the LOD components for object already in the combined mesh
			foreach(MB2_LOD l in gosInCombiner) objsThatWillBeInMesh.Add(l);
		
			foreach(int id in lodTransactions.Keys){
				Transaction ts = lodTransactions[id];
				if (ts.action == MB2_LODOperation.toAdd) objsThatWillBeInMesh.Add(ts.lod);
				if (ts.action == MB2_LODOperation.delete) objsThatWillBeInMesh.Remove(ts.lod);
			}
		}
			
		public virtual void Clear(){
			List<GameObject> objs = combinedMesh.GetObjectsInCombined();

			if (GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug, "Clear called on grid cluster num in combined " +
																			objs.Count + " numTrans=" + lodTransactions.Count
																			+ " numAssignedToMe " + gosAssignedToMe.Count,GetClusterManager().LOG_LEVEL);

			//remove everything from the combined mesh
			combinedMesh.AddDeleteGameObjects(null,objs.ToArray());
			combinedMesh.Apply();
		
			//find the LOD component for the objs in the combined mesh and Clear them
			foreach (MB2_LOD lod in gosInCombiner){
				if (lod != null) lod.Clear();
			}
			foreach (MB2_LOD lod in gosAssignedToMe){
				if (lod != null) lod.Clear();
			}			
			lodTransactions.Clear();
			gosInCombiner.Clear();
			gosAssignedToMe.Clear();
			numVertsInMesh = 0;
			numBakeImmediately = 0;
		}
		
		public virtual bool Contains(MB2_LOD lod){
			if (lodTransactions.ContainsKey(lod.GetInstanceID())) return true;
			if (gosInCombiner.Contains(lod)) return true;
			return false;
		}
		
		public virtual void CheckIntegrity(){
			foreach(MB2_LOD lod in gosAssignedToMe){
				if (lod.GetCombiner() != this) Debug.LogError("LOD "+ lod + " thinks it is in a different "+lod.GetCombiner() +" than it is \n log dump" +lod.myLog.Dump());					
			}
			
			foreach(MB2_LOD lod in gosInCombiner){
				if (!lod.isInCombined) Debug.LogError(lod + "LOD thought it was in combined but wasn't\n log dump" +lod.myLog.Dump());
				if (lod.action == MB2_LODOperation.toAdd) Debug.LogError("bad lod action\n log dump" +lod.myLog.Dump());
				if (!gosAssignedToMe.Contains(lod)) Debug.LogError("in combiner was not in assigned "+lod.GetCombiner() + " an it is \n log dump" +lod.myLog.Dump());
			}
			
			// collect all the LOD components for object already in the combined mesh
			List<GameObject> objs = combinedMesh.GetObjectsInCombined();
			for (int i = 0; i < objs.Count; i++){
				if (objs[i] != null){
					MB2_LOD lod = MB2_LODManager.GetComponentInAncestor<MB2_LOD>(objs[i].transform);
					if (lod == null){
						Debug.LogError("Couldn't find LOD for obj in combined mesh");
					} else {
						if (!gosInCombiner.Contains(lod)){
							Debug.LogError("lod was in combined mesh that is not in list of lods in cluster.");	
						}
					}
				}
			}
			
			int nToAdd = 0, nToDelete = 0, nInMesh = 0;
			foreach(int id in lodTransactions.Keys){
				Transaction t = lodTransactions[id];
				if (t.action == MB2_LODOperation.toAdd){
					if (t.lod.isInCombined) Debug.LogError("Bad action");
					nToAdd += t.lod.GetNumVerts(t.toIdx);
				}
				if (t.action == MB2_LODOperation.update){
					if (!t.lod.isInCombined) Debug.LogError("Bad action");
					nToAdd += t.lod.GetNumVerts(t.toIdx);
					nToDelete += t.lod.GetNumVerts(t.lod.currentLevelIdx);
				}
				if (t.action == MB2_LODOperation.delete){
					if (!t.lod.isInCombined) Debug.LogError("Bad action");
					nToDelete += t.lod.GetNumVerts(t.lod.currentLevelIdx);
				}				
			}
			for (int i = 0; i < combinedMesh.meshCombiners.Count; i++){
				nInMesh += combinedMesh.meshCombiners[i].combinedMesh.GetMesh().vertexCount;	
			}
			if (nInMesh != numVertsInMesh) Debug.LogError("Num verts in mesh don't match measured " + nInMesh + " thought " + numVertsInMesh);
			if (nToAdd - nToDelete != numApproxVertsInQ) Debug.LogError("Num verts in Q don't match measured " + (nToAdd - nToDelete) + " thought " + numApproxVertsInQ);
		}
		
		public void Update(){
			List<MB2_LOD> toRemove = null;
			foreach(MB2_LOD lod in gosAssignedToMe){
				if (lod == null){
					if (toRemove == null) toRemove = new List<MB2_LOD>();
					toRemove.Add(lod);
				} else if (lod.enabled && MB2_Version.GetActive(lod.gameObject)){
					lod.CheckIfLODsNeedToChange();
				}
			}
			if (toRemove != null){
				for (int i = 0; i < toRemove.Count; i++) gosAssignedToMe.Remove(toRemove[i]);
			}
		}
#endregion
#region LODCombinedMeshSplitterMerger		
		//for splitting and merging combiners
		public class LODCombinerSplitterMerger{
			public static MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;
			class LODHierarchy{
				public MB2_LOD rootLod;
				public List<MB2_LOD> lods = new List<MB2_LOD>();
				public int numVerts;
				
				public LODHierarchy(MB2_LOD root){
					rootLod = root;	
				}
				
				public void ComputeNumberOfVertices(){
					numVerts = 0;
					for (int i = 0; i < lods.Count; i++){
						MB2_LOD lod = lods[i];
						if (lod != null){
							if (lod.isInQueue && lod.action == MB2_LODOperation.toAdd || lod.action == MB2_LODOperation.update){
								numVerts += lod.GetNumVerts(lod.nextLevelIdx);
							}
							if (lod.isInCombined){
								if (lod.isInQueue && lod.action == MB2_LODOperation.delete){
								
								} else {
									numVerts += lod.GetNumVerts(lod.currentLevelIdx);
								}
							}
						}
					}
				}
			}
			
			class NewCombiner{
				public List<LODHierarchy> lods;
				public LODCombinedMesh combiner;
				public int numVerts;
				
				public NewCombiner(LODCombinedMesh c){
					lods = new List<LODHierarchy>();
					combiner = c;
					numVerts = c.GetNumVertsInMesh() + c.GetApproxNetVertsInQs();
				}
			}
	
			public static void MergeCombiner(LODCluster cell){
				//Profile.StartProfile("MergeCombiner");
				//Profile.StartProfile("MergeCombiner1");
				float tStamp = Time.realtimeSinceStartup;
				if (cell.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug
					|| LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("=========== Merging Combiner " + cell);

				//sort combiners by full ratio
				List<LODCombinedMesh> cls = cell.GetCombiners();
				if (cls.Count < 2) return;
				cls.Sort(new LODCombinerFullComparer());
				
				if (LOG_LEVEL == MB2_LogLevel.trace) Debug.Log ("ratios before" + PrintFullRatios(cls));	

				//Profile.EndProfile("MergeCombiner1");
				//Profile.StartProfile("MergeCombiner2");
				//primaryCombiner is most empty
				LODCombinedMesh a = cls[0];
				int i = 1;
				int totalInA = a.GetNumVertsInMesh() + a.GetApproxNetVertsInQs();
				if (LOG_LEVEL == MB2_LogLevel.trace) Debug.Log("============ numCombiners before " + a.cluster.GetCombiners().Count);
				while(i < cls.Count && totalInA < a.combinedMesh.maxVertsInMesh){
					//take next most empty
					LODCombinedMesh b = cls[i];
					if (totalInA + b.GetNumVertsInMesh() + b.GetApproxNetVertsInQs() > a.combinedMesh.maxVertsInMesh){
						break; //we are done
					}
					//Profile.StartProfile("MergeCombiner2.a");
					totalInA += b.GetNumVertsInMesh() + b.GetApproxNetVertsInQs();
					//remove everything from b
					List<MB2_LOD> lods = new List<MB2_LOD>(b.gosAssignedToMe);
					for (int j = 0; j < lods.Count; j++){
						lods[j].ForceRemove();	
					}
					//Profile.EndProfile("MergeCombiner2.a");
					//Profile.StartProfile("MergeCombiner2.b");
					b.ClearBake();
					//Profile.EndProfile("MergeCombiner2.b");
					//Profile.StartProfile("MergeCombiner2.c");
					for (int j = 0; j < lods.Count; j++){
						MB2_LOD lod = lods[j];
						b.UnassignFromCombiner(lod);
						lod.SetCombiner(a);
						a.AssignToCombiner(lod);
						lod.ForceAdd();
					}
					//Profile.EndProfile("MergeCombiner2.c");
					//Profile.StartProfile("MergeCombiner2.d");
					cell.RemoveAndRecycleCombiner(b);
					i++;
					//Profile.EndProfile("MergeCombiner2.d");
				}
				if (LOG_LEVEL == MB2_LogLevel.trace) Debug.Log("========= numCombiners after " + a.cluster.GetCombiners().Count);
				//Profile.EndProfile("MergeCombiner2");
				//Profile.StartProfile("MergeCombiner3");
				if (i > 1){
					a.BakeClusterCombiner();
				}
				//Profile.EndProfile("MergeCombiner3");
				float elapsedTime = Time.realtimeSinceStartup - tStamp;
				MB2_LODManager.Manager().statLastMergeTime = elapsedTime;				
				if (a.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug
					|| LOG_LEVEL >= MB2_LogLevel.debug){
						if (LOG_LEVEL == MB2_LogLevel.trace) Debug.Log ("ratios after " + PrintFullRatios(cell.GetCombiners()));				
						MB2_Log.LogDebug("=========== Done Merging Cluster merged {0} clusters in {1} sec",(i-1), elapsedTime);				
				}
				//Profile.EndProfile("MergeCombiner");
				//Profile.PrintResults();
	
				if (MB2_LODManager.CHECK_INTEGRITY) a.cluster.CheckIntegrity();
			}
			
			public static void SplitCombiner(LODCombinedMesh src){
				float tStamp = Time.realtimeSinceStartup;
				if (MB2_LODManager.CHECK_INTEGRITY) src.GetLODCluster().CheckIntegrity();
				if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug
					|| LOG_LEVEL >= MB2_LogLevel.debug){
					MB2_Log.LogDebug("=============== Splitting Combiner " + src.GetLODCluster());
					if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("ratios before " + PrintFullRatios(src.GetLODCluster().GetCombiners()));
				}
				
				//organize hierarchies all LODs in hierarchy must be in same combiner
				Dictionary<MB2_LOD,LODHierarchy> hierarchyRoot2allInHierarchy = new Dictionary<MB2_LOD, LODHierarchy>();
				foreach(MB2_LOD lod in src.gosAssignedToMe){
					MB2_LOD rootLod = lod.GetHierarchyRoot();
					LODHierarchy hierarchy;
					if (!hierarchyRoot2allInHierarchy.TryGetValue(rootLod,out hierarchy)){
						hierarchy = new LODHierarchy(rootLod);
						hierarchyRoot2allInHierarchy.Add(rootLod,hierarchy);
					}
					hierarchy.lods.Add(lod);				
				}
				
				//if only one hierarchy then quit, can't split this cluster
				if (hierarchyRoot2allInHierarchy.Count == 1) return;
				
				if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.LogDebug("Splitting Combiner found " + hierarchyRoot2allInHierarchy.Count + " hierarchies");
				//compute the size of each hierarchy
				int totalVerts = 0;
				foreach(LODHierarchy h in hierarchyRoot2allInHierarchy.Values){
					//todo should discount assigned to combiner but not in
					h.ComputeNumberOfVertices();
					totalVerts += h.numVerts;
					//Debug.Log ("hierarchy numverts " + h.numVerts);
				}
				
				//sort the hierarchies by number of verts in each
				//todo sort the hierarchies
				
				//create some new combiners
				int numCombiners = totalVerts / src.combinedMesh.maxVertsInMesh;
				if (numCombiners < 2) numCombiners = 2;
				NewCombiner[] cs = new NewCombiner[numCombiners];
				cs[0] = new NewCombiner(src);
				cs[0].numVerts = 0; //assume has zero
				
				//first look for existing combiners that are almost empty
				List<LODCombinedMesh> allCombiners = src.GetLODCluster().GetCombiners();
				int newIdx = 1;
				for (int i = 0; i < allCombiners.Count && newIdx < cs.Length; i++){
					if (allCombiners[i].GetFullRatio() < mergeCombinerThreshold){
						cs[newIdx] = new NewCombiner(allCombiners[i]);
						newIdx++;
					}
				}

				for (; newIdx < cs.Length; newIdx++){
					LODCombinedMesh cl = src.GetLODCluster().GetClusterManager().GetFreshCombiner(src.GetLODCluster());
					cs[newIdx] = new NewCombiner(cl);
				}
				if (MB2_LODManager.CHECK_INTEGRITY) src.GetLODCluster().CheckIntegrity();
				
				//distribute hierarchies across new combiners
				foreach(LODHierarchy h in hierarchyRoot2allInHierarchy.Values){
					NewCombiner hasMostSpace = cs[0];
					for (int i = 1; i < cs.Length; i++){
						if (cs[i].numVerts < hasMostSpace.numVerts){
							hasMostSpace = cs[i];
						}
					}
					hasMostSpace.lods.Add(h);
					hasMostSpace.numVerts += h.numVerts;
				}
				if (MB2_LODManager.CHECK_INTEGRITY) src.GetLODCluster().CheckIntegrity();
				
				if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace ){
					for (int i = 0; i < cs.Length; i++){
						Debug.Log("distributed " + cs[i].lods.Count + " to combiner " + i);
					}
				}
				
				//update lods in combiner
				//remove everything in primary to be moved 
				for (int i = 0; i < cs.Length; i++){ 
					NewCombiner c = cs[i];
					for (int j = 0; j < c.lods.Count; j++){
						LODHierarchy h = c.lods[j];
						for (int k = 0; k < h.lods.Count; k++){
							MB2_LOD lod = h.lods[k];
							lod.ForceRemove();
						}
					}
				}
//				for (int i = 0; i < cs.Length; i++){
//					Debug.Log("after force remove " + cs[i].lods.Count + " to combiner " + i + " addQ" + cs[i].combiner.gosToAdd.Count);
//				}				
				if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.LogDebug("Clearing primary combiner");
				//bake primary everything to be moved should be out of combiner
				cs[0].combiner.ClearBake();
//				for (int i = 0; i < cs.Length; i++){
//					Debug.Log("after primary bake " + cs[i].lods.Count + " to combiner " + i + " addQ" + cs[i].combiner.gosToAdd.Count);
//				}
								
				//switch the combiner for all new
				for (int i = 0; i < cs.Length; i++){ 
					NewCombiner c = cs[i];
					for (int j = 0; j < c.lods.Count; j++){
						LODHierarchy h = c.lods[j];
						for (int k = 0; k < h.lods.Count; k++){
							MB2_LOD lod = h.lods[k];
							if (i >= 1){ //reassign to new combiners
								lod.GetCombiner().UnassignFromCombiner(lod);
								lod.SetCombiner(c.combiner);
								c.combiner.AssignToCombiner(lod);
							}
						}
						h.rootLod.ForceAdd();
					}
				}
				if (MB2_LODManager.CHECK_INTEGRITY) src.GetLODCluster().CheckIntegrity();

				//bake new clusters
				for (int i = 0; i < cs.Length; i++){
					if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.LogDebug("Baking new combiner " + i);
					cs[i].combiner.BakeClusterCombiner();
					if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log ("baked " + i + " full ratio " + cs[i].combiner.GetFullRatio().ToString ("F5"));
				}
				
				float elapsedTime = Time.realtimeSinceStartup - tStamp;
				MB2_LODManager.Manager().statLastSplitTime = elapsedTime;
				if (src.GetClusterManager().LOG_LEVEL >= MB2_LogLevel.debug 
					|| LOG_LEVEL >= MB2_LogLevel.debug){
					if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("ratios after " + PrintFullRatios(src.GetLODCluster().GetCombiners()));					
					MB2_Log.LogDebug("=================Done split combiners " + elapsedTime + " ============");
				}
			}
		}
		
		public static string PrintFullRatios(List<LODCombinedMesh> cls){
			StringBuilder sb = new StringBuilder();
			for (int j = 0; j < cls.Count; j++){
				sb.AppendFormat("{0} full ratio {1} numObjs {2} numMeshes {3}\n", j, cls[j].GetFullRatio().ToString ("F5"), cls[j].gosInCombiner.Count, cls[j].combinedMesh.meshCombiners.Count);	
			}
			return sb.ToString();
		}
		
		class LODCombinerFullComparer : IComparer<LODCombinedMesh>{
		   int IComparer<LODCombinedMesh>.Compare(LODCombinedMesh a, LODCombinedMesh b){
				return a.GetNumVertsInMesh() + a.GetApproxNetVertsInQs()
						-b.GetNumVertsInMesh() - b.GetApproxNetVertsInQs();
		   }
		}
#endregion
	}
}
	
	
