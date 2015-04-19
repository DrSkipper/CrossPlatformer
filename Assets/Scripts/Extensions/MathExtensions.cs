using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Extensions
{
    public static class MathExtensions
    {
        public const float PI_2 = 1.5707963f;
        public const float PI_4 = 0.7853982f;
        public const float PI_8 = 0.3926991f;

        public static float Approach(this float self, float target, float maxChange)
        {
            maxChange = Mathf.Abs(maxChange);
            return self <= target ? Mathf.Min(self + maxChange, target) : Mathf.Max(self - maxChange, target);
        }

        public static int Approach(this int self, int target, int maxChange)
        {
            maxChange = Math.Abs(maxChange);
            return self <= target ? Math.Min(self + maxChange, target) : Math.Max(self - maxChange, target);
        }

        public static Vector2 AngleToVector(float angleRadians, float length)
        {
            return new Vector2((float)Math.Cos((double)angleRadians) * length, (float)Math.Sin((double)angleRadians) * length);
        }
    }
}
