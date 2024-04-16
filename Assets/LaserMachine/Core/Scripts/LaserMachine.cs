using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Lightbug.LaserMachine
{



public class LaserMachine : MonoBehaviour {

    struct LaserElement 
    {
        public Transform transform;        
        public LineRenderer lineRenderer;
        public GameObject sparks;
        public bool impact;
    };

    List<LaserElement> elementsList = new List<LaserElement>();
    

    [Header("External Data")]
    
    [SerializeField] LaserData m_data;

    [Tooltip("This variable is true by default, all the inspector properties will be overridden.")]
    [SerializeField] bool m_overrideExternalProperties = true;

    [SerializeField] LaserProperties m_inspectorProperties = new LaserProperties();
    

    LaserProperties m_currentProperties;// = new LaserProperties();
        
    float m_time = 0;
    bool m_active = true;
    bool m_assignLaserMaterial;
    bool m_assignSparks;
  		
    

    void OnEnable()
    {
        m_currentProperties = m_overrideExternalProperties ? m_inspectorProperties : m_data.m_properties;
        

        m_currentProperties.m_initialTimingPhase = Mathf.Clamp01(m_currentProperties.m_initialTimingPhase);
        m_time = m_currentProperties.m_initialTimingPhase * m_currentProperties.m_intervalTime;
        

        float angleStep = m_currentProperties.m_angularRange / m_currentProperties.m_raysNumber;        

        m_assignSparks = m_data.m_laserSparks != null;
        m_assignLaserMaterial = m_data.m_laserMaterial != null;

        for (int i = 0; i < m_currentProperties.m_raysNumber ; i++)
        {
            LaserElement element = new LaserElement();

            GameObject newObj = new GameObject("lineRenderer_" + i.ToString());

            if( m_currentProperties.m_physicsType == LaserProperties.PhysicsType.Physics2D )
                newObj.transform.position = (Vector2)transform.position;
            else
                newObj.transform.position = transform.position;

            newObj.transform.rotation = transform.rotation;
            newObj.transform.Rotate( Vector3.up , i * angleStep );
            newObj.transform.position += newObj.transform.forward * m_currentProperties.m_minRadialDistance;

            newObj.AddComponent<LineRenderer>();

            if( m_assignLaserMaterial )
                newObj.GetComponent<LineRenderer>().material = m_data.m_laserMaterial;

            newObj.GetComponent<LineRenderer>().receiveShadows = false;
            newObj.GetComponent<LineRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            newObj.GetComponent<LineRenderer>().startWidth = m_currentProperties.m_rayWidth;
            newObj.GetComponent<LineRenderer>().useWorldSpace = true;
            newObj.GetComponent<LineRenderer>().SetPosition(0, newObj.transform.position);
            newObj.GetComponent<LineRenderer>().SetPosition(1, newObj.transform.position + transform.forward * m_currentProperties.m_maxRadialDistance);
            newObj.transform.SetParent(transform);
            
            if( m_assignSparks )
            {
                GameObject sparks = Instantiate(m_data.m_laserSparks);
                sparks.transform.SetParent(newObj.transform);
                sparks.SetActive(false);
                element.sparks = sparks;
            }

            element.transform = newObj.transform;
            element.lineRenderer = newObj.GetComponent<LineRenderer>();
            element.impact = false;

            elementsList.Add(element);
        }
        
	}
        
       
	void Update () {

        if (m_currentProperties.m_intermittent)
        {
            m_time += Time.deltaTime;

            if (m_time >= m_currentProperties.m_intervalTime)
            {
                m_active = !m_active;
                m_time = 0;
                return;
            }
        }

        RaycastHit2D hitInfo2D;
        RaycastHit hitInfo3D;

        foreach (LaserElement element in elementsList)
        {
            if ( m_currentProperties.m_rotate )
            {
                if ( m_currentProperties.m_rotateClockwise )
                    element.transform.RotateAround(transform.position, transform.up, Time.deltaTime * m_currentProperties.m_rotationSpeed);    //rotate around Global!!
                else
                    element.transform.RotateAround(transform.position, transform.up, -Time.deltaTime * m_currentProperties.m_rotationSpeed);
            }


            if (m_active)
            {
                element.lineRenderer.enabled = true;
                element.lineRenderer.SetPosition(0, element.transform.position);

                if(m_currentProperties.m_physicsType == LaserProperties.PhysicsType.Physics3D)
                {
                    Physics.Linecast(
                        element.transform.position,
                        element.transform.position + element.transform.forward * m_currentProperties.m_maxRadialDistance,
                        out hitInfo3D ,
                        m_currentProperties.m_layerMask
                    );  


                    if (hitInfo3D.collider)
                    {
                        element.lineRenderer.SetPosition(1, hitInfo3D.point);

                        if( m_assignSparks )
                        {
                            element.sparks.transform.position = hitInfo3D.point; //new Vector3(rhit.point.x, rhit.point.y, transform.position.z);
                            element.sparks.transform.rotation = Quaternion.LookRotation( hitInfo3D.normal ) ;
                        }

                        /*
                        EXAMPLE : In this line you can add whatever functionality you want, 
                        for example, if the hitInfoXD.collider is not null do whatever thing you wanna do to the target object.
                        DoAction();
                        */

                    }
                    else
                    {
                        element.lineRenderer.SetPosition(1, element.transform.position + element.transform.forward * m_currentProperties.m_maxRadialDistance);

                    }

                    if( m_assignSparks )
                        element.sparks.SetActive( hitInfo3D.collider != null );
                }

                else
                {
                    hitInfo2D = Physics2D.Linecast( 
                        element.transform.position,
                        element.transform.position + element.transform.forward * m_currentProperties.m_maxRadialDistance,
                        m_currentProperties.m_layerMask 
                    );


                    if (hitInfo2D.collider)
                    {
                        element.lineRenderer.SetPosition(1, hitInfo2D.point);

                        if( m_assignSparks )
                        {
                            element.sparks.transform.position = hitInfo2D.point; //new Vector3(rhit.point.x, rhit.point.y, transform.position.z);
                            element.sparks.transform.rotation = Quaternion.LookRotation( hitInfo2D.normal ) ;
                        }

                        /*
                        EXAMPLE : In this line you can add whatever functionality you want, 
                        for example, if the hitInfoXD.collider is not null do whatever thing you wanna do to the target object.
                        DoAction();
                        */

                    }
                    else
                    {
                        element.lineRenderer.SetPosition(1, element.transform.position + element.transform.forward * m_currentProperties.m_maxRadialDistance);

                    }

                    if( m_assignSparks )
                        element.sparks.SetActive( hitInfo2D.collider != null );

                }              

                





            }
            else
            {
                element.lineRenderer.enabled = false;

                if( m_assignSparks )
                    element.sparks.SetActive(false);
            }
        }
        
    }

    /*
    EXAMPLE : 
    void DoAction()
    {

    }
    */

	
}


}
