Shader "Unlit/HologramShader"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 wPos: TEXCOORD0;
                float3 normal: TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.wPos = mul (UNITY_MATRIX_M, float4(v.vertex.xyz, 1));
                o.normal = mul ((float3x3)UNITY_MATRIX_M, v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyzx - i.wPos);
                float fresnel = pow(saturate(1 - dot(viewDir, i.normal)), 1.5f);
                fresnel = lerp(0.2f, 0.8f, fresnel);
                return float4(1,0,0,fresnel);
            }
            ENDCG
        }
    }
}
