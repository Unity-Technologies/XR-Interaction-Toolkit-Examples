using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;

public enum MB2_LODOperation{
	toAdd,
	update,
	delete,
	none,
}

/*
 <summary>
 MB2_LOD component
 
  Allowed states once set up
 	notInQueue, notInCombined, noAction=none
 	notInQueue, InCombined, Action=none
 	InQueue, notInCombined, action=Add
 	InQueue, notInCombined, action=Update
 	InQueue, InCombined, Action=none
 	InQueue, InCombined, action=Update
     InQueue, InCombined, action=Remove
  Not Allowed states
 	if inQueue action must be add, update, delete
     if notInQueue action must be none
 	InQueue, InCombined, action=Add
 	InQueue, notInCombined, action=Remove

  Philosophy

    A transaction is considered to take place at the time an LOD transition occurs.
	The game object hierarchy will be updated at this time
    However the baking may be deferred several frames and may be canceled before it occurs

  ANATOMY OF A TRANSITION

     detect LOD change

	 cancel any existing enqued transactions and undo changes they made

     determine which operation is needed

     switch which LOD hierarchy is active

     enqueue with combiner

     respond to bake callbacks

 </summary>
*/

[AddComponentMenu("Mesh Baker/LOD")]
public class MB2_LOD : MonoBehaviour {
	public enum SwitchDistanceSetup{
		notSetup,
		error,
		setup
	}
	
	[System.Serializable]
	public class LOD{
		public bool swapMeshWithLOD0=false;
		public bool bakeIntoCombined=true;
		public Animation anim;
		public Renderer lodObject;
		public int instanceID;
		public GameObject root;
		public float screenPercentage;
		public float dimension;
		public float sqrtDist;
		public float switchDistances;
		public int numVerts;
	}

	public class MB2_LODDistToCamComparer : IComparer<MB2_LOD>{
	   int IComparer<MB2_LOD>.Compare(MB2_LOD aObj, MB2_LOD bObj){
			return (int) (aObj._distSqrToClosestCamera - bObj._distSqrToClosestCamera);
	   }
	}	
	
	public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;
	public LODLog myLog;
	
	public string bakerLabel = "";
	public MB_RenderType renderType = MB_RenderType.meshRenderer;
	/// <summary>
	/// The force to level. forces the LOD into a particular level. If this setting is -1 it is ignored. otherwise the LOD is kept at the specified level.
	/// </summary>
	public int forceToLevel = -1;
	public bool bakeIntoCombined = true;
	public LOD[] levels;
	/// <summary>
	/// stores the mesh of the LOD0 object in case
	/// swapMeshWithLOD0 is used
	/// </summary>
	Mesh lodZeroMesh;

	float _distSqrToClosestCamera;
	public float distanceSquaredToClosestCamera{
		get {return _distSqrToClosestCamera;}
	}
	
	LODCombinedMesh combinedMesh;
	MB2_LODManager.BakerPrototype baker;
	MB2_LOD hierarchyRoot;
	
	int currentLODidx; //index in levels array of LOD we are currently in and transitioning from
	public int currentLevelIdx{
		get{return currentLODidx;}
	}
	int nextLODidx;    //index in levels array of LOD we are transitioning to
	public int nextLevelIdx{
		get{return nextLODidx;}	
	}
	
	int orthographicIdx; //stored for display to explain if was ortho or perspective
	
	MB2_LODManager manager = null;
	SwitchDistanceSetup setupStatus = SwitchDistanceSetup.notSetup;
	bool clustersAreSetup = false;
	
	bool _wasLODDestroyed;	
	public void SetWasDestroyedFlag(){
		_wasLODDestroyed = true;	
	}
	
	Vector3 _position;	
	
	bool _isInCombined = false;
	public bool isInCombined{
		get {return _isInCombined;}
	}
	
	bool _isInQueue = false;
	public bool isInQueue{
		get {return _isInQueue;}
	}
	
	[System.NonSerialized] public MB2_LODOperation action = MB2_LODOperation.none;
	
	public void Start(){
		if (setupStatus == SwitchDistanceSetup.notSetup && Init()) {
			MB2_LOD absoluteHighestInHierarchy = MB2_LODManager.GetComponentInAncestor<MB2_LOD>(transform,true);
			manager.SetupHierarchy(absoluteHighestInHierarchy);
		}
		if (combinedMesh != null && MB2_LODManager.CHECK_INTEGRITY) combinedMesh.GetLODCluster().CheckIntegrity();
	}
	
	public string GetStatusMessage(){
		float d = Mathf.Sqrt(_distSqrToClosestCamera);
		string status = "";
		status += "isInCombined= " + isInCombined + "\n";
		status += "isInQueue= " + isInQueue + "\n";
		status += "currentLODidx= " + currentLODidx + "\n";
		if (nextLODidx != currentLODidx) status += " switchingTo= " + nextLODidx + "\n";
		status += "bestOrthographicIdx=" + orthographicIdx + "\n";
		status += "dist to camera= " + d + "\n";
		if (combinedMesh != null) status += "meshbaker=" + combinedMesh.GetClusterManager().GetBakerPrototype().baker;
		return status;
	}
	
//	public GameObject GetCurrentLODObj() {
//		if (nextLODidx >= levels.Length) return null;
//		if (levels[nextLODidx].swapMeshWithLOD0){
//			return levels[0].lodObject.gameObject;
//		} else {
//			return levels[nextLODidx].lodObject.gameObject;
//		}
//	}

	public void _ResetPositionMarker(){
		_position = transform.position;
//		if (!combinedMesh.cluster.Contains(_position)){
//			Debug.LogError("LOD was moved to a position that is not in the cluster. " + ToString ());
//		}
	}
	
	public LODCombinedMesh GetCombiner(){
		return combinedMesh;	
	}
	
	public void SetCombiner(LODCombinedMesh c){
		combinedMesh = c;	
	}	
	
