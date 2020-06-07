using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class JumpPlatform : MonoBehaviour
{

    [SerializeField]
    [Range(0f, 100f)]
    private float _jumpHeight;

    private BoxCollider _triggerCollider;

    private void Start()
    {
        _triggerCollider = GetComponents<BoxCollider>().First(collider => collider.isTrigger);
        //float colliderHeight = _jumpHeight / 2;
        //Vector3 center = _triggerCollider.center;
        //center.y = colliderHeight / 2 / transform.localScale.y;
        //_triggerCollider.center = center;
        //Vector3 size = _triggerCollider.size;
        //size.y = colliderHeight / transform.localScale.y;
        //_triggerCollider.size = size;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DoomController>() != null)
        {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

            float g = Mathf.Abs(Physics.gravity.y);
            float vertVelocity = Mathf.Sqrt(2 * _jumpHeight * g);
            rigidbody.AddForce(transform.up * rigidbody.mass * vertVelocity, ForceMode.Impulse);
        }
    }

}
