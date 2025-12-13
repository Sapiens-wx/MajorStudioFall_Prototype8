using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PostCenterFromObject_SimpleMasterToggle : MonoBehaviour
{
    [Header("MASTER TOGGLE (overrides everything)")]
    public bool masterEnabled = true;

    [Header("Post material (shared, live-updates from Inspector)")]
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
        _mat = baseMaterial;    // 🔴 NO Instantiate: use the same material
    }

    void OnDisable()
    {
        _mat = null;            // 🔴 Do NOT DestroyImmediate, it’s a shared asset
    }

    bool IsOnScreenViewport(Vector3 vp)
    {
        return vp.z > 0.0001f &&
               vp.x >= 0f && vp.x <= 1f &&
               vp.y >= 0f && vp.y <= 1f;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!masterEnabled || _mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        if (centerTarget == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        Vector3 vp = _cam.WorldToViewportPoint(centerTarget.position);

        if (autoDisableOffscreen && !IsOnScreenViewport(vp))
        {
            Graphics.Blit(src, dst);
            return;
        }

        _mat.SetVector("_Center", new Vector4(vp.x, vp.y, 1f, 0f));
        Graphics.Blit(src, dst, _mat);
    }

    // Optional helpers
    public void EnableEffect()  => masterEnabled = true;
    public void DisableEffect() => masterEnabled = false;
    public void ToggleEffect()  => masterEnabled = !masterEnabled;
}
