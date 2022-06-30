using UnityEngine;
using System.Collections;
using DigitalOpus.MB.Core;

/*
 * For building atlases at runtime it is very important that:
 * 		- textures be in trucolor/RBGA32 format
 * 		- textures have read flag set
 * 
 * 
 * It is also Highly recommended to avoid resizing
 *      - build padding into textures in editor
 *      - don't use padding when creating atlases
 *      - don't use tiled materials
 * 
 * If you are having problems look at the Debug Log on the device
 */
public class BakeTexturesAtRuntime : MonoBehaviour {
	public GameObject target;
	float elapsedTime = 0;
	MB3_TextureCombiner.CreateAtlasesCoroutineResult result = new MB3_TextureCombiner.CreateAtlasesCoroutineResult();
	
    public string GetShaderNameForPipeline()
    {
        if (MBVersion.DetectPipeline() == MBVersion.PipelineType.URP)
        {
            return "Universal Render Pipeline/Lit";
        } else if (MBVersion.DetectPipeline() == MBVersion.PipelineType.HDRP)
        {
            return "HDRP/Lit";
        }

        return "Diffuse";
    }

	void OnGUI(){
		GUILayout.Label("Time to bake textures: " + elapsedTime);
		if (GUILayout.Button("Combine textures & build combined mesh all at once")){
			MB3_MeshBaker meshbaker = target.GetComponentInChildren<MB3_MeshBaker>();
			MB3_TextureBaker textureBaker = target.GetComponent<MB3_TextureBaker>();
			
			//These can be assets configured at runtime or you can create them
			// on the fly like this
			textureBaker.textureBakeResults = ScriptableObject.CreateInstance<MB2_TextureBakeResults>();
			textureBaker.resultMaterial = new Material( Shader.Find(GetShaderNameForPipeline()) ); 
			
			float t1 = Time.realtimeSinceStartup;
			textureBaker.CreateAtlases();
			elapsedTime = Time.realtimeSinceStartup - t1;	
			
			meshbaker.ClearMesh(); //only necessary if your not sure whats in the combined mesh
			meshbaker.textureBakeResults = textureBaker.textureBakeResults;

			//Add the objects to the combined mesh
			meshbaker.AddDeleteGameObjects(textureBaker.GetObjectsToCombine().ToArray(), null, true);
			
			meshbaker.Apply();
		}

        if (GUILayout.Button("Combine textures & build combined mesh using coroutine"))
        {
            Debug.Log("Starting to bake textures on frame " + Time.frameCount);
            MB3_TextureBaker textureBaker = target.GetComponent<MB3_TextureBaker>();

            //These can be assets configured at runtime or you can create them
            // on the fly like this
            textureBaker.textureBakeResults = ScriptableObject.CreateInstance<MB2_TextureBakeResults>();
            textureBaker.resultMaterial = new Material(Shader.Find(GetShaderNameForPipeline()));

            //register an OnSuccess function to be called when texture baking is complete
            textureBaker.onBuiltAtlasesSuccess = new MB3_TextureBaker.OnCombinedTexturesCoroutineSuccess(OnBuiltAtlasesSuccess);
			StartCoroutine(textureBaker.CreateAtlasesCoroutine(null,result,false,null,.01f));
        }
    }

    void OnBuiltAtlasesSuccess()
    {
        Debug.Log("Calling success callback. baking meshes");
        MB3_MeshBaker meshbaker = target.GetComponentInChildren<MB3_MeshBaker>();
        MB3_TextureBaker textureBaker = target.GetComponent<MB3_TextureBaker>();
        //elapsedTime = Time.realtimeSinceStartup - t1;

        if (result.isFinished &&
            result.success)
        {
            meshbaker.ClearMesh(); //only necessary if your not sure whats in the combined mesh
            meshbaker.textureBakeResults = textureBaker.textureBakeResults;
            //Add the objects to the combined mesh
            meshbaker.AddDeleteGameObjects(textureBaker.GetObjectsToCombine().ToArray(), null, true);
            meshbaker.Apply();
        }
        Debug.Log("Completed baking textures on frame " + Time.frameCount);
    }
}
