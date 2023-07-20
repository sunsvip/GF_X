Shader "Hidden/HeightMapTextureBlend"
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
				float heightmap = tex2D( _A, i.uv ).x;
				float splatMask = tex2D( _B, i.uv ).x;
				float blendStrength = tex2D( _C, i.uv ).x;
				float result = saturate( pow((( heightmap*splatMask ) * 4 ) + ( splatMask * 2 ), blendStrength ));
				return float4( result.x , 0, 0, 1 );
			}
			ENDCG
		}
	}
}
