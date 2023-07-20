Shader "Hidden/VoronoiNode"
{
	Properties
	{
		_A("_RGB", 2D) = "white" {}
		_B("_RGB", 2D) = "white" {}
		_C("_RGB", 2D) = "white" {}
		_D ("_RGB", 2D) = "white" {}
		_UseTileScale("_UseTileScale", Float) = 0
		_TileScale ("_TileScale", Int) = 1
		_MinkowskiPower("_MinkowskiPower", Float) = 0
		_DistFunc("_DistFunc", Int) = 0 
		_MethodType("_MethodType", Int) = 0
		_SearchQuality("_SearchQuality", Int) = 1
		_Octaves("_Octaves", Int) = 1
		_UseSmoothness("_UseSmoothness", Int ) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		CGINCLUDE
		sampler2D _A;
		sampler2D _B;
		sampler2D _C;
		sampler2D _D;
		float _UseTileScale = 0;
		int _TileScale = 1;
		float _MinkowskiPower = 1;
		int _DistFunc = 0;
		int _MethodType = 0;
		int _SearchQuality = 0;
		int _Octaves = 1;
		int _PreviewID = 0;
		int _UseSmoothness = 0;
		int _CustomUVs;

		float2 VoronoiHash( float2 p )
		{
			p = lerp( p,  p - _TileScale * floor (p / _TileScale), _UseTileScale );
			p = float2(dot (p, float2(127.1, 311.7)), dot (p, float2(269.5, 183.3)));
			return frac (sin (p) *43758.5453);
		}

		float Voronoi( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		{
			float2 n = floor(v);
			float2 f = frac(v);
			float F1 = 8.0;
			float F2 = 8.0; 
			float2 mg = 0;
			for (int j = -_SearchQuality; j <= _SearchQuality; j++)
			{
				for (int i = -_SearchQuality; i <= _SearchQuality; i++)
				{
					float2 g = float2(i, j);
					float2 o = VoronoiHash (n + g);
					o = (sin (time + o * 6.2831) * 0.5 + 0.5); float2 r = f - g - o;
					float d = 0;
					//Euclidean^2
					if (_DistFunc == 0)
					{
						d = 0.5 * dot (r, r);
					}
					//Euclidean
					else if (_DistFunc == 1)
					{
						d = 0.707 * sqrt (dot (r, r));
					}
					//Manhattan
					else if (_DistFunc == 2)
					{
						d = 0.5 * (abs (r.x) + abs (r.y));
					}
					//Chebyshev
					else if (_DistFunc == 3)
					{
						d = max (abs (r.x), abs (r.y));
					}
					//Minkowski
					else if (_DistFunc == 4)
					{
						d = (1 / pow(2, 1 / _MinkowskiPower))  * pow( ( pow( abs( r.x ), _MinkowskiPower) + pow( abs( r.y ), _MinkowskiPower) ),  (1 / _MinkowskiPower));
					}

					if (_MethodType == 0 && _UseSmoothness == 1)
					{
						float h = smoothstep (0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness);
						F1 = lerp (F1, d, h) - smoothness * h * (1.0 - h);
						mg = g; mr = r; id = o;
					}
					else
					{
						if (d < F1)
						{
							F2 = F1;
							F1 = d; mg = g; mr = r; id = o;
						}
						else if (d < F2)
						{
							F2 = d;
						}
						
					}

				}
			}

			//Cells
			if(_MethodType == 0)
			{
				return F1;
			}
			//Crystal
			else if (_MethodType == 1)
			{
				return F2;
			}
			//Glass 
			else if (_MethodType == 2)
			{
				return F2 - F1;
			}
			//Caustic
			else if (_MethodType == 3)
			{
				return (F2 + F1) * 0.5;
			}
			//Distance
			else if (_MethodType == 4)
			{
				F1 = 8.0;
				for (int j = -2; j <= 2; j++)
				{
					for (int i = -2; i <= 2; i++)
					{
						float2 g = mg + float2(i, j);
						float2 o = VoronoiHash (n + g);
						o = ( sin (time + o * 6.2831) * 0.5 + 0.5); 
						float2 r = f - g - o;
						float d = dot (0.5 * (mr + r), normalize (r - mr));
						F1 = min (F1, d);
					}
				}
				return F1;
			}
			else
				return F1;
		}


		ENDCG

		Pass // Voronoi - Unity
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			inline float2 UnityVoronoiRandomVector (float2 UV, float offset)
			{
				float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
				UV = frac (sin (mul (UV, m)) * 46839.32);
				return float2(sin (UV.y* +offset) * 0.5 + 0.5, cos (UV.x* offset) * 0.5 + 0.5);
			}

			//x - Out y - Cells
			float3 UnityVoronoi (float2 UV, float AngleOffset, float CellDensity, inout float2 mr)
			{
				float2 g = floor (UV * CellDensity);
				float2 f = frac (UV * CellDensity);
				float t = 8.0;
				float3 res = float3(8.0, 0.0, 0.0);

				for (int y = -1; y <= 1; y++)
				{
					for (int x = -1; x <= 1; x++)
					{
						float2 lattice = float2(x, y);
						float2 offset = UnityVoronoiRandomVector (lattice + g, AngleOffset);
						float d = distance (lattice + offset, f);
						if (d < res.x)
						{
							mr = f - lattice - offset;
							res = float3(d, offset.x, offset.y);
						}
					}
				}
				return res;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float2 uvValue = i.uv;
				if (_CustomUVs == 1)
						uvValue = tex2D(_A, i.uv).rg;
				float angleOffset = tex2D(_B, i.uv).r;
				float cellDensity = tex2D(_C, i.uv).r;
				float2 uv = 0;
				float3 voronoiVal = UnityVoronoi( uvValue, angleOffset , cellDensity, uv );
				if( _PreviewID == 2)
					return float4( uv, 0, 1 );
				else if( _PreviewID == 1)
					return float4( voronoiVal.yz, 0, 1 );
				else
					return float4( voronoiVal.xxx, 1);
			}
			ENDCG
		}

		Pass // Voronoi - ASE
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag (v2f_img i) : SV_Target
			{
				float2 uvValue = i.uv;
				if (_CustomUVs == 1)
						uvValue = tex2D(_A, i.uv).rg;
				float time = tex2D (_B, i.uv).r;
				float scale = tex2D (_C, i.uv).r;
				float smoothness = tex2D (_D, i.uv).r;
				
				float2 id = 0;
				float2 uv = 0;
				float voronoiVal = Voronoi( uvValue*scale,time, id, uv, smoothness );
				if (_Octaves == 1)
				{
					if( _PreviewID == 2)
						return float4( uv, 0, 1 );
					else if( _PreviewID == 1)
						return float4( id, 0, 1 );
					else
						return float4(voronoiVal.xxx, 1);
				}
				else
				{
					float fade = 0.5;
					float voroi = 0;
					float rest = 0;
					for (int it = 0; it < _Octaves; it++)
					{
						voroi += fade * Voronoi( uvValue*scale, time, id, uv, smoothness);
						rest += fade;
						uvValue *= 2;
						fade *= 0.5;
					}
					voroi /= rest;
					if( _PreviewID == 2)
						return float4( uv, 0, 1 );
					else if( _PreviewID == 1)
						return float4( id, 0, 1 );
					else
						return float4(voroi.xxx, 1);
				}
			}
			ENDCG
		}
	}
}
