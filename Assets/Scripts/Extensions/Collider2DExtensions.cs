using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Extensions
{
    public static class Collider2DExtensions
    {
        /**
         * Collider2D
         */
        public static float LeftX(this BoxCollider2D self) { return self.bounds.min.x; }
        public static float RightX(this BoxCollider2D self) { return self.bounds.max.x; }
        public static float TopY(this BoxCollider2D self) { return self.bounds.max.y; }
        public static float BottomY(this BoxCollider2D self) { return self.bounds.min.y; }

        /**
         * BoxCollider2D
         */
        public static GameObject CollideFirst(this BoxCollider2D self, float offsetX = 0.0f, float offsetY = 0.0f, int layerMask = Physics2D.DefaultRaycastLayers, string objectTag = null)
        {
            Bounds bounds = self.bounds;
            float boundsMinX = bounds.min.x - bounds.size.x - Mathf.Abs(offsetX);
            float boundsMinY = bounds.min.y - bounds.size.y - Mathf.Abs(offsetY);
            float boundsMaxX = bounds.max.x + bounds.size.x + Mathf.Abs(offsetX);
            float boundsMaxY = bounds.max.y + bounds.size.y + Mathf.Abs(offsetY);
            Vector2 corner1 = new Vector2(boundsMinX, boundsMinY);
            Vector2 corner2 = new Vector2(boundsMaxX, boundsMaxY);
            
            // Overlap an area significantly larger than our bounding box so we can directly compare bounds with collision candidates
            // (Relying purely on OverlapAreaAll for collision seems to be inconsistent at times)
            Collider2D[] colliders = Physics2D.OverlapAreaAll(corner1, corner2, layerMask);
            Bounds offsetBounds = bounds;
            offsetBounds.center = new Vector3(offsetBounds.center.x + offsetX, offsetBounds.center.y + offsetY, offsetBounds.center.z);

            foreach (Collider2D collider in colliders)
            {
                if (collider != self && (objectTag == null || collider.tag == objectTag) && offsetBounds.Intersects(collider.bounds))
                    return collider.gameObject;
            }

            return null;
        }

        public static bool CollideCheck(this BoxCollider2D self, GameObject checkObject, float offsetX = 0.0f, float offsetY = 0.0f)
        {
            BoxCollider2D other = checkObject.GetComponent<BoxCollider2D>();

            if (other)
            {
                Bounds offsetBounds = self.bounds;
                offsetBounds.center = new Vector3(offsetBounds.center.x + offsetX, offsetBounds.center.y + offsetY, offsetBounds.center.z);
                return offsetBounds.Intersects(other.bounds);
            }

            return false;
        }
    }
}
