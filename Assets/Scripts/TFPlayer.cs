using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Assets.Scripts.Extensions;

namespace Assets.Scripts
{
    public enum Facing
    {
        Right = 1,
        Left = -1
    }

    class TFPlayer : VoBehavior
    {
        private const uint MAX_AIM_SNAP_DIRECTIONS = 32;
        private const uint DEFAULT_AIM_SNAP_DIRECTIONS = 8;

        public const string PLAYER_STATE_NORMAL = "normal";
        public const string PLAYER_STATE_DUCKING = "ducking";
        public const string PLAYER_STATE_DODGING = "dodging";
        public const string PLAYER_STATE_LEDGE_GRAB = "ledgegrab";
        public const string PLAYER_STATE_DYING = "dying";
        public const string PLAYER_STATE_FROZEN = "frozen";

        public string SlipperyTag = null;
        public float Gravity = 0.3f;
        public float MaxFallSpeed = 2.8f;
        public float FastFallSpeed = 3.5f;
        public float JumpHeldGravityMultiplier = 0.5f;
        public float JumpBufferTime = 6.0f;
        public float JumpGraceTime = 6.0f;
        public float WallStickStart = 0.5f;
        public float SlipperyAccelerationMultiplier = 0.35f;
		public float SlipperyReturnRate = 0.1f;
        public float Friction = 0.2f;
        public float AirFriction = 0.14f;
        public float MaxRunSpeed = 1.5f;
        public float RunAcceleration = 0.15f;
        public float RunDecceleration = 0.03f;
        public float AirRunAcceleration = 0.1f;
        public float DodgeCooldownMultiplier = 0.8f;
        public float WallJumpCheck = 2.0f;
        public float WallStickMaxFall = 1.6f;
        public float WallStickAdd = 0.01f;
        public int LedgeCheckVertical = 10;

        //NOTE - Set to 0 for free-aim
        [Range(0, MAX_AIM_SNAP_DIRECTIONS)]
        public uint AimSnapDirections = DEFAULT_AIM_SNAP_DIRECTIONS;

        public TFActor actor
        {
            get
            {
                if (!_actor)
                    _actor = this.GetComponent<TFActor>() as TFActor;
                return _actor;
            }
        }

        public void Awake()
        {
            _stateMachine = new FSMStateMachine();
            _stateMachine.AddState(PLAYER_STATE_NORMAL, this.updateNormal, null, null);
            _stateMachine.AddState(PLAYER_STATE_DUCKING, this.updateDucking, this.enterDuck, this.exitDuck);
            _stateMachine.AddState(PLAYER_STATE_DODGING, this.updateDodging, this.enterDodge, this.exitDodge);
            _stateMachine.AddState(PLAYER_STATE_LEDGE_GRAB, this.updateLedgeGrab, this.enterLedgeGrab, this.exitLedgeGrab);
            _stateMachine.AddState(PLAYER_STATE_DYING, this.updateDying, this.enterDying, this.exitDying);
            _stateMachine.AddState(PLAYER_STATE_FROZEN, null, null, this.exitFrozen);
            _stateMachine.BeginWithInitialState(PLAYER_STATE_NORMAL);

            _jumpBufferTimer = new Timer(JumpBufferTime);
            _jumpBufferTimer.complete();

            _jumpGraceTimer = new Timer(JumpGraceTime);
            _jumpGraceTimer.complete();
        }

        public void Update()
        {
            // - Update spam shot counter

            // Send collision ray(s) from our position in UnitY direction (down) to detect OnGround
            // If so, store hit entity as lastPlatform. Check if this platform is slippery or hot coals and store bools for those as well.
            GameObject groundObject = this.boxCollider2D.CollideFirst(0.0f, TFPhysics.DownY, this.actor.CollisionMask, this.actor.CollisionTag);
            _onGround = groundObject != null;

            if (_onGround)
            {
                _slipperyControl = (this.SlipperyTag != null &&
                    (this.SlipperyTag == this.actor.CollisionTag ||
                    this.boxCollider2D.CollideFirst(0.0f, TFPhysics.DownY, this.actor.CollisionMask, this.SlipperyTag))) ? 0.0f : 1.0f;

                _lastPlatform = groundObject;
            }
            else
            {
                //TODO - Make this time based rather than update frame?
                _slipperyControl = Mathf.Min(_slipperyControl + this.SlipperyReturnRate, 1.0f);
            }

            // Get input state for this player
            _inputState = InputState.GetInputStateForPlayer(0);
            float? aimDirection = getAimDirection(_inputState.AimAxis);
            _aimDirection = aimDirection.HasValue ? aimDirection.GetValueOrDefault() : (_facing == Facing.Right ? 0.0f : Mathf.PI);

            // Update jumpBufferCounter, and if input indicates Jump is pressed, set it to JUMP_BUFFER (6)
            _jumpBufferTimer.update();
            if (_inputState.JumpStarted)
            {
                _jumpBufferTimer.reset();
                _jumpBufferTimer.start();
            }

            // - If we're aiming, play aiming sound (?) and update lastAimDirection to AimDirection

            // If we're on ground, do some stuff:
            if (_onGround)
            {
                _jumpGraceTimer.reset();
                _jumpGraceTimer.start();
                _wallStickMax = this.WallStickStart;
                _graceLedgeDir = 0;
            }

            // - Otherwise, update our jump grace counter (I believe this is for stored jumps)

            // Call the update method for our current state
            _stateMachine.Update();

            // - Check if we should pick up an arrow
        }

