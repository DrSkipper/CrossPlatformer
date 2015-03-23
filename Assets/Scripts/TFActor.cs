﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Extensions;

namespace Assets.Scripts
{
    class TFActor : VoBehavior
    {
        public bool Pushable = true;
        public Vector2 ActualPosition
        {
            get
            {
                Vector3 position = this.transform.position;
                return new Vector2(position.x + _positionModifier.x, 
                                   position.y + _positionModifier.y);
            }
        }

		public virtual void MoveExactH(int move, Delegate onCollide = null)
        {
			int unitDir = Math.Sign(move);
			while (move != 0)
			{
                GameObject collidedObject = this.boxCollider2D.CollideFirst(unitDir, 0.0f); // use Solid tag? or Platforms layer mask at least probably

                if (collidedObject)
                {
                    if (onCollide != null)
                    {
                        //onCollide(collidedObject);
                        return;
                    }
                    break;
                }
                else
                {
                    this.transform.position += new Vector3(unitDir, 0.0f);
                    move -= unitDir;
                }
			}
        }

        public virtual void MoveExactV(int move, Delegate onCollide = null)
        {
            int unitDir = Math.Sign(move);
            while (move != 0)
            {
                GameObject collidedObject = this.boxCollider2D.CollideFirst(0.0f, unitDir); // use Solid tag? or Platforms layer mask at least probably

                if (collidedObject)
                {
                    if (onCollide != null)
                    {
                        //onCollide(collidedObject);
                        return;
                    }
                    break;
                }
                else
                {
                    this.transform.position += new Vector3(0.0f, unitDir);
                    move -= unitDir;
                }
            }
        }
        
		public bool MoveH(float moveH, Delegate onCollide = null)
        {
            _positionModifier.x += moveH;
            int moveAmount = (int)Math.Round(_positionModifier.x);
            if (moveAmount != 0)
            {
                int unitDir = Math.Sign(moveAmount);
                _positionModifier.x -= moveAmount;
                while (moveAmount != 0)
                {
                    GameObject collidedObject = this.boxCollider2D.CollideFirst(unitDir, 0.0f); // use Solid tag? or Platforms layer mask at least probably

                    if (collidedObject)
                    {
                        _positionModifier.x = 0.0f;
                        if (onCollide != null)
                        {
                            //onCollide(collidedObject);
                        }
                        return true;
                    }
                    this.transform.position += new Vector3(unitDir, 0.0f);
                    moveAmount -= unitDir;
                }
            }
            return false;
        }

        public bool MoveV(float moveV, Delegate onCollide = null)
        {
            _positionModifier.y += moveV;
            int moveAmount = (int)Math.Round(_positionModifier.x);
            if (moveAmount != 0)
            {
                int unitDir = Math.Sign(moveAmount);
                _positionModifier.y -= moveAmount;
                while (moveAmount != 0)
                {
                    GameObject collidedObject = this.boxCollider2D.CollideFirst(0.0f, unitDir); // use Solid tag? or Platforms layer mask at least probably

                    if (collidedObject)
                    {
                        _positionModifier.y = 0.0f;
                        if (onCollide != null)
                        {
                            //onCollide(collidedObject);
                        }
                        return true;
                    }
                    this.transform.position += new Vector3(0.0f, unitDir);
                    moveAmount -= unitDir;
                }
            }
            return false;
        }

        public void Move(Vector2 amount, Delegate onCollideH = null, Delegate onCollideV = null)
        {
            this.MoveH(amount.x, onCollideH);
            this.MoveV(amount.y, onCollideV);
        }
        
		public void MoveIgnoreSolids(Vector2 amount)
        {
            _positionModifier += amount;

            int moveH = (int)Math.Round(_positionModifier.x);
            _positionModifier.x -= moveH;
            this.transform.position += new Vector3(moveH, 0.0f);

            int moveV = (int)Math.Round(_positionModifier.y);
            _positionModifier.y -= moveV;
            this.transform.position += new Vector3(0.0f, moveV);
        }
        
		public void MoveTo(Vector2 target, Delegate onCollideH = null, Delegate onCollideV = null)
        {
            this.MoveH(target.x - this.position2D.x, onCollideH);
            this.MoveV(target.y - this.position2D.y, onCollideV);
        }

        public void MoveTowards(Vector2 target, float maxAmount, Delegate onCollideH = null, Delegate onCollideV = null)
        {
            Vector2 movedPoint = Vector2.MoveTowards(this.ActualPosition, target, maxAmount);
            this.Move(movedPoint - this.ActualPosition, null, null);
        }
        
