using UnityEngine;
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
        public LayerMask CollisionMask = 0;
        public string CollisionTag = null;

        public float LeftX { get { return this.boxCollider2D.LeftX(); } }
        public float RightX { get { return this.boxCollider2D.RightX(); } }
        public float TopY { get { return this.boxCollider2D.TopY(); } }
        public float BottomY { get { return this.boxCollider2D.BottomY(); } }

        public Vector2 ActualPosition
        {
            get
            {
                Vector3 position = this.transform.position;
                return new Vector2(position.x + _positionModifier.x, 
                                   position.y + _positionModifier.y);
            }
        }

        public delegate void ActorCollisionHandler(GameObject collidedObject);

        public void Awake()
        {
            if (this.CollisionTag == "")
                this.CollisionTag = null;
        }

        public virtual void MoveExactH(int move, ActorCollisionHandler onCollide = null)
        {
			int unitDir = Math.Sign(move);
			while (move != 0)
			{
                GameObject collidedObject = this.boxCollider2D.CollideFirst(unitDir, 0.0f, CollisionMask, CollisionTag);

                if (collidedObject)
                {
                    if (onCollide != null)
                    {
                        onCollide(collidedObject);
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

        public virtual void MoveExactV(int move, ActorCollisionHandler onCollide = null)
        {
            int unitDir = Math.Sign(move);
            while (move != 0)
            {
                GameObject collidedObject = this.boxCollider2D.CollideFirst(0.0f, unitDir, CollisionMask, CollisionTag);

                if (collidedObject)
                {
                    if (onCollide != null)
                    {
                        onCollide(collidedObject);
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

        public bool MoveH(float moveH, ActorCollisionHandler onCollide = null)
        {
            _positionModifier.x += moveH;
            int moveAmount = (int)Math.Round(_positionModifier.x);
            if (moveAmount != 0)
            {
                int unitDir = Math.Sign(moveAmount);
                _positionModifier.x -= moveAmount;
                while (moveAmount != 0)
                {
                    GameObject collidedObject = this.boxCollider2D.CollideFirst(unitDir, 0.0f, CollisionMask, CollisionTag);

                    if (collidedObject)
                    {
                        _positionModifier.x = 0.0f;
                        if (onCollide != null)
                        {
                            onCollide(collidedObject);
                        }
                        return true;
                    }
                    this.transform.position += new Vector3(unitDir, 0.0f);
                    moveAmount -= unitDir;
                }
            }
            return false;
        }

        public bool MoveV(float moveV, ActorCollisionHandler onCollide = null)
        {
            _positionModifier.y += moveV;
            int moveAmount = (int)Math.Round(_positionModifier.y);
            if (moveAmount != 0)
            {
                int unitDir = Math.Sign(moveAmount);
                _positionModifier.y -= moveAmount;
                while (moveAmount != 0)
                {
                    GameObject collidedObject = this.boxCollider2D.CollideFirst(0.0f, unitDir, CollisionMask, CollisionTag);

                    if (collidedObject)
                    {
                        _positionModifier.y = 0.0f;
                        if (onCollide != null)
                        {
                            onCollide(collidedObject);
                        }
                        return true;
                    }
                    this.transform.position += new Vector3(0.0f, unitDir);
                    moveAmount -= unitDir;
                }
            }
            return false;
        }

        public void Move(Vector2 amount, ActorCollisionHandler onCollideH = null, ActorCollisionHandler onCollideV = null)
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

        public void MoveTo(Vector2 target, ActorCollisionHandler onCollideH = null, ActorCollisionHandler onCollideV = null)
        {
            this.MoveH(target.x - this.position2D.x, onCollideH);
            this.MoveV(target.y - this.position2D.y, onCollideV);
        }

        public void MoveTowards(Vector2 target, float maxAmount, ActorCollisionHandler onCollideH = null, ActorCollisionHandler onCollideV = null)
        {
            Vector2 movedPoint = Vector2.MoveTowards(this.ActualPosition, target, maxAmount);
            this.Move(movedPoint - this.ActualPosition, onCollideH, onCollideV);
        }

        public void MoveTowardsX(float targetX, float maxAmount, ActorCollisionHandler onCollide = null)
        {
            maxAmount = Mathf.Abs(maxAmount);
            float maxAmountDir = targetX > this.ActualPosition.x ? maxAmount : -maxAmount;
            float movedX = Math.Abs(targetX - this.ActualPosition.x) > maxAmount ? this.ActualPosition.x + maxAmountDir : targetX;
            this.MoveH(movedX - this.ActualPosition.x, onCollide);
        }

        public void MoveTowardsY(float targetY, float maxAmount, ActorCollisionHandler onCollide = null)
        {
            maxAmount = Mathf.Abs(maxAmount);
            float maxAmountDir = targetY > this.ActualPosition.y ? maxAmount : -maxAmount;
            float movedY = Math.Abs(targetY - this.ActualPosition.y) > maxAmount ? this.ActualPosition.x + maxAmountDir : targetY;
            this.MoveV(movedY - this.ActualPosition.y, onCollide);
        }

        public void MoveTowardsWrap(Vector2 target, float maxAmount, ActorCollisionHandler onCollide = null)
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
            return this.boxCollider2D.CollideCheck(solid, 0.0f, TFPhysics.DownY);
        }

        public GameObject CollidePoint(Vector2 point)
        {
            GameObject collidedObject = null;
            if (this.CollisionTag == null)
            {
                Collider2D collider = Physics2D.OverlapPoint(point, this.CollisionMask);
                if (collider != null)
                    collidedObject = collider.gameObject;
            }
            else
            {
                Collider2D[] colliders = Physics2D.OverlapPointAll(point, this.CollisionMask);
                foreach (Collider2D collider in colliders)
                {
                    if (collider.tag == this.CollisionTag)
                    {
                        collidedObject = collider.gameObject;
                        break;
                    }
                }
            }
            return collidedObject;
        }

        /**
         * Private
         */
        private Vector2 _positionModifier;

        //TODO - Might be better to just add CollideAll method that can use the OverlapAreaAll method
        private GameObject[] _allCollidableObjects
        {
            get
            {
                GameObject[] gameObjects = this.CollisionTag != null ? GameObject.FindGameObjectsWithTag(this.CollisionTag) : GameObject.FindObjectsOfType<GameObject>();
                List<GameObject> retVal = new List<GameObject>();

                foreach (GameObject gameObject in gameObjects)
                {
                    if ((gameObject.layer & this.CollisionMask) == gameObject.layer)
                        retVal.Add(gameObject);
                }

                return retVal.ToArray();
            }
        }

        // If we're inside an object, move ourselves out to the left
		private bool SnapToLeftmostCollision(ref GameObject block) // ref Solid block
        {
            bool result = false;
            float right = this.RightX;
            float minX = right;
            foreach (GameObject current in _allCollidableObjects)
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.LeftX() < minX)
                    {
                        minX = otherCollider.LeftX();
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
            float left = this.LeftX;
            float maxX = left;
            foreach (GameObject current in _allCollidableObjects)
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.RightX() > maxX)
                    {
                        maxX = otherCollider.RightX();
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
            float bottom = this.BottomY;
            float maxY = bottom;
            foreach (GameObject current in _allCollidableObjects)
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.TopY() > maxY)
                    {
                        maxY = otherCollider.TopY();
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
            float top = this.TopY;
            float minY = top;
            foreach (GameObject current in _allCollidableObjects)
            {
                if (this.boxCollider2D.CollideCheck(current))
                {
                    BoxCollider2D otherCollider = current.GetComponent<BoxCollider2D>();
                    if (otherCollider.BottomY() < minY)
                    {
                        minY = otherCollider.BottomY();
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
