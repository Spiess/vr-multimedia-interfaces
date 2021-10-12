Shader "Custom/BrightnessPicker"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma target 3.0

        float Hue, Saturation;

        struct Input
        {
            float brightness;
        };

        /// <summary>
        /// Convert a hue, saturation and brightness value to an RGB color.
        /// Translated to hlsl from https://github.com/hughsk/glsl-hsv2rgb/blob/master/index.glsl
        /// </summary>
        /// <param name="h">The hue of the color (from <c>0</c> to <c>1</c>).</param>
        /// <param name="s">The saturation of the color (from <c>0</c> to <c>1</c>).</param>
        /// <param name="v">The value or brightness of the color (from <c>0</c> to <c>1</c>).</param>
        fixed3 hsv_to_rgb(float h, float s, float v)
        {
            const float4 k = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
            const float3 p = abs(frac(float3(h, h, h) + k.xyz) * 6.0 - k.www);
            return v * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), s);
        }

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // the brightness is chosen along the y axis of the brightness picker
            o.brightness = 1 + v.vertex.y; // 2 * (0.5 + y/2) = 1 + y
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = hsv_to_rgb(Hue, Saturation, IN.brightness);
            o.Alpha = 1;
            o.Metallic = 0;
            o.Smoothness = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
