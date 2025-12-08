using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PostCenterFromObject_SimpleMasterToggle : MonoBehaviour
{
    [Header("MASTER TOGGLE (overrides everything)")]
    public bool masterEnabled = true;

    [Header("Post material")]
    public Material baseMaterial;

    [Header("Center target (required for auto)")]
    public Transform centerTarget;

    [Header("Auto disable when target off-screen")]
    public bool autoDisableOffscreen = true;

    Material _mat;
    Camera _cam;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        if (baseMaterial != null) _mat = Instantiate(baseMaterial);
    }

    void OnDisable()
    {
        if (_mat != null) DestroyImmediate(_mat);
    }

    bool IsOnScreenViewport(Vector3 vp)
    {
        return vp.z > 0.0001f &&
               vp.x >= 0f && vp.x <= 1f &&
               vp.y >= 0f && vp.y <= 1f;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // MASTER OFF = always pass-through
        if (!masterEnabled || _mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        // If no target, pass-through
        if (centerTarget == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        Vector3 vp = _cam.WorldToViewportPoint(centerTarget.position);

        // Auto disable if offscreen/behind camera
        if (autoDisableOffscreen && !IsOnScreenViewport(vp))
        {
            Graphics.Blit(src, dst);
            return;
        }

        // Apply effect
        _mat.SetVector("_Center", new Vector4(vp.x, vp.y, 1f, 0f));
        Graphics.Blit(src, dst, _mat);
    }

    // Optional: Timeline / Signals can call these
    public void EnableEffect()  => masterEnabled = true;
    public void DisableEffect() => masterEnabled = false;
    public void ToggleEffect()  => masterEnabled = !masterEnabled;
}
