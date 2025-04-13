using System;
using UnityEngine;

namespace Pirate {
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private KeyCode _up;
        [SerializeField] private KeyCode _left;
        [SerializeField] private KeyCode _down;
        [SerializeField] private KeyCode _right;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        #region Interface 

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped; 

        #endregion

        private float _time;

        // Runs before the object is loaded and adds all of the objects from the SerializedFields
        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        // Character movement loop that takes in input and reacts to it, runs every frame
        private void Update() {
            _time += Time.deltaTime; // Used for consistent physics regardless of the frame rate
            GatherInput();
        }
        
        private void GatherInput() {
            _frameInput = new FrameInput {
                JumpDown = Input.GetKeyDown(_up),
                JumpHeld = Input.GetKey(_up),
                Move = new Vector2(
                                    (Input.GetKey(_right) ? 1 : 0) - (Input.GetKey(_left) ? 1 : 0), 
                                    (Input.GetKey(_up) ? 1: 0) - (Input.GetKey(_down) ? 1 : 0))
            };

            if(_frameInput.JumpDown) {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate(){
            CheckCollisions();

            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
            
            Animations();
        }

        #region Collision

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions() {
            Physics2D.queriesStartInColliders = false;

            //  Ground and celing
            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);
            
            if(!_grounded && groundHit) { // Hit ground
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if(_grounded && !groundHit) { // Left the ground
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }
            
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }
        
        #endregion
        
        #region Jumping
        
        // Jump buffering and coyote time mechanics
        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;
        
        private void HandleJump() {
            if(!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true;

            if(!_jumpToConsume && !HasBufferedJump) return;

            if(_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }
        
        private void ExecuteJump() {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }
        
        #endregion

        #region Horizontal

        private void HandleDirection() {
            if(_frameInput.Move.x == 0) {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
            else {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
                if(_frameInput.Move.x == 1) _spriteRenderer.flipX = false;
                if(_frameInput.Move.x == -1) _spriteRenderer.flipX = true;
            }
        }
        
        #endregion

        #region Gravity

        private void HandleGravity() {
            if(_grounded && _frameVelocity.y <= 0f) {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else {
                var inAirGravity = _stats.FallAcceleration;
                if(_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion
        
        #region Animation

        private void Animations() {
            if(_grounded) {
                
                _animator.SetBool("isJumping", false);
                _animator.SetBool("isFalling", false);
            }
            else if(_frameVelocity.y < 0) {
                _animator.SetBool("isFalling", true);
            }
            else {
                _animator.SetBool("isJumping", true);
            }
            
            if(_frameInput.Move.x != 0){
                _animator.SetBool("isRunning", true);
            }
            else {
                _animator.SetBool("isRunning", false);
            }
        }

        #endregion
        
        private void ApplyMovement() => _rb.velocity = _frameVelocity;

        #if UNITY_EDITOR
            private void OnValidate()
            {
                if(_stats == null) Debug.LogWarning("Player stats not assigned", this);
            }
        #endif
    }
    
    public struct FrameInput {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }
    
    public interface IPlayerController {
        public event Action<bool, float> GroundedChanged;
        
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}