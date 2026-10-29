using UnityEngine;

[DisallowMultipleComponent]
public class StayInRange : MonoBehaviour
{
    [Tooltip("Compute the range from the scene geometry at start")]
    [SerializeField] bool autoFromScene = true;
    [SerializeField] Vector3 center = Vector3.zero;
    [SerializeField] Vector3 size = new Vector3(20f, 10f, 20f);

    Vector3 lastValid;
    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (autoFromScene) ComputeFromScene();
        lastValid = IsInside(transform.position) ? transform.position : center;
    }

    void LateUpdate()
    {
        Vector3 p = transform.position;
        if (IsInside(p))
        {
            lastValid = p;
            return;
        }

        // Outside (or NaN/Infinity): put it back
        bool ccWasEnabled = cc != null && cc.enabled;
        if (ccWasEnabled) cc.enabled = false;
        transform.position = lastValid;
        if (ccWasEnabled) cc.enabled = true;
    }

    public bool IsInside(Vector3 p) =>
        !float.IsNaN(p.x) && !float.IsNaN(p.z) &&
        new Bounds(center, size).Contains(new Vector3(p.x, center.y, p.z)); // horizontal check only

    void ComputeFromScene()
    {
        bool init = false;
        Bounds b = default;
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || r.transform.IsChildOf(transform)) continue; // ignore the player itself
            if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds);
        }
        if (init) { center = b.center; size = b.size; }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}