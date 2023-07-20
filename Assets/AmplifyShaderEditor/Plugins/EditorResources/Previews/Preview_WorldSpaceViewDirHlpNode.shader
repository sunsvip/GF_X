Shader "Hidden/WorldSpaceViewDirHlpNode"
{
Properties
	{
		_A ("_A", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _A;

			float4 frag( v2f_img i ) : SV_Target
			{
				return float4( float3( 0,0,-5 ) - tex2D( _A, i.uv ).rgb,1);
			}
			ENDCG
		}
	}
}
