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
    class TFPlayer : VoBehavior
    {
        private enum Facing
        {
            Right = 1,
            Left = -1
        }

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
        public float JumpPower = 3.2f;
        public float BoostedJumpPower = 4.3f;
        public float JumpHorizontalBoost = 0.5f;
        public float JumpHeldGravityMultiplier = 0.5f;
        public int JumpBufferFrames = 6;
        public int JumpGraceFrames = 6;
        public float LandingHorizontalMultiplier = 0.6f;
        public float WallStickStart = 0.5f;
        public int WallJumpCheck = 2;
        public int WallJumpTime = 12;
        public float WallStickMaxFall = 1.6f;
        public float WallStickAdd = 0.01f;
        public float SlipperyAccelerationMultiplier = 0.35f;
		public float SlipperyReturnRate = 0.1f;
        public float Friction = 0.2f;
        public float AirFriction = 0.14f;
        public float MaxRunSpeed = 1.5f;
        public float RunAcceleration = 0.15f;
        public float RunDecceleration = 0.03f;
        public float AirRunAcceleration = 0.1f;
        public int LedgeGrabOffset = 2;
        public int LedgeCheckHorizontal = 2;
        public int LedgeCheckVertical = 10;
        public int LedgeReleaseGraceFrames = 12;
        public int DodgeFrames = 20;
        public int DodgeCooldownFrames = 25;
        public int DodgeJumpGraceFrames = 12;
        public int DodgeWalljumpValidFrames = 12;
        public int DodgeSlideJumpAutomoveFrames = 16;
        public float DodgeCooldownAccelMultiplier = 0.8f;
        public float DodgeSpeed = 5.5f;
        public float DodgeSlideSpeedMultiplier = 1.4f;
        public float DodgeJumpMaxSpeed = 1.8f;
        public float DodgeDeccelMultiplier = 0.85f;
        public float DodgeSlideJumpMinSpeedReq = 1.75f;
        public float DodgeSlideJumpSpeed = 2.8f;

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

            _jumpBufferTimer = new Timer(this.JumpBufferFrames);
            _jumpBufferTimer.complete();

            _jumpGraceTimer = new Timer(this.JumpGraceFrames);
            _jumpGraceTimer.complete();

            _dodgeEndTimer = new Timer(this.DodgeFrames);
            _dodgeEndTimer.complete();

            _slipperyControl = 1.0f;
        }

        public void Update()
        {
            // - Update spam shot counter

            // Send collision ray(s) from our position in UnitY direction (down) to detect OnGround
            // If so, store hit entity as lastPlatform. Check if this platform is slippery or hot coals and store bools for those as well.
            GameObject groundObject = this.boxCollider2D.CollideFirst(0, TFPhysics.DownY, this.actor.CollisionMask, this.actor.CollisionTag);
            _onGround = groundObject != null;

            if (_onGround)
            {
                _slipperyControl = (this.SlipperyTag != null &&
                    (this.SlipperyTag == this.actor.CollisionTag ||
                    this.boxCollider2D.CollideFirst(0, TFPhysics.DownY, this.actor.CollisionMask, this.SlipperyTag))) ? 0.0f : 1.0f;

                _lastPlatform = groundObject;
            }
            else
            {
                //TODO - Make this time based rather than update frame?
                _slipperyControl = Mathf.Min(_slipperyControl + this.SlipperyReturnRate, 1.0f);
            }

            // Get input state for this player
            _inputState = InputState.GetInputStateForPlayer(0);
            _moveAxis = new DirectionalVector2(_inputState.MoveX, _inputState.MoveY);
            float? aimDirection = getAimDirection(_inputState.AimAxis);
            _aimDirection = aimDirection.HasValue ? aimDirection.GetValueOrDefault() : (_facing == Facing.Right ? 0.0f : Mathf.PI);

            // Update jumpBufferCounter, and if input indicates Jump is pressed, set it to JUMP_BUFFER (6)
            _jumpBufferTimer.update(TFPhysics.DeltaFrames);
            if (_inputState.JumpStarted)
            {
                _jumpBufferTimer.reset();
                _jumpBufferTimer.start();
            }

            // - If we're aiming, play aiming sound (?) and update lastAimDirection to AimDirection

            // Check if we're set to auto-move, and if so, set our input axis x value to our autoMove value
            if (_autoMoveTimer != null)
                _autoMoveTimer.update(TFPhysics.DeltaFrames);
            if (_autoMove != 0)
                _moveAxis.X = _autoMove;

            if (_dodgeCooldownTimer != null)
                _dodgeCooldownTimer.update(TFPhysics.DeltaFrames);

            // If we're on ground, do some stuff:
            if (_onGround)
            {
                if (_jumpGraceTimer.completed || _jumpGraceTimer.timeRemaining < this.JumpGraceFrames)
                    _jumpGraceTimer.reset(this.JumpGraceFrames);
                if (_jumpGraceTimer.paused)
                    _jumpGraceTimer.start();
                _wallStickMax = this.WallStickStart;
                _graceLedgeDir = 0;
            }

            // Otherwise, update our jump grace counter (I believe this is for stored jumps)
            else
            {
                _jumpGraceTimer.update(TFPhysics.DeltaFrames);
            }

            // Call the update method for our current state
            _stateMachine.Update();

            // - Check if we should pick up an arrow
        }

        public string updateNormal()
        {
            // If we're trying to duck, go to ducking state, unless we are within aim-down grace period

            // Calculate slippery surface modifier
            float multiplier = Mathf.Lerp(this.SlipperyAccelerationMultiplier, 1.0f, _slipperyControl);

            // Turning around
            if ((_aiming && _onGround) || (!_aiming && _slipperyControl == 1.0f && _moveAxis.X != Math.Sign(_velocity.x)))
            {
                float maxMove = (_onGround ? this.Friction : this.AirFriction) * multiplier;
                _velocity.x = _velocity.x.Approach(0.0f, maxMove * TFPhysics.DeltaFrames);
            }

            // Normal movement
            if (!_aiming && _moveAxis.X != 0)
            {
                // Deccel if past max speed
                if (Mathf.Abs(_velocity.x) > this.MaxRunSpeed && Math.Sign(_velocity.x) == _moveAxis.X)
                {
                    _velocity.x = _velocity.x.Approach(this.MaxRunSpeed * _moveAxis.floatX, this.RunDecceleration * TFPhysics.DeltaFrames);
                }

                // Accelerate
                else
                {
                    float acceleration = _onGround ? this.RunAcceleration : this.AirRunAcceleration;
                    acceleration *= multiplier;
                    if (_dodgeCooldown)
                        acceleration *= this.DodgeCooldownAccelMultiplier;

                    _velocity.x = _velocity.x.Approach(this.MaxRunSpeed * _moveAxis.floatX, acceleration *= TFPhysics.DeltaFrames);
                }
            }

            _cling = 0;

            if (!_onGround)
            {
                // If jump button is held down use smaller number for gravity
                //TODO - Figure out where the 1.0f comes from
                float gravity = (_inputState.Jump && _canJumpHold && (Math.Sign(_velocity.y) == TFPhysics.UpY || Mathf.Abs(_velocity.y) < 1.0f)) ? (this.JumpHeldGravityMultiplier * this.Gravity) : this.Gravity;
                float targetFallSpeed = this.MaxFallSpeed;
                
                // Check if we're wall sliding
                if (_moveAxis.X != 0 && canWallSlide((Facing)_moveAxis.X))
                {
                    targetFallSpeed = _wallStickMax;
                    _wallStickMax = _wallStickMax.Approach(this.WallStickMaxFall, this.WallStickAdd * TFPhysics.DeltaFrames);
                    _cling = _moveAxis.X;
                }
                else
                {
                    // Check if we need to fast fall
                    if (_inputState.MoveY == TFPhysics.DownY && Math.Sign(_velocity.y) == TFPhysics.DownY)
                        targetFallSpeed = this.FastFallSpeed;
                }

                _velocity.y = _velocity.y.Approach(TFPhysics.DownY * targetFallSpeed, TFPhysics.DownY * gravity * TFPhysics.DeltaFrames);
            }

            // Check if we need to dodge
            if (!_dodgeCooldown && _inputState.DodgeStarted)
            {
                if (_moveAxis.X != 0)
                    _facing = (Facing)_moveAxis.X;
                return PLAYER_STATE_DODGING;
            }

            // Check if it's time to jump
            if (_inputState.JumpStarted || !_jumpBufferTimer.completed)
            {
                if (!_jumpGraceTimer.completed)
                {
                    // If we're trying to jump from ledge grab, get the direction we moved away from the ledge
                    int ledgeDir = _graceLedgeDir;
                    if (_moveAxis.X != ledgeDir)
                        ledgeDir = 0;
                    jump(true, true, false, ledgeDir);
                }
                else
                {
                    if (canWallJump(Facing.Left))
                        wallJump((int)Facing.Right);
                    else if (canWallJump(Facing.Right))
                        wallJump((int)Facing.Left);
                }
            }

            if (_aiming)
            {
                // If we're aiming but no longer holding aim button, means shoot button was released and time to shoot
                if (!_inputState.Shoot)
                    shoot();
            }
            else if (_inputState.ShootStarted)
            {
                _aiming = true;
            }

            if (_moveAxis.X != 0)
                _facing = (Facing)_moveAxis.X;

            this.actor.MoveH(_velocity.x * TFPhysics.DeltaFrames, this.onCollideH);
            this.actor.MoveV(_velocity.y * TFPhysics.DeltaFrames, this.onCollideV);

            // Check for ledge grab
            if (!_onGround && !_aiming)
            {
                int velocityDirY = Math.Sign(_velocity.y);
                bool notGoingUp = velocityDirY == TFPhysics.DownY || velocityDirY == 0;
                bool notHoldingDown = _moveAxis.Y == TFPhysics.UpY || _moveAxis.Y == 0;

                if (notGoingUp && _moveAxis.X != 0 && notHoldingDown && this.boxCollider2D.CollideFirst(_moveAxis.X * this.LedgeCheckHorizontal, 0, this.actor.CollisionMask, this.actor.CollisionTag))
                {
                    int direction = _moveAxis.X;
                    for (int i = 0; i < this.LedgeCheckVertical; ++i)
                    {
                        int offsetY = (int)this.position2D.y + TFPhysics.UpY * i;
                        if (canGrabLedge(offsetY, direction))
                            return grabLedge(offsetY, direction);
                    }
                }
            }

            return PLAYER_STATE_NORMAL;
        }

        public string updateDodging()
        {
            _dodgeEndTimer.update(TFPhysics.DeltaFrames);

            this.actor.Move(_velocity * TFPhysics.DeltaFrames, this.onDodgeCollideH, this.onDodgeCollideV);
            _velocity *= Mathf.Pow(this.DodgeDeccelMultiplier, TFPhysics.DeltaFrames);

            if ((_inputState.JumpStarted || !_jumpBufferTimer.completed) && (!_dodgeSliding || canStand()))
            {
                if (canDodgeWallJump(Facing.Left))
                {
                    if (!dodgeWallJump((int)Facing.Left))
                        return PLAYER_STATE_NORMAL;
                }
                else
                {
                    if (canDodgeWallJump(Facing.Right))
                    {
                        if (!dodgeWallJump((int)Facing.Right))
                            return PLAYER_STATE_NORMAL;
                    }
                    else
                    {
                        if (!_jumpGraceTimer.completed)
                        {
                            if (_dodgeSliding)
                            {
                                //this.UseNormalHitbox();
                                _dodgeSliding = false;
                                _autoMove = Math.Sign(_velocity.x);
                                _autoMoveTimer = new Timer(this.DodgeSlideJumpAutomoveFrames, false, true, finishAutoMove);

                                if (Mathf.Abs(_velocity.x) >= this.DodgeSlideJumpMinSpeedReq)
                                {
                                    _velocity.x = Mathf.Sign(_velocity.x) * this.DodgeSlideJumpSpeed;
                                }
                                jump(true, true, false, 0);
                                return PLAYER_STATE_NORMAL;
                            }
                            _velocity.x = Mathf.Clamp(_velocity.x, -this.DodgeJumpMaxSpeed, this.DodgeJumpMaxSpeed);
                            jump(true, true, false, 0);
                            return PLAYER_STATE_NORMAL;
                        }
                    }
                }
            }

            if (_dodgeSliding && _jumpGraceTimer.completed && canStand())
            {
                //this.UseNormalHitbox();
                _dodgeSliding = false;
            }

            if (_inputState.DodgeStarted)
                return getDodgeExitState();

            if (_dodgeEndTimer != null && !_dodgeEndTimer.completed)
            {
                _dodgeEndTimer.update(TFPhysics.DeltaFrames);
                return PLAYER_STATE_DODGING;
            }

            return getDodgeExitState();
        }

        private string getDodgeExitState()
        {
            if (_dodgeSliding && (inputDucking || !canStand()))
            {
                _dodgeSliding = false;
                return PLAYER_STATE_DUCKING;
            }
            return PLAYER_STATE_NORMAL;
        }

        public string updateDucking()
        {
            return PLAYER_STATE_NORMAL;
        }

        public string updateLedgeGrab()
        {
			if (!_dodgeCooldown && _inputState.DodgeStarted)
				return PLAYER_STATE_DODGING;

			if (_inputState.ShootStarted)
			{
				_aiming = true;
				return PLAYER_STATE_NORMAL;
			}

			if (_moveAxis.Y == TFPhysics.DownY || _moveAxis.X != (int)_facing)
			{
				_graceLedgeDir = -(int)_facing;
                _jumpGraceTimer.reset(this.LedgeReleaseGraceFrames);
                _jumpGraceTimer.start();
				return PLAYER_STATE_NORMAL;
			}

			if (_inputState.JumpStarted)
			{
				if (_moveAxis.X == -(int)_facing)
					jump(true, false, false, -(int)_facing);
                else if (_moveAxis.Y != TFPhysics.DownY)
					jump(false, false, false, 0);
                return PLAYER_STATE_NORMAL;
			}

            if (!canGrabLedge((int)this.position2D.y + TFPhysics.UpY * this.LedgeGrabOffset, (int)_facing))
                return PLAYER_STATE_NORMAL;

			return PLAYER_STATE_LEDGE_GRAB;
        }

        public string updateDying()
        {
            return PLAYER_STATE_NORMAL;
        }

        public void enterDodge()
        {
            if (!_dodgeSliding && _onGround && Math.Sign(_inputState.AimAxis.y) == TFPhysics.DownY)
            {
                _dodgeSliding = true;
                //this.UseDuckingHitbox();
            }

            _dodgeEndTimer.reset();
            _dodgeEndTimer.start();

            if (_onGround)
            {
                _jumpGraceTimer.reset(this.DodgeJumpGraceFrames);
                _jumpBufferTimer.start();
            }

            if (_inputState.AimAxis == Vector2.zero || _stateMachine.PreviousState == PLAYER_STATE_DUCKING)
            {
                _velocity.x = (float)_facing * this.DodgeSpeed;
                _velocity.y = 0.0f;
            }
            else
            {
                _velocity = _inputState.AimAxis.EightWayNormal() * this.DodgeSpeed;
            }

            if (_dodgeSliding)
                _velocity *= this.DodgeSlideSpeedMultiplier;

            _canJumpHold = false;
            _dodgeCooldown = true;
            _aiming = false;
        }

        public void exitDodge()
        {
            //if (_dodgeSliding && this.CanUnduck())
            //    this.UseNormalHitbox();

            _dodgeSliding = false;
            _dodgeEndTimer.complete();
            _dodgeCooldownTimer = new Timer(this.DodgeFrames, false, true, finishDodge);
        }

        public void enterDuck()
        {

        }

        public void exitDuck()
        {

        }

        public void enterLedgeGrab()
        {
            _aiming = false;
            _velocity = Vector2.zero;
            _lastPlatform = null;
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

        //public override bool IsRiding(GameObject solid)
        //{
        //    return base.IsRiding(solid) || (_state == PLAYER_STATE_NORMAL && _cling != 0 && base.CollideCheck(solid, base.X + (float)this.Cling, base.Y)) || (this.State == Player.PlayerStates.LedgeGrab && base.CollideCheck(solid, base.X + (float)this.Facing, base.Y));
        //}

        /**
         * Private
         */
        private TFActor _actor;
        private FSMStateMachine _stateMachine;
        private InputState _inputState;
        private DirectionalVector2 _moveAxis;
        private bool _onGround;
        private float _slipperyControl;
        private GameObject _lastPlatform;
        private float _aimDirection;
        private Facing _facing = Facing.Right;
        private Timer _jumpBufferTimer;
        private Timer _jumpGraceTimer;
        private Timer _autoMoveTimer;
        private Timer _dodgeEndTimer;
        private Timer _dodgeCooldownTimer;
        private float _wallStickMax;
        private int _graceLedgeDir;
        private bool _aiming;
        private Vector2 _velocity;
        private bool _dodgeCooldown;
        private int _cling;
        private bool _canJumpHold;
        private int _autoMove;
        private bool _dodgeSliding;

        private bool inputDucking { get { return _onGround && _moveAxis.X == 0 && _moveAxis.Y == TFPhysics.DownY; } }

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

        private bool canStand()
        {
            //TODO
            /*base.Collider = this.normalHitbox;
            bool result = !base.CollideCheck(GameTags.Solid);
            base.Collider = this.duckingHitbox;*/
            return true;
        }

        private bool canWallSlide(Facing direction)
        {
            return !_aiming && _inputState.MoveY != TFPhysics.DownY && canWallJump(direction);
        }

        private bool canWallJump(Facing direction)
        {
            //TODO - is LedgeCheckVertical the right thing to use here?
            // Make sure we are far enough off the ground
            if (this.boxCollider2D.CollideFirst(0, TFPhysics.DownY * (this.LedgeCheckVertical / 2)))
                return false;

            //TODO - is the -1.0f necessary?
            // We can only wall jump if the top of our body is next to the wall
            Vector2 wallJumpCollidePoint = direction == Facing.Right ?
                new Vector2(_actor.RightX - 1.0f + this.WallJumpCheck, _actor.TopY) :
                new Vector2(_actor.LeftX - this.WallJumpCheck, _actor.TopY);
            return _actor.CollidePoint(wallJumpCollidePoint);
        }

        private bool canDodgeWallJump(Facing direction)
        {
            return direction == Facing.Right ? this.actor.CollidePoint(new Vector2(this.actor.RightX - 1f + this.WallJumpCheck, this.actor.TopY)) :
                                               this.actor.CollidePoint(new Vector2(this.actor.LeftX - this.WallJumpCheck, this.actor.TopY));
        }

        private void jump(bool particles, bool canSuper, bool forceSuper, int ledgeDir)
        {
            _jumpBufferTimer.complete();
            _jumpGraceTimer.complete();

            if (_autoMoveTimer != null)
                _autoMoveTimer.complete();

            if (forceSuper)
            {
                _velocity.y = TFPhysics.UpY * this.BoostedJumpPower;
            }
            else
            {
                //TODO - See if we were recently on or are on a jumpad
                /* GameObject jumpPad = null;
                if (canSuper)
                {
                    if (this.lastPlatform is JumpPad)
                        jumpPad = (this.lastPlatform as JumpPad);
                    else
                        jumpPad = (base.CollideFirst(GameTags.JumpPad, this.Position + Vector2.UnitY) as JumpPad);
                }
                if (jumpPad)
                {
                    this.Speed.Y = JUMP_ONPAD;
                    jumpPad.Launch(base.X);
                }
                else */
                _velocity.y = TFPhysics.UpY * this.JumpPower;
            }

            if (ledgeDir != 0)
            {
                _facing = (Facing)ledgeDir;
                _velocity.x = (float)ledgeDir * this.MaxRunSpeed;
                _autoMove = ledgeDir;

                //TODO - Is JumpGraceTime the correct thing to use here?
                _autoMoveTimer = new Timer(this.JumpGraceFrames, false, true, this.finishAutoMove);
            }

            if (_moveAxis.X != 0 && !_aiming)
                _velocity.x += this.JumpHorizontalBoost * _moveAxis.X;

            _canJumpHold = true;
        }

		private void wallJump(int dir)
        {
            _jumpBufferTimer.complete();
            _jumpGraceTimer.complete();
            
            if (_autoMoveTimer != null)
                _autoMoveTimer.complete();

            _velocity.y = TFPhysics.UpY * this.JumpPower;
            _velocity.x = (float)dir * 2f; //TODO - Where does the 2.0 come  from?
            _canJumpHold = true;
            _wallStickMax = this.WallStickStart;
            _facing = (Facing)dir;
            _autoMove = dir;
            _autoMoveTimer = new Timer(this.WallJumpTime, false, true, this.finishAutoMove);
        }

        private bool dodgeWallJump(int dir)
        {
            if (_dodgeEndTimer.timeRemaining < this.DodgeWalljumpValidFrames)
            {
                wallJump(dir);
                return false;
            }

            _jumpBufferTimer.complete();
            _jumpGraceTimer.complete();

            _velocity = MathExtensions.AngleToVector(-MathExtensions.PI_4 * (float)dir + (float)(dir + 1) * MathExtensions.PI_2, this.DodgeSpeed);
            _dodgeEndTimer.reset(this.DodgeFrames);
            _wallStickMax = this.WallStickStart;
            _facing = (Facing)dir;
            _canJumpHold = true;
            return true;
        }

        private void shoot()
        {
            _aiming = false;
            //TODO
        }

        private bool canGrabLedge(int targetY, int direction)
        {
            int targetYDistance = Mathf.RoundToInt(targetY - this.position2D.y);

            // Make sure place we'll grab into is empty
            if (this.boxCollider2D.CollideFirst(0, targetYDistance + TFPhysics.DownY * this.LedgeGrabOffset, this.actor.CollisionMask, this.actor.CollisionTag))
                return false;

            // Make sure we're not close to ground
            //TODO - Where does the 5.0 come from?
            if (this.boxCollider2D.CollideFirst(0, 5, this.actor.CollisionMask, this.actor.CollisionTag))
                return false;

            float xCheck = direction == -1 ? this.actor.LeftX - this.LedgeCheckHorizontal : this.actor.RightX + this.LedgeCheckHorizontal;

            // Make sure this is actually a ledge by checking that the space above the target location is empty
            if (this.actor.CollidePoint(new Vector2(xCheck, targetY + TFPhysics.UpY)))
                return false;

            // Make sure there is an object here to grab onto
            return this.actor.CollidePoint(new Vector2(xCheck, targetY));
        }
        private string grabLedge(int targetY, int direction)
        {
            _facing = (Facing)direction;
            _velocity.y = 0.0f;

            Vector3 oldPosition = this.transform.position;
            this.transform.position = new Vector3(Mathf.Round(oldPosition.x), targetY + TFPhysics.DownY * this.LedgeGrabOffset, oldPosition.z);

            while (!this.boxCollider2D.CollideFirst(direction, 0, this.actor.CollisionMask, this.actor.CollisionTag))
            {
                oldPosition = this.transform.position;
                this.transform.position = new Vector3(oldPosition.x + direction, oldPosition.y, oldPosition.z);
            }

            return PLAYER_STATE_LEDGE_GRAB;
        }

        private void onCollideH(GameObject solid)
        {
            _velocity.x = 0.0f;
        }

        private void onCollideV(GameObject solid)
        {
            if (Math.Sign(_velocity.y) == TFPhysics.DownY)
                _velocity.x = Mathf.Lerp(_velocity.x, 0.0f, this.LandingHorizontalMultiplier * (_velocity.y / this.MaxFallSpeed));
            _velocity.y = 0.0f;
        }

        private void onDodgeCollideH(GameObject solid)
        {
            // Transition from dodging to sliding if there is room to slide
            if (_onGround && !_dodgeSliding)
            {
                //this.UseDuckingHitbox();
                if (!this.boxCollider2D.CollideFirst(Math.Sign(_velocity.x), 0, this.actor.CollisionMask, this.actor.CollisionTag))
                {
                    Vector3 oldPosition = this.transform.position;
                    this.transform.position = new Vector3(oldPosition.x + Mathf.Sign(_velocity.x), oldPosition.y, oldPosition.z);
                    _velocity *= this.DodgeSlideSpeedMultiplier;
                    _dodgeSliding = true;
                    return;
                }
                //this.UseNormalHitbox();
            }

            //TODO - Where do these friggin' numbers come from
            // If there is space slightly above of below the collision, try to move around it
            if (!this.boxCollider2D.CollideFirst(Math.Sign(_velocity.x), -5, this.actor.CollisionMask, this.actor.CollisionTag))
                this.actor.MoveV(-2f * TFPhysics.DeltaFrames, null);
            else if (!this.boxCollider2D.CollideFirst(Math.Sign(_velocity.x), 5, this.actor.CollisionMask, this.actor.CollisionTag))
                this.actor.MoveV(2f * TFPhysics.DeltaFrames, null);

            // Otherwise, if we were dodging in a vertical direction at all, convert entirely to vertical
            else if (Mathf.Abs(_velocity.y) > 0.02f)
            {
                _velocity.y = _velocity.magnitude * Mathf.Sign(_velocity.y);
                _velocity.x = 0.0f;
            }
        }

        private void onDodgeCollideV(GameObject solid)
        {
            //TODO - Where do these friggin' numbers come from
            if (Mathf.Abs(_velocity.x) < 0.02f)
            {
                //TODO - Why check at 5 *and* 3?
                // If we're moving completely vertically, see if we can do a slight sidestep to avoid the obstacle
                int yDir = Math.Sign(_velocity.y);
                if (!this.boxCollider2D.CollideFirst(-5, yDir, this.actor.CollisionMask, this.actor.CollisionTag) ||
                    !this.boxCollider2D.CollideFirst(-3, yDir, this.actor.CollisionMask, this.actor.CollisionTag))
                    this.actor.MoveH(-2f * TFPhysics.DeltaFrames, null);
                else if (!this.boxCollider2D.CollideFirst(5, yDir, this.actor.CollisionMask, this.actor.CollisionTag) ||
                         !this.boxCollider2D.CollideFirst(3, yDir, this.actor.CollisionMask, this.actor.CollisionTag))
                    this.actor.MoveH(2f * TFPhysics.DeltaFrames, null);
            }
            else
            {
                // If we have some horizontal movement, transition to completely horizontal dodge
                _velocity.x = _velocity.magnitude * Mathf.Sign(_velocity.x);
                _velocity.y = 0f;
            }
        }

        private void finishAutoMove()
        {
            _autoMove = 0;
            _autoMoveTimer = null;
        }

        private void finishDodge()
        {
            _dodgeCooldown = false;
            _dodgeCooldownTimer = null;
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