		public void MoveTowardsX(float targetX, float maxAmount, Delegate onCollide = null)
        {
            maxAmount = targetX > this.ActualPosition.x ? Mathf.Abs(maxAmount) : -Mathf.Abs(maxAmount);
            float movedX = targetX - this.ActualPosition.x > maxAmount ? this.ActualPosition.x + maxAmount : targetX;
            this.MoveH(movedX - this.ActualPosition.x, onCollide);
        }

        public void MoveTowardsY(float targetY, float maxAmount, Delegate onCollide = null)
        {
            maxAmount = targetY > this.ActualPosition.y ? Mathf.Abs(maxAmount) : -Mathf.Abs(maxAmount);
            float movedY = targetY - this.ActualPosition.y > maxAmount ? this.ActualPosition.x + maxAmount : targetY;
            this.MoveV(movedY - this.ActualPosition.y, onCollide);
        }
        
		public void MoveTowardsWrap(Vector2 target, float maxAmount, Delegate onCollide = null)
        {
            //if (this.ActualPosition.X + (320f - target.X) < Math.Abs(target.X - this.ActualPosition.X))
            //{
            //    this.MoveTowards(target - WrapMath.AddWidth, maxAmount, onCollide, null);
            //    return;
            //}
            //if (target.X + (320f - this.ActualPosition.X) < Math.Abs(target.X - this.ActualPosition.X))
            //{
            //    this.MoveTowards(target + WrapMath.AddWidth, maxAmount, onCollide, null);
            //    return;
            //}
            this.MoveTowards(target, maxAmount, onCollide, null);
        }

        public virtual bool IsRiding(GameObject solid) // Solid solid
        {
            // Check if we are standing on this object (if so we can "ride" it - i.e. use its velocity as our base)
            return this.boxCollider2D.CollideCheck(solid, 0.0f, -Vector2.up.y);
        }

        /**
         * Private
         */
        private Vector2 _positionModifier;

        // If we're inside an object, move ourselves out to the left
		private bool SnapToLeftmostCollision(ref GameObject block) // ref Solid block
        {
            bool result = false;
            float right = this.boxCollider2D.bounds.max.x;
            float minX = right;
            foreach (GameObject current in GameObject.FindGameObjectsWithTag("solid")) //todo - tag
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.bounds.min.x < minX)
                    {
                        minX = otherCollider.bounds.min.x;
                        block = current;
                        result = true;
                    }
                }
            }
            if (result)
                this.transform.position = new Vector3(this.position2D.x + (minX - right), this.position2D.y);
            return result;
        }

        private bool SnapToRightmostCollision(ref GameObject block) // ref Solid block
        {
            bool result = false;
            float left = this.boxCollider2D.bounds.min.x;
            float maxX = left;
            foreach (GameObject current in GameObject.FindGameObjectsWithTag("solid")) //todo - tag
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.bounds.max.x > maxX)
                    {
                        maxX = otherCollider.bounds.max.x;
                        block = current;
                        result = true;
                    }
                }
            }
            if (result)
                this.transform.position = new Vector3(this.position2D.x + (maxX - left), this.position2D.y);
            return result;
        }

        private bool SnapToTopmostCollision(ref GameObject block) // ref Solid block
        {
            bool result = false;
            float bottom = this.boxCollider2D.bounds.min.y;
            float maxY = bottom;
            foreach (GameObject current in GameObject.FindGameObjectsWithTag("solid")) //todo - tag
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.bounds.max.y > maxY)
                    {
                        maxY = otherCollider.bounds.max.y;
                        block = current;
                        result = true;
                    }
                }
            }
            if (result)
                this.transform.position = new Vector3(this.position2D.x, this.position2D.y + (maxY - bottom));
            return result;
        }

        private bool SnapToBottommostCollision(ref GameObject block) // ref Solid block
        {
            bool result = false;
            float top = this.boxCollider2D.bounds.max.y;
            float minY = top;
            foreach (GameObject current in GameObject.FindGameObjectsWithTag("solid")) //todo - tag
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.bounds.min.y < minY)
                    {
                        minY = otherCollider.bounds.min.y;
                        block = current;
                        result = true;
                    }
                }
            }
            if (result)
                this.transform.position = new Vector3(this.position2D.x, this.position2D.y + (minY - top));
            return result;
        }
    }
}
