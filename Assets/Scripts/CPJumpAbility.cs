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

        public override void ResetProperties()
        {
        }

        public override void ApplyPropertyModifiers()
        {
        }

        public override void UpdateAbility()
        {
        }
    }
}
