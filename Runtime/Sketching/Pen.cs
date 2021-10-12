using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// The <c>Pen</c> is a <see cref="SketchingUtensil"/> that draws on <see cref="Sketchpad"/>s with a color from <see cref="Paint"/> or a <see cref="ColorPicker"/> and <see cref="BrightnessPicker"/>.
    /// It uses the angle of the pen relative to the drawing surface for the stroke width.
    /// </summary>
    public class Pen : SketchingUtensil
    {
        /// <summary>
        /// The minimum and maximum thickness of the drawn stroke in pixels.
        /// </summary>
        /// <remarks>
        /// Note that the actual size of the stroke depends on both the thickness and the resolution of the <see cref="Sketchpad"/>.
        /// </remarks>
        public int minThickness, maxThickness;
        /// <summary>
        /// The color this pen starts with before any other color is picked with it.
        /// </summary>
        public Color defaultColor;
        /// <summary>
        /// The distance of the pen tip from its origin determines how far the ray is cast.
        /// </summary>
        public float tipDistance;
        /// <summary>
        /// The color indicators that will be updated to the color of this pen.
        /// </summary>
        [SerializeField] protected Renderer[] colorIndicators;

        private Color CurrentColor
        {
            get => _currentColor;
            set
            {
                _currentColor = value;
                
                // backwards calculate the HSV color.
                Color.RGBToHSV(_currentColor, out _hsv.x, out _hsv.y, out _hsv.z);
                
                // update the color indicator
                foreach (var material in _colorIndicatorMaterials)
                {
                    material.color = value;
                    material.SetColor(EmissionColor, value);
                }
                
                // update any brightness and color pickers in the scene
                foreach (var brightnessPicker in FindObjectsOfType<BrightnessPicker>())
                {
                    brightnessPicker.UpdateColor(_hsv.x, _hsv.y);
                }
                foreach (var brightnessPicker in FindObjectsOfType<ColorPicker>())
                {
                    brightnessPicker.UpdateColor(_hsv.z);
                }
            }
        }
        private Color _currentColor;
        private Sketchpad _currentSketchpad;
        private Vector2 _currentPositionOnSurface;
        private Vector3 _hsv;
        private Material[] _colorIndicatorMaterials;
        
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        private void Start()
        {
            _colorIndicatorMaterials = new Material[colorIndicators.Length];
            for (var i = 0; i < colorIndicators.Length; i++)
            {
                _colorIndicatorMaterials[i] = colorIndicators[i].material;
            }
            CurrentColor = defaultColor;
        }

        private void Update()
        {
            var t = transform;
            var ray = new Ray(t.position, t.up);

            // don't continue if pen is not touching anything
            if (!Physics.Raycast(ray, out var rayHit, tipDistance))
            {
                PenLifted();
                
                return;
            }
            
            // check for paint change
            var paint = rayHit.transform.GetComponent<Paint>();
            if (paint != null)
            {
                CurrentColor = paint.paintColor;
                
                return;
            }
            
            // check for color picker
            var colorPicker = rayHit.transform.GetComponent<ColorPicker>();
            if (colorPicker != null)
            {
                // calculate the local position on the color picker
                var localPosition = rayHit.transform.InverseTransformPoint(rayHit.point);
                var positionOnPalette = new Vector2(localPosition.x, -localPosition.z);
                // convert the cartesian coordinates to polar coordinates (angle and radius) to be used as hue and saturation respectively
                _hsv.x = 0.5f - Mathf.Atan2(positionOnPalette.x, positionOnPalette.y) / 2.0f / Mathf.PI;
                _hsv.y = positionOnPalette.magnitude * 2; // capsule diameter is 1, we want a max radius of 1, so we multiply by 2
                
                CurrentColor = Color.HSVToRGB(_hsv.x, _hsv.y, _hsv.z);
                
                return;
            }
            
            // check for brightness picker
            var brightnessPicker = rayHit.transform.GetComponent<BrightnessPicker>();
            if (brightnessPicker != null)
            {
                // the brightness is chosen along the y axis of the brightness picker
                _hsv.z = 0.5f + rayHit.transform.InverseTransformPoint(rayHit.point).y / 2; // capsule length is 2, scale to 1
                
                CurrentColor = Color.HSVToRGB(_hsv.x, _hsv.y, _hsv.z);
                
                return;
            }

            // check for sketch surface
            var sketchpad = rayHit.transform.GetComponent<Sketchpad>();
            if (sketchpad != null)
            {
                var strokeWidth = minThickness + Vector3.Angle(ray.direction, sketchpad.surface.transform.forward) / 90f * (maxThickness - minThickness);
                
                if (_currentSketchpad != null && sketchpad.Equals(_currentSketchpad)) // the pen has already been on this surface last update
                {
                    var newPositionOnSurface = _currentSketchpad.PositionOnSurface(rayHit.point);
                    _currentSketchpad.DrawLine(_currentPositionOnSurface, newPositionOnSurface, strokeWidth, CurrentColor);
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
                // treat the pen touching an object that has no interactions with it as if it weren't touching anything
                PenLifted();
            }
        }

        private void PenLifted()
        {
            _currentSketchpad = null;
            _currentPositionOnSurface = Vector2.zero;
        }
    }
}
