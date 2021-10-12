using System.Linq;
using UnityEngine;

namespace Sketching
{
    internal enum FloodFillShaderMode
    {
        Reset = 0, Fill = 1, Apply = 2
    }
    
    /// <summary>
    /// The <c>Sketchpad</c> is an object that is being drawn on by a <see cref="SketchingUtensil"/>. It uses the
    /// <c>SurfaceShader</c> to draw on a texture. The user interacts with the sketchpad using a
    /// <see cref="SketchpadInteractor"/>. Use <see cref="Sketch"/> to access the drawing.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Sketchpad : MonoBehaviour
    {
        /// <summary>
        /// The resolution in pixels per unit of this surface.
        /// <remarks>Note that the actual size of the created texture depends on both the resolution and the size of the surface.</remarks>
        /// </summary>
        [Tooltip("Pixels per unit")] public int resolution;
        private int _currentResolution;
        /// <summary>
        /// The surface that is drawn on.
        /// </summary>
        public Transform surface;
        /// <summary>
        /// The material that is used to flood fill.
        /// </summary>
        [SerializeField] protected Material floodFillMaterial;

        /// <summary>
        /// The resulting drawing.
        /// </summary>
        public Texture2D Sketch => _surfaceTexture;
        private Texture2D _surfaceTexture;
        
        private RenderTexture _renderBuffer, _floodFillBuffer0, _floodFillBuffer1;
        private Material _surfaceMaterial;

        private static readonly int
            StrokeColor = Shader.PropertyToID("StrokeColor"),
            StrokeFromTo = Shader.PropertyToID("StrokeFromTo"),
            StrokeRadius = Shader.PropertyToID("StrokeRadius"),
            SurfaceWidth = Shader.PropertyToID("SurfaceWidth"),
            SurfaceHeight = Shader.PropertyToID("SurfaceHeight"),
            OriginalTex = Shader.PropertyToID("OriginalTex"),
            FloodColor = Shader.PropertyToID("FloodColor"),
            SeedPosition = Shader.PropertyToID("SeedPosition"),
            Mode = Shader.PropertyToID("Mode");

        private void Start()
        {
            _surfaceMaterial = surface.GetComponent<Renderer>().material;
            
            SetupSurface();
        }

        private void Update()
        {
            if (_currentResolution != resolution) // the surface has been resized
            {
                SetupSurface();
            }
        }

        /// <summary>
        /// Recreate the texture and render buffer for the current size of the surface.
        /// </summary>
        private void SetupSurface()
        {
            _currentResolution = resolution;
            
            var size = surface.localScale;
            int width = Mathf.RoundToInt(size.x * resolution), height = Mathf.RoundToInt(size.y * resolution);
            
            // create the texture to hold the drawing and a render buffer to render the updated drawing
            _surfaceTexture = new Texture2D(
                width, height,
                TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _surfaceTexture.SetPixels32(Enumerable
                .Repeat(new Color32(255,  255, 255, 0), _surfaceTexture.width * _surfaceTexture.height).ToArray());
            _surfaceTexture.Apply();
            _renderBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            _renderBuffer.Create();
            
            // create the flood fill buffers
            _floodFillBuffer0 = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            _floodFillBuffer1 = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            
            // tell the shaders about the new texture and the texture size
            _surfaceMaterial.mainTexture = _surfaceTexture;
            _surfaceMaterial.SetFloat(SurfaceWidth, _surfaceTexture.width);
            _surfaceMaterial.SetFloat(SurfaceHeight, _surfaceTexture.height);
            floodFillMaterial.SetTexture(OriginalTex, _surfaceTexture);
        }
        
        /// <summary>
        /// Render a new line onto the renderbuffer and copy it to the texture.
        /// </summary>
        /// <param name="from">The starting position of the line.</param>
        /// <param name="to">The end position of the line.</param>
        /// <param name="size">The line width.</param>
        /// <param name="color">The color to fill the line with.</param>
        /// <remarks><c>from</c> and <c>to</c> coordinates range from <c>(0, 0)</c> to <c>(1, 1)</c></remarks>
        public void DrawLine(Vector2 from, Vector2 to, float size, Color color)
        {
            // coordinates range from 0 to 1, so we multiply them by the current surface size to get pixel coordinates
            from.x *= _surfaceTexture.width;
            from.y *= _surfaceTexture.height;
            to.x *= _surfaceTexture.width;
            to.y *= _surfaceTexture.height;
            
            // inform the shader of the new line
            _surfaceMaterial.SetColor(StrokeColor, color);
            _surfaceMaterial.SetVector(StrokeFromTo, new Vector4(from.x, from.y, to.x, to.y));
            _surfaceMaterial.SetFloat(StrokeRadius, size/2.0f);
            
            // render the new line onto the existing texture using the render buffer
            Graphics.Blit(_surfaceTexture, _renderBuffer, _surfaceMaterial);
            
            // copy the render buffer over the old texture
            Graphics.CopyTexture(_renderBuffer, _surfaceTexture);
            
            // reset shader
            _surfaceMaterial.SetVector(StrokeFromTo, Vector4.zero);
            _surfaceMaterial.SetFloat(StrokeRadius, 0);
        }

        /// <summary>
        /// Fill an area of the same color with a new color.
        /// </summary>
        /// <param name="origin">The origin from which to start the flood fill.</param>
        /// <param name="color">The color to fill the area with.</param>
        /// <remarks><c>origin</c> coordinates range from <c>(0, 0)</c> to <c>(1, 1)</c></remarks>
        public void Fill(Vector2 origin, Color color)
        {
            // prepare and reset buffer
            int width = _surfaceTexture.width, height = _surfaceTexture.height;
            var bufferIndex = 0;
            const int steps = 1000;
            
            floodFillMaterial.SetInt(Mode, (int) FloodFillShaderMode.Reset);
            floodFillMaterial.mainTexture = _floodFillBuffer1;
            Graphics.Blit(_floodFillBuffer1, _floodFillBuffer0, floodFillMaterial);
            
            // fill
            floodFillMaterial.SetVector(SeedPosition, new Vector4(origin.x * width, origin.y * height, 0, 0));
            floodFillMaterial.SetInt(Mode, (int) FloodFillShaderMode.Fill);
            for (var i = 0; i < steps; i++)
            {
                // we use double buffers to read from the first and write on the second
                RenderTexture buffer, other;
                if (bufferIndex == 0)
                {
                    buffer = _floodFillBuffer1;
                    other = _floodFillBuffer0;
                    bufferIndex++;
                }
                else
                {
                    buffer = _floodFillBuffer0;
                    other = _floodFillBuffer1;
                    bufferIndex--;
                }
            
                floodFillMaterial.mainTexture = other;
                Graphics.Blit(other, buffer, floodFillMaterial);
            }
            
            // apply the flood fill and copy the render buffer over the old texture
            var lastBuffer = bufferIndex == 0 ? _floodFillBuffer0 : _floodFillBuffer1;
            floodFillMaterial.SetColor(FloodColor, color);
            floodFillMaterial.SetInt(Mode, (int) FloodFillShaderMode.Apply);
            floodFillMaterial.mainTexture = lastBuffer;
            Graphics.Blit(lastBuffer, _renderBuffer, floodFillMaterial);
            Graphics.CopyTexture(_renderBuffer, _surfaceTexture);
        }

        /// <summary>
        /// Project a position in world space onto the surface.
        /// </summary>
        /// <param name="worldPosition">The world space position to be projected onto the surface.</param>
        /// <returns>A 2D vector representing the local position on the surface.</returns>
        /// <remarks>The returned position has the origin at the bottom left of the surface conform with Unity convention. Values range from <c>0</c> to <c>1</c>, so to get pixel coordinates, multiply this with the surface size.</remarks>
        public Vector2 PositionOnSurface(Vector3 worldPosition)
        {
            var localPosition = surface.InverseTransformPoint(worldPosition);
            return new Vector2((.5f + localPosition.x), (.5f + localPosition.y)); // this calculation transforms the origin from the center to the bottom left
        }
    }
}
