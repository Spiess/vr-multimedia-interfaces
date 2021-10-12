using UnityEngine;
using UnityEngine.UI;
using Util;

namespace TextInput
{
    /// <summary>
    /// The <c>CircleKeyboardKey</c> allows selecting a symbol in a <see cref="CircleKeyboardGroup"/> on a
    /// <see cref="CircleKeyboard"/>.
    /// </summary>
    public class CircleKeyboardKey : MonoBehaviour
    {
        /// <summary>
        /// The character this key will type.
        /// </summary>
        public string character;
        /// <summary>
        /// The text displaying what character can be typed with this key.
        /// </summary>
        [SerializeField] protected Text text;
        /// <summary>
        /// The position of the text relative to the center of the keyboard.
        /// </summary>
        [SerializeField] protected Transform textTransform;
        
        /// <summary>
        /// Set whether the position on the keyboard is currently hovering over this key.
        /// </summary>
        public bool Hovering
        {
            set => _material.SetInt(HoveringProperty, value ? 1 : 0);
        }
        /// <summary>
        /// Set whether this key is shown (handled by the <see cref="CircleKeyboardGroup"/> this key is in).
        /// </summary>
        public bool Shown
        {
            set
            {
                _material.SetInt(ShownProperty, value ? 1 : 0);
                text.text = value ? character.ToUpper() : "";
            }
        }
        /// <summary>
        /// Handles transforming the desired polar text coordinates to the required cartesian coordinates of the
        /// <see cref="textTransform"/>.
        /// </summary>
        public Vector2 TextPosition
        {
            set => textTransform.localPosition = MathUtilities.PolarToCartesian(value);
        }
        public float PhiFrom
        {
            get => _phiFrom;
            set {
                _material.SetFloat(PhiFromProperty, value);
                _phiFrom = value;
            }
        }
        private float _phiFrom;
        public float PhiTo
        {
            get => _phiTo;
            set {
                _material.SetFloat(PhiToProperty, value);
                _phiTo = value;
            }
        }
        private float _phiTo;
        private Material _material;
        
        private static readonly int
            PhiFromProperty = Shader.PropertyToID("PhiFrom"),
            PhiToProperty = Shader.PropertyToID("PhiTo"),
            HoveringProperty = Shader.PropertyToID("Hovering"),
            ShownProperty = Shader.PropertyToID("Shown");

        private void Awake()
        {
            _material = GetComponent<Renderer>().material;
        }

        private void Start()
        {
            text.text = "";
        }
    }
}
