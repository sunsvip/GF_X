// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Linq;

namespace AmplifyShaderEditor
{
	public enum AdditionalLineType
	{
		Include,
		Define,
		Pragma,
		Custom
	}

	public enum AdditionalContainerOrigin
	{
		Native,
		ShaderFunction,
		Custom
	}


	[Serializable]
	public class AdditionalDirectiveContainerSaveItem
	{
		public AdditionalLineType LineType = AdditionalLineType.Include;
		public string LineValue = string.Empty;
		public bool GUIDToggle = false;
		public string GUIDValue = string.Empty;
		public bool ShowConditionals = false;
		public int VersionMin = 0;
		public int VersionMax = 0;
		public string Passes = string.Empty;
		public AdditionalContainerOrigin Origin = AdditionalContainerOrigin.Custom;

		public AdditionalDirectiveContainerSaveItem( AdditionalDirectiveContainer container )
		{
			LineType = container.LineType;
			LineValue = container.LineValue;
			GUIDToggle = container.GUIDToggle;
			GUIDValue = container.GUIDValue;
			ShowConditionals = container.ShowConditionals;
			VersionMin = container.VersionMin;
			VersionMax = container.VersionMax;
			Passes = container.Passes;
			Origin = container.Origin;
		}
	}

	[Serializable]
	public class AdditionalDirectiveContainer : ScriptableObject
	{
		public AdditionalLineType LineType = AdditionalLineType.Include;
		public string LineValue = string.Empty;
		public bool GUIDToggle = false;
		public string GUIDValue = string.Empty;
		public bool ShowConditionals = false;
		public int VersionMin = 0;
		public int VersionMax = 0;
		public string Passes = string.Empty;
		public AdditionalContainerOrigin Origin = AdditionalContainerOrigin.Custom;
		public TextAsset LibObject = null;
		public string OwnerId = string.Empty;

		public void Init( string ownerId, AdditionalDirectiveContainer item )
		{
			LineType = item.LineType;
			LineValue = item.LineValue;
			GUIDToggle = item.GUIDToggle;
			GUIDValue = item.GUIDValue;
			ShowConditionals = item.ShowConditionals;
			VersionMin = item.VersionMin;
			VersionMax = item.VersionMax;
			Passes = item.Passes;
			Origin = item.Origin;
			LibObject = item.LibObject;
			OwnerId = ownerId;
		}

		public void Init( AdditionalDirectiveContainerSaveItem item )
		{
			LineType = item.LineType;
			LineValue = item.LineValue;
			GUIDToggle = item.GUIDToggle;
			GUIDValue = item.GUIDValue;
			ShowConditionals = item.ShowConditionals;
			VersionMin = item.VersionMin;
			VersionMax = item.VersionMax;
			Passes = item.Passes;
			Origin = item.Origin;
			if( GUIDToggle )
			{
				LibObject = AssetDatabase.LoadAssetAtPath<TextAsset>( AssetDatabase.GUIDToAssetPath( GUIDValue ) );
			}
		}

		public void OnDestroy()
		{
			//Debug.Log( "Destoying directives" );
			LibObject = null;
		}

		public string Value
		{
			get
			{
				switch( LineType )
				{
					case AdditionalLineType.Include:
					{
						if( GUIDToggle )
						{
							string shaderPath = AssetDatabase.GUIDToAssetPath( GUIDValue );
							if( !string.IsNullOrEmpty( shaderPath ) )
								return shaderPath;
						}
						return LineValue;
					}
					case AdditionalLineType.Define: return LineValue;
					case AdditionalLineType.Pragma: return LineValue;
				}
				return LineValue;
			}
		}

		public string FormattedValue
		{
			get
			{
				switch( LineType )
				{
					case AdditionalLineType.Include:
					{
						if( GUIDToggle )
						{
							string shaderPath = AssetDatabase.GUIDToAssetPath( GUIDValue );
							if( !string.IsNullOrEmpty( shaderPath ) )
								return string.Format( Constants.IncludeFormat, shaderPath );
						}

						return string.Format( Constants.IncludeFormat, LineValue );
					}
					case AdditionalLineType.Define:
					return string.Format( Constants.DefineFormat, LineValue );
					case AdditionalLineType.Pragma:
					return string.Format( Constants.PragmaFormat, LineValue );
				}
				return LineValue;
			}
		}
	}



