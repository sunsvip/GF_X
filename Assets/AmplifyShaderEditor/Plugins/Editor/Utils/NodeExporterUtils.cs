using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

namespace AmplifyShaderEditor
{
	public enum DebugScreenShotNodeState
	{
		CreateNode,
		FocusOnNode,
		TakeScreenshot,
		WaitFrame,
		DeleteNode
	};

	public enum DebugUndoNodeState
	{
		CreateNode,
		FocusOnNode,
		WaitFrameCreate,
		DeleteNode,
		WaitFrameDelete,
		UndoNode,
		WaitFrameUndo,
		PrepareForNext
	};


	public class NodeExporterUtils
	{
		//Auto-Screenshot nodes
		private RenderTexture m_screenshotRT;
		private Texture2D m_screenshotTex2D;
		private List<ContextMenuItem> m_screenshotList = new List<ContextMenuItem>();
		private DebugScreenShotNodeState m_screenShotState;
		private bool m_takingShots = false;

		private DebugUndoNodeState m_undoState;
		private bool m_testingUndo = false;


		private AmplifyShaderEditorWindow m_window;
		private ParentNode m_node;


		private string m_pathname;

		public NodeExporterUtils( AmplifyShaderEditorWindow window )
		{
			m_window = window;
			UndoUtils.RegisterUndoRedoCallback( OnUndoRedoPerformed );
		}

		public void OnUndoRedoPerformed()
		{
			if( m_testingUndo && m_undoState == DebugUndoNodeState.WaitFrameUndo )
			{
				m_undoState = DebugUndoNodeState.PrepareForNext;
			}
		}

		public void CalculateShaderInstructions( Shader shader )
		{
			//Type shaderutilType = Type.GetType( "UnityEditor.ShaderUtil, UnityEditor" );
			//shaderutilType.InvokeMember( "OpenCompiledShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, mode, customPlatformsMask, includeAllVariants } );
		}

		public void ActivateAutoScreenShot( string pathname, int from, int to )
		{

			m_pathname = pathname;
			if( !System.IO.Directory.Exists( m_pathname ) )
			{
				System.IO.Directory.CreateDirectory( m_pathname );
			}

			m_screenshotRT = new RenderTexture( (int)m_window.position.width, (int)m_window.position.height, 0 );
			m_screenshotTex2D = new Texture2D( (int)m_window.position.width, (int)m_window.position.height, TextureFormat.RGB24, false );

			RenderTexture.active = m_screenshotRT;
			m_window.CurrentPaletteWindow.FillList( ref m_screenshotList, true );
			m_window.CurrentGraph.ClearGraph();
			if( m_window.IsShaderFunctionWindow )
			{
				m_window.CurrentGraph.CurrentOutputNode.Vec2Position = new Vector2( 1500, 0 );
			}
			else
			{
				m_window.CurrentGraph.CurrentMasterNode.Vec2Position = new Vector2( 1500, 0 );
			}
			m_window.ResetCameraSettings();

			m_takingShots = true;
			m_screenShotState = DebugScreenShotNodeState.CreateNode;

		}

		public void ActivateNodesURL( int from , int to )
		{
			m_window.CurrentPaletteWindow.FillList( ref m_screenshotList, true );
			
			if( to < 0 || to > m_screenshotList.Count )
				to  = m_screenshotList.Count;

			if( from >= to )
				return;

			for( int i = from; i < to; i++ )
			{
				if( m_screenshotList[ i ].NodeType != typeof( FunctionNode ) )
				{
					Application.OpenURL( m_screenshotList[ i ].NodeAttributes.NodeUrl );
				}
			}
		}

		public void ActivateAutoUndo()
		{
			m_window.CurrentPaletteWindow.FillList( ref m_screenshotList, true );
			m_window.CurrentGraph.ClearGraph();
			m_window.CurrentGraph.CurrentMasterNode.Vec2Position = new Vector2( 1500, 0 );
			m_window.ResetCameraSettings();

			m_testingUndo = true;
			m_undoState = DebugUndoNodeState.CreateNode;
		}

		private Type[] GetTypesInNamespace( Assembly assembly , string nameSpace )
		{
			return assembly.GetTypes().Where( t => String.Equals( t.Namespace , nameSpace , StringComparison.Ordinal ) ).ToArray();
		}

		public void GenerateNodesCSV( string path )
		{
			path += "AvailableNodesCSV.txt";
			StringBuilder result = new StringBuilder();
			result.AppendLine( "Nodes" );
			result.AppendLine( "Name,Updated" );
			try
			{
				//IEnumerable<System.Type> availableTypes = AppDomain.CurrentDomain.GetAssemblies().ToList().SelectMany( type => type.GetTypes() );
				var mainAssembly = Assembly.GetExecutingAssembly();
				Type[] availableTypes = GetTypesInNamespace( mainAssembly , "AmplifyShaderEditor" );
				foreach( System.Type type in availableTypes )
				{
					foreach( NodeAttributes attribute in Attribute.GetCustomAttributes( type ).OfType<NodeAttributes>() )
					{
						if( attribute.Available && !attribute.Deprecated )
						{
							result.AppendLine( attribute.Name + ", false" );
						}
					}
				}
			}
			catch( ReflectionTypeLoadException exception )
			{
				Debug.LogException( exception );
			}

			result.AppendLine();
			result.AppendLine( "Shader Functions" );
			result.AppendLine( "Name,Updated" );
			string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderFunction" );
			for( int i = 0 ; i < guids.Length ; i++ )
			{
				AmplifyShaderFunction sf = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( AssetDatabase.GUIDToAssetPath( guids[ i ] ) );
				result.AppendLine( sf.FunctionName + ",false" );
			}

			IOUtils.SaveTextfileToDisk( result.ToString() , path , false );
			Debug.Log( "Available nodes CSV saved to: " + path );
		}

