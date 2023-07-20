using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Texel Size", "Textures", "Texel Size for a given texture object" )]
	public sealed class TexelSizeNode : ParentNode
	{
		private readonly string[] Dummy = { string.Empty };
		[SerializeField]
		private int m_referenceSamplerId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private TexturePropertyNode m_inputReferenceNode = null;

		private TexturePropertyNode m_referenceNode = null;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		[SerializeField]
		private string m_previousTexture = string.Empty;

		private int m_cachedSamplerId = -1;
		private int m_cachedSamplerIdArray = -1;
		private int m_cachedSamplerIdCube = -1;
		private int m_cachedSamplerId3D = -1;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex" );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );
			AddOutputVectorPorts( WirePortDataType.FLOAT4, Constants.EmptyPortValue );
			ChangeOutputName( 1, "1/Width" );
			ChangeOutputName( 2, "1/Height" );
			ChangeOutputName( 3, "Width" );
			ChangeOutputName( 4, "Height" );
			m_textLabelWidth = 80;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "6b20226576a059443b58aa2d0b942276";
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

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_inputReferenceNode = m_inputPorts[ 0 ].GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
			UpdateTitle();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			m_inputReferenceNode = null;
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ 0 ].MatchPortToConnection();
			UpdateTitle();
		}

		void UpdateTitle()
		{
			if( m_inputReferenceNode != null )
			{
				m_additionalContent.text = string.Format( Constants.PropertyValueLabel, m_inputReferenceNode.PropertyInspectorName );
			}
			else if( m_referenceSamplerId > -1 && m_referenceNode != null )
			{
				m_additionalContent.text = string.Format( Constants.PropertyValueLabel, m_referenceNode.PropertyInspectorName );
			}
			else
			{
				m_additionalContent.text = string.Empty;
			}
			m_sizeIsDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			bool guiEnabledBuffer = GUI.enabled;
			EditorGUI.BeginChangeCheck();
			List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );

			if( arr != null && arr.Count > 0 )
			{
				arr.Insert( 0, "None" );
				GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
				m_referenceSamplerId = EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceSamplerId + 1, arr.ToArray() ) - 1;
			}
			else
			{
				m_referenceSamplerId = -1;
				GUI.enabled = false;
				EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceSamplerId, Dummy );
			}

			GUI.enabled = guiEnabledBuffer;
			if( EditorGUI.EndChangeCheck() )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
				}
				else
				{
					m_referenceNodeId = -1;
					m_referenceSamplerId = -1;
				}
				UpdateTitle();
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			string texelName = string.Empty;

			if( m_inputPorts[ 0 ].IsConnected )
			{
				texelName = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ) + "_TexelSize";
			}
			else if( m_referenceNode != null )
			{
				m_referenceNode.BaseGenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
				texelName = m_referenceNode.PropertyName + "_TexelSize";
			}
			else
			{
				texelName = "_TexelSize";
				UIUtils.ShowMessage( UniqueId, "Please specify a texture sample on the Texel Size node", MessageSeverity.Warning );
			}

			dataCollector.AddToUniforms( UniqueId, "float4 " + texelName + ";", dataCollector.IsSRP );

			switch( outputId )
			{
				case 0: return texelName;
				case 1: return ( texelName + ".x" );
				case 2: return ( texelName + ".y" );
				case 3: return ( texelName + ".z" );
				case 4: return ( texelName + ".w" );
			}

			return string.Empty;
		}

		void SetPreviewTexture( Texture newValue )
		{
			if( newValue is Cubemap )
			{
				m_previewMaterialPassId = 3;
				if( m_cachedSamplerIdCube == -1 )
					m_cachedSamplerIdCube = Shader.PropertyToID( "_Cube" );

				PreviewMaterial.SetTexture( m_cachedSamplerIdCube, newValue as Cubemap );
			}
			else if( newValue is Texture2DArray )
			{

				m_previewMaterialPassId = 2;
				if( m_cachedSamplerIdArray == -1 )
					m_cachedSamplerIdArray = Shader.PropertyToID( "_Array" );

				PreviewMaterial.SetTexture( m_cachedSamplerIdArray, newValue as Texture2DArray );
			}
			else if( newValue is Texture3D )
			{
				m_previewMaterialPassId = 1;
				if( m_cachedSamplerId3D == -1 )
					m_cachedSamplerId3D = Shader.PropertyToID( "_Sampler3D" );

				PreviewMaterial.SetTexture( m_cachedSamplerId3D, newValue as Texture3D );
			}
			else
			{
				m_previewMaterialPassId = 0;
				if( m_cachedSamplerId == -1 )
					m_cachedSamplerId = Shader.PropertyToID( "_Sampler" );

				PreviewMaterial.SetTexture( m_cachedSamplerId, newValue );
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			if( m_inputPorts[ 0 ].IsConnected )
			{
				SetPreviewTexture( m_inputPorts[ 0 ].InputPreviewTexture( ContainerGraph ) );
			}
			else if( m_referenceNode != null )
			{
				if( m_referenceNode.Value != null )
				{
					SetPreviewTexture( m_referenceNode.Value );
				}
				else
				{
					SetPreviewTexture( m_referenceNode.PreviewTexture );
				}
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			if( m_referenceNode != null && m_previousTexture != m_referenceNode.AdditonalTitleContent.text )
			{
				m_previousTexture = m_referenceNode.AdditonalTitleContent.text;
				PreviewIsDirty = true;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			EditorGUI.BeginChangeCheck();
			{
				List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );
				bool guiEnabledBuffer = GUI.enabled;

				if( arr != null && arr.Count > 0 )
				{
					arr.Insert( 0, "None" );
					GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
					m_referenceSamplerId = m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId + 1, arr.ToArray() ) - 1;
				}
				else
				{
					m_referenceSamplerId = -1;
					GUI.enabled = false;
					m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId, Dummy );
				}
				GUI.enabled = guiEnabledBuffer;
			}
			if( EditorGUI.EndChangeCheck() )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
				}
				else
				{
					m_referenceNodeId = -1;
					m_referenceSamplerId = -1;
				}
				UpdateTitle();
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( UIUtils.CurrentShaderVersion() > 2404 )
			{
				m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as TexturePropertyNode;
				m_referenceSamplerId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNodeId );
			}
			else
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
				}
			}
			UpdateTitle();
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 2404 )
			{
				m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
			else
			{
				m_referenceSamplerId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceNodeId );
		}

		public override void Destroy()
		{
			base.Destroy();
			m_referenceNode = null;
			m_inputReferenceNode = null;
			m_upperLeftWidget = null;
		}
	}
}
