Shader "Skybox/Blended Fade" {
    Properties {
        _SkyboxA ("Skybox A", Cube) = "white" {}
        _SkyboxB ("Skybox B", Cube) = "white" {}
        _Blend ("Blend", Range(0,1)) = 0
    }

    SubShader {
        Tags { "Queue" = "Background" "RenderType" = "Background" }
        
        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata {
            float4 position : POSITION;
            float3 texcoord : TEXCOORD0;
        };
        
        struct v2f {
            float4 position : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };
        
        uniform samplerCUBE _SkyboxA;
        uniform samplerCUBE _SkyboxB;
        uniform float _Blend;
        
        v2f vert (appdata v) {
            v2f o;
            o.position = UnityObjectToClipPos(v.position);
            o.texcoord = v.texcoord;
            return o;
        }
        
        fixed4 frag (v2f i) : SV_Target {
            fixed4 colorA = texCUBE(_SkyboxA, i.texcoord);
            fixed4 colorB = texCUBE(_SkyboxB, i.texcoord);
            return lerp(colorA, colorB, _Blend);
        }
        ENDCG

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
