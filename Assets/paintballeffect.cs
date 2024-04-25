using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paintballeffect : MonoBehaviour
{

    AudioSource audioSource;

    void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
        
    }

     void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
            
        }
       // if (collision.relativeVelocity.magnitude > 2)
            audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
