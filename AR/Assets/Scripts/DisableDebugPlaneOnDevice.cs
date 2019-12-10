using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableDebugPlaneOnDevice : MonoBehaviour
{
    public GameObject m_DebugPlane;
    
    // Start is called before the first frame update
    void Start()
    {
        if (!Application.isEditor)
            m_DebugPlane.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
