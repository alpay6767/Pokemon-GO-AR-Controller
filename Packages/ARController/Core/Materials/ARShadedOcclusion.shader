// This is based on shaders from:
// https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/src/default/Assets/UnityARKitPlugin/Examples/Common/Shaders/MobileOcclusion.shader
// https://alastaira.wordpress.com/2014/12/30/adding-shadows-to-a-unity-vertexfragment-shader-in-7-easy-steps/

Shader "AR/ShadedOcclusion"
{
    Properties
    {
        _ShadowAlpha ("Shadow Alpha", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry-1" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" "RenderType"="Transparent" "Queue"="Geometry+1" "ForceNoShadowCasting"="True" }
            LOD 150
            Blend Zero OneMinusSrcAlpha
            ZWrite On
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            
            float _ShadowAlpha;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                LIGHTING_COORDS(0, 1)
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            fixed4 frag(v2f i) : COLOR
            {
                float attenuation = LIGHT_ATTENUATION(i);
                return fixed4(0.0, 0.0, 0.0, _ShadowAlpha * (1.0 - attenuation));
            }
            
            ENDCG
        }
    }
    Fallback "VertexLit"
}