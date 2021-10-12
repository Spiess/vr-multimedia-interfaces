using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Util;

namespace TextInput
{
    /// <summary>
    /// The circle keyboard generates a circular keyboard to be typed with on a controller trackpad.
    /// </summary>
    public class CircleKeyboard : MonoBehaviour
    {
        /// <summary>
        /// The number of 3-symbol groups on this keyboard.
        /// </summary>
        private const int NumberOfGroups = 9;
        /// <summary>
        /// The percentage size of one 3-symbol group on this keyboard.
        /// </summary>
        private const float GroupSize = 1.0f / NumberOfGroups;

        /// <summary>
        /// The radius below which to cancel selecting a symbol.
        /// </summary>
        public float cancelRadius;
        /// <summary>
        /// The radius of displayed keys.
        /// </summary>
        public float keyDistance;
        /// <summary>
        /// The radius of symbol groups, and thus the main radius of the keyboard when not selecting a key.
        /// </summary>
        public float groupDistance;
        /// <summary>
        /// The characters on this keyboard.
        /// </summary>
        public List<char> alphabet;
        /// <summary>
        /// The input action to start selecting.
        /// </summary>
        [SerializeField] protected InputAction start;
        /// <summary>
        /// The input action delivering the position of the thumb on the trackpad.
        /// </summary>
        [SerializeField] protected InputAction position;
        /// <summary>
        /// The input action with which to confirm a selection.
        /// </summary>
        [SerializeField] protected InputAction confirm;
        /// <summary>
        /// The container in which <see cref="CircleKeyboardGroup"/>s and <see cref="CircleKeyboardKey"/>s are placed.
        /// </summary>
        [SerializeField] protected Transform container;
        [SerializeField] protected CircleKeyboardGroup groupPrefab;
        [SerializeField] protected CircleKeyboardKey keyPrefab;
        /// <summary>
        /// Currently, this keyboard just writes on a single input field for testing purposes.
        /// </summary>
        [SerializeField] protected InputField testInputField;

        /// <summary>
        /// The position of the thumb on this keyboard in polar coordinates.
        /// </summary>
        /// <remarks>
        /// The position is automatically transformed to polar coordinates, so when setting it, use cartesian
        /// coordinates.
        /// </remarks>
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = MathUtilities.CartesianToPolar(value);
                _position.y = 0.5f + _position.y / 2 / Mathf.PI;
                UpdateKeyboard();
            }
        }
        private Vector2 _position;
        private bool _selecting;
        private float _selectedPhi;
        private CircleKeyboardGroup _selectedGroup;
        private CircleKeyboardGroup[] _groups;
        private CircleKeyboardKey[] _keys;

        private void Start()
        {
            // prepare input actions
            
            start.canceled += context =>
            {
                foreach (var group in _groups)
                {
                    group.State = CircleKeyboardGroupState.Inactive;
                }
            };
            start.Enable();

            position.performed += context =>
            {
                Position = context.ReadValue<Vector2>();
            };
            position.Enable();

            confirm.started += _ =>
            {
                _selecting = true;
                _selectedPhi = Position.y;
                UpdateKeyboard();
            };
            confirm.canceled += _ =>
            {
                if (_selectedGroup != null)
                {
                    Write(_selectedGroup.SelectedKey.character);
                }
                DeselectAllGroups();
                UpdateKeyboard();
            };
            confirm.Enable();
            
            // instantiate groups and keys

            _groups = new CircleKeyboardGroup[NumberOfGroups];
            _keys = new CircleKeyboardKey[NumberOfGroups * 3];
            
            // groups and keys are arranged in a circular pattern
            var characterCounter = 0;
            for (var i = 0; i < NumberOfGroups; i++) // iterate over groups
            {
                var characters = new List<char>(3);
                var group = Instantiate(groupPrefab, container);
                group.PhiFrom = i * GroupSize;
                group.PhiTo = (i + 1) * GroupSize;
                group.TextPosition =
                    new Vector2(groupDistance, (0.75f - (i * GroupSize + GroupSize / 2)) * 2 * Mathf.PI);
                _groups[i] = group;

                for (var j = -1; j <= 1; j++) // iterate over keys in group
                {
                    var key = Instantiate(keyPrefab, container);
                    var k = i + j;
                    // handle keys that are (by value) at the other end of the circle
                    if (k < 0)
                    {
                        k += NumberOfGroups;
                    } else if (k >= NumberOfGroups)
                    {
                        k -= NumberOfGroups;
                    }
                    key.PhiFrom = k * GroupSize;
                    key.PhiTo = (k + 1) * GroupSize;
                    key.TextPosition =
                        new Vector2(keyDistance, (0.75f - (k * GroupSize + GroupSize / 2)) * 2 * Mathf.PI);
                    var character = alphabet[characterCounter];
                    characters.Add(character);
                    key.character = Convert.ToString(character);
                    characterCounter++;
                    group.SetKey(key, j);
                    _keys[i * 3 + (j + 1)] = key;
                }

                group.text.text = $"{characters[0]} {characters[1]} {characters[2]}";
            }
        }

        private void UpdateKeyboard()
        {
            if (_position.x < cancelRadius)
            {
                DeselectAllGroups();
                return;
            }
            
            foreach (var group in _groups)
            {
                // if a group has been selected already, use the stored phi to allow the position to be moved for
                // selecting the key
                var phi = _selecting ? _selectedPhi : _position.y;
                if (phi >= group.PhiFrom && phi <= group.PhiTo)
                {
                    if (_selecting)
                    {
                        _selectedGroup = group;
                        _selectedGroup.State = CircleKeyboardGroupState.Selected;
                        
                        // using a relative phi from the groups position, allows the thumb to go any distance in the
                        // direction of another letter (up to half the circle)
                        var relativePhi = _position.y - _selectedPhi;
                        if (relativePhi >= -GroupSize/2 && relativePhi <= GroupSize/2)
                        {
                            _selectedGroup.SelectedKey = _selectedGroup.center;
                        }
                        else if (relativePhi > -0.5f && relativePhi < 0 || relativePhi > 0.5f)
                        {
                            _selectedGroup.SelectedKey = _selectedGroup.left;
                        }
                        else
                        {
                            _selectedGroup.SelectedKey = _selectedGroup.right;
                        }
                    }
                    else
                    {
                        group.State = CircleKeyboardGroupState.Hovering;
                    }
                }
                else
                {
                    // make all other groups inactive that might have been hovered over before
                    group.State = CircleKeyboardGroupState.Inactive;
                }
            }
        }

        private void DeselectAllGroups()
        {
            foreach (var group in _groups)
            {
                group.State = CircleKeyboardGroupState.Inactive;
            }
                
            _selecting = false;
            _selectedPhi = 0;
            _selectedGroup = null;
        }

        private void Write(string s)
        {
            // currently, this just adds to the text on the test text field
            if (s == "<")
            {
                testInputField.text = testInputField.text.Substring(0, testInputField.text.Length - 1);
            }
            else
            {
                testInputField.text += s;
            }
            testInputField.caretPosition = testInputField.text.Length;
        }
    }
}
