Shader "Hidden/ParallaxOffset"
{
	Properties
	{
		_A ("_A", 2D) = "white" {}
		_B ("_B", 2D) = "white" {}
		_C ("_C", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;
			sampler2D _C;

			float4 frag( v2f_img i ) : SV_Target
			{
				float h = tex2D( _A, i.uv ).x;
				float height = tex2D( _B, i.uv ).x;
				float3 viewDir = tex2D( _C, i.uv ).xyz;
				float2 result = ParallaxOffset (h, height, viewDir);
				return float4(result, 0, 1);

			}
			ENDCG
		}

	}
}
