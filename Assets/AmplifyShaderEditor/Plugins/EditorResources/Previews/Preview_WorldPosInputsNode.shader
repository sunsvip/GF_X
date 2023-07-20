Shader "Hidden/WorldPosInputsNode"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 xy = 2 * i.uv - 1;
				float z = -sqrt(1-saturate(dot(xy,xy)));
				float4 vertexPos = float4(xy, z,1);
				float4 worldPos = mul(unity_ObjectToWorld, vertexPos);
				return float4 (worldPos.xyz, 1);
			}
			ENDCG
		}
	}
}
