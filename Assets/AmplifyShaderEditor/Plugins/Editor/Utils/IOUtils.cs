// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading;
using UnityEditor.VersionControl;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{
	public enum ShaderLoadResult
	{
		LOADED,
		TEMPLATE_LOADED,
		FILE_NOT_FOUND,
		ASE_INFO_NOT_FOUND,
		UNITY_NATIVE_PATHS
	}

	public class Worker
	{
		public static readonly object locker = new object();
		public void DoWork()
		{
			while( IOUtils.ActiveThread )
			{
				if( IOUtils.SaveInThreadFlag )
				{
					IOUtils.SaveInThreadFlag = false;
					lock( locker )
					{
						IOUtils.SaveInThreadShaderBody = string.Format( IOUtils.ShaderCopywriteMessage, VersionInfo.StaticToString() ) + IOUtils.SaveInThreadShaderBody;
						// Add checksum 
						string checksum = IOUtils.CreateChecksum( IOUtils.SaveInThreadShaderBody );
						IOUtils.SaveInThreadShaderBody += IOUtils.CHECKSUM + IOUtils.VALUE_SEPARATOR + checksum;

						// Write to disk
						StreamWriter fileWriter = new StreamWriter( IOUtils.SaveInThreadPathName );
						try
						{
							fileWriter.Write( IOUtils.SaveInThreadShaderBody );
							Debug.Log( "Saving complete" );
						}
						catch( Exception e )
						{
							Debug.LogException( e );
						}
						finally
						{
							fileWriter.Close();
						}
					}
				}
			}
			Debug.Log( "Thread closed" );
		}
	}

	public static class IOUtils
	{
		public delegate void OnShaderAction( Shader shader , bool isTemplate , string type );
		public static OnShaderAction OnShaderSavedEvent;
		public static OnShaderAction OnShaderTypeChangedEvent;

		public static readonly string ShaderCopywriteMessage = "// Made with Amplify Shader Editor v{0}\n// Available at the Unity Asset Store - http://u3d.as/y3X \n";
		public static readonly string GrabPassEmpty = "\t\tGrabPass{ }\n";
		public static readonly string GrabPassBegin = "\t\tGrabPass{ \"";
		public static readonly string GrabPassEnd = "\" }\n";
		public static readonly string PropertiesBegin = "\tProperties\n\t{\n";
		public static readonly string PropertiesEnd = "\t}\n";
		public static readonly string PropertiesElement = "\t\t{0}\n";
		public static readonly string PropertiesElementsRaw = "{0}\n";

		public static readonly string PragmaTargetHeader = "\t\t#pragma target {0}\n";
		public static readonly string InstancedPropertiesHeader = "multi_compile_instancing";
		public static readonly string VirtualTexturePragmaHeader = "multi_compile _ _VT_SINGLE_MODE";

		public static readonly string InstancedPropertiesBegin = "UNITY_INSTANCING_CBUFFER_START({0})";
		public static readonly string InstancedPropertiesEnd = "UNITY_INSTANCING_CBUFFER_END";
		public static readonly string InstancedPropertiesElement = "UNITY_DEFINE_INSTANCED_PROP({0}, {1})";
		public static readonly string InstancedPropertiesData = "UNITY_ACCESS_INSTANCED_PROP({0})";

		public static readonly string DotsInstancedPropertiesData = "\tUNITY_DOTS_INSTANCED_PROP({0}, {1})";
		public static string DotsInstancedDefinesData
		{ 
			get
			{
				if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )ASESRPBaseline.ASE_SRP_13 )
				{
					return "#define {1} UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT({0} , {1})";
				}
				else if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )ASESRPBaseline.ASE_SRP_12 )
				{
					return "#define {1} UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO({0} , Metadata{1})";
				}
				else
				{
					return "#define {1} UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO({0} , Metadata_{1})";
				}
			}
		}

		public static readonly string LWSRPInstancedPropertiesBegin = "UNITY_INSTANCING_BUFFER_START({0})";
		public static readonly string LWSRPInstancedPropertiesEnd = "UNITY_INSTANCING_BUFFER_END({0})";
		public static readonly string LWSRPInstancedPropertiesElement = "UNITY_DEFINE_INSTANCED_PROP({0}, {1})";
		public static readonly string LWSRPInstancedPropertiesData = "UNITY_ACCESS_INSTANCED_PROP({0},{1})";

		public static readonly string SRPCBufferPropertiesBegin = "CBUFFER_START( UnityPerMaterial )";//"CBUFFER_START({0})";
		public static readonly string SRPCBufferPropertiesEnd = "CBUFFER_END";


		public static readonly string InstancedPropertiesBeginTabs = "\t\t" + InstancedPropertiesBegin + "\n";
		public static readonly string InstancedPropertiesEndTabs = "\t\t" + InstancedPropertiesEnd + "\n";
		public static readonly string InstancedPropertiesElementTabs = "\t\t\t" + InstancedPropertiesElement + "\n";

		public static readonly string MetaBegin = "defaultTextures:";
		public static readonly string MetaEnd = "userData:";
		public static readonly string ShaderBodyBegin = "/*ASEBEGIN";
		public static readonly string ShaderBodyEnd = "ASEEND*/";
		//public static readonly float CurrentVersionFlt = 0.4f;
		//public static readonly string CurrentVersionStr = "Version=" + CurrentVersionFlt;

		public static readonly string CHECKSUM = "//CHKSM";
		public static readonly string LAST_OPENED_OBJ_ID = "ASELASTOPENOBJID";

		public static readonly string MAT_CLIPBOARD_ID = "ASEMATCLIPBRDID";
		public static readonly char FIELD_SEPARATOR = ';';
		public static readonly char VALUE_SEPARATOR = '=';
		public static readonly char LINE_TERMINATOR = '\n';
		public static readonly char VECTOR_SEPARATOR = ',';
		public static readonly char FLOAT_SEPARATOR = '.';
		public static readonly char CLIPBOARD_DATA_SEPARATOR = '|';
		public static readonly char MATRIX_DATA_SEPARATOR = '|';
		public readonly static string NO_TEXTURES = "<None>";
		public static readonly string SaveShaderStr = "Please enter shader name to save";
		public static readonly string FloatifyStr = ".0";

		// Node parameter names
		public const string NodeParam = "Node";
		public const string NodePosition = "Position";
		public const string NodeId = "Id";
		public const string NodeType = "Type";
		public const string WireConnectionParam = "WireConnection";

		public static readonly uint NodeTypeId = 1;

		public static readonly int InNodeId = 1;
		public static readonly int InPortId = 2;
		public static readonly int OutNodeId = 3;
		public static readonly int OutPortId = 4;

		public readonly static string DefaultASEDirtyCheckName = "__dirty";
		public readonly static string DefaultASEDirtyCheckProperty = "[HideInInspector] " + DefaultASEDirtyCheckName + "( \"\", Int ) = 1";
		public readonly static string DefaultASEDirtyCheckUniform = "uniform int " + DefaultASEDirtyCheckName + " = 1;";

		public readonly static string MaskClipValueName = "_Cutoff";
		public readonly static string MaskClipValueProperty = MaskClipValueName + "( \"{0}\", Float ) = {1}";
		public readonly static string MaskClipValueUniform = "uniform float " + MaskClipValueName + " = {0};";

		public readonly static string ChromaticAberrationProperty = "_ChromaticAberration";

		//public static readonly string ASEFolderGUID = "daca988099666ec40aaa2cde22bb4935";
		//public static string ASEResourcesPath = "/Plugins/EditorResources/";
		//public static string ASEFolderPath;

		//public static bool IsShaderFunctionWindow = false;


		public static int DefaultASEDirtyCheckId;

		// this is to be used in combination with AssetDatabase.GetAssetPath, both of these include the Assets/ path so we need to remove from one of them 
		public static string dataPath;


		public static string EditorResourcesGUID = "0932db7ec1402c2489679c4b72eab5eb";
		public static string GraphBgTextureGUID = "881c304491028ea48b5027ac6c62cf73";
		public static string GraphFgTextureGUID = "8c4a7fca2884fab419769ccc0355c0c1";
		public static string WireTextureGUID = "06e687f68dd96f0448c6d8217bbcf608";
		public static string MasterNodeOnTextureGUID = "26c64fcee91024a49980ea2ee9d1a2fb";
		public static string MasterNodeOffTextureGUID = "712aee08d999c16438e2d694f42428e8";
		public static string GPUInstancedOnTextureGUID = "4b0c2926cc71c5846ae2a29652d54fb6";
		public static string GPUInstancedOffTextureGUID = "486c7766baaf21b46afb63c1121ef03e";
		public static string MainSkinGUID = "57482289c346f104a8162a3a79aaff9d";

		public static string UpdateOutdatedGUID = "cce638be049286c41bcbd0a26c356b18";
		public static string UpdateOFFGUID = "99d70ac09b4db9742b404c3f92d8564b";
		public static string UpdateUpToDatedGUID = "ce30b12fbb3223746bcfef9ea82effe3";
		public static string LiveOffGUID = "bb16faf366bcc6c4fbf0d7666b105354";
		public static string LiveOnGUID = "6a0ae1d7892333142aeb09585572202c";
		public static string LivePendingGUID = "e3182200efb67114eb5050f8955e1746";
		public static string CleanupOFFGUID = "f62c0c3a5ddcd844e905fb2632fdcb15";
		public static string CleanUpOnGUID = "615d853995cf2344d8641fd19cb09b5d";
		public static string TakeScreenshotOFFGUID = "7587de2e3bec8bf4d973109524ccc6b1";
		public static string TakeScreenshotONGUID = "7587de2e3bec8bf4d973109524ccc6b1";
		public static string ShareOFFGUID = "bc5bd469748466a459badfab23915cb0";
		public static string ShareONGUID = "bc5bd469748466a459badfab23915cb0";
		public static string OpenSourceCodeOFFGUID = "f7e8834b42791124095a8b7f2d4daac2";
		public static string OpenSourceCodeONGUID = "8b114792ff84f6546880c031eda42bc0";
		public static string FocusNodeGUID = "da673e6179c67d346abb220a6935e359";
		public static string FitViewGUID = "1def740f2314c6b4691529cadeee2e9c";
		public static string ShowInfoWindowGUID = "77af20044e9766840a6be568806dc22e";
		public static string ShowTipsWindowGUID = "066674048bbb1e64e8cdcc6c3b4abbeb";
		public static string ShowConsoleWindowGUID = "9a81d7df8e62c044a9d1cada0c8a2131";


		public static Dictionary<string , string> NodeTypeReplacer = new Dictionary<string , string>()
		{
			{"AmplifyShaderEditor.RotateAboutAxis", "AmplifyShaderEditor.RotateAboutAxisNode"},
			{"GlobalArrayNode", "AmplifyShaderEditor.GlobalArrayNode"},
			{"AmplifyShaderEditor.SimpleMaxOp", "AmplifyShaderEditor.SimpleMaxOpNode"},
			{"AmplifyShaderEditor.SimpleMinNode", "AmplifyShaderEditor.SimpleMinOpNode"},
			{"AmplifyShaderEditor.TFHCRemap", "AmplifyShaderEditor.TFHCRemapNode"},
			{"AmplifyShaderEditor.TFHCPixelateUV", "AmplifyShaderEditor.TFHCPixelate"},
			{"AmplifyShaderEditor.VirtualTexturePropertyNode", "AmplifyShaderEditor.VirtualTextureObject"}
		};

		private static readonly string AmplifyShaderEditorDefineSymbol = "AMPLIFY_SHADER_EDITOR";

		/////////////////////////////////////////////////////////////////////////////
		// THREAD IO UTILS
		public static bool SaveInThreadFlag = false;
		public static string SaveInThreadShaderBody;
		public static string SaveInThreadPathName;
		public static Thread SaveInThreadMainThread;
		public static bool ActiveThread = true;
		private static bool UseSaveThread = false;

		private static bool Initialized = false;

		public static bool FunctionNodeChanged = false;

		public static List<AmplifyShaderEditorWindow> AllOpenedWindows = new List<AmplifyShaderEditorWindow>();

		public static Type[] GetTypesInNamespace( Assembly assembly , string nameSpace )
		{
			return assembly.GetTypes().Where( t => String.Equals( t.Namespace , nameSpace , StringComparison.Ordinal ) ).ToArray();
		}

		public static List<Assembly> LoadedAssemblies;

		public static Type[] GetAssemblyTypesArray()
		{
			Type[] availableTypes = null;
			if( LoadedAssemblies == null )
			{
				LoadedAssemblies = new List<Assembly>();
				try
				{
					UnityEditor.Compilation.Assembly[] assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies( UnityEditor.Compilation.AssembliesType.Editor );
					for( int i = 0 ; i < assemblies.Length ; i++ )
					{
						if( !assemblies[ i ].name.StartsWith( "Unity" ) && !assemblies[ i ].name.Equals( "AmplifyShaderEditor" ) )
						{
							Assembly assembly = Assembly.Load( assemblies[ i ].name );
							LoadedAssemblies.Add( assembly );
							Type[] extraTypes = GetTypesInNamespace( assembly , "AmplifyShaderEditor" );
							if( extraTypes.Length > 0 )
							{
								availableTypes = ( availableTypes == null ) ? extraTypes : availableTypes.Concat<Type>( extraTypes ).ToArray();
							}
						}
					}
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
			else
			{
				int count = LoadedAssemblies.Count;
				for( int i = 0 ; i < count ; i++ )
				{
					Type[] extraTypes = GetTypesInNamespace( LoadedAssemblies[i] , "AmplifyShaderEditor" );
					if( extraTypes.Length > 0 )
					{
						availableTypes = ( availableTypes == null ) ? extraTypes : availableTypes.Concat<Type>( extraTypes ).ToArray();
					}
				}
			}

			return availableTypes;
		}

		public static Type GetAssemblyType( string typeName )
		{
			if( LoadedAssemblies == null )
			{
				LoadedAssemblies = new List<Assembly>();
				try
				{
					UnityEditor.Compilation.Assembly[] assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies( UnityEditor.Compilation.AssembliesType.Editor );
					for( int i = 0 ; i < assemblies.Length ; i++ )
					{
						if( !assemblies[ i ].name.StartsWith( "Unity" ) && !assemblies[ i ].name.Equals( "AmplifyShaderEditor" ) )
						{
							Assembly assembly = Assembly.Load( assemblies[ i ].name );
							LoadedAssemblies.Add( assembly );
							Type type = assembly.GetType( typeName );
							if( type != null )
								return type;
						}
					}
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
			else
			{
				int count = LoadedAssemblies.Count;
				for( int i = 0 ; i < count ; i++ )
				{
					Type type = LoadedAssemblies[i].GetType( typeName );
					if( type != null )
						return type;
				}
			}

			return null;
		}

		public static void ClearLoadedAssemblies()
		{
			if( LoadedAssemblies != null )
			{
				LoadedAssemblies.Clear();
				LoadedAssemblies = null;
			}
		}

		public static void StartSaveThread( string shaderBody , string pathName )
		{
			if( Provider.enabled && Provider.isActive )
			{
				Asset loadedAsset = Provider.GetAssetByPath( FileUtil.GetProjectRelativePath( pathName ) );
				if( loadedAsset != null )
				{
					//Task statusTask = Provider.Status( loadedAsset );
					//statusTask.Wait();
					//if( Provider.CheckoutIsValid( statusTask.assetList[ 0 ] ) )
					{
						Task checkoutTask = Provider.Checkout( loadedAsset , CheckoutMode.Both );
						checkoutTask.Wait();
					}
				}
			}

			if( UseSaveThread )
			{
				if( !SaveInThreadFlag )
				{
					if( SaveInThreadMainThread == null )
					{
						Worker worker = new Worker();
						SaveInThreadMainThread = new Thread( worker.DoWork );
						SaveInThreadMainThread.Start();
						Debug.Log( "Thread created" );
					}

					SaveInThreadShaderBody = shaderBody;
					SaveInThreadPathName = pathName;
					SaveInThreadFlag = true;
				}
			}
			else
			{
				SaveTextfileToDisk( shaderBody , pathName );
			}
		}

		////////////////////////////////////////////////////////////////////////////
		public static void SetAmplifyDefineSymbolOnBuildTargetGroup( BuildTargetGroup targetGroup )
		{
			string currData = PlayerSettings.GetScriptingDefineSymbolsForGroup( targetGroup );
			if( !currData.Contains( AmplifyShaderEditorDefineSymbol ) )
			{
				if( string.IsNullOrEmpty( currData ) )
				{
					PlayerSettings.SetScriptingDefineSymbolsForGroup( targetGroup , AmplifyShaderEditorDefineSymbol );
				}
				else
				{
					if( !currData[ currData.Length - 1 ].Equals( ';' ) )
					{
						currData += ';';
					}
					currData += AmplifyShaderEditorDefineSymbol;
					PlayerSettings.SetScriptingDefineSymbolsForGroup( targetGroup , currData );
				}
			}
		}

		public static void RemoveAmplifyDefineSymbolOnBuildTargetGroup( BuildTargetGroup targetGroup )
		{
			string currData = PlayerSettings.GetScriptingDefineSymbolsForGroup( targetGroup );
			if( currData.Contains( AmplifyShaderEditorDefineSymbol ) )
			{
				currData = currData.Replace( AmplifyShaderEditorDefineSymbol + ";" , "" );
				currData = currData.Replace( ";" + AmplifyShaderEditorDefineSymbol , "" );
				currData = currData.Replace( AmplifyShaderEditorDefineSymbol , "" );
				PlayerSettings.SetScriptingDefineSymbolsForGroup( targetGroup , currData );
			}
		}

		//Adding this attribute so scripting defining symbol can be registered right away so custom nodes using ASE ( under that symbol ) can be caught 
		// the first time ASE opens
		[InitializeOnLoadMethod]
		public static void Init()
		{
			if( !Initialized )
			{
				Initialized = true;
				if( EditorPrefs.GetBool( Preferences.PrefDefineSymbol , true ) )
					SetAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
				//Array BuildTargetGroupValues = Enum.GetValues( typeof(  BuildTargetGroup ));
				//for ( int i = 0; i < BuildTargetGroupValues.Length; i++ )
				//{
				//	if( i != 0 && i != 15 && i != 16 )
				//		SetAmplifyDefineSymbolOnBuildTargetGroup( ( BuildTargetGroup ) BuildTargetGroupValues.GetValue( i ) );
				//}

				DefaultASEDirtyCheckId = Shader.PropertyToID( DefaultASEDirtyCheckName );
				dataPath = Application.dataPath.Remove( Application.dataPath.Length - 6 );


				//ASEFolderPath = AssetDatabase.GUIDToAssetPath( ASEFolderGUID );
				//ASEResourcesPath = ASEFolderPath + ASEResourcesPath;
			}
		}


		public static void DumpTemplateManagers()
		{
			for( int i = 0 ; i < AllOpenedWindows.Count ; i++ )
			{
				if( AllOpenedWindows[ i ].TemplatesManagerInstance != null )
				{
					Debug.Log( AllOpenedWindows[ i ].titleContent.text + ": " + AllOpenedWindows[ i ].TemplatesManagerInstance.GetInstanceID() );
				}
			}
		}

		public static TemplatesManager FirstValidTemplatesManager
		{
			get
			{
				for( int i = 0 ; i < AllOpenedWindows.Count ; i++ )
				{
					if( AllOpenedWindows[ i ].TemplatesManagerInstance != null )
					{
						return AllOpenedWindows[ i ].TemplatesManagerInstance;
					}
				}
				return null;
			}
		}

		public static void UpdateSFandRefreshWindows( AmplifyShaderFunction function )
		{
			for( int i = 0 ; i < AllOpenedWindows.Count ; i++ )
			{
				AllOpenedWindows[ i ].LateRefreshAvailableNodes();
				if( AllOpenedWindows[ i ].IsShaderFunctionWindow )
				{
					if( AllOpenedWindows[ i ].OpenedShaderFunction == function )
					{
						AllOpenedWindows[ i ].UpdateTabTitle();
					}
				}
			}
		}

		public static void UpdateIO()
		{
			int windowCount = AllOpenedWindows.Count;
			if( windowCount == 0 )
			{
				EditorApplication.update -= IOUtils.UpdateIO;
				return;
			}

			for( int i = 0 ; i < AllOpenedWindows.Count ; i++ )
			{
				if( AllOpenedWindows[ i ] == EditorWindow.focusedWindow )
				{
					UIUtils.CurrentWindow = AllOpenedWindows[ i ];
				}

				if( FunctionNodeChanged )
					AllOpenedWindows[ i ].CheckFunctions = true;

				if( AllOpenedWindows[ i ] == null )
				{
					AllOpenedWindows.RemoveAt( i );
					i--;
				}
			}

			if( FunctionNodeChanged )
				FunctionNodeChanged = false;
		}

		public static void Destroy()
		{
			ActiveThread = false;
			if( SaveInThreadMainThread != null )
			{
				SaveInThreadMainThread.Abort();
				SaveInThreadMainThread = null;
			}
		}

		public static void GetShaderName( out string shaderName , out string fullPathname , string defaultName , string customDatapath )
		{
			string currDatapath = String.IsNullOrEmpty( customDatapath ) ? Application.dataPath : customDatapath;
			fullPathname = EditorUtility.SaveFilePanelInProject( "Select Shader to save" , defaultName , "shader" , SaveShaderStr , currDatapath );
			if( !String.IsNullOrEmpty( fullPathname ) )
			{
				shaderName = fullPathname.Remove( fullPathname.Length - 7 ); // -7 remove .shader extension
				string[] subStr = shaderName.Split( '/' );
				if( subStr.Length > 0 )
				{
					shaderName = subStr[ subStr.Length - 1 ]; // Remove pathname 
				}
			}
			else
			{
				shaderName = string.Empty;
			}
		}

		public static void AddTypeToString( ref string myString , string typeName )
		{
			myString += typeName;
		}

		public static void AddFieldToString( ref string myString , string fieldName , object fieldValue )
		{
			myString += FIELD_SEPARATOR + fieldName + VALUE_SEPARATOR + fieldValue;
		}

		public static void AddFieldValueToString( ref string myString , object fieldValue )
		{
			myString += FIELD_SEPARATOR + fieldValue.ToString();
		}

		public static void AddLineTerminator( ref string myString )
		{
			myString += LINE_TERMINATOR;
		}

		public static string CreateChecksum( string buffer )
		{
			SHA1 sha1 = SHA1.Create();
			byte[] buf = System.Text.Encoding.UTF8.GetBytes( buffer );
			byte[] hash = sha1.ComputeHash( buf , 0 , buf.Length );
			string hashstr = BitConverter.ToString( hash ).Replace( "-" , "" );
			return hashstr;
		}

		public static void SaveTextfileToDisk( string shaderBody , string pathName , bool addAdditionalInfo = true )
		{

			if( addAdditionalInfo )
			{
				shaderBody = string.Format( ShaderCopywriteMessage, VersionInfo.StaticToString() ) + shaderBody;
				// Add checksum 
				string checksum = CreateChecksum( shaderBody );
				shaderBody += CHECKSUM + VALUE_SEPARATOR + checksum;
			}

			// Write to disk
			StreamWriter fileWriter = new StreamWriter( pathName );
			try
			{
				fileWriter.Write( shaderBody );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
			finally
			{
				fileWriter.Close();
			}
		}

		public static string AddAdditionalInfo( string shaderBody )
		{
			shaderBody = string.Format( ShaderCopywriteMessage, VersionInfo.StaticToString() ) + shaderBody;
			string checksum = CreateChecksum( shaderBody );
			shaderBody += CHECKSUM + VALUE_SEPARATOR + checksum;
			return shaderBody;
		}

		public static string LoadTextFileFromDisk( string pathName )
		{
			string result = string.Empty;
			if( !string.IsNullOrEmpty( pathName ) && File.Exists( pathName ) )
			{

				StreamReader fileReader = null;
				try
				{
					fileReader = new StreamReader( pathName );
					result = fileReader.ReadToEnd();
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
				finally
				{
					if( fileReader != null )
						fileReader.Close();
				}
			}
			return result;
		}

		public static bool IsASEShader( Shader shader )
		{
			string datapath = AssetDatabase.GetAssetPath( shader );
			if( UIUtils.IsUnityNativeShader( datapath ) )
			{
				return false;
			}

			string buffer = LoadTextFileFromDisk( datapath );
			if( String.IsNullOrEmpty( buffer ) || !IOUtils.HasValidShaderBody( ref buffer ) )
			{
				return false;
			}
			return true;
		}

		public static bool IsShaderFunction( string functionInfo )
		{
			string buffer = functionInfo;
			if( String.IsNullOrEmpty( buffer ) || !IOUtils.HasValidShaderBody( ref buffer ) )
			{
				return false;
			}
			return true;
		}

		public static bool HasValidShaderBody( ref string shaderBody )
		{
			int shaderBodyBeginId = shaderBody.IndexOf( ShaderBodyBegin );
			if( shaderBodyBeginId > -1 )
			{
				int shaderBodyEndId = shaderBody.IndexOf( ShaderBodyEnd );
				return ( shaderBodyEndId > -1 && shaderBodyEndId > shaderBodyBeginId );
			}
			return false;
		}

		public static int[] AllIndexesOf( this string str , string substr , bool ignoreCase = false )
		{
			if( string.IsNullOrEmpty( str ) || string.IsNullOrEmpty( substr ) )
			{
				throw new ArgumentException( "String or substring is not specified." );
			}

			List<int> indexes = new List<int>();
			int index = 0;

			while( ( index = str.IndexOf( substr , index , ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal ) ) != -1 )
			{
				indexes.Add( index++ );
			}

			return indexes.ToArray();
		}

		public static void AddFunctionHeader( ref string function , string header )
		{
			function += "\t\t" + header + "\n\t\t{\n";
		}

		public static void AddSingleLineFunction( ref string function , string header )
		{
			function += "\t\t" + header;
		}

		public static void AddFunctionLine( ref string function , string line )
		{
			function += "\t\t\t" + line + "\n";
		}

		public static void CloseFunctionBody( ref string function )
		{
			function += "\t\t}\n";
		}

		public static string CreateFullFunction( string header , params string[] functionLines )
		{
			string result = string.Empty;
			AddFunctionHeader( ref result , header );
			for( int i = 0 ; i > functionLines.Length ; i++ )
			{
				AddFunctionLine( ref result , functionLines[ i ] );
			}
			CloseFunctionBody( ref result );
			return result;
		}

		public static string CreateCodeComments( bool forceForwardSlash , params string[] comments )
		{
			string finalComment = string.Empty;
			if( comments.Length == 1 )
			{
				finalComment = "//" + comments[ 0 ];
			}
			else
			{
				if( forceForwardSlash )
				{
					for( int i = 0 ; i < comments.Length ; i++ )
					{
						finalComment += "//" + comments[ i ];
						if( i < comments.Length - 1 )
						{
							finalComment += "\n\t\t\t";
						}
					}
				}
				else
				{
					finalComment = "/*";
					for( int i = 0 ; i < comments.Length ; i++ )
					{
						if( i != 0 )
							finalComment += "\t\t\t";
						finalComment += comments[ i ];
						if( i < comments.Length - 1 )
							finalComment += "\n";
					}
					finalComment += "*/";
				}
			}
			return finalComment;
		}

		public static string GetUVChannelDeclaration( string uvName , int channelId , int set )
		{
			string uvSetStr = ( set == 0 ) ? "uv" : "uv" + Constants.AvailableUVSetsStr[ set ];
			return "float2 " + uvSetStr + uvName /*+ " : TEXCOORD" + channelId*/;
		}

		public static string GetUVChannelName( string uvName , int set )
		{
			string uvSetStr = ( set == 0 ) ? "uv" : "uv" + Constants.AvailableUVSetsStr[ set ];
			return uvSetStr + uvName;
		}

		public static string GetVertexUVChannelName( int set )
		{
			string uvSetStr = ( set == 0 ) ? "texcoord" : ( "texcoord" + set.ToString() );
			return uvSetStr;
		}

		//Floatify adds a .0 to the number as soon operarions with floats require that
		// if  value % 1 != 0 it has decimal numbers
		// The regex checks if number is something like 4e+07 which cannot be "floatified"
		private const string CheckFloatifyPatt = "[eE][+-]";
		public static string Floatify( float value )
		{
			string finalValue = value.ToString();
			return ( value % 1 ) != 0 ? finalValue :
				( Regex.IsMatch( finalValue , CheckFloatifyPatt ) ? finalValue : finalValue + FloatifyStr );
		}

		public static string Vector2ToString( Vector2 data )
		{
			return data.x.ToString() + VECTOR_SEPARATOR + data.y.ToString();
		}

		public static string Vector3ToString( Vector3 data )
		{
			return data.x.ToString() + VECTOR_SEPARATOR + data.y.ToString() + VECTOR_SEPARATOR + data.z.ToString();
		}

		public static string Vector4ToString( Vector4 data )
		{
			return data.x.ToString() + VECTOR_SEPARATOR + data.y.ToString() + VECTOR_SEPARATOR + data.z.ToString() + VECTOR_SEPARATOR + data.w.ToString();
		}

		public static string ColorToString( Color data )
		{
			return data.r.ToString() + VECTOR_SEPARATOR + data.g.ToString() + VECTOR_SEPARATOR + data.b.ToString() + VECTOR_SEPARATOR + data.a.ToString();
		}

		public static string Matrix3x3ToString( Matrix4x4 matrix )
		{
			return matrix[ 0 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 0 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 0 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR +
					matrix[ 1 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 1 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 1 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR +
					matrix[ 2 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 2 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 2 , 2 ].ToString();
		}

		public static string Matrix4x4ToString( Matrix4x4 matrix )
		{
			return matrix[ 0 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 0 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 0 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 0 , 3 ].ToString() + IOUtils.VECTOR_SEPARATOR +
					matrix[ 1 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 1 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 1 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 1 , 3 ].ToString() + IOUtils.VECTOR_SEPARATOR +
					matrix[ 2 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 2 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 2 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 2 , 3 ].ToString() + IOUtils.VECTOR_SEPARATOR +
					matrix[ 3 , 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 3 , 1 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 3 , 2 ].ToString() + IOUtils.VECTOR_SEPARATOR + matrix[ 3 , 3 ].ToString();
		}

		public static Vector2 StringToVector2( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length >= 2 )
			{
				return new Vector2( Convert.ToSingle( parsedData[ 0 ] ) ,
									Convert.ToSingle( parsedData[ 1 ] ) );
			}
			return Vector2.zero;
		}

		public static Vector3 StringToVector3( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length >= 3 )
			{
				return new Vector3( Convert.ToSingle( parsedData[ 0 ] ) ,
									Convert.ToSingle( parsedData[ 1 ] ) ,
									Convert.ToSingle( parsedData[ 2 ] ) );
			}
			return Vector3.zero;
		}

		public static Vector4 StringToVector4( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length >= 4 )
			{
				return new Vector4( Convert.ToSingle( parsedData[ 0 ] ) ,
									Convert.ToSingle( parsedData[ 1 ] ) ,
									Convert.ToSingle( parsedData[ 2 ] ) ,
									Convert.ToSingle( parsedData[ 3 ] ) );
			}
			return Vector4.zero;
		}

		public static Color StringToColor( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length >= 4 )
			{
				return new Color( Convert.ToSingle( parsedData[ 0 ] ) ,
									Convert.ToSingle( parsedData[ 1 ] ) ,
									Convert.ToSingle( parsedData[ 2 ] ) ,
									Convert.ToSingle( parsedData[ 3 ] ) );
			}
			return Color.white;
		}

		public static Matrix4x4 StringToMatrix3x3( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length == 9 )
			{
				Matrix4x4 matrix = new Matrix4x4();
				matrix[ 0 , 0 ] = Convert.ToSingle( parsedData[ 0 ] );
				matrix[ 0 , 1 ] = Convert.ToSingle( parsedData[ 1 ] );
				matrix[ 0 , 2 ] = Convert.ToSingle( parsedData[ 2 ] );

				matrix[ 1 , 0 ] = Convert.ToSingle( parsedData[ 3 ] );
				matrix[ 1 , 1 ] = Convert.ToSingle( parsedData[ 4 ] );
				matrix[ 1 , 2 ] = Convert.ToSingle( parsedData[ 5 ] );

				matrix[ 2 , 0 ] = Convert.ToSingle( parsedData[ 6 ] );
				matrix[ 2 , 1 ] = Convert.ToSingle( parsedData[ 7 ] );
				matrix[ 2 , 2 ] = Convert.ToSingle( parsedData[ 8 ] );
				return matrix;
			}
			return Matrix4x4.identity;
		}

		public static Matrix4x4 StringToMatrix4x4( string data )
		{
			string[] parsedData = data.Split( VECTOR_SEPARATOR );
			if( parsedData.Length == 16 )
			{
				Matrix4x4 matrix = new Matrix4x4();
				matrix[ 0 , 0 ] = Convert.ToSingle( parsedData[ 0 ] );
				matrix[ 0 , 1 ] = Convert.ToSingle( parsedData[ 1 ] );
				matrix[ 0 , 2 ] = Convert.ToSingle( parsedData[ 2 ] );
				matrix[ 0 , 3 ] = Convert.ToSingle( parsedData[ 3 ] );

				matrix[ 1 , 0 ] = Convert.ToSingle( parsedData[ 4 ] );
				matrix[ 1 , 1 ] = Convert.ToSingle( parsedData[ 5 ] );
				matrix[ 1 , 2 ] = Convert.ToSingle( parsedData[ 6 ] );
				matrix[ 1 , 3 ] = Convert.ToSingle( parsedData[ 7 ] );

				matrix[ 2 , 0 ] = Convert.ToSingle( parsedData[ 8 ] );
				matrix[ 2 , 1 ] = Convert.ToSingle( parsedData[ 9 ] );
				matrix[ 2 , 2 ] = Convert.ToSingle( parsedData[ 10 ] );
				matrix[ 2 , 3 ] = Convert.ToSingle( parsedData[ 11 ] );

				matrix[ 3 , 0 ] = Convert.ToSingle( parsedData[ 12 ] );
				matrix[ 3 , 1 ] = Convert.ToSingle( parsedData[ 13 ] );
				matrix[ 3 , 2 ] = Convert.ToSingle( parsedData[ 14 ] );
				matrix[ 3 , 3 ] = Convert.ToSingle( parsedData[ 15 ] );
				return matrix;
			}
			return Matrix4x4.identity;
		}

		public static void SaveTextureToDisk( Texture2D tex , string pathname )
		{
			byte[] rawData = tex.GetRawTextureData();
			Texture2D newTex = new Texture2D( tex.width , tex.height , tex.format , tex.mipmapCount > 1 , false );
			newTex.LoadRawTextureData( rawData );
			newTex.Apply();
			byte[] pngData = newTex.EncodeToPNG();
			File.WriteAllBytes( pathname , pngData );
		}

		//public static void SaveObjToList( string newObj )
		//{
		//	Debug.Log( UIUtils.CurrentWindow.Lastpath );
		//	UIUtils.CurrentWindow.Lastpath = newObj;
		//	string lastOpenedObj = EditorPrefs.GetString( IOUtils.LAST_OPENED_OBJ_ID );
		//	string[] allLocations = lastOpenedObj.Split( ':' );

		//	string lastLocation = allLocations[ allLocations.Length - 1 ];

		//	string resave = string.Empty;
		//	for ( int i = 0; i < allLocations.Length; i++ )
		//	{
		//		if ( string.IsNullOrEmpty( allLocations[ i ] ) )
		//			continue;

		//		resave += allLocations[ i ];
		//		resave += ":";
		//	}

		//	resave += newObj;
		//	EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, resave );
		//}

		//public static void DeleteObjFromList( string newObj )
		//{
		//	string lastOpenedObj = EditorPrefs.GetString( IOUtils.LAST_OPENED_OBJ_ID );
		//	string[] allLocations = lastOpenedObj.Split( ':' );

		//	string resave = string.Empty;
		//	for ( int i = 0; i < allLocations.Length; i++ )
		//	{
		//		if ( string.IsNullOrEmpty( allLocations[ i ] ) || newObj.Equals( allLocations[ i ] ) )
		//			continue;

		//		resave += allLocations[ i ];
		//		if ( i < allLocations.Length - 1 )
		//			resave += ":";
		//	}

		//	EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, resave );
		//}

		// Polynomial: 0xedb88320
		static readonly uint[] crc32_tab = {
			0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f,
			0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
			0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91, 0x1db71064, 0x6ab020f2,
			0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
			0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
			0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
			0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b, 0x35b5a8fa, 0x42b2986c,
			0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
			0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423,
			0xcfba9599, 0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
			0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190, 0x01db7106,
			0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
			0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d,
			0x91646c97, 0xe6635c01, 0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
			0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
			0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
			0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7,
			0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
			0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa,
			0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
			0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81,
			0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
			0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683, 0xe3630b12, 0x94643b84,
			0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
			0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
			0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
			0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5, 0xd6d6a3e8, 0xa1d1937e,
			0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
			0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55,
			0x316e8eef, 0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
			0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe, 0xb2bd0b28,
			0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
			0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f,
			0x72076785, 0x05005713, 0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
			0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
			0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
			0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69,
			0x616bffd3, 0x166ccf45, 0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
			0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc,
			0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
			0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693,
			0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
			0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
		};

		public static uint CRC32( byte[] buf, uint crc = 0 )
		{
			uint i = 0;
			uint size = ( uint )buf.Length;
			crc = crc ^ 0xFFFFFFFF;
			while ( size-- > 0 )
			{
				crc = crc32_tab[ ( crc ^ buf[ i++ ] ) & 0xFF ] ^ ( crc >> 8 );
			}
			return crc ^ 0xFFFFFFFF;
		}
	}
}
