Shader "Custom/MapTile"
{
    Properties
    {
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

                UNITY_VERTEX_INPUT_INSTANCE_ID // * Required for single pass instancing render support
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : POSITION1;

                UNITY_VERTEX_OUTPUT_STEREO // *
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 MapCenter;
            float MaxDistance;
            float FalloffRange;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v); // *
                UNITY_INITIALIZE_OUTPUT(v2f, o); // *
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // *
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // we pass the world position of the vertex to calculate its distance from the map center
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                const float distance_from_center = distance(float3(MapCenter.x, MapCenter.y, MapCenter.z), i.worldPos);
                if (distance_from_center > MaxDistance)
                {
                    // fade out the map fragment depending on the distance from the edge
                    color.a = pow(clamp((FalloffRange - (distance_from_center - MaxDistance)) / FalloffRange, 0, 1), 2);
                }
                return color;
            }
            ENDCG
        }
    }
}
