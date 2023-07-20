Shader "Hidden/TextureTransform"
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

			int _PreviewID = 0;
			sampler2D _Sampler;
			float4 _Sampler_ST;

			float4 frag( v2f_img i ) : SV_Target
			{
				return _PreviewID == 0?float4(_Sampler_ST.xy,0,0): float4(_Sampler_ST.zw,0,0);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			int _PreviewID = 0;
			sampler3D _Sampler3D;
			float4 _Sampler3D_ST;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Sampler3D_ST;
			return _PreviewID == 0 ? float4(_Sampler3D_ST.xy, 0, 0) : float4(_Sampler3D_ST.zw, 0, 0);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			int _PreviewID = 0;
			UNITY_DECLARE_TEX2DARRAY (_Array);
			float4 _Array_ST;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Array_ST;
				return _PreviewID == 0 ? float4(_Array_ST.xy, 0, 0) : float4(_Array_ST.zw, 0, 0);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			int _PreviewID = 0;
			samplerCUBE _Cube;
			float4 _Cube_ST;

			float4 frag (v2f_img i) : SV_Target
			{
				return _Cube_ST;
				return _PreviewID == 0 ? float4(_Cube_ST.xy, 0, 0) : float4(_Cube_ST.zw, 0, 0);
			}
			ENDCG
		}
	}
}
