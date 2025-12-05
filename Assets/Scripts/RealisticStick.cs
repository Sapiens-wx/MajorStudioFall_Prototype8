using UnityEngine;

public class RealisticStick : Singleton<RealisticStick>
{
    public Transform target;

    Quaternion originalRotation;
    void Start() {
        originalRotation=transform.localRotation;
    }
    void FixedUpdate() {
        Vector3 dir=(target.position-transform.position).normalized;
        transform.forward=dir;
    }
}