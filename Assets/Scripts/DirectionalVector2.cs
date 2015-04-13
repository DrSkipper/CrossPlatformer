using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    /**
     * Vector2 in which X and Y are always 1, -1, or 0
     */
    struct DirectionalVector2
    {
        public int X { get { return _x; } set { _x = Math.Sign(value); } }
        public int Y { get { return _y; } set { _y = Math.Sign(value); } }
        public float floatX { get { return (float)_x; } set { _x = Math.Sign(value); } }
        public float floatY { get { return (float)_y; } set { _y = Math.Sign(value); } }

        public DirectionalVector2(int xDir = 0, int yDir = 0)
        {
            _x = Math.Sign(xDir);
            _y = Math.Sign(yDir);
        }

        public DirectionalVector2(float xDir = 0, float yDir = 0)
        {
            _x = Math.Sign(xDir);
            _y = Math.Sign(yDir);
        }

        /**
         * Private
         */
        private int _x;
        private int _y;
    }
}
