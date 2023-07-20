// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;

using System;
namespace AmplifyShaderEditor
{
	enum POMTexTypes
	{
		Texture2D,
		Texture3D,
		TextureArray
	};

	[Serializable]
	[NodeAttributes( "Parallax Occlusion Mapping", "UV Coordinates", "Calculates offseted UVs for parallax occlusion mapping" )]
	public sealed class ParallaxOcclusionMappingNode : ParentNode
	{
		private const string ArrayIndexStr = "Array Index";
		private const string Tex3DSliceStr = "Tex3D Slice";

		private readonly string[] m_channelTypeStr = { "Red Channel", "Green Channel", "Blue Channel", "Alpha Channel" };
		private readonly string[] m_channelTypeVal = { "r", "g", "b", "a" };
		
		[SerializeField]
		private int m_selectedChannelInt = 0;

		//[SerializeField]
		//private int m_minSamples = 8;

		//[SerializeField]
		//private int m_maxSamples = 16;
		[SerializeField]
		private InlineProperty m_inlineMinSamples = new InlineProperty( 8 );

		[SerializeField]
		private InlineProperty m_inlineMaxSamples = new InlineProperty( 16 );
		
		[ SerializeField]
		private int m_sidewallSteps = 2;

		[SerializeField]
		private float m_defaultScale = 0.02f;

		[SerializeField]
		private float m_defaultRefPlane = 0f;

		[SerializeField]
		private bool m_clipEnds = false;

		[SerializeField]
		private Vector2 m_tilling = new Vector2( 1, 1 );

		[SerializeField]
		private bool m_useCurvature = false;

		[SerializeField]
		private Vector2 m_CurvatureVector = new Vector2( 0, 0 );
		
		private string m_functionHeader = "POM( {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13} )";
		private string m_functionBody = string.Empty;

		//private const string WorldDirVarStr = "worldViewDir";
		
		private InputPort m_uvPort;
		private InputPort m_texPort;
		private InputPort m_ssPort;
		private InputPort m_scalePort;
		private InputPort m_viewdirTanPort;
		private InputPort m_refPlanePort;
		private InputPort m_curvaturePort;
		private InputPort m_arrayIndexPort;

		private OutputPort m_pomUVPort;

		private Vector4Node m_texCoordsHelper;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV",-1,MasterNodePortCategory.Fragment,0);
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.SAMPLERSTATE, false, "SS", -1, MasterNodePortCategory.Fragment, 7 );
			AddInputPort( WirePortDataType.FLOAT, false, "Scale", -1, MasterNodePortCategory.Fragment, 2 );
			AddInputPort( WirePortDataType.FLOAT3, false, "ViewDir (tan)", -1, MasterNodePortCategory.Fragment, 3 );
			AddInputPort( WirePortDataType.FLOAT, false, "Ref Plane", -1, MasterNodePortCategory.Fragment, 4 );
			AddInputPort( WirePortDataType.FLOAT2, false, "Curvature", -1, MasterNodePortCategory.Fragment, 5 );
			AddInputPort( WirePortDataType.FLOAT, false, ArrayIndexStr, -1, MasterNodePortCategory.Fragment, 6 );

			AddOutputPort( WirePortDataType.FLOAT2, "Out" );

