using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

public class SwitchPlacementPrefab : MonoBehaviour
{
    public GameObject m_ChairPrefab;
    public GameObject m_TablePrefab;
    public GameObject m_KitchenChairPrefab;
    public GameObject m_KitchenTable1Prefab;
    public GameObject m_KitchenTable2Prefab;
    public GameObject m_TVTablePrefab;
    public ARPlacementInteractable m_PlacementInteractable;

    public void SwapToChair()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_ChairPrefab;
    }

    public void SwapToTable()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_TablePrefab;
    }
    
    public void SwapToKitchenChair()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_KitchenChairPrefab;
    }
    
    public void SwapToKitchenTable1()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_KitchenTable1Prefab;
    }
    
    public void SwapToKitchenTable2()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_KitchenTable2Prefab;
    }
    
    public void SwapToTVTable()
    {
        if (m_PlacementInteractable == null)
            return;

        m_PlacementInteractable.placementPrefab = m_TVTablePrefab;
    }
}
