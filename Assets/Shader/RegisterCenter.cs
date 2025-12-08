using UnityEngine;

public class RegisterAsShaderCenterOnStart : MonoBehaviour
{
    [Tooltip("If empty, will search the scene for the ShaderCenter script.")]
    public PostCenterFromObject_SimpleMasterToggle shaderCenter;

    void Start()
    {
        if (shaderCenter == null)
            shaderCenter = FindFirstObjectByType<PostCenterFromObject_SimpleMasterToggle>();

        if (shaderCenter != null)
            shaderCenter.centerTarget = transform;
        else
            Debug.LogWarning("RegisterAsShaderCenterOnStart: No PostCenterFromObject_SimpleMasterToggle found in scene.");
    }
}