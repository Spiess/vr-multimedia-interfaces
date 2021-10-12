using UnityEngine;

namespace Map
{
    public class Minimap : MonoBehaviour
    {
        [Tooltip("The zoom level to set the map to when teleporting to a location via the minimap.")]
        public float zoomTo;
        public Map map;

        public void MoveTo(Vector2 coordinates)
        {
            map.Coordinates = coordinates;
            map.Zoom = zoomTo;
        }
    }
}
