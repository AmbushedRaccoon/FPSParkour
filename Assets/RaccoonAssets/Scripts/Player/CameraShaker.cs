using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private Quaternion _startRotation;
    private Quaternion? _targetRotation;
    private float _degVelocity;
    private bool _isShaking;
    private Coroutine _shakeCoroutine;

    public void ShakeRotateCamera(Vector2 direction, float angleDeg, float degVelocity)
    {
        if (_isShaking)
        {
            return;
        }
        ShakeRotateCameraInternal(direction, angleDeg, degVelocity);
    }

    public void ShakeCamera(float duration, float maxAngle, float degVelocity)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }
        _isShaking = false;
        _shakeCoroutine = StartCoroutine(VibrateCameraCor(duration, maxAngle, degVelocity));
    }

    private void Start()
    {
        _startRotation = transform.localRotation;
    }

    private void Update()
    {
        Quaternion target = _targetRotation == null ? _startRotation : _targetRotation.Value;
        if (target == transform.localRotation)
        {
            return;
        }

        float t = (Time.deltaTime * _degVelocity) / Quaternion.Angle(transform.localRotation, target);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, target, t);
        if (transform.localRotation == _targetRotation)
        {
            _targetRotation = null;
        }
    }

    private IEnumerator VibrateCameraCor(float duration, float maxAngle, float degVelocity)
    {
        _isShaking = true;
        float elapsed = 0f;
        float timePassed = Time.realtimeSinceStartup;
        while (elapsed < duration) 
        {
            float currentTime = Time.realtimeSinceStartup;
            elapsed += currentTime - timePassed;
            timePassed = currentTime;

            ShakeRotateCameraInternal(Random.insideUnitCircle, Random.Range(0, maxAngle), degVelocity);

            yield return new WaitForSeconds(0.05f);
        }

        _isShaking = false;
    }

    private void ShakeRotateCameraInternal(Vector2 direction, float angleDeg, float degVelocity)
    {
        _degVelocity = degVelocity;
        direction = direction.normalized;
        direction *= Mathf.Tan(angleDeg * Mathf.Deg2Rad);
        Vector3 resDirection = ((Vector3)direction + Vector3.forward).normalized;
        Debug.Log(resDirection);
        _targetRotation = Quaternion.FromToRotation(Vector3.forward, resDirection);
    }
}
