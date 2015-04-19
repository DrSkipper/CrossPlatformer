using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Extensions
{
    public static class Vector2Extensions
    {
        public static float Angle(this Vector2 vector)
        {
            return Mathf.Atan2(vector.x, -Vector2.up.y * vector.y);
        }

        public static Vector2 EightWayNormal(this Vector2 vec)
        {
            float angle = vec.Angle();
            angle = (float)Math.Floor((double)((angle + MathExtensions.PI_8) / MathExtensions.PI_4)) * MathExtensions.PI_4 - MathExtensions.PI_2;
            return MathExtensions.AngleToVector(angle, 1.0f);
        }
    }
}
