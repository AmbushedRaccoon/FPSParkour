using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class DoomController : MonoBehaviour
{
    public bool IsGrounded
    {
        get => _isGrounded;
    }

    public float CharacterRadius
    {
        get => _doomCollider.radius;
    }

    public float CharacterHeight
    {
        get => _doomGuyHeight;
    }


    [SerializeField] private Transform _hellmetTransform;

    [SerializeField] [Range(1, 50)] private float _mouseSpeed = 1;

    [SerializeField] [Range(1, 20)] private float _moveSpeed = 2f;

    [SerializeField] [Range(0.1f, 200f)] private float _acceleration = 10;
    [SerializeField] private float _accelerationJumpCoef = 1f;

    [SerializeField] [Range(0, 90f)] private float _maxVerticalAngle = 90f;
    [SerializeField] [Range(0, -90f)] private float _minVerticalAngle = -90f;

    [SerializeField] [Range(0.1f, 1f)] private float _crouchHeightCoef = 0.3f;

    [SerializeField] private bool _isInverted = false;

    [SerializeField] [Range(0f, 20f)] private float _jumpHeight = 1f;
    [SerializeField] [Range(0, 5)] private int _jumpsMaxCount = 2;

    [SerializeField] [Range(0f, 1f)] private float _slideMoveSpeedCoef = 0.5f;
    [SerializeField] [Range(0f, 10f)] private float _slideAcceleration = 5f;
    [SerializeField] private bool _isSlideRotationLimited;
    [SerializeField] [Range(0f, 180f)] private float _slideRotationAngleLimit = 90f;

    private  int _jumpsCount = 2;
    private bool _isGonnaJump = false;

    [SerializeField]
    [Range(0f, 90f)]
    private float _slopeAngle = 45;

    [SerializeField]
    LayerMask _groundLayer;

    private float _rotationOX;
    private Vector3 _desiredVelocity;

    private Rigidbody _rigidbody;
    private bool _isGrounded = false;
    private CapsuleCollider _doomCollider;

    private CameraShaker _cameraShaker;

    private bool _isCrouch = false;
    private bool _prevCrouchButtonState = false;
    private bool _skipCollisionCheck = false;
    private bool _isSliding = false;

    private bool _canStand = true;
    private int _crouchObstacleCount = 0;
    private float _doomGuyHeight;
    private float _doomGuyRadius;
    private float _startSlideOYRotation;
    private ParkourController _parkourController;

    private void Start()
    {
        _rotationOX = _hellmetTransform.rotation.eulerAngles.x;
        _rigidbody = GetComponent<Rigidbody>();
        _doomCollider = GetComponent<CapsuleCollider>();
        _cameraShaker = GetComponentInChildren<CameraShaker>();
        var collider = GetComponent<CapsuleCollider>();
        _parkourController = GetComponent<ParkourController>();
        _doomGuyHeight = collider.height;
        _doomGuyRadius = collider.radius;
    }

    private void Update()
    {
        HandleMouse();
        HandleMovement();
        HandleCrouch();
        HandleSlide();
    }

    private bool CanStand()
    {
        float currentHeight = _doomGuyHeight * transform.localScale.y;
        return Physics.CheckCapsule(transform.position, transform.position + (Vector3.up * (_doomGuyHeight - currentHeight / 2)), _doomGuyRadius, _groundLayer);
    }

    private void HandleCrouch()
    {
        if (Time.realtimeSinceStartup - _parkourController.LastParkourTime < _parkourController.ParkourCrouchDelay)
        {
            return;
        }
        bool currentCrouchButtonState = Input.GetAxisRaw("Crouch") == 1;
        if (currentCrouchButtonState != _prevCrouchButtonState && currentCrouchButtonState)
        {
            if (_isCrouch && CanStand())
            {
                return;
            }
            _isCrouch = !_isCrouch;
            Crouch();
        }
        _prevCrouchButtonState = currentCrouchButtonState;
    }

    private void HandleSlide()
    {
        if (_isSliding && _rigidbody.velocity.magnitude < 0.1f)
        {
            _isSliding = false;
        }
    }

    private void Crouch()
    {
        Vector3 currentScale = transform.localScale;
        currentScale.y *= _isCrouch ? _crouchHeightCoef : 1f / _crouchHeightCoef;
        transform.localScale = currentScale;
        if (_isCrouch && _rigidbody.velocity.magnitude >= _moveSpeed * _slideMoveSpeedCoef)
        {
            _startSlideOYRotation = transform.rotation.eulerAngles.y;
            _isSliding = true;
        }
        else
        {
            _isSliding = false;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        _isGrounded = false;
        if (_skipCollisionCheck)
        {
            _skipCollisionCheck = false;
            return;
        }
        foreach (var contact in other.contacts)
        {
            if (Vector3.Angle(transform.up, contact.normal) <= _slopeAngle)
            {
                _isGrounded = true;
                _jumpsCount = _jumpsMaxCount;
                return;
            }
        }
    }

    private void HandleMouse()
    {
        Vector2 deltaMouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector2 deltaMouseStick = new Vector2(Input.GetAxis("PS4RSHorizontal"), Input.GetAxis("PS4RSVertical"));
        deltaMouse = deltaMouse.magnitude > deltaMouseStick.magnitude 
            ? deltaMouse 
            : deltaMouseStick;

        _rotationOX += deltaMouse.y * _mouseSpeed * (_isInverted ? 1 : -1);
        _rotationOX = Mathf.Clamp(_rotationOX, _minVerticalAngle, _maxVerticalAngle);


        if (!_isSlideRotationLimited
            || !_isSliding
            || Mathf.Abs(transform.rotation.eulerAngles.y + deltaMouse.x * _mouseSpeed - _startSlideOYRotation) < _slideRotationAngleLimit / 2)
        {
            transform.Rotate(transform.up, deltaMouse.x * _mouseSpeed);
        }

        _hellmetTransform.localEulerAngles = Vector3.right * _rotationOX;
    }

    private void HandleMovement()
    {
        if (_isSliding)
        {
            _desiredVelocity = Vector3.zero;
            return;
        }
        Vector3 moveDirection = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        if (moveDirection.magnitude > 1f)
        {
            moveDirection = moveDirection.normalized;
        }
        _desiredVelocity = moveDirection * _moveSpeed;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isGonnaJump = true;
        }
    }

    private void FixedUpdate()
    {
        Vector3 currentVelocity = _rigidbody.velocity;
        float oYVelocity = currentVelocity.y;
        currentVelocity.y = 0;

        if (_desiredVelocity != currentVelocity)
        {
            float a = _isSliding ? _slideAcceleration : ( _isGrounded ? _acceleration : _acceleration * _accelerationJumpCoef);

            
            float t = a * Time.deltaTime / (_desiredVelocity - currentVelocity).magnitude;
            currentVelocity = Vector3.Lerp(currentVelocity, _desiredVelocity, t);
        }

        currentVelocity.y = oYVelocity;
        _rigidbody.velocity = currentVelocity;
        if (_isGonnaJump && _jumpsCount > 0)
        {
            float g = Mathf.Abs(Physics.gravity.y);
            float vertVelocity = Mathf.Sqrt(2 * _jumpHeight * g);
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, vertVelocity, _rigidbody.velocity.z);
            _jumpsCount--;
            _skipCollisionCheck = true;
        }

        if (!_isGrounded && _jumpsCount == _jumpsMaxCount)
        {
            _jumpsCount--;
        }

        _isGonnaJump = false;
        _isGrounded = false;
    }

}
