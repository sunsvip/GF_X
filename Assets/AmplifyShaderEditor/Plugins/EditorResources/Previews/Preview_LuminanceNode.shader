Shader "Hidden/LuminanceNode"
{
	Properties
	{
		_A ("_RGB", 2D) = "white" {}
	}
	SubShader
	{
		Pass //Luminance
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			
			float4 frag(v2f_img i) : SV_Target
			{
				float lum = Luminance( tex2D( _A, i.uv ) );
				return float4( lum.xxx, 1);
			}
			ENDCG
		}
	}
}
