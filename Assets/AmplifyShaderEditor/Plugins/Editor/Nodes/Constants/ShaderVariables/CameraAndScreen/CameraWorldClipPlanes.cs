// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	public enum BuiltInShaderClipPlanesTypes
	{
		Left = 0,
		Right,
		Bottom,
		Top,
		Near,
		Far
	}

	[Serializable]
	[NodeAttributes( "Clip Planes", "Camera And Screen", "Camera World Clip Planes" )]
	public sealed class CameraWorldClipPlanes : ShaderVariablesNode
	{
		[SerializeField]
		private BuiltInShaderClipPlanesTypes m_selectedType = BuiltInShaderClipPlanesTypes.Left;
		
		private const string LabelStr = "Plane";

		private const string ValueStr = "unity_CameraWorldClipPlanes";//L,R,B,T,N,F

		private const string ValueHDRPStr = "_FrustumPlanes";// L,R,T,B,N,F

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();
		private int m_planeId;
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "ABCD", WirePortDataType.FLOAT4 );
			m_textLabelWidth = 55;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_selectedType ) );
			m_previewShaderGUID = "6afe5a4ad7bbd0e4ab352c758f543a09";
		}

		public override void OnEnable()
		{
			base.OnEnable();
			m_planeId = Shader.PropertyToID( "_PlaneId" );
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

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_upperLeftWidget.DrawWidget<BuiltInShaderClipPlanesTypes>(ref m_selectedType, this, OnWidgetUpdate );
		}

		private readonly Action<ParentNode> OnWidgetUpdate = ( x ) => {
			x.SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, ( x as CameraWorldClipPlanes ).Type ) );
		};

		public BuiltInShaderClipPlanesTypes Type { get { return m_selectedType; } }

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_selectedType = ( BuiltInShaderClipPlanesTypes ) EditorGUILayoutEnumPopup( LabelStr, m_selectedType );
			if ( EditorGUI.EndChangeCheck() )
			{
				SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_selectedType ) );
				SetSaveIsDirty();
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			PreviewMaterial.SetInt( m_planeId, (int)m_selectedType );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			if( dataCollector.TemplateDataCollectorInstance.IsHDRP )
			{
				switch( m_selectedType )
				{
					case BuiltInShaderClipPlanesTypes.Left: return ValueHDRPStr + "[0]";
					case BuiltInShaderClipPlanesTypes.Right: return ValueHDRPStr + "[1]";
					case BuiltInShaderClipPlanesTypes.Bottom: return ValueHDRPStr + "[3]";
					case BuiltInShaderClipPlanesTypes.Top: return ValueHDRPStr + "[2]";
					case BuiltInShaderClipPlanesTypes.Near: return ValueHDRPStr + "[4]";
					case BuiltInShaderClipPlanesTypes.Far: return ValueHDRPStr + "[5]";
				}
			}
			return ValueStr + "[" + ( int ) m_selectedType + "]";
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_selectedType = ( BuiltInShaderClipPlanesTypes ) Enum.Parse( typeof( BuiltInShaderClipPlanesTypes ), GetCurrentParam( ref nodeParams ) );
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_selectedType ) );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedType );
		}
	}
}