		public void Update()
		{
			if( m_testingUndo )
			{
				if( Event.current.type == EventType.Repaint )
				{
					m_window.Focus();
					switch( m_undoState )
					{
						case DebugUndoNodeState.CreateNode:
						{
							m_window.CurrentGraph.DeSelectAll();
							m_node = m_window.CreateNode( m_screenshotList[ 0 ].NodeType, Vector2.zero, null, true );
							m_node.RefreshExternalReferences();
							m_undoState = DebugUndoNodeState.FocusOnNode;
							Debug.Log( "Created " + m_node.Attributes.Name );
						}
						break;
						case DebugUndoNodeState.FocusOnNode:
						{
							m_window.FocusOnPoint( m_node.TruePosition.center, 1, false );
							m_undoState = DebugUndoNodeState.WaitFrameCreate;
							Debug.Log( "Focused " + m_node.Attributes.Name );
						}
						break;
						case DebugUndoNodeState.WaitFrameCreate:
						{
							m_undoState = DebugUndoNodeState.DeleteNode;
							Debug.Log( "Waiting on Create" );
						}
						break;
						case DebugUndoNodeState.DeleteNode:
						{
							Debug.Log( "Deleting " + m_node.Attributes.Name );
							m_window.DeleteSelectedNodeWithRepaint();
							m_undoState = DebugUndoNodeState.WaitFrameDelete;
						}
						break;
						case DebugUndoNodeState.WaitFrameDelete:
						{
							m_undoState = DebugUndoNodeState.UndoNode;
							Debug.Log( "Waiting on Delete" );
						}
						break;
						case DebugUndoNodeState.UndoNode:
						{
							Debug.Log( "Performing Undo" );
							m_undoState = DebugUndoNodeState.WaitFrameUndo;
							UndoUtils.PerformUndo();
						}
						break;
						case DebugUndoNodeState.WaitFrameUndo: { } break;
						case DebugUndoNodeState.PrepareForNext:
						{
							m_screenshotList.RemoveAt( 0 );
							Debug.Log( "Undo Performed. Nodes Left " + m_screenshotList.Count );
							m_testingUndo = m_screenshotList.Count > 0;
							if( m_testingUndo )
							{
								m_undoState = DebugUndoNodeState.CreateNode;
								Debug.Log( "Going to next node" );
							}
							else
							{
								Debug.Log( "Finished Undo Test" );
							}
						}
						break;

					}
				}
			}


			if( m_takingShots )
			{
				m_window.Focus();
				switch( m_screenShotState )
				{
					case DebugScreenShotNodeState.CreateNode:
					{
						m_node = m_window.CreateNode( m_screenshotList[ 0 ].NodeType, Vector2.zero, null, false );
						m_node.RefreshExternalReferences();
						m_screenShotState = DebugScreenShotNodeState.FocusOnNode;

						
					}
					break;
					case DebugScreenShotNodeState.FocusOnNode:
					{
						//m_window.FocusOnNode( m_node, 1, false );
						m_window.FocusOnPoint( m_node.TruePosition.center, 1, false );
						m_screenShotState = DebugScreenShotNodeState.TakeScreenshot;
					}
					break;
					case DebugScreenShotNodeState.TakeScreenshot:
					{
						if( m_screenshotRT != null && Event.current.type == EventType.Repaint )
						{
							m_screenshotTex2D.ReadPixels( new Rect( 0, 0, m_screenshotRT.width, m_screenshotRT.height ), 0, 0 );
							m_screenshotTex2D.Apply();

							byte[] bytes = m_screenshotTex2D.EncodeToPNG();
							string pictureFilename = UIUtils.ReplaceInvalidStrings( m_screenshotList[ 0 ].Name );
							pictureFilename = UIUtils.RemoveInvalidCharacters( pictureFilename );

							System.IO.File.WriteAllBytes( m_pathname + pictureFilename + ".png", bytes );
							m_screenShotState = DebugScreenShotNodeState.WaitFrame;
						}
					}
					break;
					case DebugScreenShotNodeState.WaitFrame: { Debug.Log( "Wait Frame" ); m_screenShotState = DebugScreenShotNodeState.DeleteNode; } break;
					case DebugScreenShotNodeState.DeleteNode:
					{
						m_window.DestroyNode( m_node );
						m_screenshotList.RemoveAt( 0 );
						m_takingShots = m_screenshotList.Count > 0;
						Debug.Log( "Destroy Node " + m_screenshotList.Count );

						if( m_takingShots )
						{
							m_screenShotState = DebugScreenShotNodeState.CreateNode;
						}
						else
						{
							RenderTexture.active = null;
							m_screenshotRT.Release();
							UnityEngine.Object.DestroyImmediate( m_screenshotRT );
							m_screenshotRT = null;
							UnityEngine.Object.DestroyImmediate( m_screenshotTex2D );
							m_screenshotTex2D = null;
						}
					}
					break;
				};
			}
		}

		public void Destroy()
		{
			m_window = null;
			if( m_screenshotRT != null )
			{
				m_screenshotRT.Release();
				UnityEngine.Object.DestroyImmediate( m_screenshotRT );
				m_screenshotRT = null;
			}

			if( m_screenshotTex2D != null )
			{
				UnityEngine.Object.DestroyImmediate( m_screenshotTex2D );
				m_screenshotTex2D = null;
			}
		}
	}
}
