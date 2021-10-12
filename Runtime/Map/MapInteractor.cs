using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    internal enum MapInteractorState
    {
        /// <summary>
        /// This <see cref="MapInteractor"/> is currently not doing anything.
        /// </summary>
        Inactive,
        /// <summary>
        /// This <see cref="MapInteractor"/> is currently dragging a <see cref="Map"/>.
        /// </summary>
        Dragging,
        /// <summary>
        /// There is already a <see cref="MapInteractor"/> dragging a <see cref="Map"/>, so this one is zooming it. 
        /// </summary>
        Scaling
    }
    
    /// <summary>
    /// The <c>MapInteractor</c> is added to the XRRig to interact with <see cref="Map"/>s.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MapInteractor : MonoBehaviour
    {
        /// <summary>
        /// This timeout prevents the map from being accidentally panned when quickly zooming in and letting go of both
        /// interactors.
        /// </summary>
        public float scalingTimeout = 0.1f;
        /// <summary>
        /// The input action for dragging the map.
        /// </summary>
        [SerializeField] protected InputAction drag;
        /// <summary>
        /// The input action for dropping pins.
        /// </summary>
        [SerializeField] protected InputAction dropPin;
        /// <summary>
        /// The input action for starting pin trails.
        /// </summary>
        [SerializeField] protected InputAction pinTrail;
        /// <summary>
        /// The prefab to place on the map as pins.
        /// </summary>
        [SerializeField] protected Pin pinPrefab;
        /// <summary>
        /// The map interactor on the other hand. Required for zooming the map.
        /// </summary>
        [SerializeField] protected MapInteractor other;

        private bool Inactive => State == MapInteractorState.Inactive;
        private bool Dragging => State == MapInteractorState.Dragging;
        private bool Scaling => State == MapInteractorState.Scaling;
        private MapInteractorState State
        {
            get => _state;
            set
            {
                switch (value)
                {
                    case MapInteractorState.Inactive:
                        break;
                    
                    case MapInteractorState.Dragging:
                        _startPosition = _activeMap.PositionToCoordinates(transform.position);
                        // we want to cancel any previously applied velocity on the map when we grab it
                        _activeMap.velocityCenter.velocity = Vector3.zero;
                        break;
                    
                    case MapInteractorState.Scaling:
                        Vector3 position = transform.position, otherPosition = other.transform.position;
                        _lastDistance = Vector3.Distance(position, otherPosition);                        
                        // if the map is being scaled at the same time, we want the position to be determined by the
                        // average position of both interactors, thus we also need the average starting position
                        other._startPosition = _activeMap.PositionToCoordinates((position + otherPosition) / 2.0f);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                _state = value;
            }
        }
        private MapInteractorState _state = MapInteractorState.Inactive;
        private bool _hovering;
        private Map _activeMap;
        private PinBox _activePinBox;
        private Pin _draggingPin;
        private List<Pin> _closePins = new List<Pin>();
        // dragging
        private Vector2 _startPosition;
        private Vector3 _lastPosition, _velocity;
        private bool _wasScaling;
        // scaling
        private float _lastDistance;
        
        private void Start()
        {
            drag.started += _ =>
            {
                var closestPin = GetClosestPin();
                
                if (closestPin != null) // move a pin
                {
                    _draggingPin = closestPin;
                    _draggingPin.boundTo = transform;
                }
                else if (_hovering && Inactive) // move or scale the map
                {
                    // if the other interactor is already dragging, we want to start scaling. if not, we start dragging
                    State = other.Dragging ? MapInteractorState.Scaling : MapInteractorState.Dragging;
                }
            };
            drag.canceled += _ =>
            {
                if (!Inactive)
                {
                    if (Dragging)
                    {
                        if (other.Scaling)
                        {
                            // if the other interactor was scaling, it not becomes the new dragging interactor
                            other.State = MapInteractorState.Dragging;
                            other.StartCoroutine(other.ScalingTimeout());
                        }
                        else if (!_wasScaling) // only apply the dragging impulse if the interactor wasn't scaling
                        {
                            // when we stop dragging, we apply an impulse to the map equal to the interactor velocity
                            _activeMap.velocityCenter.AddForce(-_velocity.x, 0, -_velocity.z, ForceMode.Impulse);
                        }
                    } else if (Scaling)
                    {
                        // this interactor just stopped scaling, reset the dragging position of the other interactor
                        other._startPosition = _activeMap.PositionToCoordinates(other.transform.position);
                        other.StartCoroutine(other.ScalingTimeout());
                    }

                    State = MapInteractorState.Inactive;
                }
                
                if (!_hovering)
                {
                    // if we let go after leaving the map bounds, we don't need the reference anymore
                    _activeMap = null;
                }
            };
            drag.Enable();
        
            dropPin.started += _ =>
            {
                if (_activePinBox != null)
                {
                    _draggingPin = InstantiatePin(transform);
                }
            };
            dropPin.canceled += _ =>
            {
                ReleaseDraggingPin();
            };
            dropPin.Enable();
        
            pinTrail.started += _ =>
            {
                var closestPin = GetClosestPin();
                
                if (_activeMap != null && closestPin != null && closestPin.HasFreeNeighbour())
                {
                    _draggingPin = InstantiatePin(transform);
                    _activeMap.AddPin(_draggingPin);
                    _draggingPin.transform.parent = _activeMap.transform;
                    
                    if (closestPin.next == null)
                    {
                        closestPin.AddNext(_draggingPin);
                    }
                    else
                    {
                        closestPin.AddPrev(_draggingPin);
                    }
                }
            };
            pinTrail.canceled += _ =>
            {
                ReleaseDraggingPin();
            };
            pinTrail.Enable();
        
            _lastPosition = transform.position;
        }
        
        private void Update()
        {
            if (Dragging)
            {
                var dragPosition = transform.position;
                if (other.Scaling)
                {
                    // if the map is being scaled at the same time, we want the position to be determined by the average
                    // position of both interactors
                    dragPosition += other.transform.position;
                    dragPosition /= 2.0f;
                }
                
                _activeMap.Translate(_startPosition - _activeMap.PositionToCoordinates(dragPosition));
            }
            else if (Scaling)
            {
                var newDistance = Vector3.Distance(transform.position, other.transform.position);
                
                // zoom the map by log2 (because the map zoom is determined by 2^zoom) of the relative scaling change
                _activeMap.DeltaZoom(Mathf.Log(newDistance / _lastDistance, 2));
                _lastDistance = newDistance;
            }
        }
        
        private void FixedUpdate()
        {
            if (Dragging)
            {
                // if this interactor is dragging, we'll need to keep track of its velocity to impulse the map after
                // letting go
                var newPosition = transform.position;
                _velocity = (newPosition - _lastPosition) / Time.fixedDeltaTime;
                _lastPosition = newPosition;
            }
        }
        
        private void OnTriggerEnter(Collider otherCollider)
        {
            var map = otherCollider.gameObject.GetComponent<Map>();
            if (map != null)
            {
                _activeMap = map;
                _hovering = true;
                
                if (_draggingPin != null)
                {
                    _activeMap.AddPin(_draggingPin);
                    _draggingPin.transform.parent = _activeMap.transform;
                }
            }
            
            var pinBox = otherCollider.gameObject.GetComponent<PinBox>();
            if (pinBox != null)
            {
                _activePinBox = pinBox;
            }
            
            var pin = otherCollider.gameObject.GetComponent<Pin>();
            if (pin != null)
            {
                if (pin.inMapBounds)
                {
                    _closePins.Add(pin);
                }
            }
        }
        
        private void OnTriggerExit(Collider otherCollider)
        {
            var map = otherCollider.gameObject.GetComponent<Map>();
            if (map != null)
            {
                if (Inactive) // if we're still dragging or scaling, we'll need the reference to the map
                {
                    _activeMap = null;
                }
                _hovering = false;
                
                if (_draggingPin != null)
                {
                    map.RemovePin(_draggingPin);
                    _draggingPin.transform.parent = transform;
                }
            }
            
            var pinBox = otherCollider.gameObject.GetComponent<PinBox>();
            if (pinBox != null)
            {
                _activePinBox = null;
            }
            
            var pin = otherCollider.gameObject.GetComponent<Pin>();
            if (pin != null)
            {
                _closePins.Remove(pin);
            }
        }

        private void ReleaseDraggingPin()
        {
            if (_draggingPin != null)
            {
                if (_draggingPin.map != null) // pin is on a map
                {
                    // unbind the pin from this interactor, so it stays at its position on the map
                    _draggingPin.boundTo = null;
                }
                else // pin is not on a map
                {
                    _closePins.Remove(_draggingPin);
                    _draggingPin.Destroy();
                }
                _draggingPin = null;
            }
        }

        private Pin InstantiatePin(Transform bindTo)
        {
            var pin = Instantiate(pinPrefab, transform, true);
            if (bindTo != null)
            {
                pin.boundTo = bindTo;
            }
            return pin;
        }

        private Pin GetClosestPin()
        {
            Pin closest = null;
            var closestDistance = float.MaxValue;

            foreach (var pin in _closePins)
            {
                var distance = Vector3.Distance(transform.position, pin.transform.position);
                
                if (distance < closestDistance)
                {
                    closest = pin;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private IEnumerator ScalingTimeout()
        {
            _wasScaling = true;
            yield return new WaitForSeconds(scalingTimeout);
            _wasScaling = false;
        }
    }
}
