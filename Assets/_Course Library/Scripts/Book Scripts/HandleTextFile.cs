using UnityEngine;
using System;
using TMPro;

public class HandleTextFiles : MonoBehaviour
{
    public TextAsset textAsset;
    // Start is called before the first frame update
    void Start()
    {
        if (textAsset == null)
        {
            Debug.LogWarning(String.Format("Missing TextAsset for {0}", this.name));
        }
        else
        {
            this.GetComponent<TextMeshPro>().text = textAsset.text;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
