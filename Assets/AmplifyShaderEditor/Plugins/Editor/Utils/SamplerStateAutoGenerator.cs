// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	public enum InlineSamplerFilteringMode
	{
		Point,
		Linear,
		Trilinear
	};

	public enum InlineSamplerWrapMode
	{
		Clamp,
		Repeat,
		Mirror,
		MirrorOnce
	};

	public enum InlineSamplerWrapCoordinates
	{
		All,
		U,
		V,
		W
	};

	[Serializable]
	public class InlineSamplerWrapOptions
	{
		public InlineSamplerWrapMode WrapMode = InlineSamplerWrapMode.Clamp;
		public InlineSamplerWrapCoordinates Coordinates = InlineSamplerWrapCoordinates.All;
		public string InlineValue
		{
			get
			{
				string name = "_"+WrapMode.ToString();
				if( Coordinates != InlineSamplerWrapCoordinates.All )
					name += Coordinates.ToString();
				name += "_";
				return name;
			}
		}
	}

	[Serializable]
	public class SamplerStateAutoGenerator
	{
		private const int MaxCount = 3;
		private const float ButtonLayoutWidth = 15;
		private const string AdditionalWrapsStr = "Additional Wraps";
		private const string InlineSamplerStateStr = "Inline Sampler State";

		[SerializeField]
		private InlineSamplerFilteringMode m_filterMode = InlineSamplerFilteringMode.Point;

		[SerializeField]
		private InlineSamplerWrapOptions m_mainWrapMode = new InlineSamplerWrapOptions();

		[SerializeField]
		private List<InlineSamplerWrapOptions> m_additionalWrapOptions = new List<InlineSamplerWrapOptions>();

		[SerializeField]
		private bool m_visibleWrapsFoldout = false;

		[SerializeField]
		private bool m_visibleMainFoldout = false;

		[NonSerialized]
		private UndoParentNode m_owner;

		public void Destroy()
		{
			m_mainWrapMode = null;
			m_additionalWrapOptions.Clear();
			m_additionalWrapOptions = null;
		}

		public string AddToDataCollector( ref MasterNodeDataCollector dataCollector )
		{
			string inlineSampler = "sampler_";

			inlineSampler += m_filterMode.ToString();
			inlineSampler += m_mainWrapMode.InlineValue;

			int count = m_additionalWrapOptions.Count;
			for( int i = 0; i < count; i++ )
			{
				inlineSampler += m_additionalWrapOptions[ i ].InlineValue;
			}
			return inlineSampler;
		}

		void DrawAddRemoveButtons()
		{	
			int count = m_additionalWrapOptions.Count;
			if( count < MaxCount && m_owner.GUILayoutButton( string.Empty, UIUtils.PlusStyle, GUILayout.Width( ButtonLayoutWidth ) ) )
			{
				m_additionalWrapOptions.Add( new InlineSamplerWrapOptions() );
				EditorGUI.FocusTextInControl( null );
			}

			if( count > 0 && m_owner.GUILayoutButton( string.Empty, UIUtils.MinusStyle, GUILayout.Width( ButtonLayoutWidth ) ) )
			{
				m_additionalWrapOptions.RemoveAt( count - 1 );
				EditorGUI.FocusTextInControl( null );
			}
		}

		public void Draw( UndoParentNode owner )
		{
			m_owner = owner;
			NodeUtils.DrawNestedPropertyGroup( ref m_visibleMainFoldout, InlineSamplerStateStr, DrawMain );
		}

		void DrawMain()
		{
			m_filterMode = (InlineSamplerFilteringMode)m_owner.EditorGUILayoutEnumPopup( m_filterMode );

			EditorGUILayout.BeginHorizontal();
			m_mainWrapMode.WrapMode = (InlineSamplerWrapMode)m_owner.EditorGUILayoutEnumPopup( m_mainWrapMode.WrapMode );
			m_mainWrapMode.Coordinates = (InlineSamplerWrapCoordinates)m_owner.EditorGUILayoutEnumPopup( m_mainWrapMode.Coordinates );
			EditorGUILayout.EndHorizontal();
			NodeUtils.DrawNestedPropertyGroup( ref m_visibleWrapsFoldout, AdditionalWrapsStr, DrawAdditionalWrapModes, DrawAddRemoveButtons );
		}

		void DrawAdditionalWrapModes()
		{
			EditorGUILayout.Space();
			int count = m_additionalWrapOptions.Count;
			for( int i = 0; i < count; i++ )
			{
				float maxWidth = 90;
				EditorGUILayout.BeginHorizontal();
				m_additionalWrapOptions[ i ].WrapMode = (InlineSamplerWrapMode)m_owner.EditorGUILayoutEnumPopup( m_additionalWrapOptions[ i ].WrapMode ,GUILayout.MaxWidth( maxWidth ) );
				m_additionalWrapOptions[ i ].Coordinates = (InlineSamplerWrapCoordinates)m_owner.EditorGUILayoutEnumPopup( m_additionalWrapOptions[ i ].Coordinates, GUILayout.MaxWidth( maxWidth ) );
				EditorGUILayout.EndHorizontal();
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			Enum.TryParse<InlineSamplerFilteringMode>( nodeParams[ index++ ], out m_filterMode );
			Enum.TryParse<InlineSamplerWrapCoordinates>( nodeParams[ index++ ], out m_mainWrapMode.Coordinates );

			int count = 0;
			int.TryParse( nodeParams[ index++ ], out count );
			for( int i = 0; i < count; i++ )
			{
				InlineSamplerWrapOptions option = new InlineSamplerWrapOptions();

				Enum.TryParse<InlineSamplerWrapMode>( nodeParams[ index++ ], out option.WrapMode );
				Enum.TryParse<InlineSamplerWrapCoordinates>( nodeParams[ index++ ], out option.Coordinates );

				m_additionalWrapOptions.Add( option );
			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_filterMode );

			IOUtils.AddFieldValueToString( ref nodeInfo, m_mainWrapMode.WrapMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_mainWrapMode.Coordinates );

			int count = m_additionalWrapOptions.Count;
			IOUtils.AddFieldValueToString( ref nodeInfo, count );
			if( count > 0 )
			{
				for( int i = 0; i < count; i++ )
				{
					IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalWrapOptions[i].WrapMode );
					IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalWrapOptions[i].Coordinates );
				}
			}

		}
	}
}
