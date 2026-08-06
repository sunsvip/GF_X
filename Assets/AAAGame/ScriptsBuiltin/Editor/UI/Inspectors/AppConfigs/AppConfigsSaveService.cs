#if UNITY_EDITOR
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class AppConfigsSaveService
    {
        internal static void Save(SerializedObject serializedObject, string[] dataTables, string[] configs, string[] languages, string[] procedures)
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.Update();
            WriteStringArray(serializedObject.FindProperty("mDataTables"), dataTables);
            WriteStringArray(serializedObject.FindProperty("mConfigs"), configs);
            WriteStringArray(serializedObject.FindProperty("mLanguages"), languages);
            WriteStringArray(serializedObject.FindProperty("mProcedures"), procedures);
            serializedObject.ApplyModifiedProperties();
            DataTableUpdater.ReloadAppConfigs();
        }

        private static void WriteStringArray(SerializedProperty property, string[] values)
        {
            if (property == null)
            {
                return;
            }

            var writeValues = values ?? System.Array.Empty<string>();
            property.arraySize = writeValues.Length;
            for (var i = 0; i < writeValues.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = writeValues[i];
            }
        }
    }
}
#endif
