using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lightbug.DragIt
{

[RequireComponent( typeof( Camera ) )]
[RequireComponent( typeof( SphereCollider ) )]
public class FreeCam : MonoBehaviour{

	[Header("Collision Detection")]

	[SerializeField] bool m_collisionDetection = true;
	[SerializeField] LayerMask m_collisionLayerMask;

	[Header("Movement")]
	
	[SerializeField] float speed = 4;
	
	[Header("Rotation")]

	[SerializeField] float mouseLookSensitivity = 2;
	[Range( 45f , 90f )] [SerializeField] float m_pitchMaxAngle = 80f;

	
	

	Vector3 currentVelocity;

	float m_pitch;

	Camera m_camera;
	SphereCollider m_collider;
	Collider[] m_results = new Collider[5];

	void Start () 
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		
		m_camera = GetComponent<Camera>();
		m_collider = GetComponent<SphereCollider>();
	}


	void Update()
	{
		
		float deltaPitch = - Input.GetAxis ("Mouse Y") * mouseLookSensitivity;
		float deltaYaw = Input.GetAxis ("Mouse X") * mouseLookSensitivity;

		if( m_pitch + deltaPitch > m_pitchMaxAngle )
		{
			deltaPitch = m_pitchMaxAngle - m_pitch;
		}
		else if( m_pitch + deltaPitch < - m_pitchMaxAngle )
		{
			deltaPitch = - m_pitchMaxAngle - m_pitch;
		}

		m_pitch += deltaPitch;

		
		transform.Rotate( Vector3.right , deltaPitch , Space.Self );
		transform.Rotate( Vector3.up , deltaYaw , Space.World );

		

		float rightMove = Input.GetAxisRaw("Horizontal");		
		float forwardMove = Input.GetAxisRaw("Vertical");		
		float upMove = 0;

		if (Input.GetKey (KeyCode.E))
			upMove = 1;
		else if(Input.GetKey(KeyCode.Q))
			upMove = -1;
		
		
		Vector3 targetVelocity = ( new Vector3(rightMove , upMove , forwardMove) ).normalized * speed; 
		currentVelocity = Vector3.Lerp( currentVelocity , targetVelocity , Time.deltaTime * 7f );

		Move( currentVelocity * Time.deltaTime );


		

	}

	void Depenetrate()
	{		
		Physics.OverlapSphereNonAlloc( 
			transform.position ,
			m_collider.radius ,
			m_results ,
			m_collisionLayerMask 
		);

		Vector3 direction = Vector3.zero;
		float distance = 0;

		

		for (int i = 0; i < m_results.Length; i++)
		{
			if(m_results[i] == null || m_results[i] == m_collider)
				continue;

			Physics.ComputePenetration( m_collider , transform.position , transform.rotation
			, m_results[i] , m_results[i].transform.position , m_results[i].transform.rotation , 
			out direction , out distance );

			if(distance != 0)
				transform.Translate( direction * distance , Space.World);
		}

		
	}

	void Move(Vector3 deltaPosition)
	{
		transform.Translate( deltaPosition );

		if(m_collisionDetection)
			Depenetrate();
		
	}

}

}
