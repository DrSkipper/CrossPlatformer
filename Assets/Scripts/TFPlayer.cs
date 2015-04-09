﻿using UnityEngine;
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
        public float MaxFallSpeed = 2.8f;
        public float WallJumpCheck = 2.0f;
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
            
            // - Regenerate arrows if need be

            // - Regenerate shields if need be

            // - Update opacity based on visibility and character state

            // - Handle sprite scaling for non-dodging states
            
            // - Handle opacity for ducking

            // - Send collision ray(s) from our position in UnitY direction (down) to detect OnGround
            // - If so, store hit entity as lastPlatform. Check if this platform is slippery or hot coals and store bools for those as well.
            GameObject groundObject = this.boxCollider2D.CollideFirst(0.0f, -Vector2.up.y, this.actor.CollisionMask, this.actor.CollisionTag);
            _onGround = groundObject != null;

            if (_onGround)
            {
                _slipperyControl = (this.SlipperyTag != null &&
                    (this.SlipperyTag == this.actor.CollisionTag ||
                    this.boxCollider2D.CollideFirst(0.0f, -Vector2.up.y, this.actor.CollisionMask, this.SlipperyTag))) ? 0.0f : 1.0f;

                _lastPlatform = groundObject;
            }
            else
            {
                //TODO - Make this time based rather than update frame?
                _slipperyControl = Mathf.Min(_slipperyControl + this.SlipperyReturnRate, 1.0f);
            }

            // - Check if on hot coals

            // - Get input state for this player
            _inputState = InputState.GetInputStateForPlayer(0);

            // - Get aimDirection (circular) from joystick axis
            _moveAxis = new Vector2(_inputState.MoveX, _inputState.MoveY);
            float? aimDirection = getAimDirection(_inputState.AimAxis);
            _aimDirection = aimDirection.HasValue ? aimDirection.GetValueOrDefault() : (_facing == Facing.Right ? 0.0f : Mathf.PI);

            // - If we're frozen, just set Facing to appropriate direction and exit Update function

            // - Update jumpBufferCounter, and if input indicates Jump is pressed, set it to JUMP_BUFFER (6)
            _jumpBufferTimer.update();
            if (_inputState.JumpStarted)
            {
                _jumpBufferTimer.reset();
                _jumpBufferTimer.start();
            }

            // - If we're aiming, play aiming sound (?) and update lastAimDirection to AimDirection

            // - Check if we're set to auto-move, and if so, set our input axis x value to our autoMove value

            // - If we're on ground, do some stuff:
            if (_onGround)
            {
                _jumpGraceTimer.reset();
                _jumpGraceTimer.start();
                _wallStickMax = this.WallStickStart;
                //this.flapGravity = 1f;
                _graceLedgeDir = 0;
            }

            // - Otherwise, update our jump grace counter (I believe this is for stored jumps)

            // - If there is a wing flap counter, update it or set it to zero if we have >= 0 y movement

            // - Set gliding to false

            //base.Update(); - this calls updates on the player components, including PlayerState, which results in one of those corresponding methods being called
            _stateMachine.Update();

            // - Check if we should pick up an arrow

            // - Check hat collisions

            // - If on fire and have been on fire for long enough to lose wings, lose them

            // - Update Animations
            
            // - Update hair/other animated accessory 
        }

        public string updateNormal()
        {
            // If we're trying to duck, go to ducking state, unless we are within aim-down grace period

            // Apply speed multiplier
            float multiplier = Mathf.Lerp(this.SlipperyAccelerationMultiplier, 1.0f, _slipperyControl);

            // Turning around
            if ((_aiming && _onGround) || (!_aiming && _slipperyControl == 1.0f && _moveAxis.x != Mathf.Sign(_velocity.x)))
            {
                //NOTE - doesn't account for wings
                float maxMove = (_onGround ? this.Friction : this.AirFriction) * multiplier * Time.deltaTime;
                _velocity.x = _velocity.x <= 0.0f ? Mathf.Min(_velocity.x + maxMove, 0.0f) : Mathf.Max(_velocity.x - maxMove, 0.0f);
            }

            // Normal movement
            if (!_aiming && _moveAxis.x != 0.0f)
            {
                if (_onGround && multiplier == 1.0f)
                {
                    // Add movement particles
                }

                // Deccel if past max speed
                if (Math.Abs(_velocity.x) > this.MaxRunSpeed && (float)Math.Sign(_velocity.x) == _moveAxis.x)
                {
                    float targetSpeed = this.MaxRunSpeed * _moveAxis.x;
                    float deccelAmount = this.RunDecceleration * Time.deltaTime;
                    _velocity.x = _velocity.x <= targetSpeed ? Mathf.Min(_velocity.x + deccelAmount, targetSpeed) : Mathf.Max(_velocity.x - deccelAmount, targetSpeed);
                }

                // Accelerate
                else
                {
                    float acceleration = _onGround ? this.RunAcceleration : this.AirRunAcceleration;
                    acceleration *= multiplier;
                    if (_dodgeCooldown)
                        acceleration *= this.DodgeCooldownMultiplier;

                    acceleration *= Time.deltaTime;
                    float targetSpeed = this.MaxRunSpeed * _moveAxis.x;
                    _velocity.x = _velocity.x <= targetSpeed ? Mathf.Min(_velocity.x + acceleration, targetSpeed) : Mathf.Max(_velocity.x - acceleration, targetSpeed);
                }
            }
            // Jump pad particles

            _cling = 0;

            // If on ground, return wings to normal, otherwise:

            if (!_onGround)
            {
                // Calculate flap gravity

                // If jump button is held down use smaller number for gravity
                float gravity = (_velocity.y <= 1.0f && _inputState.Jump && _canJumpHold) ? (this.JumpHeldGravityMultiplier * this.Gravity) : this.Gravity;

                float targetFallSpeed = this.MaxFallSpeed;
                if (_moveAxis.x != 0.0f && this.CanWallSlide((Facing)_moveAxis.x))
                {
                    this.wings.Normal();
                    target = this.wallStickMax;
                    this.wallStickMax = Calc.Approach(this.wallStickMax, 1.6f, 0.01f * Engine.TimeMult);
                    this.Cling = (int)this.moveAxis.X;
                    if (this.Speed.Y > 0f)
                    {
                        Sounds.char_wallslide[this.CharacterIndex].Play(base.X, 1f);
                    }
                    if (base.Level.OnInterval(3))
                    {
                        base.Level.Particles.Emit(this.DustParticleType, 1, this.Position + new Vector2((float)(3 * this.Cling), 0f), new Vector2(1f, 3f));
                    }
                }
                else
                {
                    if (this.input.MoveY == 1 && this.Speed.Y > 0f)
                    {
                        this.wings.FallFast();
                        target = FAST_FALL;
                        MatchStats[] expr_5CB_cp_0 = base.Level.Session.MatchStats;
                        int expr_5CB_cp_1 = this.PlayerIndex;
                        expr_5CB_cp_0[expr_5CB_cp_1].FastFallFrames = expr_5CB_cp_0[expr_5CB_cp_1].FastFallFrames + Engine.TimeMult;
                    }
                    else
                    {
                        if (this.input.JumpCheck && this.HasWings && this.Speed.Y >= -1f)
                        {
                            this.wings.Glide();
                            this.gliding = true;
                            target = 0.8f;
                        }
                        else
                        {
                            this.wings.Normal();
                        }
                    }
                }
                if (this.Cling == 0 || this.Speed.Y <= 0f)
                {
                    Sounds.char_wallslide[this.CharacterIndex].Stop(true);
                }
                this.Speed.Y = Calc.Approach(this.Speed.Y, target, num3 * Engine.TimeMult);
            }
            if (!this.dodgeCooldown && this.input.DodgePressed && !base.Level.Session.MatchSettings.Variants.NoDodging[this.PlayerIndex])
            {
                if (this.moveAxis.X != 0f)
                {
                    this.Facing = (Facing)this.moveAxis.X;
                }
                return 3;
            }
            if (this.onHotCoals)
            {
                this.HotCoalsBounce();
            }
            else
            {
                if (this.input.JumpPressed || this.jumpBufferCounter)
                {
                    if (this.jumpGraceCounter)
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
        private Vector2 _moveAxis;
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
            return !_aiming && _inputState.MoveY != -Math.Sign(Vector2.up.y) && canWallJump(direction);
        }

        private bool canWallJump(Facing direction)
        {
            //TODO - is LedgeCheckVertical the right thing to use here?
            // Make sure we are far enough off the ground
            if (this.boxCollider2D.CollideFirst(0.0f, -Vector2.up.y * (this.LedgeCheckVertical / 2)))
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
