// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{
	public enum ASEImportFlags
	{
		None = 0,
		URP  = 1 << 0,
		HDRP = 1 << 1,
		Both = URP | HDRP
	}

	public static class AssetDatabaseEX
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.AssetDatabase, UnityEditor" ) : type; } }

		public static void ImportPackageImmediately( string packagePath )
		{
			AssetDatabaseEX.Type.InvokeMember( "ImportPackageImmediately", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { packagePath } );
		}
	}

	public enum ASESRPBaseline
	{
		ASE_SRP_INVALID = 0,
		ASE_SRP_10 = 100000,
		ASE_SRP_11 = 110000,
		ASE_SRP_12 = 120000,
		ASE_SRP_13 = 130000,
		ASE_SRP_14 = 140000,
		ASE_SRP_15 = 150000
	}

	public class ASESRPPackageDesc
	{
		public ASESRPBaseline baseline = ASESRPBaseline.ASE_SRP_INVALID;
		public string guidURP = string.Empty;
		public string guidHDRP = string.Empty;

		public ASESRPPackageDesc( ASESRPBaseline baseline, string guidURP, string guidHDRP )
		{
			this.baseline = baseline;
			this.guidURP = guidURP;
			this.guidHDRP = guidHDRP;
		}
	}

	[Serializable]
	[InitializeOnLoad]
	public static class ASEPackageManagerHelper
	{
		private static string URPPackageId  = "com.unity.render-pipelines.universal";
		private static string HDRPPackageId = "com.unity.render-pipelines.high-definition";

		private static string NewVersionDetectedFormat = "A new {0} version {1} was detected and new templates are being imported.\nPlease hit the Update button on your ASE canvas to recompile your shader under the newest version.";
		private static string PackageBaseFormat = "ASE_PkgBase_{0}_{1}";
		private static string PackageCRCFormat = "ASE_PkgCRC_{0}_{1}";

		private static string SRPKeywordFormat = "ASE_SRP_VERSION {0}";

		private static Dictionary<int, ASESRPPackageDesc> m_srpPackageSupport = new Dictionary<int,ASESRPPackageDesc>()
		{
			{ ( int )ASESRPBaseline.ASE_SRP_10, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_10, "b460b52e6c1feae45b70b7ddc2c45bd6", "2243c8b4e1ab6914995699133f67ab5a" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_11, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_11, "b460b52e6c1feae45b70b7ddc2c45bd6", "2243c8b4e1ab6914995699133f67ab5a" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_12, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_12, "57fcea0ed8b5eb347923c4c21fa31b57", "9a5e61a8b3421b944863d0946e32da0a" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_13, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_13, "57fcea0ed8b5eb347923c4c21fa31b57", "9a5e61a8b3421b944863d0946e32da0a" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_14, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_14, "2e9da72e7e3196146bf7d27450013734", "89f0b84148d149d4d96b838d7ef60e92" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_15, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_15, "2e9da72e7e3196146bf7d27450013734", "89f0b84148d149d4d96b838d7ef60e92" ) },
		};

		private static Shader m_lateShader;
		private static Material m_lateMaterial;
		private static AmplifyShaderFunction m_lateShaderFunction;

		private static ListRequest m_packageListRequest = null;
		private static UnityEditor.PackageManager.PackageInfo m_urpPackageInfo;
		private static UnityEditor.PackageManager.PackageInfo m_hdrpPackageInfo;

		public static bool FoundURPVersion { get { return m_urpPackageInfo != null; } }
		public static bool FoundHDRPVersion { get { return m_hdrpPackageInfo != null; } }

		private static bool m_lateImport = false;
		private static string m_latePackageToImport;
		private static bool m_requireUpdateList = false;
		private static ASEImportFlags m_importingPackage = ASEImportFlags.None;

		public static bool CheckImporter { get { return m_importingPackage != ASEImportFlags.None; } }
		public static bool IsProcessing { get { return m_requireUpdateList && m_importingPackage == ASEImportFlags.None; } }

		private static ASESRPBaseline m_currentURPBaseline = ASESRPBaseline.ASE_SRP_INVALID;
		private static ASESRPBaseline m_currentHDRPBaseline = ASESRPBaseline.ASE_SRP_INVALID;

		public static ASESRPBaseline CurrentURPBaseline { get { return m_currentURPBaseline; } }
		public static ASESRPBaseline CurrentHDRPBaseline { get { return m_currentHDRPBaseline; } }

		private static int m_packageURPVersion = -1; // @diogo: starts as missing
		private static int m_packageHDRPVersion = -1;

		public static int PackageSRPVersion { get { return ( m_packageHDRPVersion >= m_packageURPVersion ) ? m_packageHDRPVersion : m_packageURPVersion; } }
		public static int CurrentSRPVersion { get { return UIUtils.CurrentWindow.MainGraphInstance.IsSRP ? PackageSRPVersion : -1; } }

		private static string m_projectName = null;
		private static string ProjectName
		{
			get
			{
				if ( string.IsNullOrEmpty( m_projectName ) )
				{
					string[] s = Application.dataPath.Split( '/' );
					m_projectName = s[ s.Length - 2 ];
				}
				return m_projectName;
			}
		}

		static ASEPackageManagerHelper()
		{
			RequestInfo( true );
		}

		static void WaitForPackageListBeforeUpdating()
		{
			if ( m_packageListRequest.IsCompleted )
			{
				Update();
				EditorApplication.update -= WaitForPackageListBeforeUpdating;
			}
		}

		public static void RequestInfo( bool updateWhileWaiting = false )
		{
			if ( !m_requireUpdateList && m_importingPackage == ASEImportFlags.None )
			{
				m_requireUpdateList = true;
				m_packageListRequest = UnityEditor.PackageManager.Client.List( true );
				if ( updateWhileWaiting )
				{
					EditorApplication.update += WaitForPackageListBeforeUpdating;
				}
			}
		}

		static void FailedPackageImport( string packageName, string errorMessage )
		{
			FinishImporter();
		}

		static void CancelledPackageImport( string packageName )
		{
			FinishImporter();
		}

		static void CompletedPackageImport( string packageName )
		{
			FinishImporter();
		}

		public static void CheckLatePackageImport()
		{
			if ( !Application.isPlaying && m_lateImport && !string.IsNullOrEmpty( m_latePackageToImport ) )
			{
				m_lateImport = false;
				StartImporting( m_latePackageToImport );
				m_latePackageToImport = string.Empty;
			}
		}

		public static void StartImporting( string packagePath )
		{
			if ( !Preferences.GlobalAutoSRP )
			{
				m_importingPackage = ASEImportFlags.None;
				return;
			}

			if ( Application.isPlaying )
			{
				if ( !m_lateImport )
				{
					m_lateImport = true;
					m_latePackageToImport = packagePath;
					Debug.LogWarning( "Amplify Shader Editor requires the \"" + packagePath + "\" package to be installed in order to continue. Please exit Play mode to proceed." );
				}
				return;
			}

			AssetDatabase.importPackageCancelled += CancelledPackageImport;
			AssetDatabase.importPackageCompleted += CompletedPackageImport;
			AssetDatabase.importPackageFailed += FailedPackageImport;
			AssetDatabase.ImportPackage( packagePath, false );
			//AssetDatabaseEX.ImportPackageImmediately( packagePath );
		}

		public static void FinishImporter()
		{
			m_importingPackage = ASEImportFlags.None;
			AssetDatabase.importPackageCancelled -= CancelledPackageImport;
			AssetDatabase.importPackageCompleted -= CompletedPackageImport;
			AssetDatabase.importPackageFailed -= FailedPackageImport;
		}

		public static void SetupLateShader( Shader shader )
		{
			if ( shader == null )
				return;

			//If a previous delayed object is pending discard it and register the new one
			// So the last selection will be the choice of opening
			//This can happen when trying to open an ASE canvas while importing templates or in play mode
			if ( m_lateShader != null )
			{
				EditorApplication.delayCall -= LateShaderOpener;
			}

			RequestInfo();
			m_lateShader = shader;
			EditorApplication.delayCall += LateShaderOpener;
		}

		public static void LateShaderOpener()
		{
			Update();
			if ( IsProcessing )
			{
				EditorApplication.delayCall += LateShaderOpener;
			}
			else
			{
				AmplifyShaderEditorWindow.ConvertShaderToASE( m_lateShader );
				m_lateShader = null;
			}
		}

		public static void SetupLateMaterial( Material material )
		{
			if ( material == null )
				return;

			//If a previous delayed object is pending discard it and register the new one
			// So the last selection will be the choice of opening
			//This can happen when trying to open an ASE canvas while importing templates or in play mode
			if ( m_lateMaterial != null )
			{
				EditorApplication.delayCall -= LateMaterialOpener;
			}

			RequestInfo();
			m_lateMaterial = material;
			EditorApplication.delayCall += LateMaterialOpener;
		}

		public static void LateMaterialOpener()
		{
			Update();
			if ( IsProcessing )
			{
				EditorApplication.delayCall += LateMaterialOpener;
			}
			else
			{
				AmplifyShaderEditorWindow.LoadMaterialToASE( m_lateMaterial );
				m_lateMaterial = null;
			}
		}

		public static void SetupLateShaderFunction( AmplifyShaderFunction shaderFunction )
		{
			if ( shaderFunction == null )
				return;

			//If a previous delayed object is pending discard it and register the new one
			// So the last selection will be the choice of opening
			//This can happen when trying to open an ASE canvas while importing templates or in play mode
			if ( m_lateShaderFunction != null )
			{
				EditorApplication.delayCall -= LateShaderFunctionOpener;
			}

			RequestInfo();
			m_lateShaderFunction = shaderFunction;
			EditorApplication.delayCall += LateShaderFunctionOpener;
		}

		public static void LateShaderFunctionOpener()
		{
			Update();
			if ( IsProcessing )
			{
				EditorApplication.delayCall += LateShaderFunctionOpener;
			}
			else
			{
				AmplifyShaderEditorWindow.LoadShaderFunctionToASE( m_lateShaderFunction, false );
				m_lateShaderFunction = null;
			}
		}

		private static readonly string SemVerPattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

		private static int PackageVersionStringToCode( string version, out int major, out int minor, out int patch )
		{
			MatchCollection matches = Regex.Matches( version, SemVerPattern, RegexOptions.Multiline );

			bool validMatch = ( matches.Count > 0 && matches[ 0 ].Groups.Count >= 4 );
			major = validMatch ? int.Parse( matches[ 0 ].Groups[ 1 ].Value ) : 99;
			minor = validMatch ? int.Parse( matches[ 0 ].Groups[ 2 ].Value ) : 99;
			patch = validMatch ? int.Parse( matches[ 0 ].Groups[ 3 ].Value ) : 99;

			int versionCode;
			versionCode = major * 10000;
			versionCode += minor * 100;
			versionCode += patch;
			return versionCode;
		}

		private static void CodeToPackageVersionElements( int versionCode, out int major, out int minor, out int patch )
		{
			major = versionCode / 10000;
			minor = versionCode / 100 - major * 100;
			patch = versionCode - ( versionCode / 100 ) * 100;
		}

		private static int PackageVersionElementsToCode( int major, int minor, int patch )
		{
			return major * 10000 + minor * 100 + patch;
		}

		private static void CheckPackageImport( ASEImportFlags flag, ASESRPBaseline baseline, string guid, string version )
		{
			Debug.Assert( flag == ASEImportFlags.HDRP || flag == ASEImportFlags.URP );

			string path = AssetDatabase.GUIDToAssetPath( guid );

			if ( !string.IsNullOrEmpty( path ) && File.Exists( path ) )
			{
				uint currentCRC = IOUtils.CRC32( File.ReadAllBytes( path ) );

				string srpName = flag.ToString();
				string packageBaseKey = string.Format( PackageBaseFormat, srpName, ProjectName );
				string packageCRCKey = string.Format( PackageCRCFormat, srpName, ProjectName );

				ASESRPBaseline savedBaseline = ( ASESRPBaseline )EditorPrefs.GetInt( packageBaseKey );
				uint savedCRC = ( uint )EditorPrefs.GetInt( packageCRCKey, 0 );

				bool foundNewVersion = ( savedBaseline != baseline ) || ( savedCRC != currentCRC );

				EditorPrefs.SetInt( packageBaseKey, ( int )baseline );
				EditorPrefs.SetInt( packageCRCKey, ( int )currentCRC );

				string testPath0 = string.Empty;
				string testPath1 = string.Empty;

				switch ( flag )
				{
					case ASEImportFlags.URP:
					{
						testPath0 = AssetDatabase.GUIDToAssetPath( TemplatesManager.URPLitGUID );
						testPath1 = AssetDatabase.GUIDToAssetPath( TemplatesManager.URPUnlitGUID );
						break;
					}
					case ASEImportFlags.HDRP:
					{
						testPath0 = AssetDatabase.GUIDToAssetPath( TemplatesManager.HDRPLitGUID );
						testPath1 = AssetDatabase.GUIDToAssetPath( TemplatesManager.HDRPUnlitGUID );
						break;
					}
				}

				if ( !File.Exists( testPath0 ) || !File.Exists( testPath1 ) || foundNewVersion )
				{
					if ( foundNewVersion )
					{
						Debug.Log( string.Format( NewVersionDetectedFormat, srpName, version ) );
					}
					m_importingPackage |= flag;
					StartImporting( path );
				}
			}
		}

		public static void Update()
		{
			CheckLatePackageImport();

			if ( m_requireUpdateList && m_importingPackage == ASEImportFlags.None )
			{
				if ( m_packageListRequest != null && m_packageListRequest.IsCompleted )
				{
					m_requireUpdateList = false;
					foreach ( UnityEditor.PackageManager.PackageInfo pi in m_packageListRequest.Result )
					{
						int version = PackageVersionStringToCode( pi.version, out int major, out int minor, out int patch );
						int baseline = PackageVersionElementsToCode( major, 0, 0 );
						ASESRPPackageDesc match;

						if ( pi.name.Equals( URPPackageId ) && m_srpPackageSupport.TryGetValue( baseline, out match ) )
						{
							// Universal Rendering Pipeline
							m_currentURPBaseline = match.baseline;
							m_packageURPVersion = version;
							m_urpPackageInfo = pi;

							CheckPackageImport( ASEImportFlags.URP, match.baseline, match.guidURP, pi.version );
						}
						else if ( pi.name.Equals( HDRPPackageId ) && m_srpPackageSupport.TryGetValue( baseline, out match ) )
						{
							// High-Definition Rendering Pipeline
							m_currentHDRPBaseline = match.baseline;
							m_packageHDRPVersion = version;
							m_hdrpPackageInfo = pi;

							CheckPackageImport( ASEImportFlags.HDRP, match.baseline, match.guidHDRP, pi.version );
						}
					}
				}
			}
		}

		public static void SetSRPInfoOnDataCollector( ref MasterNodeDataCollector dataCollector )
		{
			if ( m_requireUpdateList )
			{
				Update();
			}

			if ( dataCollector.CurrentSRPType == TemplateSRPType.HDRP )
			{
				dataCollector.AddToDirectives( string.Format( SRPKeywordFormat, m_packageHDRPVersion ), -1, AdditionalLineType.Define );
			}
			else if ( dataCollector.CurrentSRPType == TemplateSRPType.URP )
			{
				dataCollector.AddToDirectives( string.Format( SRPKeywordFormat, m_packageURPVersion ), -1, AdditionalLineType.Define );
			}
		}
	}
}
