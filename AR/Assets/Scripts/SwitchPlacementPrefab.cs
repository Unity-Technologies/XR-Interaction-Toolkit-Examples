using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.AR;

[RequireComponent(typeof(ARPlacementInteractable))]
public class SwitchPlacementPrefab : MonoBehaviour
{
    [Tooltip("The list of objects that you can place.")]
    [SerializeField]
    List<GameObject> m_ObjectsToPlace;
    public List<GameObject> objectsToPlace
    {
        get => m_ObjectsToPlace;
        set => m_ObjectsToPlace = value;
    }

    [Tooltip("The drop down menu populated by the list of placeable objects.")]
    [SerializeField]
    Dropdown m_Dropdown;
    public Dropdown dropdown
    {
        get => m_Dropdown;
        set => m_Dropdown = value;
    }

    ARPlacementInteractable m_PlacementInteractable;

    protected void Awake()
    {
        m_PlacementInteractable = GetComponent<ARPlacementInteractable>();
        m_Dropdown.ClearOptions();
        foreach (var item in m_ObjectsToPlace)
        {
            var data = new Dropdown.OptionData();
            data.text = item.name;
            m_Dropdown.options.Add(data);
        }

        SwapPlacementObject();
    }

    public void SwapPlacementObject()
    {
        m_PlacementInteractable.placementPrefab = m_ObjectsToPlace[m_Dropdown.value];
    }
}
