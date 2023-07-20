// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	// Disabling Substance Deprecated warning

	public enum TexReferenceType
	{
		Object = 0,
		Instance
	}

	public enum MipType
	{
		Auto,
		MipLevel,
		MipBias,
		Derivative
	}

	public enum ReferenceState
	{
		Self,
		Connected,
		Instance
	}

	[Serializable]
	[NodeAttributes( "Texture Sample", "Textures", "Samples a chosen texture and returns its color values, <b>Texture</b> and <b>UVs</b> can be overriden and you can select different mip modes and levels. It can also unpack and scale textures marked as normalmaps.", KeyCode.T, true, 0, int.MaxValue, typeof( Texture ), typeof( Texture2D ), typeof( Texture3D ), typeof( Cubemap ), typeof( CustomRenderTexture ), Tags = "Array" )]
	public sealed class SamplerNode : TexturePropertyNode
	{
		private const string MipModeStr = "Mip Mode";

		private const string DefaultTextureUseSematicsStr = "Use Semantics";
		private const string DefaultTextureIsNormalMapsStr = "Is Normal Map";

		private const string NormalScaleStr = "Scale";

		private float InstanceIconWidth = 19;
		private float InstanceIconHeight = 19;

		private readonly Color ReferenceHeaderColor = new Color( 2.66f, 1.02f, 0.6f, 1.0f );

		public readonly static int[] AvailableAutoCast = { 0, 1, 2, 3, 4 };
		public readonly static string[] AvailableAutoCastStr = { "Auto", "Locked To Texture 1D", "Locked To Texture 2D", "Locked To Texture 3D", "Locked To Cube" };

		[SerializeField]
		private int m_textureCoordSet = 0;

		[SerializeField]
		private bool m_autoUnpackNormals = false;

		[SerializeField]
		private bool m_useSemantics;

		[SerializeField]
		private string m_samplerType;

		[SerializeField]
		private MipType m_mipMode = MipType.Auto;

		[SerializeField]
		private TexReferenceType m_referenceType = TexReferenceType.Object;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		private SamplerNode m_referenceSampler = null;

		[SerializeField]
		private GUIStyle m_referenceStyle = null;

		[SerializeField]
		private GUIStyle m_referenceIconStyle = null;

		[SerializeField]
		private GUIContent m_referenceContent = null;

		[SerializeField]
		private float m_referenceWidth = -1;

		[SerializeField]
		private SamplerStateAutoGenerator m_samplerStateAutoGenerator = new SamplerStateAutoGenerator();

		private Vector4Node m_texCoordsHelper;

		private string m_previousAdditionalText = string.Empty;

		private int m_cachedUvsId = -1;
		private int m_cachedUnpackId = -1;
		private int m_cachedLodId = -1;

		private InputPort m_texPort;
		private InputPort m_uvPort;
		private InputPort m_lodPort;
		private InputPort m_ddxPort;
		private InputPort m_ddyPort;
		private InputPort m_normalPort;
		private InputPort m_samplerPort;
		private InputPort m_indexPort;
		private OutputPort m_colorPort;

		private TexturePropertyNode m_previewTextProp = null;
		private ReferenceState m_state = ReferenceState.Self;

		private Rect m_iconPos;

		public SamplerNode() : base() { }
		public SamplerNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			if( m_useSamplerArrayIdx < 0 )
			{
				m_useSamplerArrayIdx = 0;
			}

			m_defaultTextureValue = TexturePropertyValues.white;
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex" );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV" );
			AddInputPort( WirePortDataType.FLOAT, false, "Level" );
			AddInputPort( WirePortDataType.FLOAT2, false, "DDX" );
			AddInputPort( WirePortDataType.FLOAT2, false, "DDY" );
			AddInputPort( WirePortDataType.FLOAT, false, NormalScaleStr );
			AddInputPort( WirePortDataType.FLOAT, false, "Index" );
			AddInputPort( WirePortDataType.SAMPLERSTATE, false, "SS" );
			m_inputPorts[ 7 ].CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );

			m_texPort = m_inputPorts[ 0 ];
			m_uvPort = m_inputPorts[ 1 ];
			m_lodPort = m_inputPorts[ 2 ];
			m_ddxPort = m_inputPorts[ 3 ];
			m_ddyPort = m_inputPorts[ 4 ];
			m_normalPort = m_inputPorts[ 5 ];
			m_indexPort = m_inputPorts[ 6 ];
			m_samplerPort = m_inputPorts[ 7 ];
			m_lodPort.AutoDrawInternalData = true;
			m_indexPort.AutoDrawInternalData = true;
			m_normalPort.AutoDrawInternalData = true;
			m_lodPort.Visible = false;
			m_ddxPort.Visible = false;
			m_ddyPort.Visible = false;
			m_indexPort.Visible = false;
			m_normalPort.Visible = m_autoUnpackNormals;
			m_normalPort.FloatInternalData = 1.0f;

			//Remove output port (sampler)
			m_outputPortsDict.Remove( m_outputPorts[ 1 ].PortId );
			m_outputPorts.RemoveAt( 1 );

			m_outputPortsDict.Remove( m_outputPorts[ 0 ].PortId );
			m_outputPorts.RemoveAt( 0 );

			AddOutputColorPorts( "RGBA" );
			m_colorPort = m_outputPorts[ 0 ];
			m_currentParameterType = PropertyType.Property;
			//	m_useCustomPrefix = true;
			m_customPrefix = "Texture Sample ";
			m_referenceContent = new GUIContent( string.Empty );
			m_freeType = false;
			m_useSemantics = true;
			m_drawPicker = false;
			ConfigTextureData( TextureType.Texture2D );
			m_selectedLocation = PreviewLocation.TopCenter;
			m_previewShaderGUID = "7b4e86a89b70ae64993bf422eb406422";

			m_errorMessageTooltip = "A texture object marked as normal map is connected to this sampler. Please consider turning on the Unpack Normal Map option";
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_textLabelWidth = 135;
			m_customPrecision = false;
		}

		public override void SetPreviewInputs()
		{
			//TODO: rewrite this to be faster
			base.SetPreviewInputs();

			if( m_cachedUvsId == -1 )
				m_cachedUvsId = Shader.PropertyToID( "_CustomUVs" );

			PreviewMaterial.SetInt( m_cachedUvsId, ( m_uvPort.IsConnected ? 1 : 0 ) );

			if( m_cachedUnpackId == -1 )
				m_cachedUnpackId = Shader.PropertyToID( "_Unpack" );

			PreviewMaterial.SetInt( m_cachedUnpackId, m_autoUnpackNormals ? 1 : 0 );

			if( m_cachedLodId == -1 )
				m_cachedLodId = Shader.PropertyToID( "_LodType" );

			PreviewMaterial.SetInt( m_cachedLodId, ( m_mipMode == MipType.MipLevel ? 1 : ( m_mipMode == MipType.MipBias ? 2 : 0 ) ) );

			if( m_typeId == -1 )
				m_typeId = Shader.PropertyToID( "_Type" );

			bool usingTexture = false;
			if( m_texPort.IsConnected )
			{
				usingTexture = true;
				SetPreviewTexture( m_texPort.InputPreviewTexture( ContainerGraph ) );
			}
			else if( SoftValidReference && m_referenceSampler.TextureProperty != null )
			{
				if( m_referenceSampler.TextureProperty.Value != null )
				{
					usingTexture = true;
					SetPreviewTexture( m_referenceSampler.TextureProperty.Value );
				}
				else
				{
					usingTexture = true;
					SetPreviewTexture( m_referenceSampler.PreviewTexture );
				}
			}
			else if( TextureProperty != null )
			{
				if( TextureProperty.Value != null )
				{
					usingTexture = true;
					SetPreviewTexture( TextureProperty.Value );
				}
			}

			if( m_defaultId == -1 )
				m_defaultId = Shader.PropertyToID( "_Default" );

			if( usingTexture )
			{
				PreviewMaterial.SetInt( m_defaultId, 0 );
				m_previewMaterialPassId = 1;
			}
			else
			{
				PreviewMaterial.SetInt( m_defaultId, ( (int)m_defaultTextureValue ) + 1 );
				m_previewMaterialPassId = 0;
			}
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			if( m_referenceType == TexReferenceType.Object )
			{
				UIUtils.RegisterSamplerNode( this );
				UIUtils.RegisterPropertyNode( this );
			}
			m_textureProperty = this;

			if( UniqueId > -1 )
				ContainerGraph.SamplerNodes.OnReorderEventComplete += OnReorderEventComplete;
		}

		private void OnReorderEventComplete()
		{
			if( m_referenceType == TexReferenceType.Instance && m_referenceSampler != null )
			{
				m_referenceArrayId = ContainerGraph.SamplerNodes.GetNodeRegisterIdx( m_referenceSampler.UniqueId );
			}
		}

		public void ConfigSampler()
		{
			switch( m_currentType )
			{
				case TextureType.Texture1D:
				m_samplerType = "tex1D";
				break;
				case TextureType.ProceduralTexture:
				case TextureType.Texture2D:
				m_samplerType = "tex2D";
				break;
				case TextureType.Texture2DArray:
				m_samplerType = "tex2DArray";
				break;
				case TextureType.Texture3D:
				m_samplerType = "tex3D";
				break;
				case TextureType.Cube:
				m_samplerType = "texCUBE";
				break;
			}
		}

		public override void DrawSubProperties()
		{
			ShowDefaults();

			DrawSamplerOptions();

			EditorGUI.BeginChangeCheck();
			Type currType = ( m_autocastMode == AutoCastType.Auto ) ? typeof( Texture ) : m_textureType;
			m_defaultValue = EditorGUILayoutObjectField( Constants.DefaultValueLabel, m_defaultValue, currType, false ) as Texture;
			if( EditorGUI.EndChangeCheck() )
			{
				CheckTextureImporter( true );
				SetAdditonalTitleText( string.Format( Constants.PropertyValueLabel, GetPropertyValStr() ) );
				ConfigureInputPorts();
				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}
		}

		public override void DrawMaterialProperties()
		{
			ShowDefaults();

			DrawSamplerOptions();

			EditorGUI.BeginChangeCheck();
			Type currType = ( m_autocastMode == AutoCastType.Auto ) ? typeof( Texture ) : m_textureType;
			m_materialValue = EditorGUILayoutObjectField( Constants.MaterialValueLabel, m_materialValue, currType, false ) as Texture;
			if( EditorGUI.EndChangeCheck() )
			{
				CheckTextureImporter( true );
				SetAdditonalTitleText( string.Format( Constants.PropertyValueLabel, GetPropertyValStr() ) );
				ConfigureInputPorts();
				ConfigureOutputPorts();
			}
		}

		new void ShowDefaults()
		{
			m_defaultTextureValue = (TexturePropertyValues)EditorGUILayoutEnumPopup( DefaultTextureStr, m_defaultTextureValue );
			//AutoCastType newAutoCast = (AutoCastType)EditorGUILayoutIntPopup( AutoCastModeStr, (int)m_autocastMode, AvailableAutoCastStr, AvailableAutoCast );
			AutoCastType newAutoCast = (AutoCastType)EditorGUILayoutEnumPopup( AutoCastModeStr, m_autocastMode );
			if( newAutoCast != m_autocastMode )
			{
				m_autocastMode = newAutoCast;
				if( m_autocastMode != AutoCastType.Auto )
				{
					ConfigTextureData( m_currentType );
					ConfigureInputPorts();
					ConfigureOutputPorts();
				}
			}
		}

		public override void AdditionalCheck()
		{
			m_autoUnpackNormals = m_isNormalMap;
			ConfigureInputPorts();
			ConfigureOutputPorts();
		}


		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			if( portId == m_texPort.PortId )
			{
				m_texPort.MatchPortToConnection();
				m_textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
				if( m_textureProperty != null )
				{
					m_currentType = m_textureProperty.CurrentType;
					ConfigureInputPorts();
					ConfigureOutputPorts();
				}
				else
				{
					m_currentType = Constants.WireToTexture[ type ];
					ConfigureInputPorts();
					ConfigureOutputPorts();
				}
			}
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );

			if( portId == m_texPort.PortId )
			{
				m_texPort.MatchPortToConnection();
				m_textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
				if( m_textureProperty == null )
				{
					if( Constants.WireToTexture.TryGetValue( m_texPort.ConnectionType() , out m_currentType ) )
					{
						//m_currentType = Constants.WireToTexture[ m_texPort.ConnectionType() ];
						m_textureProperty = this;
						// This cast fails only from within shader functions if connected to a Sampler Input
						// and in this case property is set by what is connected to that input
						UIUtils.UnregisterPropertyNode( this );
						UIUtils.UnregisterTexturePropertyNode( this );
					}
				}
				else
				{
					m_currentType = m_textureProperty.CurrentType;

					UIUtils.UnregisterPropertyNode( this );
					UIUtils.UnregisterTexturePropertyNode( this );
				}

				ConfigureInputPorts();
				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}

			UpdateTitle();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );

			if( portId == m_texPort.PortId )
			{
				m_textureProperty = this;

				if( m_referenceType == TexReferenceType.Object )
				{
					UIUtils.RegisterPropertyNode( this );
					UIUtils.RegisterTexturePropertyNode( this );
				}

				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}

			UpdateTitle();
		}

		private void ForceInputPortsChange()
		{
			m_texPort.ChangeType( Constants.TextureToWire[ m_currentType ], false );
			m_normalPort.ChangeType( WirePortDataType.FLOAT, false );
			switch( m_currentType )
			{
				case TextureType.Texture1D:
				m_uvPort.ChangeType( WirePortDataType.FLOAT, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT, false );
				break;
				case TextureType.ProceduralTexture:
				case TextureType.Texture2D:
				case TextureType.Texture2DArray:
				m_uvPort.ChangeType( WirePortDataType.FLOAT2, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT2, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT2, false );
				break;
				case TextureType.Texture3D:
				case TextureType.Cube:
				m_uvPort.ChangeType( WirePortDataType.FLOAT3, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT3, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT3, false );
				break;
			}
		}

		public override void ConfigureInputPorts()
		{
			m_normalPort.Visible = AutoUnpackNormals;

			switch( m_mipMode )
			{
				case MipType.Auto:
				m_lodPort.Visible = false;
				m_ddxPort.Visible = false;
				m_ddyPort.Visible = false;
				break;
				case MipType.MipLevel:
				m_lodPort.Name = "Level";
				m_lodPort.Visible = true;
				m_ddxPort.Visible = false;
				m_ddyPort.Visible = false;
				break;
				case MipType.MipBias:
				m_lodPort.Name = "Bias";
				m_lodPort.Visible = true;
				m_ddxPort.Visible = false;
				m_ddyPort.Visible = false;
				break;
				case MipType.Derivative:
				m_lodPort.Visible = false;
				m_ddxPort.Visible = true;
				m_ddyPort.Visible = true;
				break;
			}

			switch( m_currentType )
			{
				case TextureType.Texture1D:
				m_uvPort.ChangeType( WirePortDataType.FLOAT, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT, false );
				break;
				case TextureType.ProceduralTexture:
				case TextureType.Texture2D:
				case TextureType.Texture2DArray:
				m_uvPort.ChangeType( WirePortDataType.FLOAT2, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT2, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT2, false );
				break;
				case TextureType.Texture3D:
				case TextureType.Cube:
				m_uvPort.ChangeType( WirePortDataType.FLOAT3, false );
				m_ddxPort.ChangeType( WirePortDataType.FLOAT3, false );
				m_ddyPort.ChangeType( WirePortDataType.FLOAT3, false );
				break;
			}

			if( m_currentType == TextureType.Texture2DArray )
				m_indexPort.Visible = true;
			else
				m_indexPort.Visible = false;

			m_sizeIsDirty = true;
		}

		public override void ConfigureOutputPorts()
		{
			m_outputPorts[ m_colorPort.PortId + 4 ].Visible = !AutoUnpackNormals;

			if( !AutoUnpackNormals )
			{
				m_colorPort.ChangeProperties( "RGBA", WirePortDataType.COLOR, false );
				m_outputPorts[ m_colorPort.PortId + 1 ].ChangeProperties( "R", WirePortDataType.FLOAT, false );
				m_outputPorts[ m_colorPort.PortId + 2 ].ChangeProperties( "G", WirePortDataType.FLOAT, false );
				m_outputPorts[ m_colorPort.PortId + 3 ].ChangeProperties( "B", WirePortDataType.FLOAT, false );
				m_outputPorts[ m_colorPort.PortId + 4 ].ChangeProperties( "A", WirePortDataType.FLOAT, false );

			}
			else
			{
				m_colorPort.ChangeProperties( "XYZ", WirePortDataType.FLOAT3, false );
				m_outputPorts[ m_colorPort.PortId + 1 ].ChangeProperties( "X", WirePortDataType.FLOAT, false );
				m_outputPorts[ m_colorPort.PortId + 2 ].ChangeProperties( "Y", WirePortDataType.FLOAT, false );
				m_outputPorts[ m_colorPort.PortId + 3 ].ChangeProperties( "Z", WirePortDataType.FLOAT, false );
			}

			m_sizeIsDirty = true;
		}

		void UpdateTitle()
		{
			if( m_referenceType == TexReferenceType.Object )
			{
				SetTitleText( m_propertyInspectorName );
				SetAdditonalTitleText( string.Format( Constants.PropertyValueLabel, GetPropertyValStr() ) );
			}

			m_sizeIsDirty = true;
		}

		public override void OnObjectDropped( UnityEngine.Object obj )
		{
			base.OnObjectDropped( obj );
			ConfigFromObject( obj );
		}

		public override void SetupFromCastObject( UnityEngine.Object obj )
		{
			base.SetupFromCastObject( obj );
			ConfigFromObject( obj );
		}

		void UpdateHeaderColor()
		{
			m_headerColorModifier = ( m_referenceType == TexReferenceType.Object ) ? Color.white : ReferenceHeaderColor;
		}

		void ShowSamplerUI()
		{
			EditorGUI.BeginDisabledGroup( m_samplerPort.IsConnected );
			string[] contents = UIUtils.TexturePropertyNodeArr();
			string[] arr = new string[ contents.Length + 1 ];
			arr[ 0 ] = "<None>";
			for( int i = 1; i < contents.Length + 1; i++ )
			{
				arr[ i ] = contents[ i - 1 ];
			}
			m_useSamplerArrayIdx = EditorGUILayoutPopup( "Reference Sampler", m_useSamplerArrayIdx, arr );
			EditorGUI.EndDisabledGroup();
		}

		public void DrawSamplerOptions()
		{
			if( !m_indexPort.IsConnected )
			{
				m_indexPort.FloatInternalData = EditorGUILayoutFloatField( "Index", m_indexPort.FloatInternalData );
			}

			m_textureCoordSet = EditorGUILayoutIntPopup( Constants.AvailableUVSetsLabel, m_textureCoordSet, Constants.AvailableUVSetsStr, Constants.AvailableUVSets );

			MipType newMipMode = (MipType)EditorGUILayoutEnumPopup( MipModeStr, m_mipMode );
			if( newMipMode != m_mipMode )
			{
				m_mipMode = newMipMode;
				ConfigureInputPorts();
				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}

			if( !m_lodPort.IsConnected && m_lodPort.Visible )
			{
				m_lodPort.FloatInternalData = EditorGUILayoutFloatField( newMipMode == MipType.MipBias ? "Mip Bias" : "Mip Level", m_lodPort.FloatInternalData );
			}

			if( m_currentType == TextureType.Texture2DArray && ( newMipMode == MipType.Derivative || newMipMode == MipType.MipBias ) && !UIUtils.CurrentWindow.OutsideGraph.IsSRP )
			{
				EditorGUILayout.HelpBox( "Derivative and Bias mip modes for Texture Arrays only works on some platforms (D3D11 XBOXONE GLES3 GLCORE)", MessageType.Warning );
			}

			EditorGUI.BeginChangeCheck();
			m_autoUnpackNormals = EditorGUILayoutToggle( "Unpack Normal Map", m_autoUnpackNormals );
			if( m_autoUnpackNormals && !m_normalPort.IsConnected )
			{
				m_normalPort.FloatInternalData = EditorGUILayoutFloatField( NormalScaleStr, m_normalPort.FloatInternalData );
			}

			if( EditorGUI.EndChangeCheck() )
			{
				ConfigureInputPorts();
				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}
			ShowSamplerUI();
			if( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( m_errorMessageTooltip, MessageType.Warning );
			}
		}

		public override void DrawMainPropertyBlock()
		{
			EditorGUI.BeginChangeCheck();
			m_referenceType = (TexReferenceType)EditorGUILayoutPopup( Constants.ReferenceTypeStr, (int)m_referenceType, Constants.ReferenceArrayLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				if( m_referenceType == TexReferenceType.Object )
				{
					UIUtils.RegisterSamplerNode( this );
					UIUtils.RegisterPropertyNode( this );
					if( !m_texPort.IsConnected )
						UIUtils.RegisterTexturePropertyNode( this );

					SetTitleText( m_propertyInspectorName );
					SetAdditonalTitleText( string.Format( Constants.PropertyValueLabel, GetPropertyValStr() ) );
					m_referenceArrayId = -1;
					m_referenceNodeId = -1;
					m_referenceSampler = null;
					m_textureProperty = m_texPort.IsConnected ? m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode : this;

				}
				else
				{
					UIUtils.UnregisterSamplerNode( this );
					UIUtils.UnregisterPropertyNode( this );
					if( !m_texPort.IsConnected )
						UIUtils.UnregisterTexturePropertyNode( this );
				}
				UpdateHeaderColor();
			}

			if( m_referenceType == TexReferenceType.Object )
			{
				EditorGUI.BeginChangeCheck();
				if( m_texPort.IsConnected )
				{
					m_drawAttributes = false;
					DrawSamplerOptions();
				}
				else
				{
					m_drawAttributes = true;
					base.DrawMainPropertyBlock();
				}
				if( EditorGUI.EndChangeCheck() )
				{
					OnPropertyNameChanged();
				}
			}
			else
			{
				m_drawAttributes = true;
				string[] arr = UIUtils.SamplerNodeArr();
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

				EditorGUI.BeginChangeCheck();
				m_referenceArrayId = EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceArrayId, arr );
				if( EditorGUI.EndChangeCheck() )
				{
					m_referenceSampler = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
					if( m_referenceSampler != null )
					{
						m_referenceNodeId = m_referenceSampler.UniqueId;
					}
					else
					{
						m_referenceArrayId = -1;
						m_referenceNodeId = -1;
					}
				}
				GUI.enabled = guiEnabledBuffer;

				DrawSamplerOptions();
			}
		}

		public override void OnPropertyNameChanged()
		{
			base.OnPropertyNameChanged();
			UIUtils.UpdateSamplerDataNode( UniqueId, PropertyName );
			UIUtils.UpdateTexturePropertyDataNode( UniqueId, PropertyName );
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if( m_state != ReferenceState.Self && drawInfo.CurrentEventType == EventType.MouseDown && m_previewRect.Contains( drawInfo.MousePosition ) && drawInfo.LeftMouseButtonPressed )
			{
				UIUtils.FocusOnNode( m_previewTextProp, 1, true );
				Event.current.Use();
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckReference();

			if( SoftValidReference )
			{
				m_state = ReferenceState.Instance;
				m_previewTextProp = m_referenceSampler.TextureProperty;
			}
			else if( m_texPort.IsConnected )
			{
				m_state = ReferenceState.Connected;
				m_previewTextProp = TextureProperty;
			}
			else
			{
				m_state = ReferenceState.Self;
			}

			if( m_previewTextProp == null )
				m_previewTextProp = this;
		}

		public override void OnNodeLayout( DrawInfo drawInfo )
		{
			base.OnNodeLayout( drawInfo );

			if( m_drawPreview )
			{
				m_iconPos = m_globalPosition;
				m_iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
				m_iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

				m_iconPos.y += 10 * drawInfo.InvertedZoom;
				m_iconPos.x += m_globalPosition.width - m_iconPos.width - 5 * drawInfo.InvertedZoom;
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if( !m_isVisible )
				return;

			if( drawInfo.CurrentEventType != EventType.Repaint )
				return;

			switch( m_state )
			{
				default:
				case ReferenceState.Self:
				{
					m_drawPreview = false;
					//SetTitleText( PropertyInspectorName /*m_propertyInspectorName*/ );
					//small optimization, string format or concat on every frame generates garbage
					//string tempVal = GetPropertyValStr();
					//if ( !m_previousAdditionalText.Equals( tempVal ) )
					//{
					//	m_previousAdditionalText = tempVal;
					//	m_additionalContent.text = string.Concat( "Value( ", tempVal, " )" );
					//}

					m_drawPicker = true;
				}
				break;
				case ReferenceState.Connected:
				{
					m_drawPreview = true;
					m_drawPicker = false;

					SetTitleText( m_previewTextProp.PropertyInspectorName + " (Input)" );
					m_previousAdditionalText = m_previewTextProp.AdditonalTitleContent.text;
					SetAdditonalTitleText( m_previousAdditionalText );
					// Draw chain lock
					GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );

					// Draw frame around preview
					GUI.Label( m_previewRect, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerFrame ) );
				}
				break;
				case ReferenceState.Instance:
				{
					m_drawPreview = true;
					m_drawPicker = false;

					//SetTitleText( m_previewTextProp.PropertyInspectorName + Constants.InstancePostfixStr );
					//m_previousAdditionalText = m_previewTextProp.AdditonalTitleContent.text;
					//SetAdditonalTitleText( m_previousAdditionalText );

					SetTitleTextOnCallback( m_previewTextProp.PropertyInspectorName, ( instance, newTitle ) => instance.TitleContent.text = newTitle + Constants.InstancePostfixStr );
					if( m_previewTextProp.AdditonalTitleContent.text != m_additionalContent.text )
					{
						PreviewIsDirty = true;
					}
					SetAdditonalTitleText( m_previewTextProp.AdditonalTitleContent.text );

					// Draw chain lock
					GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );

					// Draw frame around preview
					GUI.Label( m_previewRect, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerFrame ) );
				}
				break;
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
				ParentNode newNode = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
				if( newNode == null || newNode.UniqueId != m_referenceNodeId )
				{
					m_referenceSampler = null;
					int count = ContainerGraph.SamplerNodes.NodesList.Count;
					for( int i = 0; i < count; i++ )
					{
						ParentNode node = ContainerGraph.SamplerNodes.GetNode( i );
						if( node.UniqueId == m_referenceNodeId )
						{
							m_referenceSampler = node as SamplerNode;
							m_referenceArrayId = i;
							break;
						}
					}
				}
				else
				{
					m_texPort.DataType = m_referenceSampler.TexPort.DataType;
					// Set current type
					TextureType newTextureType = m_referenceSampler.CurrentType;

					// Set References Options
					AutoCastType newAutoCast = m_referenceSampler.AutocastMode;
					if( newAutoCast != m_autocastMode || newTextureType != m_currentType )
					{
						m_currentType = newTextureType;
						m_autocastMode = newAutoCast;
						//if( m_autocastMode != AutoCastType.Auto )
						{
							ConfigTextureData( m_currentType );
							ConfigureInputPorts();
							ConfigureOutputPorts();
							//ResizeNodeToPreview();
						}
					}
				}
			}

			if( m_referenceSampler == null && m_referenceNodeId > -1 )
			{
				m_referenceNodeId = -1;
				m_referenceArrayId = -1;
			}
		}

		public void SetTitleTextDelay( string newText )
		{
			if( !newText.Equals( m_content.text ) )
			{
				m_content.text = newText;
				BeginDelayedDirtyProperty();
			}
		}

		public void SetAdditonalTitleTextDelay( string newText )
		{
			if( !newText.Equals( m_additionalContent.text ) )
			{
				m_additionalContent.text = newText;
				BeginDelayedDirtyProperty();
			}
		}

		private void DrawTexturePropertyPreview( DrawInfo drawInfo, bool instance )
		{
			if( drawInfo.CurrentEventType != EventType.Repaint )
				return;

			Rect newPos = m_previewRect;

			TexturePropertyNode texProp = null;
			if( instance )
				texProp = m_referenceSampler.TextureProperty;
			else
				texProp = TextureProperty;

			if( texProp == null )
				texProp = this;

			float previewSizeX = PreviewSizeX;
			float previewSizeY = PreviewSizeY;
			newPos.width = previewSizeX * drawInfo.InvertedZoom;
			newPos.height = previewSizeY * drawInfo.InvertedZoom;

			SetTitleText( texProp.PropertyInspectorName + ( instance ? Constants.InstancePostfixStr : " (Input)" ) );
			SetAdditonalTitleText( texProp.AdditonalTitleContent.text );

			if( m_referenceStyle == null )
			{
				m_referenceStyle = UIUtils.GetCustomStyle( CustomStyle.SamplerTextureRef );
			}

			if( m_referenceIconStyle == null || m_referenceIconStyle.normal == null )
			{
				m_referenceIconStyle = UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon );
				if( m_referenceIconStyle != null && m_referenceIconStyle.normal != null && m_referenceIconStyle.normal.background != null )
				{
					InstanceIconWidth = m_referenceIconStyle.normal.background.width;
					InstanceIconHeight = m_referenceIconStyle.normal.background.height;
				}
			}

			Rect iconPos = m_globalPosition;
			iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
			iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

			iconPos.y += 10 * drawInfo.InvertedZoom;
			iconPos.x += m_globalPosition.width - iconPos.width - 5 * drawInfo.InvertedZoom;

			//if ( GUI.Button( newPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureRef )/* m_referenceStyle */) ||
			//	GUI.Button( iconPos, string.Empty, m_referenceIconStyle )
			//	)
			//{
			//	UIUtils.FocusOnNode( texProp, 1, true );
			//}

			if( texProp.Value != null )
			{
				DrawPreview( drawInfo, m_previewRect );
				GUI.Label( newPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerFrame ) );
				//UIUtils.GetCustomStyle( CustomStyle.SamplerButton ).fontSize = ( int )Mathf.Round( 9 * drawInfo.InvertedZoom );
			}
		}

		public override string GenerateSamplerPropertyName( int outputId, ref MasterNodeDataCollector dataCollector )
		{
			string generatedSamplerState = PropertyName;
			if( m_forceSamplingMacrosGen )
			{
				generatedSamplerState = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, PropertyName, m_variableMode );
			}

			if( outputId > 0 )
				return generatedSamplerState;
			else
				return PropertyName;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " cannot be used on Master Node Tessellation port" );
				return "(-1)";
			}

			OnPropertyNameChanged();

			ConfigSampler();

			string portProperty = string.Empty;
			if( m_texPort.IsConnected )
				portProperty = m_texPort.GenerateShaderForOutput( ref dataCollector, true );

			if( SoftValidReference )
			{
				OrderIndex = m_referenceSampler.RawOrderIndex;
				if( m_referenceSampler.TexPort.IsConnected )
				{
					portProperty = m_referenceSampler.TexPort.GeneratePortInstructions( ref dataCollector );
				}
				else
				{
					m_referenceSampler.RegisterProperty( ref dataCollector );
				}
			}

			if( IsObject && ( !m_texPort.IsConnected || portProperty == "0.0" ) )
				base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalVar );

			string valueName = SetFetchedData( ref dataCollector, ignoreLocalVar, outputId, portProperty );
			if( TextureProperty is VirtualTextureObject )
			{
				return valueName;
			}
			else
			{

				return GetOutputColorItem( 0, outputId, valueName );
			}
		}

		public string SampleVirtualTexture( VirtualTextureObject node, string coord )
		{
			string sampler = string.Empty;
			switch( node.Channel )
			{
				default:
				case VirtualChannel.Albedo:
				case VirtualChannel.Base:
				sampler = "VTSampleAlbedo( " + coord + " )";
				break;
				case VirtualChannel.Normal:
				case VirtualChannel.Height:
				case VirtualChannel.Occlusion:
				case VirtualChannel.Displacement:
				sampler = "VTSampleNormal( " + coord + " )";
				break;
				case VirtualChannel.Specular:
				case VirtualChannel.SpecMet:
				case VirtualChannel.Material:
				sampler = "VTSampleSpecular( " + coord + " )";
				break;
			}
			return sampler;
		}

		public string SampleTexture( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar, string portProperty, MipType currMipMode, string propertyName , VariableMode varMode )
		{
			string samplerValue = string.Empty;
			string uvCoords = GetUVCoords( ref dataCollector, ignoreLocalVar, portProperty );
			
			bool isVertex = ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation );

			bool useMacros = false;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if( outsideGraph.SamplingMacros || m_currentType == TextureType.Texture2DArray )
			{
				useMacros = Constants.TexSampleSRPMacros.ContainsKey( m_currentType );
			}
			
			if( useMacros || m_currentType == TextureType.Texture2DArray )
			{
				string suffix = string.Empty;
				switch( currMipMode )
				{
					default:
					case MipType.Auto: break;
					case MipType.MipLevel: suffix = "_LOD"; break;
					case MipType.MipBias: suffix = "_BIAS"; break;
					case MipType.Derivative: suffix = "_GRAD"; break;
				}

				if( isVertex )
					suffix = "_LOD";

				string samplerToUse = string.Empty;
				
				if( !m_samplerPort.IsConnected && m_useSamplerArrayIdx > 0 )
				{
					TexturePropertyNode samplerNode = UIUtils.GetTexturePropertyNode( m_useSamplerArrayIdx - 1 );
					if( samplerNode != null )
					{
						if( samplerNode.IsConnected )
						{
							string property = samplerNode.CurrentPropertyReference;
							samplerToUse = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, property, varMode );
						}
						else
						{
							UIUtils.ShowMessage( UniqueId, string.Format( "{0} attempting to use sampler from unconnected {1} node. Reference Sampler nodes must be in use for their samplers to be created.", m_propertyName, samplerNode.PropertyName ), MessageSeverity.Warning );
							dataCollector.AddToUniforms( UniqueId, string.Format( Constants.SamplerDeclarationSRPMacros[ m_currentType ], propertyName ) );
							samplerToUse = propertyName;
						}
					}
					else
					{
						UIUtils.ShowMessage( UniqueId, m_propertyName + " attempting to use sampler from invalid node.", MessageSeverity.Warning );
						dataCollector.AddToUniforms( UniqueId, string.Format( Constants.SamplerDeclarationSRPMacros[ m_currentType ], propertyName ) );
						samplerToUse = propertyName;
					}
				}
				else
				{
					string samplerState = m_samplerPort.GeneratePortInstructions( ref dataCollector );
					if( m_samplerPort.IsConnected && !string.IsNullOrEmpty( samplerState ) && !samplerState.Equals( "0" ) )
					{
						samplerToUse = samplerState;
					}
					else
					{
						samplerToUse = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, propertyName , varMode );
					}
				}

				if( outsideGraph.IsSRP )
				{
					if( m_currentType == TextureType.Texture3D && ( currMipMode == MipType.MipBias || currMipMode == MipType.Derivative ) )
						GeneratorUtils.AddCustom3DSRPMacros( ref dataCollector );
					samplerValue = string.Format( Constants.TexSampleSRPMacros[ m_currentType ], suffix, propertyName, samplerToUse, uvCoords );
				}
				else
				{
					GeneratorUtils.AddCustomStandardSamplingMacros( ref dataCollector, m_currentType, currMipMode );
					samplerValue = string.Format( Constants.TexSampleSamplerStandardMacros[ m_currentType ], suffix, propertyName, samplerToUse, uvCoords );
				}
			}
			else
			{
				string mipType = "";
				if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				{
					mipType = "lod";
				}

				switch( currMipMode )
				{
					case MipType.Auto:
					break;
					case MipType.MipLevel:
					mipType = "lod";
					break;
					case MipType.MipBias:
					mipType = "bias";
					break;
					case MipType.Derivative:
					break;
				}
				samplerValue = m_samplerType + mipType + "( " + propertyName + ", " + uvCoords + " )";
			}

			return samplerValue;
		}

		public string SetFetchedData( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar, int outputId, string portProperty = null )
		{
			m_precisionString = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_colorPort.DataType );
			string propertyName = CurrentPropertyReference;
			VariableMode varMode = VarModeReference;
			if( !string.IsNullOrEmpty( portProperty ) && portProperty != "0.0" )
			{
				propertyName = portProperty;
			}

			MipType currMipMode = m_mipMode;

			if( ignoreLocalVar )
			{
				if( TextureProperty is VirtualTextureObject )
					Debug.Log( "TODO" );

				if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				{
					currMipMode = MipType.MipLevel;
				}

				string samplerValue = SampleTexture( ref dataCollector, ignoreLocalVar, portProperty, currMipMode, propertyName , varMode );

				AddNormalMapTag( ref dataCollector, ref samplerValue );
				return samplerValue;
			}

			VirtualTextureObject vtex = ( TextureProperty as VirtualTextureObject );

			if( vtex != null )
			{
				string atPathname = AssetDatabase.GUIDToAssetPath( Constants.ATSharedLibGUID );
				if( string.IsNullOrEmpty( atPathname ) )
				{
					UIUtils.ShowMessage( UniqueId, "Could not find Amplify Texture on your project folder. Please install it and re-compile the shader.", MessageSeverity.Error );
				}
				else
				{
					//Need to see if the asset really exists because AssetDatabase.GUIDToAssetPath() can return a valid path if
					// the asset was previously imported and deleted after that
					UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( atPathname );
					if( obj == null )
					{
						UIUtils.ShowMessage( UniqueId, "Could not find Amplify Texture on your project folder. Please install it and re-compile the shader.", MessageSeverity.Error );
					}
					else
					{
						if( m_colorPort.IsLocalValue( dataCollector.PortCategory ) )
							return m_colorPort.LocalValue( dataCollector.PortCategory );

						//string remapPortR = ".r";
						//string remapPortG = ".g";
						//string remapPortB = ".b";
						//string remapPortA = ".a";

						//if ( vtex.Channel == VirtualChannel.Occlusion )
						//{
						//	remapPortR = ".r"; remapPortG = ".r"; remapPortB = ".r"; remapPortA = ".r";
						//}
						//else if ( vtex.Channel == VirtualChannel.SpecMet && ( ContainerGraph.CurrentStandardSurface != null && ContainerGraph.CurrentStandardSurface.CurrentLightingModel == StandardShaderLightModel.Standard ) )
						//{
						//	remapPortR = ".r"; remapPortG = ".r"; remapPortB = ".r";
						//}
						//else if ( vtex.Channel == VirtualChannel.Height || vtex.Channel == VirtualChannel.Displacement )
						//{
						//	remapPortR = ".b"; remapPortG = ".b"; remapPortB = ".b"; remapPortA = ".b";
						//}

						dataCollector.AddToPragmas( UniqueId, IOUtils.VirtualTexturePragmaHeader );
						dataCollector.AddToIncludes( UniqueId, atPathname );

						string lodBias = string.Empty;
						if( dataCollector.IsFragmentCategory )
						{
							lodBias = m_mipMode == MipType.MipLevel ? "Lod" : m_mipMode == MipType.MipBias ? "Bias" : "";
						}
						else
						{
							lodBias = "Lod";
						}

						int virtualCoordId = dataCollector.GetVirtualCoordinatesId( UniqueId, GetVirtualUVCoords( ref dataCollector, ignoreLocalVar, portProperty ), lodBias );
						string virtualSampler = SampleVirtualTexture( vtex, Constants.VirtualCoordNameStr + virtualCoordId );
						string virtualVariable = dataCollector.AddVirtualLocalVariable( UniqueId, "virtualNode" + OutputId, virtualSampler );

						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, virtualVariable, virtualSampler );

						AddNormalMapTag( ref dataCollector, ref virtualVariable );

						switch( vtex.Channel )
						{
							default:
							case VirtualChannel.Albedo:
							case VirtualChannel.Base:
							case VirtualChannel.Normal:
							case VirtualChannel.Specular:
							case VirtualChannel.SpecMet:
							case VirtualChannel.Material:
							virtualVariable = GetOutputColorItem( 0, outputId, virtualVariable );
							break;
							case VirtualChannel.Displacement:
							case VirtualChannel.Height:
							{
								if( outputId > 0 )
									virtualVariable += ".b";
								else
								{
									dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, "virtual_cast_" + OutputId, virtualVariable + ".b" );
									virtualVariable = "virtual_cast_" + OutputId;
								}
								//virtualVariable = UIUtils.CastPortType( dataCollector.PortCategory, m_currentPrecisionType, new NodeCastInfo( UniqueId, outputId ), virtualVariable, WirePortDataType.FLOAT, WirePortDataType.FLOAT4, virtualVariable );
							}
							break;
							case VirtualChannel.Occlusion:
							{
								if( outputId > 0 )
									virtualVariable += ".r";
								else
								{
									dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, "virtual_cast_" + OutputId, virtualVariable + ".r" );
									virtualVariable = "virtual_cast_" + OutputId;
								}
							}
							break;
						}

						//for ( int i = 0; i < m_outputPorts.Count; i++ )
						//{
						//	if ( m_outputPorts[ i ].IsConnected )
						//	{

						//		//TODO: make the sampler not generate local variables at all times
						//		m_textureFetchedValue = "virtualNode" + OutputId;
						//		m_isTextureFetched = true;

						//		//dataCollector.AddToLocalVariables( m_uniqueId, m_precisionString + " " + m_textureFetchedValue + " = " + virtualSampler + ";" );
						//		if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
						//			dataCollector.AddToVertexLocalVariables( UniqueId, m_precisionString + " " + m_textureFetchedValue + " = " + virtualSampler + ";" );
						//		else
						//			dataCollector.AddToLocalVariables( UniqueId, m_precisionString + " " + m_textureFetchedValue + " = " + virtualSampler + ";" );

						//		m_colorPort.SetLocalValue( m_textureFetchedValue );
						//		m_outputPorts[ m_colorPort.PortId + 1 ].SetLocalValue( m_textureFetchedValue + remapPortR );
						//		m_outputPorts[ m_colorPort.PortId + 2 ].SetLocalValue( m_textureFetchedValue + remapPortG );
						//		m_outputPorts[ m_colorPort.PortId + 3 ].SetLocalValue( m_textureFetchedValue + remapPortB );
						//		m_outputPorts[ m_colorPort.PortId + 4 ].SetLocalValue( m_textureFetchedValue + remapPortA );
						//		return m_textureFetchedValue;
						//	}
						//}

						return virtualVariable;
					}
				}
			}

			if( m_colorPort.IsLocalValue( dataCollector.PortCategory ) )
				return m_colorPort.LocalValue( dataCollector.PortCategory );

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				currMipMode = MipType.MipLevel;
				//mipType = "lod";
			}

			string samplerOp = SampleTexture( ref dataCollector, ignoreLocalVar, portProperty, currMipMode, propertyName , varMode );

			AddNormalMapTag( ref dataCollector, ref samplerOp );

			int connectedPorts = 0;
			for( int i = 0; i < m_outputPorts.Count; i++ )
			{
				if( m_outputPorts[ i ].IsConnected )
				{
					connectedPorts += 1;
					if( connectedPorts > 1 || m_outputPorts[ i ].ConnectionCount > 1 )
					{
						// Create common local var and mark as fetched
						string textureFetchedValue = m_samplerType + "Node" + OutputId;

						if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
							dataCollector.AddToVertexLocalVariables( UniqueId, m_precisionString + " " + textureFetchedValue + " = " + samplerOp + ";" );
						else
							dataCollector.AddToFragmentLocalVariables( UniqueId, m_precisionString + " " + textureFetchedValue + " = " + samplerOp + ";" );


						m_colorPort.SetLocalValue( textureFetchedValue, dataCollector.PortCategory );
						m_outputPorts[ m_colorPort.PortId + 1 ].SetLocalValue( textureFetchedValue + ".r", dataCollector.PortCategory );
						m_outputPorts[ m_colorPort.PortId + 2 ].SetLocalValue( textureFetchedValue + ".g", dataCollector.PortCategory );
						m_outputPorts[ m_colorPort.PortId + 3 ].SetLocalValue( textureFetchedValue + ".b", dataCollector.PortCategory );
						m_outputPorts[ m_colorPort.PortId + 4 ].SetLocalValue( textureFetchedValue + ".a", dataCollector.PortCategory );
						return textureFetchedValue;
					}
				}
			}
			return samplerOp;
		}

		public string GetUVCoords( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar, string portProperty )
		{
			bool isVertex = ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation );

			// make sure the final result is always a float4 with empty 0's in the middle
			string uvAppendix = ", ";
			int coordSize = 3;
			if( m_uvPort.DataType == WirePortDataType.FLOAT2 )
			{
				uvAppendix = ", 0, ";
				coordSize = 2;
			}
			else if( m_uvPort.DataType == WirePortDataType.FLOAT )
			{
				uvAppendix = ", 0, 0, ";
				coordSize = 1;
			}

			string uvs = m_uvPort.GeneratePortInstructions( ref dataCollector );

			// generate automatic UVs if not connected
			if( !m_uvPort.IsConnected )
			{
				string propertyName = CurrentPropertyReference;

				// check for references
				if( !string.IsNullOrEmpty( portProperty ) && portProperty != "0.0" )
					propertyName = portProperty;

				int coordSet = ( ( m_textureCoordSet < 0 ) ? 0 : m_textureCoordSet );
				string uvName = IOUtils.GetUVChannelName( propertyName, coordSet );
				string dummyPropUV = "_tex" /*+ ( coordSize != 2 ? "" + coordSize : "" )*/ + "coord" + ( coordSet > 0 ? ( coordSet + 1 ).ToString() : "" );
				string dummyUV = "uv" + ( coordSet > 0 ? ( coordSet + 1 ).ToString() : "" ) + dummyPropUV;

				string attr = GetPropertyValue();
				bool scaleOffset = true;
				if( attr.IndexOf( "[NoScaleOffset]" ) > -1 )
					scaleOffset = false;

				string texCoordsST = string.Empty;
				if( scaleOffset )
				{
					if( m_texCoordsHelper == null )
					{
						m_texCoordsHelper = CreateInstance<Vector4Node>();
						m_texCoordsHelper.ContainerGraph = ContainerGraph;
						m_texCoordsHelper.SetBaseUniqueId( UniqueId, true );
						m_texCoordsHelper.RegisterPropertyOnInstancing = false;
						m_texCoordsHelper.AddGlobalToSRPBatcher = true;
					}

					if( UIUtils.CurrentWindow.OutsideGraph.IsInstancedShader )
					{
						m_texCoordsHelper.CurrentParameterType = PropertyType.InstancedProperty;
					}
					else
					{
						m_texCoordsHelper.CurrentParameterType = PropertyType.Global;
					}
					m_texCoordsHelper.ResetOutputLocals();
					m_texCoordsHelper.SetRawPropertyName( propertyName + "_ST" );
					texCoordsST = m_texCoordsHelper.GenerateShaderForOutput( 0, ref dataCollector, false );
				}

				string coordInput = string.Empty;
				if( !dataCollector.IsTemplate && coordSet > 3 )
				{
					coordInput = GeneratorUtils.GenerateAutoUVs( ref dataCollector, UniqueId, coordSet, null, m_uvPort.DataType );
				}
				else
				{
					dataCollector.AddToProperties( UniqueId, "[HideInInspector] " + dummyPropUV + "( \"\", 2D ) = \"white\" {}", 9999 );
					if( isVertex )
					{
						coordInput = Constants.VertexShaderInputStr + ".texcoord";
						if( coordSet > 0 )
							coordInput += coordSet.ToString();
					}
					else
					{
						coordInput = Constants.InputVarStr + "." + dummyUV;
						dataCollector.AddToInput( UniqueId, dummyUV, dataCollector.GetMaxTextureChannelSize( coordSet ));
					}
				}

				if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
				{
					string result = string.Empty;
					if( dataCollector.TemplateDataCollectorInstance.GetCustomInterpolatedData( TemplateHelperFunctions.IntToUVChannelInfo[ m_textureCoordSet ], m_uvPort.DataType, PrecisionType.Float, ref result, false, dataCollector.PortCategory ) )
					{
						coordInput = result;
					}
					else
					if( dataCollector.TemplateDataCollectorInstance.HasUV( m_textureCoordSet ) )
						coordInput = dataCollector.TemplateDataCollectorInstance.GetUVName( m_textureCoordSet, m_uvPort.DataType );
					else
						coordInput = dataCollector.TemplateDataCollectorInstance.RegisterUV( m_textureCoordSet, m_uvPort.DataType );
				}

				if( !scaleOffset )
					uvName += OutputId;

				if( coordSize > 2 )
				{
					uvName += coordSize;
					dataCollector.UsingHigherSizeTexcoords = true;
					dataCollector.AddLocalVariable( UniqueId, "float" + coordSize + " " + uvName + " = " + coordInput + ";" );
					if( scaleOffset )
					{
						string scaleOffsetValue = GeneratorUtils.GenerateScaleOffsettedUV( m_currentType , coordInput+".xy" , texCoordsST, false );
						dataCollector.AddLocalVariable( UniqueId , uvName + ".xy = " + scaleOffsetValue+";" );
					}
				}
				else
				{
					if( coordSize == 1 )
						uvName += coordSize;

					if( scaleOffset )
					{
						string scaleOffsetValue = GeneratorUtils.GenerateScaleOffsettedUV( m_currentType , coordInput , texCoordsST, false );
						dataCollector.AddLocalVariable( UniqueId , PrecisionType.Float , m_uvPort.DataType , uvName , scaleOffsetValue );
					}
					else
						dataCollector.AddLocalVariable( UniqueId , PrecisionType.Float , m_uvPort.DataType , uvName , coordInput );
				}

				uvs = uvName;
			}


			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if( m_currentType == TextureType.Texture2DArray )
			{
				string index = m_indexPort.GeneratePortInstructions( ref dataCollector );
				uvs = string.Format( "{0},{1}", uvs, index );

				if( !( ( outsideGraph.SamplingMacros || m_currentType == TextureType.Texture2DArray ) && outsideGraph.IsSRP ) )
				{
					uvs = "float3(" + uvs + ")";
				}
			}

			if( isVertex )
			{
				string lodLevel = m_lodPort.GeneratePortInstructions( ref dataCollector );
				if( ( outsideGraph.SamplingMacros || m_currentType == TextureType.Texture2DArray ) && m_currentType != TextureType.Texture1D )
					return uvs + ", " + lodLevel;
				else
					return UIUtils.PrecisionWirePortToCgType( PrecisionType.Float, WirePortDataType.FLOAT4 ) + "( " + uvs + uvAppendix + lodLevel + ")";
			}
			else
			{
				if( ( m_mipMode == MipType.MipLevel || m_mipMode == MipType.MipBias ) /*&& m_lodPort.IsConnected*/ )
				{
					string lodLevel = m_lodPort.GeneratePortInstructions( ref dataCollector );
					if( ( outsideGraph.SamplingMacros || m_currentType == TextureType.Texture2DArray ) && m_currentType != TextureType.Texture1D )
						return uvs + ", " + lodLevel;
					else
						return UIUtils.PrecisionWirePortToCgType( PrecisionType.Float, WirePortDataType.FLOAT4 ) + "( " + uvs + uvAppendix + lodLevel + ")";
				}
				else if( m_mipMode == MipType.Derivative )
				{
					string ddx = m_ddxPort.GeneratePortInstructions( ref dataCollector );
					string ddy = m_ddyPort.GeneratePortInstructions( ref dataCollector );

					return uvs + ", " + ddx + ", " + ddy;
				}
				else
				{
					return uvs;
				}
			}
		}

		public string GetVirtualUVCoords( ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar, string portProperty )
		{
			string bias = "";
			if( !dataCollector.IsFragmentCategory || m_mipMode == MipType.MipBias || m_mipMode == MipType.MipLevel )
			{
				string lodLevel = m_lodPort.GeneratePortInstructions( ref dataCollector );
				bias += ", " + lodLevel;
			}

			if( m_uvPort.IsConnected )
			{
				string uvs = m_uvPort.GeneratePortInstructions( ref dataCollector );
				return uvs + bias;
			}
			else
			{
				string propertyName = CurrentPropertyReference;
				if( !string.IsNullOrEmpty( portProperty ) )
				{
					propertyName = portProperty;
				}
				string uvChannelName = IOUtils.GetUVChannelName( propertyName, m_textureCoordSet );


				string uvCoord = string.Empty;
				if( dataCollector.IsTemplate )
				{
					string uvName = string.Empty;
					if( dataCollector.TemplateDataCollectorInstance.HasUV( m_textureCoordSet ) )
					{
						uvName = dataCollector.TemplateDataCollectorInstance.GetUVName( m_textureCoordSet, m_uvPort.DataType );
					}
					else
					{
						uvName = dataCollector.TemplateDataCollectorInstance.RegisterUV( m_textureCoordSet, m_uvPort.DataType );
					}

					string attr = GetPropertyValue();

					if( attr.IndexOf( "[NoScaleOffset]" ) > -1 )
					{
						dataCollector.AddLocalVariable( UniqueId, PrecisionType.Float, WirePortDataType.FLOAT2, uvChannelName, uvName );
					}
					else
					{
						dataCollector.AddToUniforms( UniqueId, "uniform float4 " + propertyName + "_ST;" );
						dataCollector.AddLocalVariable( UniqueId , PrecisionType.Float , WirePortDataType.FLOAT2 , uvChannelName , GeneratorUtils.GenerateScaleOffsettedUV( m_currentType , uvName , propertyName,true ) );
					}
					uvCoord = uvChannelName;
				}
				else
				{
					if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
					{
						uvCoord = Constants.VertexShaderInputStr + ".texcoord";
						if( m_textureCoordSet > 0 )
						{
							uvCoord += m_textureCoordSet.ToString();
						}
					}
					else
					{
						propertyName = CurrentPropertyReference;
						if( !string.IsNullOrEmpty( portProperty ) && portProperty != "0.0" )
						{
							propertyName = portProperty;
						}
						uvChannelName = IOUtils.GetUVChannelName( propertyName, m_textureCoordSet );

						string dummyPropUV = "_texcoord" + ( m_textureCoordSet > 0 ? ( m_textureCoordSet + 1 ).ToString() : "" );
						string dummyUV = "uv" + ( m_textureCoordSet > 0 ? ( m_textureCoordSet + 1 ).ToString() : "" ) + dummyPropUV;

						dataCollector.AddToProperties( UniqueId, "[HideInInspector] " + dummyPropUV + "( \"\", 2D ) = \"white\" {}", 100 );
						dataCollector.AddToInput( UniqueId, dummyUV, WirePortDataType.FLOAT2 );

						string attr = GetPropertyValue();

						if( attr.IndexOf( "[NoScaleOffset]" ) > -1 )
						{
							dataCollector.AddToLocalVariables( UniqueId, PrecisionType.Float, WirePortDataType.FLOAT2, uvChannelName, Constants.InputVarStr + "." + dummyUV );
						}
						else
						{
							dataCollector.AddToUniforms( UniqueId, "uniform float4 " + propertyName + "_ST;" );
							string offsettedUV = GeneratorUtils.GenerateScaleOffsettedUV( m_currentType , dummyUV , propertyName,true );
							dataCollector.AddToLocalVariables( UniqueId, PrecisionType.Float, WirePortDataType.FLOAT2, uvChannelName, Constants.InputVarStr + "." + offsettedUV );
						}
						uvCoord = uvChannelName;
					}
				}
				return uvCoord + bias;
			}
		}

		private void AddNormalMapTag( ref MasterNodeDataCollector dataCollector, ref string value )
		{
			if( m_autoUnpackNormals )
			{
				bool isScaledNormal = false;
				if( m_normalPort.IsConnected )
				{
					isScaledNormal = true;
				}
				else
				{
					if( m_normalPort.FloatInternalData != 1 )
					{
						isScaledNormal = true;
					}
				}

				string scaleValue = isScaledNormal ? m_normalPort.GeneratePortInstructions( ref dataCollector ) : "1.0f";
				value = GeneratorUtils.GenerateUnpackNormalStr( ref dataCollector, CurrentPrecisionType, UniqueId, OutputId, value, isScaledNormal, scaleValue, UnpackInputMode.Tangent );

				if( isScaledNormal )
				{
					if( !( dataCollector.IsTemplate && dataCollector.IsSRP ) )
					{
						dataCollector.AddToIncludes( UniqueId, Constants.UnityStandardUtilsLibFuncs );
					}
				}
			}
		}

		public override void ReadOutputDataFromString( ref string[] nodeParams )
		{
			base.ReadOutputDataFromString( ref nodeParams );
			ConfigureOutputPorts();
		}

		public override int InputIdFromDeprecated( int oldInputId )
		{
			// this is not a good solution, it doesn't check for the deprecated type and thus assumes it always comes from texture array
			switch( oldInputId )
			{
				default:
				return oldInputId;
				case 0:
				return 1;
				case 1:
				return 6;
				case 2:
				return 2;
				case 3:
				return 5;
				case 4:
				return 3;
				case 5:
				return 4;
				case 6:
				return 0;
			}
		}

		public override void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null )
		{
			base.ReadFromDeprecated( ref nodeParams, oldType );
			if( oldType == typeof( TextureArrayNode ) )
			{
				base.ReadFromStringArray( ref nodeParams );
				string textureName = GetCurrentParam( ref nodeParams );
				m_defaultValue = AssetDatabase.LoadAssetAtPath<Texture2DArray>( textureName );
				if( m_defaultValue )
				{
					m_materialValue = m_defaultValue;
				}

				m_textureCoordSet = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_referenceType = (TexReferenceType)Enum.Parse( typeof( TexReferenceType ), GetCurrentParam( ref nodeParams ) );
				m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				if( m_referenceType == TexReferenceType.Instance )
				{
					UIUtils.UnregisterSamplerNode( this );
					UIUtils.UnregisterPropertyNode( this );
				}
				UpdateHeaderColor();

				if( UIUtils.CurrentShaderVersion() > 3202 )
					m_mipMode = (MipType)Enum.Parse( typeof( MipType ), GetCurrentParam( ref nodeParams ) );

				if( UIUtils.CurrentShaderVersion() > 5105 )
					m_autoUnpackNormals = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

				m_currentType = TextureType.Texture2DArray;
				//m_autocastMode = AutoCastType.LockedToTexture2DArray;

				if( m_defaultValue == null )
				{
					ConfigureInputPorts();
					ConfigureOutputPorts();
					//ResizeNodeToPreview();
				}
				else
				{
					if( m_materialValue == null )
					{
						ConfigFromObject( m_defaultValue, false, false );
					}
					else
					{
						CheckTextureImporter( false, false );
					}
					ConfigureInputPorts();
					ConfigureOutputPorts();
				}

				if( !m_isNodeBeingCopied && m_referenceType == TexReferenceType.Object )
				{
					ContainerGraph.SamplerNodes.UpdateDataOnNode( UniqueId, DataToArray );
				}

				// reading input data due to internal data being lost
				int count = 0;
				if( UIUtils.CurrentShaderVersion() > 7003 )
				{
					try
					{
						count = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] );
					}
					catch( Exception e )
					{
						Debug.LogException( e );
					}
				}
				else
				{
					count = ( m_oldInputCount < 0 ) ? m_inputPorts.Count : m_oldInputCount;
				}

				for( int i = 0; i < count && i < nodeParams.Length && m_currentReadParamIdx < nodeParams.Length; i++ )
				{
					if( UIUtils.CurrentShaderVersion() < 5003 )
					{
						int newId = VersionConvertInputPortId( i );
						string InternalData = string.Empty;
						
						if( UIUtils.CurrentShaderVersion() > 23 )
						{
							Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
						}

						InternalData = nodeParams[ m_currentReadParamIdx++ ];
						if( UIUtils.CurrentShaderVersion() >= 3100 && m_currentReadParamIdx < nodeParams.Length )
						{
							nodeParams[ m_currentReadParamIdx++ ].ToString();
						}

						if( newId == 2 )
						{
							m_indexPort.InternalData = InternalData;
							m_indexPort.UpdatePreviewInternalData();
						}

						if( newId == 3 )
						{
							m_lodPort.InternalData = InternalData;
							m_lodPort.UpdatePreviewInternalData();
						}
					}
					else
					{
						string portIdStr = nodeParams[ m_currentReadParamIdx++ ];
						int portId = -1;
						try
						{
							portId = Convert.ToInt32( portIdStr );
						}
						catch( Exception e )
						{
							Debug.LogException( e );
						}

						Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
						string InternalData = nodeParams[ m_currentReadParamIdx++ ];
						bool isEditable = Convert.ToBoolean( nodeParams[ m_currentReadParamIdx++ ] );
						if( isEditable && m_currentReadParamIdx < nodeParams.Length )
						{
							nodeParams[ m_currentReadParamIdx++ ].ToString();
						}

						if( portId == 1 )
						{
							m_indexPort.InternalData = InternalData;
							m_indexPort.UpdatePreviewInternalData();
						}

						if( portId == 2 )
						{
							m_lodPort.InternalData = InternalData;
							m_lodPort.UpdatePreviewInternalData();
						}
					}
				}
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			string defaultTextureGUID = GetCurrentParam( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 14101 )
			{
				m_defaultValue = AssetDatabase.LoadAssetAtPath<Texture>( AssetDatabase.GUIDToAssetPath( defaultTextureGUID ) );
				string materialTextureGUID = GetCurrentParam( ref nodeParams );
				m_materialValue = AssetDatabase.LoadAssetAtPath<Texture>( AssetDatabase.GUIDToAssetPath( materialTextureGUID ) );
			}
			else
			{
				m_defaultValue = AssetDatabase.LoadAssetAtPath<Texture>( defaultTextureGUID );
			}
			m_useSemantics = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_textureCoordSet = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_isNormalMap = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_defaultTextureValue = (TexturePropertyValues)Enum.Parse( typeof( TexturePropertyValues ), GetCurrentParam( ref nodeParams ) );
			m_autocastMode = (AutoCastType)Enum.Parse( typeof( AutoCastType ), GetCurrentParam( ref nodeParams ) );
			m_autoUnpackNormals = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

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
					UIUtils.UnregisterSamplerNode( this );
					UIUtils.UnregisterPropertyNode( this );
				}
				UpdateHeaderColor();
			}
			if( UIUtils.CurrentShaderVersion() > 2406 )
				m_mipMode = (MipType)Enum.Parse( typeof( MipType ), GetCurrentParam( ref nodeParams ) );


			if( UIUtils.CurrentShaderVersion() > 3201 )
				m_currentType = (TextureType)Enum.Parse( typeof( TextureType ), GetCurrentParam( ref nodeParams ) );

			if( m_defaultValue == null )
			{
				ConfigureInputPorts();
				ConfigureOutputPorts();
				//ResizeNodeToPreview();
			}
			else
			{
				if( m_materialValue == null )
				{
					ConfigFromObject( m_defaultValue, false, false );
				}
				else
				{
					CheckTextureImporter( false, false );
				}
				ConfigureInputPorts();
				ConfigureOutputPorts();
			}

			if( !m_isNodeBeingCopied && m_referenceType == TexReferenceType.Object )
			{
				ContainerGraph.SamplerNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}

			if( UIUtils.CurrentShaderVersion() >= 6001 && UIUtils.CurrentShaderVersion() < 7003 )
			{
				m_oldInputCount = 6;
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			ForceInputPortsChange();

			if( m_useSamplerArrayIdx > -1 )
			{
				m_useSamplerArrayIdx = UIUtils.GetTexturePropertyNodeRegisterId( m_useSamplerArrayIdx ) + 1;
			}
			else
			{
				m_useSamplerArrayIdx = 0;
			}

			EditorGUI.BeginChangeCheck();
			if( m_referenceType == TexReferenceType.Instance )
			{
				if( UIUtils.CurrentShaderVersion() > 22 )
				{


					m_referenceSampler = ContainerGraph.GetNode( m_referenceNodeId ) as SamplerNode;
					m_referenceArrayId = ContainerGraph.SamplerNodes.GetNodeRegisterIdx( m_referenceNodeId );
				}
				else
				{
					m_referenceSampler = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
					if( m_referenceSampler != null )
					{
						m_referenceNodeId = m_referenceSampler.UniqueId;
					}
				}
			}

			if( EditorGUI.EndChangeCheck() )
			{
				OnPropertyNameChanged();
			}
		}

		public override void ReadAdditionalData( ref string[] nodeParams ) { }

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( m_defaultValue != null ) ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_defaultValue ) ) : Constants.NoStringValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( m_materialValue != null ) ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_materialValue ) ) : Constants.NoStringValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useSemantics.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_textureCoordSet.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_isNormalMap.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultTextureValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_autocastMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_autoUnpackNormals );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceType );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( ( m_referenceSampler != null ) ? m_referenceSampler.UniqueId : -1 ) );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_mipMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentType );
		}

		public override void WriteAdditionalToString( ref string nodeInfo, ref string connectionsInfo ) { }

		public override int VersionConvertInputPortId( int portId )
		{
			int newPort = portId;
			//change normal scale port to last
			if( UIUtils.CurrentShaderVersion() < 2407 )
			{
				if( portId == 1 )
					newPort = 4;
			}

			if( UIUtils.CurrentShaderVersion() < 2408 )
			{
				newPort = newPort + 1;
			}

			return newPort;
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

			m_samplerStateAutoGenerator.Destroy();
			m_samplerStateAutoGenerator = null;
			m_defaultValue = null;
			m_materialValue = null;
			m_referenceSampler = null;
			m_referenceStyle = null;
			m_referenceContent = null;
			m_texPort = null;
			m_uvPort = null;
			m_lodPort = null;
			m_ddxPort = null;
			m_ddyPort = null;
			m_normalPort = null;
			m_colorPort = null;
			m_samplerPort = null;
			m_indexPort = null;

			if( m_referenceType == TexReferenceType.Object )
			{
				UIUtils.UnregisterSamplerNode( this );
				UIUtils.UnregisterPropertyNode( this );
			}
			if( UniqueId > -1 )
				ContainerGraph.SamplerNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		public override string GetPropertyValStr()
		{
			return m_materialMode ? ( m_materialValue != null ? m_materialValue.name : IOUtils.NO_TEXTURES ) : ( m_defaultValue != null ? m_defaultValue.name : IOUtils.NO_TEXTURES );
		}

		public TexturePropertyNode TextureProperty
		{
			get
			{
				if( m_referenceSampler != null )
				{
					m_textureProperty = m_referenceSampler as TexturePropertyNode;
				}
				else if( m_texPort.IsConnected )
				{
					m_textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
				}

				if( m_textureProperty == null )
					return this;

				return m_textureProperty;
			}
		}

		public override string GetPropertyValue()
		{
			if( SoftValidReference )
			{
				if( m_referenceSampler.TexPort.IsConnected )
				{
					return string.Empty;
				}
				else
				{
					return m_referenceSampler.TextureProperty.GetPropertyValue();
				}
			}
			else
			if( m_texPort.IsConnected && ( m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode ) != null )
			{
				return TextureProperty.GetPropertyValue();
			}

			switch( m_currentType )
			{
				case TextureType.Texture1D:
				{
					return PropertyAttributes + GetTexture1DPropertyValue();
				}
				case TextureType.ProceduralTexture:
				case TextureType.Texture2D:
				{
					return PropertyAttributes + GetTexture2DPropertyValue();
				}
				case TextureType.Texture3D:
				{
					return PropertyAttributes + GetTexture3DPropertyValue();
				}
				case TextureType.Cube:
				{
					return PropertyAttributes + GetCubePropertyValue();
				}
				case TextureType.Texture2DArray:
				{
					return PropertyAttributes + GetTexture2DArrayPropertyValue();
				}
			}
			return string.Empty;
		}

		public override string GetUniformValue()
		{

			if( SoftValidReference )
			{
				if( m_referenceSampler.TexPort.IsConnected )
					return string.Empty;
				else
					return m_referenceSampler.TextureProperty.GetUniformValue();
			}
			else if( m_texPort.IsConnected && ( m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode ) != null )
			{
				return TextureProperty.GetUniformValue();
			}

			return base.GetUniformValue();
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			if( SoftValidReference )
			{
				if( m_referenceSampler.TexPort.IsConnected )
				{
					base.GetUniformData( out dataType, out dataName, ref fullValue );
					return false;
				}
				else
					return m_referenceSampler.TextureProperty.GetUniformData( out dataType, out dataName, ref fullValue );
			}
			else if( m_texPort.IsConnected && ( m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode ) != null )
			{
				return TextureProperty.GetUniformData( out dataType, out dataName, ref fullValue );

			}

			return base.GetUniformData( out dataType, out dataName, ref fullValue );
		}

		public string UVCoordsName { get { return Constants.InputVarStr + "." + IOUtils.GetUVChannelName( CurrentPropertyReference, m_textureCoordSet ); } }
		public bool HasPropertyReference
		{
			get
			{
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					SamplerNode node = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
					if( node != null )
						return true;
				}

				if( m_texPort.IsConnected )
				{
					return true;
				}

				return false;
			}
		}

		public override string CurrentPropertyReference
		{
			get
			{
				string propertyName = string.Empty;
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					SamplerNode node = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
					propertyName = ( node != null ) ? node.TextureProperty.PropertyName : PropertyName;
				}
				else if( m_texPort.IsConnected && ( m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode ) != null )
				{
					propertyName = TextureProperty.PropertyName;
				}
				else
				{
					propertyName = PropertyName;
				}
				return propertyName;
			}
		}

		public VariableMode VarModeReference
		{
			get
			{
				VariableMode mode;
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					SamplerNode node = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );
					mode = ( node != null ) ? node.CurrentVariableMode : m_variableMode;
				}
				else if( m_texPort.IsConnected && ( m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode ) != null )
				{
					mode = TextureProperty.CurrentVariableMode;
				}
				else
				{
					mode = m_variableMode;
				}
				return mode;
			}
		}

		public bool SoftValidReference
		{
			get
			{
				if( m_referenceType == TexReferenceType.Instance && m_referenceArrayId > -1 )
				{
					m_referenceSampler = ContainerGraph.SamplerNodes.GetNode( m_referenceArrayId );

					m_texPort.Locked = true;

					if( m_referenceContent == null )
						m_referenceContent = new GUIContent();


					if( m_referenceSampler != null )
					{
						m_referenceContent.image = m_referenceSampler.Value;
						if( m_referenceWidth != m_referenceSampler.Position.width )
						{
							m_referenceWidth = m_referenceSampler.Position.width;
							m_sizeIsDirty = true;
						}
					}
					else
					{
						m_referenceArrayId = -1;
						m_referenceWidth = -1;
					}

					return m_referenceSampler != null;
				}
				m_texPort.Locked = false;
				return false;
			}
		}
		public override void ForceUpdateFromMaterial( Material material )
		{
			if( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( PropertyName ) )
			{
				m_materialValue = material.GetTexture( PropertyName );
				CheckTextureImporter( true );
				PreviewIsDirty = true;
			}

		}
		public override void SetContainerGraph( ParentGraph newgraph )
		{
			base.SetContainerGraph( newgraph );
			m_textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
			if( m_textureProperty == null )
			{
				m_textureProperty = this;
			}
		}

		public bool AutoUnpackNormals
		{
			get { return m_autoUnpackNormals; }
			set
			{
				if( value != m_autoUnpackNormals )
				{
					m_autoUnpackNormals = value;
					if( !UIUtils.IsLoading )
					{
						m_defaultTextureValue = value ? TexturePropertyValues.bump : TexturePropertyValues.white;
					}
				}
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if( dataCollector.IsTemplate )
			{
				if( !m_texPort.IsConnected )
					dataCollector.TemplateDataCollectorInstance.SetUVUsage( m_textureCoordSet , m_uvPort.DataType );
			}
			else
			{
				if( !m_uvPort.IsConnected )
					dataCollector.SetTextureChannelSize( m_textureCoordSet , m_uvPort.DataType );

				if( m_textureCoordSet > 3 )
				{
					dataCollector.AddCustomAppData( string.Format( TemplateHelperFunctions.TexUVFullSemantic , m_textureCoordSet ) );
				}
			}
		}

		private InputPort TexPort { get { return m_texPort; } }
		public bool IsObject { get { return ( m_referenceType == TexReferenceType.Object ) || ( m_referenceSampler == null ); } }
	}
}
