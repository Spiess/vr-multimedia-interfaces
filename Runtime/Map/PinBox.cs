using UnityEngine;

namespace Map
{
  public class PinBox : MonoBehaviour
  {
    public Transform pinPrefab;

    private Transform _pin;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private void Start()
    {
      _previousPosition = transform.position;
    }

    private void Update()
    {
      var t = transform;
      if (t.position != _previousPosition)
      {
        var position = t.position;
        var rotation = t.rotation;
        var rotationDelta = rotation * Quaternion.Inverse(_previousRotation);

        var pinDelta = _pin.position - _previousPosition;

        _pin.position = position + rotationDelta * pinDelta;
        _pin.rotation = rotation * Quaternion.Euler(90, 0, 0);

        _previousPosition = position;
        _previousRotation = rotation;
      }

      // Prevent spawning of new pins while the box is being moved
      if (_pin == null || Vector3.Distance(_pin.position, GetExpectedPinPosition()) > Vector3.kEpsilon)
      {
        InstantiatePin();
      }
    }

    private void InstantiatePin()
    {
      _pin = Instantiate(pinPrefab, GetExpectedPinPosition(), transform.rotation * Quaternion.Euler(90, 0, 0));
    }

    private Vector3 GetExpectedPinPosition()
    {
      var t = transform;
      return t.position + t.up * .02f;
    }
  }
}