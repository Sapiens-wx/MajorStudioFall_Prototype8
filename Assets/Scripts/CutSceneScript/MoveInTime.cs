using System.Collections;
using UnityEngine;

public class MoveInTime : MonoBehaviour
{
    [Header("Move Settings")]
    public float t = 10f; // seconds

    public Vector3 endPos;

    Coroutine _moveRoutine;

    public void MoveToEnd()
    {
        Move(endPos);
    }


    // Call this to move to a world-space position in t seconds
    public void Move(Vector3 targetPosition)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveRoutine(targetPosition, t));
    }

    IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = target;
            yield break;
        }

        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, target, u);
            yield return null;
        }

        transform.position = target; // ensure exact
    }
}
