using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShakerBackup : MonoBehaviour
{

    private Quaternion _startRotation;
    private Quaternion? _targetRotation;
    private float _degVelocity = 0f;
    private bool _isShaking = false;
    private Coroutine _vibrateCoroutine; 

    private void Start()
    {
        _startRotation = transform.localRotation;
        _targetRotation = null;
    }

    public void ShakeCamera(float duration, float maxAngle, float degVeloctiy)
    {
        StopCoroutine(_vibrateCoroutine);
        _isShaking = false;
        _vibrateCoroutine = StartCoroutine(VibrateCameraCor(duration, maxAngle, degVeloctiy));
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

    public void ShakeRotateCamera(Vector2 direction, float angleDeg, float degVelocity)
    {
        if (_isShaking)
        {
            return;
        }
        ShakeRotateCameraInternal(direction, angleDeg, degVelocity);
    }

    private void ShakeRotateCameraInternal(Vector2 direction, float angleDeg, float degVelocity)
    {
        direction *= -1;
        _degVelocity = degVelocity;

        direction *= Mathf.Tan(angleDeg * Mathf.Deg2Rad);
        Vector3 resDirection = ((Vector3)direction + transform.forward).normalized;
        _targetRotation = Quaternion.FromToRotation(transform.forward, resDirection);
    }

    private IEnumerator VibrateCameraCor(float duration, float maxAngle, float degVelocity)
    {
        float elapsed = 0f;
        _isShaking = true;
        float timePassed = Time.realtimeSinceStartup;
        while (elapsed < duration)
        {
            float currentTime = Time.realtimeSinceStartup;
            elapsed += currentTime - timePassed;
            timePassed = currentTime;
            ShakeRotateCameraInternal(Random.insideUnitSphere.normalized, Random.Range(0, maxAngle), degVelocity);

            yield return new WaitForSeconds(0.05f);
        }
        _isShaking = false;
    }

    //private IEnumerator ShakeRotateCor(float duration, float angleDeg, Vector2 direction)
    //{
    //    float elapsed = 0f;
    //    Quaternion startRotation = transform.localRotation;

    //    float halfDuration = duration / 2;
    //    direction = direction.normalized;
    //    while (elapsed < duration)
    //    {
    //        Vector2 currentDirection = direction;
    //        float t = elapsed < halfDuration ? elapsed / halfDuration : (duration - elapsed) / halfDuration;
    //        float currentAngle = Mathf.Lerp(0f, angleDeg, t);
    //        currentDirection *= Mathf.Tan(currentAngle * Mathf.Deg2Rad);
    //        Vector3 resDirection = ((Vector3)currentDirection + Vector3.forward).normalized;
    //        transform.localRotation = Quaternion.FromToRotation(Vector3.forward, resDirection);

    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }
    //    transform.localRotation = startRotation;
    //}

    //private IEnumerator ShakeCameraCor(float duration, float magnitude, float noize)
    //{
    //    float elapsed = 0f;
    //    Vector3 startPosition = transform.localPosition;
    //    Vector2 noizeStartPoint0 = Random.insideUnitCircle * noize;
    //    Vector2 noizeStartPoint1 = Random.insideUnitCircle * noize;

    //    while (elapsed < duration)
    //    {
    //        Vector2 currentNoizePoint0 = Vector2.Lerp(noizeStartPoint0, Vector2.zero, elapsed / duration);
    //        Vector2 currentNoizePoint1 = Vector2.Lerp(noizeStartPoint1, Vector2.zero, elapsed / duration);
    //        Vector2 cameraPostionDelta = new Vector2(Mathf.PerlinNoise(currentNoizePoint0.x, currentNoizePoint0.y), Mathf.PerlinNoise(currentNoizePoint1.x, currentNoizePoint1.y));
    //        cameraPostionDelta *= magnitude;

    //        transform.localPosition = startPosition + (Vector3)cameraPostionDelta;

    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }
    //    transform.localPosition = startPosition;
    //}
}
