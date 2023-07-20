Shader "Hidden/ObjectScaleNode"
{
	SubShader
	{
		
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
				return float4(objectScale, 1);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag (v2f_img i) : SV_Target
			{
				float3 objectScale = 1.0 / float3(length (unity_WorldToObject[0].xyz), length (unity_WorldToObject[1].xyz), length (unity_WorldToObject[2].xyz));
				return float4(objectScale, 1);
			}
			ENDCG
		}
	}
}
