using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

namespace Map
{
    [RequireComponent(typeof(Collider))]
    public class MinimapInteractor : MonoBehaviour
    {
        /// <summary>
        /// The input action for rotating the globe.
        /// </summary>
        [SerializeField] protected InputAction drag;
        /// <summary>
        /// The input action for choosing a location.
        /// </summary>
        [SerializeField] protected InputAction dropPin;
        /// <summary>
        /// The attachment point at which to attach picked up <see cref="MinimapPin"/>s to.
        /// </summary>
        [SerializeField] protected Transform attachmentPoint;
        /// <summary>
        /// The prefab to choose a location with.
        /// </summary>
        [SerializeField] protected MinimapPin pinPrefab;

        private bool _hovering, _dragging;
        private Minimap _activeMinimap;
        private PinBox _activePinBox;
        private MinimapPin _draggingPin;
        private Vector3 _startPositionSpherical;
        private Quaternion _startRotation;

        private void Start()
        {
            drag.started += _ =>
            {
                if (_hovering && !_dragging)
                {
                    _startPositionSpherical = PositionSpherical();
                    _startRotation = _activeMinimap.minimapPitch.localRotation;
                    _dragging = true;
                }
            };
            drag.canceled += _ =>
            {
                _dragging = false;
            
                if (!_hovering)
                {
                    // if we let go after leaving the minimap bounds, we don't need the reference anymore
                    _activeMinimap = null;
                }
            };
            drag.Enable();

            dropPin.started += _ =>
            {
                if (_activePinBox != null)
                {
                    _draggingPin = Instantiate(pinPrefab, attachmentPoint, true);
                    _draggingPin.boundTo = attachmentPoint;
                }
            };
            dropPin.canceled += _ =>
            {
                if (_draggingPin != null)
                {
                    if (_draggingPin.minimap != null && _hovering) // pin is on a map
                    {
                        _activeMinimap.MoveTo(_draggingPin.coordinates);
                    }
                    Destroy(_draggingPin.gameObject);
                    _draggingPin = null;
                }
            };
            dropPin.Enable();
        }

        private void Update()
        {
            if (_dragging)
            {
                // update interactor axis
                _activeMinimap.interactorAxis.LookAt(transform.position);
                
                var delta = PositionSpherical() - _startPositionSpherical;
                _activeMinimap.minimapTransform.Rotate(_activeMinimap.minimapTransform.up,
                    MathUtilities.RadianToDegrees(delta.z));
                _activeMinimap.minimapPitch.localRotation = _startRotation;
                _activeMinimap.minimapPitch.Rotate(_activeMinimap.interactorAxis.right,
                    MathUtilities.RadianToDegrees(delta.y), Space.World);
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            var minimap = otherCollider.gameObject.GetComponent<Minimap>();
            if (minimap != null)
            {
                _activeMinimap = minimap;
                _hovering = true;
                
                if (_draggingPin != null)
                {
                    _draggingPin.minimap = _activeMinimap;
                    _draggingPin.transform.parent = _activeMinimap.transform;
                }
            }
            
            var map = otherCollider.gameObject.GetComponent<Map>();
            if (map != null)
            {
                if (_draggingPin != null)
                {
                    _draggingPin.boundTo = null;
                }
            }
            
            var pinBox = otherCollider.gameObject.GetComponent<PinBox>();
            if (pinBox != null)
            {
                _activePinBox = pinBox;
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            var minimap = otherCollider.gameObject.GetComponent<Minimap>();
            if (minimap != null)
            {
                if (!_dragging) // if we're still dragging, we'll need the references
                {
                    _activeMinimap = null;
                }
                _hovering = false;
                
                if (_draggingPin != null)
                {
                    _draggingPin.minimap = null;
                    _draggingPin.transform.parent = attachmentPoint;
                }
            }
            
            var map = otherCollider.gameObject.GetComponent<Map>();
            if (map != null)
            {
                if (_draggingPin != null)
                {
                    _draggingPin.boundTo = attachmentPoint;
                }
            }
            
            var pinBox = otherCollider.gameObject.GetComponent<PinBox>();
            if (pinBox != null)
            {
                _activePinBox = null;
            }
        }

        private Vector3 PositionSpherical()
        {
            if (_activeMinimap == null)
            {
                throw new Exception("Cannot get spherical position unless this interactor is hovering over a minimap.");
            }

            return MathUtilities.CartesianToSpherical(
                _activeMinimap.minimapTransform.InverseTransformPoint(transform.position));
        }
    }
}
