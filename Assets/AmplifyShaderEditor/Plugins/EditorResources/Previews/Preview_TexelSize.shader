Shader "Hidden/TexelSize"
{
	Properties
	{
		_Sampler ("_Sampler", 2D) = "white" {}
		_Sampler3D ("_Sampler3D", 3D) = "white" {}
		_Array ("_Array", 2DArray) = "white" {}
		_Cube ("_Cube", CUBE) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Sampler;
			float4 _Sampler_TexelSize;

			float4 frag( v2f_img i ) : SV_Target
			{
				return _Sampler_TexelSize;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler3D _Sampler3D;
			float4 _Sampler3D_TexelSize;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Sampler3D_TexelSize;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			UNITY_DECLARE_TEX2DARRAY (_Array);
			float4 _Array_TexelSize;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Array_TexelSize;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			samplerCUBE _Cube;
			float4 _Cube_TexelSize;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Cube_TexelSize;
			}
			ENDCG
		}
	}
}