	public enum ReordableAction
	{
		None,
		Add,
		Remove
	}

	[Serializable]
	public sealed class TemplateAdditionalDirectivesHelper : TemplateModuleParent
	{
		private string NativeFoldoutStr = "Native";

		[SerializeField]
		private List<AdditionalDirectiveContainer> m_additionalDirectives = new List<AdditionalDirectiveContainer>();

		[SerializeField]
		private List<AdditionalDirectiveContainer> m_shaderFunctionDirectives = new List<AdditionalDirectiveContainer>();

		[SerializeField]
		private List<string> m_nativeDirectives = new List<string>();

		[SerializeField]
		private int m_nativeDirectivesIndex = -1;

		[SerializeField]
		private bool m_nativeDirectivesFoldout = false;

		//ONLY USED BY SHADER FUNCTIONS
		// Since AdditionalDirectiveContainer must be a ScriptableObject because of serialization shenanigans it will not serialize the info correctly into the shader function when saving it into a file ( it only saves the id )
		// For it to properly work, each AdditionalDirectiveContainer should be added to the SF asset, but that would make it to have children ( which are seen on the project inspector )
		// Must revisit this later on and come up with a proper solution
		[SerializeField]
		private List<AdditionalDirectiveContainerSaveItem> m_directivesSaveItems = new List<AdditionalDirectiveContainerSaveItem>();


		private ReordableAction m_actionType = ReordableAction.None;
		private int m_actionIndex = 0;
		private ReorderableList m_reordableList = null;
		private GUIStyle m_propertyAdjustment;
		private UndoParentNode m_currOwner;
		private Rect m_nativeRect = Rect.zero;

		public TemplateAdditionalDirectivesHelper( string moduleName ) : base( moduleName ) { }

		//public void AddShaderFunctionItem( AdditionalLineType type, string item )
		//{
		//	UpdateShaderFunctionDictionary();
		//	string id = type + item;
		//	if( !m_shaderFunctionDictionary.ContainsKey( id ) )
		//	{
		//		AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
		//		newItem.LineType = type;
		//		newItem.LineValue = item;
		//		newItem.hideFlags = HideFlags.HideAndDontSave;
		//		m_shaderFunctionDirectives.Add( newItem );
		//		m_shaderFunctionDictionary.Add( id, newItem );
		//	}
		//}

		public void AddShaderFunctionItems( string ownerOutputId, List<AdditionalDirectiveContainer> functionList )
		{
			RemoveShaderFunctionItems( ownerOutputId );
			if( functionList.Count > 0 )
			{
				for( int i = 0; i < functionList.Count; i++ )
				{
					AdditionalDirectiveContainer item = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
					item.Init( ownerOutputId, functionList[ i ] );
					m_shaderFunctionDirectives.Add( item );
				}
			}
			//if( functionList.Count > 0 )
			//{

			//	m_shaderFunctionDirectives.AddRange( functionList );
			//}
		}

		public void RemoveShaderFunctionItems( string ownerOutputId/*, List<AdditionalDirectiveContainer> functionList */)
		{
			List<AdditionalDirectiveContainer>  list = m_shaderFunctionDirectives.FindAll( ( x ) => x.OwnerId.Equals( ownerOutputId ));
			for( int i = 0; i < list.Count; i++ )
			{
				m_shaderFunctionDirectives.Remove( list[ i ] );
				ScriptableObject.DestroyImmediate( list[ i ] );
			}
			list.Clear();
			list = null;

			//for( int i = 0; i < functionList.Count; i++ )
			//{
			//	m_shaderFunctionDirectives.Remove( functionList[ i ] );
			//}
		}

		//public void RemoveShaderFunctionItem( AdditionalLineType type, string item )
		//{
		//	m_shaderFunctionDirectives.RemoveAll( x => x.LineType == type && x.LineValue.Equals( item ) );
		//}

		public void AddItems( AdditionalLineType type, List<string> items )
		{
			int count = items.Count;
			for( int i = 0; i < count; i++ )
			{
				AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
				newItem.LineType = type;
				newItem.LineValue = items[ i ];
				newItem.hideFlags = HideFlags.HideAndDontSave;
				m_additionalDirectives.Add( newItem );
			}
			UpdateNativeIndex();
		}

