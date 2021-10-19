using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;

namespace Map
{
  /// <summary>
  /// The <c>Map</c> allows the user to drop <see cref="Pin"/>s to choose locations. The user interacts with the map
  /// using a <see cref="MapInteractor"/>.
  /// </summary>
  [RequireComponent(typeof(BoxCollider)), RequireComponent(typeof(Rigidbody))]
  public class Map : MonoBehaviour
  {
    /// <summary>
    /// The minimum and maximum zoom levels to allow. Currently these are the Mapbox minimum and maximum values.
    /// </summary>
    /// <remarks>
    /// The actual size of the map grows exponentially to the zoom level.
    /// </remarks>
    private const float MinZoom = 0, MaxZoom = 22;

    /// <summary>
    /// The maximum distance at which the map is still drawn fully opaque. After this, the map is being blended out
    /// over <see cref="falloffRange"/>.
    /// </summary>
    public float maxDistance;

    /// <summary>
    /// The range over which to blend the map to transparent. Anything further than
    /// <c>maxDistance + falloffRange</c> is completely transparent
    /// </summary>
    public float falloffRange;

    /// <summary>
    /// The material that blends out the map using <see cref="maxDistance"/> and <see cref="falloffRange"/>.
    /// </summary>
    [SerializeField] protected Material mapTileMaterial;

    /// <summary>
    /// The <c>velocityCenter</c> is used to smoothly pan the map. <see cref="MapInteractor"/>s add a force to this
    /// when letting go.
    /// </summary>
    public Rigidbody velocityCenter;

    /// <summary>
    /// The zoom level of the map, bound by <see cref="MinZoom"/> and <see cref="MaxZoom"/>. Use this to reset the
    /// zoom level to a certain value. To zoom in by a certain amount, use <see cref="DeltaZoom"/> instead.
    /// </summary>
    /// <remarks>
    /// The actual size of the map grows exponentially to the zoom level.
    /// </remarks>
    public float Zoom
    {
      get => _mapboxRoot.Zoom;
      set => _mapboxRoot.UpdateMap(new Vector2d(Coordinates.x, Coordinates.y), Mathf.Clamp(value, MinZoom, MaxZoom));
    }

    /// <summary>
    /// The coordinates in the center of the map. Use this to reset the center to a new position. To pan the map by
    /// a certain amount, use <see cref="Translate(UnityEngine.Vector2)"/> or <see cref="Translate(float, float)"/>
    /// instead.
    /// </summary>
    public Vector2 Coordinates
    {
      get
      {
        var center = _mapboxRoot.CenterLatitudeLongitude;
        return new Vector2((float) center.x, (float) center.y);
      }
      set => _mapboxRoot.UpdateMap(new Vector2d(value.x, value.y));
    }

    /// <summary>
    /// The list of all pins currently on this map.
    /// </summary>
    public List<Pin> Pins => _pins;

    /// <summary>
    /// The list of all pins currently on this map and inside its view bounds.
    /// </summary>
    public List<Pin> PinsInMapBounds
    {
      get { return _pins.Where(pin => pin.inMapBounds).ToList(); }
    }

    private List<Pin> _pins = new List<Pin>();

    private AbstractMap _mapboxRoot;

    private Vector3 _previousPosition;

    private static readonly int
      MapCenter = Shader.PropertyToID("MapCenter"),
      MaxDistance = Shader.PropertyToID("MaxDistance"),
      FalloffRange = Shader.PropertyToID("FalloffRange");

    public float colliderDepth = 10;

    private void Start()
    {
      _mapboxRoot = GetComponentInChildren<AbstractMap>();

      // calculate the collider size from the drawing distance
      var mapCollider = GetComponent<BoxCollider>();
      if (mapCollider != null)
      {
        var size = (maxDistance + falloffRange) * 2;
        var scale = transform.localScale;
        mapCollider.size = new Vector3(size / scale.x, colliderDepth, size / scale.z);
      }

      // TODO: use AbstractMap::SetTileMaterial instead of updating materials manually
      _mapboxRoot.OnUpdated += UpdateTileRenderers;
      UpdateTileRenderers();
    }

    private void Update()
    {
      // Handle interactions
      switch (_interactors.Count)
      {
        case 1: // Dragging
          var interactor = _interactors.First();
          Drag(interactor.Key.position, interactor.Value);
          break;
        case 2:
        {
          var interactor0 = _interactors.First();
          var interactor1 = _interactors.Last();
          var currentPosition0 = interactor0.Key.position;
          var currentPosition1 = interactor1.Key.position;
          var previousPosition0 = interactor0.Value;
          var previousPosition1 = interactor1.Value;

          var currentDistance = Vector3.Distance(currentPosition0, currentPosition1);
          var previousDistance = Vector3.Distance(previousPosition0, previousPosition1);

          // zoom the map by log2 (because the map zoom is determined by 2^zoom) of the relative scaling change
          DeltaZoom(Mathf.Log(currentDistance / previousDistance, 2));

          // Move the map by the center of hands
          Drag((currentPosition0 + currentPosition1) / 2, (previousPosition0 + previousPosition1) / 2);
        }
          break;
      }

      foreach (var interactor in _interactors.Keys.ToList())
      {
        _interactors[interactor] = interactor.position;
      }

      // move the map to the new velocity center position and reset the velocity center.
      if (velocityCenter.transform.localPosition.sqrMagnitude > 1e-5f)
      {
        // Calculate world space coordinates from mapbox root rather than real world space to counteract weirdness
        var rootSpace = _mapboxRoot.Root.transform.TransformPoint(velocityCenter.transform.localPosition);
        Coordinates = PositionToCoordinates(rootSpace);
      }

      velocityCenter.transform.localPosition = Vector3.zero;
      if (_previousPosition != transform.position)
      {
        UpdateTileRenderers();
      }

      _previousPosition = transform.position;
    }

