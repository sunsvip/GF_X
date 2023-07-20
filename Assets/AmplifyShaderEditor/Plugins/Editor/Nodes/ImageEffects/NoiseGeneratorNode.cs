// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
// Based on the work by https://github.com/keijiro/NoiseShader

using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public enum NoiseGeneratorType
	{
		Simplex2D,
		Simplex3D,
		Gradient,
		Simple
	};

	[Serializable]
	[NodeAttributes( "Noise Generator", "Miscellaneous", "Collection of procedural noise generators", tags: "simplex gradient" )]
	public sealed class NoiseGeneratorNode : ParentNode
	{
		private const string TypeLabelStr = "Type";
		private const string SetTo01RangeOpStr = "{0} = {0}*0.5 + 0.5;";
		private const string SetToMinus1To1RangeOpStr = "{0} = {0}*2 - 1;";
		private const string SetTo01RangeLabel = "0-1 Range";
		private const string SetTo01RangePreviewId = "_To01Range";
		private const string UseUnityVersionLabel = "Use Unity Version";

		// Simple
		private const string SimpleNoiseRandomValueFunc = "inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }";
		private const string SimpleNoiseInterpolateFunc = "inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }";
		private const string SimpleValueNoiseHeader = "inline float valueNoise (float2 uv)";
		private readonly string[] SimpleValueNoiseBody = {   "inline float valueNoise (float2 uv)\n",
														"{\n",
														"\tfloat2 i = floor(uv);\n",
														"\tfloat2 f = frac( uv );\n",
														"\tf = f* f * (3.0 - 2.0 * f);\n",
														"\tuv = abs( frac(uv) - 0.5);\n",
														"\tfloat2 c0 = i + float2( 0.0, 0.0 );\n",
														"\tfloat2 c1 = i + float2( 1.0, 0.0 );\n",
														"\tfloat2 c2 = i + float2( 0.0, 1.0 );\n",
														"\tfloat2 c3 = i + float2( 1.0, 1.0 );\n",
														"\tfloat r0 = noise_randomValue( c0 );\n",
														"\tfloat r1 = noise_randomValue( c1 );\n",
														"\tfloat r2 = noise_randomValue( c2 );\n",
														"\tfloat r3 = noise_randomValue( c3 );\n",
														"\tfloat bottomOfGrid = noise_interpolate( r0, r1, f.x );\n",
														"\tfloat topOfGrid = noise_interpolate( r2, r3, f.x );\n",
														"\tfloat t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );\n",
														"\treturn t;\n",
														"}\n"};

		private const string SimpleNoiseHeader = "float SimpleNoise(float2 UV, float Scale)";
		private const string SimpleNoiseFunc = "SimpleNoise( {0} )";
		private readonly string[] SimpleNoiseBody = {   "float SimpleNoise(float2 UV)\n",
														"{\n",
														"\tfloat t = 0.0;\n",
														"\tfloat freq = pow( 2.0, float( 0 ) );\n",
														"\tfloat amp = pow( 0.5, float( 3 - 0 ) );\n",
														"\tt += valueNoise( UV/freq )*amp;\n",
														"\tfreq = pow(2.0, float(1));\n",
														"\tamp = pow(0.5, float(3-1));\n",
														"\tt += valueNoise( UV/freq )*amp;\n",
														"\tfreq = pow(2.0, float(2));\n",
														"\tamp = pow(0.5, float(3-2));\n",
														"\tt += valueNoise( UV/freq )*amp;\n",
														"\treturn t;\n",
														"}\n"};

		// Simplex 2D
		private const string Simplex2DFloat3Mod289Func = "float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }";
		private const string Simplex2DFloat2Mod289Func = "float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }";
		private const string Simplex2DPermuteFunc = "float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }";

		private const string SimplexNoise2DHeader = "float snoise( float2 v )";
		private const string SimplexNoise2DFunc = "snoise( {0} )";
		private readonly string[] SimplexNoise2DBody = {"float snoise( float2 v )\n",
														"{\n",
														"\tconst float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );\n",
														"\tfloat2 i = floor( v + dot( v, C.yy ) );\n",
														"\tfloat2 x0 = v - i + dot( i, C.xx );\n",
														"\tfloat2 i1;\n",
														"\ti1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );\n",
														"\tfloat4 x12 = x0.xyxy + C.xxzz;\n",
														"\tx12.xy -= i1;\n",
														"\ti = mod2D289( i );\n",
														"\tfloat3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );\n",
														"\tfloat3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );\n",
														"\tm = m * m;\n",
														"\tm = m * m;\n",
														"\tfloat3 x = 2.0 * frac( p * C.www ) - 1.0;\n",
														"\tfloat3 h = abs( x ) - 0.5;\n",
														"\tfloat3 ox = floor( x + 0.5 );\n",
														"\tfloat3 a0 = x - ox;\n",
														"\tm *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );\n",
														"\tfloat3 g;\n",
														"\tg.x = a0.x * x0.x + h.x * x0.y;\n",
														"\tg.yz = a0.yz * x12.xz + h.yz * x12.yw;\n",
														"\treturn 130.0 * dot( m, g );\n",
														"}\n"};
		// Simplex 3D



		private const string Simplex3DFloat3Mod289 = "float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }";
		private const string Simplex3DFloat4Mod289 = "float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }";
		private const string Simplex3DFloat4Permute = "float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }";
		private const string TaylorInvSqrtFunc = "float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }";

		private const string SimplexNoise3DHeader = "float snoise( float3 v )";
		private const string SimplexNoise3DFunc = "snoise( {0} )";
		private readonly string[] SimplexNoise3DBody =
		{
			"float snoise( float3 v )\n",
			"{\n",
			"\tconst float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );\n",
			"\tfloat3 i = floor( v + dot( v, C.yyy ) );\n",
			"\tfloat3 x0 = v - i + dot( i, C.xxx );\n",
			"\tfloat3 g = step( x0.yzx, x0.xyz );\n",
			"\tfloat3 l = 1.0 - g;\n",
			"\tfloat3 i1 = min( g.xyz, l.zxy );\n",
			"\tfloat3 i2 = max( g.xyz, l.zxy );\n",
			"\tfloat3 x1 = x0 - i1 + C.xxx;\n",
			"\tfloat3 x2 = x0 - i2 + C.yyy;\n",
			"\tfloat3 x3 = x0 - 0.5;\n",
			"\ti = mod3D289( i);\n",
			"\tfloat4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );\n",
			"\tfloat4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)\n",
			"\tfloat4 x_ = floor( j / 7.0 );\n",
			"\tfloat4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)\n",
			"\tfloat4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;\n",
			"\tfloat4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;\n",
			"\tfloat4 h = 1.0 - abs( x ) - abs( y );\n",
			"\tfloat4 b0 = float4( x.xy, y.xy );\n",
			"\tfloat4 b1 = float4( x.zw, y.zw );\n",
			"\tfloat4 s0 = floor( b0 ) * 2.0 + 1.0;\n",
			"\tfloat4 s1 = floor( b1 ) * 2.0 + 1.0;\n",
			"\tfloat4 sh = -step( h, 0.0 );\n",
			"\tfloat4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;\n",
			"\tfloat4 a1 = b1.xzyw + s1.xzyw * sh.zzww;\n",
			"\tfloat3 g0 = float3( a0.xy, h.x );\n",
			"\tfloat3 g1 = float3( a0.zw, h.y );\n",
			"\tfloat3 g2 = float3( a1.xy, h.z );\n",
			"\tfloat3 g3 = float3( a1.zw, h.w );\n",
			"\tfloat4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );\n",
			"\tg0 *= norm.x;\n",
			"\tg1 *= norm.y;\n",
			"\tg2 *= norm.z;\n",
			"\tg3 *= norm.w;\n",
			"\tfloat4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );\n",
			"\tm = m* m;\n",
			"\tm = m* m;\n",
			"\tfloat4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );\n",
			"\treturn 42.0 * dot( m, px);\n",
			"}\n"
		};

		//Gradient Noise
		private readonly string UnityGradientNoiseFunc = "UnityGradientNoise({0},{1})";
		private readonly string[] UnityGradientNoiseFunctionsBody =
		{
			"float2 UnityGradientNoiseDir( float2 p )\n",
			"{\n",
			"\tp = fmod(p , 289);\n",
			"\tfloat x = fmod((34 * p.x + 1) * p.x , 289) + p.y;\n",
			"\tx = fmod( (34 * x + 1) * x , 289);\n",
			"\tx = frac( x / 41 ) * 2 - 1;\n",
			"\treturn normalize( float2(x - floor(x + 0.5 ), abs( x ) - 0.5 ) );\n",
			"}\n",
			"\n",
			"float UnityGradientNoise( float2 UV, float Scale )\n",
			"{\n",
			"\tfloat2 p = UV * Scale;\n",
			"\tfloat2 ip = floor( p );\n",
			"\tfloat2 fp = frac( p );\n",
			"\tfloat d00 = dot( UnityGradientNoiseDir( ip ), fp );\n",
			"\tfloat d01 = dot( UnityGradientNoiseDir( ip + float2( 0, 1 ) ), fp - float2( 0, 1 ) );\n",
			"\tfloat d10 = dot( UnityGradientNoiseDir( ip + float2( 1, 0 ) ), fp - float2( 1, 0 ) );\n",
			"\tfloat d11 = dot( UnityGradientNoiseDir( ip + float2( 1, 1 ) ), fp - float2( 1, 1 ) );\n",
			"\tfp = fp * fp * fp * ( fp * ( fp * 6 - 15 ) + 10 );\n",
			"\treturn lerp( lerp( d00, d01, fp.y ), lerp( d10, d11, fp.y ), fp.x ) + 0.5;\n",
			"}\n"
		};
		private readonly string GradientNoiseFunc = "GradientNoise({0},{1})";
		private readonly string[] GradientNoiseFunctionsBody =
		{
			"//https://www.shadertoy.com/view/XdXGW8\n",
			"float2 GradientNoiseDir( float2 x )\n",
			"{\n",
			"\tconst float2 k = float2( 0.3183099, 0.3678794 );\n",
			"\tx = x * k + k.yx;\n",
			"\treturn -1.0 + 2.0 * frac( 16.0 * k * frac( x.x * x.y * ( x.x + x.y ) ) );\n",
			"}\n",
			"\n",
			"float GradientNoise( float2 UV, float Scale )\n",
			"{\n",
			"\tfloat2 p = UV * Scale;\n",
			"\tfloat2 i = floor( p );\n",
			"\tfloat2 f = frac( p );\n",
			"\tfloat2 u = f * f * ( 3.0 - 2.0 * f );\n",
			"\treturn lerp( lerp( dot( GradientNoiseDir( i + float2( 0.0, 0.0 ) ), f - float2( 0.0, 0.0 ) ),\n",
			"\t\t\tdot( GradientNoiseDir( i + float2( 1.0, 0.0 ) ), f - float2( 1.0, 0.0 ) ), u.x ),\n",
			"\t\t\tlerp( dot( GradientNoiseDir( i + float2( 0.0, 1.0 ) ), f - float2( 0.0, 1.0 ) ),\n",
			"\t\t\tdot( GradientNoiseDir( i + float2( 1.0, 1.0 ) ), f - float2( 1.0, 1.0 ) ), u.x ), u.y );\n",
			"}\n"
		};
		
		[SerializeField]
		private NoiseGeneratorType m_type = NoiseGeneratorType.Simplex2D;

		[SerializeField]
		private bool m_setTo01Range = true;

		[SerializeField]
		private bool m_unityVersion = false;
		private int m_setTo01RangePreviewId;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV" );
			AddInputPort( WirePortDataType.FLOAT, false, "Scale" );
			m_inputPorts[ 1 ].FloatInternalData = 1;
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_type ) );
			m_previewShaderGUID = "cd2d37ef5da190b42a91a5a690ba2a7d";
			ConfigurePorts();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			m_setTo01RangePreviewId = Shader.PropertyToID( SetTo01RangePreviewId );
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();
			if( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			float range01 = m_setTo01Range ? 1 : 0;
			PreviewMaterial.SetFloat( m_setTo01RangePreviewId, range01 );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_upperLeftWidget.DrawWidget<NoiseGeneratorType>( ref m_type, this, OnWidgetUpdate );
		}

		private readonly Action<ParentNode> OnWidgetUpdate = ( x ) =>
		{
			( x as NoiseGeneratorNode ).ConfigurePorts();
		};

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_type = (NoiseGeneratorType)EditorGUILayoutEnumPopup( TypeLabelStr, m_type );
			if( EditorGUI.EndChangeCheck() )
			{
				ConfigurePorts();
			}
			
			m_setTo01Range = EditorGUILayoutToggle( SetTo01RangeLabel, m_setTo01Range );

			if( m_type == NoiseGeneratorType.Gradient )
			{
				EditorGUI.BeginChangeCheck();
				m_unityVersion = EditorGUILayoutToggle( UseUnityVersionLabel, m_unityVersion );
				if( EditorGUI.EndChangeCheck() )
				{
					ConfigurePorts();
				}
			}
			//EditorGUILayout.HelpBox( "Node still under construction. Use with caution", MessageType.Info );
		}

		private void ConfigurePorts()
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_type ) );

			switch( m_type )
			{
				case NoiseGeneratorType.Simplex2D:
				{
					m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
					m_previewMaterialPassId = 0;
				}
				break;

				case NoiseGeneratorType.Simplex3D:
				{
					m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT3, false );
					m_previewMaterialPassId = 1;
				}
				break;
				case NoiseGeneratorType.Gradient:
				{
					m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
					m_previewMaterialPassId = m_unityVersion ? 3 : 2;
				}
				break;
				case NoiseGeneratorType.Simple:
				{
					m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
					m_previewMaterialPassId = 4;
				}
				break;
			}
			PreviewIsDirty = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ outputId ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}

			string size = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string scale = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			switch( m_type )
			{
				case NoiseGeneratorType.Simplex2D:
				{

					string float3Mod289Func = ( dataCollector.IsTemplate ) ? Simplex2DFloat3Mod289Func : "\t\t" + Simplex2DFloat3Mod289Func;
					dataCollector.AddFunction( Simplex2DFloat3Mod289Func, float3Mod289Func );

					string float2Mod289Func = ( dataCollector.IsTemplate ) ? Simplex2DFloat2Mod289Func : "\t\t" + Simplex2DFloat2Mod289Func;
					dataCollector.AddFunction( Simplex2DFloat2Mod289Func, float2Mod289Func );

					string permuteFunc = ( dataCollector.IsTemplate ) ? Simplex2DPermuteFunc : "\t\t" + Simplex2DPermuteFunc;
					dataCollector.AddFunction( Simplex2DPermuteFunc, permuteFunc );

					dataCollector.AddFunction( SimplexNoise2DHeader, SimplexNoise2DBody, false );


					if( m_inputPorts[ 1 ].IsConnected || m_inputPorts[ 1 ].FloatInternalData != 1.0f )
					{
						size = string.Format( "{0}*{1}", size, scale );
					}

					RegisterLocalVariable( 0, string.Format( SimplexNoise2DFunc, size ), ref dataCollector, ( "simplePerlin2D" + OutputId ) );
				}
				break;
				case NoiseGeneratorType.Simplex3D:
				{

					string float3Mod289Func = ( dataCollector.IsTemplate ) ? Simplex3DFloat3Mod289 : "\t\t" + Simplex3DFloat3Mod289;
					dataCollector.AddFunction( Simplex3DFloat3Mod289, float3Mod289Func );

					string float4Mod289Func = ( dataCollector.IsTemplate ) ? Simplex3DFloat4Mod289 : "\t\t" + Simplex3DFloat4Mod289;
					dataCollector.AddFunction( Simplex3DFloat4Mod289, float4Mod289Func );

					string permuteFunc = ( dataCollector.IsTemplate ) ? Simplex3DFloat4Permute : "\t\t" + Simplex3DFloat4Permute;
					dataCollector.AddFunction( Simplex3DFloat4Permute, permuteFunc );

					string taylorInvSqrtFunc = ( dataCollector.IsTemplate ) ? TaylorInvSqrtFunc : "\t\t" + TaylorInvSqrtFunc;
					dataCollector.AddFunction( TaylorInvSqrtFunc, taylorInvSqrtFunc );

					dataCollector.AddFunction( SimplexNoise3DHeader, SimplexNoise3DBody, false );

					if( m_inputPorts[ 1 ].IsConnected || m_inputPorts[ 1 ].FloatInternalData != 1.0f )
					{
						size = string.Format( "{0}*{1}", size, scale );
					}

					RegisterLocalVariable( 0, string.Format( SimplexNoise3DFunc, size ), ref dataCollector, ( "simplePerlin3D" + OutputId ) );
				}
				break;

				case NoiseGeneratorType.Gradient:
				{
					string[] body = m_unityVersion ? UnityGradientNoiseFunctionsBody : GradientNoiseFunctionsBody;
					string func = m_unityVersion ? UnityGradientNoiseFunc : GradientNoiseFunc;

					dataCollector.AddFunction( body[ 0 ], body, false);
					RegisterLocalVariable( 0, string.Format( func, size, scale ), ref dataCollector, ( "gradientNoise" + OutputId ) );
				}
				break;

				case NoiseGeneratorType.Simple:
				{
					string randomValue = ( dataCollector.IsTemplate ) ? SimpleNoiseRandomValueFunc : "\t\t" + SimpleNoiseRandomValueFunc;
					dataCollector.AddFunction( SimpleNoiseRandomValueFunc, randomValue );

					string interpolate = ( dataCollector.IsTemplate ) ? SimpleNoiseInterpolateFunc : "\t\t" + SimpleNoiseInterpolateFunc;
					dataCollector.AddFunction( SimpleNoiseInterpolateFunc, interpolate );

					dataCollector.AddFunction( SimpleValueNoiseHeader, SimpleValueNoiseBody, false );

					dataCollector.AddFunction( SimpleNoiseHeader, SimpleNoiseBody, false );

					if( m_inputPorts[ 1 ].IsConnected || m_inputPorts[ 1 ].FloatInternalData != 1.0f )
					{
						size = string.Format( "{0}*{1}", size, scale );
					}
					RegisterLocalVariable( 0, string.Format( SimpleNoiseFunc, size ), ref dataCollector, ( "simpleNoise" + OutputId ) );
				}
				break;
			}

			if( m_type == NoiseGeneratorType.Simple && !m_setTo01Range )
			{
				dataCollector.AddLocalVariable( outputId, string.Format( SetToMinus1To1RangeOpStr, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) ) );
			}

			if( m_setTo01Range && m_type != NoiseGeneratorType.Simple )
			{
				dataCollector.AddLocalVariable( outputId, string.Format( SetTo01RangeOpStr, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) ) );
			}

			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_type = (NoiseGeneratorType)Enum.Parse( typeof( NoiseGeneratorType ), GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() < 16903 )
			{
				m_setTo01Range = false;
			}
			else
			{
				m_setTo01Range = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_unityVersion = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			ConfigurePorts();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_type );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_setTo01Range );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_unityVersion );
		}
	}
}
