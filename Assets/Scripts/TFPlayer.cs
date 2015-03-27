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
        public const uint MAX_AIM_SNAP_DIRECTIONS = 32;
        public const uint DEFAULT_AIM_SNAP_DIRECTIONS = 8;

        public string SlipperyTag = null;
        public float JumpBufferTime = 6.0f;
        public float JumpGraceTime = 6.0f;
        public float WallStickStart = 0.5f;

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
                _slipperyControl = Mathf.Min(_slipperyControl + 0.1f, 1.0f);
            }

            // - Check if on hot coals

            // - Get input state for this player
            // - Get aimDirection (circular) from joystick axis
            _moveAxis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _aimAxis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            float? aimDirection = getAimDirection(_aimAxis);
            _aimDirection = aimDirection.HasValue ? aimDirection.GetValueOrDefault() : (_facing == Facing.Right ? 0.0f : Mathf.PI);

            // - If we're frozen, just set Facing to appropriate direction and exit Update function

            // - Update jumpBufferCounter, and if input indicates Jump is pressed, set it to JUMP_BUFFER (6)
            _jumpBufferTimer.update();
            if (Input.GetButtonDown("Jump"))
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
    //TODO -  UPDATE STATE MACHINE

            // - Check if we should pick up an arrow

            // - Check hat collisions

            // - If on fire and have been on fire for long enough to lose wings, lose them

            // - Update Animations
            
            // - Update hair/other animated accessory 
        }

        /**
         * Private
         */
        private TFActor _actor;
        private bool _onGround;
        private float _slipperyControl;
        private GameObject _lastPlatform;
        private Vector2 _moveAxis;
        private Vector2 _aimAxis;
        private float _aimDirection;
        private Facing _facing = Facing.Right;
        private Timer _jumpBufferTimer;
        private Timer _jumpGraceTimer;
        private float _wallStickMax;
        private int _graceLedgeDir;

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