    /// <summary>
    /// Updates custom shader variables to produce map fade effect
    /// </summary>
    private void UpdateTileRenderers()
    {
      foreach (Transform tile in _mapboxRoot.transform)
      {
        if (tile.TryGetComponent<MeshRenderer>(out var tileRenderer))
        {
          var material = tileRenderer.material;
          var tileTexture = material.mainTexture;
          material = mapTileMaterial;
          tileRenderer.material = material;
          material.mainTexture = tileTexture;
          tileRenderer.material.SetVector(MapCenter, transform.position);
          tileRenderer.material.SetFloat(MaxDistance, maxDistance);
          tileRenderer.material.SetFloat(FalloffRange, falloffRange);
        }
      }
    }

    private void OnDrawGizmos()
    {
      // Visualize bounding box
      Gizmos.color = Color.white;
      var size = (maxDistance + falloffRange) * 2;
      var t = transform;
      Gizmos.DrawWireCube(t.position, new Vector3(size, t.localScale.y * colliderDepth, size));
    }

    private void OnDrawGizmosSelected()
    {
      // Visualize max distance when selected
      Gizmos.color = Color.white;
      var size = maxDistance * 2;
      var t = transform;
      Gizmos.DrawWireCube(t.position, new Vector3(size, t.localScale.y * colliderDepth, size));
    }

    #region Interactions

    /// <summary>
    /// List of currently active interactors and their previous positions
    /// </summary>
    private Dictionary<Transform, Vector3> _interactors = new Dictionary<Transform, Vector3>();

    public void InteractionChange(Transform interactor, bool active)
    {
      if (active)
      {
        if (_interactors.ContainsKey(interactor))
        {
          return;
        }

        _interactors.Add(interactor, interactor.position);
      }
      else if (_interactors.ContainsKey(interactor))
      {
        _interactors.Remove(interactor);
      }
    }

    private void Drag(Vector3 currentPosition, Vector3 previousPosition)
    {
      var startCoordinates = PositionToCoordinates(previousPosition);
      var endCoordinates = PositionToCoordinates(currentPosition);
      Translate(startCoordinates - endCoordinates);
    }

    #endregion

    #region Translation and Zoom

    /// <summary>
    /// Zoom in or out by a certain amount. To reset the zoom level to a certain value, use <see cref="Zoom"/>
    /// instead.
    /// </summary>
    /// <param name="delta">
    /// How much to zoom in (positive) or out (negative). A value of <c>1</c> doubles the size of the map.
    /// </param>
    public void DeltaZoom(float delta)
    {
      Zoom += delta;
    }

    /// <summary>
    /// Pan the map by a certain amount. To reset the center to a new position, use <see cref="Coordinates"/>
    /// instead.
    /// </summary>
    /// <param name="coordinateDelta">A vector with the translation latitude and longitude.</param>
    public void Translate(Vector2 coordinateDelta)
    {
      Coordinates += coordinateDelta;
    }

    /// <summary>
    /// Pan the map by a certain amount. To reset the center to a new position, use <see cref="Coordinates"/>
    /// instead.
    /// </summary>
    /// <param name="latitude">The latitude to translate by.</param>
    /// <param name="longitude">The longitude to translate by.</param>
    public void Translate(float latitude, float longitude)
    {
      Translate(new Vector2(latitude, longitude));
    }

    #endregion

    #region Conversions

    /// <summary>
    /// Translate a position in world space to a position on the map.
    /// </summary>
    /// <param name="position">The position in world space. Will be projected onto the map plane.</param>
    /// <returns>A vector with the latitude and longitude of the position on the map.</returns>
    public Vector2 PositionToCoordinates(Vector3 position)
    {
      var position2d = _mapboxRoot.WorldToGeoPosition(position);
      return new Vector2((float) position2d.x, (float) position2d.y);
    }

    /// <summary>
    /// Translate a position on the map to a position in world space.
    /// </summary>
    /// <param name="coordinates">A vector with the latitude and longitude position on the map.</param>
    /// <returns>The position in world space.</returns>
    public Vector3 CoordinatesToPosition(Vector2 coordinates)
    {
      return _mapboxRoot.GeoToWorldPosition(new Vector2d(coordinates.x, coordinates.y), false);
    }

    /// <summary>
    /// Translate a position on the map to a position in world space.
    /// </summary>
    /// <param name="latitude">The latitude of the position on the map.</param>
    /// <param name="longitude">The longitude of the position on the map.</param>
    /// <returns>The position in world space.</returns>
    public Vector3 CoordinatesToPosition(float latitude, float longitude)
    {
      return _mapboxRoot.GeoToWorldPosition(new Vector2d(latitude, longitude), false);
    }

    #endregion

    #region Pins

    public void AddPin(Pin pin)
    {
      _pins.Add(pin);
      pin.map = this;
    }

    public void RemovePin(Pin pin)
    {
      _pins.Remove(pin);
      pin.map = null;
    }

    #endregion
  }
}