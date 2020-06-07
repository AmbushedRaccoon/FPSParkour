using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParkourController : MonoBehaviour
{
    public float LastParkourTime
    {
        get; private set;
    }

    public float ParkourCrouchDelay
    {
        get; private set;
    }

    [SerializeField]
    private LayerMask _obstacleMask;

    [SerializeField]
    [Range(0f, 5f)]
    private float _parkourHeight = 1f;
    [SerializeField]
    [Range(0, 60f)]
    float _translationVelocity = 2f;
    [SerializeField]
    [Range(0, 60f)]
    float _climbVelocity = 2f;
    [SerializeField]
    [Range(0, 60f)]
    float _forwardVelocity = 2f;
    [SerializeField]
    [Range(0, 20f)]
    float _wallRunVerticalAcceleration = 5f;
    [SerializeField]
    [Range(0, 5f)]
    float _wallRunVerticalDownCoef = 1f;


    private List<ParkourObstacle> _parkourObstacles = new List<ParkourObstacle>();
    private DoomController _playerController;
    private CapsuleCollider _parkourCollider;
    private Rigidbody _rigidbody;
    private bool _isClimbing = false;
    private float _velocityBeforeClimb;
    private Coroutine _parkourCoroutine;
    private Coroutine _wallRunCoroutine;
    private List<ParkourEntryPoint> _parkourStartEntries = new List<ParkourEntryPoint>();
    private List<GameObject> _debugSpheres = new List<GameObject>();
    private const int _spheresMaxCount = 1;

    private void Start()
    {
        _playerController = GetComponent<DoomController>();
        _rigidbody = GetComponent<Rigidbody>();
        foreach (CapsuleCollider collider in GetComponents<CapsuleCollider>())
        {
            if (collider.isTrigger)
            {
                _parkourCollider = collider;
                break;
            }
        }
    }

    ParkourEntryPoint CheckParkourStartEntry(Vector3 direction, bool isClimbing)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, _parkourCollider.radius, _obstacleMask))
        {
            var parkourObstacle = _parkourObstacles.Find(obstacle => obstacle.Collider == hit.collider);
            ParkourObstacle.Wall wall = default(ParkourObstacle.Wall);
            if (parkourObstacle != null && parkourObstacle.TryGetWall(hit.point, ref wall))
            {
                return new ParkourEntryPoint(hit.point, wall, parkourObstacle, isClimbing);
            }
        }
        return null;
    }

    void CheckParkourStartEntries()
    {
        _parkourStartEntries.Clear();
        ParkourEntryPoint forwardEntry = CheckParkourStartEntry(transform.forward, true);
        if (forwardEntry != null)
        {
            _parkourStartEntries.Add(forwardEntry);
        }
        ParkourEntryPoint rightEntry = CheckParkourStartEntry(transform.right, false);
        if (rightEntry != null)
        {
            _parkourStartEntries.Add(rightEntry);
        }
        ParkourEntryPoint leftEntry = CheckParkourStartEntry(transform.right * -1, false);
        if (leftEntry != null)
        {
            _parkourStartEntries.Add(leftEntry);
        }
    }

    void FilterSameWall()
    {
        for (int i = 0; i < _parkourStartEntries.Count; i++)
        {
            for (int j = i + 1; j < _parkourStartEntries.Count; j++)
            {
                if (_parkourStartEntries[i].Wall.Equals(_parkourStartEntries[j].Wall))
                {
                    Vector3 wallLine = _parkourStartEntries[i].Point - _parkourStartEntries[j].Point;
                    Vector3 forwardLine = _parkourStartEntries[i].Point - transform.position;
                    Vector3 rightLine = _parkourStartEntries[j].Point - transform.position;
                    int indexToRemove = Vector3.Angle(wallLine, forwardLine) > Vector3.Angle(wallLine, rightLine) ? j : i;
                    _parkourStartEntries.RemoveAt(indexToRemove);
                    i = -1;
                    break;
                }
            }
        }
    }

    ParkourEntryPoint ConvertParkourEntryToNearest(ParkourEntryPoint entry)
    {
        var result = new ParkourEntryPoint(entry);
        Vector3 closestPoint = result.Wall.Plane.ClosestPointOnPlane(transform.position);
        if (result.ParkourObstacle.Collider.bounds.ClosestPoint(closestPoint) == closestPoint)
        {
            result.Point = closestPoint;
        }
        return result;
    }

    ParkourEntryPoint GetStartParkourEntry()
    {
        int minIndex = 0;
        float minAngle = float.PositiveInfinity;
        for (int i = 0; i < _parkourStartEntries.Count; i++)
        {
            float currentAngle = Mathf.Abs(90f - Vector3.Angle(_parkourStartEntries[i].Wall.Plane.normal, _parkourStartEntries[i].Point - transform.position));
            if (currentAngle < minAngle)
            {
                minAngle = currentAngle;
                minIndex = i;
            }
        }
        return _parkourStartEntries[minIndex];
    }

    private void Update()
    {
        if (_wallRunCoroutine != null)
        {
            return;
        }
        bool isForwardPressed = Input.GetAxis("Vertical") > 0f;
        bool isCrouchPressed = Input.GetAxis("Crouch") > 0f;
        bool isJumpPressed = Input.GetKey(KeyCode.Space);
        if (isCrouchPressed && _parkourCoroutine != null)
        {
            StopCoroutine(_parkourCoroutine);
            _parkourCoroutine = null;
            StopClimbing(false);
        }
        if (!_playerController.IsGrounded && isForwardPressed && !_isClimbing && _parkourObstacles.Count > 0 && isJumpPressed)
        {
            CheckParkourStartEntries();
            FilterSameWall();
            if (_parkourStartEntries.Count == 0)
            {
                return;
            }

            var startEntry = GetStartParkourEntry();
            if (!startEntry.IsClimbing)
            {
                startEntry = ConvertParkourEntryToNearest(startEntry);
            }
            DrawSphere(startEntry.Point, 0.2f);
            if (startEntry.IsClimbing)
            {
                StartClimbing(startEntry);
            }
            else
            {
                StartWallRun(startEntry);
            }
        }
    }

    private void StartClimbing(ParkourEntryPoint climbingStartEntry)
    {
        _isClimbing = true;
        Plane wallPlane = new Plane(climbingStartEntry.Wall.GetPoint(0), climbingStartEntry.Wall.GetPoint(1), climbingStartEntry.Wall.GetPoint(2));
        Vector3 startPostion1 = climbingStartEntry.Point + (wallPlane.normal * _playerController.CharacterRadius * -1);
        Vector3 startPostion2 = climbingStartEntry.Point + (wallPlane.normal * _playerController.CharacterRadius);
        Vector3 climbStartPosition = Vector3.Distance(transform.position, startPostion1) < Vector3.Distance(transform.position, startPostion2) ? startPostion1 : startPostion2;
        Quaternion climbStartRtotation = Quaternion.LookRotation(climbingStartEntry.Point - climbStartPosition, Vector3.up);

        _velocityBeforeClimb = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.z).magnitude;
        _rigidbody.velocity = Vector3.zero;
        _playerController.enabled = false;
        _rigidbody.isKinematic = true;

        _parkourCoroutine = StartCoroutine(Climbing(climbingStartEntry.ParkourObstacle, climbingStartEntry.Wall, climbStartPosition, climbStartRtotation));
    }

    private void StopWallRun()
    {
        if (_wallRunCoroutine != null)
        {
            StopCoroutine(_wallRunCoroutine);
            _wallRunCoroutine = null;
        }

        _playerController.enabled = true;
        _rigidbody.isKinematic = false;
    }

    private void StartWallRun(ParkourEntryPoint wallRunStartEntry)
    {
        float horizontalSpeed = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.z).magnitude;
        float verticalSpeed = _rigidbody.velocity.y; 

        _rigidbody.velocity = Vector3.zero;
        _playerController.enabled = false;
        _rigidbody.isKinematic = true;

        _wallRunCoroutine = StartCoroutine(WallRun(wallRunStartEntry, verticalSpeed, horizontalSpeed));
    }

    private void StopClimbing(bool isComplite = true)
    {
        _isClimbing = false;
        _playerController.enabled = true;
        _rigidbody.isKinematic = false;
        LastParkourTime = Time.realtimeSinceStartup;
        ParkourCrouchDelay = 0.5f;
        if (isComplite)
        {
            ParkourCrouchDelay = 0f;
            _rigidbody.velocity = transform.forward * _velocityBeforeClimb;
        }
    }

    private IEnumerator WallRun(ParkourEntryPoint entryPoint, float verticalSpeed, float horizontalSpeed)
    {
        float elapsedTime = 0f;
        Vector3 directionVector = Vector3.ProjectOnPlane(transform.forward, entryPoint.Wall.Plane.normal).normalized;
        
        while (true)
        {
            float currentDeltaX = horizontalSpeed * elapsedTime;
            float currentDeltaY = verticalSpeed * elapsedTime - _wallRunVerticalAcceleration * Mathf.Pow(elapsedTime, 2f);
            Vector3 currentPosition = entryPoint.Point + directionVector * currentDeltaX;
            currentPosition.y = entryPoint.Point.y + currentDeltaY;

            Vector3 currentPosition0 = currentPosition + entryPoint.Wall.Plane.normal * _playerController.CharacterRadius;
            Vector3 currentPosition1 = currentPosition + -entryPoint.Wall.Plane.normal * _playerController.CharacterRadius;

            transform.position = Vector3.Distance(currentPosition0, transform.position) < Vector3.Distance(currentPosition1, transform.position)
                ? currentPosition0
                : currentPosition1;

            elapsedTime += Time.deltaTime;

            if (elapsedTime > 0.5f)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, -transform.up, out hit, _playerController.CharacterHeight / 2 + 0.5f, _obstacleMask))
                {
                    Debug.Log("StopWallRun");
                    StopWallRun();
                }
            }
            yield return null;
        }
    }

    private IEnumerator Climbing(ParkourObstacle obstacle, ParkourObstacle.Wall wall, Vector3 startPoint, Quaternion startRotation)
    {
        Vector3 startPlayerPosition = transform.position;
        Quaternion startPlayerRotation = transform.rotation;
        float elapsed = 0f;
        float translationTime = Vector3.Distance(startPlayerPosition, startPoint) / _translationVelocity;
        while (elapsed < translationTime)
        {
            transform.position = Vector3.Lerp(startPlayerPosition, startPoint, elapsed / translationTime);
            transform.rotation = Quaternion.Lerp(startPlayerRotation, startRotation, elapsed / translationTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = startRotation;

        startPlayerPosition = transform.position;
        Vector3 targetUpPosition = new Vector3(transform.position.x,
            wall.GetHighestPoint().y + _playerController.CharacterHeight / 2,
            transform.position.z);
        Vector3 targetForwardPosition = targetUpPosition + transform.forward * _playerController.CharacterRadius * 2;
        elapsed = 0f;
        float climbTime = Vector3.Distance(startPlayerPosition, targetUpPosition) / _climbVelocity;
        while (elapsed < climbTime)
        {
            transform.position = Vector3.Lerp(startPlayerPosition, targetUpPosition, elapsed / climbTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        float forwardTime = Vector3.Distance(startPlayerPosition, targetForwardPosition) / _forwardVelocity;
        elapsed = 0f;
        startPlayerPosition = transform.position;
        while (elapsed < forwardTime)
        {
            transform.position = Vector3.Lerp(startPlayerPosition, targetForwardPosition, elapsed / forwardTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        StopClimbing();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _obstacleMask) != 0)
        {
            _parkourObstacles.Add(GetParkourObstacle(other.gameObject));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & _obstacleMask) != 0)
        {
            BoxCollider collider = other.GetComponent<BoxCollider>();
            _parkourObstacles.Remove(_parkourObstacles.Find(obstacle => obstacle.Collider == collider));
        }
    }

    private void DrawSphere(Vector3 position, float scale)
    {
        _debugSpheres.Insert(0, GameObject.CreatePrimitive(PrimitiveType.Sphere));
        Transform sphereTransform = _debugSpheres[0].transform;
        sphereTransform.position = position;
        sphereTransform.localScale = new Vector3(scale, scale, scale);
        sphereTransform.GetComponent<MeshRenderer>().material.color = Color.red;
        sphereTransform.GetComponent<Collider>().enabled = false;

        while (_debugSpheres.Count > _spheresMaxCount)
        {
            int removeIndex = _debugSpheres.Count - (_debugSpheres.Count - _spheresMaxCount);
            GameObject.Destroy(_debugSpheres[removeIndex]);
            _debugSpheres.RemoveAt(removeIndex);
        }
    }

    private ParkourObstacle GetParkourObstacle(GameObject gameObject)
    {
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        var points = new Vector3[8];
        float xSize = collider.size.x;
        float ySize = collider.size.y;
        float zSize = collider.size.z;
        Vector3 center = collider.center;
        //Wall0 - 0, 1, 2, 3
        //Wall1 - 0, 2, 4, 6
        //Wall2 - 4, 5, 6, 7
        //Wall3 - 1, 3, 5, 7
        //Floor - 0, 1, 4, 5

        for (int i = 0; i < 7; i++)
        {
            int coefX = (i & (1 << 0)) == 0 ? 1 : -1;
            int coefY = (i & (1 << 1)) == 0 ? 1 : -1;
            int coefZ = (i & (1 << 2)) == 0 ? 1 : -1;
            points[0] = gameObject.transform.TransformPoint(new Vector3(center.x + xSize / 2 * coefX, center.y + ySize / 2 * coefY, center.z + zSize / 2 * coefZ));
        }

        points[0] = gameObject.transform.TransformPoint(new Vector3(center.x + xSize / 2, center.y + ySize / 2, center.z + zSize / 2));
        points[1] = gameObject.transform.TransformPoint(new Vector3(center.x - xSize / 2, center.y + ySize / 2, center.z + zSize / 2));
        points[2] = gameObject.transform.TransformPoint(new Vector3(center.x + xSize / 2, center.y - ySize / 2, center.z + zSize / 2));
        points[3] = gameObject.transform.TransformPoint(new Vector3(center.x - xSize / 2, center.y - ySize / 2, center.z + zSize / 2));
        points[4] = gameObject.transform.TransformPoint(new Vector3(center.x + xSize / 2, center.y + ySize / 2, center.z - zSize / 2));
        points[5] = gameObject.transform.TransformPoint(new Vector3(center.x - xSize / 2, center.y + ySize / 2, center.z - zSize / 2));
        points[6] = gameObject.transform.TransformPoint(new Vector3(center.x + xSize / 2, center.y - ySize / 2, center.z - zSize / 2));
        points[7] = gameObject.transform.TransformPoint(new Vector3(center.x - xSize / 2, center.y - ySize / 2, center.z - zSize / 2));

        return new ParkourObstacle(new ParkourObstacle.Wall(points[0], points[1], points[4], points[5]),
            new ParkourObstacle.Wall[]
            {
                new ParkourObstacle.Wall(points[0], points[1], points[2], points[3]),
                new ParkourObstacle.Wall(points[0], points[2], points[4], points[5]),
                new ParkourObstacle.Wall(points[4], points[5], points[6], points[7]),
                new ParkourObstacle.Wall(points[1], points[3], points[5], points[7]),
            },
            collider);
    }
}
