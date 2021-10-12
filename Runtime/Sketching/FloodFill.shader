Shader "Custom/FloodFill"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            };

            sampler2D _MainTex, OriginalTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 SeedPosition;
            fixed4 FloodColor;
            int Mode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                switch (Mode)
                {
                    default:
                    case 0: // Reset
                        return fixed4(0, 0, 0, 1);
                    case 1: // Fill
                        const fixed4 white = fixed4(1, 1, 1, 1), black = fixed4(0, 0, 0, 1);
                        const float2 position = float2(i.uv.x * _MainTex_TexelSize.z, i.uv.y * _MainTex_TexelSize.w);
                        const float2 seed_uv = float2(SeedPosition.x / _MainTex_TexelSize.z, SeedPosition.y / _MainTex_TexelSize.w);
                        
                        if (all(tex2D(OriginalTex, i.uv) == tex2D(OriginalTex, seed_uv)))
                        {
                            // check seed position
                            if (distance(position, SeedPosition.xy) <= 0.5)
                                return white;
                            
                            // check fragment itself
                            if (all(tex2D(_MainTex, i.uv) == white))
                                return white;
                            
                            // check east
                            float2 neighbour = i.uv + float2(_MainTex_TexelSize.x, 0);
                            if (neighbour.x < 1 && all(tex2D(_MainTex, neighbour) == white))
                                return white;
                            
                            // check west
                            neighbour = i.uv - float2(_MainTex_TexelSize.x, 0);
                            if (neighbour.x >= 0 && all(tex2D(_MainTex, neighbour) == white))
                                return white;
                            
                            // check north
                            neighbour = i.uv + float2(0, _MainTex_TexelSize.y);
                            if (neighbour.y < 1 && all(tex2D(_MainTex, neighbour) == white))
                                return white;
                            
                            // check south
                            neighbour = i.uv - float2(0, _MainTex_TexelSize.y);
                            if (neighbour.y >= 0 && all(tex2D(_MainTex, neighbour) == white))
                                return white;
                        }

                        return black; // this fragment should not (yet) be filled
                    case 2: // Apply
                        if (all(tex2D(_MainTex, i.uv) == fixed4(1, 1, 1, 1)))
                        {
                            // fragment should be filled
                            return FloodColor;
                        }

                        // otherwise return the original texture
                        return tex2D(OriginalTex, i.uv);
                }
            }
            ENDCG
        }
    }
}