		public void AddNativeContainer()
		{
			if( m_nativeDirectives.Count > 0 )
			{
				if( m_additionalDirectives.FindIndex( x => x.Origin.Equals( AdditionalContainerOrigin.Native ) ) == -1 )
				{
					AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
					newItem.Origin = AdditionalContainerOrigin.Native;
					newItem.hideFlags = HideFlags.HideAndDontSave;
					//m_additionalDirectives.Add( newItem );
					//m_nativeDirectivesIndex = m_additionalDirectives.Count - 1;
					m_additionalDirectives.Insert( 0, newItem );
					m_nativeDirectivesIndex = 0;
				}
			}
		}

		public void FillNativeItems( List<string> nativeItems )
		{
			m_nativeDirectives.Clear();
			m_nativeDirectives.AddRange( nativeItems );
			AddNativeContainer();
		}

		void DrawNativeItems()
		{
			EditorGUILayout.Separator();
			EditorGUI.indentLevel++;
			int count = m_nativeDirectives.Count;
			for( int i = 0; i < count; i++ )
			{
				EditorGUILayout.LabelField( m_nativeDirectives[ i ] );
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Separator();
		}

		void DrawNativeItemsRect()
		{
			int count = m_nativeDirectives.Count;
			m_nativeRect.y += EditorGUIUtility.singleLineHeight;
			for( int i = 0; i < count; i++ )
			{
				EditorGUI.LabelField( m_nativeRect, m_nativeDirectives[ i ] );
				m_nativeRect.y += EditorGUIUtility.singleLineHeight;
			}
		}

		void DrawButtons()
		{
			EditorGUILayout.Separator();

			// Add keyword
			if( GUILayout.Button( string.Empty, UIUtils.PlusStyle, GUILayout.Width( Constants.PlusMinusButtonLayoutWidth ) ) )
			{
				AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
				newItem.hideFlags = HideFlags.HideAndDontSave;
				m_additionalDirectives.Add( newItem );
				UpdateNativeIndex();
				EditorGUI.FocusTextInControl( null );
				m_isDirty = true;
			}

			//Remove keyword
			if( GUILayout.Button( string.Empty, UIUtils.MinusStyle, GUILayout.Width( Constants.PlusMinusButtonLayoutWidth ) ) )
			{
				if( m_additionalDirectives.Count > 0 )
				{
					AdditionalDirectiveContainer itemToDelete = m_additionalDirectives[ m_additionalDirectives.Count - 1 ];
					m_additionalDirectives.RemoveAt( m_additionalDirectives.Count - 1 );
					ScriptableObject.DestroyImmediate( itemToDelete );
					EditorGUI.FocusTextInControl( null );
				}
				m_isDirty = true;
			}
		}

		void DrawConditionals( Rect rect, AdditionalDirectiveContainer directive )
		{
			var tog = rect;
			tog.width = 15;
			tog.x += 5;
			tog.y -= ( tog.height - ( EditorGUIUtility.singleLineHeight + 5 ) ) * 0.5f + 1;
			tog.xMin += 2;
			tog.yMin -= 2;
			bool value = GUI.Toggle( tog, directive.ShowConditionals, "", UIUtils.MenuItemToggleStyle );
			if ( Event.current.button == Constants.FoldoutMouseId )
			{
				directive.ShowConditionals = value;
			}
			if ( directive.ShowConditionals )
			{
				const int labelWidth = 70;
				const int versionEditWidth = 55;
				Rect condPos = rect;
				condPos.height = EditorGUIUtility.singleLineHeight;

				// List of passes to apply directive
				directive.Passes = ( directive.Passes != null ) ? directive.Passes : string.Empty;
				condPos.x = rect.x + 23;
				condPos.y += EditorGUIUtility.singleLineHeight + 2;
				condPos.width = labelWidth;
				EditorGUI.LabelField( condPos, new GUIContent( "Passes", "Template pass names separated by semicolon (;). Empty means it will be included in all passes." ) );
				condPos.x += labelWidth;
				condPos.xMax = rect.xMax + 1;
				directive.Passes = m_currOwner.EditorGUITextField( condPos, string.Empty, directive.Passes );

				// Range of SRP versions to apply directive
				condPos.x = rect.x + 23;
				condPos.y += EditorGUIUtility.singleLineHeight + 2;
				condPos.width = labelWidth;
				EditorGUI.LabelField( condPos, new GUIContent( "SRPVersion", "Valid SRP version numbers must have 6 digits and be equal or higher than 100000, the lowest supported version." ) );
				condPos.x += labelWidth;

				condPos.width = versionEditWidth;
				string minText = ( directive.VersionMin == 0 ) ? string.Empty : directive.VersionMin.ToString();
				minText = m_currOwner.EditorGUITextField( condPos, string.Empty, minText );
				directive.VersionMin = int.TryParse( minText, out int min ) ? min : 0;
				condPos.x += versionEditWidth + 5;

				EditorGUI.LabelField( condPos, "to" );
				condPos.x += 20;

				string maxText = ( directive.VersionMax == 0 ) ? string.Empty : directive.VersionMax.ToString();
				maxText = m_currOwner.EditorGUITextField( condPos, string.Empty, maxText );
				directive.VersionMax = int.TryParse( maxText, out int max ) ? max : 0;
				condPos.x = rect.x + 40;
			}
		}

		public override void Draw( UndoParentNode currOwner, bool style = true )
		{
			m_currOwner = currOwner;
			if( m_reordableList == null )
			{
				m_reordableList = new ReorderableList( m_additionalDirectives, typeof( AdditionalDirectiveContainer ), true, false, false, false )
				{
					headerHeight = 0,
					footerHeight = 0,
					showDefaultBackground = false,
					elementHeightCallback = ( index ) =>
					{
						if( m_additionalDirectives[ index ].Origin == AdditionalContainerOrigin.Native && m_nativeDirectivesFoldout )
						{
							return ( m_nativeDirectives.Count + 1 ) * ( EditorGUIUtility.singleLineHeight ) + 5;
						}
						int lineCount = ( m_additionalDirectives[ index ].ShowConditionals ) ? 3 : 1;
						return EditorGUIUtility.singleLineHeight * lineCount + 5;
					},
					drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
					{
						if( m_additionalDirectives[ index ].Origin == AdditionalContainerOrigin.Native && m_nativeDirectivesFoldout )
						{
							rect.height = ( m_nativeDirectives.Count + 1 ) * ( EditorGUIUtility.singleLineHeight ) + 5;
						}

						if( m_additionalDirectives[ index ] != null )
						{
							float labelWidthStyleAdjust = 0;

							if ( m_additionalDirectives[ index ].Origin == AdditionalContainerOrigin.Native )
							{
								rect.xMin -= 5;
								rect.xMax += 1;

								m_nativeRect = rect;
								m_nativeRect.y -= ( m_nativeRect.height - ( EditorGUIUtility.singleLineHeight + 5 ) ) * 0.5f;
								m_nativeRect.xMin += 2;
								m_nativeRect.xMax -= 2;
								m_nativeRect.yMax -= 2;

								NodeUtils.DrawNestedPropertyGroup( ref m_nativeDirectivesFoldout, rect, NativeFoldoutStr, DrawNativeItemsRect, 4 );
								return;
							}
							else
							{
								rect.xMin -= 10;
								labelWidthStyleAdjust = 15;

								DrawConditionals( rect, m_additionalDirectives[ index ] );
							}

							const int conditionalsOffset = 20;
							rect.x += conditionalsOffset;

							float popUpWidth = 88;
							float widthAdjust = m_additionalDirectives[ index ].LineType == AdditionalLineType.Include ? -14 : 0;
							widthAdjust -= conditionalsOffset;
							Rect popupPos = new Rect( rect.x, rect.y, popUpWidth, EditorGUIUtility.singleLineHeight );
							Rect GUIDTogglePos = m_additionalDirectives[ index ].LineType == AdditionalLineType.Include ? new Rect( rect.x + rect.width - 3 * Constants.PlusMinusButtonLayoutWidth - conditionalsOffset + 3, rect.y, Constants.PlusMinusButtonLayoutWidth, Constants.PlusMinusButtonLayoutWidth ) : new Rect();
							Rect buttonPlusPos = new Rect( rect.x + rect.width - 2 * Constants.PlusMinusButtonLayoutWidth - conditionalsOffset + 1, rect.y - 2, Constants.PlusMinusButtonLayoutWidth, Constants.PlusMinusButtonLayoutWidth );
							Rect buttonMinusPos = new Rect( rect.x + rect.width - Constants.PlusMinusButtonLayoutWidth - conditionalsOffset + 1, rect.y - 2, Constants.PlusMinusButtonLayoutWidth, Constants.PlusMinusButtonLayoutWidth );
							float labelWidthBuffer = EditorGUIUtility.labelWidth;
							Rect labelPos = new Rect( rect.x + popupPos.width - labelWidthStyleAdjust, rect.y, labelWidthStyleAdjust + rect.width - popupPos.width - buttonPlusPos.width - buttonMinusPos.width + widthAdjust, EditorGUIUtility.singleLineHeight );



							m_additionalDirectives[ index ].LineType = (AdditionalLineType)m_currOwner.EditorGUIEnumPopup( popupPos, m_additionalDirectives[ index ].LineType );

							if( m_additionalDirectives[ index ].LineType == AdditionalLineType.Include )
							{
								if( m_additionalDirectives[ index ].GUIDToggle )
								{
									//if( m_additionalDirectives[ index ].LibObject == null && !string.IsNullOrEmpty( m_additionalDirectives[ index ].GUIDValue ) )
									//{
									//	m_additionalDirectives[ index ].LibObject = AssetDatabase.LoadAssetAtPath<TextAsset>( AssetDatabase.GUIDToAssetPath( m_additionalDirectives[ index ].GUIDValue ) );
									//}

									EditorGUI.BeginChangeCheck();
									TextAsset obj = m_currOwner.EditorGUIObjectField( labelPos, m_additionalDirectives[ index ].LibObject, typeof( TextAsset ), false ) as TextAsset;
									if( EditorGUI.EndChangeCheck() )
									{
										string pathName = AssetDatabase.GetAssetPath( obj );
										string extension = Path.GetExtension( pathName );
										extension = extension.ToLower();
										if( extension.Equals( ".cginc" ) || extension.Equals( ".hlsl" ) )
										{
											m_additionalDirectives[ index ].LibObject = obj;
											m_additionalDirectives[ index ].GUIDValue = AssetDatabase.AssetPathToGUID( pathName );
										}
									}
								}
								else
								{
									m_additionalDirectives[ index ].LineValue = m_currOwner.EditorGUITextField( labelPos, string.Empty, m_additionalDirectives[ index ].LineValue );
								}

								if( GUI.Button( GUIDTogglePos, m_additionalDirectives[ index ].GUIDToggle ? UIUtils.FloatIntIconOFF : UIUtils.FloatIntIconON, UIUtils.FloatIntPickerONOFF ) )
									m_additionalDirectives[ index ].GUIDToggle = !m_additionalDirectives[ index ].GUIDToggle;
							}
							else
							{
								m_additionalDirectives[ index ].LineValue = m_currOwner.EditorGUITextField( labelPos, string.Empty, m_additionalDirectives[ index ].LineValue );
							}

							//NodeUtils.DrawNestedPropertyGroup( ref m_additionalDirectives[ index ].ShowConditionals, rect, "TEMP", DrawConditionals, 4 );

							if ( GUI.Button( buttonPlusPos, string.Empty, UIUtils.PlusStyle ) )
							{
								m_actionType = ReordableAction.Add;
								m_actionIndex = index;
							}

							if( GUI.Button( buttonMinusPos, string.Empty, UIUtils.MinusStyle ) )
							{
								m_actionType = ReordableAction.Remove;
								m_actionIndex = index;
							}
						}
					},
					onReorderCallback = ( ReorderableList list ) =>
					{
						UpdateNativeIndex();
					}
				};
			}

			if( m_actionType != ReordableAction.None )
			{
				switch( m_actionType )
				{
					case ReordableAction.Add:
					{
						AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
						newItem.hideFlags = HideFlags.HideAndDontSave;
						m_additionalDirectives.Insert( m_actionIndex + 1, newItem );
					}
					break;
					case ReordableAction.Remove:
					AdditionalDirectiveContainer itemToDelete = m_additionalDirectives[ m_actionIndex ];
					m_additionalDirectives.RemoveAt( m_actionIndex );
					ScriptableObject.DestroyImmediate( itemToDelete );
					break;
				}
				m_isDirty = true;
				m_actionType = ReordableAction.None;
				EditorGUI.FocusTextInControl( null );
			}
			bool foldoutValue = currOwner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedAdditionalDirectives;
			if( style )
			{
				NodeUtils.DrawPropertyGroup( ref foldoutValue, m_moduleName, DrawReordableList, DrawButtons );
			}
			else
			{
				NodeUtils.DrawNestedPropertyGroup( ref foldoutValue, m_moduleName, DrawReordableList, DrawButtons );
			}
			currOwner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedAdditionalDirectives = foldoutValue;
		}

		void DrawReordableList()
		{
			if( m_reordableList != null )
			{
				if( m_propertyAdjustment == null )
				{
					m_propertyAdjustment = new GUIStyle();
					m_propertyAdjustment.padding.left = 17;
				}
				//EditorGUILayout.BeginVertical( m_propertyAdjustment );
				EditorGUILayout.Space();
				if( m_nativeDirectives.Count > 0 )
				{
					//NodeUtils.DrawNestedPropertyGroup( ref m_nativeDirectivesFoldout, NativeFoldoutStr, DrawNativeItems, 4 );
				}
				if( m_additionalDirectives.Count == 0 )
				{
					EditorGUILayout.HelpBox( "Your list is Empty!\nUse the plus button to add one.", MessageType.Info );
				}
				else
				{
					m_reordableList.DoLayoutList();
				}
				EditorGUILayout.Space();
				//EditorGUILayout.EndVertical();
			}
		}

		public void AddAllToDataCollector( ref MasterNodeDataCollector dataCollector, TemplatePass pass, TemplateIncludePragmaContainter nativesContainer )
		{
			//List<AdditionalDirectiveContainer> list = m_additionalDirectives;
			//int count = list.FindIndex( x => x.Origin.Equals( AdditionalContainerOrigin.Native ) );
			//for( int i = 0; i < count; i++ )
			//{
			//	switch( list[ i ].LineType )
			//	{
			//		case AdditionalLineType.Include:
			//		{
			//			string value = list[ i ].Value;
			//			if( !string.IsNullOrEmpty( value ) &&
			//			  !nativesContainer.HasInclude( value ) )
			//			{
			//				dataCollector.AddToMisc( list[ i ].FormattedValue );
			//			}
			//		}
			//		break;
			//		case AdditionalLineType.Define:
			//		{
			//			if( !string.IsNullOrEmpty( list[ i ].LineValue ) &&
			//			  !nativesContainer.HasDefine( list[ i ].LineValue ) )
			//			{
			//				dataCollector.AddToMisc( list[ i ].FormattedValue );
			//			}
			//		}
			//		break;
			//		case AdditionalLineType.Pragma:
			//		{
			//			if( !string.IsNullOrEmpty( list[ i ].LineValue ) &&
			//			  !nativesContainer.HasPragma( list[ i ].LineValue ) )
			//			{
			//				dataCollector.AddToMisc( list[ i ].FormattedValue );
			//			}
			//		}
			//		break;
			//		default:
			//		case AdditionalLineType.Custom:
			//		dataCollector.AddToMisc( list[ i ].LineValue );
			//		break;
			//	}
			//}

			AddToDataCollector( ref dataCollector, pass, nativesContainer, false );
			AddToDataCollector( ref dataCollector, pass, nativesContainer, true );
		}

		public void AddAllToDataCollector( ref MasterNodeDataCollector dataCollector )
		{
			AddToDataCollector( ref dataCollector, null, false );
			AddToDataCollector( ref dataCollector, null, true );
		}

		bool TestConditionals( TemplatePass pass, AdditionalDirectiveContainer directive )
		{
			bool isSRP = ASEPackageManagerHelper.CurrentSRPVersion > 0;
			if ( isSRP && directive.VersionMin != 0 && ASEPackageManagerHelper.CurrentSRPVersion < directive.VersionMin )
			{
				return false;
			}

			if ( isSRP && directive.VersionMax != 0 && ASEPackageManagerHelper.CurrentSRPVersion > directive.VersionMax )
			{
				return false;
			}

			if ( pass != null && !string.IsNullOrEmpty( directive.Passes ) )
			{
				string[] passes = directive.Passes.Split( ';' );
				if ( !passes.Contains( pass.PassNameContainer.Data ) )
				{
					return false;
				}
			}

			return true;
		}

		void AddToDataCollector( ref MasterNodeDataCollector dataCollector, TemplatePass pass, TemplateIncludePragmaContainter nativesContainer, bool fromSF )
		{
			List<AdditionalDirectiveContainer> list = fromSF ? m_shaderFunctionDirectives : m_additionalDirectives;
			int count = list.Count;
			for( int i = 0; i < count; i++ )
			{
				AdditionalDirectiveContainer directive = list[ i ];
				int orderIdx = fromSF ? 1 : ( i > m_nativeDirectivesIndex ? 1 : -1 );

				if ( !TestConditionals( pass, directive ) )
				{
					continue;
				}

				switch( directive.LineType )
				{
					case AdditionalLineType.Include:
					{
						string value = directive.Value;
						if( !string.IsNullOrEmpty( value ) &&
						  !nativesContainer.HasInclude( value ) )
						{
							dataCollector.AddToDirectives( directive.FormattedValue, orderIdx );
						}
					}
					break;
					case AdditionalLineType.Define:
					{
						if( !string.IsNullOrEmpty( directive.LineValue ) &&
						  !nativesContainer.HasDefine( directive.LineValue ) )
						{
							dataCollector.AddToDirectives( directive.FormattedValue, orderIdx );
						}
					}
					break;
					case AdditionalLineType.Pragma:
					{
						if( !string.IsNullOrEmpty( directive.LineValue ) &&
						  !nativesContainer.HasPragma( directive.LineValue ) )
						{
							dataCollector.AddToDirectives( directive.FormattedValue, orderIdx );
						}
					}
					break;
					default:
					case AdditionalLineType.Custom:
					dataCollector.AddToDirectives( directive.LineValue, orderIdx );
						break;
				}
			}
		}

		void AddToDataCollector( ref MasterNodeDataCollector dataCollector, TemplatePass pass, bool fromSF )
		{
			List<AdditionalDirectiveContainer> list = fromSF ? m_shaderFunctionDirectives : m_additionalDirectives;
			int orderIdx = 1;
			int count = list.Count;
			for( int i = 0; i < count; i++ )
			{
				AdditionalDirectiveContainer directive = list[ i ];

				if ( !TestConditionals( pass, directive ) )
				{
					continue;
				}

				switch ( directive.LineType )
				{
					case AdditionalLineType.Include:
					{
						string value = directive.FormattedValue;
						if( !string.IsNullOrEmpty( value ) )
						{
							dataCollector.AddToDirectives( value, orderIdx );
						}
					}
					break;
					case AdditionalLineType.Define:
					{
						if( !string.IsNullOrEmpty( directive.LineValue ) )
						{
							dataCollector.AddToDirectives( directive.FormattedValue, orderIdx );
						}
					}
					break;
					case AdditionalLineType.Pragma:
					{
						if( !string.IsNullOrEmpty( directive.LineValue ) )
						{
							dataCollector.AddToDirectives( directive.FormattedValue, orderIdx );
						}
					}
					break;
					default:
					case AdditionalLineType.Custom:
					dataCollector.AddToDirectives( directive.LineValue, orderIdx );
					break;
				}
			}
		}

		public override void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			try
			{
				m_nativeDirectivesIndex = -1;
				int count = Convert.ToInt32( nodeParams[ index++ ] );
				m_additionalDirectives.Clear();
				for( int i = 0; i < count; i++ )
				{
					AdditionalLineType lineType = (AdditionalLineType)Enum.Parse( typeof( AdditionalLineType ), nodeParams[ index++ ] );
					string lineValue = nodeParams[ index++ ];
					AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
					newItem.hideFlags = HideFlags.HideAndDontSave;
					newItem.LineType = lineType;
					newItem.LineValue = lineValue.Replace( Constants.SemiColonSeparator, ';' );
					if( UIUtils.CurrentShaderVersion() > 15607 )
					{
						newItem.GUIDToggle = Convert.ToBoolean( nodeParams[ index++ ] );
						newItem.GUIDValue = nodeParams[ index++ ];
						if( newItem.GUIDToggle )
						{
							newItem.LibObject = AssetDatabase.LoadAssetAtPath<TextAsset>( AssetDatabase.GUIDToAssetPath( newItem.GUIDValue ) );
							if( newItem.LibObject == null )
							{
								Debug.LogWarning( "Include file not found with GUID " + newItem.GUIDValue );
							}
						}
					}
					AdditionalContainerOrigin origin = AdditionalContainerOrigin.Custom;
					if( UIUtils.CurrentShaderVersion() > 16902 )
					{
						origin = (AdditionalContainerOrigin)Enum.Parse( typeof( AdditionalContainerOrigin ), nodeParams[ index++ ] );
						newItem.Origin = origin;
					}
					if ( UIUtils.CurrentShaderVersion() > 19103 )
					{
						newItem.ShowConditionals = Convert.ToBoolean( nodeParams[ index++ ] );
						newItem.VersionMin = Convert.ToInt32( nodeParams[ index++ ] );
						newItem.VersionMax = Convert.ToInt32( nodeParams[ index++ ] );
						newItem.Passes = nodeParams[ index++ ];
						newItem.Passes.Replace( Constants.SemiColonSeparator, ';' );
					}

					m_additionalDirectives.Add( newItem );

					if( origin == AdditionalContainerOrigin.Native )
					{
						m_nativeDirectivesIndex = i;
					}
				}
				AddNativeContainer();
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
		}

		public override void WriteToString( ref string nodeInfo )
		{
			if( m_additionalDirectives.Count == 1 && m_additionalDirectives[ 0 ].Origin == AdditionalContainerOrigin.Native )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, 0 );
				return;
			}

			IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives.Count );
			for( int i = 0; i < m_additionalDirectives.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].LineType );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].LineValue.Replace( ';', Constants.SemiColonSeparator ) );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].GUIDToggle );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].GUIDValue );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].Origin );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].ShowConditionals );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].VersionMin );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].VersionMax );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_additionalDirectives[ i ].Passes.Replace( ';', Constants.SemiColonSeparator ) );
			}
		}

		// read comment on m_directivesSaveItems declaration
		public void UpdateSaveItemsFromDirectives()
		{
			bool foundNull = false;
			m_directivesSaveItems.Clear();
			for( int i = 0; i < m_additionalDirectives.Count; i++ )
			{
				if( m_additionalDirectives[ i ] != null )
				{
					m_directivesSaveItems.Add( new AdditionalDirectiveContainerSaveItem( m_additionalDirectives[ i ] ) );
				}
				else
				{
					foundNull = true;
				}
			}

			if( foundNull )
			{
				m_additionalDirectives.RemoveAll( item => item == null );
			}
		}

		public void CleanNullDirectives()
		{
			m_additionalDirectives.RemoveAll( item => item == null );
		}

		public void ResetDirectivesOrigin()
		{
			for( int i = 0; i < m_directivesSaveItems.Count; i++ )
			{
				m_directivesSaveItems[ i ].Origin = AdditionalContainerOrigin.Custom;
			}
		}

		// read comment on m_directivesSaveItems declaration
		public void UpdateDirectivesFromSaveItems()
		{
			if( m_directivesSaveItems.Count > 0 )
			{
				for( int i = 0; i < m_additionalDirectives.Count; i++ )
				{
					if( m_additionalDirectives[ i ] != null )
						ScriptableObject.DestroyImmediate( m_additionalDirectives[ i ] );
				}

				m_additionalDirectives.Clear();

				for( int i = 0; i < m_directivesSaveItems.Count; i++ )
				{
					AdditionalDirectiveContainer newItem = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
					newItem.hideFlags = HideFlags.HideAndDontSave;
					newItem.Init( m_directivesSaveItems[ i ] );
					m_additionalDirectives.Add( newItem );
				}
				UpdateNativeIndex();
				//m_directivesSaveItems.Clear();
			}
		}

		void UpdateNativeIndex()
		{
			m_nativeDirectivesIndex = -1;
			int count = m_additionalDirectives.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_additionalDirectives[ i ].Origin == AdditionalContainerOrigin.Native )
				{
					m_nativeDirectivesIndex = i;
					break;
				}
			}
		}

		public override void Destroy()
		{
			base.Destroy();

			m_nativeDirectives.Clear();
			m_nativeDirectives = null;

			for( int i = 0; i < m_additionalDirectives.Count; i++ )
			{
				ScriptableObject.DestroyImmediate( m_additionalDirectives[ i ] );
			}

			m_additionalDirectives.Clear();
			m_additionalDirectives = null;

			for( int i = 0; i < m_shaderFunctionDirectives.Count; i++ )
			{
				ScriptableObject.DestroyImmediate( m_shaderFunctionDirectives[ i ] );
			}

			m_shaderFunctionDirectives.Clear();
			m_shaderFunctionDirectives = null;


			m_propertyAdjustment = null;
			m_reordableList = null;
		}


		public List<AdditionalDirectiveContainer> DirectivesList { get { return m_additionalDirectives; } }
		public bool IsValid { get { return m_validData; } set { m_validData = value; } }
	}
}
