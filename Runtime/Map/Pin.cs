using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    /// <summary>
    /// The <c>Pin</c> can be dropped onto a <see cref="Map"/> to store and display a position on it.
    /// </summary>
    public class Pin : MonoBehaviour
    {
        /// <summary>
        /// This scales how much this <c>Pin</c> is being lifted to indicate how far away it is from the
        /// <see cref="Map"/> bounds (whenever the map is panned far enough for the <c>Pin</c> to leave its bounds).
        /// </summary>
        public float overshootPositionScale = 0.005f;
        /// <summary>
        /// This scales how much this <c>Pin</c> is being rotated away from the map to indicate how far away it is from
        /// the <see cref="Map"/> bounds (whenever the map is panned far enough for the <c>Pin</c> to leave its bounds).
        /// </summary>
        public float overshootRotationScale = 10;
        /// <summary>
        /// The transform of the billboard on which this <c>Pin</c>s coordinates are displayed on. Used to rotate it
        /// towards the user.
        /// </summary>
        public Transform coordinatesBillboard;
        /// <summary>
        /// The text on the billboard on which this <c>Pin</c>s coordinates are displayed on.
        /// </summary>
        public Text coordinatesText;
    
        /// <summary>
        /// A reference to the <see cref="Map"/> this <c>Pin</c> was dropped on.
        /// </summary>
        [HideInInspector] public Map map;
        /// <summary>
        /// The coordinates of this <c>Pin</c> on the <see cref="map"/>.
        /// </summary>
        [HideInInspector] public Vector2 coordinates;
        /// <summary>
        /// While dropping or moving the pin, it is bound to a <see cref="MapInteractor"/>, which updates its
        /// coordinates.
        /// </summary>
        [HideInInspector] public Transform boundTo;
        /// <summary>
        /// Stores whether this pin is currently on a <see cref="Map"/> and inside its visible bounds.
        /// </summary>
        [HideInInspector] public bool inMapBounds;
        /// <summary>
        /// If this pin is part of a pin trail, <c>prev</c> and <c>next</c> are its neighbours in the trail.
        /// </summary>
        [HideInInspector] public Pin prev, next;

        /// <summary>
        /// The camera to rotate this <c>Pin</c>s coordinate billboard towards.
        /// </summary>
        private Transform _camera;
        private LineRenderer _line;

        private void Start()
        {
            SetTransform();

            // set the camera to the main camera the user is looking through
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _camera = mainCamera.transform;
            }

            _line = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            SetTransform();

            // rotate the billboard towards the user and update its coordinate display
            coordinatesBillboard.rotation =
                Quaternion.LookRotation(transform.position - _camera.position, Vector3.up);
            coordinatesText.text = $"{coordinates.x}, {coordinates.y}";
            
            // draw a line to the next pin if it exists
            if (map != null && next != null)
            {
                Transform thisTransform = transform, nextTransform = next.transform;
                if (next.map == null)
                {
                    // if a pin in a trail is lifted off the map, connections to it shouldn't be drawn
                    nextTransform = thisTransform;
                }
                
                _line.enabled = true;
                _line.SetPosition(0, thisTransform.position + 0.018f * thisTransform.up);
                _line.SetPosition(1, nextTransform.position + 0.018f * nextTransform.up);
            }
            else
            {
                _line.enabled = false;
            }
        }

        private void SetTransform()
        {
            if (map != null)
            {
                if (boundTo != null)
                {
                    coordinates = map.PositionToCoordinates(boundTo.position);
                }
        
                transform.position = map.CoordinatesToPosition(coordinates);

                // check whether this pin is outside the map bounds
                var offset = transform.position - map.transform.position;
                var max = map.maxDistance + map.falloffRange;
                inMapBounds = offset.magnitude <= max;
                if (!inMapBounds)
                {
                    // this pin is outside the map bounds - slightly rotate it and lift it up to indicate how far outside
                    // the map bounds it is
                    var overshoot = Mathf.Log(1 + 100 * (offset.magnitude - max));
                    offset.Normalize();
                    transform.position = map.transform.position + offset * max;
                    transform.Translate(new Vector3(0, overshoot * overshootPositionScale, 0), transform.parent);
                    transform.rotation =
                        Quaternion.LookRotation(offset) *
                        Quaternion.AngleAxis(Mathf.Clamp(overshoot * overshootRotationScale, 0, 45), Vector3.right);
                }
                else
                {
                    transform.rotation = Quaternion.identity;
                }
            }
            else // pin is not on a map currently
            {
                transform.position = new Vector3(1000000, 0, 0);
                inMapBounds = false;
            }
        }

        public void Destroy()
        {
            if (next != null)
            {
                next.prev = null;
            }
            if (prev != null)
            {
                prev.next = null;
            }
            Destroy(gameObject);
        }

        #region Trails

        public void AddNext(Pin pin)
        {
            next = pin;
            pin.prev = this;
        }

        public void AddPrev(Pin pin)
        {
            prev = pin;
            pin.next = this;
        }

        public bool HasFreeNeighbour()
        {
            return prev == null || next == null;
        }

        #endregion
    }
}
