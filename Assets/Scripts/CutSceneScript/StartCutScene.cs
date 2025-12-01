using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class StartCutScene : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [Tooltip("Root GameObject of the Cinemachine camera to enable during the cutscene.")]
    [SerializeField] private GameObject cinematicCameraRoot;

    [Tooltip("GameObject that plays the cutscene (e.g., Timeline). Should be set to Play On Awake.")]
    [SerializeField] private GameObject cutsceneObject;

    [Tooltip("Player's main camera. This will be disabled during the cutscene.")]
    [SerializeField] private Camera playerCamera;

    [Header("Trigger Settings")]
    [Tooltip("Only trigger when the entering collider has the 'Player' tag.")]
    [SerializeField] private bool onlyPlayerTag = true;

    [Tooltip("If true, the trigger will be consumed after first activation and won't react again.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasActivated;

    private void Awake()
    {
        // Ensure the collider is a trigger (required for OnTriggerEnter to behave as intended).
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void Reset()
    {
        // Try to auto-fill the player camera if available.
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }
    }

    // Public method: can be called by other scripts or UnityEvents to start the cutscene.
    public void Activate()
    {
        if (triggerOnlyOnce && hasActivated) return;
        hasActivated = true;

        // Disable player camera
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        // Enable cinematic camera
        if (cinematicCameraRoot != null)
        {
            cinematicCameraRoot.SetActive(true);
        }

        // Enable cutscene object (assumed to be Play On Awake)
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
        }
    }

    // Public method: revert state after cutscene has finished.
    public void Deactivate()
    {
        // Disable cinematic camera
        if (cinematicCameraRoot != null)
        {
            cinematicCameraRoot.SetActive(false);
        }

        // Disable cutscene object
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(false);
        }

        // Re-enable player camera
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasActivated) return;

        if (onlyPlayerTag)
        {
            if (!other.CompareTag("Player")) return;
        }

        Activate();
    }
}