        public string updateNormal()
        {
            // If we're trying to duck, go to ducking state, unless we are within aim-down grace period

            // Apply speed multiplier
            float multiplier = Mathf.Lerp(this.SlipperyAccelerationMultiplier, 1.0f, _slipperyControl);

            // Turning around
            if ((_aiming && _onGround) || (!_aiming && _slipperyControl == 1.0f && _inputState.MoveX != Math.Sign(_velocity.x)))
            {
                float maxMove = (_onGround ? this.Friction : this.AirFriction) * multiplier;
                _velocity.x = _velocity.x.Approach(0.0f, maxMove * Time.deltaTime);
            }

            // Normal movement
            if (!_aiming && _inputState.MoveX != 0)
            {
                // Deccel if past max speed
                if (Math.Abs(_velocity.x) > this.MaxRunSpeed && Math.Sign(_velocity.x) == _inputState.MoveX)
                {
                    _velocity.x = _velocity.x.Approach(this.MaxRunSpeed * (float)_inputState.MoveX, this.RunDecceleration * Time.deltaTime);
                }

                // Accelerate
                else
                {
                    float acceleration = _onGround ? this.RunAcceleration : this.AirRunAcceleration;
                    acceleration *= multiplier;
                    if (_dodgeCooldown)
                        acceleration *= this.DodgeCooldownMultiplier;

                    _velocity.x = _velocity.x.Approach(this.MaxRunSpeed * (float)_inputState.MoveX, acceleration *= Time.deltaTime);
                }
            }

            _cling = 0;

            if (!_onGround)
            {
                // If jump button is held down use smaller number for gravity
                float gravity = (_velocity.y <= 1.0f && _inputState.Jump && _canJumpHold) ? (this.JumpHeldGravityMultiplier * this.Gravity) : this.Gravity;
                float targetFallSpeed = this.MaxFallSpeed;
                
                // Check if we're wall sliding
                if (_inputState.MoveX != 0 && canWallSlide((Facing)_inputState.MoveX))
                {
                    targetFallSpeed = _wallStickMax;
                    _wallStickMax = _wallStickMax.Approach(this.WallStickMaxFall, this.WallStickAdd * Time.deltaTime);
                    _cling = _inputState.MoveX;
                }
                else
                {
                    // Check if we need to fast fall
                    if (_inputState.MoveY == TFPhysics.DownY && Math.Sign(_velocity.y) == TFPhysics.DownY)
                        targetFallSpeed = this.FastFallSpeed;
                }
                _velocity.y = _velocity.y.Approach(targetFallSpeed, gravity * Time.deltaTime);
            }

            // Check if we need to dodge
            if (!_dodgeCooldown && _inputState.DodgeStarted)
            {
                if (_inputState.MoveX != 0)
                    _facing = (Facing)_inputState.MoveX;
                return PLAYER_STATE_DODGING;
            }

            // Check if it's time to jump
            if (_inputState.JumpStarted || !_jumpBufferTimer.completed)
            {
                if (!_jumpGraceTimer.completed)
                {
                    int num4 = this.graceLedgeDir;
                    if (this.input.MoveX != num4)
                    {
                        num4 = 0;
                    }
                    this.Jump(true, true, false, num4);
                }
                else
                {
                    if (this.CanWallJump(Facing.Left))
                    {
                        this.WallJump(1);
                    }
                    else
                    {
                        if (this.CanWallJump(Facing.Right))
                        {
                            this.WallJump(-1);
                        }
                        else
                        {
                            if (this.HasWings && !this.flapBounceCounter)
                            {
                                this.WingsJump();
                            }
                        }
                    }
                }
            }

            if (this.Aiming)
            {
                if (!this.input.ShootCheck)
                {
                    this.ShootArrow();
                }
            }
            else
            {
                if (this.input.ShootPressed)
                {
                    this.Aiming = true;
                }
            }
            if (this.moveAxis.X != 0f)
            {
                this.Facing = (Facing)this.moveAxis.X;
            }
            base.MoveH(this.Speed.X * Engine.TimeMult, this.onCollideH);
            base.MoveV(this.Speed.Y * Engine.TimeMult, this.onCollideV);
            if (!this.OnGround && !this.Aiming && this.Speed.Y >= 0f && this.moveAxis.X != 0f && this.moveAxis.Y <= 0f && base.CollideCheck(GameTags.Solid, this.Position + Vector2.UnitX * this.moveAxis.X * 2f))
            {
                int direction = Math.Sign(this.moveAxis.X);
                for (int i = 0; i < 10; i++)
                {
                    if (this.CanGrabLedge((int)base.Y - i, direction))
                    {
                        return this.GrabLedge((int)base.Y - i, direction);
                    }
                }
            }

            return PLAYER_STATE_NORMAL;
        }

