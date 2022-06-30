using UnityEngine;
using System.Collections;

public class MB2_MoveCharacter : MonoBehaviour {

	CharacterController characterController;
	public float speed = 5f;
	public Transform target;
	
	void Start() {
		characterController = GetComponent<CharacterController>();
	}
	
	void Update () {
		if (Time.frameCount % 500 == 0) return;
		Vector3 toTarget = target.position - transform.position;
		toTarget.Normalize();
		characterController.Move(toTarget * speed * Time.deltaTime);
	}
}
