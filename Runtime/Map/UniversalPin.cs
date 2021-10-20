using UnityEngine;
using Util;

namespace Map
{
  public class UniversalPin : MonoBehaviour
  {
    public Transform pinModel;

    private Minimap _minimap;
    private Map _map;

    private void Update()
    {
      if (_minimap != null)
      {
        pinModel.LookAt(_minimap.transform);
      }
      else if (_map != null)
      {
        pinModel.rotation = _map.transform.rotation * Quaternion.Euler(90, 0, 0);
      }
    }

    private void OnTriggerEnter(Collider other)
    {
      if (other.TryGetComponent<Minimap>(out var minimap))
      {
        _minimap = minimap;
      }
      else if (other.TryGetComponent<Map>(out var map))
      {
        _map = map;
      }
    }

    private void OnTriggerExit(Collider other)
    {
      if (_minimap != null && _minimap.transform == other.transform)
      {
        _minimap = null;
        pinModel.localRotation = Quaternion.identity;
      }
      else if (_map != null && _map.transform == other.transform)
      {
        _map = null;
        pinModel.localRotation = Quaternion.identity;
      }
    }

    public void OnGrab()
    {
      if (_map != null)
      {
        _map.RemovePin(this);
      }
    }

    public void OnDrop()
    {
      if (_minimap != null)
      {
        // Move map to the location where the pin was dropped
        var spherical =
          MathUtilities.CartesianToSpherical(_minimap.transform.InverseTransformPoint(transform.position));
        var coordinates = new Vector2(90 - spherical.y / Mathf.PI * 180, -spherical.z / Mathf.PI * 180);
        _minimap.MoveTo(coordinates);
        // TODO: Destroy this pin or attach it to the minimap
      }
      else if (_map != null)
      {
        _map.AddPin(this);
      }
    }
  }
}