Shader "Hidden/ShadeVertexLights"
{
	Properties
	{
		_A ("_A", 2D) = "white" {}
		_B ("_B", 2D) = "white" {}
		_LightCount( "_LightCount", Int ) = 4
		_IsSpotlight ("_IsSpotlight", Int) = 0
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
			sampler2D _B;
			int _LightCount;
			int _IsSpotlight;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 vertexPosition = tex2D( _A, i.uv );
				float3 vertexNormal = tex2D( _B, i.uv ).xyz;
				float3 result = ShadeVertexLightsFull (vertexPosition, vertexNormal, _LightCount, (_IsSpotlight > 0));
				return float4(result, 1);
			}
			ENDCG
		}
	}
}
