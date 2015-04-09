using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class TFPhysics
    {
        public static int UpY { get { return Math.Sign(Vector2.up.y); } }
        public static int DownY { get { return -TFPhysics.UpY; } }
    }
}
