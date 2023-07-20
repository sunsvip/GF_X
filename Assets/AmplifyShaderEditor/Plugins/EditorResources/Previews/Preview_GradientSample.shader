Shader "Hidden/GradientSample"
{
	Properties
	{
		_GTime( "_Time", 2D ) = "white" {}
		_GType( "_GType", Int ) = 0
		_GColorNum( "_GColorNum", Int ) = 0
		_GAlphaNum( "_GAlphaNum", Int ) = 0
		_Col0( "_Col0", Vector ) = ( 0, 0, 0, 0 )
		_Col1( "_Col1", Vector ) = ( 0, 0, 0, 0 )
		_Col2( "_Col2", Vector ) = ( 0, 0, 0, 0 )
		_Col3( "_Col3", Vector ) = ( 0, 0, 0, 0 )
		_Col4( "_Col4", Vector ) = ( 0, 0, 0, 0 )
		_Col5( "_Col5", Vector ) = ( 0, 0, 0, 0 )
		_Col6( "_Col6", Vector ) = ( 0, 0, 0, 0 )
		_Col7( "_Col7", Vector ) = ( 0, 0, 0, 0 )
		_Alp0( "_Alp0", Vector ) = ( 0, 0, 0, 0 )
		_Alp1( "_Alp1", Vector ) = ( 0, 0, 0, 0 )
		_Alp2( "_Alp2", Vector ) = ( 0, 0, 0, 0 )
		_Alp3( "_Alp3", Vector ) = ( 0, 0, 0, 0 )
		_Alp4( "_Alp4", Vector ) = ( 0, 0, 0, 0 )
		_Alp5( "_Alp5", Vector ) = ( 0, 0, 0, 0 )
		_Alp6( "_Alp6", Vector ) = ( 0, 0, 0, 0 )
		_Alp7( "_Alp7", Vector ) = ( 0, 0, 0, 0 )
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
		
			sampler2D _GTime;
			int _GType;
			int _GColorNum;
			int _GAlphaNum;
			float4 _Col0;
			float4 _Col1;
			float4 _Col2;
			float4 _Col3;
			float4 _Col4;
			float4 _Col5;
			float4 _Col6;
			float4 _Col7;
			float4 _Alp0;
			float4 _Alp1;
			float4 _Alp2;
			float4 _Alp3;
			float4 _Alp4;
			float4 _Alp5;
			float4 _Alp6;
			float4 _Alp7;

			struct Gradient
			{
				int type;
				int colorsLength;
				int alphasLength;
				float4 colors[ 8 ];
				float2 alphas[ 8 ];
			};

			Gradient NewGradient( int type, int colorsLength, int alphasLength,
				float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
				float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7 )
			{
				Gradient g;
				g.type = type;
				g.colorsLength = colorsLength;
				g.alphasLength = alphasLength;
				g.colors[ 0 ] = colors0;
				g.colors[ 1 ] = colors1;
				g.colors[ 2 ] = colors2;
				g.colors[ 3 ] = colors3;
				g.colors[ 4 ] = colors4;
				g.colors[ 5 ] = colors5;
				g.colors[ 6 ] = colors6;
				g.colors[ 7 ] = colors7;
				g.alphas[ 0 ] = alphas0;
				g.alphas[ 1 ] = alphas1;
				g.alphas[ 2 ] = alphas2;
				g.alphas[ 3 ] = alphas3;
				g.alphas[ 4 ] = alphas4;
				g.alphas[ 5 ] = alphas5;
				g.alphas[ 6 ] = alphas6;
				g.alphas[ 7 ] = alphas7;
				return g;
			}

			float4 SampleGradient( Gradient gradient, float time )
			{
				float3 color = gradient.colors[ 0 ].rgb;
				UNITY_UNROLL
				for( int c = 1; c < 8; c++ )
				{
					float colorPos = saturate( ( time - gradient.colors[ c - 1 ].w ) / ( gradient.colors[ c ].w - gradient.colors[ c - 1 ].w ) ) * step( c, (float)gradient.colorsLength - 1 );
					color = lerp( color, gradient.colors[ c ].rgb, lerp( colorPos, step( 0.01, colorPos ), gradient.type ) );
				}
				#ifndef UNITY_COLORSPACE_GAMMA
				color = GammaToLinearSpace( color );
				#endif
				float alpha = gradient.alphas[ 0 ].x;
				UNITY_UNROLL
				for( int a = 1; a < 8; a++ )
				{
					float alphaPos = saturate( ( time - gradient.alphas[ a - 1 ].y ) / ( gradient.alphas[ a ].y - gradient.alphas[ a - 1 ].y ) ) * step( a, (float)gradient.alphasLength - 1 );
					alpha = lerp( alpha, gradient.alphas[ a ].x, lerp( alphaPos, step( 0.01, alphaPos ), gradient.type ) );
				}
				return float4( color, alpha );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				Gradient gradient = NewGradient( _GType, _GColorNum, _GAlphaNum, _Col0, _Col1, _Col2, _Col3, _Col4, _Col5, _Col6, _Col7, _Alp0.xy, _Alp1.xy, _Alp2.xy, _Alp3.xy, _Alp4.xy, _Alp5.xy, _Alp6.xy, _Alp7.xy );
				float time = tex2D( _GTime, i.uv ).r;
				return SampleGradient( gradient, time );
			}
			ENDCG
		}
	}
}
