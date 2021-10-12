using System;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace TextInput
{
    public enum CircleKeyboardGroupState
    {
        Inactive = 0, Hovering = 1, Selected = 2
    }
    
    /// <summary>
    /// The <c>CircleKeyboardGroup</c> is a 3-symbol group of <see cref="CircleKeyboardKey"/>s on a
    /// <see cref="CircleKeyboard"/>.
    /// </summary>
    public class CircleKeyboardGroup : MonoBehaviour
    {
        /// <summary>
        /// The text shown for this group.
        /// </summary>
        public Text text;
        /// <summary>
        /// The position of the text relative to the center of the keyboard.
        /// </summary>
        [SerializeField] protected Transform textTransform;
        
        /// <summary>
        /// The three <see cref="CircleKeyboardKey"/>s in this group.
        /// </summary>
        [HideInInspector] public CircleKeyboardKey left, center, right;
        
        /// <summary>
        /// The state of this group. Handles showing or hiding of the keys in this group and its appearance.
        /// </summary>
        public CircleKeyboardGroupState State
        {
            get => _state;
            set
            {
                var showKeys = value == CircleKeyboardGroupState.Selected;
                left.Shown = showKeys;
                center.Shown = showKeys;
                right.Shown = showKeys;
                _material.SetFloat(StateProperty, Convert.ToInt32(value));
                _state = value;
            }
        }
        private CircleKeyboardGroupState _state;
        /// <summary>
        /// The currently selected key. Keeps track of un-hovering all other keys when a new one is hovered.
        /// </summary>
        public CircleKeyboardKey SelectedKey
        {
            get => _selectedKey;
            set
            {
                left.Hovering = false;
                center.Hovering = false;
                right.Hovering = false;

                _selectedKey = value;
                _selectedKey.Hovering = true;
            }
        }
        private CircleKeyboardKey _selectedKey;
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
            StateProperty = Shader.PropertyToID("State");

        private void Awake()
        {
            _material = GetComponent<Renderer>().material;
        }

        /// <summary>
        /// Assign a <see cref="CircleKeyboardKey"/> in this group by its index.
        /// </summary>
        /// <param name="key">The key to add to the group.</param>
        /// <param name="index">The index (-1 for left, 0 for center and 1 for right) of the new key.</param>
        /// <exception cref="ArgumentOutOfRangeException">For any index except for -1, 0 or 1.</exception>
        public void SetKey(CircleKeyboardKey key, int index)
        {
            switch (index)
            {
                case -1:
                    left = key;
                    break;
                case 0:
                    center = key;
                    break;
                case 1:
                    right = key;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }
        }
    }
}
