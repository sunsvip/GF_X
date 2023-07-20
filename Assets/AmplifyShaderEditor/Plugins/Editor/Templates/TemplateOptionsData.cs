// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	/*ase_pass_options OLDEST
	DefineOnConnected:portId:definevalue	
	DefineOnUnconnected:portId:definevalue
	Options:name:defaultOption:opt0:opt1:opt2
	SetVisible:PortId:OptionName:OptionValue
	*/

	/*ase_pass_options OLD
	Option:Option Name:UI Type:Default:Item0,Item1,Item3...ItemN
	Action:Action Type:Action Data:ConditionA && ConditionB || ConditionC:
	*/

	/*ase_pass_options:UniqueId:PropagateDataToHiddenPasses
	Option:Color Offset:A,B,C:A
		A:ShowPort:My Port Name
		B,C:HidePort:My Port Name
		B:SetDefine:MY_DEFINE
		C:SetDefine:MY_COLOR_DEFINE
	Option:My Other Option:True,False
		True:ShowOption:Color Offset
		False:HideOption:Color Offset
	Port:My Port Name
		On:SetDefine:MY_COLOR_DEFINE
		Off:UnsetDefine:MY_COLOR_DEFINE
	*/
	public enum AseOptionsUIWidget
	{
		Dropdown,
		Toggle,
		Float,
		FloatRange,
		Int,
		IntRange
	}

	public enum AseOptionsType
	{
		Option,
		Port,
		Field
	}


	public enum AseOptionItemSetup
	{
		None,
		InvertActionOnDeselection
	}

	public enum AseOptionsActionType
	{
		ShowOption,
		HideOption,
		SetOption,
		HidePort,
		ShowPort,
		SetPortName,
		SetDefine,
		RemoveDefine,
		SetUndefine,
		RemoveUndefine,
		ExcludePass,
		IncludePass,
		SetPropertyOnPass,
		SetPropertyOnSubShader,
		SetShaderProperty,
		SetMaterialProperty,
		ExcludeAllPassesBut
	}

	public enum PropertyActionsEnum
	{
		CullMode,
		ColorMask,
		ColorMask1,
		ColorMask2,
		ColorMask3,
		ZWrite,
		ZTest,
		ZOffsetFactor,
		ZOffsetUnits,
		BlendRGB,
		BlendAlpha,
		BlendOpRGB,
		BlendOpAlpha,
		BlendRGB1,
		BlendAlpha1,
		BlendOpRGB1,
		BlendOpAlpha1,
		BlendRGB2,
		BlendAlpha2,
		BlendOpRGB2,
		BlendOpAlpha2,
		BlendRGB3,
		BlendAlpha3,
		BlendOpRGB3,
		BlendOpAlpha3,
		StencilReference,
		StencilReadMask,
		StencilWriteMask,
		StencilComparison,
		StencilPass,
		StencilFail,
		StencilZFail,
		RenderType,
		RenderQueue,
		DisableBatching,
		ChangeTagValue
	}

	public enum AseOptionsSetup
	{
		CopyOptionsFromMainPass,
		Id,
		Name
	}

	[Serializable]
	public class ItemColorMask
	{
		public bool ValueR = true;
		public bool ValueG = true;
		public bool ValueB = true;
		public bool ValueA = true;

		public bool MaskR = false;
		public bool MaskG = false;
		public bool MaskB = false;
		public bool MaskA = false;

		public bool[] GetColorMask( bool[] input )
		{
			bool[] result = { ValueR, ValueG, ValueB, ValueA };
			result[ 0 ] = MaskR ? ValueR : input[ 0 ];
			result[ 1 ] = MaskG ? ValueG : input[ 1 ];
			result[ 2 ] = MaskB ? ValueB : input[ 2 ];
			result[ 3 ] = MaskA ? ValueA : input[ 3 ];
			return result;
		}

		public void SetColorMask( int index, string value )
		{
			switch( index )
			{
				default:
				case 0:
				{
					MaskR = !string.IsNullOrEmpty( value );
					if(MaskR)
						ValueR = Convert.ToBoolean( value );
				}
				break;
				case 1:
				{
					MaskG = !string.IsNullOrEmpty( value );
					if( MaskG )
						ValueG = Convert.ToBoolean( value );
				}
				break;
				case 2:
				{
					MaskB = !string.IsNullOrEmpty( value );
					if( MaskB )
						ValueB = Convert.ToBoolean( value );
				}
				break;
				case 3:
				{
					MaskA = !string.IsNullOrEmpty( value );
					if( MaskA )
						ValueA = Convert.ToBoolean( value );
				}
				break;
			}
		}
	}

	[Serializable]
	public class TemplateActionItem
	{
		public AseOptionsActionType ActionType;
		public string ActionData = string.Empty;
		public int ActionDataIdx = -1;

		public string PassName;
		public bool AllPasses = false;

		public PropertyActionsEnum PropertyAction;
		//CULL
		public CullMode ActionCullMode;
		//COLOR MASK
		public ItemColorMask ColorMask = new ItemColorMask();
		public ItemColorMask ColorMask1 = new ItemColorMask();
		public ItemColorMask ColorMask2 = new ItemColorMask();
		public ItemColorMask ColorMask3 = new ItemColorMask();

		//DEPTH
		public ZWriteMode ActionZWrite;
		public ZTestMode ActionZTest;
		public float ActionZOffsetFactor;
		public float ActionZOffsetUnits;

		//BLEND OPS
		public AvailableBlendFactor ActionBlendRGBSource;
		public AvailableBlendFactor ActionBlendRGBDest;
		public AvailableBlendFactor ActionBlendAlphaSource;
		public AvailableBlendFactor ActionBlendAlphaDest;
		public AvailableBlendOps ActionBlendOpRGB;
		public AvailableBlendOps ActionBlendOpAlpha;

		public AvailableBlendFactor ActionBlendRGBSource1;
		public AvailableBlendFactor ActionBlendRGBDest1;
		public AvailableBlendFactor ActionBlendAlphaSource1;
		public AvailableBlendFactor ActionBlendAlphaDest1;
		public AvailableBlendOps ActionBlendOpRGB1;
		public AvailableBlendOps ActionBlendOpAlpha1;

		public AvailableBlendFactor ActionBlendRGBSource2;
		public AvailableBlendFactor ActionBlendRGBDest2;
		public AvailableBlendFactor ActionBlendAlphaSource2;
		public AvailableBlendFactor ActionBlendAlphaDest2;
		public AvailableBlendOps ActionBlendOpRGB2;
		public AvailableBlendOps ActionBlendOpAlpha2;

		public AvailableBlendFactor ActionBlendRGBSource3;
		public AvailableBlendFactor ActionBlendRGBDest3;
		public AvailableBlendFactor ActionBlendAlphaSource3;
		public AvailableBlendFactor ActionBlendAlphaDest3;
		public AvailableBlendOps ActionBlendOpRGB3;
		public AvailableBlendOps ActionBlendOpAlpha3;

		//STENCIL 
		public int ActionStencilReference;
		public int ActionStencilReadMask;
		public int ActionStencilWriteMask;
		public int ActionStencilComparison;
		public int ActionStencilPass;
		public int ActionStencilFail;
		public int ActionStencilZFail;

		public bool CopyFromSubShader = false;

		public string ActionBuffer;
		public override string ToString()
		{
			return ActionType + " " + ActionData + " " + ActionDataIdx;
		}

		public bool GetIntValueFromActionBuffer( out int result )
		{
			return int.TryParse( ActionBuffer, out result );
		}

		public bool GetFloatValueFromActionBuffer( out float result )
		{
			return float.TryParse( ActionBuffer, out result );
		}
	}

	[Serializable]
	public class TemplateActionItemGrid
	{
		[Serializable]
		public class TemplateActionItemRow
		{
			public TemplateActionItem[] Columns;
		}

		public TemplateActionItemRow[] Rows;

		public TemplateActionItemGrid( int rowsCount )
		{
			Rows = new TemplateActionItemRow[ rowsCount ];
		}

		public TemplateActionItem this[ int row, int column ]
		{
			get { return Rows[ row ].Columns[ column ]; }
			set { Rows[ row ].Columns[ column ] = value; }
		}

		public TemplateActionItem[] this[ int row ]
		{
			get { return Rows[ row ].Columns; }

			set
			{
				if( Rows[ row ] == null )
					Rows[ row ] = new TemplateActionItemRow();

				Rows[ row ].Columns = value;
			}
		}
	}

	[Serializable]
	public class TemplateOptionsItem
	{
		public AseOptionsType Type;
		public AseOptionsUIWidget UIWidget;
		public AseOptionItemSetup Setup = AseOptionItemSetup.None;

		public string Id = string.Empty;
		public string Name = string.Empty;
		public string DefaultOption = string.Empty;
		public string[] Options = null;
		public string[] DisplayOptions = null;
		public int DisableIdx = -1;

		[SerializeField]
		private float m_defaultFieldValue;

		public float FieldMin;
		public float FieldMax;

		public bool FieldInline;
		public string FieldInlineName;
		public string FieldInlineOutput = string.Empty;

		[SerializeField]
		public InlineProperty FieldValue = new InlineProperty();

		public TemplateActionItemGrid ActionsPerOption = null;

		public int Count = 0;

		[SerializeField]
		private int m_defaultOptionIndex = -1;

		~TemplateOptionsItem()
		{
			Options = null;
		}

		public int OptionIndexFor( string option )
		{
			for( int i = 0; i < Options.Length; i++ )
			{
				if( Options[ i ].Equals( option ) )
				{
					return i;
				}
			}
			Debug.LogWarning( "Couldn't find index for option: " + option );
			return 0;
		}

		public int DefaultOptionIndex
		{
			get
			{
				if( m_defaultOptionIndex > -1 )
					return m_defaultOptionIndex;

				for( int i = 0; i < Options.Length; i++ )
				{
					if( Options[ i ].Equals( DefaultOption ) )
					{
						m_defaultOptionIndex = i;
						return i;
					}
				}
				Debug.LogWarning( "Couldn't find index for default option: " + DefaultOption );
				return 0;
			}
		}

		public float DefaultFieldValue
		{
			get
			{
				return m_defaultFieldValue;
			}
			set
			{
				m_defaultFieldValue = value;
			}
		}
	}

	[Serializable]
	public class TemplateOptionsContainer
	{
		public bool Enabled = false;
		public string Body = string.Empty;
		public int Index = -1;
		public int Id = -1;
		public string Name = string.Empty;
		public bool CopyOptionsFromMainPass = false;
		public TemplateOptionsItem[] Options = null;
		~TemplateOptionsContainer()
		{
			Options = null;
		}

		public void CopyPortOptionsFrom( TemplateOptionsContainer container, string passName )
		{
			if( container == null || container.Options == null )
				return;

			List<TemplateOptionsItem> newItems = new List<TemplateOptionsItem>();
			for( int i = 0; i < container.Options.Length; i++ )
			{
				if( container.Options[ i ].Type == AseOptionsType.Port &&
					container.Options[ i ].Id.Equals( passName ) )
				{
					newItems.Add( container.Options[ i ] );
				}
			}

			if( newItems.Count > 0 )
			{
				Enabled = true;
				if( Options == null )
				{
					Options = newItems.ToArray();
				}
				else
				{
					Array.Resize<TemplateOptionsItem>( ref Options, Options.Length + newItems.Count );
					Array.Copy( newItems.ToArray(), Options, newItems.Count );
				}
			}
			newItems.Clear();
			newItems = null;
		}

		public int EndIndex { get { return Index + Body.Length; } }
	}

	public class TemplateOptionsToolsHelper
	{
		//public const string PassOptionsMainPattern = @"\/\*ase_pass_options:([\w:= ]*)[\n]([\w: \t;\n&|,_\+-]*)\*\/";
		//public const string SubShaderOptionsMainPattern = @"\/\*ase_subshader_options:([\w:= ]*)[\n]([\w: \t;\n&|,_\+-]*)\*\/";
		public const string PassOptionsMainPattern = "\\/\\*ase_pass_options:([\\w:= ]*)[\n]([\\w: \t;\n&|,_\\+\\-\\(\\)\\[\\]\\\"\\=\\/\\.]*)\\*\\/";
		public const string SubShaderOptionsMainPattern = "\\/\\*ase_subshader_options:([\\w:= ]*)[\n]([\\w: \t;\n&|,_\\+\\-\\(\\)\\[\\]\\\"\\=\\/\\.]*)\\*\\/";
		public static readonly char OptionsDataSeparator = ',';
		public static Dictionary<string, AseOptionsSetup> AseOptionsSetupDict = new Dictionary<string, AseOptionsSetup>()
		{
			{ "CopyOptionsFromMainPass",AseOptionsSetup.CopyOptionsFromMainPass},
			{ "Id",AseOptionsSetup.Id},
			{ "Name",AseOptionsSetup.Name},
		};

		public static Dictionary<string, AseOptionsUIWidget> AseOptionsUITypeDict = new Dictionary<string, AseOptionsUIWidget>()
		{
			{ "Dropdown",AseOptionsUIWidget.Dropdown },
			{ "Toggle", AseOptionsUIWidget.Toggle }
		};

		public static Dictionary<string, AseOptionsActionType> AseOptionsActionTypeDict = new Dictionary<string, AseOptionsActionType>()
		{
			{"ShowOption",  AseOptionsActionType.ShowOption },
			{"HideOption",  AseOptionsActionType.HideOption },
			{"SetOption",  AseOptionsActionType.SetOption },
			{"HidePort",    AseOptionsActionType.HidePort },
			{"ShowPort",    AseOptionsActionType.ShowPort },
			{"SetPortName",    AseOptionsActionType.SetPortName },
			{"SetDefine",   AseOptionsActionType.SetDefine },
			{"RemoveDefine",   AseOptionsActionType.RemoveDefine },
			{"SetUndefine", AseOptionsActionType.SetUndefine },
			{"RemoveUndefine", AseOptionsActionType.RemoveUndefine },
			{"ExcludePass", AseOptionsActionType.ExcludePass },
			{"IncludePass", AseOptionsActionType.IncludePass },
			{"SetPropertyOnPass", AseOptionsActionType.SetPropertyOnPass },
			{"SetPropertyOnSubShader", AseOptionsActionType.SetPropertyOnSubShader },
			{"SetShaderProperty", AseOptionsActionType.SetShaderProperty },
			{"SetMaterialProperty", AseOptionsActionType.SetMaterialProperty },
			{"ExcludeAllPassesBut",AseOptionsActionType.ExcludeAllPassesBut}
		};

		public static Dictionary<string, AseOptionItemSetup> AseOptionItemSetupDict = new Dictionary<string, AseOptionItemSetup>
		{
			{"None", AseOptionItemSetup.None },
			{ "InvertActionOnDeselection", AseOptionItemSetup.InvertActionOnDeselection}
		};

		public static bool InvertAction( AseOptionsActionType original, ref AseOptionsActionType inverted )
		{
			bool success = true;
			switch( original )
			{
				case AseOptionsActionType.ShowOption:
				inverted = AseOptionsActionType.HideOption;
				break;
				case AseOptionsActionType.HideOption:
				inverted = AseOptionsActionType.ShowOption;
				break;
				case AseOptionsActionType.HidePort:
				inverted = AseOptionsActionType.ShowPort;
				break;
				case AseOptionsActionType.ShowPort:
				inverted = AseOptionsActionType.HidePort;
				break;
				case AseOptionsActionType.SetDefine:
				inverted = AseOptionsActionType.RemoveDefine;
				break;
				case AseOptionsActionType.RemoveDefine:
				inverted = AseOptionsActionType.SetDefine;
				break;
				case AseOptionsActionType.SetUndefine:
				inverted = AseOptionsActionType.RemoveUndefine;
				break;
				case AseOptionsActionType.RemoveUndefine:
				inverted = AseOptionsActionType.SetUndefine;
				break;
				case AseOptionsActionType.ExcludePass:
				inverted = AseOptionsActionType.IncludePass;
				break;
				case AseOptionsActionType.IncludePass:
				inverted = AseOptionsActionType.ExcludePass;
				break;
				case AseOptionsActionType.SetPortName:
				case AseOptionsActionType.SetOption:
				case AseOptionsActionType.SetPropertyOnPass:
				case AseOptionsActionType.SetPropertyOnSubShader:
				success = false;
				break;
			}
			return success;
		}


		public static TemplateOptionsContainer GenerateOptionsContainer( bool isSubShader, string data )
		{
			TemplateOptionsContainer optionsContainer = new TemplateOptionsContainer();

			Match match = Regex.Match( data, isSubShader ? SubShaderOptionsMainPattern : PassOptionsMainPattern );
			optionsContainer.Enabled = match.Success;
			if( match.Success )
			{
				try
				{
					optionsContainer.Body = match.Value;
					optionsContainer.Index = match.Index;

					List<TemplateOptionsItem> optionItemsList = new List<TemplateOptionsItem>();
					List<List<TemplateActionItem>> actionItemsList = new List<List<TemplateActionItem>>();
					Dictionary<string, int> optionItemToIndex = new Dictionary<string, int>();
					TemplateOptionsItem currentOption = null;

					//OPTIONS OVERALL SETUP
					string[] setupLines = match.Groups[ 1 ].Value.Split( ':' );
					for( int i = 0; i < setupLines.Length; i++ )
					{
						if( AseOptionsSetupDict.ContainsKey( setupLines[ i ] ) )
						{
							AseOptionsSetup setup = AseOptionsSetupDict[ setupLines[ i ] ];
							switch( setup )
							{
								case AseOptionsSetup.CopyOptionsFromMainPass: optionsContainer.CopyOptionsFromMainPass = true; break;
							}
						}
						else
						{
							string[] args = setupLines[ i ].Split( '=' );
							if( args.Length > 1 && AseOptionsSetupDict.ContainsKey( args[ 0 ] ) )
							{
								AseOptionsSetup setup = AseOptionsSetupDict[ args[ 0 ] ];
								switch( setup )
								{
									case AseOptionsSetup.Id: if( !int.TryParse( args[ 1 ], out optionsContainer.Id ) ) optionsContainer.Id = -1; break;
									case AseOptionsSetup.Name: optionsContainer.Name = args[ 1 ]; break;
								}
							}
						}
					}

					//AVAILABLE OPTIONS
					string body = match.Groups[ 2 ].Value.Replace( "\t", string.Empty );
					string[] optionLines = body.Split( '\n' );
					for( int oL = 0; oL < optionLines.Length; oL++ )
					{
						string[] optionItems = optionLines[ oL ].Split( ':' );
						if( optionItems.Length > 0 )
						{
							string[] itemIds = optionItems[ 0 ].Split( OptionsDataSeparator );
							switch( itemIds[ 0 ] )
							{
								case "Option":
								{
									//Fills previous option with its actions
									//actionItemsList is cleared over here
									FillOptionAction( currentOption, ref actionItemsList );

									optionItemToIndex.Clear();
									currentOption = new TemplateOptionsItem();
									currentOption.Type = AseOptionsType.Option;
									string[] optionItemSetup = optionItems[ 1 ].Split( OptionsDataSeparator );
									currentOption.Name = optionItemSetup[ 0 ];
									if( optionItemSetup.Length > 1 )
									{
										if( AseOptionItemSetupDict.ContainsKey( optionItemSetup[ 1 ] ) )
											currentOption.Setup = AseOptionItemSetupDict[ optionItemSetup[ 1 ] ];
									}

									currentOption.Id = itemIds.Length > 1 ? itemIds[ 1 ] : optionItems[ 1 ];
									currentOption.DisplayOptions = optionItems[ 2 ].Split( OptionsDataSeparator );
									currentOption.DisableIdx = currentOption.DisplayOptions.Length;
									optionItems[ 2 ] += ",disable";
									currentOption.Options = optionItems[ 2 ].Split( OptionsDataSeparator );
									currentOption.Count = currentOption.Options.Length;

									for( int opIdx = 0; opIdx < currentOption.Options.Length; opIdx++ )
									{
										optionItemToIndex.Add( currentOption.Options[ opIdx ], opIdx );
										actionItemsList.Add( new List<TemplateActionItem>() );
									}

									if( optionItems.Length > 3 )
									{
										currentOption.DefaultOption = optionItems[ 3 ];
									}
									else
									{
										currentOption.DefaultOption = currentOption.Options[ 0 ];
									}

									if( currentOption.Options.Length == 2 || ( currentOption.Options.Length == 3 && currentOption.Options[ 2 ].Equals( "disable" ) ) )
									{
										if( ( currentOption.Options[ 0 ].Equals( "true" ) && currentOption.Options[ 1 ].Equals( "false" ) ) ||
											( currentOption.Options[ 0 ].Equals( "false" ) && currentOption.Options[ 1 ].Equals( "true" ) ) )
										{
											// Toggle 0 is false and 1 is true
											currentOption.Options[ 0 ] = "false";
											currentOption.Options[ 1 ] = "true";
											currentOption.UIWidget = AseOptionsUIWidget.Toggle;
										}
									}
									else if( currentOption.Options.Length > 2 )
									{
										currentOption.UIWidget = AseOptionsUIWidget.Dropdown;
									}
									else
									{
										Debug.LogWarning( "Detected an option with less than two items:" + optionItems[ 1 ] );
									}
									optionItemsList.Add( currentOption );
								}
								break;
								case "Port":
								{
									//Fills previous option with its actions
									//actionItemsList is cleared over here
									FillOptionAction( currentOption, ref actionItemsList );

									optionItemToIndex.Clear();

									currentOption = new TemplateOptionsItem();
									currentOption.Type = AseOptionsType.Port;
									if( isSubShader && optionItems.Length > 2 )
									{
										currentOption.Id = optionItems[ 1 ];
										currentOption.Name = optionItems[ 2 ];
									}
									else
									{
										currentOption.Name = optionItems[ 1 ];
									}

									currentOption.Options = new string[] { "On", "Off" };
									optionItemToIndex.Add( currentOption.Options[ 0 ], 0 );
									optionItemToIndex.Add( currentOption.Options[ 1 ], 1 );

									actionItemsList.Add( new List<TemplateActionItem>() );
									actionItemsList.Add( new List<TemplateActionItem>() );

									optionItemsList.Add( currentOption );
								}
								break;
								case "Field":
								{
									//Fills previous option with its actions
									//actionItemsList is cleared over here
									FillOptionAction( currentOption, ref actionItemsList );

									optionItemToIndex.Clear();
									currentOption = new TemplateOptionsItem();
									currentOption.Type = AseOptionsType.Field;
									
									currentOption.Id = optionItems[ 1 ];
									currentOption.Name = optionItems[ 1 ];

									currentOption.UIWidget = AseOptionsUIWidget.Float;
									if( optionItems[ 2 ].Equals( "Int" ) )
										currentOption.UIWidget = AseOptionsUIWidget.Int;

									if( optionItems.Length >= 3 )
									{
										currentOption.DefaultFieldValue = Convert.ToSingle( optionItems[ 3 ], System.Globalization.CultureInfo.InvariantCulture );
									}

									if( optionItems.Length >= 6 )
									{
										if( currentOption.UIWidget == AseOptionsUIWidget.Int )
											currentOption.UIWidget = AseOptionsUIWidget.Int;
										else
											currentOption.UIWidget = AseOptionsUIWidget.FloatRange;

										currentOption.FieldMin = Convert.ToSingle( optionItems[ 4 ], System.Globalization.CultureInfo.InvariantCulture );
										currentOption.FieldMax = Convert.ToSingle( optionItems[ 5 ], System.Globalization.CultureInfo.InvariantCulture );
									}

									if( optionItems.Length == 5 || optionItems.Length == 7 )
									{
										currentOption.FieldInline = true;
										currentOption.FieldInlineName = optionItems[ optionItems.Length - 1 ];
									}

									currentOption.Options = new string[] { "Change", "Inline", "disable" };

									optionItemToIndex.Add( currentOption.Options[ 0 ], 0 );
									optionItemToIndex.Add( currentOption.Options[ 1 ], 1 );
									optionItemToIndex.Add( currentOption.Options[ 2 ], 2 );
									currentOption.DisableIdx = 2;

									actionItemsList.Add( new List<TemplateActionItem>() );
									actionItemsList.Add( new List<TemplateActionItem>() );
									actionItemsList.Add( new List<TemplateActionItem>() );

									optionItemsList.Add( currentOption );
								}
								break;
								default:
								{
									if( optionItemToIndex.ContainsKey( optionItems[ 0 ] ) )
									{
										int idx = 0;
										if( currentOption != null && currentOption.UIWidget == AseOptionsUIWidget.Toggle )
										{
											idx = ( optionItems[ 0 ].Equals( "true" ) ) ? 1 : 0;
											if( optionItems[ 0 ].Equals( "disable" ) )
												idx = 2;
										}
										else
										{
											idx = optionItemToIndex[ optionItems[ 0 ] ];
										}
										actionItemsList[ idx ].Add( CreateActionItem( isSubShader, optionItems ) );
									}
									else
									{
										//string[] ids = optionItems[ 0 ].Split( ',' );
										if( itemIds.Length > 1 )
										{
											for( int i = 0; i < itemIds.Length; i++ )
											{
												if( optionItemToIndex.ContainsKey( itemIds[ i ] ) )
												{
													int idx = optionItemToIndex[ itemIds[ i ] ];
													actionItemsList[ idx ].Add( CreateActionItem( isSubShader, optionItems ) );
												}
											}
										}
									}

								}
								break;
							}
						}
					}

					//Fills last option with its actions
					FillOptionAction( currentOption, ref actionItemsList );

					actionItemsList.Clear();
					actionItemsList = null;

					optionsContainer.Options = optionItemsList.ToArray();
					optionItemsList.Clear();
					optionItemsList = null;

					optionItemToIndex.Clear();
					optionItemToIndex = null;
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
			return optionsContainer;
		}

		static void FillOptionAction( TemplateOptionsItem currentOption, ref List<List<TemplateActionItem>> actionItemsList )
		{
			if( currentOption != null )
			{
				int count = actionItemsList.Count;
				currentOption.ActionsPerOption = new TemplateActionItemGrid( count );
				for( int i = 0; i < count; i++ )
				{
					currentOption.ActionsPerOption[ i ] = actionItemsList[ i ].ToArray();
					actionItemsList[ i ].Clear();
				}
				actionItemsList.Clear();
			}
		}

		static TemplateActionItem CreateActionItem( bool isSubshader, string[] optionItems )
		{
			TemplateActionItem actionItem = new TemplateActionItem();
			try
			{
				actionItem.ActionType = AseOptionsActionTypeDict[ optionItems[ 1 ] ];
				int optionsIdx = 2;
				if( optionItems.Length > 3 )
				{
					optionsIdx = 3;
					actionItem.PassName = optionItems[ 2 ];
				}
				else
				{
					actionItem.AllPasses = isSubshader;
				}

				actionItem.ActionData = optionItems[ optionsIdx ];

				switch( actionItem.ActionType )
				{
					case AseOptionsActionType.ShowOption:
					case AseOptionsActionType.HideOption:
					{
						string[] arr = optionItems[ optionsIdx ].Split( OptionsDataSeparator );
						if( arr.Length > 1 )
						{
							actionItem.ActionData = arr[ 0 ];
							if( !int.TryParse( arr[ 1 ], out actionItem.ActionDataIdx ) )
							{
								actionItem.ActionDataIdx = -1;
							}
						}
					}
					break;
					case AseOptionsActionType.SetOption:
					{
						string[] arr = optionItems[ optionsIdx ].Split( OptionsDataSeparator );
						if( arr.Length > 1 )
						{
							actionItem.ActionData = arr[ 0 ];
							if( !int.TryParse( arr[ 1 ], out actionItem.ActionDataIdx ) )
							{
								Debug.LogWarning( "SetOption value must be a the selection index" );
							}
						}
					}
					break;
					case AseOptionsActionType.HidePort:
					case AseOptionsActionType.ShowPort:
					{
						if( !int.TryParse( actionItem.ActionData, out actionItem.ActionDataIdx ) )
							actionItem.ActionDataIdx = -1;
					}
					break;
					case AseOptionsActionType.SetPortName:
					{
						string[] arr = optionItems[ optionsIdx ].Split( OptionsDataSeparator );
						if( arr.Length > 1 )
						{
							int.TryParse( arr[ 0 ], out actionItem.ActionDataIdx );
							actionItem.ActionData = arr[ 1 ];
						}
					}
					break;
					case AseOptionsActionType.SetDefine:
					case AseOptionsActionType.RemoveDefine:
					case AseOptionsActionType.SetUndefine:
					case AseOptionsActionType.RemoveUndefine:
					case AseOptionsActionType.ExcludePass:
					case AseOptionsActionType.IncludePass:
					case AseOptionsActionType.ExcludeAllPassesBut:
					break;
					case AseOptionsActionType.SetShaderProperty:
					{
						int optIndex = optionItems[ optionsIdx ].IndexOf( OptionsDataSeparator );
						if( optIndex > -1 )
						{
							actionItem.ActionData = optionItems[ optionsIdx ].Substring( 0, optIndex );
							actionItem.ActionBuffer = optionItems[ optionsIdx ].Substring( optIndex + 1, optionItems[ optionsIdx ].Length - optIndex - 1);
						}
					}break;
					case AseOptionsActionType.SetMaterialProperty:
					{
						int optIndex = optionItems[ optionsIdx ].IndexOf( OptionsDataSeparator );
						if( optIndex > -1 )
						{
							actionItem.ActionData = optionItems[ optionsIdx ].Substring( 0, optIndex );
						}
					}
					break;
					case AseOptionsActionType.SetPropertyOnPass:
					case AseOptionsActionType.SetPropertyOnSubShader:
					{
						string[] arr = optionItems[ optionsIdx ].Split( OptionsDataSeparator );
						actionItem.PropertyAction = (PropertyActionsEnum)Enum.Parse( typeof( PropertyActionsEnum ), arr[ 0 ] );
						if( arr.Length == 1 && actionItem.ActionType == AseOptionsActionType.SetPropertyOnPass )
						{
							actionItem.CopyFromSubShader = true;
						}
						else
						{
							switch( actionItem.PropertyAction )
							{
								case PropertyActionsEnum.CullMode:
								{
									if( arr.Length > 1 )
										actionItem.ActionCullMode = (CullMode)Enum.Parse( typeof( CullMode ), arr[ 1 ] );
								}
								break;
								case PropertyActionsEnum.ColorMask:
								{
									if( arr.Length > 4 )
									{
										actionItem.ColorMask.SetColorMask( 0, arr[ 1 ] );
										actionItem.ColorMask.SetColorMask( 1, arr[ 2 ] );
										actionItem.ColorMask.SetColorMask( 2, arr[ 3 ] );
										actionItem.ColorMask.SetColorMask( 3, arr[ 4 ] );
									}
								}
								break;
								case PropertyActionsEnum.ColorMask1:
								{
									if( arr.Length > 4 )
									{
										actionItem.ColorMask1.SetColorMask( 0, arr[ 1 ] );
										actionItem.ColorMask1.SetColorMask( 1, arr[ 2 ] );
										actionItem.ColorMask1.SetColorMask( 2, arr[ 3 ] );
										actionItem.ColorMask1.SetColorMask( 3, arr[ 4 ] );
									}
								}
								break;
								case PropertyActionsEnum.ColorMask2:
								{
									if( arr.Length > 4 )
									{
										actionItem.ColorMask2.SetColorMask( 0, arr[ 1 ] );
										actionItem.ColorMask2.SetColorMask( 1, arr[ 2 ] );
										actionItem.ColorMask2.SetColorMask( 2, arr[ 3 ] );
										actionItem.ColorMask2.SetColorMask( 3, arr[ 4 ] );
									}
								}
								break;
								case PropertyActionsEnum.ColorMask3:
								{
									if( arr.Length > 4 )
									{
										actionItem.ColorMask3.SetColorMask( 0, arr[ 1 ] );
										actionItem.ColorMask3.SetColorMask( 1, arr[ 2 ] );
										actionItem.ColorMask3.SetColorMask( 2, arr[ 3 ] );
										actionItem.ColorMask3.SetColorMask( 3, arr[ 4 ] );
									}
								}
								break;
								case PropertyActionsEnum.ZWrite:
								{
									if( arr.Length > 1 )
										actionItem.ActionZWrite = (ZWriteMode)Enum.Parse( typeof( ZWriteMode ), arr[ 1 ] );
								}
								break;
								case PropertyActionsEnum.ZTest:
								{
									if( arr.Length > 1 )
										actionItem.ActionZTest = (ZTestMode)Enum.Parse( typeof( ZTestMode ), arr[ 1 ] );
								}
								break;
								case PropertyActionsEnum.ZOffsetFactor:
								{
									if( arr.Length > 1 )
										actionItem.ActionZOffsetFactor = Convert.ToSingle( arr[ 1 ] );
								}
								break;
								case PropertyActionsEnum.ZOffsetUnits:
								{
									if( arr.Length > 1 )
										actionItem.ActionZOffsetUnits = Convert.ToSingle( arr[ 1 ] );
								}
								break;
								case PropertyActionsEnum.BlendRGB:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendRGBSource = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendRGBDest = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendRGB1:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendRGBSource1 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendRGBDest1 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendRGB2:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendRGBSource2 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendRGBDest2 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendRGB3:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendRGBSource3 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendRGBDest3 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendAlpha:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendAlphaSource = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendAlphaDest = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendAlpha1:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendAlphaSource1 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendAlphaDest1 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendAlpha2:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendAlphaSource2 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendAlphaDest2 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendAlpha3:
								{
									if( arr.Length > 2 )
									{
										actionItem.ActionBlendAlphaSource3 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 1 ] );
										actionItem.ActionBlendAlphaDest3 = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), arr[ 2 ] );
									}
								}
								break;
								case PropertyActionsEnum.BlendOpRGB:
								{
									if( arr.Length > 1 )
									{
										actionItem.ActionBlendOpRGB = (AvailableBlendOps)Enum.Parse( typeof( AvailableBlendOps ), arr[ 1 ] );

									}
								}
								break;
								case PropertyActionsEnum.BlendOpAlpha:
								{
									if( arr.Length > 1 )
									{
										actionItem.ActionBlendOpAlpha = (AvailableBlendOps)Enum.Parse( typeof( AvailableBlendOps ), arr[ 1 ] );
									}
								}
								break;
								case PropertyActionsEnum.StencilReference:
								{
									if( arr.Length > 1 )
									{
										int.TryParse( arr[ 1 ], out actionItem.ActionStencilReference );
									}
								}
								break;
								case PropertyActionsEnum.StencilReadMask:
								{
									if( arr.Length > 1 )
									{
										int.TryParse( arr[ 1 ], out actionItem.ActionStencilReadMask );
									}
								}
								break;
								case PropertyActionsEnum.StencilWriteMask:
								{
									if( arr.Length > 1 )
									{
										int.TryParse( arr[ 1 ], out actionItem.ActionStencilWriteMask );
									}
								}
								break;
								case PropertyActionsEnum.StencilComparison:
								{
									if( arr.Length > 1 )
										actionItem.ActionStencilComparison = StencilBufferOpHelper.StencilComparisonValuesDict[ arr[ 1 ] ];
								}
								break;
								case PropertyActionsEnum.StencilPass:
								{
									if( arr.Length > 1 )
										actionItem.ActionStencilPass = StencilBufferOpHelper.StencilOpsValuesDict[ arr[ 1 ] ];
								}
								break;
								case PropertyActionsEnum.StencilFail:
								{
									if( arr.Length > 1 )
										actionItem.ActionStencilFail = StencilBufferOpHelper.StencilOpsValuesDict[ arr[ 1 ] ];
								}
								break;
								case PropertyActionsEnum.StencilZFail:
								{
									if( arr.Length > 1 )
										actionItem.ActionStencilZFail = StencilBufferOpHelper.StencilOpsValuesDict[ arr[ 1 ] ];
								}
								break;
								case PropertyActionsEnum.RenderType:
								{
									if( arr.Length > 1 )
										actionItem.ActionData = arr[ 1 ];
								}
								break;
								case PropertyActionsEnum.RenderQueue:
								{
									if( arr.Length > 1 )
										actionItem.ActionData = arr[ 1 ];
									if( arr.Length > 2 )
									{
										int.TryParse( arr[ 2 ], out actionItem.ActionDataIdx );
									}
									else
									{
										actionItem.ActionDataIdx = 0;
									}
								}
								break;
								case PropertyActionsEnum.DisableBatching:
								{
									if( arr.Length > 1 )
										actionItem.ActionData = arr[ 1 ];
								}
								break;
								case PropertyActionsEnum.ChangeTagValue:
								{
									if( arr.Length > 2 )
									{
										//Tag Name
										actionItem.ActionData = arr[ 1 ];
										//Tag Value
										actionItem.ActionBuffer = arr[ 2 ];
									}
								}
								break;
							}
						}
					}
					break;
				}
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
			return actionItem;
		}
	}
}
