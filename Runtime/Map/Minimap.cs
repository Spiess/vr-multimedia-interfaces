using UnityEngine;

namespace Map
{
    public class Minimap : MonoBehaviour
    {
        [Tooltip("The zoom level to set the map to when teleporting to a location via the minimap.")]
        public float zoomTo;
        public Transform minimapPitch;
        public Transform minimapTransform;
        public Transform minimapScale;
        public Transform interactorAxis;
        public Map map;

        private void Start()
        {
            var scale = transform.localScale;
            var distance = map.falloffRange + map.maxDistance + scale.z / 2;
            minimapTransform.localPosition = new Vector3(0, scale.y / 2, distance);
            minimapScale.localScale /= map.transform.localScale.x;
        }

        public void MoveTo(Vector2 coordinates)
        {
            map.Coordinates = coordinates;
            map.Zoom = zoomTo;
        }
    }
}
