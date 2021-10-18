using UnityEngine;
using Util;

namespace Map
{
  public class UniversalPin : MonoBehaviour
  {
    public Transform pinModel;

    private Minimap _minimap;

    private void Update()
    {
      if (_minimap != null)
      {
        pinModel.LookAt(_minimap.transform);
      }
    }

    private void OnTriggerEnter(Collider other)
    {
      if (other.TryGetComponent<Minimap>(out var minimap))
      {
        _minimap = minimap;
      }
    }

    private void OnTriggerExit(Collider other)
    {
      if (_minimap != null && _minimap.transform == other.transform)
      {
        _minimap = null;
        pinModel.localRotation = Quaternion.identity;
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
    }
  }
}