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
        public static int LeftX(this BoxCollider2D self) { return Mathf.RoundToInt(self.bounds.min.x); }
        public static int RightX(this BoxCollider2D self) { return Mathf.RoundToInt(self.bounds.max.x); }
        public static int TopY(this BoxCollider2D self) { return Mathf.RoundToInt(self.bounds.max.y); }
        public static int BottomY(this BoxCollider2D self) { return Mathf.RoundToInt(self.bounds.min.y); }

        /**
         * BoxCollider2D
         */
        public static GameObject CollideFirst(this BoxCollider2D self, int offsetX = 0, int offsetY = 0, int layerMask = Physics2D.DefaultRaycastLayers, string objectTag = null)
        {
            // Overlap an area significantly larger than our bounding box so we can directly compare bounds with collision candidates
            // (Relying purely on OverlapAreaAll for collision seems to be inconsistent at times)
            Bounds bounds = self.bounds;
            Vector2 corner1 = new Vector2(bounds.min.x - bounds.size.x + offsetX, bounds.min.y - bounds.size.y + offsetY);
            Vector2 corner2 = new Vector2(bounds.max.x + bounds.size.x + offsetX, bounds.max.y + bounds.size.y + offsetY);
            Collider2D[] colliders = Physics2D.OverlapAreaAll(corner1, corner2, layerMask);

            // If there is only one collider, it is our collider, so there is nothing to collide with
            if (colliders.Length <= 1)
                return null;
            
            // Apply offset to our bounds and make sure we're using integer/pixel-perfect math
            bounds.center = new Vector3(Mathf.Round(bounds.center.x) + offsetX, Mathf.Round(bounds.center.y) + offsetY, Mathf.Round(bounds.center.z));

            // Account for bounds intersections marking true when colliders end at same point
            bounds.size = new Vector3(bounds.size.x - 2, bounds.size.y - 2, bounds.size.z);

            foreach (Collider2D collider in colliders)
            {
                if (collider != self && (objectTag == null || collider.tag == objectTag))
                {
                    // Make sure we're using integer/pixel-perfect math
                    Bounds otherBounds = collider.bounds;
                    otherBounds.center = new Vector3(Mathf.Round(otherBounds.center.x), Mathf.Round(otherBounds.center.y), Mathf.Round(otherBounds.center.z));

                    if (bounds.Intersects(otherBounds))
                        return collider.gameObject;
                }
            }

            return null;
        }

        public static bool CollideCheck(this BoxCollider2D self, GameObject checkObject, int offsetX = 0, int offsetY = 0)
        {
            BoxCollider2D other = checkObject.GetComponent<BoxCollider2D>();

            if (other)
            {
                // Apply offset to our bounds and make sure we're using integer/pixel-perfect math
                Bounds bounds = self.bounds;
                Bounds otherBounds = other.bounds;
                bounds.center = new Vector3(Mathf.Round(bounds.center.x) + offsetX, Mathf.Round(bounds.center.y) + offsetY, Mathf.Round(bounds.center.z));
                otherBounds.center = new Vector3(Mathf.Round(otherBounds.center.x), Mathf.Round(otherBounds.center.y), Mathf.Round(otherBounds.center.z));

                // Account for bounds intersections marking true when colliders end at same point
                bounds.size = new Vector3(bounds.size.x - 2, bounds.size.y - 2, bounds.size.z);

                return bounds.Intersects(otherBounds);
            }

            return false;
        }
    }
}
