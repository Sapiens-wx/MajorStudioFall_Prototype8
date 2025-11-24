using UnityEngine;

public class ImpactReceiver : MonoBehaviour
{
    [Header("Debug / Identification")]
    [Tooltip("Optional name to show in logs. If empty, GameObject.name is used.")]
    public string receiverNameOverride;

    [Header("Visual Target")]
    [Tooltip("Renderer whose material will react to impacts.")]
    public Renderer targetRenderer;
    [Tooltip("Which material index on this renderer to drive (0 = first).")]
    public int materialIndex = 0;

    [Header("Impact Visual Settings")]
    [Tooltip("Radius of the impact effect, in object-space units.")]
    public float impactRadius = 0.5f;
    [Tooltip("How long the effect should take to fully fade, in seconds.")]
    public float decayTime = 1.5f;

    // Last impact data (just for debugging / inspection in the Inspector)
    [Header("Last Impact (Read-Only)")]
    [SerializeField] private Vector3 lastHitPointWS;
    [SerializeField] private float lastHitPower;
    [SerializeField] private Vector3 lastRelativeVelocity;
    [SerializeField] private string lastSourceObjectName;

    // Shader property IDs (match the shader we’ll write)
    private static readonly int HitPowerID     = Shader.PropertyToID("_HitPower");
    private static readonly int HitStartTimeID = Shader.PropertyToID("_HitStartTime");
    private static readonly int HitPointOS_ID  = Shader.PropertyToID("_HitPointOS");
    private static readonly int RadiusID       = Shader.PropertyToID("_ImpactRadius");
    private static readonly int DecayTimeID    = Shader.PropertyToID("_DecayTime");

    private void Awake()
    {
        // If user forgets to assign, try to grab a renderer on this object
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Called by CarImpactReporter when a collision happens.
    /// Also pushes data into the material for the shader to use.
    /// </summary>
    public void ReportImpact(Vector3 hitPointWorld,
                             float hitPower01,
                             GameObject sourceObject,
                             Vector3 relativeVelocity)
    {
        // --- Store for debug / inspector ---
        lastHitPointWS       = hitPointWorld;
        lastHitPower         = hitPower01;
        lastRelativeVelocity = relativeVelocity;
        lastSourceObjectName = sourceObject != null ? sourceObject.name : "Unknown";

        string receiverName = string.IsNullOrEmpty(receiverNameOverride)
            ? gameObject.name
            : receiverNameOverride;

        Debug.Log(
            $"[ImpactReceiver] {receiverName} was hit by '{lastSourceObjectName}'\n" +
            $"  World Hit Point : {lastHitPointWS}\n" +
            $"  Normalized Power: {lastHitPower:F3} (0–1)\n" +
            $"  Relative Velocity: {lastRelativeVelocity} (mag {lastRelativeVelocity.magnitude:F2})",
            this
        );

        // --- Push data to material for shader ---

        if (targetRenderer == null)
        {
            Debug.LogWarning($"[ImpactReceiver] No targetRenderer assigned on {receiverName}.", this);
            return;
        }

        // Clamp material index
        int matIndex = Mathf.Clamp(materialIndex, 0, targetRenderer.sharedMaterials.Length - 1);

        // This automatically creates a unique material instance for this renderer
        Material mat = targetRenderer.materials[matIndex];

        // Convert world-space hit point into this object’s local space
        Vector3 hitPointOS = transform.InverseTransformPoint(hitPointWorld);

        mat.SetFloat(HitPowerID, hitPower01);
        mat.SetFloat(HitStartTimeID, Time.time);
        mat.SetVector(HitPointOS_ID, hitPointOS);
        mat.SetFloat(RadiusID, impactRadius);
        mat.SetFloat(DecayTimeID, decayTime);
    }
}
