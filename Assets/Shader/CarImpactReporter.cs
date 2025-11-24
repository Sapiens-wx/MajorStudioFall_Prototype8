using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarImpactReporter : MonoBehaviour
{
    [Header("Impact Settings")]
    [Tooltip("Impulse magnitude that maps to HitPower = 1.0.")]
    public float maxExpectedImpulse = 5000f;

    [Tooltip("Log details of every contact in OnCollisionEnter.")]
    public bool verboseLogging = true;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Overall impulse for this collision
        float impulseMagnitude = collision.impulse.magnitude;
        float hitPower01 = Mathf.Clamp01(impulseMagnitude / Mathf.Max(1f, maxExpectedImpulse));

        if (verboseLogging)
        {
            Debug.Log(
                $"[CarImpactReporter] Car '{gameObject.name}' collided with '{collision.gameObject.name}'\n" +
                $"  Total Impulse Magnitude: {impulseMagnitude:F2}\n" +
                $"  Normalized Hit Power   : {hitPower01:F3}",
                this
            );
        }

        // Process each contact point
        foreach (ContactPoint contact in collision.contacts)
        {
            Collider otherCol = contact.otherCollider;
            Vector3 hitPointWS = contact.point;
            Vector3 relativeVel = collision.relativeVelocity;

            // Look for an ImpactReceiver on what we hit (on this collider or its parents)
            ImpactReceiver receiver = otherCol.GetComponentInParent<ImpactReceiver>();

            if (receiver != null)
            {
                receiver.ReportImpact(hitPointWS, hitPower01, this.gameObject, relativeVel);
            }
            else if (verboseLogging)
            {
                Debug.Log(
                    $"[CarImpactReporter] Hit '{otherCol.gameObject.name}' at {hitPointWS}, " +
                    "but it has no ImpactReceiver.",
                    this
                );
            }
        }
    }
}