using UnityEngine;

public class DirectionRotator : MonoBehaviour
{
    [Header("Local direction positions (relative to this object)")]
    public Vector3 front = Vector3.forward;
    public Vector3 back = Vector3.back;
    public Vector3 left = Vector3.left;
    public Vector3 right = Vector3.right;
    public Vector3 extra = Vector3.up;

    [Header("Keys")]
    public KeyCode frontKey = KeyCode.W;
    public KeyCode backKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Rotation")]
    public float rotateSpeed = 5f;

    private Quaternion baseLocalRotation;    // localRotation at start
    private Quaternion targetLocalRotation;  // slerp target

    void Start()
    {
        baseLocalRotation = transform.localRotation;
        targetLocalRotation = baseLocalRotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(frontKey))
        {
            HandleFrontKey();
        }
        else if (Input.GetKeyDown(backKey))
        {
            SetTarget(back);
        }
        else if (Input.GetKeyDown(leftKey))
        {
            SetTarget(left);
        }
        else if (Input.GetKeyDown(rightKey))
        {
            SetTarget(right);
        }

        // Smooth rotation using localRotation
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetLocalRotation,
            Time.deltaTime * rotateSpeed
        );
    }

    // ---------- FRONT KEY SPECIAL LOGIC ----------
    private void HandleFrontKey()
    {
        Vector3 curForward = transform.localRotation * front.normalized;
        Vector3 frontDir = baseLocalRotation * front.normalized;
        Vector3 extraDir = baseLocalRotation * extra.normalized;

        bool facingFront = Vector3.Angle(curForward, frontDir) < 5f;
        bool facingExtra = Vector3.Angle(curForward, extraDir) < 5f;

        if (facingFront)
        {
            SetTarget(extra);
        }
        else if (facingExtra)
        {
            SetTarget(front);
        }
        else
        {
            SetTarget(front);
        }
    }

    // ---------- GENERIC TARGET ROTATION ----------
    private void SetTarget(Vector3 localDir)
    {
        localDir.Normalize();

        // direction under baseLocalRotation
        Vector3 targetLocalDir = baseLocalRotation * localDir;

        // compute final rotation (in local space)
        targetLocalRotation = Quaternion.LookRotation(targetLocalDir, Vector3.up);
    }

    // ---------- DRAW GIZMOS ----------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float radius = 0.1f;

        Matrix4x4 m = transform.localToWorldMatrix;

        Gizmos.DrawWireSphere(m.MultiplyPoint(front), radius);
        Gizmos.DrawWireSphere(m.MultiplyPoint(back), radius);
        Gizmos.DrawWireSphere(m.MultiplyPoint(left), radius);
        Gizmos.DrawWireSphere(m.MultiplyPoint(right), radius);
        Gizmos.DrawWireSphere(m.MultiplyPoint(extra), radius);
    }
}