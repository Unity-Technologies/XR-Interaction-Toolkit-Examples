using UnityEngine;
using System.Collections;
using DigitalOpus.MB.Core;

public class MB2_MeshBakerLODTestHarness : MonoBehaviour {
	
	/*
	 * tudor house should be 
	 * .5
	 * .2
	 * .08
	 * .04
	*/
	
	public static MB2_MeshBakerLODTestHarness harness;
	MB2_LODManager manager;
	MB2_LODCamera cam;
	public MB2_LOD lod;
	Test[] tests = new Test[] {
		//       lev, dobake, inComb, inQ,         action         , nx, curr  inComb , inQ   ,         action        , nx, curr
		//Test 0..3 basic add two updates and remove baking every frame
		new Test(2,    true,  false, true, MB2_LODOperation.toAdd ,  2,    4,   true ,  false,MB2_LODOperation.none  ,  2,    2),	
		new Test(3,    true,  true,  true, MB2_LODOperation.update,  3,    2,   true ,  false,MB2_LODOperation.none  ,  3,    3),
		new Test(1,    true,  true,  true, MB2_LODOperation.update,  1,    3,   true ,  false,MB2_LODOperation.none  ,  1,    1),
		new Test(4,    true,  true,  true, MB2_LODOperation.delete,  4,    1,   false,  false,MB2_LODOperation.none  ,  4,    4),	
		//Test 4..5 add without bake then delete
		new Test(2,    false,  false, true, MB2_LODOperation.toAdd ,  2,    4,   false ,  true,MB2_LODOperation.toAdd ,  2,    4),	
		new Test(4,    true,  false, false, MB2_LODOperation.none ,  4,    4,   false ,  false,MB2_LODOperation.none  ,  4,    4),
		//Test 6..8 add without bake then update/add then delete
		new Test(2,    false,  false, true, MB2_LODOperation.toAdd ,  2,    4,   false ,  true,MB2_LODOperation.toAdd ,  2,    4),	
		new Test(0,    false,  false, false, MB2_LODOperation.none  ,  0,    0,   false ,  false,MB2_LODOperation.none  ,  0,    0),
		new Test(4,    false,  false,  false, MB2_LODOperation.none,  4,    4,   false,  false,MB2_LODOperation.none  ,  4,    4),	
		//Test 9..12 add without bake then update/add without bake then update/add with bake then delete
		new Test(2,    false,  false, true, MB2_LODOperation.toAdd ,  2,    4,   false ,  true,MB2_LODOperation.toAdd ,  2,    4),	
		new Test(0,    false,  false, false, MB2_LODOperation.none  ,  0,    0,  false ,  false,MB2_LODOperation.none  ,  0,    0),
		new Test(3,    true,  false, true, MB2_LODOperation.toAdd  ,  3,    0,  true ,  false,MB2_LODOperation.none  ,  3,    3),
		new Test(4,    true,  true,  true, MB2_LODOperation.delete,  4,    3,   false,  false,MB2_LODOperation.none  ,  4,    4),	
		//Test 13..15 add with bake then delete without bake then update/add
		new Test(2,    true,  false, true, MB2_LODOperation.toAdd ,  2,    4,   true ,  false,MB2_LODOperation.none ,  2,    2),	
		new Test(4,    false,  true, true, MB2_LODOperation.delete  ,  4,    2,   true ,  true,MB2_LODOperation.delete  ,  4,    2),
		new Test(3,    true,  true,  true, MB2_LODOperation.update,  3,    2,   true,  false,MB2_LODOperation.none  ,  3,    3),	
		//Test 16..17 already in mesh update/add without bake then delete
		new Test(0,    false,  true, true, MB2_LODOperation.delete  ,  0,    3,  true ,  true,MB2_LODOperation.delete  ,  0,    3),
		new Test(4,    true,  true,  true, MB2_LODOperation.delete,  4,    3,   false,  false,MB2_LODOperation.none  ,  4,    4),			
		
		//Test 18..20 disable in fixed update then move then enable
		new Test(4,    true,  false,  false, MB2_LODOperation.none,  4,    4,   false,  false,MB2_LODOperation.none  ,  4,    4, Test.Action.disable, Test.When.lateUpdate),			
		new Test(1,    true,  false,  false, MB2_LODOperation.none,  4,    4,   false,  false,MB2_LODOperation.none  ,  4,    4, Test.Action.enable, Test.When.lateUpdate),			
		new Test(1,    true,  false,  true, MB2_LODOperation.toAdd,  1,    4,   true,  false,MB2_LODOperation.none  ,  1,    1),			

		//       lev, dobake, inComb, inQ,         action         , nx, curr  inComb , inQ   ,         action        , nx, curr		
		//Test 21..23 delete without bake and disable then move close and enable
		new Test(4,    false,  true,  true, MB2_LODOperation.delete,  4,    1,   true,  true,MB2_LODOperation.delete  ,  4,    1, Test.Action.disable, Test.When.lateUpdate),			
		new Test(1,    true,  true,  true, MB2_LODOperation.delete,  4,    1,   false,  false,MB2_LODOperation.none  ,  4,    4),			
		new Test(2,    true,  false,  true, MB2_LODOperation.toAdd,  2,    4,   true, false,MB2_LODOperation.none  ,  2,    2, Test.Action.enable, Test.When.fixedUpdate),	
		
		//Test 24 ..25 force level to 1 and move to 3, then move to 4 
		new Test(3,    true,  true,  true, MB2_LODOperation.update,  1,    2,   true, false,MB2_LODOperation.none  ,  1,    1, Test.Action.custom, Test.When.fixedUpdate, new ActionForceToLevel(1)),	
		new Test(4,    true,  true,  false, MB2_LODOperation.none,  1,    1,   true,  false,MB2_LODOperation.none  ,  1,    1, Test.Action.custom, Test.When.preRender, new ActionForceToLevel(-1)),			
		
		//26 .. 27 Try to destroy in late upadate
		new Test(3,    false,  true,  true, MB2_LODOperation.update,  3,    1,   true, true,MB2_LODOperation.delete  ,  4,    1, Test.Action.destroy, Test.When.preRender),
		new Test(3,    true,  true,  true, MB2_LODOperation.delete,  4,    1,   false, false,MB2_LODOperation.none  ,  4,    4),
	};
	Test currentTest = null;
	int testNum = 0;
	
