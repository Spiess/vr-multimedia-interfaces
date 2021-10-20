using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public class Minimap : MonoBehaviour
  {
    [Tooltip("The zoom level to set the map to when teleporting to a location via the minimap.")]
    public float zoomTo;

    public Map map;

    private Dictionary<UniversalPin, Vector3> _pins = new Dictionary<UniversalPin, Vector3>();

    private void Update()
    {
      UpdatePins();
    }

    public void MoveTo(Vector2 coordinates)
    {
      map.Coordinates = coordinates;
      map.Zoom = zoomTo;
    }

    public void AddPin(UniversalPin pin)
    {
      var t = transform;
      var pinTransform = pin.transform;

      var position = t.position;
      var positionDelta = (pinTransform.position - position).normalized * (t.localScale.x / 2 + .004f);
      pinTransform.position = position + positionDelta;

      _pins.Add(pin, Quaternion.Inverse(t.rotation) * positionDelta);
    }

    public void RemovePin(UniversalPin pin)
    {
      _pins.Remove(pin);
    }

    private void UpdatePins()
    {
      var t = transform;
      foreach (var pin in _pins.Keys)
      {
        pin.transform.position = (t.rotation * _pins[pin]) + t.position;
      }
    }
  }
}