using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Map
{
    /// <summary>
    /// The <c>MinimapPin</c> is used to choose a location on a <see cref="Minimap"/>.
    /// </summary>
    public class MinimapPin : MonoBehaviour
    {
        /// <summary>
        /// A reference to the <see cref="Map"/> this <c>Pin</c> was dropped on.
        /// </summary>
        [HideInInspector] public Minimap minimap;
        /// <summary>
        /// The coordinates of this <c>Pin</c> on the <see cref="minimap"/>.
        /// </summary>
        [HideInInspector] public Vector2 coordinates;
        /// <summary>
        /// While moving the pin, it is bound to a <see cref="MinimapInteractor"/>, which updates its coordinates.
        /// </summary>
        [HideInInspector] public Transform boundTo;

        private Vector3 _positionSpherical;

        private void Start()
        {
            SetTransform();
        }
        
        private void Update()
        {
            SetTransform();
        }
        
        private void SetTransform()
        {
            if (boundTo != null)
            {
                if (minimap != null)
                {
                    _positionSpherical =
                        MathUtilities.CartesianToSpherical(
                            minimap.minimapPitch.InverseTransformPoint(boundTo.transform.position));
                
                    transform.localPosition =
                        MathUtilities.SphericalToCartesian(new Vector3(.5f, _positionSpherical.y,
                            _positionSpherical.z));
                    transform.LookAt(minimap.transform);
                
                    // translate the spherical coordinates to mapbox latitude and longitude
                    coordinates = new Vector2(90 - _positionSpherical.y / Mathf.PI * 180,
                        -_positionSpherical.z / Mathf.PI * 180);
                }
                else
                {
                    transform.position = boundTo.position;
                    transform.rotation = boundTo.rotation;
                }
            }
            else
            {
                transform.position = new Vector3(1000000, 0, 0);
            }
        }
    }
}
