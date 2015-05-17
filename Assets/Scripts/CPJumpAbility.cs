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
    class CPJumpAbility : CPPlayerAbility
    {
        public float JumpPower = 3.2f;
        public float JumpHorizontalBoost = 0.5f;
        public float JumpHeldGravityMultiplier = 0.5f;
        public int JumpBufferFrames = 6;
        public int JumpGraceFrames = 6;
        public float WallStickStart = 0.5f;
        public int WallJumpCheck = 2;
        public int WallJumpTime = 12;
        public float WallStickMaxFall = 1.6f;
        public float WallStickAdd = 0.01f;

        [HideInInspector] public float MULT_JumpPower = 1.0f;
        [HideInInspector] public float MULT_JumpHorizontalBoost = 1.0f;
        [HideInInspector] public float MULT_JumpHeldGravityMultiplier = 1.0f;
        [HideInInspector] public int BONUS_JumpBufferFrames = 0;
        [HideInInspector] public int BONUS_JumpGraceFrames = 0;
        [HideInInspector] public float MULT_WallStickStart = 1.0f;
        [HideInInspector] public int BONUS_WallJumpCheck = 0;
        [HideInInspector] public int BONUS_WallJumpTime = 0;
        [HideInInspector] public float MULT_WallStickMaxFall = 1.0f;
        [HideInInspector] public float MULT_WallStickAdd = 1.0f;

        [HideInInspector] public bool CanJumpHold = true;

        public float CalcJumpPower { get { return this.JumpPower * this.MULT_JumpPower; } }
        public float CalcJumpHorizontalBoost { get { return this.JumpHorizontalBoost * this.MULT_JumpHorizontalBoost; } }
        public float CalcJumpHeldGravityMultiplier { get { return this.JumpHeldGravityMultiplier * this.MULT_JumpHeldGravityMultiplier; } }
        public int CalcJumpBufferFrames { get { return this.JumpBufferFrames * this.BONUS_JumpBufferFrames; } }
        public int CalcJumpGraceFrames { get { return this.JumpGraceFrames * this.BONUS_JumpGraceFrames; } }
        public float CalcWallStickStart { get { return this.WallStickStart * this.MULT_WallStickStart; } }
        public int CalcWallJumpCheck { get { return this.WallJumpCheck + this.BONUS_WallJumpCheck; } }
        public int CalcWallJumpTime { get { return this.WallJumpTime + this.BONUS_WallJumpTime; } }
        public float CalcWallStickMaxFall { get { return this.WallStickMaxFall * this.MULT_WallStickMaxFall; } }
        public float CalcWallStickAdd { get { return this.WallStickAdd * this.MULT_WallStickAdd; } }

        public void Awake()
        {
            _wallStick = this.CalcWallStickStart;

            _jumpBufferTimer = new Timer(this.JumpBufferFrames);
            _jumpBufferTimer.complete();

            _jumpGraceTimer = new Timer(this.JumpGraceFrames);
            _jumpGraceTimer.complete();
        }

        public override void ResetProperties()
        {
            this.MULT_JumpPower = 1.0f;
            this.MULT_JumpHorizontalBoost = 1.0f;
            this.MULT_JumpHeldGravityMultiplier = 1.0f;
            this.BONUS_JumpBufferFrames = 0;
            this.BONUS_JumpGraceFrames = 0;
            this.MULT_WallStickStart = 1.0f;
            this.BONUS_WallJumpCheck = 0;
            this.BONUS_WallJumpTime = 0;
            this.MULT_WallStickMaxFall = 1.0f;
            this.MULT_WallStickAdd = 1.0f;
        }

        public override void ApplyPropertyModifiers()
        {
            _jumpBufferTimer.update(TFPhysics.DeltaFrames);
            if (this.Player.inputState.JumpStarted)
            {
                _jumpBufferTimer.reset();
                _jumpBufferTimer.start();
            }

            if (this.Player.onGround)
            {
                _wallStick = this.CalcWallStickStart;

                if (_jumpGraceTimer.completed || _jumpGraceTimer.timeRemaining < this.JumpGraceFrames)
                    _jumpGraceTimer.reset(this.JumpGraceFrames);
                if (_jumpGraceTimer.paused)
                    _jumpGraceTimer.start();
            }
            else
            {
                _jumpGraceTimer.update(TFPhysics.DeltaFrames);

                // If jump button is held down use smaller number for gravity
                if (this.Player.inputState.Jump && this.CanJumpHold && (Math.Sign(this.Player.velocity.y) == TFPhysics.UpY || Mathf.Abs(this.Player.velocity.y) < 1.0f)) //TODO - Where does the 1.0f come from?
                {
                    this.Player.MULT_Gravity *= this.CalcJumpHeldGravityMultiplier;
                }

                // If we're wall sliding, modify max fall speed
                if (this.Player.moveAxis.X != 0 && canWallSlide((CPPlayer.Facing)this.Player.moveAxis.X))
                {
                    this.Player.CanFastFall = false;
                    this.Player.MULT_MaxFallSpeed = _wallStick / this.Player.MaxFallSpeed;
                    _wallStick = _wallStick.Approach(this.CalcWallStickMaxFall, this.CalcWallStickAdd * TFPhysics.DeltaFrames);
                }
                else
                {
                    this.Player.CanFastFall = true;
                }
            }
        }

        public override void UpdateAbility()
        {
            // Check if it's time to jump
            if (this.Player.inputState.JumpStarted || !_jumpBufferTimer.completed)
            {
                if (!_jumpGraceTimer.completed)
                {
                    // If we're trying to jump from ledge grab, get the direction we moved away from the ledge
                    //int ledgeDir = _graceLedgeDir;
                    //if (_moveAxis.X != ledgeDir)
                    //    ledgeDir = 0;
                    //jump(true, true, false, ledgeDir);
                    jump();
                }
                else
                {
                    if (canWallJump(CPPlayer.Facing.Left))
                        wallJump((int)CPPlayer.Facing.Right);
                    else if (canWallJump(CPPlayer.Facing.Right))
                        wallJump((int)CPPlayer.Facing.Left);
                }
            }
        }


        /**
         * Private
         */
        private float _wallStick;
        private Timer _jumpBufferTimer;
        private Timer _jumpGraceTimer;

        private bool canWallSlide(CPPlayer.Facing direction)
        {
            return this.Player.inputState.MoveY != TFPhysics.DownY && canWallJump(direction);
        }

        private bool canWallJump(CPPlayer.Facing direction)
        {
            //TODO - Where does the 5 come from?
            // Make sure we are far enough off the ground
            if (this.boxCollider2D.CollideFirst(0, TFPhysics.DownY * 5, this.Player.actor.CollisionMask, this.Player.actor.CollisionTag))
                return false;

            //TODO - Is the -1.0f necessary?
            // We can only wall jump if the top of our body is next to the wall
            Vector2 wallJumpCollidePoint = direction == CPPlayer.Facing.Right ?
                new Vector2(this.Player.actor.RightX - 1.0f + this.WallJumpCheck, this.Player.actor.TopY) :
                new Vector2(this.Player.actor.LeftX - this.WallJumpCheck, this.Player.actor.TopY);
            return this.Player.actor.CollidePoint(wallJumpCollidePoint);
        }

        private void jump()
        {
            _jumpBufferTimer.complete();
            _jumpGraceTimer.complete();

            this.Player.SetVelocityY(TFPhysics.UpY * this.CalcJumpPower);

            if (this.Player.moveAxis.X != 0)
                this.Player.SetVelocityX(this.Player.velocity.x + this.CalcJumpHorizontalBoost * this.Player.moveAxis.X);

            this.CanJumpHold = true;
        }

        private void wallJump(int dir)
        {
            _jumpBufferTimer.complete();
            _jumpGraceTimer.complete();

            this.Player.SetVelocityY(TFPhysics.UpY * this.CalcJumpPower);
            this.Player.SetVelocityX((float)dir * 2f); //TODO - Where does the 2.0 come  from?
            this.CanJumpHold = true;
            _wallStick = this.CalcWallStickStart;
            this.Player.facing = (CPPlayer.Facing)dir;
            this.Player.SetAutoMove(this.CalcWallJumpTime, dir);
        }
    }
}
