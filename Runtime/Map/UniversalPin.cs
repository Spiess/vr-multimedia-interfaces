using UnityEngine;

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
        Debug.Log("Dropped on Minimap!");
      }
    }
  }
}