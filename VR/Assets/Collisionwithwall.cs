using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collisionwithwall : MonoBehaviour
{
    public GameObject Poster1;
    public Animator PosterChange;

    // Start is called before the first frame update
    void Start()
    {

    }

    // void OnCollisionEnter()
    //{
    //    PosterChange.Play();
    //}

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PosterWall"))
            {
            PosterChange.SetTrigger("PosterChange");

            Destroy(other.gameObject);
        }
    }
}

