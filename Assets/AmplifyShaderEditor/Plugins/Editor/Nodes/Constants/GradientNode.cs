// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Gradient", "Constants And Properties", "Gradient property" )]
	public sealed class GradientNode : ParentNode
	{
		[SerializeField]
		private Gradient m_gradient = new Gradient();

		private string m_functionHeader = "NewGradient( {0}, {1}, {2}," +
			" {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}," +
			" {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18} )";
		private string m_functionBody = string.Empty;

		private string m_functionHeaderStruct = "Gradient( {0} )";
		private string m_functionBodyStruct = string.Empty;

		public Gradient Gradient { get { return m_gradient; } }

		public GradientNode() : base() { }
		public GradientNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_insideSize.Set( 128, m_insideSize.y );
			AddOutputPort( WirePortDataType.OBJECT, Constants.EmptyPortValue );
			m_autoWrapProperties = true;
			m_textLabelWidth = 100;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			{
				m_gradient = EditorGUILayoutEx.GradientField( "Gradient" , m_gradient );
			}
			if( EditorGUI.EndChangeCheck() )
			{
				PreviewIsDirty = true;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if( !m_isVisible )
				return;

			EditorGUI.BeginChangeCheck();
			{
				m_gradient = EditorGUIEx.GradientField( m_remainingBox , m_gradient );
			}
			if( EditorGUI.EndChangeCheck() )
			{
				PreviewIsDirty = true;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			m_functionBodyStruct = string.Empty;
			m_functionBody = string.Empty;
			if( !dataCollector.IsSRP )
			{
				GenerateGradientStruct( ref m_functionBodyStruct );
				dataCollector.AddFunctions( m_functionHeaderStruct, m_functionBodyStruct, "0" );
				GenerateGradient( ref m_functionBody );
			}
			else
			{
				dataCollector.AddToIncludes( UniqueId, "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl" );
			}

			string[] colors = new string[ 8 ];
			for( int i = 0; i < 8; i++ )
			{
				if( i < m_gradient.colorKeys.Length )
				{
					colors[ i ] = "float4( " + m_gradient.colorKeys[ i ].color.r + ", " + m_gradient.colorKeys[ i ].color.g + ", " + m_gradient.colorKeys[ i ].color.b + ", " + m_gradient.colorKeys[ i ].time + " )";
				}
				else
				{
					colors[ i ] = "0";
				}
			}

			string[] alphas = new string[ 8 ];
			for( int i = 0; i < 8; i++ )
			{
				if( i < m_gradient.alphaKeys.Length )
				{
					alphas[ i ] = "float2( " + m_gradient.alphaKeys[ i ].alpha + ", " + m_gradient.alphaKeys[ i ].time + " )";
				}
				else
				{
					alphas[ i ] = "0";
				}
			}

			string functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, (int)m_gradient.mode, m_gradient.colorKeys.Length, m_gradient.alphaKeys.Length
				, colors[ 0 ], colors[ 1 ], colors[ 2 ], colors[ 3 ], colors[ 4 ], colors[ 5 ], colors[ 6 ], colors[ 7 ]
				, alphas[ 0 ], alphas[ 1 ], alphas[ 2 ], alphas[ 3 ], alphas[ 4 ], alphas[ 5 ], alphas[ 6 ], alphas[ 7 ] );

			dataCollector.AddLocalVariable( UniqueId, "Gradient gradient" + OutputId + " = " + functionResult + ";" );

			return "gradient" + OutputId;
		}

		public static void GenerateGradientStruct( ref string body )
		{
			body = string.Empty;
			IOUtils.AddFunctionHeader( ref body, "struct Gradient" );
			IOUtils.AddFunctionLine( ref body, "int type;" );
			IOUtils.AddFunctionLine( ref body, "int colorsLength;" );
			IOUtils.AddFunctionLine( ref body, "int alphasLength;" );
			IOUtils.AddFunctionLine( ref body, "float4 colors[8];" );
			IOUtils.AddFunctionLine( ref body, "float2 alphas[8];" );
			IOUtils.AddSingleLineFunction( ref body, "};\n" );
		}

		public static void GenerateGradient( ref string body )
		{
			body = string.Empty;
			IOUtils.AddFunctionHeader( ref body, "Gradient NewGradient(int type, int colorsLength, int alphasLength, \n\t\tfloat4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,\n\t\tfloat2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)" );
			IOUtils.AddFunctionLine( ref body, "Gradient g;" );
			IOUtils.AddFunctionLine( ref body, "g.type = type;" );
			IOUtils.AddFunctionLine( ref body, "g.colorsLength = colorsLength;" );
			IOUtils.AddFunctionLine( ref body, "g.alphasLength = alphasLength;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 0 ] = colors0;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 1 ] = colors1;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 2 ] = colors2;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 3 ] = colors3;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 4 ] = colors4;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 5 ] = colors5;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 6 ] = colors6;" );
			IOUtils.AddFunctionLine( ref body, "g.colors[ 7 ] = colors7;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 0 ] = alphas0;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 1 ] = alphas1;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 2 ] = alphas2;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 3 ] = alphas3;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 4 ] = alphas4;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 5 ] = alphas5;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 6 ] = alphas6;" );
			IOUtils.AddFunctionLine( ref body, "g.alphas[ 7 ] = alphas7;" );
			IOUtils.AddFunctionLine( ref body, "return g;" );
			IOUtils.CloseFunctionBody( ref body );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_gradient.mode = (GradientMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			int colorCount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			int alphaCount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );

			var colorKeys = new GradientColorKey[ colorCount ];
			for( int i = 0; i < colorCount; i++ )
			{
				Vector4 colorKey = IOUtils.StringToVector4( GetCurrentParam( ref nodeParams ) );
				colorKeys[ i ].color = colorKey;
				colorKeys[ i ].time = colorKey.w;
			}
			m_gradient.colorKeys = colorKeys;

			var alphaKeys = new GradientAlphaKey[ alphaCount ];
			for( int i = 0; i < alphaCount; i++ )
			{
				Vector2 alphaKey = IOUtils.StringToVector2( GetCurrentParam( ref nodeParams ) );
				alphaKeys[ i ].alpha = alphaKey.x;
				alphaKeys[ i ].time = alphaKey.y;
			}
			m_gradient.alphaKeys = alphaKeys;
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_gradient.mode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_gradient.colorKeys.Length );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_gradient.alphaKeys.Length );

			for( int i = 0; i < m_gradient.colorKeys.Length; i++ )
			{
				Vector4 colorKey = new Vector4( m_gradient.colorKeys[ i ].color.r, m_gradient.colorKeys[ i ].color.g, m_gradient.colorKeys[ i ].color.b, m_gradient.colorKeys[ i ].time );
				IOUtils.AddFieldValueToString( ref nodeInfo, IOUtils.Vector4ToString( colorKey ) );
			}

			for( int i = 0; i < m_gradient.alphaKeys.Length; i++ )
			{
				Vector2 alphaKey = new Vector4( m_gradient.alphaKeys[ i ].alpha, m_gradient.alphaKeys[ i ].time );
				IOUtils.AddFieldValueToString( ref nodeInfo, IOUtils.Vector2ToString( alphaKey ) );
			}
		}
	}

	internal static class EditorGUILayoutEx
	{
		public static System.Type Type = typeof( EditorGUILayout );
		public static Gradient GradientField( Gradient value, params GUILayoutOption[] options )
		{
			return EditorGUILayout.GradientField( value, options );
		}

		public static Gradient GradientField( string label, Gradient value, params GUILayoutOption[] options )
		{
			return EditorGUILayout.GradientField( label, value, options );
		}
	}

	internal static class EditorGUIEx
	{
		public static System.Type Type = typeof( EditorGUI );

		public static Gradient GradientField( Rect position, Gradient gradient )
		{
			return EditorGUI.GradientField( position, gradient );
		}
	}
}
