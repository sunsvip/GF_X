// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{

	[Serializable]
	[NodeAttributes( "Grab Screen Color" , "Camera And Screen" , "Grabed pixel color value from screen" )]
	public sealed class ScreenColorNode : PropertyNode
	{
		private readonly string[] ASEDeclareMacro =
		{
			"#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)",
			"#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);",
			"#else",
			"#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)",
			"#endif"
		};

		private readonly Color ReferenceHeaderColor = new Color( 0.6f , 3.0f , 1.25f , 1.0f );

		private const string SamplerType = "tex2D";
		private const string GrabTextureDefault = "_GrabTexture";
		private const string ScreenColorStr = "screenColor";

		[SerializeField]
		private TexReferenceType m_referenceType = TexReferenceType.Object;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private GUIStyle m_referenceIconStyle = null;

		private ScreenColorNode m_referenceNode = null;

		[SerializeField]
		private bool m_normalize = false;

		[SerializeField]
		private bool m_useCustomGrab = false;

		[SerializeField]
		private float m_referenceWidth = -1;

		[SerializeField]
		private bool m_exposure = false;

		[SerializeField]
		private bool m_isURP2D = false;

		//SRP specific code
		private const string OpaqueTextureDefine = "REQUIRE_OPAQUE_TEXTURE 1";
		private const string FetchVarName = "fetchOpaqueVal";

		//private string LWFetchOpaqueTexture = "SAMPLE_TEXTURE2D( _CameraOpaqueTexture, sampler_CameraOpaqueTexture, {0})";

		private string LWFetchOpaqueTexture = "float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( {0} ), 1.0 )";

#if UNITY_2021_1_OR_NEWER
		private const string URP2DHelpBox = "For the Grab Screen Color to properly work a proper setup is required:" +
											"\n- On the 2D Asset Renderer the \"Foremost Sorting Layer\" must be set to the last layer which is going to be caught by the Grab Screen Color" +
											"\n- The \"Sorting Layer\" of the sprite itself which will be using the shader with the Grab Screen Color must be set to one which is above the one specified on the previous step";

		private readonly string[] URP2DDeclaration = {  "TEXTURE2D_X( _CameraSortingLayerTexture );",
														"SAMPLER( sampler_CameraSortingLayerTexture );" };
		private readonly string URP2DFunctionHeader = "float4( ASESample2DSortingLayer({0}), 1.0 )";
		private readonly string[] URP2DFunctionBody =
		{
			"float3 ASESample2DSortingLayer( float2 uv )\n" +
			"{\n"+
			"\treturn SAMPLE_TEXTURE2D_X(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, UnityStereoTransformScreenSpaceTex(uv)).rgb;\n"+
			"}\n"
		};
#endif

		private const string HDSampleSceneColorHeader5 = "ASEHDSampleSceneColor({0}, {1}, {2})";
		private readonly string[] HDSampleSceneColorFunc5 =
		{
			"float4 ASEHDSampleSceneColor(float2 uv, float lod, float exposureMultiplier)\n",
			"{\n",
			"\t#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)\n",
			"\treturn float4( SampleCameraColor(uv, lod) * exposureMultiplier, 1.0 );\n",
			"\t#endif\n",
			"\treturn float4(0.0, 0.0, 0.0, 1.0);\n",
			"}\n",
		};

		private const string HDSampleSceneColorHeader4 = "ASEHDSampleSceneColor({0})";
		private readonly string[] HDSampleSceneColorFunc4 =
		{
			"float4 ASEHDSampleSceneColor( float2 uv )\n",
			"{\n",
			"\t#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)\n",
			"\treturn float4( SampleCameraColor(uv), 1.0 );\n",
			"\t#endif\n",
			"\treturn float4(0.0, 0.0, 0.0, 1.0);\n",
			"}\n",
		};

		public ScreenColorNode() : base() { }
		public ScreenColorNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.FLOAT2, false, "UV" );
			AddInputPort( WirePortDataType.FLOAT, false, "LOD" );
			m_inputPorts[ 1 ].FloatInternalData = 0;

			AddOutputColorPorts( "RGBA" );

			m_currentParameterType = PropertyType.Global;
			m_underscoredGlobal = true;
			m_useVarSubtitle = true;
			m_customPrefix = "Grab Screen ";
			m_freeType = false;
			m_drawAttributes = false;
			m_showTitleWhenNotEditing = false;
			m_textLabelWidth = 125;
			m_showAutoRegisterUI = true;
			m_globalDefaultBehavior = false;
			m_showVariableMode = true;
		}
		
		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			if( m_referenceType == TexReferenceType.Object )
				UIUtils.RegisterScreenColorNode( this );

			if( UniqueId > -1 )
				ContainerGraph.ScreenColorNodes.OnReorderEventComplete += OnReorderEventComplete;

		}

		private void OnReorderEventComplete()
		{
			if( m_referenceType == TexReferenceType.Instance && m_referenceNode != null )
			{
				m_referenceArrayId = ContainerGraph.ScreenColorNodes.GetNodeRegisterIdx( m_referenceNode.UniqueId );
			}
		}

		void UpdateHeaderColor()
		{
			m_headerColorModifier = ( m_referenceType == TexReferenceType.Object ) ? Color.white : ReferenceHeaderColor;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			if( m_referenceNodeId > -1 && m_referenceNode == null )
			{
				m_referenceNode = UIUtils.GetScreenColorNode( m_referenceNodeId ) as ScreenColorNode;
				if( m_referenceNode == null )
				{
					m_referenceNodeId = -1;
					m_referenceArrayId = -1;
					m_sizeIsDirty = true;
				}
			}

			if( m_showSubtitle == m_containerGraph.IsSRP )
			{
				m_showSubtitle = !m_containerGraph.IsSRP;
				m_sizeIsDirty = true;
			}

			if( ContainerGraph.IsHDRP || ContainerGraph.ParentWindow.IsShaderFunctionWindow )
			{
				m_inputPorts[ 1 ].Visible = true;
			}
			else
			{
				m_inputPorts[ 1 ].Visible = false;
			}
		}

		protected override void ChangeSizeFinished()
		{
			if( m_referenceType == TexReferenceType.Instance )
			{
				m_position.width += 20;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			CheckReference();

			if( SoftValidReference )
			{
				m_content.text = m_referenceNode.TitleContent.text + Constants.InstancePostfixStr;
				SetAdditonalTitleText( m_referenceNode.AdditonalTitleContent.text );

				if( m_referenceIconStyle == null )
				{
					m_referenceIconStyle = UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon );
				}

				Rect iconPos = m_globalPosition;
				iconPos.width = 19 * drawInfo.InvertedZoom;
				iconPos.height = 19 * drawInfo.InvertedZoom;

				iconPos.y += 6 * drawInfo.InvertedZoom;
				iconPos.x += m_globalPosition.width - iconPos.width - 7 * drawInfo.InvertedZoom;

				if( GUI.Button( iconPos, string.Empty, m_referenceIconStyle ) )
				{
					UIUtils.FocusOnNode( m_referenceNode, 1, true );
				}
			}
		}

		void CheckReference()
		{
			if( m_referenceType != TexReferenceType.Instance )
			{
				return;
			}

			if( m_referenceArrayId > -1 )
			{
				ParentNode newNode = UIUtils.GetScreenColorNode( m_referenceArrayId );
				if( newNode == null || newNode.UniqueId != m_referenceNodeId )
				{
					m_referenceNode = null;
					int count = UIUtils.GetScreenColorNodeAmount();
					for( int i = 0; i < count; i++ )
					{
						ParentNode node = UIUtils.GetScreenColorNode( i );
						if( node.UniqueId == m_referenceNodeId )
						{
							m_referenceNode = node as ScreenColorNode;
							m_referenceArrayId = i;
							break;
						}
					}
				}
			}

			if( m_referenceNode == null && m_referenceNodeId > -1 )
			{
				m_referenceNodeId = -1;
				m_referenceArrayId = -1;
			}
		}

		public override void DrawMainPropertyBlock()
		{
			EditorGUI.BeginChangeCheck();
			m_referenceType = (TexReferenceType)EditorGUILayoutPopup( Constants.ReferenceTypeStr, (int)m_referenceType, Constants.ReferenceArrayLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				m_sizeIsDirty = true;
				if( m_referenceType == TexReferenceType.Object )
				{
					UIUtils.RegisterScreenColorNode( this );
					m_content.text = m_propertyInspectorName;
				}
				else
				{
					UIUtils.UnregisterScreenColorNode( this );
					if( SoftValidReference )
					{
						m_content.text = m_referenceNode.TitleContent.text + Constants.InstancePostfixStr;
					}
				}
				UpdateHeaderColor();
			}
			
			if( m_referenceType == TexReferenceType.Object )
			{
				EditorGUI.BeginDisabledGroup( m_containerGraph.IsSRP );
				{
					EditorGUI.BeginChangeCheck();
					m_useCustomGrab = EditorGUILayoutToggle( "Custom Grab Pass", m_useCustomGrab );
					EditorGUI.BeginDisabledGroup( !m_useCustomGrab );
					DrawMainPropertyBlockNoPrecision();
					EditorGUI.EndDisabledGroup();

					m_normalize = EditorGUILayoutToggle( "Normalize", m_normalize );
					if( EditorGUI.EndChangeCheck() )
					{
						UpdatePort();
						if( m_useCustomGrab )
						{
							BeginPropertyFromInspectorCheck();
						}
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				string[] arr = UIUtils.ScreenColorNodeArr();
				bool guiEnabledBuffer = GUI.enabled;
				if( arr != null && arr.Length > 0 )
				{
					GUI.enabled = true;
				}
				else
				{
					m_referenceArrayId = -1;
					GUI.enabled = false;
				}

				m_referenceArrayId = EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceArrayId, arr );
				GUI.enabled = guiEnabledBuffer;
				EditorGUI.BeginDisabledGroup( m_containerGraph.IsSRP );
				{
					EditorGUI.BeginChangeCheck();
					m_normalize = EditorGUILayoutToggle( "Normalize", m_normalize );
					if( EditorGUI.EndChangeCheck() )
					{
						UpdatePort();
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			ShowVariableMode();
			ShowAutoRegister();
			if( ContainerGraph.IsHDRP || ContainerGraph.ParentWindow.IsShaderFunctionWindow )
			{
				m_exposure = EditorGUILayoutToggle( "Exposure", m_exposure );
			}

#if UNITY_2021_1_OR_NEWER
			if( ( ContainerGraph.IsLWRP || ContainerGraph.ParentWindow.IsShaderFunctionWindow ) && ASEPackageManagerHelper.CurrentHDRPBaseline >= ASESRPBaseline.ASE_SRP_11 )
			{
				m_isURP2D = EditorGUILayoutToggle( "2D Renderer" , m_isURP2D);
				if( m_isURP2D )
				{
					EditorGUILayout.HelpBox( URP2DHelpBox , MessageType.Info );
				}
			}
#endif
		}

		private void UpdatePort()
		{
			if( m_normalize )
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4, false );
			else
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
		}

		public override void DrawTitle( Rect titlePos )
		{
			if( !m_isEditing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( titlePos, "Grab Screen Color", UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}

			if( m_useCustomGrab || SoftValidReference )
			{
				base.DrawTitle( titlePos );
				m_previousAdditonalTitle = m_additionalContent.text;
			}
			else
			if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				SetAdditonalTitleTextOnCallback( GrabTextureDefault, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( Constants.SubTitleVarNameFormatStr, newSubTitle ) );
				//GUI.Label( titlePos, PropertyInspectorName, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public void SetMacros( ref MasterNodeDataCollector dataCollector )
		{
			if( !dataCollector.IsTemplate || dataCollector.CurrentSRPType == TemplateSRPType.BiRP )
			{
				for( int i = 0; i < ASEDeclareMacro.Length; i++ )
				{
					dataCollector.AddToDirectives( ASEDeclareMacro[ i ] );
				}
			}
		}
		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			SetMacros( ref dataCollector );

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputColorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			string valueName = string.Empty;
			if( dataCollector.IsSRP )
			{
				valueName = FetchVarName + OutputId;
				dataCollector.AddToDirectives( OpaqueTextureDefine, -1 , AdditionalLineType.Define);
				string uvCoords = GetUVCoords( ref dataCollector, ignoreLocalVar, false );
				if( dataCollector.TemplateDataCollectorInstance.IsLWRP )
				{

#if UNITY_2021_1_OR_NEWER
					if( m_isURP2D )
					{
						dataCollector.AddToUniforms( UniqueId , URP2DDeclaration[ 0 ] );
						dataCollector.AddToUniforms( UniqueId , URP2DDeclaration[ 1 ] );
						dataCollector.AddFunction( URP2DFunctionBody[ 0 ] , URP2DFunctionBody , false );
						dataCollector.AddLocalVariable( UniqueId , CurrentPrecisionType , WirePortDataType.FLOAT4 , valueName , string.Format( URP2DFunctionHeader , uvCoords ) );
					}
					else
#endif
					{
						dataCollector.AddLocalVariable( UniqueId , CurrentPrecisionType , WirePortDataType.FLOAT4 , valueName , string.Format( LWFetchOpaqueTexture , uvCoords ) );
					}
				}
				else
				{
					string lod = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					dataCollector.AddFunction( HDSampleSceneColorFunc5[ 0 ], HDSampleSceneColorFunc5, false );
					string exposureValue = m_exposure ? "1.0" : "GetInverseCurrentExposureMultiplier()";
					dataCollector.AddLocalVariable( UniqueId, m_currentPrecisionType, WirePortDataType.FLOAT4, valueName, string.Format( HDSampleSceneColorHeader5, uvCoords, lod, exposureValue ) );					
				}
			}
			else
			{
				base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalVar );
				string propertyName = CurrentPropertyReference;
				OnPropertyNameChanged();
				//bool emptyName = string.IsNullOrEmpty( m_propertyInspectorName ) || propertyName == GrabTextureDefault;
				bool emptyName = string.IsNullOrEmpty( m_propertyInspectorName ) || !m_useCustomGrab;
				dataCollector.AddGrabPass( emptyName ? string.Empty : propertyName );
				valueName = SetFetchedData( ref dataCollector, ignoreLocalVar );
			}

			m_outputPorts[ 0 ].SetLocalValue( valueName, dataCollector.PortCategory );
			return GetOutputColorItem( 0, outputId, valueName );
		}


		public override void OnPropertyNameChanged()
		{
			base.OnPropertyNameChanged();
			UIUtils.UpdateScreenColorDataNode( UniqueId, DataToArray );
		}

		public string SetFetchedData( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			string propertyName = CurrentPropertyReference;

			bool isProjecting = m_normalize;

			if( !m_inputPorts[ 0 ].IsConnected ) // to generate proper screen pos by itself
				isProjecting = true;

			if( ignoreLocalVar )
			{
				string samplerValue = SamplerType + ( isProjecting ? "proj" : "" ) + "( " + propertyName + ", " + GetUVCoords( ref dataCollector, ignoreLocalVar, isProjecting ) + " )";
				return samplerValue;
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string uvValue = GetUVCoords( ref dataCollector, ignoreLocalVar, isProjecting );
			if( isProjecting )
			{
				uvValue = string.Format( "{0}.xy/{0}.w", uvValue );
			}
			string samplerOp = string.Format( "UNITY_SAMPLE_SCREENSPACE_TEXTURE({0},{1})", propertyName, uvValue );
			dataCollector.AddLocalVariable( UniqueId, UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType ) + " " + ScreenColorStr + OutputId + " = " + samplerOp + ";" );
			return ScreenColorStr + OutputId;
		}

		private string GetUVCoords( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar, bool isProjecting )
		{
			string result = string.Empty;

			if( m_inputPorts[ 0 ].IsConnected )
			{
				result = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, ( isProjecting ? WirePortDataType.FLOAT4 : WirePortDataType.FLOAT2 ), ignoreLocalVar, true );
			}
			else
			{
				string customScreenPos = null;

				if( dataCollector.IsTemplate )
					customScreenPos = dataCollector.TemplateDataCollectorInstance.GetScreenPos( CurrentPrecisionType );

				if( isProjecting )
					result = GeneratorUtils.GenerateGrabScreenPosition( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos, customScreenPos );
				else
					result = GeneratorUtils.GenerateGrabScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos, customScreenPos );
			}

			if( isProjecting && !dataCollector.IsSRP )
				return result;
			else
				return result;
		}

		public override void Destroy()
		{
			base.Destroy();
			if( m_referenceType == TexReferenceType.Object )
			{
				UIUtils.UnregisterScreenColorNode( this );
			}
			if( UniqueId > -1 )
				ContainerGraph.ScreenColorNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		public bool SoftValidReference
		{
			get
			{
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					m_referenceNode = UIUtils.GetScreenColorNode( m_referenceArrayId );
					if( m_referenceNode == null )
					{
						m_referenceArrayId = -1;
						m_referenceWidth = -1;
					}
					else if( m_referenceWidth != m_referenceNode.Position.width )
					{
						m_referenceWidth = m_referenceNode.Position.width;
						m_sizeIsDirty = true;
					}
					return m_referenceNode != null;
				}
				return false;
			}
		}

		public string CurrentPropertyReference
		{
			get
			{
				string propertyName = string.Empty;
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					ScreenColorNode node = UIUtils.GetScreenColorNode( m_referenceArrayId );
					propertyName = ( node != null ) ? node.PropertyName : m_propertyName;
				}
				else if( !m_useCustomGrab )
				{
					propertyName = GrabTextureDefault;
				}
				else
				{
					propertyName = m_propertyName;
				}
				return propertyName;
			}
		}


		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 12 )
			{
				m_referenceType = (TexReferenceType)Enum.Parse( typeof( TexReferenceType ), GetCurrentParam( ref nodeParams ) );
				if( UIUtils.CurrentShaderVersion() > 22 )
				{
					m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
				else
				{
					m_referenceArrayId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}

				if( m_referenceType == TexReferenceType.Instance )
				{
					UIUtils.UnregisterScreenColorNode( this );
				}

				UpdateHeaderColor();
			}

			if( UIUtils.CurrentShaderVersion() > 12101 )
			{
				m_useCustomGrab = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			else
			{
				m_useCustomGrab = true;
			}

			if( UIUtils.CurrentShaderVersion() > 14102 )
			{
				m_normalize = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 18801 )
			{
				m_exposure = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 18923 )
			{
				m_isURP2D = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( !m_isNodeBeingCopied && m_referenceType == TexReferenceType.Object )
			{
				ContainerGraph.ScreenColorNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceType );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( ( m_referenceNode != null ) ? m_referenceNode.UniqueId : -1 ) );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useCustomGrab );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_normalize );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_exposure );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_isURP2D );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( m_referenceType == TexReferenceType.Instance )
			{
				if( UIUtils.CurrentShaderVersion() > 22 )
				{
					m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as ScreenColorNode;
					m_referenceArrayId = UIUtils.GetScreenColorNodeRegisterId( m_referenceNodeId );
				}
				else
				{
					m_referenceNode = UIUtils.GetScreenColorNode( m_referenceArrayId );
					if( m_referenceNode != null )
					{
						m_referenceNodeId = m_referenceNode.UniqueId;
					}
				}
			}

			if( UIUtils.CurrentShaderVersion() <= 14102 )
			{
				if( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT4 )
					m_normalize = true;
				else
					m_normalize = false;
			}
		}

		public override string PropertyName
		{
			get
			{
				if( m_useCustomGrab )
					return base.PropertyName;
				else
					return GrabTextureDefault;
			}
		}

		public override string GetPropertyValStr()
		{
			return PropertyName;
		}

		public override string DataToArray { get { return m_propertyName; } }

		public override string GetUniformValue()
		{
			if( SoftValidReference )
			{
				if( m_referenceNode.IsConnected )
					return string.Empty;

				return m_referenceNode.GetUniformValue();
			}
			return "ASE_DECLARE_SCREENSPACE_TEXTURE( " + PropertyName + " )";
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			if( SoftValidReference )
			{
				//if ( m_referenceNode.IsConnected )
				//{
				//	dataType = string.Empty;
				//	dataName = string.Empty;
				//}

				return m_referenceNode.GetUniformData( out dataType, out dataName, ref fullValue );
			}
			dataName = "ASE_DECLARE_SCREENSPACE_TEXTURE( " + PropertyName + " )";
			dataType = string.Empty;
			fullValue = true;
			return true;
		}

		public override void CheckIfAutoRegister( ref MasterNodeDataCollector dataCollector )
		{
			if( m_autoRegister && (m_connStatus != NodeConnectionStatus.Connected  || InsideShaderFunction ))
			{
				SetMacros( ref dataCollector );
				RegisterProperty( ref dataCollector );
				string propertyName = CurrentPropertyReference;
				bool emptyName = string.IsNullOrEmpty( m_propertyInspectorName ) || propertyName == GrabTextureDefault;
				dataCollector.AddGrabPass( emptyName ? string.Empty : propertyName );
			}
		}
	}
}
