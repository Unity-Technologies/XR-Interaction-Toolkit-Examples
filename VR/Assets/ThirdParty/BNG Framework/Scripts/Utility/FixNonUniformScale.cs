using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this component to an object with a non-uniform scale (not 1,1,1) to make it 1,1,1 and move / resize any box collider / mesh renderers to match
/// </summary>
public class FixNonUniformScale : MonoBehaviour
{

    bool running = false;

    // Only call on selected
    void OnDrawGizmosSelected() {
        if(running) {
            return;
        }

        running = true;

        MakeUniform();

        DestroyImmediate(this);
    }

    public void MakeUniform() {
        Vector3 originalScale = transform.localScale;

        // Nothing to scale here
        if(originalScale == Vector3.one) {
            return;
        }

        // Scale to where we should be
        transform.localScale = Vector3.one;

        MeshRenderer ren = GetComponent<MeshRenderer>();
        MeshFilter filter = GetComponent<MeshFilter>();

        if (ren != null) {
            Transform renObject = new GameObject("Renderer").transform;

            if(filter) {
                MeshFilter mf = renObject.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = filter.sharedMesh;
                DestroyImmediate(filter);
            }

            MeshRenderer newRenderer = renObject.gameObject.AddComponent<MeshRenderer>();
            newRenderer.sharedMaterial = ren.sharedMaterial;

            renObject.parent = transform;
            renObject.localPosition = Vector3.zero;
            renObject.localRotation = Quaternion.identity;
            renObject.localScale = originalScale;

            DestroyImmediate(ren);
        }
        BoxCollider col = GetComponent<BoxCollider>();
        if(col != null) {
            // Need to resize box collider
            col.center = Vector3.zero;
            col.size = originalScale;
        }
    }
}
