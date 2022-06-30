using UnityEngine;
using UnityEditor;
using System.Collections;
using DigitalOpus.MB.Lod;
using DigitalOpus.MB.Core;
using System.Reflection;

[CustomEditor(typeof(MB2_LOD)),CanEditMultipleObjects]
public class MB2_LODEditor : Editor {
	
	private static GUIContent
		insertLODContent = new GUIContent("+", "add baker"),
		deleteLODContent = new GUIContent("-", "delete this baker"),
		screenPercentageContent = new GUIContent("Screen Percentage", "Threshold where the LOD should switch to the next LOD level. .5 means switch when the object fills half the screen with its largest dimension."),
		bakerLabelContent = new GUIContent("Baker Label", "Optional. Specify a label here to force this LOD object to be baked by a particular baker. Use this to resolve conflicts where a object could be baked by multiple bakers. The label here must match the label specified for the baker."),
		meshRendererContent = new GUIContent("Baker Render Type", "Specifies whether this LOD object should be baked into a MeshRenderer combined mesh or SkinnedMeshRenderer combined mesh. This setting must match the 'Render Type' of the baker that will bake this LOD."),
		forceToLevelContent = new GUIContent("Force To Level", "Forces a LOD to a particular level and keeps it there."),
		swapMeshWithLOD0Content = new GUIContent("Swap Mesh With LOD 0", "Instead of activating this level. This mesh will be assigned to level 0 which will remain active.\n\nThis is useful for preserving animations during transitions. Animations can be removed from this level."),		
		bakeIntoCombinedContent = new GUIContent("Bake Into Combined","If false then LOD will be applied to this object but the mesh will NOT be baked into a combined mesh. This LOD will also not participate in limits on the maximum number of meshes allowed per level. The LODManager does not need a baker or combined material for this object."),
		bakeIntoCombinedLevelContent = new GUIContent("Bake Into Combined","Should this level be baked into a combined mesh when it becomes active. Often it is most efficient not to bake level 0 meshes because they have a lot of vertices and only a few are visible at a time so there is little gain to baking them.");
	
	private static GUILayoutOption
		buttonWidth = GUILayout.MaxWidth(20f);	

	private SerializedProperty lods, logLevel, bakerLabel, renderType, forceToLevel, bakeIntoCombined;	
	
//    float CamFOV = -0.01f;
//    float PrevCamFOV = 1f;
	
	//=================================
    // The following methods are from:

    // http://www.olivereberlei.com/517/wrestling-with-the-editor-camera-in-unity/
	
    static void SetEditorCameraValue<T>( string fieldName, T newValue, SceneView sceneView = null )    {
        FieldInfo field = typeof( SceneView ).GetField( fieldName, BindingFlags.Instance | BindingFlags.NonPublic );
        object animBool = field.GetValue(( sceneView != null ) ? sceneView : SceneView.lastActiveSceneView);
        FieldInfo field2 = animBool.GetType().GetField( "m_Value", BindingFlags.Instance | BindingFlags.NonPublic );
        T currentValue = (T)field2.GetValue( animBool );
        object[] param = new object[2];
        param[0] = newValue;
        param[1] = currentValue;
        animBool.GetType().GetMethod( "BeginAnimating", BindingFlags.Instance | BindingFlags.NonPublic ).Invoke( animBool, param );
    }

    static T GetEditorCameraValue<T>( string fieldName, SceneView sceneView = null ) {
        FieldInfo field = typeof( SceneView ).GetField( fieldName, BindingFlags.Instance | BindingFlags.NonPublic );
        object animBool = field.GetValue(( sceneView != null ) ? sceneView : SceneView.lastActiveSceneView);
        FieldInfo field2 = animBool.GetType().GetField( "m_Value", BindingFlags.Instance | BindingFlags.NonPublic );
        return (T)field2.GetValue( animBool );
    }

    public static void SetFOV(float newSize, SceneView sceneView = null) { SetEditorCameraValue<float>( "m_Size", newSize, sceneView ); }
    public static float GetFOV(SceneView sceneView = null) { return GetEditorCameraValue<float>("m_Size"); }	
	
	//========================
	
	public void OnEnable (){
		lods = serializedObject.FindProperty("levels");
		logLevel = serializedObject.FindProperty("LOG_LEVEL");
		bakerLabel = serializedObject.FindProperty("bakerLabel");
		renderType = serializedObject.FindProperty("renderType");
		//framesBetweenChecks = lod.FindProperty("framesBetweenLODChangedChecks");
		forceToLevel = serializedObject.FindProperty("forceToLevel");
		bakeIntoCombined = serializedObject.FindProperty("bakeIntoCombined");
	}	
	
