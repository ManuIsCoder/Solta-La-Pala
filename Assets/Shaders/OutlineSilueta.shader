Shader "Custom/OutlineSilueta"
{
    Properties
    {
        _Color ("Color Silueta", Color) = (1, 1, 1, 1)
        _Grosor ("Grosor Silueta", Float) = 0.025
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+100" }
        Cull Front
        ZWrite On
        ZTest LEqual

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
                float4 pos : SV_POSITION;
            };

            fixed4 _Color;
            float _Grosor;

            v2f vert(appdata v)
            {
                v2f o;
                float3 normWorld = UnityObjectToWorldNormal(v.normal);
                float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
                posWorld.xyz += normWorld * _Grosor;
                o.pos = mul(UNITY_MATRIX_VP, posWorld);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
