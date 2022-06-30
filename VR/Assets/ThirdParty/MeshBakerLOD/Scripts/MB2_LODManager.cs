using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;
using System.Text;

// todo add validation disable code when available in Mesh Baker

	public class MB2_LODManager : MonoBehaviour {

		static MB2_LODManager _manager;
		
		//if this is set to the current frame then the Manager has been destroyed and should reject service calls
		//can't use a simple bool because it is impossible to 
		static int destroyedFrameCount = -1;

		//Singleton, returns this manager.
	    public static MB2_LODManager Manager(){
			if (destroyedFrameCount == Time.frameCount) return null;
			if (_manager == null) {
				MB2_LODManager[] sm = (MB2_LODManager[]) FindObjectsOfType(typeof(MB2_LODManager));
				if (sm == null || sm.Length == 0){
					Debug.LogError("There were MB2_LOD scripts in the scene that couldn't find an MB2_LODManager in the scene. Try dragging the LODManager prefab into the scene and configuring some bakers.");
				} else if (sm.Length > 1){
					Debug.LogError("There was more than one LODManager object found in the scene.");
					_manager = null;
				} else {
					_manager = sm[0];	
				}
	        }
	        return _manager;
	    }
		
		public enum ChangeType{
			changeAdd,
			changeRemove,
			changeUpdate
		}
		
		public struct BakeDiagnostic{
			public int frame;
			public int deltaTime; 
			public int bakeTime;
			public int checkLODNeedToChangeTime;
			public int gcTime;
			public int numCombinedMeshsBaked;
			public int numCombinedMeshsInQ;
		
			public BakeDiagnostic(MB2_LODManager manager){
				frame = Time.frameCount;
				numCombinedMeshsInQ = manager.dirtyCombinedMeshes.Count;
				deltaTime = 0;
				if (manager.statLastBakeFrame == frame){	
					bakeTime = (int) (manager.statLastCombinedMeshBakeTime * 1000f);
					numCombinedMeshsBaked = manager.statLastNumBakes;
				} else {
					bakeTime = 0;
					numCombinedMeshsBaked = 0;
				}
				checkLODNeedToChangeTime = (int) (manager.statLastCheckLODNeedToChangeTime * 1000f);
				if (manager.statLastGCFrame == frame){
					gcTime = (int) (manager.statLastGarbageCollectionTime * 1000f);
				} else {
					gcTime = 0;
				}	
			}
		
			public static string PrettyPrint(BakeDiagnostic[] data){
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("--------------------------------------------");
				sb.AppendLine("Frame  deltaTime  numBakes  bakeTime  gcTime  checkTime  numInQ");
				for (int i = 0; i < data.Length; i++) {
					if (data[i].numCombinedMeshsBaked > 0){
					sb.AppendFormat("{0}:     {1}       {2}       {3}       {4}       {5}       {6}\n",data[i].frame.ToString().PadLeft(4),data[i].deltaTime.ToString().PadLeft(4),data[i].numCombinedMeshsBaked.ToString().PadLeft(4),data[i].bakeTime.ToString().PadLeft(4),data[i].gcTime.ToString().PadLeft(4),data[i].checkLODNeedToChangeTime.ToString().PadLeft(4),data[i].numCombinedMeshsInQ.ToString().PadLeft(4));		
					} else {
					sb.AppendFormat("{0}:     {1}       {2}        -        {3}       {4}       {5}\n",data[i].frame.ToString().PadLeft(4),data[i].deltaTime.ToString().PadLeft(4),data[i].bakeTime.ToString().PadLeft(4),data[i].gcTime.ToString().PadLeft(4),data[i].checkLODNeedToChangeTime.ToString().PadLeft(4),data[i].numCombinedMeshsInQ.ToString().PadLeft(4));							
					}
				}
				sb.AppendLine("--------------------------------------------");
				return sb.ToString();
			}
		}
	
		[System.Serializable]
		public class BakerPrototype{
			public enum CombinerType{
				grid,
				simple,
				moving,
			}
			
			public MB3_MeshBaker meshBaker;
			public int lightMapIndex = -1;
			public int layer = 1;
			public UnityEngine.Rendering.ShadowCastingMode castShadow = UnityEngine.Rendering.ShadowCastingMode.On;
			public bool receiveShadow = true;
			public string label = "";
			public CombinerType clusterType = CombinerType.grid;
			public int maxVerticesPerCombinedMesh = 32000; //todo multi-mesh should error if mesh to add is bigger than max
			public float gridSize = 250;
			public HashSet<Material> materials = new HashSet<Material>();
			public int[] maxNumberPerLevel = new int[0];
			public bool updateSkinnedMeshApproximateBounds = false;
			public int numFramesBetweenLODChecks = 20;
			[System.NonSerialized] public LODClusterManager baker;
		
			public bool Initialize(BakerPrototype[] bakers){
				if (meshBaker == null){
					Debug.LogError("Baker does not have a MeshBaker assigned. Create a 'Mesh and Material Baker'." +
								 	"and assign it to this baker.");	
					return false;
				}
				if (maxVerticesPerCombinedMesh < 3){
					Debug.LogError("Baker maxVerticesPerCombinedMesh must be greater than 3.");	
					return false;	
				}
				if (gridSize <= 0 && clusterType == BakerPrototype.CombinerType.grid){
					Debug.LogError("Baker gridSize must be greater than zero.");
					return false;	
				}			
				if (meshBaker.textureBakeResults == null || meshBaker.textureBakeResults.materialsAndUVRects == null || meshBaker.textureBakeResults.materialsAndUVRects.Length == 0){
					Debug.LogError("Baker does not have a texture bake result or the texture bake result contains no materials. Assign a texture bake result.");	
					return false;
				}
				if (meshBaker.meshCombiner.renderType == MB_RenderType.skinnedMeshRenderer && clusterType == BakerPrototype.CombinerType.simple && updateSkinnedMeshApproximateBounds == false){
					Debug.Log("You are combining skinned meshes but Update Skinned Mesh Approximate bounds is not checked. You should check this setting if your meshes can move outside the fixed bounds or your meshes may vanish unexpectedly.");
				}
				if (numFramesBetweenLODChecks < 1){
					Debug.LogError("'Num Frames Between LOD Checks' must be greater than zero.");	
					return false;				
				}
				if (materials == null) materials = new HashSet<Material>();
				MB2_TextureBakeResults res = meshBaker.textureBakeResults;
				for (int j = 0; j < res.materialsAndUVRects.Length; j++){
					if (res.materialsAndUVRects[j].material != null && res.materialsAndUVRects[j].material.shader != null){
						materials.Add(res.materialsAndUVRects[j].material);
					} else {
						Debug.LogError("One of the materials or shaders is null in prototype ");
						return false;
					}
				}
				
				int[] m = maxNumberPerLevel;
				for (int j = 0; j < m.Length; j++){
					if (m[j] < 0) {
						Debug.LogError("Max Number Per Level values must be positive");
						m[j] = 1000;
					}
				}
	
				for (int i = 0; i < bakers.Length; i++){
					//validate labels
					if (bakers[i] != this){
						if (bakers[i].label.Length > 0 && label.Equals(bakers[i].label)){
							Debug.LogError("Bakers have duplicate label" + bakers[i].label);
							return false;
						}
					}
					//validate materials
					if (bakers[i] != this){
						if (materials.Overlaps(bakers[i].materials) &
							bakers[i].label.Length == 0 &
							label.Length == 0){
							if (bakers[i].materials.Count != 1){
								Debug.LogWarning("Bakers " + i + " share materials with another baker. Assigning LOD objects to bakers may be ambiguous. Try setting labels to resolve conflicts.");
							}
						}
					}
				}
				CreateClusterManager();
				return true;
			}
		
			public void CreateClusterManager(){
				LODClusterManager b = null;
				if (clusterType == BakerPrototype.CombinerType.grid){
					b = new LODClusterManagerGrid(this);
					((LODClusterManagerGrid)b).gridSize = (int) gridSize;
				} else if (clusterType == BakerPrototype.CombinerType.simple){
					b = new LODClusterManagerSimple(this);
				} else if (clusterType == BakerPrototype.CombinerType.moving){
					b = new LODClusterManagerMoving(this);
				}
				baker = b;
			}
		
			public void Clear(){
				if (baker != null) baker.Clear();
			}
		}
	
		public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;		
		public static bool ENABLED = true;
		public static bool CHECK_INTEGRITY = false;
		public bool baking_enabled=true; //used by test harness to deffer baking for some frames.
		
		public float maxCombineTimePerFrame = .03f; //seconds
		public bool ignoreLightmapping = true;
		public BakerPrototype[] bakers;
		public LODCheckScheduler checkScheduler;
		
		Dictionary<LODCombinedMesh,LODCombinedMesh> dirtyCombinedMeshes = new Dictionary<LODCombinedMesh,LODCombinedMesh>();
		MB2_LODCamera[] lodCameras = null;
		public IComparer<LODCombinedMesh> combinedMeshPriorityComparer = new MB2_LODClusterComparer();
		public List<MB2_LOD> limbo = new List<MB2_LOD>(); //LOD components waiting to be destroyed.	
	
		public int numBakersPerGC = 2;
		int bakesSinceLastGC = 0;		
		int GCcollectionCount = 0;
		bool isSetup = false;
		public BakeDiagnostic[] frameInfo;
		[HideInInspector] public int statTotalNumBakes = 0;
		
		[HideInInspector] public float statAveCombinedMeshBakeTime = .03f;
		[HideInInspector] public float statMaxCombinedMeshBakeTime = 0f;
		[HideInInspector] public float statMinCombinedMeshBakeTime = 100f;
		[HideInInspector] public float statTotalCombinedMeshBakeTime = .03f;
		[HideInInspector] public float statLastCombinedMeshBakeTime = 0f;

		//[HideInInspector] public float statLastGarbageCollectionTime = 0f;
		[HideInInspector] public int statLastNumBakes = 0;
		[HideInInspector] public int statLastGCFrame = 0;
	
		[HideInInspector] public float statLastGarbageCollectionTime = 0f;
		[HideInInspector] public float statTotalGarbageCollectionTime = 0f;
		//[HideInInspector] public int statFrameSinceLastBake = 0;
		[HideInInspector] public int statLastBakeFrame = 0;
		[HideInInspector] public int statNumDirty = 0;
		[HideInInspector] public int statNumSplit = 0;
		[HideInInspector] public int statNumMerge = 0;
		[HideInInspector] public float statLastMergeTime = 0;
		[HideInInspector] public float statLastSplitTime = 0;
		[HideInInspector] public float statLastCheckLODNeedToChangeTime = 0;
		[HideInInspector] public float statTotalCheckLODNeedToChangeTime = 0;
		
		void Awake(){
			destroyedFrameCount = -1;
			if (LOG_LEVEL >= MB2_LogLevel.debug){
				frameInfo = new BakeDiagnostic[30];
			}
			_Setup();
		}
	
		void _Setup(){
			if (isSetup) return;
			checkScheduler = new LODCheckScheduler();
			checkScheduler.Init(this);
			if (bakers.Length == 0){
				Debug.LogWarning("LOD Manager has no bakers. LOD objects will not be added to any combined meshes.");	
			}
			
			if (numBakersPerGC <= 0){
				MB2_Log.Log(MB2_LogLevel.info,"LOD Manager Number of Bakes before gargage collection is less than one. Garbage collector will never be run by the LOD Manager.", LOG_LEVEL);	
			}
			
			if (maxCombineTimePerFrame <= 0){
				Debug.LogError("Combine Time Per Frame must be greater than zero.");	
			}
			
			dirtyCombinedMeshes.Clear();
			
			for (int i = 0; i < bakers.Length; i++){
				if (!bakers[i].Initialize(bakers)){
					ENABLED = false;
					return;
				}
			}
			
			MB2_Log.Log(MB2_LogLevel.info,"LODManager.Start called initialized " + bakers.Length + " bakers", LOG_LEVEL);
			//Bake all the meshese
			UpdateMeshesThatNeedToChange();
			isSetup = true;
		}
	
		public void SetupHierarchy(MB2_LOD lod){
			if (!isSetup) _Setup();
			if (!isSetup) return; // there are problems don't let clusters register
			lod.SetupHierarchy(bakers,ignoreLightmapping);
		}
	
		public void RemoveClustersIntersecting(Bounds bnds){
			if (!ENABLED) return;
			MB2_Log.Log(MB2_LogLevel.debug,"MB2_LODManager.RemoveClustersIntersecting " + bnds,LOG_LEVEL);
			for (int i = 0; i < bakers.Length; i++){
				bakers[i].baker.RemoveCluster(bnds);
			}
		}
	
		public void AddDirtyCombinedMesh(LODCombinedMesh c){
			if (!dirtyCombinedMeshes.ContainsKey(c)) dirtyCombinedMeshes.Add(c,c);
		}	
		
		void Update(){
			//Profile.StartProfile("MB2_LODManager.Update");
			checkScheduler.CheckIfLODsNeedToChange();
			//Profile.EndProfile("MB2_LODManager.Update");
		}
	
		//need to use Update instead of late update so Destroyed LODs can be removed
		//from mesh before being destroyed
		void LateUpdate(){
			//if (Time.deltaTime > .04f) Debug.Log("Previous frame was slow " + Time.frameCount);
			//Profile.StartProfile("MB2_LODManager.LateUpdate");
			//Profile.StartProfile("MB2_LODManager.GarbageCollection");
			if (System.GC.CollectionCount(0) > GCcollectionCount) GCcollectionCount = System.GC.CollectionCount(0);

			//Profile.EndProfile("MB2_LODManager.GarbageCollection");

			UpdateMeshesThatNeedToChange();
			//Profile.StartProfile("MB2_LODManager.LateUpdate_Extra");
			DestroyObjectsInLimbo();
			UpdateSkinnedMeshApproximateBoundsIfNecessary();
			if (LOG_LEVEL == MB2_LogLevel.debug){
				if (frameInfo == null) frameInfo = new BakeDiagnostic[30];
				int idx = Time.frameCount % 30;
				frameInfo[idx] = new BakeDiagnostic(this);
				if (idx == 0) frameInfo[29].deltaTime = (int) (Time.deltaTime * 1000f);
				else frameInfo[idx-1].deltaTime = (int) (Time.deltaTime * 1000f);
				if (idx == 29){
					Debug.Log(BakeDiagnostic.PrettyPrint(frameInfo));
				}
			}
			//Profile.EndProfile("MB2_LODManager.LateUpdate_Extra");
			//Profile.EndProfile("MB2_LODManager.LateUpdate");
		}
	
		void UpdateMeshesThatNeedToChange(){
			if (destroyedFrameCount == Time.frameCount) return;
			if (!ENABLED) return;
			if (!baking_enabled) return;
			if (lodCameras == null || lodCameras.Length == 0) return;
			statNumDirty = dirtyCombinedMeshes.Count;

			//Profile.StartProfile("MB2_LODManager.UpdateMeshesThatNeedToChange");
			float startTime = Time.realtimeSinceStartup;		
		
			if (bakesSinceLastGC > numBakersPerGC){
				float sTime = Time.realtimeSinceStartup;
				System.GC.Collect();
				statLastGarbageCollectionTime = Time.realtimeSinceStartup - sTime;
				statTotalGarbageCollectionTime += statLastGarbageCollectionTime;
				statLastGCFrame = Time.frameCount;
				bakesSinceLastGC = 0;
			}
			
			float totalElapsedTime = 0f;		
			if (statNumDirty > 0){
				List<LODCombinedMesh> dirtyCombinedMeshsList = PrioritizeCombinedMeshs();
				if (LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace, String.Format("LODManager.UpdateMeshesThatNeedToChange called. dirty clusters= {0}",dirtyCombinedMeshsList.Count), LOG_LEVEL);
//				int numCombinedMeshsBakedImmediate = 0;
//				int numCombinedMeshsBakedNonImmediate = 0;
				statLastNumBakes = 0;
				int i = dirtyCombinedMeshsList.Count - 1;
				// do the bakeImmediately
				while (i >= 0 && dirtyCombinedMeshsList[i].NumBakeImmediately() > 0){
					float sTime = Time.realtimeSinceStartup;
					LODCombinedMesh cl = dirtyCombinedMeshsList[i];
					if(cl.cluster == null){
						dirtyCombinedMeshsList.RemoveAt(i);
					} else {
						cl.Bake();
						//bake callbacks might make dirty so check before removing
						if (!cl.IsDirty()) dirtyCombinedMeshes.Remove(cl);
//						numCombinedMeshsBakedImmediate++;
						float elapsedTime = Time.realtimeSinceStartup - sTime;
						if (elapsedTime > statMaxCombinedMeshBakeTime) statMaxCombinedMeshBakeTime = elapsedTime;
						if (elapsedTime < statMinCombinedMeshBakeTime) statMinCombinedMeshBakeTime = elapsedTime;
						statAveCombinedMeshBakeTime = statAveCombinedMeshBakeTime * (statTotalNumBakes - 1f) / (float) statTotalNumBakes + elapsedTime / statTotalNumBakes;
						totalElapsedTime += elapsedTime;
					}
					i--;
				}
				
				// do as many other bakes as we can with remaining time
				while (Time.realtimeSinceStartup - startTime < maxCombineTimePerFrame && i >= 0){	
					float sTime = Time.realtimeSinceStartup;
					if(dirtyCombinedMeshsList[i].cluster == null){
						dirtyCombinedMeshsList.RemoveAt(i);
					} else {				
						dirtyCombinedMeshsList[i].Bake();
//						numCombinedMeshsBakedNonImmediate++;
						//bake callbacks might make dirty so check before removing
					 	if (!dirtyCombinedMeshsList[i].IsDirty()) dirtyCombinedMeshes.Remove(dirtyCombinedMeshsList[i]);
						float elapsedTime = Time.realtimeSinceStartup - sTime;
						if (elapsedTime > statMaxCombinedMeshBakeTime) statMaxCombinedMeshBakeTime = elapsedTime;
						if (elapsedTime < statMinCombinedMeshBakeTime) statMinCombinedMeshBakeTime = elapsedTime;
						statAveCombinedMeshBakeTime = statAveCombinedMeshBakeTime * (statTotalNumBakes - 1f) / (float) statTotalNumBakes + elapsedTime / statTotalNumBakes;
						totalElapsedTime += elapsedTime;
					}
					i--;
				}
//				statLastNumBakes = numCombinedMeshsBakedNonImmediate + numCombinedMeshsBakedImmediate;
				bakesSinceLastGC += statLastNumBakes;
				statLastCombinedMeshBakeTime = totalElapsedTime;
				statTotalCombinedMeshBakeTime += statLastCombinedMeshBakeTime;
				//statTotalNumBakes is updated when MeshBaker is called
//				if (numCombinedMeshsBakedImmediate > 0 || numCombinedMeshsBakedNonImmediate > 0){
					//statFrameSinceLastBake = Time.frameCount - statLastBakeFrame;
					//Debug.Log( String.Format("LODManager.UpdateMeshesThatNeedToChange frame {0} baked for {1} sec immediate clusters={2}, clusters={3}. Remaining clusters to be baked={4}", Time.frameCount, totalElapsedTime, numCombinedMeshsBakedImmediate, numCombinedMeshsBakedNonImmediate, dirtyCombinedMeshes.Count));
//				}
			}
			//Profile.EndProfile("MB2_LODManager.UpdateMeshesThatNeedToChange");				
			if (CHECK_INTEGRITY) checkIntegrity();
		}
		
		List<LODCombinedMesh> PrioritizeCombinedMeshs(){
			Plane[][] cameraFustrums = new Plane[lodCameras.Length][]; //todo cache these
			for (int i = 0; i < cameraFustrums.Length; i++){
				cameraFustrums[i] = GeometryUtility.CalculateFrustumPlanes(lodCameras[i].GetComponent<Camera>());
			}
			Vector3[] cameraPositions = new Vector3[lodCameras.Length]; //todo cache these
			for (int i = 0; i < cameraPositions.Length; i++){
				cameraPositions[i] = lodCameras[i].transform.position;
			}			
			List<LODCombinedMesh> dirtyCombinedMeshsList = new List<LODCombinedMesh>(dirtyCombinedMeshes.Keys);
			for (int i = dirtyCombinedMeshsList.Count - 1; i >= 0; i--){
				if (dirtyCombinedMeshsList[i].cluster == null){ 
					dirtyCombinedMeshsList.RemoveAt(i);
				}else {
					dirtyCombinedMeshsList[i].PrePrioritize(cameraFustrums, cameraPositions);
				}
			}
			dirtyCombinedMeshsList.Sort(combinedMeshPriorityComparer);
			return dirtyCombinedMeshsList;
		}
	
		void checkIntegrity(){
			for (int i = 0; i < bakers.Length; i++){
				bakers[i].baker.CheckIntegrity();
			}		
		}
		
		void printSet(HashSet<Material> s){
			IEnumerator e = s.GetEnumerator();
			Debug.Log("== Set =====");
			while(e.MoveNext()){
				Debug.Log(e.Current);	
			}
		}
		
		void OnDestroy(){
			destroyedFrameCount = Time.frameCount;
			MB2_Log.Log(MB2_LogLevel.debug, "Destroying LODManager", LOG_LEVEL);
			for (int i = 0; i < bakers.Length; i++){
				if (bakers[i].baker != null){
					bakers[i].baker.Destroy();
				}
			}
			//Profile.PrintResults();
		}	
		
		void OnDrawGizmos(){
			if (!ENABLED) return;
			if (bakers != null){
				for (int i = 0; i < bakers.Length; i++){
					if (bakers[i].baker != null){
						bakers[i].baker.DrawGizmos();
					}
				}
			}
		}
		
		public string GetStats(){
			String statsString = "";
			statsString += "statTotalNumBakes=" + statTotalNumBakes + "\n";
			statsString += "statTotalNumSplit=" + statNumSplit + "\n";
			statsString += "statTotalNumMerge=" + statNumMerge + "\n";
			statsString += "statAveCombinedMeshBakeTime=" + statAveCombinedMeshBakeTime + "\n";
			statsString += "statMaxCombinedMeshBakeTime=" + statMaxCombinedMeshBakeTime + "\n";
			statsString += "statMinCombinedMeshBakeTime=" + statMinCombinedMeshBakeTime + "\n";

			statsString += "statTotalGarbageCollectionTime=" + statTotalGarbageCollectionTime + "\n";
			statsString += "statTotalCombinedMeshBakeTime=" + statTotalCombinedMeshBakeTime + "\n";
			statsString += "statTotalCheckLODNeedToChangeTime=" + statTotalCheckLODNeedToChangeTime + "\n";

			statsString += "statLastSplitTime=" + statLastSplitTime + "\n";
			statsString += "statLastMergeTime=" + statLastMergeTime + "\n";
		
			statsString += "statLastGarbageCollectionTime=" + statLastGarbageCollectionTime + "\n";
			statsString += "statLastBakeFrame=" + statLastBakeFrame + "\n";
			statsString += "statNumDirty=" + statNumDirty + "\n";
			return statsString;
		}
	
//		public static MB2_LOD GetLODComponentInAncestor(Transform tt, bool highest=false){
//			MB2_LOD h;
//			if (highest) h = t.GetComponent<MB2_LOD>();
//			Transform t = tt;
//			while (t != null){
//				MB2_LOD c = t.GetComponent<MB2_LOD>();
//				if (c != null) return c;
//				if (t == t.root) return null;
//				t = t.parent;
//			}
//			return null;
//		}
	
		public static T GetComponentInAncestor<T>(Transform tt, bool highest=false) where T : Component{
			Transform t = tt;
			if (highest){
				T h = null;
				while(t != null){
					T h2 = t.GetComponent<T>();
					if (h2 != null) h = h2;
					if (t == t.root) break;
					t = t.parent;
				}
				return h;
			} else {
				while(t != null && t.parent != t){;
					T bb = t.GetComponent<T>();
					if (bb != null) return bb;
					t = t.parent;
				}
				return null;
			}
		}	
	
		public MB2_LODCamera[] GetCameras(){
			if (lodCameras == null){
				MB2_LODCamera[] lcs = (MB2_LODCamera[]) MB2_Version.FindSceneObjectsOfType(typeof(MB2_LODCamera));
				if (lcs.Length == 0){
					MB2_Log.Log(MB2_LogLevel.error,"There was no cameras in the scene with an MB2_LOD camera script attached",LOG_LEVEL);
				} else {
					lodCameras = lcs;
				}			
			}
			return lodCameras;
		}
	
		public void AddBaker(BakerPrototype bp){
			if (!bp.Initialize(bakers)) return;
			BakerPrototype[] newBPs = new BakerPrototype[bakers.Length + 1];
			Array.Copy(bakers,newBPs,bakers.Length);
			newBPs[newBPs.Length - 1] = bp;
			bakers = newBPs;
			Debug.Log((bakers[0] == bp) + " a " + bakers[0].Equals(bp));
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"Adding Baker to LODManager.",LOG_LEVEL);
		}
	
		public void RemoveBaker(BakerPrototype bp){
			List<BakerPrototype> newBPs = new List<BakerPrototype>();
			newBPs.AddRange(bakers);
			Debug.Log((bakers[0] == bp) + " " + bakers[0].Equals(bp));
			if (newBPs.Contains(bp)){
				bp.Clear();
				newBPs.Remove(bp);
				bakers = newBPs.ToArray();
				if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"Found BP and removed",LOG_LEVEL);			
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"Remving Baker from LODManager.",LOG_LEVEL);
		}
	
		public void AddCamera(MB2_LODCamera cam){
			MB2_LODCamera[] cams = GetCameras();
			for (int i = 0; i < cams.Length; i++) if (cam == cams[i]) return;
			MB2_LODCamera[] newCams = new MB2_LODCamera[cams.Length + 1];
			Array.Copy(cams,newCams,cams.Length);
			newCams[newCams.Length - 1] = cam;
			lodCameras = newCams;
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug, "MB2_LODManager.AddCamera added a camera length is now " + lodCameras.Length,LOG_LEVEL);
		}
	
		public void RemoveCamera(MB2_LODCamera cam){
			MB2_LODCamera[] cams = GetCameras();
			List<MB2_LODCamera> newCams = new List<MB2_LODCamera>();
			newCams.AddRange(cams);
			newCams.Remove(cam);
			lodCameras = newCams.ToArray();
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug, "MB2_LODManager.RemovedCamera removed a camera length is now " + lodCameras.Length,LOG_LEVEL);
		}
		
		public void LODDestroy(MB2_LOD lodComponent){		
			lodComponent.SetWasDestroyedFlag();
			MB2_LOD[] lodsInThisAndChildren = lodComponent.GetComponents<MB2_LOD>();
			for (int i = 0; i < lodsInThisAndChildren.Length; i++){
				if (lodComponent != lodsInThisAndChildren[i]){
					LODDestroy(lodsInThisAndChildren[i]);
				}
			}
			bool inLimbo = false;
			if (!lodComponent.isInCombined) {
				MonoBehaviour.Destroy(lodComponent.gameObject);
			}else{
				MB2_Version.SetActiveRecursively(lodComponent.gameObject,false);
				limbo.Add(lodComponent);
				inLimbo = true;
			}
			if (LOG_LEVEL == MB2_LogLevel.trace) MB2_Log.Log(MB2_LogLevel.trace, "MB2_LODManager.LODDestroy " + lodComponent + " inLimbo=" + inLimbo,LOG_LEVEL);
		}
		
		void DestroyObjectsInLimbo(){
			if (limbo.Count == 0) return;
			int numDest = 0;
			for (int i = limbo.Count - 1; i >= 0; i--){
				if (limbo[i] == null){
					Debug.LogWarning("An object that was destroyed using LODManager.Manager().Destroy was also destroyed using unity Destroy. This object cannot be cleaned up by the LODManager.");
					continue;
				}
				if (!limbo[i].isInCombined){
					GameObject.Destroy(limbo[i].gameObject);
					limbo.RemoveAt(i);
					numDest++;
				} 
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"MB2_LODManager DestroyObjectsInLimbo destroyed " + numDest,LOG_LEVEL);
		}

		void UpdateSkinnedMeshApproximateBoundsIfNecessary(){
			for (int i = 0; i < bakers.Length; i++){
				if (bakers[i].updateSkinnedMeshApproximateBounds == true && bakers[i].meshBaker.meshCombiner.renderType == MB_RenderType.skinnedMeshRenderer){
					bakers[i].baker.UpdateSkinnedMeshApproximateBounds();
				}
			}
		}
	
		public int GetNextFrameCheckOffset(){
			return checkScheduler.GetNextFrameCheckOffset();	
		}
	
		public float GetDistanceSqrToClosestPerspectiveCamera(Vector3 pos){
			if (lodCameras.Length == 0) return 0f;
			float distSqr = float.PositiveInfinity;
			for (int i = 0; i < lodCameras.Length; i++){
				MB2_LODCamera cam = lodCameras[i];
				if (cam.enabled && MB2_Version.GetActive(cam.gameObject) && cam.GetComponent<Camera>().orthographic == false){
					Vector3 d = cam.transform.position - pos;
					float distSqr2 = Vector3.Dot(d,d);
					if (distSqr2 < distSqr) distSqr = distSqr2;
				}
			}
			return distSqr;
		}
	
		public void ForceBakeAllDirty(){
			foreach (LODCombinedMesh cm in dirtyCombinedMeshes.Keys){
				cm.ForceBakeImmediately();
			}
		}

		/// <summary>
		/// The purpose of this is so that a world can be "slipped" to the origin if the player has moved far from the origin 
	    /// to avoid floating point rounding problems. 
		/// This updates the the cluster bounds, and resets the lod positions if the world has been translated.
		/// Does NOT move the LOD game objects. These should be moved before TranslateWorld is called.
		/// This will move the combined meshes.
		/// This should be called in LateUpdate, after all LODs have been checked
		/// This is expensive so don't call it frequently.
		/// </summary>
		/// <param name="translation">Translation.</param>
		public void TranslateWorld(Vector3 translation){
			for (int i = 0; i < bakers.Length; i++){
				if (bakers[i].clusterType == BakerPrototype.CombinerType.grid){
					((LODClusterManagerGrid) (bakers[i].baker)).TranslateAllClusters(translation);
				}
			}
		}
	}