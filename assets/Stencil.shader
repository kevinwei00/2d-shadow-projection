Shader "Custom/Stencil" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags { 
			"RenderType"="Transparent" 
			"Queue"="Overlay"
		}
		Blend DstColor SrcColor

		Stencil
		{
			Ref 1
			Comp Greater
			Pass Replace
		}

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}