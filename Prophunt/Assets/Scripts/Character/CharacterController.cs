using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Character
{
    public class CharacterController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private UnityEngine.CharacterController _characterController;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private GameObject model;

        [Header("Properties")]
        [SerializeField] private float speed;
        [SerializeField] private float gravityAcceleration;
        [SerializeField] private float animationLerp;
        [SerializeField] private float jumpForce;

        [Header("Footsteps")]
        [SerializeField] private AudioSource footstepAudio;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float footstepFrequency;

        public Vector3 HeadPosition => cameraAnchor.position;
        public Vector3 ForwardDirection => cameraAnchor.forward;
        public bool OnGround { get; private set; }

        private Renderer[] _modelRenderer;

        //  scaled between -1; 1
        private float xSpeed, zSpeed;
        private readonly NetworkVariable<float> _rotX = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _rotY = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Vector2 _movementCommand;
        private Vector2 _rotateCommand;
        private bool _sneaking;
        private Vector3 _moveDir;
        private float _gravity;
        private bool _jump;
        private float _lastFootstep;

        private void Awake()
        {
            _modelRenderer = model.GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            ProcessInput();
            UpdatePosition();
            UpdateRotation();
            UpdateAnimatorValues();
        }

        public void SetVisible(bool visible)
        {
            var renderMode = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            foreach (var renderer in _modelRenderer)
            {
                renderer.shadowCastingMode = renderMode;
            }
        }

        public void SendInput(Vector2 movement, Vector2 rotate, bool sneaking)
        {
            _movementCommand = movement;
            _rotateCommand = rotate;
            _sneaking = sneaking;
        }

        public void Jump()
        {
            if (OnGround)
                _jump = true;
        }

        private void UpdateAnimatorValues()
        {
            if (!IsOwner) return;

            _animator.SetFloat("X", xSpeed);
            _animator.SetFloat("Z", zSpeed);
            _animator.SetBool("Sneak", _sneaking);
        }

        private void ProcessInput()
        {
            if (!IsOwner) return;

            _moveDir = transform.forward * _movementCommand.y;
            _moveDir += transform.right * _movementCommand.x;
            _moveDir.Normalize();
            _moveDir *= speed;
            if (_sneaking)
            {
                _moveDir /= 2f;
            }
            if (_jump)
                _gravity = jumpForce;
            _moveDir.y = _gravity;

            _rotX.Value = (_rotX.Value + _rotateCommand.x) % 360;
            _rotY.Value = Mathf.Clamp(_rotY.Value - _rotateCommand.y, -90f, 90f);
        }

        private void UpdatePosition()
        {
            if (!IsOwner) return;

            var previousPosition = transform.position;

            var collisionFlags = _characterController.Move(_moveDir * Time.deltaTime); // hit ground
            OnGround = (collisionFlags & CollisionFlags.Below) != 0;

            if (OnGround || (collisionFlags & CollisionFlags.Above) != 0)
                _gravity = -1f;
            else
                _gravity -= Time.deltaTime * gravityAcceleration;

            _jump = false;

            var velocity = (transform.position - previousPosition) / Time.deltaTime;
            velocity /= speed;

            var localVelocity = Quaternion.Inverse(transform.root.rotation) * velocity;
            xSpeed = Mathf.Clamp(Mathf.Lerp(xSpeed, localVelocity.x * 2, Time.deltaTime * animationLerp), -1, 1);
            zSpeed = Mathf.Clamp(Mathf.Lerp(zSpeed, localVelocity.z * 2, Time.deltaTime * animationLerp), -1, 1);

            if (OnGround)
            {
                if (localVelocity.sqrMagnitude > 0.2 && !_sneaking)
                {
                    _lastFootstep -= Time.deltaTime;
                    if (_lastFootstep <= 0f)
                    {
                        _lastFootstep = footstepFrequency;
                        PlayFootstepRpc((byte)Random.Range(0, footstepSounds.Length));
                    }
                }
            } else
            {
                _lastFootstep = 0f;
            }
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        private void PlayFootstepRpc(byte clipId)
        {
            footstepAudio.clip = footstepSounds[clipId];
            footstepAudio.Play();
        }

        private void UpdateRotation()
        {
            transform.root.localRotation = Quaternion.Euler(0f, _rotX.Value, 0f);
            cameraAnchor.localRotation = Quaternion.Euler(_rotY.Value, 0f, 0f);
        }
    }
}