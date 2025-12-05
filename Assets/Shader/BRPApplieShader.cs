using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BRPApplieShader : MonoBehaviour
{
    [Header("Toggle")]
    public bool enableEffect = true;

    [Header("Material")]
    public Material shaderMat;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!enableEffect || shaderMat == null)
        {
            Graphics.Blit(src, dst);   // pass-through (no effect)
            return;
        }

        Graphics.Blit(src, dst, shaderMat);
    }
}