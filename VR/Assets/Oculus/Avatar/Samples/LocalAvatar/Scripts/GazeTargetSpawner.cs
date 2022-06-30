using UnityEngine;

public class GazeTargetSpawner : MonoBehaviour
{
    public GameObject GazeTargetPrefab;
    public int NumberOfDummyTargets = 100;
    public int RadiusMultiplier = 3;
    [SerializeField]
    private bool isVisible;
    public bool IsVisible
    {
        get
        {
            return isVisible;
        }
        set
        {
            isVisible = value;
            GazeTarget[] dummyGazeTargets = gameObject.GetComponentsInChildren<GazeTarget>();
            for (int i = 0; i < dummyGazeTargets.Length; ++i)
            {
                MeshRenderer dummyMesh = dummyGazeTargets[i].GetComponent<MeshRenderer>();
                if (dummyMesh != null)
                {
                    dummyMesh.enabled = isVisible;
                }
            }
        }
    }

    void Start ()
    {
        for (int i = 0; i < NumberOfDummyTargets; ++i)
        {
            GameObject target = Instantiate(GazeTargetPrefab, transform);
            target.name += "_" + i;
            target.transform.localPosition = Random.insideUnitSphere * RadiusMultiplier;
            target.transform.rotation = Quaternion.identity;
            target.GetComponent<MeshRenderer>().enabled = IsVisible;
        }
    }

    void OnValidate()
    {
        // Run through OnValidate to pick up changes from inspector
        IsVisible = isVisible;
    }
}
