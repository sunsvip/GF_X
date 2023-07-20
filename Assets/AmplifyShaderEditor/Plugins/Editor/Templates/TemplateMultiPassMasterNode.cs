// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//#define SHOW_TEMPLATE_HELP_BOX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace AmplifyShaderEditor
{
	public enum HDSRPMaterialType
	{
		SubsurfaceScattering,
		Standard,
		Specular,
		Anisotropy,
		Iridescence,
		Translucent
	}

	public enum InvisibilityStatus
	{
		LockedInvisible,
		Invisible,
		Visible
	}

	public enum SetTemplateSource
	{
		NewShader,
		ShaderLoad,
		HotCodeReload
	};

	[Serializable]
	[NodeAttributes( "Template Master Node" , "Master" , "Shader Generated according to template rules" , null , KeyCode.None , false )]
	public sealed class TemplateMultiPassMasterNode : MasterNode
	{
		private const double MaxLODEditTimestamp = 1;

		private static int PASS_SELECTOR_VERSION = 16200;
		private static int PASS_UNIQUE_ID_VERSION = 16204;

		private const string LodNameId = "LODName";
		private const string LodValueId = "LODValue";

		private const string LodSubtitle = "LOD( {0} )";
		private const string AdditionalLODsStr = "LODs";

		private const string SubTitleFormatterStr = "(SubShader {0} Pass {1})";
		private const string NoSubShaderPropertyStr = "No Sub-Shader properties available";
		private const string NoPassPropertyStr = "No Pass properties available";

		private const string WarningMessage = "Templates is a feature that is still heavily under development and users may experience some problems.\nPlease email support@amplify.pt if any issue occurs.";
		private const string OpenTemplateStr = "Edit Template";
		private const string ReloadTemplateStr = "Reload Template";
		private const string CommonPropertiesStr = "Common Properties ";
		private const string SubShaderModuleStr = "SubShader ";
		private const string PassModuleStr = "Pass ";

		private const string PassNameStr = "Name";
		private const string PassNameFormateStr = "Name \"{0}\"";
		private const string SubShaderLODValueLabel = "LOD Value";
		private const string SubShaderLODNameLabel = "LOD Name";



		private bool m_reRegisterTemplateData = false;
		private bool m_fireTemplateChange = false;
		private bool m_fetchMasterNodeCategory = false;

		[SerializeField]
		private string m_templateGUID = "4e1801f860093ba4f9eb58a4b556825b";

		[SerializeField]
		private int m_passIdx = 0;

		//[SerializeField]
		//private string m_passIdxStr = string.Empty;

		[SerializeField]
		private bool m_passFoldout = false;

		[SerializeField]
		private int m_subShaderIdx = 0;

		//[SerializeField]
		//private string m_subShaderIdxStr = string.Empty;

		[SerializeField]
		private bool m_subStringFoldout = false;

		[SerializeField]
		private bool m_lodFoldout = false;


		[SerializeField]
		private string m_mainLODName = string.Empty;

		//[SerializeField]
		//private string m_subShaderLODStr;

		//[SerializeField]
		//private bool m_mainMPMasterNode = false;

		[NonSerialized]
		private TemplateMultiPass m_templateMultiPass = null;

		[NonSerialized]
		private TemplateMultiPassMasterNode m_mainMasterNodeRef = null;

		[SerializeField]
		private TemplateModulesHelper m_subShaderModule = new TemplateModulesHelper();

		[SerializeField]
		private TemplateModulesHelper m_passModule = new TemplateModulesHelper();

		[SerializeField]
		private UsePassHelper m_usePass;

		[SerializeField]
		private string m_passName = string.Empty;

		[SerializeField]
		private string m_passUniqueId = string.Empty;

		[SerializeField]
		private string m_originalPassName = string.Empty;

		[SerializeField]
		private bool m_hasLinkPorts = false;

		[SerializeField]
		private InvisibilityStatus m_isInvisible = InvisibilityStatus.Visible;

		[SerializeField]
		private int m_invisibleOptions = 0;

		[SerializeField]
		private bool m_invalidNode = false;

		[SerializeField]
		private FallbackPickerHelper m_fallbackHelper = null;

		[SerializeField]
		private DependenciesHelper m_dependenciesHelper = new DependenciesHelper();

		[SerializeField]
		private TemplateOptionsUIHelper m_subShaderOptions = new TemplateOptionsUIHelper( true );

		[SerializeField]
		private TemplateOptionsUIHelper m_passOptions = new TemplateOptionsUIHelper( false );

		[SerializeField]
		private TemplatePassSelectorHelper m_passSelector = new TemplatePassSelectorHelper();

		[SerializeField]
		private TemplateOptionsDefinesContainer m_optionsDefineContainer = new TemplateOptionsDefinesContainer();

		[SerializeField]
		private TerrainDrawInstancedHelper m_drawInstancedHelper = new TerrainDrawInstancedHelper();

		// HATE THIS BELOW, MUST REMOVE HD SPECIFIC CODE FROM GENERIC MASTER NODE
		private const string HDSRPMaterialTypeStr = "Material Type";
		private const string SRPMaterialSubsurfaceScatteringKeyword = "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1";
		private const string SRPMaterialTransmissionKeyword = "_MATERIAL_FEATURE_TRANSMISSION 1";
		private const string SRPHDMaterialSpecularKeyword = "_MATERIAL_FEATURE_SPECULAR_COLOR 1";
		//private const string SRPLWMaterialSpecularKeyword = "_SPECULAR_SETUP 1";
		private const string SRPMaterialAnisotropyKeyword = "_MATERIAL_FEATURE_ANISOTROPY 1";
		private const string SRPMaterialIridiscenceKeyword = "_MATERIAL_FEATURE_IRIDESCENCE 1";
		//private const string SRPMaterialNormalMapKeyword = "_NORMALMAP 1";
		//private const string SRPMaterialAlphaTestKeyword = "_ALPHATEST_ON 1";
		//private const string SRPMaterialBlendModeAlphaClipThresholdKeyword = "_AlphaClip 1";
		private const string SRPMaterialTransparentKeyword = "_SURFACE_TYPE_TRANSPARENT 1";
		private const string SRPMaterialBlendModeAddKeyword = "_BLENDMODE_ADD 1";
		private const string SRPMaterialBlendModeAlphaKeyword = "_BLENDMODE_ALPHA 1";
		private const string SRPMaterialClearCoatKeyword = "_MATERIAL_FEATURE_CLEAR_COAT";

		[NonSerialized]
		private bool m_fetchPorts = true;
		[NonSerialized]
		private InputPort m_specularPort;
		[NonSerialized]
		private InputPort m_metallicPort;
		[NonSerialized]
		private InputPort m_coatMaskPort;
		[NonSerialized]
		private InputPort m_diffusionProfilePort;
		[NonSerialized]
		private InputPort m_subsurfaceMaskPort;
		[NonSerialized]
		private InputPort m_thicknessPort;
		[NonSerialized]
		private InputPort m_anisotropyPort;
		[NonSerialized]
		private InputPort m_iridescenceThicknessPort;
		[NonSerialized]
		private InputPort m_iridescenceMaskPort;
		[NonSerialized]
		private InputPort m_indexOfRefractionPort;
		[NonSerialized]
		private InputPort m_transmittanceColorPort;
		[NonSerialized]
		private InputPort m_transmittanceAbsorptionDistancePort;
		[NonSerialized]
		private InputPort m_transmittanceMaskPort;

		[SerializeField]
		private HDSRPMaterialType m_hdSrpMaterialType = HDSRPMaterialType.Standard;

		[NonSerialized]
		private bool m_refreshLODValueMasterNodes = false;
		[NonSerialized]
		private bool m_refocusLODValueMasterNodes = false;
		[NonSerialized]
		private double m_refreshLODValueMasterNodesTimestamp;

		//////////////////////////////////////////////////////////////////////////
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_masterNodeCategory = 1;// First Template
			m_marginPreviewLeft = 20;
			m_shaderNameIsTitle = true;
			m_customInspectorName = string.Empty;
			m_customPrecision = true;
		}

		public override void ReleaseResources()
		{
			// Internal template resources ( for inline properties) are released by first node on the list
			// As it's also registered that way
			if( IsLODMainFirstPass )
				m_containerGraph.ClearInternalTemplateNodes();

			if( !IsLODMainMasterNode )
				return;
			TemplateMultiPass template = ( m_templateMultiPass == null ) ? m_containerGraph.ParentWindow.TemplatesManagerInstance.GetTemplate( m_templateGUID ) as TemplateMultiPass : m_templateMultiPass;
			//Maintained the logic of being the main master node to unregister since this method is being called
			//over the main master node in multiple places
			//but it will unregister with unique of the first master node (pass 0) since it was the one
			//to register it
			int passUniqueId = ( m_passIdx == 0 ) ? UniqueId : ContainerGraph.MultiPassMasterNodes.NodesList[ 0 ].UniqueId;

			if( template != null && template.AvailableShaderProperties != null )
			{
				// Unregister old template properties
				int oldPropertyCount = template.AvailableShaderProperties.Count;
				for( int i = 0 ; i < oldPropertyCount ; i++ )
				{
					ContainerGraph.ParentWindow.DuplicatePrevBufferInstance.ReleaseUniformName( passUniqueId , template.AvailableShaderProperties[ i ].PropertyName );
				}
			}
		}

		public void CopyOptionsFrom( TemplateMultiPassMasterNode origin )
		{
			//Copy options
			SubShaderOptions.CopyOptionsValuesFrom( origin.SubShaderOptions );
			PassOptions.CopyOptionsValuesFrom( origin.PassOptions );

			//Copy selected passes
			if( IsMainOutputNode )
				m_passSelector.CopyFrom( origin.PassSelector );
		}

		void RegisterProperties()
		{
			//First pass must be the one to always register properties so all modules
			//can extract a valid negative Id when reading inline properties
			if( /*!IsLODMainMasterNode*/!IsLODMainFirstPass )
			{
				m_reRegisterTemplateData = false;
				return;
			}

			if( m_templateMultiPass != null )
			{
				m_reRegisterTemplateData = false;
				// Register old template properties
				int newPropertyCount = m_templateMultiPass.AvailableShaderProperties.Count;
				for( int i = 0 ; i < newPropertyCount ; i++ )
				{
					m_containerGraph.AddInternalTemplateNode( m_templateMultiPass.AvailableShaderProperties[ i ] );
					int nodeId = ContainerGraph.ParentWindow.DuplicatePrevBufferInstance.CheckUniformNameOwner( m_templateMultiPass.AvailableShaderProperties[ i ].PropertyName );
					if( nodeId > -1 )
					{
						if( UniqueId != nodeId )
						{
							ParentNode node = m_containerGraph.GetNode( nodeId );
							if( node != null )
							{
								UIUtils.ShowMessage( string.Format( "Template requires property name {0} which is currently being used by {1}. Please rename it and reload template." , m_templateMultiPass.AvailableShaderProperties[ i ].PropertyName , node.Attributes.Name ) );
							}
							else
							{
								UIUtils.ShowMessage( string.Format( "Template requires property name {0} which is currently being on your graph. Please rename it and reload template." , m_templateMultiPass.AvailableShaderProperties[ i ].PropertyName ) );
							}
						}
					}
					else
					{
						UIUtils.RegisterUniformName( UniqueId , m_templateMultiPass.AvailableShaderProperties[ i ].PropertyName );
					}
				}
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
			m_reRegisterTemplateData = true;

			if( m_usePass == null )
			{
				m_usePass = ScriptableObject.CreateInstance<UsePassHelper>();
				m_usePass.Init( " Additional Use Passes" );
			}

			if( m_fallbackHelper == null )
			{
				m_fallbackHelper = ScriptableObject.CreateInstance<FallbackPickerHelper>();
				m_fallbackHelper.Init();
			}
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			if( UniqueId >= 0 )
			{
				if( m_lodIndex == -1 )
				{
					m_containerGraph.MultiPassMasterNodes.AddNode( this );
				}
				else
				{
					m_containerGraph.LodMultiPassMasternodes[ m_lodIndex ].AddNode( this );
				}
			}
		}

		public override void OnInputPortConnected( int portId , int otherNodeId , int otherPortId , bool activateNode = true )
		{
			base.OnInputPortConnected( portId , otherNodeId , otherPortId , activateNode );
			m_passOptions.CheckImediateActionsForPort( this , portId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			m_passOptions.CheckImediateActionsForPort( this , portId );
		}

		public void ForceTemplateRefresh()
		{
			SetTemplate( null , false , true , m_subShaderIdx , m_passIdx , SetTemplateSource.HotCodeReload );
		}

		public void SetTemplate( TemplateMultiPass template , bool writeDefaultData , bool fetchMasterNodeCategory , int subShaderIdx , int passIdx , SetTemplateSource source )
		{
			if( subShaderIdx > -1 )
				m_subShaderIdx = subShaderIdx;

			if( passIdx > -1 )
				m_passIdx = passIdx;

			ReleaseResources();
			bool hotCodeOrRead = ( template == null );
			m_templateMultiPass = ( hotCodeOrRead ) ? m_containerGraph.ParentWindow.TemplatesManagerInstance.GetTemplate( m_templateGUID ) as TemplateMultiPass : template;
			if( m_templateMultiPass != null )
			{

				string passName = string.IsNullOrEmpty( m_passUniqueId ) ? ( m_isInvisible == InvisibilityStatus.LockedInvisible ? m_passName : m_originalPassName ) : m_passUniqueId;
				int newPassIdx = m_passIdx;
				int newSubShaderIdx = m_subShaderIdx;
				m_templateMultiPass.GetSubShaderandPassFor( passName , ref newSubShaderIdx , ref newPassIdx );
				if( newPassIdx == -1 || newSubShaderIdx == -1 )
				{
					//m_containerGraph.MarkToDelete( this );
					ContainerGraph.ParentWindow.SetOutdatedShaderFromTemplate();
					m_invalidNode = true;
					UIUtils.ShowMessage( "Template changed drastically. Removing invalid passes." );
					return;
				}
				else
				{
					if( m_passIdx != newPassIdx )
						m_passIdx = newPassIdx;

					if( m_subShaderIdx != newSubShaderIdx )
						m_subShaderIdx = newSubShaderIdx;
				}

				m_containerGraph.CurrentSRPType = m_templateMultiPass.SRPtype;
				if( m_templateMultiPass.IsSinglePass )
				{
					SetAdditonalTitleText( string.Empty );
				}
				else if( m_templateMultiPass.SubShaders[ 0 ].MainPass != m_passIdx )
				{
					SetAdditonalTitleText( string.Format( SubTitleFormatterStr , m_subShaderIdx , m_passIdx ) );
				}
				m_invalidNode = false;
				if( m_subShaderIdx >= m_templateMultiPass.SubShaders.Count ||
					m_passIdx >= m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes.Count )
				{
					if( DebugConsoleWindow.DeveloperMode )
						Debug.LogFormat( "Inexisting pass {0}. Cancelling template fetch" , m_originalPassName );

					return;
				}

				m_isMainOutputNode = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].IsMainPass;
				if( m_isMainOutputNode )
				{
					// We cannot use UIUtils.MasterNodeOnTexture.height since this method can be
					// called before UIUtils is initialized
					m_insideSize.y = 55;
				}
				else
				{
					m_insideSize.y = 0;
				}

				//IsMainOutputNode = m_mainMPMasterNode;
				if( source != SetTemplateSource.HotCodeReload )
				{
					//Only set this if no hotcode reload happens ( via new shader or load )
					m_isInvisible = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].IsInvisible ? InvisibilityStatus.LockedInvisible : InvisibilityStatus.Visible;
				}
				else
				{
					// On hot code reload we only need to verify if template pass visibility data changes
					// and change accordingly
					if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].IsInvisible )
					{
						if( m_isInvisible != InvisibilityStatus.LockedInvisible )
							m_isInvisible = InvisibilityStatus.LockedInvisible;
					}
					else
					{
						if( m_isInvisible == InvisibilityStatus.LockedInvisible )
						{
							m_isInvisible = InvisibilityStatus.Visible;
						}
					}
				}

				m_invisibleOptions = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].InvisibleOptions;

				m_originalPassName = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].PassNameContainer.Data;

				if( !hotCodeOrRead )
				{
					if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].LODContainer.Index > -1 )
					{
						//m_subShaderLODStr = m_templateMultiPass.SubShaders[ m_subShaderIdx ].LODContainer.Id;
						ShaderLOD = Convert.ToInt32( m_templateMultiPass.SubShaders[ m_subShaderIdx ].LODContainer.Data );
					}
					else
					{
						ShaderLOD = 0;
					}
				}

				m_shaderNameIsTitle = IsMainOutputNode;
				m_fetchMasterNodeCategory = fetchMasterNodeCategory;
				m_templateGUID = m_templateMultiPass.GUID;
				UpdatePortInfo();

				RegisterProperties();

				// template is null when hot code reloading or loading from file so inspector name shouldn't be changed
				if( !hotCodeOrRead )
				{
					m_customInspectorName = m_templateMultiPass.CustomInspectorContainer.Data;
					CheckLegacyCustomInspectors();
					if( m_isMainOutputNode )
					{
						m_passSelector.Clear();
						m_passSelector.Setup( m_templateMultiPass.SubShaders[ m_subShaderIdx ] );
					}
				}
				else
				{
					//Hotcode reload or ReadFromString
					// Setup is only made if internal pass array is null
					if( m_isMainOutputNode )
					{
						m_passSelector.Setup( m_templateMultiPass.SubShaders[ m_subShaderIdx ] );
					}
				}

				SetupCustomOptionsFromTemplate( template != null );

				if( string.IsNullOrEmpty( m_fallbackHelper.RawFallbackShader ) )
					m_fallbackHelper.RawFallbackShader = m_templateMultiPass.FallbackContainer.Data;

				//bool updateInfofromTemplate = UpdatePortInfo();
				//if( updateInfofromTemplate )
				//{
				m_subShaderModule.FetchDataFromTemplate( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules );
				m_passModule.FetchDataFromTemplate( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules );
				//}

				//RegisterProperties();
				if( writeDefaultData )
				{
					//ShaderName = m_templateMultiPass.DefaultShaderName;
					ShaderName = m_shaderName;
					m_passName = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].PassNameContainer.Data;
					if( !m_templateMultiPass.IsSinglePass /*&& !m_shaderNameIsTitle*/ )
					{
						if( m_templateMultiPass.SubShaders[ 0 ].MainPass != m_passIdx )
							SetClippedTitle( m_passName );
					}
				}

				UpdateSubShaderPassStr();

				if( m_isMainOutputNode )
					m_fireTemplateChange = true;
			}
			else
			{
				m_invalidNode = true;
			}
		}

		public override void OnRefreshLinkedPortsComplete()
		{
			if( m_invalidNode )
				return;

			if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPIsPBRHD )
				ConfigHDPorts();

			SetReadOptions();
		}

		public void SetReadOptions()
		{
			m_passOptions.SetReadOptions();
			if( m_isMainOutputNode )
				m_subShaderOptions.SetReadOptions();
		}

		bool UpdatePortInfo()
		{
			List<TemplateInputData> inputDataList = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].InputDataList;
			int count = inputDataList.Count;
			if( count != m_inputPorts.Count )
			{
				DeleteAllInputConnections( true );

				for( int i = 0 ; i < count ; i++ )
				{
					InputPort port = AddInputPort( inputDataList[ i ].DataType , false , inputDataList[ i ].PortName , inputDataList[ i ].OrderId , inputDataList[ i ].PortCategory , inputDataList[ i ].PortUniqueId );
					port.ExternalLinkId = inputDataList[ i ].LinkId;
					m_hasLinkPorts = m_hasLinkPorts || !string.IsNullOrEmpty( inputDataList[ i ].LinkId );
				}
				return true;
			}
			else
			{
				for( int i = 0 ; i < count ; i++ )
				{
					m_inputPorts[ i ].ChangeProperties( inputDataList[ i ].PortName , inputDataList[ i ].DataType , false );
					m_inputPorts[ i ].ExternalLinkId = inputDataList[ i ].LinkId;
				}
				return false;
			}
		}

		public void SetPropertyActionFromItem( bool actionFromUser , TemplateModulesHelper module , TemplateActionItem item )
		{
			// this was added because when switching templates the m_mainMasterNodeRef was not properly set yet and was causing issues, there's probably a better place for this
			if( !m_isMainOutputNode && m_mainMasterNodeRef == null )
			{
				m_mainMasterNodeRef = m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode;
			}

			TemplateModulesHelper subShaderModule = m_isMainOutputNode ? m_subShaderModule : m_mainMasterNodeRef.SubShaderModule;
			switch( item.PropertyAction )
			{
				case PropertyActionsEnum.CullMode:
				{
					if( item.CopyFromSubShader )
					{
						module.CullModeHelper.CurrentCullMode = subShaderModule.CullModeHelper.CurrentCullMode;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.CullModeHelper.CustomEdited;
						if( performAction )
						{
							module.CullModeHelper.CustomEdited = false;
							module.CullModeHelper.CurrentCullMode = item.ActionCullMode;
						}
					}

				}
				break;
				case PropertyActionsEnum.ColorMask:
				{
					if( item.CopyFromSubShader )
						module.ColorMaskHelper.ColorMask = subShaderModule.ColorMaskHelper.ColorMask;
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.ColorMaskHelper.CustomEdited;
						if( performAction )
						{
							module.ColorMaskHelper.CustomEdited = false;
							module.ColorMaskHelper.ColorMask = item.ColorMask.GetColorMask( module.ColorMaskHelper.ColorMask );
						}
					}
				}
				break;
				case PropertyActionsEnum.ColorMask1:
				{
					if( item.CopyFromSubShader )
						module.ColorMaskHelper1.ColorMask = subShaderModule.ColorMaskHelper1.ColorMask;
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.ColorMaskHelper1.CustomEdited;
						if( performAction )
						{
							module.ColorMaskHelper1.CustomEdited = false;
							module.ColorMaskHelper1.ColorMask = item.ColorMask1.GetColorMask( module.ColorMaskHelper1.ColorMask );
						}
					}
				}
				break;
				case PropertyActionsEnum.ColorMask2:
				{
					if( item.CopyFromSubShader )
						module.ColorMaskHelper2.ColorMask = subShaderModule.ColorMaskHelper2.ColorMask;
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.ColorMaskHelper2.CustomEdited;
						if( performAction )
						{
							module.ColorMaskHelper2.CustomEdited = false;
							module.ColorMaskHelper2.ColorMask = item.ColorMask2.GetColorMask( module.ColorMaskHelper2.ColorMask );
						}
					}
				}
				break;
				case PropertyActionsEnum.ColorMask3:
				{
					if( item.CopyFromSubShader )
						module.ColorMaskHelper3.ColorMask = subShaderModule.ColorMaskHelper3.ColorMask;
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.ColorMaskHelper3.CustomEdited;
						if( performAction )
						{
							module.ColorMaskHelper3.CustomEdited = false;
							module.ColorMaskHelper3.ColorMask = item.ColorMask3.GetColorMask( module.ColorMaskHelper3.ColorMask );
						}
					}
				}
				break;
				case PropertyActionsEnum.ZWrite:
				{
					if( item.CopyFromSubShader )
					{
						module.DepthOphelper.ZWriteModeValue = subShaderModule.DepthOphelper.ZWriteModeValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.DepthOphelper.CustomEdited;
						if( performAction )
						{
							module.DepthOphelper.CustomEdited = false;
							module.DepthOphelper.ZWriteModeValue = item.ActionZWrite;
						}
					}
				}
				break;
				case PropertyActionsEnum.ZTest:
				{
					if( item.CopyFromSubShader )
					{
						module.DepthOphelper.ZTestModeValue = subShaderModule.DepthOphelper.ZTestModeValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.DepthOphelper.CustomEdited;
						if( performAction )
						{
							module.DepthOphelper.CustomEdited = false;
							module.DepthOphelper.ZTestModeValue = item.ActionZTest;
						}
					}
				}
				break;
				case PropertyActionsEnum.ZOffsetFactor:
				{
					if( item.CopyFromSubShader )
					{
						module.DepthOphelper.OffsetFactorValue = subShaderModule.DepthOphelper.OffsetFactorValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.DepthOphelper.CustomEdited;
						if( performAction )
						{
							module.DepthOphelper.CustomEdited = false;
							module.DepthOphelper.OffsetFactorValue = item.ActionZOffsetFactor;
						}
					}
				}
				break;
				case PropertyActionsEnum.ZOffsetUnits:
				{
					if( item.CopyFromSubShader )
					{
						module.DepthOphelper.OffsetUnitsValue = subShaderModule.DepthOphelper.OffsetUnitsValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.DepthOphelper.CustomEdited;
						if( performAction )
						{
							module.DepthOphelper.CustomEdited = false;
							module.DepthOphelper.OffsetUnitsValue = item.ActionZOffsetUnits;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendRGB:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper.SourceFactorRGB = subShaderModule.BlendOpHelper.SourceFactorRGB;
						module.BlendOpHelper.DestFactorRGB = subShaderModule.BlendOpHelper.DestFactorRGB;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper.CustomEdited = false;
							module.BlendOpHelper.SourceFactorRGB = item.ActionBlendRGBSource;
							module.BlendOpHelper.DestFactorRGB = item.ActionBlendRGBDest;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendRGB1:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper1.SourceFactorRGB = subShaderModule.BlendOpHelper1.SourceFactorRGB;
						module.BlendOpHelper1.DestFactorRGB = subShaderModule.BlendOpHelper1.DestFactorRGB;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper1.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper1.CustomEdited = false;
							module.BlendOpHelper1.SourceFactorRGB = item.ActionBlendRGBSource1;
							module.BlendOpHelper1.DestFactorRGB = item.ActionBlendRGBDest1;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendRGB2:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper2.SourceFactorRGB = subShaderModule.BlendOpHelper2.SourceFactorRGB;
						module.BlendOpHelper2.DestFactorRGB = subShaderModule.BlendOpHelper2.DestFactorRGB;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper2.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper2.CustomEdited = false;
							module.BlendOpHelper2.SourceFactorRGB = item.ActionBlendRGBSource2;
							module.BlendOpHelper2.DestFactorRGB = item.ActionBlendRGBDest2;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendRGB3:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper3.SourceFactorRGB = subShaderModule.BlendOpHelper3.SourceFactorRGB;
						module.BlendOpHelper3.DestFactorRGB = subShaderModule.BlendOpHelper3.DestFactorRGB;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper3.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper3.CustomEdited = false;
							module.BlendOpHelper3.SourceFactorRGB = item.ActionBlendRGBSource3;
							module.BlendOpHelper3.DestFactorRGB = item.ActionBlendRGBDest3;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendAlpha:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper.SourceFactorAlpha = subShaderModule.BlendOpHelper.SourceFactorAlpha;
						module.BlendOpHelper.DestFactorAlpha = subShaderModule.BlendOpHelper.DestFactorAlpha;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper.CustomEdited = false;
							module.BlendOpHelper.CurrentAlphaIndex = 1;
							module.BlendOpHelper.SourceFactorAlpha = item.ActionBlendAlphaSource;
							module.BlendOpHelper.DestFactorAlpha = item.ActionBlendAlphaDest;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendAlpha1:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper1.SourceFactorAlpha = subShaderModule.BlendOpHelper1.SourceFactorAlpha;
						module.BlendOpHelper1.DestFactorAlpha = subShaderModule.BlendOpHelper1.DestFactorAlpha;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper1.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper1.CustomEdited = false;
							module.BlendOpHelper1.CurrentAlphaIndex = 1;
							module.BlendOpHelper1.SourceFactorAlpha = item.ActionBlendAlphaSource1;
							module.BlendOpHelper1.DestFactorAlpha = item.ActionBlendAlphaDest1;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendAlpha2:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper2.SourceFactorAlpha = subShaderModule.BlendOpHelper2.SourceFactorAlpha;
						module.BlendOpHelper2.DestFactorAlpha = subShaderModule.BlendOpHelper2.DestFactorAlpha;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper2.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper2.CustomEdited = false;
							module.BlendOpHelper2.CurrentAlphaIndex = 1;
							module.BlendOpHelper2.SourceFactorAlpha = item.ActionBlendAlphaSource2;
							module.BlendOpHelper2.DestFactorAlpha = item.ActionBlendAlphaDest2;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendAlpha3:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper3.SourceFactorAlpha = subShaderModule.BlendOpHelper3.SourceFactorAlpha;
						module.BlendOpHelper3.DestFactorAlpha = subShaderModule.BlendOpHelper3.DestFactorAlpha;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper3.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper3.CustomEdited = false;
							module.BlendOpHelper3.CurrentAlphaIndex = 1;
							module.BlendOpHelper3.SourceFactorAlpha = item.ActionBlendAlphaSource3;
							module.BlendOpHelper3.DestFactorAlpha = item.ActionBlendAlphaDest3;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendOpRGB:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper.BlendOpRGB = subShaderModule.BlendOpHelper.BlendOpRGB;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper.CustomEdited = false;
							module.BlendOpHelper.BlendOpRGB = item.ActionBlendOpRGB;
						}
					}
				}
				break;
				case PropertyActionsEnum.BlendOpAlpha:
				{
					if( item.CopyFromSubShader )
					{
						module.BlendOpHelper.BlendOpAlpha = subShaderModule.BlendOpHelper.BlendOpAlpha;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.BlendOpHelper.CustomEdited;
						if( performAction )
						{
							module.BlendOpHelper.CustomEdited = false;
							module.BlendOpHelper.BlendOpAlpha = item.ActionBlendOpAlpha;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilReference:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.ReferenceValue = subShaderModule.StencilBufferHelper.ReferenceValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.ReferenceValue = item.ActionStencilReference;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilReadMask:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.ReadMaskValue = subShaderModule.StencilBufferHelper.ReadMaskValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.ReadMaskValue = item.ActionStencilReadMask;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilWriteMask:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.WriteMaskValue = subShaderModule.StencilBufferHelper.WriteMaskValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.WriteMaskValue = item.ActionStencilWriteMask;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilComparison:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.ComparisonFunctionIdxValue = subShaderModule.StencilBufferHelper.ComparisonFunctionIdxValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.ComparisonFunctionIdxValue = item.ActionStencilComparison;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilPass:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.PassStencilOpIdxValue = subShaderModule.StencilBufferHelper.PassStencilOpIdxValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.PassStencilOpIdxValue = item.ActionStencilPass;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilFail:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.FailStencilOpIdxValue = subShaderModule.StencilBufferHelper.FailStencilOpIdxValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.FailStencilOpIdxValue = item.ActionStencilFail;
						}
					}
				}
				break;
				case PropertyActionsEnum.StencilZFail:
				{
					if( item.CopyFromSubShader )
					{
						module.StencilBufferHelper.ZFailStencilOpIdxValue = subShaderModule.StencilBufferHelper.ZFailStencilOpIdxValue;
					}
					else
					{
						bool performAction = !ContainerGraph.IsLoading || !module.StencilBufferHelper.CustomEdited;
						if( performAction )
						{
							module.StencilBufferHelper.CustomEdited = false;
							module.StencilBufferHelper.ZFailStencilOpIdxValue = item.ActionStencilZFail;
						}
					}
				}
				break;
				case PropertyActionsEnum.RenderType:
				{
					module.TagsHelper.AddSpecialTag( TemplateSpecialTags.RenderType , item );
				}
				break;
				case PropertyActionsEnum.RenderQueue:
				{
					module.TagsHelper.AddSpecialTag( TemplateSpecialTags.Queue , item );
				}
				break;
				case PropertyActionsEnum.DisableBatching:
				{
					module.TagsHelper.AddSpecialTag( TemplateSpecialTags.DisableBatching , item );
				}
				break;
				case PropertyActionsEnum.ChangeTagValue:
				{
					module.TagsHelper.ChangeTagValue( item.ActionData , item.ActionBuffer );
				}
				break;
			}
		}

		public void OnCustomPassOptionSelected( bool actionFromUser , bool isRefreshing , bool invertAction , TemplateOptionUIItem uiItem , params TemplateActionItem[] validActions )
		{
			m_passOptions.OnCustomOptionSelected( actionFromUser , isRefreshing , invertAction , this , uiItem , validActions );
		}

		public void OnCustomSubShaderOptionSelected( bool actionFromUser , bool isRefreshing , bool invertAction , TemplateOptionUIItem uiItem , params TemplateActionItem[] validActions )
		{
			if( m_isMainOutputNode )
				m_subShaderOptions.OnCustomOptionSelected( actionFromUser , isRefreshing , invertAction , this , uiItem , validActions );
		}

		void SetupCustomOptionsFromTemplate( bool newTemplate )
		{
			m_passOptions.SetupCustomOptionsFromTemplate( this , newTemplate );
			if( m_isMainOutputNode )
				m_subShaderOptions.SetupCustomOptionsFromTemplate( this , newTemplate );
		}

		void SetPassCustomOptionsInfo( TemplateMultiPassMasterNode masterNode )
		{
			TemplateMultiPassMasterNode mainMasterNode = masterNode.IsMainOutputNode ? masterNode : ( m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode );
			mainMasterNode.SubShaderOptions.SetSubShaderCustomOptionsPortsInfo( masterNode , ref m_currentDataCollector );
			masterNode.PassOptions.SetCustomOptionsInfo( masterNode , ref m_currentDataCollector );
		}

		void RefreshCustomOptionsDict()
		{
			m_passOptions.RefreshCustomOptionsDict();
			if( m_isMainOutputNode )
				m_subShaderOptions.RefreshCustomOptionsDict();
		}

		void SetCategoryIdxFromTemplate()
		{
			int templateCount = m_containerGraph.ParentWindow.TemplatesManagerInstance.TemplateCount;
			for( int i = 0 ; i < templateCount ; i++ )
			{
				int idx = i + 1;
				TemplateMultiPass templateData = m_containerGraph.ParentWindow.TemplatesManagerInstance.GetTemplate( i ) as TemplateMultiPass;
				if( templateData != null && m_templateMultiPass != null && m_templateMultiPass.GUID.Equals( templateData.GUID ) )
					m_masterNodeCategory = idx;
			}
		}

		public void CheckTemplateChanges()
		{
			if( m_invalidNode )
				return;

			if( IsLODMainMasterNode )
			{
				if( m_containerGraph.MultiPassMasterNodes.Count != m_templateMultiPass.MasterNodesRequired )
				{
					if( m_availableCategories == null )
						RefreshAvailableCategories();

					if( DebugConsoleWindow.DeveloperMode )
						Debug.Log( "Template Pass amount was changed. Rebuiling master nodes" );

					m_containerGraph.ParentWindow.ReplaceMasterNode( m_availableCategories[ m_masterNodeCategory ] , true );
				}
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			if( m_invalidNode )
			{
				return;
			}
			base.OnNodeLogicUpdate( drawInfo );

			if( m_templateMultiPass == null )
			{
				// Hotcode reload has happened
				SetTemplate( null , false , true , m_subShaderIdx , m_passIdx , SetTemplateSource.HotCodeReload );
				CheckTemplateChanges();
			}

			if( m_reRegisterTemplateData )
			{
				RegisterProperties();
			}

			if( m_fetchMasterNodeCategory )
			{
				if( m_availableCategories != null )
				{
					m_fetchMasterNodeCategory = false;
					SetCategoryIdxFromTemplate();
				}
			}

			if( m_fireTemplateChange )
			{
				m_fireTemplateChange = false;
				m_containerGraph.FireMasterNodeReplacedEvent();
			}

			if( m_subShaderModule.HasValidData )
			{
				m_subShaderModule.OnLogicUpdate( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules );
			}

			if( m_passModule.HasValidData )
			{
				m_passModule.OnLogicUpdate( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules );
			}

			if( !m_isMainOutputNode && m_mainMasterNodeRef == null )
			{
				m_mainMasterNodeRef = m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode;
			}

			if( m_refreshLODValueMasterNodes && ( EditorApplication.timeSinceStartup - m_refreshLODValueMasterNodesTimestamp ) > MaxLODEditTimestamp )
			{
				m_refreshLODValueMasterNodes = false;
				m_refocusLODValueMasterNodes = true;
				m_containerGraph.SortLODMasterNodes();
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			if( m_isInvisible == InvisibilityStatus.Visible )
			{
				base.Draw( drawInfo );
			}
		}

		public override void OnNodeLayout( DrawInfo drawInfo )
		{
			if( m_invalidNode )
			{
				if( m_isMainOutputNode )
				{
					string newGUID = string.Empty;
					if( m_containerGraph.ParentWindow.TemplatesManagerInstance.CheckIfDeprecated( m_templateGUID , out newGUID ) )
					{
						m_shaderModelIdx = 0;
						SetMasterNodeCategoryFromGUID( newGUID );
						m_containerGraph.ParentWindow.ReplaceMasterNode( m_availableCategories[ m_masterNodeCategory ] , false );
					}
					else
					{
						UIUtils.ShowMessage( "Invalid current template. Switching to Standard Surface" , MessageSeverity.Error );
						m_shaderModelIdx = 0;
						m_masterNodeCategory = 0;
						m_containerGraph.ParentWindow.ReplaceMasterNode( new MasterNodeCategoriesData( AvailableShaderTypes.SurfaceShader , m_shaderName ) , false );
					}
				}
				return;
			}

			if( m_isInvisible != InvisibilityStatus.Visible )
			{
				return;
			}

			if( !IsMainOutputNode )
			{
				if( !IsInvisible && Docking )
				{
					m_useSquareNodeTitle = true;
					TemplateMultiPassMasterNode master = ContainerGraph.CurrentMasterNode as TemplateMultiPassMasterNode;
					m_position = master.TruePosition;
					m_position.height = 32;
					int masterIndex = ContainerGraph.MultiPassMasterNodes.NodesList.IndexOf( master );
					int index = ContainerGraph.MultiPassMasterNodes.GetNodeRegisterIdx( UniqueId );
					if( index > masterIndex )
					{
						int backTracking = 0;
						for( int i = index - 1 ; i > masterIndex ; i-- )
						{
							if( !ContainerGraph.MultiPassMasterNodes.NodesList[ i ].IsInvisible && ContainerGraph.MultiPassMasterNodes.NodesList[ i ].Docking )
								backTracking++;
						}
						m_position.y = master.TruePosition.yMax + 1 + 33 * ( backTracking );// ContainerGraph.MultiPassMasterNodes.NodesList[ index - 1 ].TruePosition.yMax;
						base.OnNodeLayout( drawInfo );
					}
					else
					{
						int forwardTracking = 1;
						for( int i = index + 1 ; i < masterIndex ; i++ )
						{
							if( !ContainerGraph.MultiPassMasterNodes.NodesList[ i ].IsInvisible && ContainerGraph.MultiPassMasterNodes.NodesList[ i ].Docking )
								forwardTracking++;
						}
						m_position.y = master.TruePosition.y - 33 * ( forwardTracking );// ContainerGraph.MultiPassMasterNodes.NodesList[ index - 1 ].TruePosition.yMax;
						base.OnNodeLayout( drawInfo );
					}
				}
				else
				{
					m_useSquareNodeTitle = false;
					base.OnNodeLayout( drawInfo );
				}
			}
			else
			{
				base.OnNodeLayout( drawInfo );
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );
			if( m_invalidNode )
				return;

			if( m_isInvisible == InvisibilityStatus.Visible )
			{
				if( m_containerGraph.IsInstancedShader )
				{
					DrawInstancedIcon( drawInfo );
				}
			}
		}

		public override void UpdateFromShader( Shader newShader )
		{
			if( m_currentMaterial != null && m_currentMaterial.shader != newShader )
			{
				m_currentMaterial.shader = newShader;
			}
			CurrentShader = newShader;
		}

		public override void UpdateMasterNodeMaterial( Material material )
		{
			m_currentMaterial = material;
			FireMaterialChangedEvt();
		}

		void DrawReloadButton()
		{
			if( GUILayout.Button( ReloadTemplateStr ) && m_templateMultiPass != null )
			{
				m_templateMultiPass.Reload();
			}
		}

		void DrawOpenTemplateButton()
		{
			GUILayout.BeginHorizontal();
			{
				if( GUILayout.Button( OpenTemplateStr ) && m_templateMultiPass != null )
				{
					try
					{
						string pathname = AssetDatabase.GUIDToAssetPath( m_templateMultiPass.GUID );
						if( !string.IsNullOrEmpty( pathname ) )
						{
							Shader selectedTemplate = AssetDatabase.LoadAssetAtPath<Shader>( pathname );
							if( selectedTemplate != null )
							{
								AssetDatabase.OpenAsset( selectedTemplate , 1 );
							}
						}
					}
					catch( Exception e )
					{
						Debug.LogException( e );
					}
				}

				if( GUILayout.Button( "\u25C4" , GUILayout.Width( 18 ) , GUILayout.Height( 18 ) ) && m_templateMultiPass != null )
				{
					try
					{
						string pathname = AssetDatabase.GUIDToAssetPath( m_templateMultiPass.GUID );
						if( !string.IsNullOrEmpty( pathname ) )
						{
							Shader selectedTemplate = AssetDatabase.LoadAssetAtPath<Shader>( pathname );
							if( selectedTemplate != null )
							{
								Event.current.Use();
								Selection.activeObject = selectedTemplate;
								EditorGUIUtility.PingObject( Selection.activeObject );
							}
						}
					}
					catch( Exception e )
					{
						Debug.LogException( e );
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if( m_invalidNode )
				return;

			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout , CommonPropertiesStr , DrawCommonProperties );
			NodeUtils.DrawPropertyGroup( ref m_subStringFoldout , SubShaderModuleStr , DrawSubShaderProperties );
			NodeUtils.DrawPropertyGroup( ref m_passFoldout , PassModuleStr , DrawPassProperties );

			DrawMaterialInputs( UIUtils.MenuItemToolbarStyle , false );

			if( m_propertyOrderChanged )
			{
				List<TemplateMultiPassMasterNode> mpNodes = ContainerGraph.MultiPassMasterNodes.NodesList;
				int count = mpNodes.Count;
				for( int i = 0 ; i < count ; i++ )
				{
					if( mpNodes[ i ].UniqueId != UniqueId )
					{
						mpNodes[ i ].CopyPropertyListFrom( this );
					}
				}
			}

#if SHOW_TEMPLATE_HELP_BOX
			EditorGUILayout.HelpBox( WarningMessage, MessageType.Warning );
#endif
		}

		// this will be removed later when PBR options are created
		void SetExtraDefine( string define )
		{
			List<TemplateMultiPassMasterNode> nodes = this.ContainerGraph.MultiPassMasterNodes.NodesList;
			int count = nodes.Count;
			for( int nodeIdx = 0 ; nodeIdx < count ; nodeIdx++ )
			{
				nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( "#define " + define , false );
			}
		}

		void AddHDKeywords()
		{
			if( m_templateMultiPass.CustomTemplatePropertyUI == CustomTemplatePropertyUIEnum.None )
				return;

			if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.SRPType != TemplateSRPType.HDRP ||
				!m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPIsPBR )
				return;

			switch( m_hdSrpMaterialType )
			{
				case HDSRPMaterialType.SubsurfaceScattering:
				{
					SetExtraDefine( SRPMaterialSubsurfaceScatteringKeyword );
					//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialSubsurfaceScatteringKeyword );
					if( m_thicknessPort != null && m_thicknessPort.HasOwnOrLinkConnection )
					{
						SetExtraDefine( SRPMaterialTransmissionKeyword );
						//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialTransmissionKeyword );
					}
				}
				break;
				case HDSRPMaterialType.Standard:
				break;
				case HDSRPMaterialType.Specular:
				{
					SetExtraDefine( SRPHDMaterialSpecularKeyword );
					//m_currentDataCollector.AddToDefines( UniqueId, SRPHDMaterialSpecularKeyword );
				}
				break;
				case HDSRPMaterialType.Anisotropy:
				{
					SetExtraDefine( SRPMaterialAnisotropyKeyword );
					//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialAnisotropyKeyword );
				}
				break;
				case HDSRPMaterialType.Iridescence:
				{
					SetExtraDefine( SRPMaterialIridiscenceKeyword );
					//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialIridiscenceKeyword );
				}
				break;
				case HDSRPMaterialType.Translucent:
				{
					SetExtraDefine( SRPMaterialTransmissionKeyword );
					//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialTransmissionKeyword );
				}
				break;
			}

			if( m_coatMaskPort != null && m_coatMaskPort.HasOwnOrLinkConnection )
			{
				SetExtraDefine( SRPMaterialClearCoatKeyword );
				//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialClearCoatKeyword );
			}
		}

		void FetchHDPorts()
		{
			if( m_fetchPorts )
			{
				m_fetchPorts = false;
				if( m_inputPorts.Count > 4 )
				{
					m_specularPort = GetInputPortByUniqueId( 3 );
					m_metallicPort = GetInputPortByUniqueId( 4 );
					m_coatMaskPort = GetInputPortByUniqueId( 11 );
					m_diffusionProfilePort = GetInputPortByUniqueId( 12 );
					m_subsurfaceMaskPort = GetInputPortByUniqueId( 13 );
					m_thicknessPort = GetInputPortByUniqueId( 14 );
					m_anisotropyPort = GetInputPortByUniqueId( 15 );
					m_iridescenceThicknessPort = GetInputPortByUniqueId( 16 );
					m_iridescenceMaskPort = GetInputPortByUniqueId( 17 );
					m_indexOfRefractionPort = GetInputPortByUniqueId( 18 );
					m_transmittanceColorPort = GetInputPortByUniqueId( 19 );
					m_transmittanceAbsorptionDistancePort = GetInputPortByUniqueId( 20 );
					m_transmittanceMaskPort = GetInputPortByUniqueId( 21 );
				}
			}
		}

		void ConfigHDPorts()
		{
			if( m_templateMultiPass.CustomTemplatePropertyUI == CustomTemplatePropertyUIEnum.None )
				return;

			if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.SRPType != TemplateSRPType.HDRP ||
				!m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPIsPBR )
				return;

			FetchHDPorts();
			if( m_inputPorts.Count > 4 )
			{
				switch( m_hdSrpMaterialType )
				{
					case HDSRPMaterialType.SubsurfaceScattering:
					{
						m_specularPort.Visible = false;
						m_metallicPort.Visible = false;
						m_coatMaskPort.Visible = true;
						m_diffusionProfilePort.Visible = true;
						m_subsurfaceMaskPort.Visible = true;
						m_thicknessPort.Visible = true;
						m_anisotropyPort.Visible = false;
						m_iridescenceThicknessPort.Visible = false;
						m_iridescenceMaskPort.Visible = false;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
					case HDSRPMaterialType.Standard:
					{
						m_specularPort.Visible = false;
						m_metallicPort.Visible = true;
						m_coatMaskPort.Visible = true;
						m_diffusionProfilePort.Visible = false;
						m_subsurfaceMaskPort.Visible = false;
						m_thicknessPort.Visible = false;
						m_anisotropyPort.Visible = false;
						m_iridescenceThicknessPort.Visible = false;
						m_iridescenceMaskPort.Visible = false;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
					case HDSRPMaterialType.Specular:
					{
						m_specularPort.Visible = true;
						m_metallicPort.Visible = false;
						m_coatMaskPort.Visible = true;
						m_diffusionProfilePort.Visible = false;
						m_subsurfaceMaskPort.Visible = false;
						m_thicknessPort.Visible = false;
						m_anisotropyPort.Visible = false;
						m_iridescenceThicknessPort.Visible = false;
						m_iridescenceMaskPort.Visible = false;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
					case HDSRPMaterialType.Anisotropy:
					{
						m_specularPort.Visible = false;
						m_metallicPort.Visible = true;
						m_coatMaskPort.Visible = true;
						m_diffusionProfilePort.Visible = false;
						m_subsurfaceMaskPort.Visible = false;
						m_thicknessPort.Visible = false;
						m_anisotropyPort.Visible = true;
						m_iridescenceThicknessPort.Visible = false;
						m_iridescenceMaskPort.Visible = false;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
					case HDSRPMaterialType.Iridescence:
					{
						m_specularPort.Visible = false;
						m_metallicPort.Visible = true;
						m_coatMaskPort.Visible = true;
						m_diffusionProfilePort.Visible = false;
						m_subsurfaceMaskPort.Visible = false;
						m_thicknessPort.Visible = false;
						m_anisotropyPort.Visible = false;
						m_iridescenceThicknessPort.Visible = true;
						m_iridescenceMaskPort.Visible = true;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
					case HDSRPMaterialType.Translucent:
					{
						m_specularPort.Visible = false;
						m_metallicPort.Visible = false;
						m_coatMaskPort.Visible = false;
						m_diffusionProfilePort.Visible = true;
						m_subsurfaceMaskPort.Visible = false;
						m_thicknessPort.Visible = true;
						m_anisotropyPort.Visible = false;
						m_iridescenceThicknessPort.Visible = false;
						m_iridescenceMaskPort.Visible = false;
						m_indexOfRefractionPort.Visible = false;
						m_transmittanceColorPort.Visible = false;
						m_transmittanceAbsorptionDistancePort.Visible = false;
						m_transmittanceMaskPort.Visible = false;
					}
					break;
				}
			}
			m_sizeIsDirty = ( m_isInvisible == InvisibilityStatus.Visible );
		}


		public void SetShaderLODValueAndLabel( int value )
		{
			if( ShaderLOD != value )
				ShaderLOD = value;

			if( ContainerGraph.HasLODs )
			{
				SetClippedAdditionalTitle( string.Format( LodSubtitle , ShaderLOD ) );
			}
			else
			{
				SetAdditonalTitleText( string.Empty );
			}
		}

		void DrawLODAddRemoveButtons()
		{
			DrawLODAddRemoveButtons( -2 , true );
		}

		void DrawLODAddRemoveButtons( int index , bool showRemove )
		{
			if( GUILayoutButton( string.Empty , UIUtils.PlusStyle , GUILayout.Width( 15 ) ) )
			{
				Vector2 minPos = Vec2Position;
				//bool newNodePositionMode = false;
				//if( newNodePositionMode )
				//{
				//	for( int lod = 0; lod < ContainerGraph.LodMultiPassMasternodes.Count; lod++ )
				//	{
				//		if( ContainerGraph.LodMultiPassMasternodes[ lod ].Count != 0 )
				//		{
				//			Vector2 currPos = ContainerGraph.LodMultiPassMasternodes[ lod ].NodesList[ m_passIdx ].Vec2Position;
				//			if( currPos.y > minPos.y )
				//			{
				//				minPos = currPos;
				//			}
				//		}
				//		else
				//		{
				//			if( index < 0 )
				//			{
				//				index = lod;
				//			}
				//			break;
				//		}
				//	}
				//}
				//else
				//{
				for( int lod = ContainerGraph.LodMultiPassMasternodes.Count - 1 ; lod >= 0 ; lod-- )
				{
					if( ContainerGraph.LodMultiPassMasternodes[ lod ].Count != 0 )
					{
						minPos = ContainerGraph.LodMultiPassMasternodes[ lod ].NodesList[ m_passIdx ].Vec2Position;
						break;
					}
				}
				//}

				minPos.y += HeightEstimate + 10;
				ContainerGraph.CreateLodMasterNodes( m_templateMultiPass , index , minPos );
			}

			if( showRemove && GUILayoutButton( string.Empty , UIUtils.MinusStyle , GUILayout.Width( 15 ) ) )
			{
				ContainerGraph.DestroyLodMasterNodes( index );
			}
		}

		void SetupLODNodeName()
		{
			if( IsMainOutputNode )
			{
				if( string.IsNullOrEmpty( m_mainLODName ) )
				{
					m_shaderNameIsTitle = true;
					m_content.text = GenerateClippedTitle( m_croppedShaderName );
				}
				else
				{
					m_shaderNameIsTitle = false;
					m_content.text = GenerateClippedTitle( m_mainLODName );
				}
			}
			else
			{
				m_shaderNameIsTitle = false;
				m_content.text = GenerateClippedTitle( m_passName );
			}
		}

		public void DrawLodRowItem( bool listMode )
		{
			float labelWidthBuffer = EditorGUIUtility.labelWidth;
			EditorGUILayout.BeginHorizontal();
			if( listMode )
			{
				if( GUILayout.Button( "\u25b6" , GUILayout.Width( 18 ) , GUILayout.Height( 18 ) ) )
				{
					m_containerGraph.ParentWindow.FocusOnNode( this , 1 , false , true );
				}
				EditorGUI.BeginChangeCheck();
				GUI.SetNextControlName( LodValueId + m_lodIndex );
				m_shaderLOD = EditorGUILayoutIntField( string.Empty , m_shaderLOD , GUILayout.Width( 50 ) );
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				EditorGUIUtility.labelWidth = 45;
				GUI.SetNextControlName( LodValueId + m_lodIndex );
				m_shaderLOD = EditorGUILayoutIntField( "LOD" , ShaderLOD , GUILayout.Width( 100 ) );
				EditorGUIUtility.labelWidth = labelWidthBuffer;
			}

			if( EditorGUI.EndChangeCheck() )
			{
				m_refreshLODValueMasterNodes = true;
				m_refreshLODValueMasterNodesTimestamp = EditorApplication.timeSinceStartup;

				if( ContainerGraph.HasLODs )
					SetClippedAdditionalTitle( string.Format( LodSubtitle , ShaderLOD ) );
			}

			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName( LodNameId + ShaderLOD );
			if( listMode )
			{
				m_mainLODName = EditorGUILayoutTextField( string.Empty , m_mainLODName , GUILayout.Width( 100 ) );
			}
			else
			{
				GUILayout.Space( -15 );
				EditorGUIUtility.labelWidth = 45;
				m_mainLODName = EditorGUILayoutTextField( string.Empty , m_mainLODName );
				EditorGUIUtility.labelWidth = labelWidthBuffer;
			}
			if( EditorGUI.EndChangeCheck() )
			{
				// If reorder is scheduled make sure it doesn't happen when editing LOD name
				if( m_refreshLODValueMasterNodes )
					m_refreshLODValueMasterNodesTimestamp = EditorApplication.timeSinceStartup;

				SetupLODNodeName();
			}

			if( listMode )
				DrawLODAddRemoveButtons( m_lodIndex , ( m_lodIndex >= 0 ) );

			EditorGUILayout.EndHorizontal();

			if( m_refocusLODValueMasterNodes )
			{
				m_refocusLODValueMasterNodes = false;
				string focusedControl = GUI.GetNameOfFocusedControl();
				if( focusedControl.Contains( LodValueId ) )
				{
					GUI.FocusControl( LodValueId + m_lodIndex );
					TextEditor te = (TextEditor)GUIUtility.GetStateObject( typeof( TextEditor ) , GUIUtility.keyboardControl );
					if( te != null )
					{
						te.SelectTextEnd();
					}
				}
				else if( focusedControl.Contains( LodNameId ) )
				{
					GUI.FocusControl( LodNameId + m_lodIndex );
					TextEditor te = (TextEditor)GUIUtility.GetStateObject( typeof( TextEditor ) , GUIUtility.keyboardControl );
					if( te != null )
					{
						te.SelectTextEnd();
					}
				}
			}
		}

		void DrawLOD()
		{
			if( m_templateMultiPass.CanAddLODs && m_lodIndex == -1 )
			{
				EditorGUILayout.Space();

				DrawLodRowItem( true );
				EditorGUILayout.Space();

				for( int i = 0 ; i < ContainerGraph.LodMultiPassMasternodes.Count ; i++ )
				{
					if( ContainerGraph.LodMultiPassMasternodes[ i ].NodesList.Count > 0 )
					{
						TemplateMultiPassMasterNode masterNode = m_containerGraph.LodMultiPassMasternodes[ i ].NodesList[ m_passIdx ];
						masterNode.DrawLodRowItem( true );
						EditorGUILayout.Space();
					}
				}
				EditorGUILayout.Space();
			}
		}

		void DrawCommonProperties()
		{
			if( m_isMainOutputNode )
			{
				//if( m_templateMultiPass.CanAddLODs && m_lodIndex == -1 )
				//{
				//	if( GUILayoutButton( string.Empty, UIUtils.PlusStyle, GUILayout.Width( 15 ) ) )
				//	{
				//		ContainerGraph.CreateLodMasterNodes( m_templateMultiPass, Vec2Position );
				//	}


				//	if( GUILayoutButton( string.Empty, UIUtils.MinusStyle, GUILayout.Width( 15 ) ) )
				//	{
				//		ContainerGraph.DestroyLodMasterNodes();
				//	}

				//}

				//EditorGUILayout.LabelField( "LOD: " + m_lodIndex );
				DrawShaderName();
				DrawCurrentShaderType();

				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPIsPBRHD )
				{
					if( m_templateMultiPass.CustomTemplatePropertyUI == CustomTemplatePropertyUIEnum.HDPBR )
					{
						EditorGUI.BeginChangeCheck();
						CurrentHDMaterialType = (HDSRPMaterialType)EditorGUILayoutEnumPopup( HDSRPMaterialTypeStr , m_hdSrpMaterialType );
						if( EditorGUI.EndChangeCheck() )
							ConfigHDPorts();
					}
				}

				EditorGUI.BeginChangeCheck();
				DrawPrecisionProperty( false );
				if( EditorGUI.EndChangeCheck() )
					ContainerGraph.CurrentPrecision = m_currentPrecisionType;

				DrawSamplingMacros();

				m_drawInstancedHelper.Draw( this );
				m_fallbackHelper.Draw( this );
				DrawCustomInspector( m_templateMultiPass.SRPtype != TemplateSRPType.BiRP );
				m_subShaderOptions.DrawCustomOptions( this );
				m_dependenciesHelper.Draw( this , true );
			}
			//EditorGUILayout.LabelField( m_subShaderIdxStr );
			//EditorGUILayout.LabelField( m_passIdxStr );

			if( IsLODMainMasterNode && m_templateMultiPass.CanAddLODs )
			{
				NodeUtils.DrawNestedPropertyGroup( ref m_lodFoldout , AdditionalLODsStr , DrawLOD , DrawLODAddRemoveButtons );
			}

			DrawOpenTemplateButton();
			if( DebugConsoleWindow.DeveloperMode )
				DrawReloadButton();

		}

		public void DrawSubShaderProperties()
		{
			if( !m_isMainOutputNode )
			{
				m_mainMasterNodeRef.DrawSubShaderProperties();
				return;
			}

			bool noValidData = true;
			if( ShaderLOD > 0 )
			{
				noValidData = false;
				if( m_templateMultiPass.CanAddLODs && m_containerGraph.LodMultiPassMasternodes[ 0 ].Count > 0 )
				{
					DrawLodRowItem( false );
				}
				else
				{
					ShaderLOD = EditorGUILayoutIntField( SubShaderLODValueLabel , ShaderLOD );
				}
			}

			if( m_subShaderModule.HasValidData )
			{
				noValidData = false;
				m_subShaderModule.Draw( this , m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules );
				//if( m_subShaderModule.IsDirty )
				//{
				//	List<TemplateMultiPassMasterNode> mpNodes = UIUtils.CurrentWindow.CurrentGraph.MultiPassMasterNodes.NodesList;
				//	int count = mpNodes.Count;
				//	for( int i = 0; i < count; i++ )
				//	{
				//		if( mpNodes[ i ].SubShaderIdx == m_subShaderIdx && mpNodes[ i ].UniqueId != UniqueId )
				//		{
				//			mpNodes[ i ].SubShaderModule.CopyFrom( m_subShaderModule );
				//		}
				//	}
				//	m_subShaderModule.IsDirty = false;
				//}
			}

			m_passSelector.Draw( this );

			if( noValidData )
			{
				EditorGUILayout.HelpBox( NoSubShaderPropertyStr , MessageType.Info );
			}
		}

		void DrawPassProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_passName = EditorGUILayoutTextField( PassNameStr , m_passName );
			if( EditorGUI.EndChangeCheck() )
			{
				if( m_passName.Length > 0 )
				{
					m_passName = UIUtils.RemoveShaderInvalidCharacters( m_passName );
				}
				else
				{
					m_passName = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].PassNameContainer.Data;
				}
				//if( !m_templateMultiPass.IsSinglePass )
				//	SetClippedTitle( m_passName );
			}
			EditorGUILayout.LabelField( Pass.Modules.PassUniqueName );
			if( m_passModule.HasValidData )
			{
				m_passModule.Draw( this , m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules , m_subShaderModule );
			}

			m_usePass.Draw( this , false );
			m_passOptions.DrawCustomOptions( this );
		}

		bool CreateInstructionsForList( TemplateData templateData , ref List<InputPort> ports , ref string shaderBody , ref List<string> vertexInstructions , ref List<string> fragmentInstructions )
		{
			if( ports.Count == 0 )
				return true;
			AddHDKeywords();
			bool isValid = true;
			//UIUtils.CurrentWindow.CurrentGraph.ResetNodesLocalVariables();
			for( int i = 0 ; i < ports.Count ; i++ )
			{
				TemplateInputData inputData = templateData.InputDataFromId( ports[ i ].PortId );
				if( ports[ i ].HasOwnOrLinkConnection )
				{
					//if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.SRPType == TemplateSRPType.URP )
					//{
					//	if( ports[ i ].Name.Contains( "Normal" ) )
					//	{
					//		m_currentDataCollector.AddToDirectives( SRPMaterialNormalMapKeyword, -1, AdditionalLineType.Define );
					//	}

					//	if( ports[ i ].Name.Contains( "Alpha Clip Threshold" ) )
					//	{
					//		m_currentDataCollector.AddToDirectives( SRPMaterialBlendModeAlphaClipThresholdKeyword, -1, AdditionalLineType.Define );
					//	}

					//	if( ports[ i ].Name.Contains( "Specular" ) )
					//	{
					//		m_currentDataCollector.AddToDirectives( SRPLWMaterialSpecularKeyword, -1, AdditionalLineType.Define );
					//	}
					//}
					//else if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.SRPType == TemplateSRPType.HDRP )
					//{
					//	if( ports[ i ].Name.Contains( "Normal" ) )
					//	{
					//		//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialNormalMapKeyword );
					//	}

					//	if( ports[ i ].Name.Contains( "Alpha Clip Threshold" ) )
					//	{
					//		//m_currentDataCollector.AddToDefines( UniqueId, SRPMaterialAlphaTestKeyword );
					//	}

					//}

					m_currentDataCollector.ResetInstructions();
					m_currentDataCollector.ResetVertexInstructions();

					m_currentDataCollector.PortCategory = ports[ i ].Category;
					string newPortInstruction = ports[ i ].GeneratePortInstructions( ref m_currentDataCollector );

					if( m_currentDataCollector.DirtySpecialLocalVariables )
					{
						string cleanVariables = m_currentDataCollector.SpecialLocalVariables.Replace( "\t" , string.Empty );
						m_currentDataCollector.AddInstructions( cleanVariables , false );
						m_currentDataCollector.ClearSpecialLocalVariables();
					}

					if( m_currentDataCollector.DirtyVertexVariables )
					{
						string cleanVariables = m_currentDataCollector.VertexLocalVariables.Replace( "\t" , string.Empty );
						m_currentDataCollector.AddVertexInstruction( cleanVariables , UniqueId , false );
						m_currentDataCollector.ClearVertexLocalVariables();
					}

					// fill functions
					for( int j = 0 ; j < m_currentDataCollector.InstructionsList.Count ; j++ )
					{
						fragmentInstructions.Add( m_currentDataCollector.InstructionsList[ j ].PropertyName );
					}

					for( int j = 0 ; j < m_currentDataCollector.VertexDataList.Count ; j++ )
					{
						vertexInstructions.Add( m_currentDataCollector.VertexDataList[ j ].PropertyName );
					}

					m_templateMultiPass.SetPassInputData( m_subShaderIdx , m_passIdx , ports[ i ].PortId , newPortInstruction );
					isValid = m_templateMultiPass.FillTemplateBody( m_subShaderIdx , m_passIdx , inputData.TagId , ref shaderBody , newPortInstruction ) && isValid;
				}
				else
				{
					m_templateMultiPass.SetPassInputData( m_subShaderIdx , m_passIdx , ports[ i ].PortId , inputData.DefaultValue );
					isValid = m_templateMultiPass.FillTemplateBody( m_subShaderIdx , m_passIdx , inputData.TagId , ref shaderBody , inputData.DefaultValue ) && isValid;
				}
			}
			return isValid;
		}

		public string BuildShaderBody( MasterNodeDataCollector inDataCollector , ref MasterNodeDataCollector outDataCollector )
		{
			List<TemplateMultiPassMasterNode> list = ContainerGraph.MultiPassMasterNodes.NodesList;
			int currentSubshader = list[ 0 ].SubShaderIdx;
			m_templateMultiPass.SetShaderName( string.Format( TemplatesManager.NameFormatter , m_shaderName ) );
			if( string.IsNullOrEmpty( m_customInspectorName ) )
			{
				m_templateMultiPass.SetCustomInspector( string.Empty );
			}
			else
			{
				m_templateMultiPass.SetCustomInspector( CustomInspectorFormatted );
			}

			m_templateMultiPass.SetFallback( m_fallbackHelper.FallbackShader );
			m_templateMultiPass.SetDependencies( m_dependenciesHelper.GenerateDependencies() );

			if( inDataCollector != null )
				outDataCollector.CopyPropertiesFromDataCollector( inDataCollector );

			outDataCollector.TemplateDataCollectorInstance.CurrentSRPType = m_templateMultiPass.SRPtype;

			int lastActivePass = m_passSelector.LastActivePass;
			int count = list.Count;
			bool filledSubshaderData = false;

			bool foundExcludePassName = false;
			string excludePassName = string.Empty;

			foundExcludePassName = CheckExcludeAllPassOptions( m_subShaderOptions , out excludePassName );
			for( int i = 0 ; i < count ; i++ )
			{
				bool removePass = !m_passSelector.IsVisible( i ) || ( foundExcludePassName && !list[ i ].OriginalPassName.Equals( excludePassName ) );

				list[ 0 ].CurrentTemplate.IdManager.SetPassIdUsage( i , removePass );
				if( removePass )
				{
					if( m_isMainOutputNode )
					{
						//Make sure that property change options are set even if the main master node is invisible
						CheckPropertyChangesOnOptions( m_subShaderOptions );
					}
					continue;
				}

				list[ i ].CollectData();
				list[ i ].FillPassData( this , outDataCollector.TemplateDataCollectorInstance );

				if( list[ i ].SubShaderIdx == currentSubshader )
				{
					outDataCollector.CopyPropertiesFromDataCollector( list[ i ].CurrentDataCollector );
				}
				else
				{
					list[ i - 1 ].FillPropertyData( outDataCollector );
					list[ i - 1 ].FillSubShaderData();
					outDataCollector.Destroy();
					outDataCollector = new MasterNodeDataCollector();
					outDataCollector.CopyPropertiesFromDataCollector( list[ i ].CurrentDataCollector );

					currentSubshader = list[ i ].SubShaderIdx;
				}

				// Last element must the one filling subshader data
				// as only there all properties are caught
				//if( i == ( count - 1 ) )
				if( i == lastActivePass )
				{
					list[ i ].FillPropertyData( outDataCollector );
				}

				if( list[ i ].IsMainOutputNode )
				{
					filledSubshaderData = true;
					list[ i ].FillSubShaderData();
				}
			}

			if( !filledSubshaderData )
			{
				FillSubShaderData();
			}
			outDataCollector.TemplateDataCollectorInstance.BuildCBuffer( -1 );

			//Fill uniforms is set on last since we need to collect all srp batcher data ( if needed )
			//To set it into each pass
			for( int i = 0 ; i < count ; i++ )
			{
				bool removePass = !m_passSelector.IsVisible( i ) || ( foundExcludePassName && !list[ i ].OriginalPassName.Equals( excludePassName ) );
				if( removePass )
					continue;

				list[ i ].FillUniforms( outDataCollector.TemplateDataCollectorInstance );
			}

			return list[ 0 ].CurrentTemplate.IdManager.BuildShader();
		}

		public string BuildLOD( MasterNodeDataCollector inDataCollector , ref MasterNodeDataCollector outDataCollector )
		{
			UsageListTemplateMultiPassMasterNodes bufferNodesList = ContainerGraph.MultiPassMasterNodes;
			int bufferMasterNodeId = ContainerGraph.CurrentMasterNodeId;

			ContainerGraph.MultiPassMasterNodes = ContainerGraph.LodMultiPassMasternodes[ m_lodIndex ];
			ContainerGraph.CurrentMasterNodeId = UniqueId;

			m_templateMultiPass.ResetState();
			base.Execute( string.Empty , false );
			string shaderBody = BuildShaderBody( inDataCollector , ref outDataCollector );


			ContainerGraph.MultiPassMasterNodes = bufferNodesList;
			ContainerGraph.CurrentMasterNodeId = bufferMasterNodeId;
			return shaderBody;
		}

		public override Shader Execute( string pathname , bool isFullPath )
		{
			ForceReordering();
			MasterNodeDataCollector overallDataCollector = new MasterNodeDataCollector();

			//BUILD LOD
			string allLodSubShaders = string.Empty;
			if( m_templateMultiPass.CanAddLODs && ContainerGraph.HasLODs )
			{
				for( int lod = 0 ; lod < ContainerGraph.LodMultiPassMasternodes.Count ; lod++ )
				{
					if( ContainerGraph.LodMultiPassMasternodes[ lod ].Count == 0 )
						break;

					TemplateMultiPassMasterNode newMasterNode = ContainerGraph.LodMultiPassMasternodes[ lod ].NodesList.Find( ( x ) => x.IsMainOutputNode );
					string lodSubShaders = newMasterNode.BuildLOD( null , ref overallDataCollector );
					lodSubShaders = TemplateHelperFunctions.GetSubShaderFrom( lodSubShaders ) + "\n";
					allLodSubShaders += lodSubShaders;
				}
			}

			//BUILD MAIN
			m_templateMultiPass.ResetState();
			base.Execute( pathname , isFullPath );
			MasterNodeDataCollector dummy = new MasterNodeDataCollector();
			string shaderBody = BuildShaderBody( overallDataCollector , ref dummy );

			if( m_templateMultiPass.CanAddLODs )
			{
				//COMBINE LOD WITH MAIN
				// Commented the if out since we always want to replace the tag with something, even string.empty to clean the tag out of the final shader
				//if( !string.IsNullOrEmpty( allLodSubShaders ) )
				shaderBody = shaderBody.Replace( TemplatesManager.TemplateLODsTag , allLodSubShaders );
			}

			UpdateShaderAsset( ref pathname , ref shaderBody , isFullPath );
			return m_currentShader;
		}

		public void CollectData()
		{
			if( m_inputPorts.Count == 0 )
				return;

			ContainerGraph.ResetNodesLocalVariables();
			m_optionsDefineContainer.RemoveTemporaries();
			m_currentDataCollector = new MasterNodeDataCollector( this );
			m_currentDataCollector.TemplateDataCollectorInstance.SetMultipassInfo( m_templateMultiPass , m_subShaderIdx , m_passIdx , m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.SRPType );
			m_currentDataCollector.TemplateDataCollectorInstance.FillSpecialVariables( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ] );
			SetupNodeCategories();
			if( m_containerGraph.IsInstancedShader )
			{
				string blockName = UIUtils.RemoveInvalidCharacters( ContainerGraph.GetMainMasterNodeOfLOD( -1 ).ShaderName );
				m_currentDataCollector.SetupInstancePropertiesBlock( blockName );
			}
			TemplateData templateData = m_templateMultiPass.CreateTemplateData( m_shaderName , string.Empty , m_subShaderIdx , m_passIdx );
			m_currentDataCollector.TemplateDataCollectorInstance.BuildFromTemplateData( m_currentDataCollector , templateData );

			if( m_currentDataCollector.TemplateDataCollectorInstance.InterpData.DynamicMax )
			{
				int interpolatorAmount = -1;
				if( m_passModule.ShaderModelHelper.ValidData )
				{
					interpolatorAmount = m_passModule.ShaderModelHelper.InterpolatorAmount;
				}
				else
				{
					TemplateModulesHelper subShaderModule = IsMainOutputNode ? m_subShaderModule : ( m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode ).SubShaderModule;
					if( subShaderModule.ShaderModelHelper.ValidData )
					{
						interpolatorAmount = subShaderModule.ShaderModelHelper.InterpolatorAmount;
					}
				}

				if( interpolatorAmount > -1 )
				{
					m_currentDataCollector.TemplateDataCollectorInstance.InterpData.RecalculateAvailableInterpolators( interpolatorAmount );
				}
			}

			//Copy Properties
			{
				int shaderPropertiesAmount = m_templateMultiPass.AvailableShaderProperties.Count;
				for( int i = 0 ; i < shaderPropertiesAmount ; i++ )
				{
					m_currentDataCollector.SoftRegisterUniform( m_templateMultiPass.AvailableShaderProperties[ i ] );
				}
			}
			//Copy Globals from SubShader level
			{
				int subShaderGlobalAmount = m_templateMultiPass.SubShaders[ m_subShaderIdx ].AvailableShaderGlobals.Count;
				for( int i = 0 ; i < subShaderGlobalAmount ; i++ )
				{
					m_currentDataCollector.SoftRegisterUniform( m_templateMultiPass.SubShaders[ m_subShaderIdx ].AvailableShaderGlobals[ i ] );
				}
			}
			//Copy Globals from Pass Level
			{
				int passGlobalAmount = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].AvailableShaderGlobals.Count;
				for( int i = 0 ; i < passGlobalAmount ; i++ )
				{
					m_currentDataCollector.SoftRegisterUniform( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].AvailableShaderGlobals[ i ] );
				}
			}
			// Check Current Options for property changes on subshader
			if( m_isMainOutputNode )
			{
				CheckPropertyChangesOnOptions( m_subShaderOptions );
			}

			// Check Current Options for property changes on pass
			CheckPropertyChangesOnOptions( m_passOptions );


			//Set SRP info
			if( m_templateMultiPass.SRPtype != TemplateSRPType.BiRP )
				ASEPackageManagerHelper.SetSRPInfoOnDataCollector( ref m_currentDataCollector );

			RegisterStandaloneFuntions();
			m_containerGraph.CheckPropertiesAutoRegister( ref m_currentDataCollector );

			//Sort ports by both
			List<InputPort> fragmentPorts = new List<InputPort>();
			List<InputPort> vertexPorts = new List<InputPort>();

			SortInputPorts( ref vertexPorts , ref fragmentPorts );


			string shaderBody = templateData.TemplateBody;

			List<string> vertexInstructions = new List<string>();
			List<string> fragmentInstructions = new List<string>();

			bool validBody = true;

			//validBody = CreateInstructionsForList( templateData, ref fragmentPorts, ref shaderBody, ref vertexInstructions, ref fragmentInstructions ) && validBody;
			//ContainerGraph.ResetNodesLocalVariablesIfNot( MasterNodePortCategory.Vertex );
			//validBody = CreateInstructionsForList( templateData, ref vertexPorts, ref shaderBody, ref vertexInstructions, ref fragmentInstructions ) && validBody;
			validBody = CreateInstructionsForList( templateData , ref vertexPorts , ref shaderBody , ref vertexInstructions , ref fragmentInstructions ) && validBody;
			validBody = CreateInstructionsForList( templateData , ref fragmentPorts , ref shaderBody , ref vertexInstructions , ref fragmentInstructions ) && validBody;

			if( !m_isMainOutputNode && m_mainMasterNodeRef == null )
			{
				m_mainMasterNodeRef = m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode;
			}

			TerrainDrawInstancedHelper drawInstanced = m_isMainOutputNode ? m_drawInstancedHelper : m_mainMasterNodeRef.DrawInstancedHelperInstance;
			drawInstanced.UpdateDataCollectorForTemplates( ref m_currentDataCollector , ref vertexInstructions );

			templateData.ResetTemplateUsageData();

			// Fill vertex interpolators assignment
			for( int i = 0 ; i < m_currentDataCollector.VertexInterpDeclList.Count ; i++ )
			{
				vertexInstructions.Add( m_currentDataCollector.VertexInterpDeclList[ i ] );
			}

			vertexInstructions.AddRange( m_currentDataCollector.TemplateDataCollectorInstance.GetInterpUnusedChannels() );

			//Fill common local variables and operations
			validBody = m_templateMultiPass.FillVertexInstructions( m_subShaderIdx , m_passIdx , vertexInstructions.ToArray() ) && validBody;
			validBody = m_templateMultiPass.FillFragmentInstructions( m_subShaderIdx , m_passIdx , fragmentInstructions.ToArray() ) && validBody;

			vertexInstructions.Clear();
			vertexInstructions = null;

			fragmentInstructions.Clear();
			fragmentInstructions = null;

			// Add Instanced Properties
			if( m_containerGraph.IsInstancedShader )
			{
				m_currentDataCollector.OptimizeInstancedProperties();
				m_currentDataCollector.TabifyInstancedVars();

				//string cbufferBegin = m_currentDataCollector.IsSRP ?
				//							string.Format( IOUtils.SRPInstancedPropertiesBegin, "UnityPerMaterial" ) :
				//							string.Format( IOUtils.InstancedPropertiesBegin, m_currentDataCollector.InstanceBlockName );
				//string cBufferEnd = m_currentDataCollector.IsSRP ? ( string.Format( IOUtils.SRPInstancedPropertiesEnd, m_currentDataCollector.InstanceBlockName ) ) : IOUtils.InstancedPropertiesEnd;
				string cbufferBegin = m_currentDataCollector.IsSRP ?
							string.Format( IOUtils.LWSRPInstancedPropertiesBegin , m_currentDataCollector.InstanceBlockName ) :
							string.Format( IOUtils.InstancedPropertiesBegin , m_currentDataCollector.InstanceBlockName );
				string cBufferEnd = m_currentDataCollector.IsSRP ? ( string.Format( IOUtils.LWSRPInstancedPropertiesEnd , m_currentDataCollector.InstanceBlockName ) ) : IOUtils.InstancedPropertiesEnd;

				m_currentDataCollector.InstancedPropertiesList.Insert( 0 , new PropertyDataCollector( -1 , cbufferBegin ) );
				m_currentDataCollector.InstancedPropertiesList.Add( new PropertyDataCollector( -1 , cBufferEnd ) );
				m_currentDataCollector.UniformsList.AddRange( m_currentDataCollector.InstancedPropertiesList );
			}

			if( m_currentDataCollector.DotsPropertiesList.Count > 0 )
			{
				m_currentDataCollector.DotsPropertiesList.Insert( 0 , new PropertyDataCollector( -1 , "UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)" ) );
				m_currentDataCollector.DotsPropertiesList.Insert( 0 , new PropertyDataCollector( -1 , "#ifdef UNITY_DOTS_INSTANCING_ENABLED" ) );
				m_currentDataCollector.DotsPropertiesList.Insert( 0 , new PropertyDataCollector( -1 , "" ) );
				m_currentDataCollector.DotsPropertiesList.Add( new PropertyDataCollector( -1 , "UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)" ) );
				m_currentDataCollector.DotsDefinesList.Add( new PropertyDataCollector( -1 , "#endif" ) );
				m_currentDataCollector.UniformsList.AddRange( m_currentDataCollector.DotsPropertiesList );
				m_currentDataCollector.UniformsList.AddRange( m_currentDataCollector.DotsDefinesList );
			}

			TemplateShaderModelModule shaderModelModule = m_isMainOutputNode ? m_subShaderModule.ShaderModelHelper : m_mainMasterNodeRef.SubShaderModule.ShaderModelHelper;
			string shaderModel = string.Empty;
			if( m_passModule.ShaderModelHelper.ValidData )
			{
				shaderModel = m_passModule.ShaderModelHelper.CurrentShaderModel;
			}
			else if( shaderModelModule.ValidData )
			{
				shaderModel = shaderModelModule.CurrentShaderModel;
			}
			else if( m_templateMultiPass.GlobalShaderModel.IsValid )
			{
				shaderModel = m_templateMultiPass.GlobalShaderModel.Value;
			}
			else
			{
				shaderModel = ( m_templateMultiPass.SRPtype == TemplateSRPType.HDRP ) ? "4.5" : "3.0";
			}

			m_currentDataCollector.TemplateDataCollectorInstance.CheckInterpolatorOverflow( shaderModel , m_passName );
		}

		public bool CheckExcludeAllPassOptions( TemplateOptionsUIHelper optionsUI , out string passName )
		{
			List<TemplateOptionUIItem> options = optionsUI.PassCustomOptionsUI;
			for( int optionIdx = 0 ; optionIdx < options.Count ; optionIdx++ )
			{
				if( options[ optionIdx ].IsVisible )
				{
					TemplateActionItem[] actionItems = options[ optionIdx ].CurrentOptionActions.Columns;
					for( int actionIdx = 0 ; actionIdx < actionItems.Length ; actionIdx++ )
					{
						if( actionItems[ actionIdx ].ActionType == AseOptionsActionType.ExcludeAllPassesBut )
						{
							passName = actionItems[ actionIdx ].ActionData;
							return true;
						}
					}
				}
			}

			passName = string.Empty;
			return false;
		}

		public void CheckPropertyChangesOnOptions( TemplateOptionsUIHelper optionsUI )
		{
			//Only Main LOD master node can change shader properties
			if( !IsLODMainMasterNode )
				return;

			List<TemplateOptionUIItem> options = optionsUI.PassCustomOptionsUI;
			for( int optionIdx = 0 ; optionIdx < options.Count ; optionIdx++ )
			{
				if( options[ optionIdx ].IsVisible )
				{
					TemplateActionItem[] actionItems = options[ optionIdx ].CurrentOptionActions.Columns;
					for( int actionIdx = 0 ; actionIdx < actionItems.Length ; actionIdx++ )
					{
						if( actionItems[ actionIdx ].ActionType == AseOptionsActionType.SetShaderProperty && !string.IsNullOrEmpty( actionItems[ actionIdx ].ActionBuffer ) )
						{
							TemplateShaderPropertyData data = m_templateMultiPass.GetShaderPropertyData( actionItems[ actionIdx ].ActionData );
							if( data != null )
							{
								string newPropertyValue = data.CreatePropertyForValue( actionItems[ actionIdx ].ActionBuffer );
								CurrentTemplate.IdManager.SetReplacementText( data.FullValue , newPropertyValue );
								if( CurrentMaterial != null )
								{
									switch( data.PropertyDataType )
									{
										case WirePortDataType.FLOAT:
										{
											float value = 0;
											if( actionItems[ actionIdx ].GetFloatValueFromActionBuffer( out value ) )
											{
												CurrentMaterial.SetFloat( data.PropertyName , value );
											}
										}
										break;
										case WirePortDataType.INT:
										{
											int value = 0;
											if( actionItems[ actionIdx ].GetIntValueFromActionBuffer( out value ) )
											{
												CurrentMaterial.SetInt( data.PropertyName , value );
											}
										}
										break;
										case WirePortDataType.UINT:
										case WirePortDataType.FLOAT2:
										case WirePortDataType.FLOAT3:
										case WirePortDataType.FLOAT4:
										case WirePortDataType.COLOR:
										break;
									}

								}
							}
						}
					}

					if( options[ optionIdx ].Options.Type == AseOptionsType.Field )
					{
						foreach( var item in CurrentTemplate.IdManager.RegisteredTags )
						{
							if( item.Output.Equals( options[ optionIdx ].Options.FieldInlineName ) )
							{
								var node = options[ optionIdx ].Options.FieldValue.GetPropertyNode();
								if( node != null && ( node.IsConnected || node.AutoRegister || node.UniqueId < -1 ) && options[ optionIdx ].Options.FieldValue.Active )
								{
									item.Replacement = node.PropertyName;
								}
							}
						}
					}
				}
			}
		}

		public void FillPropertyData( MasterNodeDataCollector dataCollector = null )
		{
			MasterNodeDataCollector currDataCollector = ( dataCollector == null ) ? m_currentDataCollector : dataCollector;

			// Temporary hack
			if( m_templateMultiPass.SRPtype != TemplateSRPType.BiRP )
			{
				if( m_templateMultiPass.AvailableShaderProperties.Find( x => x.PropertyName.Equals( "_AlphaCutoff" ) ) == null )
				{
					if( !currDataCollector.ContainsProperty("_AlphaCutoff") )
					{
						currDataCollector.AddToProperties( UniqueId, "[HideInInspector] _AlphaCutoff(\"Alpha Cutoff \", Range(0, 1)) = 0.5", -1 );
					}
				}

				if( m_templateMultiPass.AvailableShaderProperties.Find( x => x.PropertyName.Equals( "_EmissionColor" ) ) == null )
				{
					if( !currDataCollector.ContainsProperty( "_EmissionColor" ) )
					{
						currDataCollector.AddToProperties( UniqueId, "[HideInInspector] _EmissionColor(\"Emission Color\", Color) = (1,1,1,1)", -1 );
					}
				}
			}

			// here we add ASE attributes to the material properties that allows materials to communicate with ASE
			//if( m_templateMultiPass.SRPtype != TemplateSRPType.BiRP )
			{
				string currentInspector = IsLODMainMasterNode ? m_customInspectorName : ContainerGraph.GetMainMasterNodeOfLOD( -1 ).CurrentInspector;
				bool isASENativeInspector = Constants.DefaultCustomInspector.Equals( currentInspector );
				bool isUnityNativeInspector = Constants.UnityNativeInspectors.FindIndex( x => x.Equals( currentInspector ) ) > 0;

				List<PropertyDataCollector> list = new List<PropertyDataCollector>( currDataCollector.PropertiesDict.Values );
				list.Sort( ( x , y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
				if( isUnityNativeInspector )
				{
					for( int i = 0 ; i < list.Count ; i++ )
					{
						if( !( list[ i ].PropertyName.Contains( "[HideInInspector]" ) || list[ i ].PropertyName.Contains( "//" ) ) )
						{
							list[ i ].PropertyName = "[ASEBegin]" + list[ i ].PropertyName;
							break;
						}
					}
				}

				if( !isASENativeInspector )
				{
					for( int i = list.Count - 1 ; i >= 0 ; i-- )
					{
						if( !( list[ i ].PropertyName.Contains( "[HideInInspector]" ) || list[ i ].PropertyName.Contains( "//" ) ) )
						{
							list[ i ].PropertyName = "[ASEEnd]" + list[ i ].PropertyName;
							break;
						}
					}
				}
			}

			m_templateMultiPass.SetPropertyData( currDataCollector.BuildUnformatedPropertiesStringArr() );
		}

		public void FillSubShaderData( /*MasterNodeDataCollector dataCollector = null */)
		{
			//MasterNodeDataCollector currDataCollector = ( dataCollector == null ) ? m_currentDataCollector : dataCollector;
			//// SubShader Data

			//m_templateMultiPass.SetPropertyData( currDataCollector.BuildUnformatedPropertiesStringArr() );
			//templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModulePass, m_subShaderIdx, currDataCollector.GrabPassList );
			if( ShaderLOD > -1 )
			{
				string lodUniqueId = m_templateMultiPass.SubShaders[ m_subShaderIdx ].UniquePrefix + "Module" + m_templateMultiPass.SubShaders[ m_subShaderIdx ].LODContainer.Id;
				m_templateMultiPass.IdManager.SetReplacementText( lodUniqueId , "LOD " + ShaderLOD );
			}

			SetModuleData( m_subShaderModule , true );
		}

		public bool CheckDefineListItem( PropertyDataCollector item )
		{
			//The IsDirective flag in this context is used to determine if its #pragma
			if( item.IsDirective )
			{
				return !m_currentDataCollector.ContainsPragma( item.PropertyName );
			}
			else
			{
				return !m_currentDataCollector.ContainsDefine( item.PropertyName );
			}

		}

		public void FillPassData( TemplateMultiPassMasterNode masterNode , TemplateDataCollector mainTemplateDataCollector )
		{
			if( m_isInvisible != InvisibilityStatus.Visible )
			{
				if( masterNode.UniqueId != UniqueId )
				{
					if( ( m_invisibleOptions & (int)InvisibleOptionsEnum.SyncProperties ) > 0 )
					{
						PassModule.SyncWith( masterNode.PassModule );
					}
				}

				int inputCount = m_inputPorts.Count;
				for( int i = 0 ; i < inputCount ; i++ )
				{
					if( m_inputPorts[ i ].HasExternalLink )
					{
						TemplateMultiPassMasterNode linkedNode = m_inputPorts[ i ].ExternalLinkNode as TemplateMultiPassMasterNode;
						if( linkedNode != null )
						{
							SetLinkedModuleData( linkedNode.PassModule );
						}
					}
				}
			}

			SetModuleData( m_passModule , false );
			if( m_currentDataCollector != null )
			{
				if( Pass.CustomOptionsContainer.CopyOptionsFromMainPass )
				{
					SetPassCustomOptionsInfo( m_containerGraph.CurrentMasterNode as TemplateMultiPassMasterNode );
				}
				else
				{
					SetPassCustomOptionsInfo( this );
				}

				var inputArray = m_currentDataCollector.VertexInputList.ToArray();

				m_templateMultiPass.SetPassData( TemplateModuleDataType.PassVertexData , m_subShaderIdx , m_passIdx , inputArray );
				m_templateMultiPass.SetPassData( TemplateModuleDataType.PassInterpolatorData , m_subShaderIdx , m_passIdx , m_currentDataCollector.InterpolatorList.ToArray() );

				List<PropertyDataCollector> afterNativesIncludePragmaDefineList = new List<PropertyDataCollector>();
				afterNativesIncludePragmaDefineList.AddRange( m_currentDataCollector.IncludesList );
				afterNativesIncludePragmaDefineList.AddRange( m_currentDataCollector.DefinesList );
				//includePragmaDefineList.AddRange( m_optionsDefineContainer.DefinesList );
				afterNativesIncludePragmaDefineList.AddRange( m_currentDataCollector.PragmasList );
				CheckSamplingMacrosFlag();
				m_currentDataCollector.AddASEMacros();
				afterNativesIncludePragmaDefineList.AddRange( m_currentDataCollector.AfterNativeDirectivesList );

				//includePragmaDefineList.AddRange( m_currentDataCollector.MiscList );

				List<PropertyDataCollector> beforeNatives = new List<PropertyDataCollector>();
				int defineListCount = m_optionsDefineContainer.DefinesList.Count;
				for( int i = 0 ; i < defineListCount ; i++ )
				{
					if( CheckDefineListItem( m_optionsDefineContainer.DefinesList[ i ] ) )
					{
						beforeNatives.Add( m_optionsDefineContainer.DefinesList[ i ] );
					}
				}

				beforeNatives.AddRange( m_currentDataCollector.BeforeNativeDirectivesList );

				m_templateMultiPass.SetPassData( TemplateModuleDataType.ModulePragmaBefore , m_subShaderIdx , m_passIdx , beforeNatives );
				m_templateMultiPass.SetPassData( TemplateModuleDataType.ModulePragma , m_subShaderIdx , m_passIdx , afterNativesIncludePragmaDefineList );

				m_currentDataCollector.TemplateDataCollectorInstance.CloseLateDirectives();

				//Add Functions
				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.FunctionsTag.IsValid )
				{
					m_currentDataCollector.FunctionsList.InsertRange( 0 , m_currentDataCollector.TemplateDataCollectorInstance.LateDirectivesList );
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleFunctions , m_subShaderIdx , m_passIdx , m_currentDataCollector.FunctionsList );
				}
				else
				{
					m_currentDataCollector.UniformsList.InsertRange( 0 , m_currentDataCollector.TemplateDataCollectorInstance.LateDirectivesList );
					m_currentDataCollector.UniformsList.AddRange( m_currentDataCollector.FunctionsList );
				}

				//copy srp batch if present
				//if( m_currentDataCollector.IsSRP )
				//{
				//	m_currentDataCollector.UniformsList.AddRange( mainTemplateDataCollector.SrpBatcherPropertiesList );
				//}
				//m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleGlobals, m_subShaderIdx, m_passIdx, m_currentDataCollector.UniformsList );

				m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleInputVert , m_subShaderIdx , m_passIdx , m_currentDataCollector.TemplateDataCollectorInstance.VertexInputParamsStr );
				m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleInputFrag , m_subShaderIdx , m_passIdx , m_currentDataCollector.TemplateDataCollectorInstance.FragInputParamsStr );

				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessVControlTag != null && m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessVControlTag.IsValid )
					m_templateMultiPass.SetPassData( TemplateModuleDataType.VControl , m_subShaderIdx , m_passIdx , inputArray );

				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessControlData != null && m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessControlData.IsValid )
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ControlData , m_subShaderIdx , m_passIdx , m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessControlData.GenerateControl( m_currentDataCollector.TemplateDataCollectorInstance.VertexDataDict , m_currentDataCollector.VertexInputList ) );

				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessDomainData != null && m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessDomainData.IsValid )
					m_templateMultiPass.SetPassData( TemplateModuleDataType.DomainData , m_subShaderIdx , m_passIdx , m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].TessDomainData.GenerateDomain( m_currentDataCollector.TemplateDataCollectorInstance.VertexDataDict , m_currentDataCollector.VertexInputList ) );

				afterNativesIncludePragmaDefineList.Clear();
				afterNativesIncludePragmaDefineList = null;

				beforeNatives.Clear();
				beforeNatives = null;
			}

			m_templateMultiPass.SetPassData( TemplateModuleDataType.PassNameData , m_subShaderIdx , m_passIdx , string.Format( PassNameFormateStr , m_passName ) );
		}

		public List<PropertyDataCollector> CrossCheckSoftRegisteredUniformList( List<PropertyDataCollector> uniformList )
		{
			List<PropertyDataCollector> newItems = new List<PropertyDataCollector>();
			for( int i = 0 ; i < uniformList.Count ; i++ )
			{
				if( !m_currentDataCollector.CheckIfSoftRegistered( uniformList[ i ].PropertyName ) )
				{
					newItems.Add( uniformList[ i ] );
				}
			}
			return newItems;
		}

		public void FillUniforms( TemplateDataCollector mainTemplateDataCollector )
		{
			if( m_currentDataCollector.IsSRP )
			{

				if( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPBatcherTag.IsValid )
				{
					List<PropertyDataCollector> finalList = CrossCheckSoftRegisteredUniformList( mainTemplateDataCollector.SrpBatcherPropertiesList );
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleSRPBatcher , m_subShaderIdx , m_passIdx , finalList );
					finalList.Clear();
					finalList = null;
				}
				else
				{
					List<PropertyDataCollector> finalList = CrossCheckSoftRegisteredUniformList( mainTemplateDataCollector.FullSrpBatcherPropertiesList );
					m_currentDataCollector.UniformsList.AddRange( finalList );
					finalList.Clear();
					finalList = null;
				}
			}
			m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleGlobals , m_subShaderIdx , m_passIdx , m_currentDataCollector.UniformsList );
		}

		void SetLinkedModuleData( TemplateModulesHelper linkedModule )
		{
			//if(	linkedModule.AdditionalPragmas.ValidData )
			//{
			//	linkedModule.AdditionalPragmas.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
			//}

			//if( linkedModule.AdditionalIncludes.ValidData )
			//{
			//	linkedModule.AdditionalIncludes.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
			//}

			//if( linkedModule.AdditionalDefines.ValidData )
			//{
			//	linkedModule.AdditionalDefines.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
			//}

			if( linkedModule.AdditionalDirectives.ValidData )
			{
				var pass = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ];
				linkedModule.AdditionalDirectives.AddAllToDataCollector( ref m_currentDataCollector , pass, pass.Modules.IncludePragmaContainer );
			}
		}

		void SetModuleData( TemplateModulesHelper module , bool isSubShader )
		{
			if( isSubShader )
			{

				//if ( module.AdditionalPragmas.ValidData )
				//{
				//	module.AdditionalPragmas.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.IncludePragmaContainer );
				//}

				//if ( module.AdditionalIncludes.ValidData )
				//{
				//	module.AdditionalIncludes.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.IncludePragmaContainer );
				//}

				//if ( module.AdditionalDefines.ValidData )
				//{
				//	module.AdditionalDefines.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.IncludePragmaContainer );
				//}

				if( module.AdditionalDirectives.ValidData )
				{
					module.AdditionalDirectives.AddAllToDataCollector( ref m_currentDataCollector , null, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules.IncludePragmaContainer );
				}

				if( module.TagsHelper.ValidData )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleTag , m_subShaderIdx , module.TagsHelper.GenerateTags() );
				}

				if( module.AllModulesMode )
				{
					string body = module.GenerateAllModulesString( isSubShader );
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.AllModules , m_subShaderIdx , body.Split( '\n' ) );
				}

				if( module.ShaderModelHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleShaderModel , m_subShaderIdx , module.ShaderModelHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.BlendOpHelper.IndependentModule && module.BlendOpHelper.ValidBlendMode )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendMode , m_subShaderIdx , module.BlendOpHelper.CurrentBlendFactor );
				}

				if( module.BlendOpHelper1.IndependentModule && module.BlendOpHelper1.ValidBlendMode )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendMode1 , m_subShaderIdx , module.BlendOpHelper1.CurrentBlendFactor );
				}

				if( module.BlendOpHelper2.IndependentModule && module.BlendOpHelper2.ValidBlendMode )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendMode2 , m_subShaderIdx , module.BlendOpHelper2.CurrentBlendFactor );
				}

				if( module.BlendOpHelper3.IndependentModule && module.BlendOpHelper3.ValidBlendMode )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendMode3 , m_subShaderIdx , module.BlendOpHelper3.CurrentBlendFactor );
				}

				if( module.BlendOpHelper.IndependentModule && module.BlendOpHelper.ValidBlendOp )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendOp , m_subShaderIdx , module.BlendOpHelper.CurrentBlendOp );
				}

				if( module.BlendOpHelper1.IndependentModule && module.BlendOpHelper1.ValidBlendOp )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendOp1 , m_subShaderIdx , module.BlendOpHelper1.CurrentBlendOp );
				}

				if( module.BlendOpHelper2.IndependentModule && module.BlendOpHelper2.ValidBlendOp )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendOp2 , m_subShaderIdx , module.BlendOpHelper2.CurrentBlendOp );
				}

				if( module.BlendOpHelper3.IndependentModule && module.BlendOpHelper3.ValidBlendOp )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleBlendOp3 , m_subShaderIdx , module.BlendOpHelper3.CurrentBlendOp );
				}

				if( module.AlphaToMaskHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleAlphaToMask , m_subShaderIdx , module.AlphaToMaskHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.CullModeHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleCullMode , m_subShaderIdx , module.CullModeHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleColorMask , m_subShaderIdx , module.ColorMaskHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper1.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleColorMask1 , m_subShaderIdx , module.ColorMaskHelper1.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper2.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleColorMask2 , m_subShaderIdx , module.ColorMaskHelper2.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper3.ValidAndIndependent )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleColorMask3 , m_subShaderIdx , module.ColorMaskHelper3.GenerateShaderData( isSubShader ) );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidZTest )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleZTest , m_subShaderIdx , module.DepthOphelper.CurrentZTestMode );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidZWrite )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleZwrite , m_subShaderIdx , module.DepthOphelper.CurrentZWriteMode );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidOffset )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleZOffset , m_subShaderIdx , module.DepthOphelper.CurrentOffset );
				}

				if( module.StencilBufferHelper.ValidAndIndependent )
				{
					CullMode cullMode = ( module.CullModeHelper.ValidData ) ? module.CullModeHelper.CurrentCullMode : CullMode.Back;
					string value = module.StencilBufferHelper.CreateStencilOp( cullMode );
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleStencil , m_subShaderIdx , value.Split( '\n' ) );
				}

				if( module.RenderingPlatforms.LoadedFromTemplate )
				{
					m_templateMultiPass.SetSubShaderData( TemplateModuleDataType.ModuleRenderPlatforms , m_subShaderIdx , module.RenderingPlatforms.CreateResult(true) );
				}

			}
			else
			{
				//if ( module.AdditionalPragmas.ValidData )
				//{
				//	module.AdditionalPragmas.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
				//}

				//if ( module.AdditionalIncludes.ValidData )
				//{
				//	module.AdditionalIncludes.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
				//}

				//if ( module.AdditionalDefines.ValidData )
				//{
				//	module.AdditionalDefines.AddToDataCollector( ref m_currentDataCollector, m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.IncludePragmaContainer );
				//}
				List<PropertyDataCollector> aboveUsePass = new List<PropertyDataCollector>();
				List<PropertyDataCollector> belowUsePass = new List<PropertyDataCollector>();
				m_usePass.BuildUsePassInfo( m_currentDataCollector , ref aboveUsePass , ref belowUsePass );
				//TODO Must place this on the correct place
				aboveUsePass.AddRange( belowUsePass );

				//adding grab pass after use pass on purpose, so it wont be caught by them
				aboveUsePass.AddRange( m_currentDataCollector.GrabPassList );

				m_templateMultiPass.SetPassData( TemplateModuleDataType.ModulePass , m_subShaderIdx , m_passIdx , aboveUsePass );
				//m_templateMultiPass.SetPassData( TemplateModuleDataType.EndPass, m_subShaderIdx, m_passIdx, bellowUsePass);

				if( module.AdditionalDirectives.ValidData )
				{
					var pass = m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ];
					module.AdditionalDirectives.AddAllToDataCollector( ref m_currentDataCollector , pass, pass.Modules.IncludePragmaContainer );
				}

				if( module.TagsHelper.ValidData )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleTag , m_subShaderIdx , m_passIdx , module.TagsHelper.GenerateTags() );
				}

				if( module.AllModulesMode )
				{
					string body = module.GenerateAllModulesString( isSubShader );
					m_templateMultiPass.SetPassData( TemplateModuleDataType.AllModules , m_subShaderIdx , m_passIdx , body.Split( '\n' ) );
				}

				if( module.ShaderModelHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleShaderModel , m_subShaderIdx , m_passIdx , module.ShaderModelHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.BlendOpHelper.IndependentModule && module.BlendOpHelper.ValidBlendMode )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendMode , m_subShaderIdx , m_passIdx , module.BlendOpHelper.CurrentBlendFactor );
				}

				if( module.BlendOpHelper1.IndependentModule && module.BlendOpHelper1.ValidBlendMode )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendMode1 , m_subShaderIdx , m_passIdx , module.BlendOpHelper1.CurrentBlendFactor );
				}

				if( module.BlendOpHelper2.IndependentModule && module.BlendOpHelper2.ValidBlendMode )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendMode2 , m_subShaderIdx , m_passIdx , module.BlendOpHelper2.CurrentBlendFactor );
				}

				if( module.BlendOpHelper3.IndependentModule && module.BlendOpHelper3.ValidBlendMode )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendMode3 , m_subShaderIdx , m_passIdx , module.BlendOpHelper3.CurrentBlendFactor );
				}

				if( module.BlendOpHelper.IndependentModule && module.BlendOpHelper.ValidBlendOp )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendOp , m_subShaderIdx , m_passIdx , module.BlendOpHelper.CurrentBlendOp );
				}

				if( module.BlendOpHelper1.IndependentModule && module.BlendOpHelper1.ValidBlendOp )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendOp1 , m_subShaderIdx , m_passIdx , module.BlendOpHelper1.CurrentBlendOp );
				}

				if( module.BlendOpHelper2.IndependentModule && module.BlendOpHelper2.ValidBlendOp )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendOp2 , m_subShaderIdx , m_passIdx , module.BlendOpHelper2.CurrentBlendOp );
				}

				if( module.BlendOpHelper3.IndependentModule && module.BlendOpHelper3.ValidBlendOp )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleBlendOp3 , m_subShaderIdx , m_passIdx , module.BlendOpHelper3.CurrentBlendOp );
				}

				if( module.AlphaToMaskHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleAlphaToMask , m_subShaderIdx , m_passIdx , module.AlphaToMaskHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.CullModeHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleCullMode , m_subShaderIdx , m_passIdx , module.CullModeHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleColorMask , m_subShaderIdx , m_passIdx , module.ColorMaskHelper.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper1.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleColorMask1 , m_subShaderIdx , m_passIdx , module.ColorMaskHelper1.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper2.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleColorMask2 , m_subShaderIdx , m_passIdx , module.ColorMaskHelper2.GenerateShaderData( isSubShader ) );
				}

				if( module.ColorMaskHelper3.ValidAndIndependent )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleColorMask3 , m_subShaderIdx , m_passIdx , module.ColorMaskHelper3.GenerateShaderData( isSubShader ) );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidZTest )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleZTest , m_subShaderIdx , m_passIdx , module.DepthOphelper.CurrentZTestMode );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidZWrite )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleZwrite , m_subShaderIdx , m_passIdx , module.DepthOphelper.CurrentZWriteMode );
				}

				if( module.DepthOphelper.IndependentModule && module.DepthOphelper.ValidOffset )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleZOffset , m_subShaderIdx , m_passIdx , module.DepthOphelper.CurrentOffset );
				}

				if( module.StencilBufferHelper.ValidAndIndependent )
				{
					CullMode cullMode = ( module.CullModeHelper.ValidData ) ? module.CullModeHelper.CurrentCullMode : CullMode.Back;
					string value = module.StencilBufferHelper.CreateStencilOp( cullMode );
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleStencil , m_subShaderIdx , m_passIdx , value.Split( '\n' ) );
				}

				if( module.RenderingPlatforms.LoadedFromTemplate )
				{
					m_templateMultiPass.SetPassData( TemplateModuleDataType.ModuleRenderPlatforms , m_subShaderIdx , m_passIdx , module.RenderingPlatforms.CreateResult( true ) );
				}
			}
		}

		public override string GenerateShaderForOutput( int outputId , ref MasterNodeDataCollector dataCollector , bool ignoreLocalvar )
		{
			return "0";
		}

		public override void Destroy()
		{
			base.Destroy();

			m_drawInstancedHelper = null;

			m_optionsDefineContainer.Destroy();
			m_optionsDefineContainer = null;

			m_passSelector.Destroy();
			m_passSelector = null;

			m_subShaderOptions.Destroy();
			m_passOptions.Destroy();

			m_fallbackHelper.Destroy();
			GameObject.DestroyImmediate( m_fallbackHelper );
			m_fallbackHelper = null;

			m_usePass.Destroy();
			GameObject.DestroyImmediate( m_usePass );
			m_usePass = null;

			m_dependenciesHelper.Destroy();
			m_dependenciesHelper = null;

			m_subShaderModule.Destroy();
			m_subShaderModule = null;
			m_passModule.Destroy();
			m_passModule = null;
			if( m_lodIndex == -1 )
			{
				ContainerGraph.MultiPassMasterNodes.RemoveNode( this );
			}
			else
			{
				ContainerGraph.LodMultiPassMasternodes[ m_lodIndex ].RemoveNode( this );
			}
		}

		void UpdateSubShaderPassStr()
		{
			//m_subShaderIdxStr = SubShaderModuleStr + m_templateMultiPass.SubShaders[ m_subShaderIdx ].Idx;
			//m_passIdxStr = PassModuleStr + m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Idx;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{

			base.ReadFromString( ref nodeParams );
			try
			{
				string currShaderName = GetCurrentParam( ref nodeParams );
				if( currShaderName.Length > 0 )
					currShaderName = UIUtils.RemoveShaderInvalidCharacters( currShaderName );

				m_templateGUID = GetCurrentParam( ref nodeParams );

				bool hasUniqueName = false;
				if( UIUtils.CurrentShaderVersion() > PASS_UNIQUE_ID_VERSION )
				{
					hasUniqueName = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				}

				if( hasUniqueName )
					m_passUniqueId = GetCurrentParam( ref nodeParams );

				m_subShaderIdx = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_passIdx = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				if( UIUtils.CurrentShaderVersion() > LOD_SUBSHADER_VERSION )
				{

					if( m_lodIndex != -1 )
					{
						m_containerGraph.MultiPassMasterNodes.RemoveNode( this );
						m_containerGraph.LodMultiPassMasternodes[ m_lodIndex ].AddNode( this );
					}
				}

				m_passName = GetCurrentParam( ref nodeParams );
				SetTemplate( null , false , true , m_subShaderIdx , m_passIdx , SetTemplateSource.ShaderLoad );
				////If value gotten from template is > -1 then it contains the LOD field
				////and we can properly write the value
				//if( m_subShaderLOD > -1 )
				//{
				//	m_subShaderLOD = subShaderLOD;
				//}

				// only in here, after SetTemplate, we know if shader name is to be used as title or not
				ShaderName = currShaderName;
				m_visiblePorts = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );

				m_subShaderModule.ReadFromString( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Modules, ref m_currentReadParamIdx , ref nodeParams );
				m_passModule.ReadFromString( m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[m_passIdx].Modules, ref m_currentReadParamIdx , ref nodeParams );
				if( UIUtils.CurrentShaderVersion() > 15308 )
				{
					m_fallbackHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					m_dependenciesHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
				}

				if( UIUtils.CurrentShaderVersion() > 15402 )
				{
					m_usePass.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
				}

				if( UIUtils.CurrentShaderVersion() > 15409 )
				{
					m_hdSrpMaterialType = (HDSRPMaterialType)Enum.Parse( typeof( HDSRPMaterialType ) , GetCurrentParam( ref nodeParams ) );
				}

				if( UIUtils.CurrentShaderVersion() > 15501 )
				{
					if( m_isMainOutputNode && UIUtils.CurrentShaderVersion() > PASS_SELECTOR_VERSION )
						m_subShaderOptions.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );

					m_passOptions.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
				}

				if( m_isMainOutputNode && UIUtils.CurrentShaderVersion() > PASS_SELECTOR_VERSION )
				{
					m_passSelector.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );

					// @diogo: reset pass selector for any shader below version 19000 (when pass number was changed)
					if ( UIUtils.CurrentShaderVersion() < 19000 )
					{
						m_passSelector.Reset();
					}
				}

				if( m_isMainOutputNode && UIUtils.CurrentShaderVersion() > 16203 )
				{
					m_drawInstancedHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
				}

				if( m_isMainOutputNode && UIUtils.CurrentShaderVersion() > LOD_SUBSHADER_VERSION )
				{
					m_mainLODName = GetCurrentParam( ref nodeParams );
					SetupLODNodeName();
				}
				else
				{
					m_content.text = GenerateClippedTitle( m_passName );
				}

				if( UIUtils.CurrentShaderVersion() > 18302 )
					SamplingMacros = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				else
					SamplingMacros = false;

				//if( m_templateMultiPass != null && !m_templateMultiPass.IsSinglePass )
				//{
				//	SetClippedTitle( m_passName );
				//}
			}
			catch( Exception e )
			{
				Debug.LogException( e , this );
			}

			m_containerGraph.CurrentCanvasMode = NodeAvailability.TemplateShader;
			if ( m_isMainOutputNode )
			{
				m_containerGraph.CurrentPrecision = m_currentPrecisionType;
			}
			CheckLegacyCustomInspectors();
		}

		void CheckLegacyCustomInspectors()
		{
#if UNITY_2021_2_OR_NEWER
			if( m_templateMultiPass.SubShaders[ 0 ].Modules.SRPType == TemplateSRPType.HDRP && ASEPackageManagerHelper.CurrentHDRPBaseline >= ASESRPBaseline.ASE_SRP_11 )
			{
				if( Constants.CustomInspectorHDLegacyTo11.ContainsKey( m_customInspectorName ) )
				{
					UIUtils.ShowMessage( string.Format( "Detected obsolete custom inspector '{0}' in shader meta. Converting to new one '{1}'" , m_customInspectorName , Constants.CustomInspectorHDLegacyTo11[ m_customInspectorName ] ) , MessageSeverity.Warning );
					m_customInspectorName = Constants.CustomInspectorHDLegacyTo11[ m_customInspectorName ];
				}
			}

			if( m_templateMultiPass.SubShaders[ 0 ].Modules.SRPType == TemplateSRPType.URP && ASEPackageManagerHelper.CurrentURPBaseline>= ASESRPBaseline.ASE_SRP_12 )
			{
				if( Constants.CustomInspectorURP10To12.ContainsKey( m_customInspectorName ) )
				{
					string newCustomInspector = string.Empty;
					if( TemplatesManager.URPLitGUID.Equals( m_templateMultiPass.GUID ))
					{
						newCustomInspector = "UnityEditor.ShaderGraphLitGUI";
					}
					else if( TemplatesManager.URPUnlitGUID.Equals( m_templateMultiPass.GUID ) )
					{
						newCustomInspector = "UnityEditor.ShaderGraphUnlitGUI";
					}

					if( !string.IsNullOrEmpty( newCustomInspector ) )
					{
						UIUtils.ShowMessage( string.Format( "Detected obsolete custom inspector '{0}' in shader meta. Converting to new one '{1}'" , m_customInspectorName , newCustomInspector ) , MessageSeverity.Warning );
						m_customInspectorName = newCustomInspector;
					}
				}

			}

#elif UNITY_2021_1_OR_NEWER
			if( m_templateMultiPass.SubShaders[ 0 ].Modules.SRPType == TemplateSRPType.HDRP && ASEPackageManagerHelper.CurrentHDRPBaseline >= ASESRPBaseline.ASE_SRP_11 )
			{
				if( Constants.CustomInspectorHDLegacyTo11.ContainsKey( m_customInspectorName ) )
				{
					UIUtils.ShowMessage( string.Format( "Detected obsolete custom inspector '{0}' in shader meta. Converting to new one '{1}'" , m_customInspectorName , Constants.CustomInspectorHDLegacyTo11[ m_customInspectorName ] ) , MessageSeverity.Warning );
					m_customInspectorName = Constants.CustomInspectorHDLegacyTo11[ m_customInspectorName ];
				}
			}
#elif UNITY_2020_2_OR_NEWER
			if(  m_templateMultiPass.SubShaders[0].Modules.SRPType == TemplateSRPType.HDRP && ASEPackageManagerHelper.CurrentHDRPBaseline >= ASESRPBaseline.ASE_SRP_10 )
			{
				if( Constants.CustomInspectorHD7To10.ContainsKey( m_customInspectorName ) )
				{
					UIUtils.ShowMessage( string.Format("Detected obsolete custom inspector '{0}' in shader meta. Converting to new one '{1}'", m_customInspectorName , Constants.CustomInspectorHD7To10[ m_customInspectorName ] ), MessageSeverity.Warning );
					m_customInspectorName = Constants.CustomInspectorHD7To10[ m_customInspectorName ];
				}
			}
#endif
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , ShaderName );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_templateGUID );

			bool hasUniquePassName = Pass.Modules.HasPassUniqueName;
			IOUtils.AddFieldValueToString( ref nodeInfo , hasUniquePassName );
			if( hasUniquePassName )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo , Pass.Modules.PassUniqueName );
			}

			IOUtils.AddFieldValueToString( ref nodeInfo , m_subShaderIdx );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_passIdx );

			IOUtils.AddFieldValueToString( ref nodeInfo , m_passName );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_visiblePorts );
			m_subShaderModule.WriteToString( ref nodeInfo );
			m_passModule.WriteToString( ref nodeInfo );
			m_fallbackHelper.WriteToString( ref nodeInfo );
			m_dependenciesHelper.WriteToString( ref nodeInfo );
			m_usePass.WriteToString( ref nodeInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_hdSrpMaterialType );
			if( m_isMainOutputNode )
				m_subShaderOptions.WriteToString( ref nodeInfo );

			m_passOptions.WriteToString( ref nodeInfo );

			if( m_isMainOutputNode )
			{
				m_passSelector.WriteToString( ref nodeInfo );
				m_drawInstancedHelper.WriteToString( ref nodeInfo );
			}

			if( m_isMainOutputNode )
				IOUtils.AddFieldValueToString( ref nodeInfo , m_mainLODName );

			IOUtils.AddFieldValueToString( ref nodeInfo , m_samplingMacros );
		}

		public override void ReadFromDeprecated( ref string[] nodeParams , Type oldType = null )
		{
			base.ReadFromString( ref nodeParams );
			try
			{
				string currShaderName = GetCurrentParam( ref nodeParams );
				if( currShaderName.Length > 0 )
					currShaderName = UIUtils.RemoveShaderInvalidCharacters( currShaderName );

				string templateGUID = GetCurrentParam( ref nodeParams );
				string templateShaderName = string.Empty;
				if( UIUtils.CurrentShaderVersion() > 13601 )
				{
					templateShaderName = GetCurrentParam( ref nodeParams );
				}

				TemplateMultiPass template = m_containerGraph.ParentWindow.TemplatesManagerInstance.GetTemplate( templateGUID ) as TemplateMultiPass;
				if( template != null )
				{
					m_templateGUID = templateGUID;
					SetTemplate( null , false , true , 0 , 0 , SetTemplateSource.ShaderLoad );
				}
				else
				{
					template = m_containerGraph.ParentWindow.TemplatesManagerInstance.GetTemplateByName( templateShaderName ) as TemplateMultiPass;
					if( template != null )
					{
						m_templateGUID = template.GUID;
						SetTemplate( null , false , true , 0 , 0 , SetTemplateSource.ShaderLoad );
					}
					else
					{
						m_masterNodeCategory = -1;
					}
				}

				if( m_invalidNode )
					return;

				// only in here, after SetTemplate, we know if shader name is to be used as title or not
				ShaderName = currShaderName;
				if( UIUtils.CurrentShaderVersion() > 13902 )
				{

					//BLEND MODULE
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.BlendData.ValidBlendMode )
					{
						m_subShaderModule.BlendOpHelper.ReadBlendModeFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.BlendData.ValidBlendMode )
					{
						m_passModule.BlendOpHelper.ReadBlendModeFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

					if( m_templateMultiPass.SubShaders[ 0 ].Modules.BlendData.ValidBlendOp )
					{
						m_subShaderModule.BlendOpHelper.ReadBlendOpFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.BlendData.ValidBlendOp )
					{
						m_passModule.BlendOpHelper.ReadBlendOpFromString( ref m_currentReadParamIdx , ref nodeParams );
					}


					//CULL MODE
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.CullModeData.DataCheck == TemplateDataCheck.Valid )
					{
						m_subShaderModule.CullModeHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.CullModeData.DataCheck == TemplateDataCheck.Valid )
					{
						m_passModule.CullModeHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

					//COLOR MASK
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.ColorMaskData.DataCheck == TemplateDataCheck.Valid )
					{
						m_subShaderModule.ColorMaskHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.ColorMaskData.DataCheck == TemplateDataCheck.Valid )
					{
						m_passModule.ColorMaskHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

					//STENCIL BUFFER
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.StencilData.DataCheck == TemplateDataCheck.Valid )
					{
						m_subShaderModule.StencilBufferHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.StencilData.DataCheck == TemplateDataCheck.Valid )
					{
						m_passModule.StencilBufferHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

				}

				if( UIUtils.CurrentShaderVersion() > 14202 )
				{
					//DEPTH OPTIONS
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.DepthData.ValidZWrite )
					{
						m_subShaderModule.DepthOphelper.ReadZWriteFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.DepthData.ValidZWrite )
					{
						m_passModule.DepthOphelper.ReadZWriteFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

					if( m_templateMultiPass.SubShaders[ 0 ].Modules.DepthData.ValidZTest )
					{
						m_subShaderModule.DepthOphelper.ReadZTestFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.DepthData.ValidZTest )
					{
						m_subShaderModule.DepthOphelper.ReadZTestFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

					if( m_templateMultiPass.SubShaders[ 0 ].Modules.DepthData.ValidOffset )
					{
						m_subShaderModule.DepthOphelper.ReadOffsetFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.DepthData.ValidOffset )
					{
						m_passModule.DepthOphelper.ReadOffsetFromString( ref m_currentReadParamIdx , ref nodeParams );
					}

				}

				//TAGS
				if( UIUtils.CurrentShaderVersion() > 14301 )
				{
					if( m_templateMultiPass.SubShaders[ 0 ].Modules.TagData.DataCheck == TemplateDataCheck.Valid )
					{
						m_subShaderModule.TagsHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
					else if( m_templateMultiPass.SubShaders[ 0 ].Passes[ 0 ].Modules.TagData.DataCheck == TemplateDataCheck.Valid )
					{
						m_passModule.TagsHelper.ReadFromString( ref m_currentReadParamIdx , ref nodeParams );
					}
				}

				SamplingMacros = false;
			}
			catch( Exception e )
			{
				Debug.LogException( e , this );
			}
			m_containerGraph.CurrentCanvasMode = NodeAvailability.TemplateShader;
		}

		public void ForceOptionsRefresh()
		{
			m_passOptions.Refresh();
			if( m_isMainOutputNode )
				m_subShaderOptions.Refresh();
		}

		public void SetPassVisible( string passName , bool visible )
		{
			TemplateMultiPassMasterNode node = m_containerGraph.GetMasterNodeOfPass( passName , m_lodIndex );
			if( node != null )
			{
				m_passSelector.SetPassVisible( passName , visible );
				node.IsInvisible = !visible;
			}

		}

		public override void RefreshExternalReferences()
		{
			if( m_invalidNode )
				return;

			base.RefreshExternalReferences();
			if( IsLODMainMasterNode )
			{
				SetMasterNodeCategoryFromGUID( m_templateGUID );
			}

			CheckTemplateChanges();
			if( m_templateMultiPass != null && m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].Modules.SRPIsPBRHD && UIUtils.CurrentShaderVersion() < 15410 )
			{
				FetchHDPorts();
				m_hdSrpMaterialType = ( m_specularPort != null && m_specularPort.HasOwnOrLinkConnection ) ? HDSRPMaterialType.Specular : HDSRPMaterialType.Standard;
				ConfigHDPorts();
			}

			if( ContainerGraph.HasLODs )
			{
				SetClippedAdditionalTitle( string.Format( LodSubtitle , ShaderLOD ) );
			}

			if( m_isMainOutputNode )
			{
				List<TemplateMultiPassMasterNode> masterNodes = ( m_lodIndex == -1 ) ? m_containerGraph.MultiPassMasterNodes.NodesList : m_containerGraph.LodMultiPassMasternodes[ m_lodIndex ].NodesList;
				masterNodes.Sort( ( x , y ) => ( x.PassIdx.CompareTo( y.PassIdx ) ) );
				int passAmount = m_templateMultiPass.SubShaders[ m_subShaderIdx ].PassAmount;
				if( passAmount != masterNodes.Count )
				{
					UIUtils.ShowMessage( "Template master nodes amount was modified. Could not set correctly its visibility options." );
				}
				else
				{
					for( int i = 0 ; i < passAmount ; i++ )
					{
						if( i != m_passIdx )
						{
							masterNodes[ i ].IsInvisible = !m_passSelector.IsVisible( i );
						}
					}
				}
			}
		}

		public override void ReadInputDataFromString( ref string[] nodeParams )
		{
			//For a Template Master Node an input port data must be set by its template and not meta data
			if( UIUtils.CurrentShaderVersion() > 17007 )
				return;

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

			for( int i = 0 ; i < count && i < nodeParams.Length && m_currentReadParamIdx < nodeParams.Length ; i++ )
			{
				if( UIUtils.CurrentShaderVersion() < 5003 )
				{
					int newId = VersionConvertInputPortId( i );
					if( UIUtils.CurrentShaderVersion() > 23 )
					{
						m_currentReadParamIdx++;
					}

					m_currentReadParamIdx++;
					if( m_inputPorts[ newId ].IsEditable && UIUtils.CurrentShaderVersion() >= 3100 && m_currentReadParamIdx < nodeParams.Length )
					{
						m_currentReadParamIdx++;
					}
				}
				else
				{
					m_currentReadParamIdx++;
					m_currentReadParamIdx++;
					m_currentReadParamIdx++;
					bool isEditable = Convert.ToBoolean( nodeParams[ m_currentReadParamIdx++ ] );
					if( isEditable && m_currentReadParamIdx < nodeParams.Length )
					{
						m_currentReadParamIdx++;
					}
				}
			}
		}

		//For a Template Master Node an input port data must be set by its template and not meta data
		public override void WriteInputDataToString( ref string nodeInfo ) { }

		public override float HeightEstimate
		{
			get
			{
				float heightEstimate = 0;
				heightEstimate = 32 + Constants.INPUT_PORT_DELTA_Y;
				if( m_templateMultiPass != null && !m_templateMultiPass.IsSinglePass )
				{
					heightEstimate += 22;
				}
				float internalPortSize = 0;
				for( int i = 0 ; i < InputPorts.Count ; i++ )
				{
					if( InputPorts[ i ].Visible )
						internalPortSize += 18 + Constants.INPUT_PORT_DELTA_Y;
				}

				return heightEstimate + Mathf.Max( internalPortSize , m_insideSize.y );
			}
		}

		public HDSRPMaterialType CurrentHDMaterialType
		{
			get { return m_hdSrpMaterialType; }
			set
			{
				m_hdSrpMaterialType = value;
				if( m_isMainOutputNode )
				{
					List<TemplateMultiPassMasterNode> mpNodes = ContainerGraph.MultiPassMasterNodes.NodesList;
					int count = mpNodes.Count;
					for( int i = 0 ; i < count ; i++ )
					{
						if( mpNodes[ i ].UniqueId != UniqueId )
						{
							mpNodes[ i ].CurrentHDMaterialType = value;
						}
					}
				}
			}
		}
		public TemplateSubShader SubShader { get { return m_templateMultiPass.SubShaders[ m_subShaderIdx ]; } }
		public TemplatePass Pass { get { return m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ]; } }
		public int SubShaderIdx { get { return m_subShaderIdx; } }
		public int PassIdx { get { return m_passIdx; } }
		public TemplateMultiPass CurrentTemplate { get { return m_templateMultiPass; } }
		public TemplateModulesHelper SubShaderModule { get { return m_subShaderModule; } }
		public TemplateModulesHelper PassModule { get { return m_passModule; } }
		public string PassName { get { return m_templateMultiPass.SubShaders[ m_subShaderIdx ].Passes[ m_passIdx ].PassNameContainer.Data; } }
		public string PassUniqueName
		{
			get
			{
				return string.IsNullOrEmpty( m_passUniqueId ) ? m_originalPassName : m_passUniqueId;
			}
		}

		public string OriginalPassName { get { return m_originalPassName; } }
		public bool HasLinkPorts { get { return m_hasLinkPorts; } }
		public bool IsInvisible
		{
			get
			{
				return m_isInvisible != InvisibilityStatus.Visible;
			}
			set
			{
				if( m_isInvisible != InvisibilityStatus.LockedInvisible && !m_isMainOutputNode )
				{
					m_isInvisible = value ? InvisibilityStatus.Invisible : InvisibilityStatus.Visible;
					if( value )
					{
						for( int i = 0 ; i < m_inputPorts.Count ; i++ )
						{
							m_inputPorts[ i ].FullDeleteConnections();
						}
					}
				}
			}
		}

		public TemplatePassSelectorHelper PassSelector { get { return m_passSelector; } }
		public TemplateOptionsUIHelper PassOptions { get { return m_passOptions; } }
		public TemplateOptionsUIHelper SubShaderOptions { get { return m_subShaderOptions; } }
		public TemplateOptionsDefinesContainer OptionsDefineContainer { get { return m_optionsDefineContainer; } }
		public TerrainDrawInstancedHelper DrawInstancedHelperInstance { get { return m_drawInstancedHelper; } }
		public bool InvalidNode { get { return m_invalidNode; } }
		public override void SetName( string name )
		{
			ShaderName = name;
		}
		public bool IsLODMainFirstPass { get { return m_passIdx == 0 && m_lodIndex == -1; } }
		public override AvailableShaderTypes CurrentMasterNodeCategory { get { return AvailableShaderTypes.Template; } }
	}
}
