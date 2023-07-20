Shader "Hidden/ClipPlanes"
{
	Properties
	{
		_PlaneId ("_PlaneId", Int) = 0
	}
	
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			int _PlaneId;
			float4 frag( v2f_img i ) : SV_Target
			{
				return unity_CameraWorldClipPlanes[_PlaneId];
			}
			ENDCG
		}
	}
}