	void Start () {
		MB2_MeshBakerLODTestHarness.harness = this;
		manager = MB2_LODManager.Manager();
		manager.checkScheduler.FORCE_CHECK_EVERY_FRAME = true;
		cam = GetComponent<MB2_LODCamera>();
		manager.LOG_LEVEL = DigitalOpus.MB.Core.MB2_LogLevel.trace;
		lod.LOG_LEVEL = DigitalOpus.MB.Core.MB2_LogLevel.trace;

	}
	
	void FixedUpdate(){
		if (currentTest != null && currentTest.whenToAct == Test.When.fixedUpdate) currentTest.DoActions();	
	}
	
	void Update () {
		if (currentTest != null && currentTest.whenToAct == Test.When.update) currentTest.DoActions();
		for (int i = 0; i < manager.bakers.Length; i++){
			manager.bakers[i].baker.LOG_LEVEL = MB2_LogLevel.trace;	
		}		
	}
	
	void LateUpdate(){
		if (currentTest != null){
			currentTest.CheckStateBetweenUpdateAndBake();
		}		
		if (currentTest != null && currentTest.whenToAct == Test.When.lateUpdate) currentTest.DoActions();		
	}
	
	void OnPreRender(){
		if (currentTest != null && currentTest.whenToAct == Test.When.preRender) currentTest.DoActions();		
	}
	
	void OnPostRender(){
		if (currentTest != null){
			currentTest.CheckStateAfterBake();	
		}		
		if (currentTest != null && currentTest.whenToAct == Test.When.postRender) currentTest.DoActions();
		currentTest = null;
		if (testNum >= tests.Length) {
			Debug.Log("Done testing");
			return;
		}		
		Debug.Log("fr=" + Time.frameCount + " ======= starting test " + testNum);
		currentTest = tests[testNum++];
		currentTest.SetupTest(lod,cam,manager);		
	}
	
