Shader "Hidden/FogAndAmbientColors"
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
				return UNITY_LIGHTMODEL_AMBIENT;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag (v2f_img i) : SV_Target
			{
				return unity_AmbientSky;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag (v2f_img i) : SV_Target
			{
				return unity_AmbientEquator;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag (v2f_img i) : SV_Target
			{
				return unity_AmbientGround;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag (v2f_img i) : SV_Target
			{
				return unity_FogColor;
			}
			ENDCG
		}
	}
}
