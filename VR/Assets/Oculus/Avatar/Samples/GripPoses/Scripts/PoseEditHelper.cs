using UnityEngine;
using System.Collections;
using System;

public class PoseEditHelper : MonoBehaviour {

    public Transform poseRoot;

    void OnDrawGizmos()
    {
        if (poseRoot != null)
        {
            DrawJoints(poseRoot);
        }
    }

    private void DrawJoints(Transform joint)
    {
        Gizmos.DrawWireSphere(joint.position, 0.005f);
        for (int i = 0; i < joint.childCount; ++i)
        {
            Transform child = joint.GetChild(i);
            if (child.name.EndsWith("_grip") || child.name.EndsWith("hand_ignore"))
            {
                continue;
            }
            Gizmos.DrawLine(joint.position, child.position);
            DrawJoints(child);
        }
    }
}
