Shader "Hidden/ComputeScreenPos"
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
				float4 a = tex2D( _A, i.uv );
				float4 screenPos = ComputeScreenPos(a);
				return screenPos;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag (v2f_img i) : SV_Target
			{
				float4 a = tex2D(_A, i.uv);
				float4 screenPos = ComputeScreenPos(a);
				screenPos = screenPos / screenPos.w;
				screenPos.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPos.z : screenPos.z* 0.5 + 0.5;
				return screenPos;
			}
			ENDCG
		}
	}
}
