using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// The <c>Eraser</c> is a <see cref="SketchingUtensil"/> erases any color from a <see cref="Sketchpad"/>.
    /// It uses the angle of the eraser relative to its movement as width.
    /// </summary>
    public class Eraser : SketchingUtensil
    {
        /// <summary>
        /// The minimum and maximum thickness of the eraser.
        /// </summary>
        /// <remarks>
        /// Note that the actual size of the eraser depends on both the thickness and the resolution of the <see cref="Sketchpad"/>.
        /// </remarks>
        public int minThickness, maxThickness;
        /// <summary>
        /// The distance of the eraser tip from its origin determines how far the ray is cast.
        /// </summary>
        public float tipDistance;

        private Sketchpad _currentSketchpad;
        private Vector2 _currentPositionOnSurface;

        private void Update()
        {
            var t = transform;
            var ray = new Ray(t.position, t.up);

            // don't continue if eraser is not touching anything
            if (!Physics.Raycast(ray, out var rayHit, tipDistance))
            {
                EraserLifted();
                
                return;
            }

            // check for sketch surface
            var sketchpad = rayHit.transform.GetComponent<Sketchpad>();
            if (sketchpad != null)
            {
                var strokeWidth = minThickness + Vector3.Angle(ray.direction, sketchpad.surface.transform.forward) / 90f * (maxThickness - minThickness);
                
                if (_currentSketchpad != null && sketchpad.Equals(_currentSketchpad)) // the eraser has already been on this surface last update
                {
                    var newPositionOnSurface = _currentSketchpad.PositionOnSurface(rayHit.point);
                    _currentSketchpad.DrawLine(_currentPositionOnSurface, newPositionOnSurface, strokeWidth, Color.white);
                    _currentPositionOnSurface = newPositionOnSurface;
                }
                else
                {
                    _currentSketchpad = sketchpad;
                    _currentPositionOnSurface = _currentSketchpad.PositionOnSurface(rayHit.point);
                }
            }
            else
            {
                // treat the eraser touching an object that has no interactions with it as if it weren't touching anything
                EraserLifted();
            }
        }

        private void EraserLifted()
        {
            _currentSketchpad = null;
            _currentPositionOnSurface = Vector2.zero;
        }
    }
}
