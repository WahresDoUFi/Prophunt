using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Character
{
    public class CharacterController : MonoBehaviour
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

        public bool OnGround { get; private set; }

        private Renderer[] _modelRenderer;

        //  scaled between -1; 1
        private float xSpeed, zSpeed;
        private float _rotX, _rotY;

        private Vector2 _movementCommand;
        private Vector2 _rotateCommand;
        private bool _sneaking;
        private Vector3 _moveDir;
        private float _gravity;
        private bool _jump;

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
            _animator.SetFloat("X", xSpeed);
            _animator.SetFloat("Z", zSpeed);
            _animator.SetBool("Sneak", _sneaking);
        }

        private void ProcessInput()
        {
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

            _rotX = (_rotX + _rotateCommand.x) % 360;
            _rotY = Mathf.Clamp(_rotY - _rotateCommand.y, -90f, 90f);
        }

        private void UpdatePosition()
        {
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
        }

        private void UpdateRotation()
        {
            transform.root.localRotation = Quaternion.Euler(0f, _rotX, 0f);
            cameraAnchor.localRotation = Quaternion.Euler(_rotY, 0f, 0f);
        }
    }
}