using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MB_SwitchBakedObjectsTexture : MonoBehaviour {
    // The target renderer where we will switch materials.
    public MeshRenderer targetRenderer;

    // The list of materials to cycle through.
    public Material[] materials;

    // The Mesh Baker that will do the baking
    public MB3_MeshBaker meshBaker;

    public void OnGUI()
    {
        GUILayout.Label("Press space to switch the material on one of the cubes. " +
                "This scene reuses the Texture Bake Result from the SceneBasic example.");
    }

    public void Start()
    {
        // Bake the mesh.
        meshBaker.AddDeleteGameObjects(meshBaker.GetObjectsToCombine().ToArray(),null,true);
        meshBaker.Apply();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Cycle the material on targetRenderer to the next material in materials.

            Material mat = targetRenderer.sharedMaterial;
            //Find the index of the current material on the Renderer
            int materialIdx = -1;
            for (int i = 0; i < materials.Length; i++){
                if (materials[i] == mat){
                    materialIdx = i;
                }
            }

            // Get the next material in the cycle.
            materialIdx++;
            if (materialIdx >= materials.Length) materialIdx = 0;

            if (materialIdx != -1)
            {
                // Assign the material to the disabled renderer
                targetRenderer.sharedMaterial = materials[materialIdx];
                Debug.Log("Updating Material to: " + targetRenderer.sharedMaterial);

                // Update the Mesh Baker combined mesh
                GameObject[] gameObjects = new GameObject[] { targetRenderer.gameObject };
                meshBaker.UpdateGameObjects(gameObjects, false, false, false, false, true, false, false, false, false);
                
                // We could have used AddDelteGameObjects instead of UpdateGameObjects.
                // UpdateGameObjects is faster, but does not work if the material change causes
                // the object to switch submeshes in the combined mesh.
                // meshBaker.AddDeleteGameObjects(gameObjects, gameObjects,false);
                // Apply the changes.
                meshBaker.Apply();
            }
        }
    }
}
