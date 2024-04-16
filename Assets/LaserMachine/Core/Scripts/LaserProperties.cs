using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lightbug.LaserMachine
{

[System.Serializable]
public class LaserProperties
{
    [Header("Collision Detection")]

    public LayerMask m_layerMask;
    
    public enum PhysicsType { Physics2D , Physics3D };
    public PhysicsType m_physicsType = PhysicsType.Physics3D;
    

    [Header("Shape")]

    public float m_rayWidth = 0.01f;
    [Range( 1f , 360f)] public float m_angularRange = 360f;
    [Range( 1 , 50 )] public int m_raysNumber = 8; 
    public float m_minRadialDistance = 1;	
	public float m_maxRadialDistance = 25;
    

    [Header("Rotation")]
    
    public bool m_rotate = true;
    public bool m_rotateClockwise = true;
    public float m_rotationSpeed = 20;

    [Header("Intermittentency")]

    public bool m_intermittent = false;
    public float m_intervalTime = 2;

    [Range( 0f , 1f )]
    public float m_initialTimingPhase = 0;

    
}

}
