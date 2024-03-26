using UnityEngine;

namespace HikersMod.Components;

internal class VaultingController : MonoBehaviour
{
    private PlayerCharacterController _characterController;
    private CapsuleCollider _capsuleCollider;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void Update()
    {
        Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
        Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
        if (!_characterController.IsGrounded() && localVelocity.y >= 0f)
        {
            _capsuleCollider.height = 1f;
            _capsuleCollider.center = new Vector3(0f, 0.5f, 0f);
        }
        else
        {
            _capsuleCollider.height = Mathf.MoveTowards(_capsuleCollider.height, 2f, Time.deltaTime);
            _capsuleCollider.center = new Vector3(0f, Mathf.MoveTowards(_capsuleCollider.center.y, 0f, Time.deltaTime), 0f);
        }
    }
}