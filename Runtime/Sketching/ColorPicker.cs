using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// The <c>ColorPicker</c> component is used to give <see cref="Pen"/>s a way to change their HSV hue and saturation.
    /// Used in conjunction with a <see cref="BrightnessPicker"/>.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ColorPicker : MonoBehaviour
    {
        private Material _material;
        
        private static readonly int Brightness = Shader.PropertyToID("Brightness");

        private void Awake()
        {
            _material = GetComponent<Renderer>().material;
            _material.SetFloat(Brightness, 1);
        }

        /// <summary>
        /// Updates the <c>Color Picker</c> material with a new brightness.
        /// </summary>
        public void UpdateColor(float brightness)
        {
            _material.SetFloat(Brightness, brightness);
        }
    }
}
