using UnityEngine;
using System.Collections;

public class SimpleCharacterController2 : MonoBehaviour {

    public float speed = 20.0f;
    public float rotateSpeed = 3.0f;

    public void  Update()
    {
        CharacterController controller = GetComponent<CharacterController>();
        transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float curSpeed = speed * Input.GetAxis("Vertical");
        //if (Time.frameCount % 10 == 0) Debug.Log(forward + " " + curSpeed);
        controller.SimpleMove(forward * curSpeed);
    }
}
