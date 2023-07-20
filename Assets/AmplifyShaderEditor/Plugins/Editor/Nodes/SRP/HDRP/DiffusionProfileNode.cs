// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using System.Reflection;

namespace AmplifyShaderEditor
{
	public static class HDUtilsEx
	{
		private static System.Type type = null;
#if UNITY_2019_3_OR_NEWER
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Rendering.HighDefinition.HDUtils, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }
#else
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Experimental.Rendering.HDPipeline.HDUtils, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }
#endif

		public static string ConvertVector4ToGUID( Vector4 vector )
		{
			object[] parameters = new object[] { vector };
			MethodInfo method;
			method = Type.GetMethod( "ConvertVector4ToGUID", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof( Vector4 ) }, null );
			if( method == null )
				method = Type.GetMethod( "ConvertVector4ToGUID", new Type[] { typeof( Vector4 ) } );
			return (string)method.Invoke( null, parameters );
		}

		public static Vector4 ConvertGUIDToVector4( string guid )
		{
			object[] parameters = new object[] { guid };
			MethodInfo method;
			method = Type.GetMethod( "ConvertGUIDToVector4", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof( string ) }, null );
			if( method == null )
				method = Type.GetMethod( "ConvertGUIDToVector4", new Type[] { typeof( string ) } );
			return (Vector4)method.Invoke( null, parameters );
		}
	}

	public static class DiffusionProfileSettingsEx
	{
		private static System.Type type = null;
#if UNITY_2019_3_OR_NEWER
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }
#else
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Experimental.Rendering.HDPipeline.DiffusionProfileSettings, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }
#endif

		public static uint Hash( UnityEngine.Object m_instance )
		{
			FieldInfo field;
			field = Type.GetField( "profile", BindingFlags.Instance | BindingFlags.NonPublic );
			if( field == null )
				field = Type.GetField( "profile" );
			var profile = field.GetValue( m_instance );
			var hashField = profile.GetType().GetField( "hash" );
			return (uint)hashField.GetValue( profile );
		}
	}

	[Serializable]
	[NodeAttributes( "Diffusion Profile", "Constants And Properties", "Returns Diffusion Profile Hash Id. To be used on Diffusion Profile port on HDRP templates.", KeyCode.None, true, 0, int.MaxValue )]
	public sealed class DiffusionProfileNode : PropertyNode
	{
		[SerializeField]
		private UnityEngine.Object m_defaultValue;

		[SerializeField]
		private UnityEngine.Object m_materialValue;

		[SerializeField]
		private bool m_defaultInspector = false;

		private bool m_isEditingFields;
		//[NonSerialized]
		//private DiffusionProfileSettings m_previousValue;

		public const string NodeErrorMsg = "Only valid on HDRP";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_drawPrecisionUI = false;
			m_currentPrecisionType = PrecisionType.Float;
			m_srpBatcherCompatible = true;
			m_freeType = false;
#if UNITY_2019_3_OR_NEWER
			m_freeType = true;
#endif
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_errorMessageTooltip = NodeErrorMsg;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			UIUtils.RegisterPropertyNode( this );
		}

		public override void CopyDefaultsToMaterial()
		{
			m_materialValue = m_defaultValue;
		}

		public override void DrawSubProperties()
		{
			m_defaultValue = EditorGUILayoutObjectField( Constants.DefaultValueLabel, m_defaultValue, DiffusionProfileSettingsEx.Type, true ) /*as UnityEngine.Object*/;
		}

		public override void DrawMaterialProperties()
		{
			if( m_materialMode )
				EditorGUI.BeginChangeCheck();

			m_materialValue = EditorGUILayoutObjectField( Constants.MaterialValueLabel, m_materialValue, DiffusionProfileSettingsEx.Type, true ) /*as DiffusionProfileSettings*/;

			if( m_materialMode && EditorGUI.EndChangeCheck() )
			{
				m_requireMaterialUpdate = true;
			}
		}

		public override void DrawMainPropertyBlock()
		{
			EditorGUILayout.BeginVertical();
			{
				if( m_freeType )
				{
					PropertyType parameterType = (PropertyType)EditorGUILayoutEnumPopup( ParameterTypeStr, m_currentParameterType );
					if( parameterType != m_currentParameterType )
					{
						ChangeParameterType( parameterType );
						BeginPropertyFromInspectorCheck();
					}
				}

				if( m_freeName )
				{
					switch( m_currentParameterType )
					{
						case PropertyType.Property:
						case PropertyType.InstancedProperty:
						{
							m_defaultInspector = EditorGUILayoutToggle( "Default Inspector", m_defaultInspector );
							if( m_defaultInspector )
								EditorGUILayout.HelpBox("While \"Default Inspector\" is turned ON you can't reorder this property or change it's name, and you can only have one per shader, use it only if you intend to share this shader with non-ASE users",MessageType.Info);
							EditorGUI.BeginDisabledGroup( m_defaultInspector );
							ShowPropertyInspectorNameGUI();
							ShowPropertyNameGUI( true );
							EditorGUI.EndDisabledGroup();
							ShowVariableMode();
							ShowAutoRegister();
							ShowPrecision();
							ShowToolbar();
						}
						break;
						case PropertyType.Global:
						{
							ShowPropertyInspectorNameGUI();
							ShowPropertyNameGUI( false );
							ShowVariableMode();
							ShowAutoRegister();
							ShowPrecision();
							ShowDefaults();
						}
						break;
						case PropertyType.Constant:
						{
							ShowPropertyInspectorNameGUI();
							ShowPrecision();
							ShowDefaults();
						}
						break;
					}
				}
			}
			EditorGUILayout.EndVertical();
		}

		public override void OnNodeLogicUpdate(DrawInfo drawInfo)
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
								 ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.HDRP );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox(NodeErrorMsg, MessageType.Error );
			}				
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if( m_currentParameterType != PropertyType.Constant )
			{
				if( m_defaultInspector )
					return "_DiffusionProfileHash";
				return PropertyData( dataCollector.PortCategory );
			}

