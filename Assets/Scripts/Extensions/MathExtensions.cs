using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Extensions
{
    public static class MathExtensions
    {
        public static float Approach(this float self, float target, float maxChange)
        {
            return self <= target ? Mathf.Min(self + maxChange, target) : Mathf.Max(self - maxChange, target);
        }

        public static int Approach(this int self, int target, int maxChange)
        {
            return self <= target ? Math.Min(self + maxChange, target) : Math.Max(self - maxChange, target);
        }
    }
}
