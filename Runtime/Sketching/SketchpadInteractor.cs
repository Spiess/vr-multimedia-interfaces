using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sketching
{
    /// <summary>
    /// The <c>SketchpadInteractor</c> is added to the XRRig to interact with <see cref="Sketchpad"/>s.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SketchpadInteractor : MonoBehaviour
    {
        /// <summary>
        /// The attachment point at which to attach picked up <see cref="SketchingUtensil"/>s to.
        /// </summary>
        [SerializeField] protected Transform attachmentPoint;
        /// <summary>
        /// The input action to select and drop <see cref="SketchingUtensil"/>s.
        /// </summary>
        [SerializeField] protected InputAction selectSketchingUtensil;
        /// <summary>
        /// The sketchpad interactor on the other hand. This allows for picking up <see cref="SketchingUtensil"/>s from
        /// the other hand.
        /// </summary>
        [SerializeField] protected SketchpadInteractor other;
        
        private SketchingUtensil Utensil { get; set; }
        private readonly List<SketchingUtensil> _collidingUtensils = new List<SketchingUtensil>();

        private void Start()
        {
            selectSketchingUtensil.canceled += _ =>
            {
                if (Utensil == null)
                {
                    if (_collidingUtensils.Count > 0)
                    {
                        // this interactor does not have a utensil selected yet and there are utensils close by, find
                        // the closest utensil
                        var closestCollidingUtensil = _collidingUtensils[0];
                        var smallestDistance = Vector3.Distance(closestCollidingUtensil.transform.position,
                            transform.position);
                        for (var i = 1; i < _collidingUtensils.Count; i++)
                        {
                            var utensil = _collidingUtensils[i];
                            var distance = Vector3.Distance(utensil.transform.position, transform.position);
                            if (distance < smallestDistance)
                            {
                                closestCollidingUtensil = utensil;
                                smallestDistance = distance;
                            }
                        }
                        Utensil = closestCollidingUtensil;

                        if (!Utensil.IsMoving) // don't attach the utensil if it's still being detached
                        {
                            // attach the utensil
                            if (Utensil.attachmentPoint != null)
                            {
                                attachmentPoint.localPosition = Utensil.attachmentPoint.localPosition;
                                attachmentPoint.localRotation = Utensil.attachmentPoint.localRotation;
                            }
                            else
                            {
                                attachmentPoint.localPosition = Vector3.zero;
                                attachmentPoint.localRotation = Quaternion.identity;
                            }
                            StartCoroutine(Utensil.PickUp(attachmentPoint));
                            
                            if (other.Utensil == Utensil) // this interactor just took the utensil from the other interactor
                            {
                                other.Utensil = null;
                            }
                        }
                    }
                }
                else if(!Utensil.IsMoving) // don't detach the utensil if it's still being attached
                {
                    if(Utensil.CollidingWithGhost) // a utility is currently selected and is colliding with its ghost
                    {
                        StartCoroutine(Utensil.PutBack());
                        Utensil = null;
                    }
                    else // a utility is currently selected but it isn't colliding with its ghost
                    {
                        Utensil.transform.SetParent(null);
                        Utensil = null;
                    }
                }
            };
            selectSketchingUtensil.Enable();
        }

        private void OnTriggerEnter(Collider other)
        {
            var utensil = other.GetComponent<SketchingUtensil>();
            if (utensil != null)
            {
                _collidingUtensils.Add(utensil);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var utensil = other.GetComponent<SketchingUtensil>();
            if (utensil != null)
            {
                _collidingUtensils.Remove(utensil);
            }
        }
    }
}
