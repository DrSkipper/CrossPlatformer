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
    class CPLedgeGrabAbility : CPPlayerAbility
    {
        public const string PLAYER_STATE_LEDGE_GRAB = "ledgegrab";

        public int LedgeGrabOffset = 2;
        public int LedgeCheckHorizontal = 2;
        public int LedgeCheckVertical = 10;
        public int LedgeReleaseGraceFrames = 12;

        [HideInInspector] public int BONUS_LedgeGrabOffset = 0;
        [HideInInspector] public int BONUS_LedgeCheckHorizontal = 0;
        [HideInInspector] public int BONUS_LedgeCheckVertical = 0;
        [HideInInspector] public int BONUS_LedgeReleaseGraceFrames = 0;

        public int CalcLedgeGrabOffset { get { return this.LedgeGrabOffset + BONUS_LedgeGrabOffset; } }
        public int CalcLedgeCheckHorizontal { get { return this.LedgeCheckHorizontal * BONUS_LedgeCheckHorizontal; } }
        public int CalcLedgeCheckVertical { get { return this.LedgeCheckVertical + BONUS_LedgeCheckVertical; } }
        public int CalcLedgeReleaseGraceFrames { get { return this.LedgeReleaseGraceFrames + BONUS_LedgeReleaseGraceFrames; } }

        public void Awake()
        {
            _jumpAbility = this.GetComponent<CPJumpAbility>();
            this.Player.stateMachine.AddState(PLAYER_STATE_LEDGE_GRAB, this.updateLedgeGrab, this.enterLedgeGrab, this.exitLedgeGrab);
        }

        public override void ResetProperties()
        {
            BONUS_LedgeGrabOffset = 0;
            BONUS_LedgeCheckHorizontal = 0;
            BONUS_LedgeCheckVertical = 0;
            BONUS_LedgeReleaseGraceFrames = 0;
        }

        public override void ApplyPropertyModifiers()
        {
        }

        public override string PostUpdateAbility()
        {
            // Check for ledge grab
            if (!this.Player.onGround)
            {
                int velocityDirY = Math.Sign(this.Player.velocity.y);
                bool notGoingUp = velocityDirY == TFPhysics.DownY || velocityDirY == 0;
                bool notHoldingDown = this.Player.moveAxis.Y == TFPhysics.UpY || this.Player.moveAxis.Y == 0;

                if (notGoingUp && this.Player.moveAxis.X != 0 && notHoldingDown && this.boxCollider2D.CollideFirst(this.Player.moveAxis.X * this.LedgeCheckHorizontal, 0, this.Player.actor.CollisionMask, this.Player.actor.CollisionTag))
                {
                    int direction = this.Player.moveAxis.X;
                    for (int i = 0; i < this.CalcLedgeCheckVertical; ++i)
                    {
                        int offsetY = (int)this.position2D.y + TFPhysics.UpY * i;
                        if (canGrabLedge(offsetY, direction))
                            return grabLedge(offsetY, direction);
                    }
                }
            }

            return null;
        }

        public string updateLedgeGrab()
        {
            if (this.Player.moveAxis.Y == TFPhysics.DownY || this.Player.moveAxis.X != (int)this.Player.facing)
            {
                if (_jumpAbility != null)
                    _jumpAbility.SetJumpGrace(this.CalcLedgeReleaseGraceFrames, -(int)this.Player.facing);
                return CPPlayer.PLAYER_STATE_NORMAL;
            }

            if (_jumpAbility != null && this.Player.inputState.JumpStarted)
            {
                if (this.Player.moveAxis.X == -(int)this.Player.facing)
                    _jumpAbility.jump(-(int)this.Player.facing);
                else if (this.Player.moveAxis.Y != TFPhysics.DownY)
                    _jumpAbility.jump();
                return CPPlayer.PLAYER_STATE_NORMAL;
            }

            if (!canGrabLedge((int)this.position2D.y + TFPhysics.UpY * this.CalcLedgeGrabOffset, (int)this.Player.facing))
                return CPPlayer.PLAYER_STATE_NORMAL;

            return PLAYER_STATE_LEDGE_GRAB;
        }

        /**
         * Private
         */
        private CPJumpAbility _jumpAbility;


        private void enterLedgeGrab()
        {
            this.Player.SetVelocityX(0.0f);
            this.Player.SetVelocityY(0.0f);
            this.Player.lastPlatform = null;
        }

        private void exitLedgeGrab()
        {
        }

        private bool canGrabLedge(int targetY, int direction)
        {
            int targetYDistance = Mathf.RoundToInt(targetY - this.position2D.y);

            // Make sure place we'll grab into is empty
            if (this.boxCollider2D.CollideFirst(0, targetYDistance + TFPhysics.DownY * this.CalcLedgeGrabOffset, this.Player.actor.CollisionMask, this.Player.actor.CollisionTag))
                return false;

            // Make sure we're not close to ground
            //TODO - Where does the 5.0 come from?
            if (this.boxCollider2D.CollideFirst(0, 5, this.Player.actor.CollisionMask, this.Player.actor.CollisionTag))
                return false;

            float xCheck = direction == -1 ? this.Player.actor.LeftX - this.CalcLedgeCheckHorizontal : this.Player.actor.RightX + this.CalcLedgeCheckHorizontal;

            // Make sure this is actually a ledge by checking that the space above the target location is empty
            if (this.Player.actor.CollidePoint(new Vector2(xCheck, targetY + TFPhysics.UpY)))
                return false;

            // Make sure there is an object here to grab onto
            return this.Player.actor.CollidePoint(new Vector2(xCheck, targetY));
        }

        private string grabLedge(int targetY, int direction)
        {
            this.Player.facing = (CPPlayer.Facing)direction;
            this.Player.SetVelocityY(0.0f);

            Vector3 oldPosition = this.transform.position;
            this.transform.position = new Vector3(Mathf.Round(oldPosition.x), targetY + TFPhysics.DownY * this.CalcLedgeGrabOffset, oldPosition.z);

            while (!this.boxCollider2D.CollideFirst(direction, 0, this.Player.actor.CollisionMask, this.Player.actor.CollisionTag))
            {
                oldPosition = this.transform.position;
                this.transform.position = new Vector3(oldPosition.x + direction, oldPosition.y, oldPosition.z);
            }

            return PLAYER_STATE_LEDGE_GRAB;
        }
    }
}
