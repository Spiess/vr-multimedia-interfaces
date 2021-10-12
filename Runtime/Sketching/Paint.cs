using UnityEngine;

namespace Sketching
{
    /// <summary>
    /// The <c>Paint</c> component is used to give objects a static colour that can be picked with a <see cref="Pen"/>.
    /// For a dynamic color picker see <see cref="ColorPicker"/> and <see cref="BrightnessPicker"/>.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class Paint : MonoBehaviour
    {
        /// <summary>
        /// The color of this paint. Used by the <see cref="Pen"/> to change color.
        /// </summary>
        public Color paintColor;

        private void Start()
        {
            GetComponent<Renderer>().material.color = paintColor;
        }
    }
}
