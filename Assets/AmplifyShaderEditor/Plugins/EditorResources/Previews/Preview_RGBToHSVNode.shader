Shader "Hidden/RGBToHSVNode"
{
	Properties
	{
		_A ( "_RGB",  2D ) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			uniform sampler2D _A;

			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
				float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
				float d = q.x - min( q.w, q.y );
				float e = 1.0e-10;
				return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			float4 frag ( v2f_img i ) : SV_Target
			{
				float3 rgb = tex2D ( _A, i.uv ).rgb;
				return float4( RGBToHSV(rgb), 1 );
			}
			ENDCG
		}
	}
}