	class Test{
		public enum Action{
			none,
			disable,
			enable,
			destroy,
			activate,
			deactivate,
			custom,
		}
		
		public enum When{
			fixedUpdate,
			update,
			lateUpdate,
			preRender,
			postRender
		}
		
		public MB2_LOD target;
		public MB2_LODCamera camera;
		public MB2_LODManager manager;
		public int level;
		public bool doBake;
		public Action act = Action.none;
		public When whenToAct = When.update;
		
		//these distances are for the tudor house model
		public float[] distances = new float[] {10f,30f,70f,150f,250f};
		
		public bool int_inCombined;
		public bool int_inQueue;
		public MB2_LODOperation int_action;
		public int int_currentIdx;
		public int int_nextIdx;
		
		public bool fin_inCombined;
		public bool fin_inQueue;
		public MB2_LODOperation fin_action;
		public int fin_currentIdx;
		public int fin_nextIdx;	
		
		public CustomAction customAction;
		
		public Test(){}
		public Test( int level,
					 bool doBake,
					 bool int_inCombined,
					 bool int_inQueue,
					 MB2_LODOperation int_action,
					 int int_nextIdx,
					 int int_currentIdx,
					 bool fin_inCombined,
					 bool fin_inQueue,
					 MB2_LODOperation fin_action,
					 int fin_nextIdx,
					 int fin_currentIdx,
					 Action act = Action.none,
					 When when = When.update,
					 CustomAction a = null
					 ){
			this.level = level;
			this.doBake = doBake;
			this.int_inCombined = int_inCombined;
			this.int_inQueue = int_inQueue;
			this.int_action = int_action;
			this.int_currentIdx = int_currentIdx;
			this.int_nextIdx = int_nextIdx;
			this.fin_inCombined = fin_inCombined;
			this.fin_inQueue = fin_inQueue;
			this.fin_action = fin_action;
			this.fin_currentIdx = fin_currentIdx;
			this.fin_nextIdx = fin_nextIdx;
			this.act = act;
			this.whenToAct = when;
			this.customAction = a;
		}
		
		public void SetupTest(MB2_LOD targ, MB2_LODCamera cam, MB2_LODManager m){
			target = targ;
			camera = cam;
			float dist = distances[level];
			Debug.Log("fr=" + Time.frameCount + " PreRender SetupTest moving to dist " + dist);
			Vector3 p = cam.transform.position;
			p.z = dist;
			cam.transform.position = p;
			manager = m;
			manager.baking_enabled = doBake;
		}
		
		public void CheckStateBetweenUpdateAndBake(){
			Debug.Log("fr=" + Time.frameCount + " CheckStateBetweenUpdateAndBake");
			target.CheckState(int_inCombined, int_inQueue, int_action,int_nextIdx, int_currentIdx);
			//todo check if object is in combined mesh
		}
		
		public void CheckStateAfterBake(){
			Debug.Log("fr=" + Time.frameCount + " CheckStateAfterBake");
			target.CheckState(fin_inCombined, fin_inQueue, fin_action,fin_nextIdx, fin_currentIdx);
			//todo check if object is in combined
			//todo check correct LODs are disabled/enabled
		}
		
		public void DoActions(){
			Debug.Log("fr=" + Time.frameCount + " DoActions " + act + " " + whenToAct);
			if (act == Action.activate) MB2_Version.SetActive(target.gameObject,true);
			if (act == Action.deactivate) MB2_Version.SetActive(target.gameObject,false);
			if (act == Action.enable) target.enabled = true;
			if (act == Action.disable) target.enabled = false;
			if (act == Action.destroy) MB2_LODManager.Manager().LODDestroy(target);
			if (act == Action.custom) customAction.DoAction();
		}
	}
	
	interface CustomAction{
		void DoAction();
	}
	
	class ActionForceToLevel:CustomAction{
		int level;
		
		public ActionForceToLevel(int l){
			level = l;
		}
		
		public void DoAction(){
			MB2_MeshBakerLODTestHarness.harness.lod.forceToLevel = level;
			Debug.Log("ActionForceToLevel forcing to " + level);
		}
	}
}
