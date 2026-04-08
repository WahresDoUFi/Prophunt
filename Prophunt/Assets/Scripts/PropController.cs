using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using Weapons;

public class PropController : NetworkBehaviour, IDamageable
{
    public static int GetPropCount => AliveProps.Count;
    public static List<PropController> AliveProps = new();

    [Header("References")]
    [SerializeField] private Transform propParent;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonCamera;
    [SerializeField] private PhysicsMaterial propPhysicsMaterial;
    [SerializeField] private AudioSource jumpAudio;

    [Header("Properties")]
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float maxSlope;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float healthMultiplier;
    [SerializeField] private string outlineLayer;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepAudio;
    [SerializeField] private float footstepFrequency;
    [SerializeField] private AudioClip[] footstepSounds;

    [Header("Taunt")]
    [SerializeField] private AudioSource tauntAudio;
    [SerializeField] private AudioClip[] tauntSounds;

    public float MaxHealth => _maxHealth;
    public float Health => _health.Value;
    public byte MaxRerolls { set => _maxRerolls.Value = value; }

    private BoxCollider _boxCollider;
    private GameObject _propObject;
    private bool _jumping;
    private bool _sneaking;
    private Rigidbody _rigidbody;
    private Vector3 _moveDir;
    private bool _onGround;
    private float _maxHealth;
    private float _lastFootstep;

    private float xRot, yRot;

