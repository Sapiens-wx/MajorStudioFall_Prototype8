using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class OutlinePostProcess : MonoBehaviour
{
    [Tooltip("Material using one of the outline shaders below (usually the final one).")]
    public Material outlineMaterial;

    private Camera _cam;

    private void OnEnable()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null)
        {
            // Ask Unity to build a depth+normals texture for this camera
            _cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (outlineMaterial != null)
        {
            Graphics.Blit(src, dst, outlineMaterial);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }
}