#if UNITY_2019_3_OR_NEWER
			return RoundTrip.ToRoundTrip( HDShadowUtilsEx.Asfloat( DefaultHash ) );
#else
			return "asfloat(" + DefaultHash.ToString() + ")";
#endif
		}


		public override string GetUniformValue()
		{
			if( m_defaultInspector )
				return "float _DiffusionProfileHash";
			return base.GetUniformValue();
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			if( m_defaultInspector )
			{
				dataType = "float";
				dataName = "_DiffusionProfileHash";
				return true;
			}
			return base.GetUniformData( out dataType, out dataName, ref fullValue );
		}

		public override string GetPropertyValue()
		{
			Vector4 asset = Vector4.zero;
			if( m_defaultValue != null )
				asset = HDUtilsEx.ConvertGUIDToVector4( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_defaultValue ) ) );
			string assetVec = RoundTrip.ToRoundTrip( asset.x ) + ", " + RoundTrip.ToRoundTrip( asset.y ) + ", " + RoundTrip.ToRoundTrip( asset.z ) + ", " + RoundTrip.ToRoundTrip( asset.w );
			string lineOne = string.Empty;
			string lineTwo = string.Empty;
			if( m_defaultInspector )
			{
				lineOne = PropertyAttributes + "[HideInInspector]_DiffusionProfileAsset(\"" + m_propertyInspectorName + "\", Vector) = ( " + assetVec + " )";
				lineTwo = "\n[HideInInspector]_DiffusionProfileHash(\"" + m_propertyInspectorName + "\", Float) = " + RoundTrip.ToRoundTrip( HDShadowUtilsEx.Asfloat( DefaultHash ) );
			}
			else
			{
#if UNITY_2020_2_OR_NEWER
				lineOne = "\n[DiffusionProfile]" + m_propertyName + "(\"" + m_propertyInspectorName + "\", Float) = " + RoundTrip.ToRoundTrip( HDShadowUtilsEx.Asfloat( DefaultHash ) );
				lineTwo = PropertyAttributes + "[HideInInspector]" + m_propertyName + "_Asset(\"" + m_propertyInspectorName + "\", Vector) = ( " + assetVec + " )";
#else
				lineOne = PropertyAttributes + "[ASEDiffusionProfile(" + m_propertyName + ")]" + m_propertyName + "_asset(\"" + m_propertyInspectorName + "\", Vector) = ( " + assetVec + " )";
				lineTwo = "\n[HideInInspector]" + m_propertyName + "(\"" + m_propertyInspectorName + "\", Float) = " + RoundTrip.ToRoundTrip( HDShadowUtilsEx.Asfloat( DefaultHash ) );
#endif
			}

			return lineOne + lineTwo;
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );

			if( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction )
			{
				if( m_materialValue != null )
				{
					Vector4 asset = HDUtilsEx.ConvertGUIDToVector4( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_materialValue ) ) );
					if( m_defaultInspector )
					{
						mat.SetVector( "_DiffusionProfileAsset", asset );
						mat.SetFloat( "_DiffusionProfileHash", HDShadowUtilsEx.Asfloat( MaterialHash ) );
					} 
					else
					{
						mat.SetVector( m_propertyName + "_asset", asset );
						mat.SetFloat( m_propertyName, HDShadowUtilsEx.Asfloat( MaterialHash ) );
					}
				}
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			string propertyAsset = m_propertyName + "_asset";
			if( m_defaultInspector )
				propertyAsset = "_DiffusionProfileAsset";

			if( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( propertyAsset ) )
			{
				var guid = HDUtilsEx.ConvertVector4ToGUID( material.GetVector( propertyAsset ) );
				var profile = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), DiffusionProfileSettingsEx.Type );
				if( profile != null )
					m_materialValue = profile;
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			string defaultGuid = ( m_defaultValue != null ) ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_defaultValue ) ) : "0";
			IOUtils.AddFieldValueToString( ref nodeInfo, defaultGuid );
			string materialGuid = ( m_materialValue != null ) ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_materialValue ) ) : "0";
			IOUtils.AddFieldValueToString( ref nodeInfo, materialGuid );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultInspector );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			if( UIUtils.CurrentShaderVersion() > 17004 )
				base.ReadFromString( ref nodeParams );
			else
				ParentReadFromString( ref nodeParams );

			string defaultGuid = GetCurrentParam( ref nodeParams );
			if( defaultGuid.Length > 1 )
			{
				m_defaultValue = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( defaultGuid ), DiffusionProfileSettingsEx.Type );
			}
			if( UIUtils.CurrentShaderVersion() > 17004 )
			{
				string materialGuid = GetCurrentParam( ref nodeParams );
				if( materialGuid.Length > 1 )
				{
					m_materialValue = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( materialGuid ), DiffusionProfileSettingsEx.Type );
				}
			}
			if( UIUtils.CurrentShaderVersion() > 17900 )
			{
				m_defaultInspector = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void ReadOutputDataFromString( ref string[] nodeParams )
		{
			base.ReadOutputDataFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() < 17005 )
				m_outputPorts[ 0 ].ChangeProperties( Constants.EmptyPortValue, WirePortDataType.FLOAT, false );
		}

		public override string GetPropertyValStr()
		{
			if( m_defaultInspector )
				return "_DiffusionProfileHash";
			return PropertyName;
		}

		//Vector4 ProfileGUID { get { return ( m_diffusionProfile != null ) ? HDUtils.ConvertGUIDToVector4( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_diffusionProfile ) ) ) : Vector4.zero; } }
		uint DefaultHash { get { return ( m_defaultValue != null ) ? DiffusionProfileSettingsEx.Hash( m_defaultValue ) : 0; } }
		uint MaterialHash { get { return ( m_materialValue != null ) ? DiffusionProfileSettingsEx.Hash( m_materialValue ) : 0; } }

		private static class HDShadowUtilsEx
		{
			private static System.Type type = null;
			public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Rendering.HighDefinition.HDShadowUtils, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }

			public static float Asfloat( uint val )
			{
#if UNITY_2019_3_OR_NEWER
				object[] parameters = new object[] { val };
				MethodInfo method = Type.GetMethod( "Asfloat", new Type[] { typeof( uint ) } );
				return (float)method.Invoke( null, parameters );
#else
				return HDShadowUtils.Asfloat( val );
#endif
			}

			public static uint Asuint( float val )
			{
#if UNITY_2019_3_OR_NEWER

				object[] parameters = new object[] { val };
				MethodInfo method = Type.GetMethod( "Asuint", new Type[] { typeof( float ) } );
				return (uint)method.Invoke( null, parameters );
#else
				return HDShadowUtils.Asuint( val );
#endif
			}
		}

		private static class RoundTrip
		{
			private static String[] zeros = new String[ 1000 ];

			static RoundTrip()
			{
				for( int i = 0; i < zeros.Length; i++ )
				{
					zeros[ i ] = new String( '0', i );
				}
			}

			public static String ToRoundTrip( double value )
			{
				String str = value.ToString( "r" );
				int x = str.IndexOf( 'E' );
				if( x < 0 ) return str;

				int x1 = x + 1;
				String exp = str.Substring( x1, str.Length - x1 );
				int e = int.Parse( exp );

				String s = null;
				int numDecimals = 0;
				if( value < 0 )
				{
					int len = x - 3;
					if( e >= 0 )
					{
						if( len > 0 )
						{
							s = str.Substring( 0, 2 ) + str.Substring( 3, len );
							numDecimals = len;
						}
						else
							s = str.Substring( 0, 2 );
					}
					else
					{
						// remove the leading minus sign
						if( len > 0 )
						{
							s = str.Substring( 1, 1 ) + str.Substring( 3, len );
							numDecimals = len;
						}
						else
							s = str.Substring( 1, 1 );
					}
				}
				else
				{
					int len = x - 2;
					if( len > 0 )
					{
						s = str[ 0 ] + str.Substring( 2, len );
						numDecimals = len;
					}
					else
						s = str[ 0 ].ToString();
				}

				if( e >= 0 )
				{
					e = e - numDecimals;
					String z = ( e < zeros.Length ? zeros[ e ] : new String( '0', e ) );
					s = s + z;
				}
				else
				{
					e = ( -e - 1 );
					String z = ( e < zeros.Length ? zeros[ e ] : new String( '0', e ) );
					if( value < 0 )
						s = "-0." + z + s;
					else
						s = "0." + z + s;
				}

				return s;
			}
		}
	}
}
#endif