	public override void OnInspectorGUI(){
		serializedObject.Update();
		MB2_LOD lodObj = (MB2_LOD) target;
		MBVersionEditor.SetInspectorLabelWidth(200f);
		EditorGUILayout.HelpBox(lodObj.GetStatusMessage() ,MessageType.Info);
		EditorGUILayout.PropertyField(logLevel);
		EditorGUILayout.PropertyField(bakerLabel, bakerLabelContent);
		//EditorGUILayout.PropertyField(framesBetweenChecks, framesBetweenChecksContent);
		EditorGUILayout.PropertyField(renderType, meshRendererContent);
		EditorGUILayout.PropertyField(forceToLevel, forceToLevelContent);
		EditorGUILayout.PropertyField(bakeIntoCombined, bakeIntoCombinedContent);
		
		//==========================
		/*
        EditorGUILayout.Space();
        if(CamFOV==-0.01f) {
            CamFOV = Mathf.Pow(GetFOV(), 1f / 3f) / 7.937f;
            PrevCamFOV = CamFOV;
        }

        CamFOV = EditorGUILayout.Slider("Camera Distance Test", CamFOV,0.01f,1.0f);
        if(PrevCamFOV != CamFOV) {
            SceneView.lastActiveSceneView.FrameSelected();
            SetFOV(Mathf.Pow(CamFOV*7.937f,3f));
            SceneView.lastActiveSceneView.Repaint();
            PrevCamFOV = CamFOV;
        }

        GUILayout.Label("Distance from camera to switch: " + Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, lodObj.transform.position).ToString ());
        GUILayout.BeginHorizontal();
        GUILayout.Label("Apply to LOD:");
        for(int i = 0; i < lods.arraySize; i++){
	        if(GUILayout.Button(i.ToString())) {
	            Renderer mr = lodObj.levels[i].lodObject;
	            if (mr != null){
	                Bounds b = mr.bounds;
	                if (mr is SkinnedMeshRenderer){
	                    bool a = MB2_Version.GetActive(mr.gameObject);
	                    bool e = mr.enabled;
	                    MB2_Version.SetActive(mr.gameObject,true);
	                    mr.enabled = true;
	                    b = mr.bounds;
	                    MB2_Version.SetActive(mr.gameObject, a);
	                    mr.enabled = e;
	                }
	                float h = (b.size.x + b.size.y + b.size.z) / 3f;
	                float d = 50 / Mathf.Tan( Mathf.Deg2Rad * SceneView.lastActiveSceneView.camera.fieldOfView / 2f);
	                lodObj.levels[i].screenPercentage = ((h*d)/Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, lodObj.transform.position))/50f;
	            }
	            lodObj.CalculateSwitchDistances(SceneView.lastActiveSceneView.camera.fieldOfView, false);
        	}
		}
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        */		
		//==========================
		
		GUILayout.Label("LOD Levels ",EditorStyles.boldLabel);
		if (lods.arraySize == 0){
			if (GUILayout.Button("Add LOD Level")){
				lodObj.levels = new MB2_LOD.LOD[1];
				lodObj.levels[0] = new MB2_LOD.LOD();
			}
		}
		for(int i = 0; i < lods.arraySize; i++){
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(" --LOD " + i,EditorStyles.boldLabel);
			if(GUILayout.Button(insertLODContent, EditorStyles.miniButtonMid, buttonWidth)){
				lods.InsertArrayElementAtIndex(i);
			}
			if(GUILayout.Button(deleteLODContent, EditorStyles.miniButtonRight, buttonWidth)){
				lods.DeleteArrayElementAtIndex(i);
				continue;
			}
			EditorGUILayout.EndHorizontal();
			SerializedProperty level = lods.GetArrayElementAtIndex(i);
			EditorGUILayout.PropertyField(level.FindPropertyRelative("lodObject"));
			if (i > 0) EditorGUILayout.PropertyField(level.FindPropertyRelative("swapMeshWithLOD0"), swapMeshWithLOD0Content);			
			EditorGUILayout.PropertyField(level.FindPropertyRelative("screenPercentage"), screenPercentageContent);
			EditorGUILayout.PropertyField(level.FindPropertyRelative("bakeIntoCombined"), bakeIntoCombinedLevelContent);
			SerializedProperty dim = level.FindPropertyRelative("dimension");
			SerializedProperty sqrtDist = level.FindPropertyRelative("sqrtDist");
			EditorGUILayout.LabelField("Largest Dimension =" + dim.floatValue);
			EditorGUILayout.LabelField("Distance from camera to switch=" + sqrtDist.floatValue);
		}
		if (MB2_LODManager.CHECK_INTEGRITY){
			if (GUILayout.Button("Dump Log")){
				Debug.Log ("log dump " + lodObj.myLog.Dump()); 
					
			}
		}		
		
		if (serializedObject.ApplyModifiedProperties()){
			//todo try to get fov from camera			
			lodObj.CalculateSwitchDistances(60, false);
		}
	}
}
