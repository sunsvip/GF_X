Shader "Hidden/SwitchBySRPVersion"
{
	Properties
	{
		_A ("_A", 2D) = "white" {}
		_B ("_B", 2D) = "white" {}
		_C ("_C", 2D) = "white" {}
		_D ("_D", 2D) = "white" {}
		_E ("_E", 2D) = "white" {}
		_F ("_F", 2D) = "white" {}
		_G ("_G", 2D) = "white" {}
		_H( "_H", 2D ) = "white" {}
		_Condition( "_Condition", Int ) = 0
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
			sampler2D _C;
			sampler2D _D;
			sampler2D _E;
			sampler2D _F;
			sampler2D _G;
			sampler2D _H;
			int _Condition;

			float4 frag(v2f_img i) : SV_Target
			{
				switch ( _Condition )
				{
					case 1: return tex2D( _B, i.uv );
					case 2: return tex2D( _C, i.uv );
					case 3: return tex2D( _D, i.uv );
					case 4: return tex2D( _E, i.uv );
					case 5: return tex2D( _F, i.uv );
					case 6: return tex2D( _G, i.uv );
				}
				return tex2D( _A, i.uv );
			}
			ENDCG
		}
	}
}
