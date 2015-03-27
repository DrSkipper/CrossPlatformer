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
            return Mathf.Atan2(vector.x, vector.y);
        }
    }
}