			m_uvPort = GetInputPortByUniqueId( 0 );
			m_texPort = GetInputPortByUniqueId( 1 );
			m_texPort.CreatePortRestrictions( WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLER2DARRAY );
			m_ssPort = GetInputPortByUniqueId( 7 );
			m_ssPort.CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );
			m_scalePort = GetInputPortByUniqueId( 2 );
			m_viewdirTanPort = GetInputPortByUniqueId( 3 );
			m_refPlanePort = GetInputPortByUniqueId( 4 );
			m_pomUVPort = m_outputPorts[ 0 ];
			m_curvaturePort = GetInputPortByUniqueId( 5 );
			m_arrayIndexPort = GetInputPortByUniqueId( 6 );

			m_scalePort.FloatInternalData = 0.02f;
			m_useInternalPortData = false;
			m_textLabelWidth = 130;
			m_autoWrapProperties = true;
			m_curvaturePort.Visible = false;
			m_arrayIndexPort.Visible = false;
			UpdateSampler();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_texPort.MatchPortToConnection();
			UpdateIndexPort();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			if( !m_texPort.CheckValidType( type ) )
			{
				m_texPort.FullDeleteConnections();
				UIUtils.ShowMessage( UniqueId, "Parallax Occlusion Mapping node only accepts SAMPLER2D, SAMPLER3D and SAMPLER2DARRAY input types.\nTexture Object connected changed to "+ type + ", connection was lost, please review and update accordingly.", MessageSeverity.Warning );
			} else
			{
				m_texPort.MatchPortToConnection();
			}
			UpdateIndexPort();
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_selectedChannelInt = EditorGUILayoutPopup( "Channel", m_selectedChannelInt, m_channelTypeStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateSampler();
			}
			//EditorGUIUtility.labelWidth = 105;

			//m_minSamples = EditorGUILayoutIntSlider( "Min Samples", m_minSamples, 1, 128 );
			UndoParentNode inst = this;
			m_inlineMinSamples.CustomDrawer( ref inst, ( x ) => { m_inlineMinSamples.IntValue = EditorGUILayoutIntSlider( "Min Samples", m_inlineMinSamples.IntValue, 1, 128 ); }, "Min Samples" );
			//m_maxSamples = EditorGUILayoutIntSlider( "Max Samples", m_maxSamples, 1, 128 );
			m_inlineMaxSamples.CustomDrawer( ref inst, ( x ) => { m_inlineMaxSamples.IntValue = EditorGUILayoutIntSlider( "Max Samples", m_inlineMaxSamples.IntValue, 1, 128 ); }, "Max Samples" );

			m_sidewallSteps = EditorGUILayoutIntSlider( "Sidewall Steps", m_sidewallSteps, 0, 10 );

			EditorGUI.BeginDisabledGroup(m_scalePort.IsConnected );
			m_defaultScale = EditorGUILayoutSlider( "Default Scale", m_defaultScale, 0, 1 );
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup( m_refPlanePort.IsConnected );
			m_defaultRefPlane = EditorGUILayoutSlider( "Default Ref Plane", m_defaultRefPlane, 0, 1 );
			EditorGUI.EndDisabledGroup();
			//EditorGUIUtility.labelWidth = m_textLabelWidth;

			if( m_arrayIndexPort.Visible && !m_arrayIndexPort.IsConnected )
			{
				m_arrayIndexPort.FloatInternalData = EditorGUILayoutFloatField( "Array Index", m_arrayIndexPort.FloatInternalData );
			}

			//float cached = EditorGUIUtility.labelWidth;
			//EditorGUIUtility.labelWidth = 70;
			m_clipEnds = EditorGUILayoutToggle( "Clip Edges", m_clipEnds );
			//EditorGUIUtility.labelWidth = -1;
			//EditorGUIUtility.labelWidth = 100;
			//EditorGUILayout.BeginHorizontal();
			//EditorGUI.BeginDisabledGroup( !m_clipEnds );
			//m_tilling = EditorGUILayout.Vector2Field( string.Empty, m_tilling );
			//EditorGUI.EndDisabledGroup();
			//EditorGUILayout.EndHorizontal();
			//EditorGUIUtility.labelWidth = cached;

			EditorGUI.BeginChangeCheck();
			m_useCurvature = EditorGUILayoutToggle( "Clip Silhouette", m_useCurvature );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateCurvaturePort();
			}

			EditorGUI.BeginDisabledGroup( !(m_useCurvature && !m_curvaturePort.IsConnected) );
			m_CurvatureVector = EditorGUILayoutVector2Field( string.Empty, m_CurvatureVector );
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.HelpBox( "WARNING:\nTex must be connected to a Texture Object for this node to work\n\nMin and Max samples:\nControl the minimum and maximum number of layers extruded\n\nSidewall Steps:\nThe number of interpolations done to smooth the extrusion result on the side of the layer extrusions, min is used at steep angles while max is used at orthogonal angles\n\n"+
				"Ref Plane:\nReference plane lets you adjust the starting reference height, 0 = deepen ground, 1 = raise ground, any value above 0 might cause distortions at higher angles\n\n"+
				"Clip Edges:\nThis will clip the ends of your uvs to give a more 3D look at the edges. It'll use the tilling given by your Heightmap input.\n\n"+
				"Clip Silhouette:\nTurning this on allows you to use the UV coordinates to clip the effect curvature in U or V axis, useful for cylinders, works best with 'Clip Edges' turned OFF", MessageType.None );
		}

		private void UpdateIndexPort()
		{
			m_arrayIndexPort.Visible = m_texPort.DataType != WirePortDataType.SAMPLER2D;
			if( m_arrayIndexPort.Visible )
			{
				m_arrayIndexPort.Name = m_texPort.DataType == WirePortDataType.SAMPLER3D ? Tex3DSliceStr : ArrayIndexStr;
			}
			SizeIsDirty = true;
		}

		private void UpdateSampler()
		{
			m_texPort.Name = "Tex (" + m_channelTypeVal[ m_selectedChannelInt ].ToUpper() + ")";
		}

		private void UpdateCurvaturePort()
		{
			if ( m_useCurvature )
				m_curvaturePort.Visible = true;
			else
				m_curvaturePort.Visible = false;

			m_sizeIsDirty = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !m_texPort.IsConnected )
			{
				UIUtils.ShowMessage( UniqueId, "Parallax Occlusion Mapping node only works if a Texture Object is connected to its Tex (R) port" );
				return "0";
			}
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;

			string arrayIndex = m_arrayIndexPort.Visible?m_arrayIndexPort.GeneratePortInstructions( ref dataCollector ):"0";
			string textcoords = m_uvPort.GeneratePortInstructions( ref dataCollector );
			if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
			{
				string texName = "pomTexCoord" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, texName, string.Format( "float3({0},{1})", textcoords, arrayIndex ) );
				textcoords = texName;
			}

			string texture = m_texPort.GeneratePortInstructions( ref dataCollector );
			GeneratePOMfunction( ref dataCollector );
			string scale = m_defaultScale.ToString();
			if( m_scalePort.IsConnected )
				scale = m_scalePort.GeneratePortInstructions( ref dataCollector );

			string viewDirTan = "";
			if ( !m_viewdirTanPort.IsConnected )
			{
				if ( !dataCollector.DirtyNormal )
					dataCollector.ForceNormal = true;

				
				if ( dataCollector.IsTemplate )
				{
					viewDirTan = dataCollector.TemplateDataCollectorInstance.GetTangentViewDir( CurrentPrecisionType );
				}
				else
				{
					viewDirTan = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, ViewSpace.Tangent );
					//dataCollector.AddToInput( UniqueId, SurfaceInputs.VIEW_DIR, m_currentPrecisionType );
					//viewDirTan = Constants.InputVarStr + "." + UIUtils.GetInputValueFromType( SurfaceInputs.VIEW_DIR );
				}
			}
			else
			{
				viewDirTan = m_viewdirTanPort.GeneratePortInstructions( ref dataCollector );
			}

			//generate world normal
			string normalWorld = string.Empty;
			if ( dataCollector.IsTemplate )
			{
				normalWorld = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( CurrentPrecisionType );
			}
			else
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, CurrentPrecisionType );
				dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
				normalWorld = GeneratorUtils.GenerateWorldNormal( ref dataCollector, UniqueId );
			}

			string worldViewDir = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, ViewSpace.World );
			
			string dx = "ddx("+ textcoords + ")";
			string dy = "ddy(" + textcoords + ")";

			string refPlane = m_defaultRefPlane.ToString();
			if ( m_refPlanePort.IsConnected )
				refPlane = m_refPlanePort.GeneratePortInstructions( ref dataCollector );


			string curvature = "float2("+ m_CurvatureVector.x + "," + m_CurvatureVector.y + ")";
			if ( m_useCurvature )
			{
				dataCollector.AddToProperties( UniqueId, "[Header(Parallax Occlusion Mapping)]", 300 );
				dataCollector.AddToProperties( UniqueId, "_CurvFix(\"Curvature Bias\", Range( 0 , 1)) = 1", 301 );
				dataCollector.AddToUniforms( UniqueId, "uniform float _CurvFix;" );

				if ( m_curvaturePort.IsConnected )
					curvature = m_curvaturePort.GeneratePortInstructions( ref dataCollector );
			}


			string localVarName = "OffsetPOM" + OutputId;
			string textCoordsST = string.Empty;
			//string textureSTType = dataCollector.IsSRP ? "float4 " : "uniform float4 ";
			//dataCollector.AddToUniforms( UniqueId, textureSTType + texture +"_ST;");
			if( m_texCoordsHelper == null )
			{
				m_texCoordsHelper = CreateInstance<Vector4Node>();
				m_texCoordsHelper.ContainerGraph = ContainerGraph;
				m_texCoordsHelper.SetBaseUniqueId( UniqueId, true );
				m_texCoordsHelper.RegisterPropertyOnInstancing = false;
				m_texCoordsHelper.AddGlobalToSRPBatcher = true;
			}

			if( outsideGraph.IsInstancedShader )
			{
				m_texCoordsHelper.CurrentParameterType = PropertyType.InstancedProperty;
			}
			else
			{
				m_texCoordsHelper.CurrentParameterType = PropertyType.Global;
			}
			m_texCoordsHelper.ResetOutputLocals();
			m_texCoordsHelper.SetRawPropertyName( texture + "_ST" );
			textCoordsST = m_texCoordsHelper.GenerateShaderForOutput( 0, ref dataCollector, false );
			//////

			string textureArgs = string.Empty;
			if( outsideGraph.SamplingMacros || m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY )
			{
				string sampler = string.Empty;
				if( m_ssPort.IsConnected )
				{
					sampler = m_ssPort.GeneratePortInstructions( ref dataCollector );
				}
				else
				{
					sampler = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, texture , VariableMode.Create );
				}
				if( outsideGraph.IsSRP )
				{
					textureArgs = texture + ", " + sampler;
				}
				else
				{
					textureArgs = texture + ", " + sampler;
				}
			}
			else
			{
				textureArgs = texture;
			}
			//string functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, ( (m_pomTexType == POMTexTypes.TextureArray) ? "UNITY_PASS_TEX2DARRAY(" + texture + ")": texture), textcoords, dx, dy, normalWorld, worldViewDir, viewDirTan, m_minSamples, m_maxSamples, scale, refPlane, texture+"_ST.xy", curvature, arrayIndex );
			string functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, textureArgs, textcoords, dx, dy, normalWorld, worldViewDir, viewDirTan, m_inlineMinSamples.GetValueOrProperty(false), m_inlineMinSamples.GetValueOrProperty(false), scale, refPlane, textCoordsST + ".xy", curvature, arrayIndex );

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_pomUVPort.DataType, localVarName, functionResult );

			return GetOutputVectorItem( 0, outputId, localVarName );
		}

		private void GeneratePOMfunction( ref MasterNodeDataCollector dataCollector )
		{
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			m_functionBody = string.Empty;
			switch( m_texPort.DataType )
			{
				default:
				case WirePortDataType.SAMPLER2D:
				{
					string sampleParam = string.Empty;
					sampleParam = GeneratorUtils.GetPropertyDeclaraction( "heightMap", TextureType.Texture2D, ", " ) + GeneratorUtils.GetSamplerDeclaraction( "samplerheightMap", TextureType.Texture2D, ", " );
					IOUtils.AddFunctionHeader( ref m_functionBody, string.Format("inline float2 POM( {0}float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )", sampleParam ));
				}
				break;
				case WirePortDataType.SAMPLER3D:
				{
					string sampleParam = string.Empty;
					sampleParam = GeneratorUtils.GetPropertyDeclaraction( "heightMap", TextureType.Texture3D, ", " ) + GeneratorUtils.GetSamplerDeclaraction( "samplerheightMap", TextureType.Texture3D, ", " );
					IOUtils.AddFunctionHeader( ref m_functionBody, string.Format("inline float2 POM( {0}float3 uvs, float3 dx, float3 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )", sampleParam ) );
				}
				break;
				case WirePortDataType.SAMPLER2DARRAY:
				if( outsideGraph.IsSRP )
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float2 POM( TEXTURE2D_ARRAY(heightMap), SAMPLER(samplerheightMap), float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )" );
				else
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float2 POM( UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(heightMap), SamplerState samplerheightMap, float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )" );
				break;
			}
			
			IOUtils.AddFunctionLine( ref m_functionBody, "float3 result = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "int stepIndex = 0;" );
			//IOUtils.AddFunctionLine( ref m_functionBody, "int numSteps = ( int )( minSamples + dot( viewWorld, normalWorld ) * ( maxSamples - minSamples ) );" );
			//IOUtils.AddFunctionLine( ref m_functionBody, "int numSteps = ( int )lerp( maxSamples, minSamples, length( fwidth( uvs ) ) * 10 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, saturate( dot( normalWorld, viewWorld ) ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float layerHeight = 1.0 / numSteps;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "uvs.xy += refPlane * plane;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 deltaTex = -plane * layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 prevTexOffset = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float prevRayZ = 1.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float prevHeight = 0.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 currTexOffset = deltaTex;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float currRayZ = 1.0f - layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float currHeight = 0.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float intersection = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 finalTexOffset = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "while ( stepIndex < numSteps + 1 )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "{" );

			string textureProp = "heightMap";
			string sampleState = "samplerheightMap";

			string uvs = "uvs + currTexOffset";
			if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
				uvs = "float3(uvs.xy + currTexOffset, uvs.z)";
			else if( m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY )
				uvs = outsideGraph.IsSRP ? uvs + ", index" : "float3(" + uvs + ", index)";

			string samplingCall = GeneratorUtils.GenerateSamplingCall( ref dataCollector, m_texPort.DataType, textureProp, sampleState, uvs, MipType.Derivative, "dx", "dy" );
			if( m_useCurvature )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, " \tresult.z = dot( curv, currTexOffset * currTexOffset );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tcurrHeight = " + samplingCall + "." + m_channelTypeVal[ m_selectedChannelInt ] + " * ( 1 - result.z );" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, " \tcurrHeight = " + samplingCall + "." + m_channelTypeVal[ m_selectedChannelInt ] + ";" );
			}
			IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( currHeight > currRayZ )" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tstepIndex = numSteps + 1;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \telse" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tstepIndex++;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevTexOffset = currTexOffset;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevRayZ = currRayZ;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevHeight = currHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrTexOffset += deltaTex;" );
			if ( m_useCurvature )
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ -= layerHeight * ( 1 - result.z ) * (1+_CurvFix);" );
			else
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ -= layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
			IOUtils.AddFunctionLine( ref m_functionBody, "}" );

			if ( m_sidewallSteps > 0 )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "int sectionSteps = " + m_sidewallSteps + ";" );
				IOUtils.AddFunctionLine( ref m_functionBody, "int sectionIndex = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "float newZ = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "float newHeight = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "while ( sectionIndex < sectionSteps )" );
				IOUtils.AddFunctionLine( ref m_functionBody, "{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tintersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tfinalTexOffset = prevTexOffset + intersection * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tnewZ = prevRayZ - intersection * layerHeight;" );

				string uvs2 = "uvs + finalTexOffset";
				if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
					uvs2 = "float3(uvs.xy + finalTexOffset, uvs.z)";
				else if( m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY )
					uvs2 = outsideGraph.IsSRP ? uvs2 + ", index" : "float3(" + uvs2 + ", index)";

				string samplingCall2 = GeneratorUtils.GenerateSamplingCall( ref dataCollector, m_texPort.DataType, textureProp, sampleState, uvs2, MipType.Derivative, "dx", "dy" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tnewHeight = " + samplingCall2 + "." + m_channelTypeVal[ m_selectedChannelInt ] + ";" );

				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( newHeight > newZ )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrTexOffset = finalTexOffset;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrHeight = newHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ = newZ;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tdeltaTex = intersection * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tlayerHeight = intersection * layerHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \telse" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevTexOffset = finalTexOffset;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevHeight = newHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevRayZ = newZ;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tdeltaTex = ( 1 - intersection ) * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tlayerHeight = ( 1 - intersection ) * layerHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tsectionIndex++;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "}" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "finalTexOffset = currTexOffset;" );
			}

			if ( m_useCurvature )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
				IOUtils.AddFunctionLine( ref m_functionBody, "if ( unity_LightShadowBias.z == 0.0 )" );
				IOUtils.AddFunctionLine( ref m_functionBody, "{" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( result.z > 1 )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tclip( -1 );" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
				IOUtils.AddFunctionLine( ref m_functionBody, "}" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
			}

			if ( m_clipEnds )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "result.xy = uvs.xy + finalTexOffset;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
				IOUtils.AddFunctionLine( ref m_functionBody, "if ( unity_LightShadowBias.z == 0.0 )" );
				IOUtils.AddFunctionLine( ref m_functionBody, "{" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( result.x < 0 )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tclip( -1 );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( result.x > tilling.x )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tclip( -1 );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( result.y < 0 )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tclip( -1 );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( result.y > tilling.y )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tclip( -1 );" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
				IOUtils.AddFunctionLine( ref m_functionBody, "}" );
				IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				IOUtils.AddFunctionLine( ref m_functionBody, "return result.xy;" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "return uvs.xy + finalTexOffset;" );
			}
			IOUtils.CloseFunctionBody( ref m_functionBody );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_selectedChannelInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			//m_minSamples = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			//m_maxSamples = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() < 15406 )
			{
				m_inlineMinSamples.IntValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_inlineMaxSamples.IntValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
			else
			{
				m_inlineMinSamples.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );
				m_inlineMaxSamples.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );
			}
			m_sidewallSteps = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_defaultScale = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			m_defaultRefPlane = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			if ( UIUtils.CurrentShaderVersion() > 3001 )
			{
				m_clipEnds = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				string[] vector2Component = GetCurrentParam( ref nodeParams ).Split( IOUtils.VECTOR_SEPARATOR );
				if ( vector2Component.Length == 2 )
				{
					m_tilling.x = Convert.ToSingle( vector2Component[ 0 ] );
					m_tilling.y = Convert.ToSingle( vector2Component[ 1 ] );
				}
			}

			if ( UIUtils.CurrentShaderVersion() > 5005 )
			{
				m_useCurvature = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_CurvatureVector = IOUtils.StringToVector2( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 13103 )
			{
				//if( UIUtils.CurrentShaderVersion() < 15307 )
				//{
				//	GetCurrentParam( ref nodeParams );
				//	//bool arrayIndexVisible = false;
				//	//arrayIndexVisible = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				//	//m_pomTexType = arrayIndexVisible ? POMTexTypes.TextureArray : POMTexTypes.Texture2D;
				//}
				//else
				//{
				//	GetCurrentParam( ref nodeParams );
				//	//m_pomTexType = (POMTexTypes)Enum.Parse( typeof(POMTexTypes), GetCurrentParam( ref nodeParams ) );
				//}
				if( UIUtils.CurrentShaderVersion() <= 18201 )
				{
					GetCurrentParam( ref nodeParams );
				}
				UpdateIndexPort();
			}

			UpdateSampler();
			//GeneratePOMfunction( string.Empty );
			UpdateCurvaturePort();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedChannelInt );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_minSamples );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_maxSamples );
			m_inlineMinSamples.WriteToString( ref nodeInfo );
			m_inlineMaxSamples.WriteToString( ref nodeInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_sidewallSteps );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultScale );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultRefPlane );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_clipEnds );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_tilling.x.ToString() + IOUtils.VECTOR_SEPARATOR + m_tilling.y.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useCurvature );
			IOUtils.AddFieldValueToString( ref nodeInfo, IOUtils.Vector2ToString( m_CurvatureVector ) );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_useTextureArray );
			//IOUtils.AddFieldValueToString( ref nodeInfo, true );
		}

		public override void Destroy()
		{
			base.Destroy();
			//Not calling m_texCoordsHelper.Destroy() on purpose so UIUtils does not incorrectly unregister stuff
			if( m_texCoordsHelper != null )
			{
				DestroyImmediate( m_texCoordsHelper );
				m_texCoordsHelper = null;
			}


			m_uvPort = null;
			m_texPort = null;
			m_scalePort = null;
			m_viewdirTanPort = null;
			m_pomUVPort = null;
		}
	}
}
