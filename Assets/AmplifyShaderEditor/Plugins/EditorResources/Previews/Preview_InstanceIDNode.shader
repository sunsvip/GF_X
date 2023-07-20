Shader "Hidden/InstanceIDNode"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag (v2f_img i) : SV_Target
			{
				uint currInstanceId = 0;
				#ifdef UNITY_INSTANCING_ENABLED
				currInstanceId = unity_InstanceID;
				#endif

				return currInstanceId;
			}
			ENDCG
		}
	}
}
