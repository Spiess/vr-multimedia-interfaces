using System.Collections;
using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// A <c>SketchingUtensil</c> is an object that can be picked up by a <see cref="SketchpadInteractor"/> and shows
    /// its originating position with a ghost, where it can also be returned to.
    /// </summary>
    [RequireComponent(typeof(Collider)), RequireComponent(typeof(Rigidbody))]
    public class SketchingUtensil : MonoBehaviour
    {
        /// <summary>
        /// The speed at which this utensil is attached to a <see cref="SketchpadInteractor"/>. The time to attach is the inverse of this speed (in seconds).
        /// </summary>
        public float attachmentSpeed;
        /// <summary>
        /// The attachment point on the <see cref="SketchpadInteractor"/> to use when it picks up this utensil.
        /// </summary>
        public Transform attachmentPoint;
        /// <summary>
        /// The ghost of this utensil that will be shown when it is picked up by a <see cref="SketchpadInteractor"/>.
        /// </summary>
        /// <remarks>
        /// The transform of the ghost will be used to put the utensil back in its holder.
        /// </remarks>
        public SphereCollider ghost;
        
        /// <summary>
        /// Whether this utensil is currently colliding with its ghost.
        /// </summary>
        public bool CollidingWithGhost { get; private set; }
        /// <summary>
        /// Whether this utensil is still being moved by a <see cref="MoveTo"/> coroutine.
        /// </summary>
        public bool IsMoving { get; private set; }

        public IEnumerator PickUp(Transform attachmentPoint)
        {
            return MoveTo(Vector3.zero, Quaternion.identity, attachmentPoint, true);
        }
        public IEnumerator PutBack()
        {
            var ghostTransform = ghost.transform;
            return MoveTo(ghostTransform.localPosition, ghostTransform.localRotation, ghostTransform.parent, false);
        }
        public IEnumerator MoveTo(Vector3 position, Quaternion rotation, Transform newParent, bool showGhost)
        {
            IsMoving = true;
            
            // if the utensil is being moved away from the ghost, start showing it before starting any movement
            if (showGhost && ghost != null)
            {
                ghost.gameObject.SetActive(true);
            }
            
            // set the new parent of this utensil, while keeping its world position
            var t = transform;
            t.SetParent(newParent);
            
            // start animating this utensil towards its new position
            var time = 0f;
            var oldPosition = t.localPosition;
            var oldRotation = t.localRotation;
            while(true)
            {
                time += Time.deltaTime;
                var progress = time * attachmentSpeed;
                t.localPosition = Vector3.Lerp(oldPosition, position, progress);
                t.localRotation = Quaternion.Slerp(oldRotation, rotation, progress);
                if (progress >= 1)
                {
                    // if the utensil is being returned to its ghost, hide the ghost when it is reached
                    if (!showGhost && ghost != null)
                    {
                        ghost.gameObject.SetActive(false);
                    }

                    IsMoving = false;
                    
                    break;
                }
                
                yield return null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckCollisionWithGhost(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            CheckCollisionWithGhost(other, false);
        }

        private void CheckCollisionWithGhost(Collider other, bool result)
        {
            if (other == ghost)
            {
                CollidingWithGhost = result;
            }
        }
    }
}
