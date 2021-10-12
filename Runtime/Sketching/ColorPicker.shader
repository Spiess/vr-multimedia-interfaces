Shader "Custom/ColorPicker"
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

        float Brightness;

        struct Input
        {
            float3 pos;
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
            
            // we pass the local space vertex position to later convert to polar coordinates
            o.pos = v.vertex;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // convert the local vertex position to a 2D representation
            const float2 position = float2(IN.pos.x*2, -IN.pos.z*2);
            // convert the cartesian coordinates to polar coordinates (angle and radius) to be used as hue and saturation respectively
            const float h = 0.5 - atan2(position.x, position.y)/2/UNITY_PI;
            const float s = sqrt(position.x*position.x + position.y*position.y);
            
            o.Albedo = hsv_to_rgb(h, s, Brightness);
            o.Alpha = 1;
            o.Metallic = 0;
            o.Smoothness = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
