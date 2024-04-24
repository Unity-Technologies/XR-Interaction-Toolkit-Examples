using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlarmTrigger : MonoBehaviour
{
    public Audiosource alarm;
    void Start()
    {
        alarm = this.GetComponent<AudioSource>();
    }

void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.name == "Line (1)" || collision.gameObject.name == "Line (2)" || collision.gameObject.name == "Line (3)"
        || collision.gameObject.name == "Line (4)" || collision.gameObject.name == "Line (5)" || collision.gameObject.name == "Line (6)")
        {
            alarm.Play();
        }
        alarm.Play();
    }
}
