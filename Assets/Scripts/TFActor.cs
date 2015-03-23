using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    class TFActor : VoBehavior
    {
		public virtual void MoveExactH(int move, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }

        public virtual void MoveExactV(int move, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }
        
		public bool MoveH(float moveH, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }

        public bool MoveV(float moveV, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }

        public void Move(Vector2 amount, Delegate onCollideH = null, Delegate onCollideV = null)
        {
            this.MoveH(amount.x, onCollideH);
            this.MoveV(amount.y, onCollideV);
        }
        
		public void MoveIgnoreSolids(Vector2 amount)
        {
            throw new NotSupportedException();
        }
        
		public void MoveTo(Vector2 target, Delegate onCollideH = null, Delegate onCollideV = null)
        {
            throw new NotSupportedException();
        }
        
		public void MoveTowardsX(float targetX, float maxAmount, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }

        public void MoveTowardsY(float targetY, float maxAmount, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }
        
		public void MoveTowardsWrap(Vector2 target, float maxAmount, Delegate onCollide = null)
        {
            throw new NotSupportedException();
        }

        public virtual bool IsRiding(GameObject solid) // Solid solid
        {
            // Check if we are standing on this object (if so we can "ride" it - use its velocity as our base
            throw new NotSupportedException();
        }

        /**
         * Private
         */
		private bool SnapToLeftmostCollision(ref GameObject block) // ref Solid block
        {
            throw new NotSupportedException();
        }

        private bool SnapToRightmostCollision(ref GameObject block) // ref Solid block
        {
            throw new NotSupportedException();
        }

        private bool SnapToTopmostCollision(ref GameObject block) // ref Solid block
        {
            throw new NotSupportedException();
        }

        private bool SnapToBottommostCollision(ref GameObject block) // ref Solid block
        {
            throw new NotSupportedException();
        }
    }
}
