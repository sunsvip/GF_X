Shader "Hidden/ObjectToClipPos"
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
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			
			float4 frag( v2f_img i ) : SV_Target
			{
				float3 pos = tex2D( _A, i.uv ).xyz;
				return UnityObjectToClipPos(pos);
			}
			ENDCG
		}

	}
}
