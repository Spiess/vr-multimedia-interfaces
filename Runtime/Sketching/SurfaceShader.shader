Shader "Custom/SurfaceShader"
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 StrokeColor;
            float4 StrokeFromTo; // from (x, y) and to (z, w)
            float StrokeRadius; // in pixels
            float SurfaceWidth, SurfaceHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // prepare the position of the fragment on the image
                const float2 position = float2(i.uv.x*SurfaceWidth, i.uv.y*SurfaceHeight);

                // prepare the two ends of the stroke, making sure to always have the stroke rising
                float2 lower, higher;
                if (StrokeFromTo.w < StrokeFromTo.y)
                {
                    lower = StrokeFromTo.zw;
                    higher = StrokeFromTo.xy;
                }
                else
                {
                    lower = StrokeFromTo.xy;
                    higher = StrokeFromTo.zw;
                }

                // prepare the stroke and calculate the slope of the stroke bounds
                const float2 stroke = higher - lower;
                const float inverse_slope = - stroke.x / stroke.y;
                
                if (position.y >= inverse_slope * (position.x - higher.x) + higher.y)
                {
                    // if the fragment is above the higher bound, we want to draw the higher end of the stroke
                    if (distance(position, higher) < StrokeRadius)
                    {
                        return StrokeColor;
                    }
                }
                else if(position.y <= inverse_slope * (position.x - lower.x) + lower.y)
                {
                    // if the fragment is below the lower bound, we want to draw the lower end of the stroke
                    if (distance(position, lower) < StrokeRadius)
                    {
                        return StrokeColor;
                    }
                }
                else
                {
                    // the fragment is between the two bounds, calculate the fragments' distance from the stroke with
                    // a simplified form of the formula d = |ax + by + c| / sqrt(a^2 + b^2)
                    const float slope = stroke.y / stroke.x;
                    if(abs(slope * position.x - position.y - slope * lower.x + lower.y)/sqrt(slope*slope + 1) < StrokeRadius)
                    {
                        // the fragment is inside the main part of the stroke
                        return StrokeColor;
                    }
                }

                // return the previous texture content if this fragment wasn't drawn on
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
