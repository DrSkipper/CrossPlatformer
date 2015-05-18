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
    class CPPlayer : VoBehavior
    {
        public enum Facing
        {
            Right = 1,
            Left = -1
        }

        public float Gravity = 0.3f;
        public float MaxFallSpeed = 2.8f;
        public float FastFallSpeed = 3.5f;
        public float LandingHorizontalMultiplier = 0.6f;
        public float Friction = 0.2f;
        public float AirFriction = 0.14f;
        public float MaxRunSpeed = 1.5f;
        public float RunAcceleration = 0.15f;
        public float RunDecceleration = 0.03f;
        public float AirRunAcceleration = 0.1f;

        [HideInInspector] public float MULT_Gravity = 1.0f;
        [HideInInspector] public float MULT_MaxFallSpeed = 1.0f;
        [HideInInspector] public float MULT_FastFallSpeed = 1.0f;
        [HideInInspector] public float MULT_LandingHorizontalMultiplier = 1.0f;
        [HideInInspector] public float MULT_Friction = 1.0f;
        [HideInInspector] public float MULT_AirFriction = 1.0f;
        [HideInInspector] public float MULT_MaxRunSpeed = 1.0f;
        [HideInInspector] public float MULT_RunAcceleration = 1.0f;
        [HideInInspector] public float MULT_RunDecceleration = 1.0f;
        [HideInInspector] public float MULT_AirRunAcceleration = 1.0f;

        [HideInInspector] public bool CanFastFall = true;

        public float CalcGravity            { get { return this.Gravity * this.MULT_Gravity; } }
        public float CalcMaxFallSpeed       { get { return this.MaxFallSpeed * this.MULT_MaxFallSpeed; } }
        public float CalcFastFallSpeed      { get { return this.FastFallSpeed * this.MULT_FastFallSpeed; } }
        public float CalcLandingHorizontalMultiplier { get { return this.LandingHorizontalMultiplier * this.MULT_LandingHorizontalMultiplier; } }
        public float CalcFriction           { get { return this.Friction * this.MULT_Friction; } }
        public float CalcAirFriction        { get { return this.AirFriction * this.MULT_AirFriction; } }
        public float CalcMaxRunSpeed        { get { return this.MaxRunSpeed * this.MULT_MaxRunSpeed; } }
        public float CalcRunAcceleration    { get { return this.RunAcceleration * this.MULT_RunAcceleration; } }
        public float CalcRunDecceleration   { get { return this.RunDecceleration * this.MULT_RunDecceleration; } }
        public float CalcAirRunAcceleration { get { return this.AirRunAcceleration * this.MULT_AirRunAcceleration; } }

        public const string PLAYER_STATE_NORMAL = "normal";

        public TFActor actor
        {
            get
            {
                if (!_actor)
                    _actor = this.GetComponent<TFActor>() as TFActor;
                return _actor;
            }
        }

        public InputState inputState { get { return _inputState; } }
        public DirectionalVector2 moveAxis { get { return _moveAxis; } }
        public Facing facing { get { return _facing; } set { _facing = value; } }
        public Vector2 velocity { get { return _velocity; } }
        public bool onGround { get { return _onGround; } }
        public GameObject lastPlatform { get { return _lastPlatform; } set { _lastPlatform = value; } }
        public FSMStateMachine stateMachine { get { return _stateMachine; } }

        public void SetVelocityX(float vx) { _velocity.x = vx; }
        public void SetVelocityY(float vy) { _velocity.y = vy; }

        public void SetAutoMove(float duration, int direction)
        {
            _autoMove = direction;
            if (_autoMoveTimer != null)
                _autoMoveTimer.complete();
            _autoMoveTimer = new Timer(duration, false, true, this.finishAutoMove);
        }

        public void Awake()
        {
            this.ReloadAbilities();
            _stateMachine.AddState(PLAYER_STATE_NORMAL, this.updateNormal, null, null);
            _stateMachine.BeginWithInitialState(PLAYER_STATE_NORMAL);
        }

        public void Update()
        {
            this.resetProperties();

            // Send collision ray(s) from our position in UnitY direction (down) to detect OnGround
            GameObject groundObject = this.boxCollider2D.CollideFirst(0, TFPhysics.DownY, this.actor.CollisionMask, this.actor.CollisionTag);
            _onGround = groundObject != null;

            if (_onGround)
                _lastPlatform = groundObject;

            // Get input state for this player
            _inputState = InputState.GetInputStateForPlayer(0);
            _moveAxis = new DirectionalVector2(_inputState.MoveX, _inputState.MoveY);

            // Check if we're set to auto-move, and if so, set our input axis x value to our autoMove value
            if (_autoMoveTimer != null)
                _autoMoveTimer.update(TFPhysics.DeltaFrames);
            if (_autoMove != 0)
                _moveAxis.X = _autoMove;

            this.ApplyAbilityPropertyModifiers();
            _stateMachine.Update();
        }

        public void ReloadAbilities()
        {
            _abilities = this.gameObject.GetComponents<CPPlayerAbility>().ToList<CPPlayerAbility>();
            _abilities.OrderBy(ability => ability.Priority);
        }

        public void ApplyAbilityPropertyModifiers()
        {
            _abilities.ForEach(element => element.ApplyPropertyModifiers());
        }

        public string UpdateAbilities()
        {
            string prevState = _stateMachine.CurrentState;
            foreach (CPPlayerAbility ability in _abilities)
            {
                string state = ability.UpdateAbility();
                if (state != null && state != prevState)
                    return state;
            }
            return prevState;
        }

        public string PostUpdateAbilities()
        {
            string prevState = _stateMachine.CurrentState;
            foreach (CPPlayerAbility ability in _abilities)
            {
                string state = ability.PostUpdateAbility();
                if (state != null && state != prevState)
                    return state;
            }
            return prevState;
        }

        private string updateNormal()
        {
            // Turning around
            if (_moveAxis.X != Math.Sign(_velocity.x))
            {
                float maxMove = _onGround ? this.CalcFriction : this.CalcAirFriction;
                _velocity.x = _velocity.x.Approach(0.0f, maxMove * TFPhysics.DeltaFrames);
            }

            // Normal movement
            if (_moveAxis.X != 0)
            {
                // Deccel if past max speed
                if (Mathf.Abs(_velocity.x) > this.CalcMaxRunSpeed && Math.Sign(_velocity.x) == _moveAxis.X)
                {
                    _velocity.x = _velocity.x.Approach(this.CalcMaxRunSpeed * _moveAxis.floatX, this.CalcRunDecceleration * TFPhysics.DeltaFrames);
                }

                // Accelerate
                else
                {
                    float acceleration = _onGround ? this.CalcRunAcceleration : this.CalcAirRunAcceleration;
                    _velocity.x = _velocity.x.Approach(this.CalcMaxRunSpeed * _moveAxis.floatX, acceleration *= TFPhysics.DeltaFrames);
                }
            }

            if (!_onGround)
            {
                float targetFallSpeed = this.CalcMaxFallSpeed;

                // Check if we need to fast fall
                if (this.CanFastFall && _inputState.MoveY == TFPhysics.DownY && Math.Sign(_velocity.y) == TFPhysics.DownY)
                    targetFallSpeed = this.CalcFastFallSpeed;

                _velocity.y = _velocity.y.Approach(TFPhysics.DownY * targetFallSpeed, TFPhysics.DownY * this.CalcGravity * TFPhysics.DeltaFrames);
            }

            string state = this.UpdateAbilities();
            if (state != PLAYER_STATE_NORMAL)
                return state;

            if (_moveAxis.X != 0)
                _facing = (Facing)_moveAxis.X;

            this.actor.MoveH(_velocity.x * TFPhysics.DeltaFrames, this.onCollideH);
            this.actor.MoveV(_velocity.y * TFPhysics.DeltaFrames, this.onCollideV);

            state = this.PostUpdateAbilities();
            if (state != PLAYER_STATE_NORMAL)
                return state;
            return PLAYER_STATE_NORMAL;
        }

        /**
         * Private
         */
        private TFActor _actor;
        private InputState _inputState;
        private DirectionalVector2 _moveAxis;
        private bool _onGround;
        private GameObject _lastPlatform;
        private Facing _facing = Facing.Right;
        private Vector2 _velocity;
        private int _autoMove;
        private Timer _autoMoveTimer;
        private List<CPPlayerAbility> _abilities;
        private FSMStateMachine _stateMachine = new FSMStateMachine();

        private void onCollideH(GameObject solid)
        {
            _velocity.x = 0.0f;
        }

        private void onCollideV(GameObject solid)
        {
            if (Math.Sign(_velocity.y) == TFPhysics.DownY)
                _velocity.x = Mathf.Lerp(_velocity.x, 0.0f, this.CalcLandingHorizontalMultiplier * (_velocity.y / this.CalcMaxFallSpeed));
            _velocity.y = 0.0f;
        }

        private void resetProperties()
        {
            this.MULT_Gravity = 1.0f;
            this.MULT_MaxFallSpeed = 1.0f;
            this.MULT_FastFallSpeed = 1.0f;
            this.MULT_LandingHorizontalMultiplier = 1.0f;
            this.MULT_Friction = 1.0f;
            this.MULT_AirFriction = 1.0f;
            this.MULT_MaxRunSpeed = 1.0f;
            this.MULT_RunAcceleration = 1.0f;
            this.MULT_RunDecceleration = 1.0f;
            this.MULT_AirRunAcceleration = 1.0f;

            _abilities.ForEach(element => element.ResetProperties());
        }

        private void finishAutoMove()
        {
            _autoMove = 0;
            _autoMoveTimer = null;
        }
    }
}