	public Vector3 GetHierarchyPosition(){
		return hierarchyRoot._position;	
	}
	
	public void AdjustNextLevelIndex(int newIdx){
		if (newIdx < 0){
			Debug.LogError("Bad argument " + newIdx);
			return;
		}
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace, "AdjustNextLevelIndex " + this + " newIdx=" + newIdx, LOG_LEVEL);
		if (newIdx > levels.Length) newIdx = levels.Length;
		DoStateTransition(newIdx);		
	}
	
//	public GameObject GetLODObjInMesh() {
//		if (currentLODidx >= levels.Length){
//			Debug.LogError("Called GLODObjInMesh when level was too high.");
//			return null;
//		}
//		if (levels[currentLODidx].lodObject == null) {
//			Debug.LogError("An LOD object was destroyed while still part of a combined mesh. Try using the MB2_LODManager.Manager().LODDestroy method to destroy LODs");
//			return null;
//		}
//		if (levels[currentLODidx].swapMeshWithLOD0){
//			return levels[0].lodObject.gameObject;
//		} else {
//			return levels[currentLODidx].lodObject.gameObject;
//		}
//	}
	
	public int GetGameObjectID(int idx){
		if (idx >= levels.Length){
			Debug.LogError("Called GetGameObjectID when level was too high. " + idx);
			return -1;
		}
		if (levels[idx].swapMeshWithLOD0){
			return levels[0].instanceID;
		} else {
			return levels[idx].instanceID;
		}		
	}
	
	public GameObject GetRendererGameObject(int idx){
		if (idx >= levels.Length){
			Debug.LogError("Called GetRendererGameObject when level was too high. " + idx);
			return null;
		}
		if (levels[idx].swapMeshWithLOD0){
			return levels[0].lodObject.gameObject;
		} else {
			return levels[idx].lodObject.gameObject;
		}			
	}
	
	public int GetNumVerts(int idx){
		if (idx >= levels.Length){
			return 0;
		}
		return levels[idx].numVerts;				
	}
	
	bool Init(){
		if (setupStatus == SwitchDistanceSetup.setup) return true;
		if (MB2_LODManager.CHECK_INTEGRITY){
			myLog = new LODLog(100);
		} else {
			myLog = new LODLog(0);
		}
		manager = MB2_LODManager.Manager();		
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + "Init called",LOG_LEVEL);	

		if (!MB2_LODManager.ENABLED) return false;
		if (isInQueue || isInCombined){
			Debug.LogError("Should not call Init on LOD that is in queue or in combined.");	
			return false;
		}
				
		_isInCombined = false;
		setupStatus = SwitchDistanceSetup.notSetup;
	
		if (manager == null){
			Debug.LogError("LOD coruld not find LODManager");
			return false;
		}		
		
		MB2_LODCamera[] lodCameras = manager.GetCameras();
		if (lodCameras.Length == 0){
			Debug.LogError("There is no camera with an MB2_LODCamera script on it.");
			return false;
		}

		float fov = 60f;
		bool mainIsPersp = false;
		for (int i = 0 ; i < lodCameras.Length; i++){
			Camera c = lodCameras[i].GetComponent<Camera>();
			if (c == null){
				Debug.LogError("MB2_LODCamera script is not not attached to an object with a Camera component.");
				setupStatus = SwitchDistanceSetup.error;
				return false;			
			}
			if (c == Camera.main && !c.orthographic){
				fov = c.fieldOfView;
				mainIsPersp = true;
			}
			if (!c.orthographic && mainIsPersp == false) fov = c.fieldOfView;
		}

        if (levels != null){
            if (!bakeIntoCombined)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    if (levels[i].bakeIntoCombined)
                    {
                        Debug.LogWarning("Setting bakeIntoCombined to false for level "+ i + " because bakeIntoCombined was false for the LOD");
                        levels[i].bakeIntoCombined = false;
                    }
                }
            }

            for (int i = 0; i < levels.Length; i++){
				LOD l = levels[i];
				if (l.lodObject == null){
					Debug.LogError(this + " LOD Level " + i + " does not have a renderer.");
					return false;						
				}
				if (l.lodObject is SkinnedMeshRenderer && renderType == MB_RenderType.meshRenderer){
					Debug.LogError(this + " LOD Level " + i + " is a skinned mesh but Baker Render Type was MeshRenderer. Baker Render Type must be set to SkinnedMesh on this LOD component.");
					return false;						
				}
				if (!l.lodObject.transform.IsChildOf(transform)){
					Debug.LogError(this + " LOD Level " + i + " is not a child of the LOD object.");
					return false;						
				}
				if (l.lodObject.gameObject == this.gameObject){
					Debug.LogError(this + " MB2_LOD component must be a parent ancestor of the level of detail renderers. It cannot be attached to the same game object as the level of detail renderers.");	
				}
				if (i == 0 && l.swapMeshWithLOD0 == true){
					Debug.LogWarning(this + " The first level of an LOD cannot have swap Mesh With LOD set.");
					l.swapMeshWithLOD0 = false;
				}
				Animation an = l.lodObject.GetComponent<Animation>();
				Transform t = l.lodObject.transform;
				while(t.parent != transform && t.parent != t){
					t = t.parent;
					if (an == null) an = t.GetComponent<Animation>();
				}
				l.anim = an;
				l.root = t.gameObject;
				if (l.swapMeshWithLOD0){
					MB2_Version.SetActiveRecursively(l.root,false);
				}
                if (l.bakeIntoCombined)
                {
                    l.numVerts = MB_Utility.GetMesh(l.lodObject.gameObject).vertexCount;
                }
				l.instanceID = l.lodObject.gameObject.GetInstanceID();
				
				//Todo check that there are no intervening LOD scripts between this and lodObject
				if (renderType == MB_RenderType.skinnedMeshRenderer && 
					combinedMesh != null &&
					combinedMesh.GetClusterManager().GetBakerPrototype().meshBaker != null &&
					combinedMesh.GetClusterManager().GetBakerPrototype().meshBaker.meshCombiner.renderType == MB_RenderType.meshRenderer){
					Debug.LogError(" LOD " + this + " RenderType is SkinnedMeshRenderer but baker " + i + " is a MeshRenderer. won't be able to add this to the combined mesh.");
					return false;
				}
			}
			lodZeroMesh = MB_Utility.GetMesh(levels[0].lodObject.gameObject);
			
			if (CalculateSwitchDistances(fov, true)){
				for (int i = 0; i < levels.Length; i++){
					if (levels[i].anim != null) levels[i].anim.Sample ();
					MySetActiveRecursively(i, false);
				}
				setupStatus = SwitchDistanceSetup.setup;
				currentLODidx = levels.Length;
				nextLODidx = levels.Length;
				_position = transform.position;					
				return true;
			} else {
				setupStatus = SwitchDistanceSetup.error;	
			}
		} else {
			Debug.LogError("LOD " + this + " had no levels.");
			setupStatus = SwitchDistanceSetup.error;			
			return false;	
		}
		return false;
	}

	Vector3 AbsVector3(Vector3 v){
		return new Vector3(Mathf.Abs (v.x),Mathf.Abs (v.y),Mathf.Abs (v.z));
	}	
	
	public bool CalculateSwitchDistances(float fieldOfView, bool showWarnings){
		bool success = true;
		if (levels.Length == 0){
			Debug.LogError(this + " does not have any LOD levels set up.");
			success = false;
		}
		
		for (int i = 1; i < levels.Length; i++){
			if (levels[i].screenPercentage >= levels[i - 1].screenPercentage){
				if (showWarnings) Debug.LogError("LOD object " + this + " screenPercentage must be in decending order");	
				success = false;
			}
		}		
		
		float[] hs = new float[levels.Length];
		for (int i = 0; i < levels.Length; i++){
			Renderer mr = levels[i].lodObject;
			if (mr != null){
				Bounds b = mr.bounds;
				if (mr is SkinnedMeshRenderer){ 
					//render.bounds does not always return a correct value in 4.3
					//get the bounds from the skinned mesh ourselves and transform it using
					//local2world matrix.
					//bounds of an inactive mesh renderer is always zero
					bool a = GetActiveSelf(mr.gameObject);
					bool e = mr.enabled;
					SetActiveSelf(mr.gameObject,true);
					mr.enabled = true;
					Matrix4x4 l2w = mr.transform.localToWorldMatrix;
					SkinnedMeshRenderer smr = (SkinnedMeshRenderer) mr;
					b =  new Bounds(l2w * (smr.localBounds.center),
					                AbsVector3(l2w * (smr.localBounds.size)));
					//b = mr.bounds;
					//Debug.Log ("localbounds " + ((SkinnedMeshRenderer)mr).localBounds.ToString ("F5")); 
					SetActiveSelf(mr.gameObject, a);
					mr.enabled = e;
					//Debug.Log ("cc b" + b.ToString ("F5") + " bnds " + mr.bounds.ToString ("F5") + " "  + i);
				}
				levels[i].switchDistances = 0;
				levels[i].sqrtDist = 0;
				if (levels[i].screenPercentage <= 0){
					if (showWarnings) Debug.LogError("LOD object " + this + " screenPercentage must be greater than zero.");	
					success = false;
					continue;
				}				
				float h = (b.size.x + b.size.y + b.size.z) / 3f;
				if (h == 0){
					if (showWarnings) Debug.LogError("LOD " + this + " the object has no size");
					success = false;
					continue;
				}
				hs[i] = h;
				levels[i].dimension = h;
				for (int j = 0; j < i; j++){
					if (hs[j] > 1.5f*h || hs[j] < h/1.5f){
						if (showWarnings) Debug.LogError("LOD " + this + " the render bounds of lod levels " + i + " and " + j + " are very differnt sizes." +
							"They should be very close to the same size. LOD uses these to determine when to switch from one LOD to another.");
						success = false;
					}
				}
				float d = 50f / Mathf.Tan( Mathf.Deg2Rad * fieldOfView / 2f); //distance to virtual screen that is 100 units wide
				levels[i].switchDistances =  h * d / (50f * levels[i].screenPercentage);
				levels[i].sqrtDist = levels[i].switchDistances;
				levels[i].switchDistances = levels[i].switchDistances * levels[i].switchDistances;	
			} else {
				success = false;	
			}
		}
		if (LOG_LEVEL >= MB2_LogLevel.trace && myLog != null) myLog.Log(MB2_LogLevel.trace,this + "CalculateSwitchDistances called fov=" +fieldOfView + " success="+success,LOG_LEVEL);		
		return success;
	}
	
	//like destroy but for recycling
	//Assumes that this LOD has been removed
	public void Clear(){
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + "Clear called",LOG_LEVEL);
		currentLODidx = levels.Length;// + 1;
		nextLODidx = currentLODidx;
		setupStatus = SwitchDistanceSetup.notSetup;
		_isInQueue = false;
		_isInCombined = false;
		action = MB2_LODOperation.none;
		combinedMesh = null;
		clustersAreSetup = false;		
	}
	
	public void CheckIfLODsNeedToChange() { 
		if (!MB2_LODManager.ENABLED) return;
//		if ((Time.frameCount + framesBetweenLODChangedChecksOffset) % framesBetweenLODChangedChecks != 0) return;

		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
		
		if (setupStatus == SwitchDistanceSetup.error || setupStatus == SwitchDistanceSetup.notSetup) return;
		MB2_LODCamera[] lodCameras = manager.GetCameras();
		if (lodCameras.Length == 0) return;

		////Profile.StartProfile("MB2_LOD.Update2");
		int lodIdx = levels.Length;
		if (forceToLevel != -1){
			if (forceToLevel < 0 || forceToLevel > levels.Length){
				Debug.LogWarning("Force To Level was not a valid level index value for LOD " + this);
			} else {
				lodIdx = forceToLevel;
				_distSqrToClosestCamera = levels[lodIdx].sqrtDist;
			}
		} else {
			_distSqrToClosestCamera = manager.GetDistanceSqrToClosestPerspectiveCamera(transform.position);
			for (int i = 0; i < levels.Length; i++){
				if (_distSqrToClosestCamera < levels[i].switchDistances){
					lodIdx = i;
					break;
				}
			}
			orthographicIdx = GetHighestLODIndexOfOrothographicCameras(lodCameras);
			if (orthographicIdx < lodIdx){
				lodIdx = orthographicIdx;
			}
		}
		//if (LOG_LEVEL == MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + " Update called current level=" + currentLODidx + " current next level=" + nextLODidx + " measured level=" + lodIdx,LOG_LEVEL);
		////Profile.EndProfile("MB2_LOD.Update2");
		if (lodIdx != nextLODidx){//if LOD needs to change
			////Profile.StartProfile("MB2_LOD.Update3");	
			if (bakeIntoCombined && combinedMesh.GetClusterManager().GetBakerPrototype().clusterType == MB2_LODManager.BakerPrototype.CombinerType.grid && _position != transform.position){
				Debug.LogError("Can't move LOD " + this + " after it has been added to a combined mesh unless baker type is 'Simple'"  );
				return;
			}			
			DoStateTransition(lodIdx);
			////Profile.EndProfile("MB2_LOD.Update3");
		}
	}
	
	void DoStateTransition(int lodIdx){
		if (lodIdx < 0 || lodIdx > levels.Length) Debug.LogError("lodIdx out of range " + lodIdx);

		//undo changes made by partially completed transaction
		if (_isInQueue) {
			if (action == MB2_LODOperation.toAdd){
				SwapBetweenLevels(nextLODidx,currentLODidx);
			} else if (action == MB2_LODOperation.delete){
				SwapBetweenLevels(nextLODidx,currentLODidx);
			}
			_CallLODCancelTransaction();
			action = MB2_LODOperation.none;
			nextLODidx = currentLevelIdx;
		}

		//deduce what the new action is
		MB2_LODOperation newAction = MB2_LODOperation.none;
		if (_isInCombined) {
			if (lodIdx == levels.Length || levels [lodIdx].bakeIntoCombined == false)
					newAction = MB2_LODOperation.delete;
			else
					newAction = MB2_LODOperation.update;
		} else if (lodIdx < levels.Length && levels [lodIdx].bakeIntoCombined == true) {
					//if (lodIdx == levels.Length || levels[lodIdx].bakeIntoCombined == false) newAction = MB2_LODOperation.delete; //might need to remove from queue
			newAction = MB2_LODOperation.toAdd;				
		} else {
			newAction = MB2_LODOperation.none;
		}
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + " DoStateTransition newA=" + newAction + " newNextLevel=" + lodIdx,LOG_LEVEL);

		//if adding or deleting from outside of levels.Length can enable or disable the hierarchies immediately
		if (newAction == MB2_LODOperation.toAdd && currentLevelIdx == levels.Length){
			SwapBetweenLevels(nextLODidx,lodIdx);
		} else if (newAction == MB2_LODOperation.delete && lodIdx == levels.Length){	
			SwapBetweenLevels(nextLODidx,lodIdx);
		} //update is not included here because swap happens in the onBake callback

        if (!bakeIntoCombined && lodIdx != currentLevelIdx)
        {
            SwapBetweenLevels(nextLODidx, lodIdx);
        }

		nextLODidx = lodIdx;
		if (bakeIntoCombined){
			action = newAction;	
			_CallLODChanged();
		} else {
			action = MB2_LODOperation.none;
			currentLODidx = nextLODidx;
			if (LOG_LEVEL >= MB2_LogLevel.debug) myLog.Log(MB2_LogLevel.debug,this + " LODChanged but not baking next level=" + nextLODidx,LOG_LEVEL);				
		}
	}

	//sets the LOD to an out of the combinedMesh state without baking
	public void ForceRemove(){
		action = MB2_LODOperation.none;
		_isInQueue = false;
		_isInCombined = false;
		if (currentLODidx != nextLODidx && currentLODidx < levels.Length) MySetActiveRecursively(currentLODidx,false);
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"ForceRemove called " + this,LOG_LEVEL);	
		if (nextLODidx < levels.Length) {
			MySetActiveRecursively(nextLODidx,true);
		}
		currentLODidx = nextLODidx;
	}
	
	public void ForceAdd(){
		if (isInCombined || isInQueue || nextLODidx >= levels.Length || levels[nextLODidx].bakeIntoCombined == false) return;
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"ForceAdd called " + this,LOG_LEVEL);		
		if (nextLODidx < levels.Length) action = MB2_LODOperation.toAdd;
		combinedMesh.LODChanged(this,false);
	}	

	void _CallLODCancelTransaction(){
		if (LOG_LEVEL >= MB2_LogLevel.debug) myLog.Log(MB2_LogLevel.debug,this + " calling _CallLODCancelTransaction action=" + action + " next level=" + nextLODidx,LOG_LEVEL);
		if (currentLevelIdx < levels.Length && currentLevelIdx > 0 && levels[currentLevelIdx].swapMeshWithLOD0){
			//get the mesh in idx and put it in zero
			Mesh m = MB_Utility.GetMesh(levels[currentLevelIdx].lodObject.gameObject);
			SetMesh(levels[0].lodObject.gameObject, m);
		} else if (currentLevelIdx == 0){
			//restore level zero mesh
			SetMesh(levels[0].lodObject.gameObject, lodZeroMesh);
		}
		combinedMesh.LODCancelTransaction(this);			
	}
	
	void _CallLODChanged(){
		if (LOG_LEVEL >= MB2_LogLevel.debug) myLog.Log(MB2_LogLevel.debug,this + " calling LODChanged action=" + action + " next level=" + nextLODidx,LOG_LEVEL);
		if (action != MB2_LODOperation.none) {
			if (nextLODidx < levels.Length && nextLODidx > 0 && levels[nextLODidx].swapMeshWithLOD0){
				//get the mesh in idx and put it in zero
				Mesh m = MB_Utility.GetMesh(levels[nextLODidx].lodObject.gameObject);
				SetMesh(levels[0].lodObject.gameObject, m);
			} else if (nextLODidx == 0){
				//restore level zero mesh
				SetMesh(levels[0].lodObject.gameObject, lodZeroMesh);
			}
		
			combinedMesh.LODChanged (this, false);
		} else if (nextLODidx == levels.Length ||
			(nextLODidx < levels.Length && levels[nextLODidx].bakeIntoCombined == false)) {
			OnBakeAdded();
		}
	}

	public void OnBakeRemoved(){
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"OnBakeRemoved " + this,LOG_LEVEL);
		if (!_isInQueue || action != MB2_LODOperation.delete || !isInCombined) Debug.LogError("OnBakeRemoved called on an LOD in an invalid state: " + ToString());
		_isInCombined = false;
		action = MB2_LODOperation.none;
		_isInQueue = false;		
		if (currentLODidx < levels.Length) MySetActiveRecursively(currentLODidx,false);
		if (nextLODidx < levels.Length) MySetActiveRecursively(nextLODidx,true);
		currentLODidx = nextLODidx;
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}

	public void OnBakeAdded(){
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"OnBakeAdded " + this,LOG_LEVEL);
		
		if (nextLODidx < levels.Length && levels[nextLODidx].bakeIntoCombined){
			if (!_isInQueue || action != MB2_LODOperation.toAdd || isInCombined) Debug.LogError("OnBakeAdded called on an LOD in an invalid state: " + ToString() + " log " + myLog.Dump());
			_isInCombined = true;
		} else {
			if (_isInQueue || action != MB2_LODOperation.none || isInCombined) Debug.LogError("OnBakeAdded called on an LOD in an invalid state: " + ToString() + " log " + myLog.Dump());			
			_isInCombined = false;	
		}
		_isInQueue = false;
		action = MB2_LODOperation.none;
		SwapBetweenLevels(currentLODidx,nextLODidx);
		currentLODidx = nextLODidx;
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	public void OnBakeUpdated(){
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"OnBakeUpdated " + this,LOG_LEVEL);
		if (!_isInQueue || action != MB2_LODOperation.update || !isInCombined) Debug.LogError("OnBakeUpdated called on an LOD in an invalid state: " + ToString());
		if (nextLODidx >= levels.Length) Debug.LogError("Update will remove all meshes from combined. This should never happen.");
		_isInQueue = false;
		_isInCombined = true;					
		action = MB2_LODOperation.none;
		SwapBetweenLevels(currentLODidx,nextLODidx);
		currentLODidx = nextLODidx;
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}

	public void OnRemoveFromQueue(){
		_isInQueue = false;
		action = MB2_LODOperation.none;
		nextLODidx = currentLODidx;
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"OnRemoveFromQueue complete " + this,LOG_LEVEL);				
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	public void OnAddToQueue(){
		_isInQueue = true;
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,"OnAddToAddQueue complete " + this,LOG_LEVEL);						
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();		
	}

	
	void OnDestroy(){
		if (setupStatus != SwitchDistanceSetup.setup) return;
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + "OnDestroy called",LOG_LEVEL);		
		if (!_wasLODDestroyed) myLog.Log(MB2_LogLevel.debug,"An MB2_LOD object " + this + " was destroyed using Unity's Destroy method. This can leave destroyed meshes in the combined mesh. Try using MB2_LODManager.Manager().LODDestroy() instead.", LOG_LEVEL);
		_removeIfInCombined();
		if (combinedMesh != null) combinedMesh.UnassignFromCombiner(this);
		combinedMesh = null;
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	
	void OnEnable(){
		if (setupStatus != SwitchDistanceSetup.setup) return;
		if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + "OnEnable called",LOG_LEVEL);				
		//May have been enabled with SetActiveRecursively which would enable all my children. Check and disable
		if (levels != null){
			for (int i = 0; i < levels.Length; i++){
				if (!_isInCombined && currentLODidx == i){
					MySetActiveRecursively(i,true);
				}else{;
					MySetActiveRecursively(i,false);
				}
			}
		}
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	void OnDisable(){
		if (myLog != null && LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + "OnDisable called",LOG_LEVEL);
		_removeIfInCombined();
		if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	void _removeIfInCombined(){
		if (_isInCombined || _isInQueue){
			for (int i = 0; i < levels.Length; i++){
				if (levels[i].lodObject != null &&
					!levels[i].swapMeshWithLOD0) MySetActiveRecursively(i,false);
			}
			nextLODidx = levels.Length;
			if (isInCombined || isInQueue){
				if (MB2_LODManager.Manager() != null) {
					action = MB2_LODOperation.delete;
					if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace,this + " Calling  LODManager.RemoveLOD",LOG_LEVEL);
					combinedMesh.RemoveLOD(this,true);
				}
			}
		}
	}
	
	public bool ArePrototypesSetup(){
		return clustersAreSetup;
	}
	
	public MB2_LODManager.BakerPrototype GetBaker(MB2_LODManager.BakerPrototype[] allPrototypes, bool ignoreLightmapping){
		if (baker != null) return baker;
		if (setupStatus != SwitchDistanceSetup.setup){
			if (!Init()) return null;
		}
		myLog.Log(MB2_LogLevel.debug,this + " GetBaker called setting up baker",LOG_LEVEL);
		MB2_LODManager.BakerPrototype finalBaker = null;
		for (int i = 0; i < levels.Length; i++){
            if (levels[i].bakeIntoCombined == false)
            {
                continue;
            }
			////Profile.StartProfile("MB2_LODManager.GetMultiMeshBakerFor1");
			Renderer r = levels[i].lodObject;
			Mesh m = MB_Utility.GetMesh(levels[i].lodObject.gameObject);
			Rect rc = new Rect(0,0,1,1);
			////Profile.StartProfile("MB2_LODManager.GetMultiMeshBakerFor1.1");
			bool hasOBUVs = MB_Utility.hasOutOfBoundsUVs(m,ref rc);
			////Profile.EndProfile("MB2_LODManager.GetMultiMeshBakerFor1.1");
			int lightmapIdx = r.lightmapIndex;
	
			HashSet<Material> mats = new HashSet<Material>();
			for (int j = 0; j < r.sharedMaterials.Length; j++){
				if (r.sharedMaterials[j] != null && r.sharedMaterials[j].shader != null){
					mats.Add(r.sharedMaterials[j]);
				}
			}
			
			////Profile.EndProfile("MB2_LODManager.GetMultiMeshBakerFor1");
			////Profile.StartProfile("MB2_LODManager.GetMultiMeshBakerFor2");
			//todo validate prototype matches if labeled
			//first check if there is a matching label
			if (bakerLabel != null && bakerLabel.Length > 0){ 
				for (int j = 0; j < allPrototypes.Length; j++){
					if (allPrototypes[j].label.Equals(bakerLabel)){
						if (!ignoreLightmapping && lightmapIdx  != allPrototypes[j].lightMapIndex) Debug.LogError("LOD " + this + " had a bakerLabel, but had a different lightmap index than that baker"); 
						if (!mats.IsSubsetOf(allPrototypes[j].materials)) Debug.LogError("LOD " + this + " had a bakerLabel, but had materials are not in that baker"); 
						finalBaker = allPrototypes[j];
						break;
					}
				}
				if (finalBaker == null) Debug.LogError("LOD " + this + " had a bakerLabel '" + bakerLabel + "' that was not matched by any baker");
				else continue;
			}
			
			////Profile.EndProfile("MB2_LODManager.GetMultiMeshBakerFor2");
			////Profile.StartProfile("MB2_LODManager.GetMultiMeshBakerFor3");
			MB2_LODManager.BakerPrototype mmb = null;
			MB2_LODManager.BakerPrototype mmbBest = null;
			string allErrors = "";
			string errorStr = "";
			for (int j = 0; j < allPrototypes.Length; j++){
				errorStr = "";
				MB2_LODManager.BakerPrototype mmbCandidate = null;
				MB2_LODManager.BakerPrototype mmbBestCandidate = null;				
				if (hasOBUVs){
					if (allPrototypes[j].materials.SetEquals(mats)){
						mmbBestCandidate = allPrototypes[j];
					}	
				}
				if (mats.IsSubsetOf(allPrototypes[j].materials)){
					mmbCandidate = allPrototypes[j];
				}
				if (!ignoreLightmapping && lightmapIdx != allPrototypes[j].lightMapIndex && mmbCandidate != null){
					errorStr += "\n  lightmapping check failed";
				}
				if (allPrototypes[j].meshBaker.meshCombiner.renderType == MB_RenderType.skinnedMeshRenderer && renderType != MB_RenderType.skinnedMeshRenderer && mmbCandidate != null){
					errorStr += "\n  rendertype did not match";
				}
				if (allPrototypes[j].meshBaker.meshCombiner.renderType == MB_RenderType.meshRenderer && renderType != MB_RenderType.meshRenderer && mmbCandidate != null){
					errorStr += "\n  rendertype did not match";
				}
				if (errorStr.Length == 0){
					if (mmbBestCandidate != null){
						if (mmbBest != null) {
							Debug.LogWarning("The set of materials on LOD " + this + " matched multiple bakers. Try use labels to resolve the conflict.");	
						}
						mmbBest = mmbBestCandidate;
					}
					if (mmbCandidate != null){
						if (mmb != null) {
							Debug.LogWarning("The set of materials on LOD " + this + " matched multiple bakers. Try use labels to resolve the conflict.");	
						}
						mmb = mmbCandidate;
					}
				} else {
					allErrors += "LOD " + i + " Baker " + j + " matched the materials but could not match because: " + errorStr;	
				}
			}
			
			if (mmbBest != null){
				finalBaker = mmbBest;
				continue;
			}
	#if UNITY_EDITOR
			if (mmbBest == null && hasOBUVs) Debug.LogWarning("LOD " + this + " has out of bounds UVs. It probably requires a baker that exactly matches the materials (no extra). Such a baker could not be found.");
	#endif
			if (mmb == null){
				string ms = "";
				foreach(Material mt in mats){
					ms += mt + ",";
				} //todo check multiple materials and fixOBuvs
				Debug.LogError("Could not find a baker that can accept the materials on LOD " + this + "\n" +
					"materials [" + ms + "]\n" +
					"lightmapIndex = " + lightmapIdx + " (ignore lightmapping = " + ignoreLightmapping + ")\n" +
					"out of bounds uvs " + hasOBUVs + " (if true then set of prototype materials must match exactly.)\n" +
					allErrors);	
				return null;
			}		
			////Profile.EndProfile("MB2_LODManager.GetMultiMeshBakerFor3");
			finalBaker = mmb;
		}
		baker = finalBaker;
		return baker;
	}
	
	public void SetupHierarchy(MB2_LODManager.BakerPrototype[] allPrototypes, bool ignoreLightmapping){
		if (LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.LogDebug("Setting up hierarchy for " + this);
		MB2_LOD h = MB2_LODManager.GetComponentInAncestor<MB2_LOD>(transform);
		if (h != this) Debug.LogError ("Should only be called on the root LOD.");
		hierarchyRoot = this;
		if (combinedMesh != null) return; //done
		GetBaker(allPrototypes,ignoreLightmapping);
		if (baker != null){
			LODCluster cell = baker.baker.GetClusterFor(GetHierarchyPosition());
			combinedMesh = cell.SuggestCombiner();
			cell.AssignLODToCombiner(this);
		} else
        {
            if (!bakeIntoCombined && !GetComponent<MB2_LODSoloChecker>())
            {
                gameObject.AddComponent<MB2_LODSoloChecker>();
            }
        }
		
		_RecurseSetup(transform, allPrototypes, ignoreLightmapping);
	}
	
	static void _RecurseSetup(Transform t, MB2_LODManager.BakerPrototype[] allPrototypes, bool ignoreLightmapping){
		for (int i = 0; i < t.childCount; i++){
			Transform tt = t.GetChild (i);
			MB2_LOD lod2 = tt.GetComponent<MB2_LOD>();			
			if (lod2 != null){
				if (lod2.Init ())lod2.GetBaker(allPrototypes,ignoreLightmapping);
				if (lod2.baker != null){
					lod2.FindHierarchyRoot();
					LODCluster cell = lod2.baker.baker.GetClusterFor(lod2.GetHierarchyPosition());
					lod2.combinedMesh = cell.SuggestCombiner();
					cell.AssignLODToCombiner(lod2);
//					if (lod2.framesBetweenLODChangedChecksOffset == -1){
//						lod2.framesBetweenLODChangedChecksOffset = lod2.combinedMesh.GetFrameCheckOffset();
//					}		
					lod2.clustersAreSetup = true;		
				}
			}
			_RecurseSetup(tt,allPrototypes,ignoreLightmapping);
		}		
	}
	
	public MB2_LOD GetHierarchyRoot(){ return hierarchyRoot;}
	
	MB2_LOD FindHierarchyRoot(){
		Transform t = transform.parent;
		MB2_LOD highest = this;
		while (t != null){ // search ancestor
			MB2_LOD c = t.GetComponent<MB2_LOD>();
			if (c != null){
				MB2_LODManager.BakerPrototype otherBaker = c.baker; //bakekrs in parents should have been setup		
				if (otherBaker != null && otherBaker == baker) highest = c;
			}
			if (t == t.root) break;
			t = t.parent;
		}
		hierarchyRoot = highest;
		return highest;
	}
	
	public override string ToString ()
	{
		string nxtID = "";
		if (nextLODidx < levels.Length){
			nxtID += levels[nextLODidx].instanceID;
		}
		return string.Format("[MB2_LOD {0} id={1}: inComb={2} inQ={3} act={4} nxt={5} curr={6} nxtRendInstId={7}]", name, GetInstanceID(), isInCombined, isInQueue, action, nextLODidx, currentLODidx, nxtID);
	}
	
	/// <summary>
	/// Used by the test harness. Compares expected values to current values.
	/// </summary>
	public void CheckState(bool exInCombined,
							bool exInQueue,
							MB2_LODOperation exAction,
							int exNextIdx,
							int exCurrentIdx){
			if (isInCombined != exInCombined) Debug.LogError("inCombined Test fail. was " + isInCombined + " expects=" + exInCombined);
			if (isInQueue != exInQueue) Debug.LogError(GetInstanceID() + " inQueue Test fail. was " + isInQueue + " expects=" + exInQueue);
			if (action != exAction) Debug.LogError("action Test fail. was " + action + " expects=" + exAction);
			if (nextLODidx != exNextIdx) Debug.LogError("next idx Test fail. was " + nextLODidx + " expects=" + exNextIdx);
			if (currentLODidx != exCurrentIdx) Debug.LogError("current idx Test fail. was " + currentLODidx + " expects=" + exCurrentIdx);
			if (MB2_LODManager.CHECK_INTEGRITY) CheckIntegrity();
	}
	
	void CheckIntegrity(){
		if (this == null) return;
		if (_isInCombined && currentLODidx >= levels.Length) Debug.LogError(this + " IntegrityCheckFailed invalid currentLODidx" + this);	
		if (action != MB2_LODOperation.none && isInQueue == false) Debug.LogError(this + " Invalid action if not in queue " + this);
		if (action == MB2_LODOperation.none && isInQueue == true) Debug.LogError(this + " Invalid action if in queue " + this);
		if (action == MB2_LODOperation.toAdd && isInCombined == true) Debug.LogError(this + " Invalid action if in combined " + this);
		if (action == MB2_LODOperation.delete && isInCombined == false) Debug.LogError(this + " Invalid action if not in combined " + this);
		if (action == MB2_LODOperation.delete && currentLODidx >= levels.Length) Debug.LogError(this + " Invalid currentLODidx " + currentLODidx);
		if (setupStatus == SwitchDistanceSetup.setup){
			for (int i = 0; i < levels.Length; i++){
				if (levels[i].lodObject != null){
					if (GetActiveSelf(levels[i].lodObject.gameObject)){
						if ((i == 0 && currentLODidx < levels.Length && levels[currentLODidx].swapMeshWithLOD0) || levels[i].swapMeshWithLOD0){
							
						} else {
							if (!isInQueue && i != currentLODidx){
								Debug.LogError("f=" + Time.frameCount + " " + this + " lodObject of wrong level was active was:" + i + " should be:" + currentLODidx);			
								Debug.Log("LogDump " + myLog.Dump());
							}
							Renderer r = levels[i].lodObject.GetComponent<Renderer>();
							if (_isInCombined && r.enabled){
								Debug.LogError("f=" + Time.frameCount + " " + this + " lodObject object in combined and its renderer was enabled id " + levels[i].instanceID + " when inCombined. should all be inactive " + currentLODidx);										
								Debug.Log("LogDump " + myLog.Dump());
							}
						}
					}
				}
			}
		}
		if (combinedMesh != null){
			if (!combinedMesh.IsAssignedToThis(this))Debug.LogError("LOD was assigned to combinedMesh but combinedMesh didn't contain " + this);
			LODCluster cell = combinedMesh.GetLODCluster();
			if (cell != null && !cell.GetCombiners().Contains (combinedMesh)) Debug.LogError ("Cluster was assigned to cell but it wasn't in its list of clusters");
		}
		//make sure something is visible
		if (GetActiveSelf(gameObject) && !_isInCombined){
			bool foundOne = false;
			for (int i = 0; i < levels.Length; i++){
				if (GetActiveSelf(levels[i].lodObject.gameObject) &&
				    levels[i].lodObject.enabled) foundOne = true;	
			}
			if (!foundOne && nextLODidx < levels.Length) Debug.LogError("All levels were invisible " + this);
		}
	}
	
	int GetHighestLODIndexOfOrothographicCameras(MB2_LODCamera[] cameras){
		if (cameras.Length == 0) return 0;
		int bestLODidx = levels.Length;
		for (int i = 0; i < cameras.Length; i++){
			MB2_LODCamera cam = cameras[i];
			if (cam.enabled && GetActiveSelf(cam.gameObject) && cam.GetComponent<Camera>().orthographic == true){
				float fustrumHeight = cam.GetComponent<Camera>().orthographicSize*2;
				float screenPercentage = levels[0].dimension / fustrumHeight;
				for (int j = 0; j < bestLODidx; j++){
					if (screenPercentage > levels[j].screenPercentage){
						if (j < bestLODidx){
							bestLODidx = j;
						}
						break;
					}
				}
			}
		}
		return bestLODidx;
	}
	
	void SwapBetweenLevels(int oldIdx,int newIdx){ 
		if (oldIdx < levels.Length && newIdx < levels.Length){
			//if switching between two swapMeshWithLOD0 levels don't want to
			//enable disable so that animations keep playing.
			if ((oldIdx == 0 || levels[oldIdx].swapMeshWithLOD0) && 
			    (newIdx == 0 || levels[newIdx].swapMeshWithLOD0)){
				gameObject.SendMessage("LOD_OnSetLODActive",levels[0].root,SendMessageOptions.DontRequireReceiver);
				return;
			}
		}
		if (oldIdx < levels.Length){
			MySetActiveRecursively(oldIdx,false);
		}
		if (newIdx < levels.Length){
			MySetActiveRecursively(newIdx,true);
		}
	}

	void MySetActiveRecursively(int idx, bool a){
		if (idx >= levels.Length) return;
		if (idx > 0 && levels[idx].swapMeshWithLOD0){
			if (a == true){
				//get the mesh in idx and put it in zero
				Mesh m = MB_Utility.GetMesh(levels[idx].lodObject.gameObject);
				SetMesh(levels[0].lodObject.gameObject, m);
				if (levels[0].anim != null) levels[0].anim.Sample();
			} else {
				//restore level zero mesh
				SetMesh(levels[0].lodObject.gameObject, lodZeroMesh);
				if (levels[0].anim != null) levels[0].anim.Sample();
			}
			if (GetActiveSelf(levels[0].root) != a){
				MB2_Version.SetActiveRecursively(levels[0].root,a);
				gameObject.SendMessage("LOD_OnSetLODActive",levels[0].root,SendMessageOptions.DontRequireReceiver);
			}
			if (a == true && !_isInCombined){
				levels[0].lodObject.enabled = true;
			}			
			if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace, "SettingActive (swaps mesh on level zero) to " + a + " for level " + idx + " on " + this, LOG_LEVEL);
		} else {
			if (GetActiveSelf(levels[idx].root) != a){
				MB2_Version.SetActiveRecursively(levels[idx].root,a);
				gameObject.SendMessage("LOD_OnSetLODActive",levels[idx].root,SendMessageOptions.DontRequireReceiver);
			}
			if (a == true && !_isInCombined){
				levels[idx].lodObject.enabled = true;
			}			
			if (LOG_LEVEL >= MB2_LogLevel.trace) myLog.Log(MB2_LogLevel.trace, "SettingActive to " + a + " for level " + idx + " on " + this, LOG_LEVEL);
		}
	}

	void SetMesh(GameObject go, Mesh m){ 
		if (go == null) return;
		MeshFilter mf = go.GetComponent<MeshFilter>();
		if (mf != null){
			mf.sharedMesh = m;
			return;
		}
		
		SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
		if (smr != null){
			smr.sharedMesh = m;
			return;
		}
		Debug.LogError("Object " + go.name + " does not have a MeshFilter or a SkinnedMeshRenderer component");
	}
	
			//todo check using this correctly
	public static bool GetActiveSelf(GameObject go){
		#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
		return go.active;
		#else
		return go.activeSelf;
		#endif			
	}
	
	public static void SetActiveSelf(GameObject go, bool isActive){
		#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
		go.active = isActive;
		#else
		go.SetActive(isActive);
		#endif
	}

	/// <summary>
	/// Don't want to call every frame is slower than expected
	/// Sets the combinedMesh for LOD.
	/// All LODs in a hierarchy need to use the same combinedMesh. 
	/// Searches the ancestors and children to see if combinedMesh has been set already
	/// </param>
//	LODCombinedMesh SetClusterForLOD(MB2_LODManager.BakerPrototype[] allPrototypes, bool ignoreLightmapping){
//		if (baker == null){
//			GetBaker(allPrototypes,ignoreLightmapping);
//			if (baker == null) return null; //error getting baker
//		}
//		if (hierarchyRoot == null) FindHierarchyRoot(baker,allPrototypes,ignoreLightmapping);
//		if (hierarchyRoot == this){
//
//			//make sure all LOD children are initialized
//		} else {
//			if (hierarchyRoot.combinedMesh == null) hierarchyRoot.SetClusterForLOD(allPrototypes,ignoreLightmapping);
//			combinedMesh = hierarchyRoot.combinedMesh;
//		}
//		combinedMesh.AssignToCluster(this);
//		return combinedMesh;
//	}
}