        public string updateDodging()
        {
            return PLAYER_STATE_DODGING;
        }

        public string updateDucking()
        {
            return PLAYER_STATE_DUCKING;
        }

        public string updateLedgeGrab()
        {
            return PLAYER_STATE_LEDGE_GRAB;
        }

        public string updateDying()
        {
            return PLAYER_STATE_DYING;
        }

        public void enterDodge()
        {

        }

        public void exitDodge()
        {

        }

        public void enterDuck()
        {

        }

        public void exitDuck()
        {

        }

        public void enterLedgeGrab()
        {

        }

        public void exitLedgeGrab()
        {

        }

        public void enterDying()
        {

        }

        public void exitDying()
        {

        }

        public void exitFrozen()
        {

        }

        /**
         * Private
         */
        private TFActor _actor;
        private FSMStateMachine _stateMachine;
        private bool _onGround;
        private float _slipperyControl;
        private GameObject _lastPlatform;
        private float _aimDirection;
        private Facing _facing = Facing.Right;
        private Timer _jumpBufferTimer;
        private Timer _jumpGraceTimer;
        private float _wallStickMax;
        private int _graceLedgeDir;
        private bool _aiming;
        private Vector2 _velocity;
        private bool _dodgeCooldown;
        private int _cling;
        private bool _canJumpHold;
        private InputState _inputState;

        private float? getAimDirection(Vector2 axis)
        {
            if (axis == Vector2.zero)
                return null;

            if (this.AimSnapDirections > 0)
            {
                float increment = (Mathf.PI * 2.0f) / (float)(this.AimSnapDirections);
                return new float?(Mathf.Round((axis.Angle() / increment)) * increment);
            }
            return new float?(axis.Angle());
        }

        private bool canWallSlide(Facing direction)
        {
            return !_aiming && _inputState.MoveY != TFPhysics.DownY && canWallJump(direction);
        }

        private bool canWallJump(Facing direction)
        {
            //TODO - is LedgeCheckVertical the right thing to use here?
            // Make sure we are far enough off the ground
            if (this.boxCollider2D.CollideFirst(0.0f, (float)TFPhysics.DownY * (this.LedgeCheckVertical / 2)))
                return false;

            //TODO - is the -1.0f necessary?
            // We can only wall jump if the top of our body is next to the wall
            Vector2 wallJumpCollidePoint = direction == Facing.Right ?
                new Vector2(_actor.RightX - 1.0f + this.WallJumpCheck, _actor.TopY) :
                new Vector2(_actor.LeftX - this.WallJumpCheck, _actor.TopY);
            return _actor.CollidePoint(wallJumpCollidePoint);
        }

        /**
         * Editor Assistance
         */
        [ExecuteInEditMode]
        void OnValidate()
        {
            // If AimSnapDirections isn't a power of 2, clamp to the nearest power of 2
            if (this.AimSnapDirections > 2)
            {
                uint pow2 = 2;
                while (pow2 < this.AimSnapDirections)
                {
                    pow2 *= 2;
                }

                if (pow2 > this.AimSnapDirections)
                {
                    uint prevPow2 = pow2 / 2;
                    this.AimSnapDirections = (pow2 < MAX_AIM_SNAP_DIRECTIONS && pow2 - this.AimSnapDirections < this.AimSnapDirections - prevPow2) ? pow2 : prevPow2;
                }
            }
        }
    }
}
