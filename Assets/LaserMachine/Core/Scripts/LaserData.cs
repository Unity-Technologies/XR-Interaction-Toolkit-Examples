using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lightbug.LaserMachine
{

[CreateAssetMenu(menuName = "Laser Machine/Laser Data")]
public class LaserData : ScriptableObject {
        
    [Header("Asset Resources")]

    public GameObject m_laserSparks;    
    public Material m_laserMaterial;

    [Header("Laser Properties")]

    public LaserProperties m_properties = new LaserProperties();

   
}

}
