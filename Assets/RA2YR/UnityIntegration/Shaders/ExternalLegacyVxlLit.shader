Shader "RA2YR/ExternalLegacyVxlLit"
{
    Properties
    {
        _AmbientColor ("Ambient", Color) = (0.36, 0.36, 0.36, 1)
        _DirectionalColor ("Directional", Color) = (0.9, 0.9, 0.9, 1)
        _LightDirection ("Light Direction", Vector) = (0.35, 0.8, -0.45, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _AmbientColor;
            fixed4 _DirectionalColor;
            float4 _LightDirection;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.normal = normalize(UnityObjectToWorldNormal(input.normal));
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal = normalize(input.normal);
                float3 lightDirection = normalize(_LightDirection.xyz);
                float diffuse = saturate(dot(normal, lightDirection));
                float3 lighting = _AmbientColor.rgb + diffuse * _DirectionalColor.rgb;
                return fixed4(input.color.rgb * lighting, input.color.a);
            }
            ENDCG
        }
    }
    FallBack Off
}
