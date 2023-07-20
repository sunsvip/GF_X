using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Internal;
using System;

namespace AmplifyShaderEditor
{
	public class UndoUtils
	{
		public static void RegisterUndoRedoCallback( Undo.UndoRedoCallback onUndoRedo )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Undo.undoRedoPerformed -= onUndoRedo;
				Undo.undoRedoPerformed += onUndoRedo;
			}			
		}

		public static void UnregisterUndoRedoCallback( Undo.UndoRedoCallback onUndoRedo )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Undo.undoRedoPerformed -= onUndoRedo;
			}
		}

		public static void RegisterCompleteObjectUndo( UnityEngine.Object objectToUndo, string name )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_RegisterCompleteObjectUndo" );
				Undo.RegisterCompleteObjectUndo( objectToUndo, name );
				Profiler.EndSample();
			}
		}

		public static void RegisterCreatedObjectUndo( UnityEngine.Object objectToUndo, string name )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_RegisterCreatedObjectUndo" );
				Undo.RegisterCreatedObjectUndo( objectToUndo, name );
				Profiler.EndSample();
			}
		}

		public static void ClearUndo( UnityEngine.Object obj )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_ClearUndo" );
				Undo.ClearUndo( obj );
				Profiler.EndSample();
			}
		}

		public static void RecordObject( UnityEngine.Object objectToUndo, string name )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_RecordObject" );
				Undo.RecordObject( objectToUndo, name );
				Profiler.EndSample();
			}
		}

		public static void RecordObjects( UnityEngine.Object[] objectsToUndo, string name )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_RecordObjects" );
				Undo.RecordObjects( objectsToUndo, name );
				Profiler.EndSample();
			}
		}

		public static void DestroyObjectImmediate( UnityEngine.Object objectToUndo )
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_DestroyObjectImmediate" );
				Undo.DestroyObjectImmediate( objectToUndo );
				Profiler.EndSample();
			}
		}

		public static void PerformUndo()
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_PerformUndo" );
				Undo.PerformUndo();
				Profiler.EndSample();
			}
		}

		public static void PerformRedo()
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_PerformRedo" );
				Undo.PerformRedo();
				Profiler.EndSample();
			}
		}

		public static void IncrementCurrentGroup()
		{
			if ( Preferences.GlobalEnableUndo )
			{
				Profiler.BeginSample( "Undo_IncrementCurrentGroup" );
				Undo.IncrementCurrentGroup();
				Profiler.EndSample();
			}
		}
	}
}
