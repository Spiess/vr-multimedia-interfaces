Shader "Custom/CircleKeyboardKey"
{
    Properties
    {
        _InactiveColor ("Inactive Color", Color) = (0.4, 0.4, 0.4, 1)
        _HoveringColor ("Hovering Color", Color) = (0.6, 0.6, 0.6, 1)
        _MinRadius ("Minimum Radius", Float) = 0.333
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 pos : POSITION1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _InactiveColor, _HoveringColor;
            float _MinRadius;
            float PhiFrom, PhiTo;
            bool Hovering, Shown;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // we pass the local space vertex position to later convert to polar coordinates
                o.pos = float2(v.vertex.x, v.vertex.y);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = fixed4(0, 0, 0, 0);
                if (Shown)
                {
                    const float radius = sqrt(i.pos.x*i.pos.x + i.pos.y*i.pos.y);
                    if (radius >= _MinRadius && radius <= 0.5) // the quad has a size of 1, we want a radius half that size
                    {
                        // the fragment is inside the radius
                        const float phi = 0.5 + atan2(i.pos.x, i.pos.y)/2/UNITY_PI;
                        if (phi >= PhiFrom && phi <= PhiTo)
                        {
                            // the fragment is inside the keyboard group
                            color = Hovering ? _HoveringColor : _InactiveColor;
                        }
                    }
                }
                return color;
            }
            ENDCG
        }
    }
}
