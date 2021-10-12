using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// The <c>BrightnessPicker</c> component is used to give <see cref="Pen"/>s a way to change their HSV brightness.
    /// Used in conjunction with a <see cref="ColorPicker"/>.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BrightnessPicker : MonoBehaviour
    {
        private Material _material;
        
        private static readonly int
            Hue = Shader.PropertyToID("Hue"),
            Saturation = Shader.PropertyToID("Saturation");

        private void Awake()
        {
            _material = GetComponent<Renderer>().material;
            _material.SetFloat(Hue, 0);
            _material.SetFloat(Saturation, 0);
        }

        /// <summary>
        /// Updates the <c>Brightness Picker</c> material with a new hue and saturation.
        /// </summary>
        public void UpdateColor(float hue, float saturation)
        {
            _material.SetFloat(Hue, hue);
            _material.SetFloat(Saturation, saturation);
        }
    }
}
