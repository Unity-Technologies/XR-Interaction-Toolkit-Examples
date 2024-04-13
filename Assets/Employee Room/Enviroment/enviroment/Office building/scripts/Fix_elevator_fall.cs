using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fix_elevator_fall : MonoBehaviour
{
   
    
    public float PlayerHeightLocal;
    public bool inElevator;
    public GameObject Player;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            inElevator = true;
            Player = other.gameObject;
            Player.transform.parent = transform;
            



        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            inElevator = false;
            Player = null;
            other.transform.parent = null;

        }
    }
    
 


}