    private readonly NetworkVariable<Vector3> _position = new(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Quaternion> _rotation = new(Quaternion.identity, writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> _propIndex = new(-1);
    private readonly NetworkVariable<byte> _maxRerolls = new();
    private readonly NetworkVariable<float> _health = new(writePerm: NetworkVariableWritePermission.Owner);

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnNetworkSpawn()
    {
        AliveProps.Add(this);
        _propIndex.OnValueChanged += (_, _) => PropUpdated();
        PropUpdated();
        if (IsOwner)
        {
            RerollPropRpc();
            thirdPersonCamera.GetComponent<CinemachineCamera>().Priority = 3;
        }
        else
        {
            _position.OnValueChanged += PositionUpdated;
            _rotation.OnValueChanged += RotationUpdated;
            PositionUpdated(_position.Value, _position.Value);
            RotationUpdated(_rotation.Value, _rotation.Value);
        }

        if (IsHost)
        {
            _health.OnValueChanged += HealthChanged;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        ProcessInput();
        RotateCamera();
        MoveCamera();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (_rigidbody == null) return;
        if (_jumping) JumpSoundRpc();
        _jumping = false;

        var targetRotation = Quaternion.Euler(0f, xRot, 0f);
        if (_rigidbody.isKinematic == false)
        {
            _rigidbody.rotation = targetRotation;
            _rigidbody.linearVelocity = _moveDir;
        }
        _onGround = false;

        var center = _boxCollider.transform.TransformPoint(_boxCollider.center);
        var size = _boxCollider.size;
        size.y *= 0.5f;
        var halfExtends = Vector3.Scale(size, Vector3.one * 0.5f);
        if (Physics.BoxCast(center, halfExtends, Vector3.down, out RaycastHit hit, _boxCollider.transform.rotation, size.y + groundCheckDistance))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) < maxSlope)
                _onGround = true;
        }

        _position.Value = _rigidbody.position;
        _rotation.Value = _rigidbody.rotation;

        if (!_sneaking && _onGround && _moveDir.sqrMagnitude > 1)
        {
            _lastFootstep -= Time.fixedDeltaTime;
            if (_lastFootstep < 0)
            {
                _lastFootstep += footstepFrequency;
                PlayFootstepRpc(Random.Range(0, footstepSounds.Length));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        AliveProps.Remove(this);
        GameManager.Instance.PropDied();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RerollPropRpc()
    {
        _propIndex.Value = PropProvider.GetRandomPropIndex(_propIndex.Value);
    }

    private void ProcessInput()
    {
        if (_rigidbody == null) return;
        var input = InputManager.GetPlayerMovement();
        _sneaking = InputManager.IsSneaking();
        _moveDir = _rigidbody.transform.forward * input.y;
        _moveDir += _rigidbody.transform.right * input.x;
        _moveDir.Normalize();
        _moveDir *= speed;
        if (_sneaking)
            _moveDir *= 0.5f;
        _jumping |= InputManager.JumpTriggered() && _onGround;
        if (_jumping)
            _moveDir.y = jumpForce;
        else
            _moveDir.y = _rigidbody.linearVelocity.y;

        if (InputManager.AbilityTriggered())
        {
            _moveDir = Vector3.zero;
            FreezePositionRpc(_rigidbody.position, _rigidbody.rotation);
        } else if (_rigidbody.isKinematic)
        {
            if (_moveDir.magnitude > 0.1f)
                _rigidbody.isKinematic = false;
        }

        if (InputManager.ReloadTriggered())
        {
            RerollPropRpc();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    private void FreezePositionRpc(Vector3 position, Quaternion rotation)
    {
        _rigidbody.isKinematic = true;
        _rigidbody.position = position;
        _rigidbody.rotation = rotation;
    }

    private void RotateCamera()
    {
        xRot = (xRot + InputManager.GetMouseDelta().x) % 360;
        yRot = Mathf.Clamp(yRot - InputManager.GetMouseDelta().y, -90f, 90f);

        cameraTarget.rotation = Quaternion.Euler(yRot, xRot, 0f);
    }

    private void MoveCamera()
    {
        if (_rigidbody == null) return;
        var targetPosition = _rigidbody.transform.TransformPoint(_rigidbody.centerOfMass);
        cameraTarget.transform.position = Vector3.Lerp(cameraTarget.transform.position, targetPosition, Time.fixedDeltaTime * 10f);
    }

    public void Taunt()
    {
        PlayTauntRpc(Random.Range(0, tauntSounds.Length));
    }

    [Rpc(SendTo.Everyone)]
    private void PlayTauntRpc(int clipId)
    {
        tauntAudio.clip = tauntSounds[clipId];
        tauntAudio.Play();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    void JumpSoundRpc()
    {
        jumpAudio.Play();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    void PlayFootstepRpc(int clipId)
    {
        footstepAudio.clip = footstepSounds[clipId];
        footstepAudio.Play();
    }

    void PropUpdated()
    {
        if (_propIndex.Value == -1) return;
        var spawnPosition = propParent.transform.position;
        var spawnRotation = Quaternion.identity;
        if (_propObject != null)
        {
            spawnPosition = _propObject.transform.position;
            spawnRotation = _propObject.transform.rotation;
            Destroy(_propObject);
        }
        _propObject = Instantiate(PropProvider.GetPropById(_propIndex.Value), spawnPosition, spawnRotation);
        if (GameManager.Instance.HunterClientId != NetworkManager.Singleton.LocalClientId &&
            !IsOwner)
            _propObject.layer = LayerMask.NameToLayer(outlineLayer);
        _propObject.transform.SetParent(propParent, true);

        _rigidbody = _propObject.GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
        _rigidbody.isKinematic = !IsOwner;
        _boxCollider = _propObject.GetComponent<BoxCollider>();
        _boxCollider.enabled = true;
        _boxCollider.material = propPhysicsMaterial;
        if (_propObject.TryGetComponent(out MeshCollider meshCollider))
        {
            meshCollider.enabled = false;
        }

        var propSize = _boxCollider.bounds.size.magnitude;
        thirdPersonCamera.CameraDistance = propSize * 2f;
        thirdPersonCamera.ShoulderOffset = new Vector3(0f, thirdPersonCamera.CameraDistance / 3f, 0f);

        if (IsOwner)
        {
            var healthPercentage = Health > 0 ? Health / MaxHealth : 1f;
            _maxHealth = propSize * healthMultiplier;
            _health.Value = MaxHealth * healthPercentage;
        }
    }

    private void PositionUpdated(Vector3 oldPosition, Vector3 newPosition)
    {
        cameraTarget.position = newPosition;
        if (_rigidbody == null) return;
        _rigidbody.position = oldPosition;
        _rigidbody.MovePosition(newPosition);
    }

    private void RotationUpdated(Quaternion oldRotation, Quaternion newRotation)
    {
        if (_rigidbody == null) return;
        _rigidbody.rotation = oldRotation;
        _rigidbody.MoveRotation(newRotation);
    }

    private void HealthChanged(float previous, float current)
    {
        if (current <= 0)
            NetworkObject.Despawn();
    }

    public void Damage(float damage, Vector3 point, Vector3 direction)
    {
        TakeDamageRpc(damage, point, direction);
    }

    [Rpc(SendTo.Owner)]
    private void TakeDamageRpc(float damage, Vector3 point, Vector3 direction)
    {
        _health.Value -= damage;
        _rigidbody.AddForceAtPosition(point, direction * damage);
    }
}
