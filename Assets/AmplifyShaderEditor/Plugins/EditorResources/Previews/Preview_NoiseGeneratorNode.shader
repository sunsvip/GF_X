Shader "Hidden/NoiseGeneratorNode"
{
	Properties
	{
		_A ("_RGB", 2D) = "white" {}
		_B ("_RGB", 2D) = "white" {}
		_To01Range ("_To01Range", Float) = 0
	}
	
	SubShader
	{
		CGINCLUDE
		sampler2D _A;
		sampler2D _B;
		float _To01Range;
		ENDCG

		Pass //Simplex2D
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag


			float3 mod2D289 ( float3 x ) { return x - floor ( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289 ( float2 x ) { return x - floor ( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute ( float3 x ) { return mod2D289 ( ( ( x * 34.0 ) + 1.0 ) * x ); }

			float snoise ( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor ( v + dot ( v, C.yy ) );
				float2 x0 = v - i + dot ( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289 ( i );
				float3 p = permute ( permute ( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max ( 0.5 - float3( dot ( x0, x0 ), dot ( x12.xy, x12.xy ), dot ( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac ( p * C.www ) - 1.0;
				float3 h = abs ( x ) - 0.5;
				float3 ox = floor ( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot ( m, g );
			}
			float4 frag(v2f_img i) : SV_Target
			{
				float2 size = tex2D( _A, i.uv ).rg;
				float scale = tex2D (_B, i.uv).r;
				float noiseVal = snoise ( size * scale );
				noiseVal = (_To01Range > 0) ? noiseVal * 0.5 + 0.5 : noiseVal;
				return float4( noiseVal.xxx, 1);
			}
			ENDCG
		}

		Pass //Simplex3D
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float3 mod3D289 ( float3 x ) { return x - floor ( x / 289.0 ) * 289.0; }

			float4 mod3D289 ( float4 x ) { return x - floor ( x / 289.0 ) * 289.0; }

			float4 permute ( float4 x ) { return mod3D289 ( ( x * 34.0 + 1.0 ) * x ); }

			float4 taylorInvSqrt ( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

			float snoise ( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor ( v + dot ( v, C.yyy ) );
				float3 x0 = v - i + dot ( i, C.xxx );
				float3 g = step ( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min ( g.xyz, l.zxy );
				float3 i2 = max ( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289 ( i );
				float4 p = permute ( permute ( permute ( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor ( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor ( j / 7.0 );
				float4 y_ = floor ( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs ( x ) - abs ( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor ( b0 ) * 2.0 + 1.0;
				float4 s1 = floor ( b1 ) * 2.0 + 1.0;
				float4 sh = -step ( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt ( float4( dot ( g0, g0 ), dot ( g1, g1 ), dot ( g2, g2 ), dot ( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max ( 0.6 - float4( dot ( x0, x0 ), dot ( x1, x1 ), dot ( x2, x2 ), dot ( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot ( x0, g0 ), dot ( x1, g1 ), dot ( x2, g2 ), dot ( x3, g3 ) );
				return 42.0 * dot ( m, px );
			}

			float4 frag ( v2f_img i ) : SV_Target
			{
				float3 size = tex2D ( _A, i.uv ).rgb;
				float scale = tex2D (_B, i.uv).r;
				float noiseVal = snoise ( size * scale );
				noiseVal = (_To01Range > 0) ? noiseVal * 0.5 + 0.5 : noiseVal;
				return float4( noiseVal.xxx, 1 );
			}
			ENDCG
		}

		Pass // Gradient - Shader Toy
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			//https://www.shadertoy.com/view/XdXGW8
			float2 GradientNoiseDir (float2 x)
			{
				const float2 k = float2(0.3183099, 0.3678794);
				x = x * k + k.yx;
				return -1.0 + 2.0 * frac (16.0 * k * frac (x.x * x.y * (x.x + x.y)));
			}

			float GradientNoise (float2 UV, float Scale)
			{
				float2 p = UV * Scale;
				float2 i = floor (p);
				float2 f = frac (p);
				float2 u = f * f * (3.0 - 2.0 * f);
				return lerp (lerp (dot (GradientNoiseDir (i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
						dot (GradientNoiseDir (i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
						lerp (dot (GradientNoiseDir (i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
						dot (GradientNoiseDir (i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float3 size = tex2D (_A, i.uv).rgb;
				float scale = tex2D (_B, i.uv).r;
				float noiseVal = GradientNoise (size , scale);
				noiseVal = (_To01Range > 0) ? noiseVal * 0.5 + 0.5 : noiseVal;
				return float4(noiseVal.xxx, 1);
			}
			ENDCG
		}

		Pass // Gradient - Unity
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float2 UnityGradientNoiseDir (float2 p)
			{
				p = fmod (p , 289);
				float x = fmod ((34 * p.x + 1) * p.x , 289) + p.y;
				x = fmod ((34 * x + 1) * x , 289);
				x = frac (x / 41) * 2 - 1;
				return normalize (float2(x - floor (x + 0.5), abs (x) - 0.5));
			}

			float UnityGradientNoise (float2 UV, float Scale)
			{
				float2 p = UV * Scale;
				float2 ip = floor (p);
				float2 fp = frac (p);
				float d00 = dot (UnityGradientNoiseDir (ip), fp);
				float d01 = dot (UnityGradientNoiseDir (ip + float2(0, 1)), fp - float2(0, 1));
				float d10 = dot (UnityGradientNoiseDir (ip + float2(1, 0)), fp - float2(1, 0));
				float d11 = dot (UnityGradientNoiseDir (ip + float2(1, 1)), fp - float2(1, 1));
				fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
				return lerp (lerp (d00, d01, fp.y), lerp (d10, d11, fp.y), fp.x) + 0.5;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float3 size = tex2D (_A, i.uv).rgb;
				float scale = tex2D (_B, i.uv).r;
				float noiseVal = UnityGradientNoise(size , scale);
				noiseVal = (_To01Range > 0) ? noiseVal * 0.5 + 0.5 : noiseVal;
				return float4(noiseVal.xxx, 1);
			}
			ENDCG
		}

		Pass // Simple
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }
			inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }
			inline float valueNoise (float2 uv)
			{
				float2 i = floor(uv);
				float2 f = frac( uv );
				f = f* f * (3.0 - 2.0 * f);
				uv = abs( frac(uv) - 0.5);
				float2 c0 = i + float2( 0.0, 0.0 );
				float2 c1 = i + float2( 1.0, 0.0 );
				float2 c2 = i + float2( 0.0, 1.0 );
				float2 c3 = i + float2( 1.0, 1.0 );
				float r0 = noise_randomValue( c0 );
				float r1 = noise_randomValue( c1 );
				float r2 = noise_randomValue( c2 );
				float r3 = noise_randomValue( c3 );
				float bottomOfGrid = noise_interpolate( r0, r1, f.x );
				float topOfGrid = noise_interpolate( r2, r3, f.x );
				float t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );
				return t;
			}
			
			float SimpleNoise(float2 UV)
			{
				float t = 0.0;
				float freq = pow( 2.0, float( 0 ) );
				float amp = pow( 0.5, float( 3 - 0 ) );
				t += valueNoise( UV/freq )*amp;
				freq = pow(2.0, float(1));
				amp = pow(0.5, float(3-1));
				t += valueNoise( UV/freq )*amp;
				freq = pow(2.0, float(2));
				amp = pow(0.5, float(3-2));
				t += valueNoise( UV/freq )*amp;
				return t;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float3 size = tex2D (_A, i.uv).rgb;
				float scale = tex2D (_B, i.uv).r;
				float noiseVal = SimpleNoise(size * scale);
				noiseVal = (_To01Range == 0) ? noiseVal * 2 - 1 : noiseVal;
				return float4(noiseVal.xxx, 1);
			}
			ENDCG
		}
	}
}
