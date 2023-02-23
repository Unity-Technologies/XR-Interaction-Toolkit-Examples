namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// This class rotates the flippy door of the ClawMachine when there is any rigidbody inside its trigger.
    /// This class uses the <c>m_Count</c> integer to count the rigidbodies in the trigger and then check
    /// it to update the rotation of the <c>m_Trasform</c>.
    /// </summary>
    public class FlippyDoor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The transform of the FlippyDoor that will be rotated")]
        Transform m_Transform;

        int m_Count;

        void Update()
        {
            var eulerAngles = m_Transform.eulerAngles;
            var desiredAngle = m_Count > 0 ? 90f : 0f;
            eulerAngles.x = Mathf.LerpAngle(eulerAngles.x, desiredAngle, Time.deltaTime * 4f);
            m_Transform.eulerAngles = eulerAngles;
        }

        void OnTriggerEnter(Collider other)
        {
            m_Count++;
        }

        void OnTriggerExit(Collider other)
        {
            m_Count--;
        }
    }
}
