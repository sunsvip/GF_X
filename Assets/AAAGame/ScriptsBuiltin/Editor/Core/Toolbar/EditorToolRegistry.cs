using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UGF.EditorTools
{
    internal static class EditorToolRegistry
    {
        private static readonly List<Type> s_EditorToolTypes = new List<Type>();

        public static IReadOnlyList<Type> GetOrderedEditorToolTypes()
        {
            if (s_EditorToolTypes.Count == 0)
            {
                Rebuild();
            }

            return s_EditorToolTypes;
        }

        public static void Rebuild()
        {
            s_EditorToolTypes.Clear();

            var editorAssembly = typeof(EditorToolbarExtension).Assembly;
            Type[] assemblyTypes;
            try
            {
                assemblyTypes = editorAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                assemblyTypes = exception.Types.Where(type => type != null).ToArray();
            }

            var editorToolTypes = assemblyTypes.Where(type =>
                type.IsClass
                && !type.IsAbstract
                && type.IsSubclassOf(typeof(EditorToolBase))
                && type.GetCustomAttribute<EditorToolMenuAttribute>() != null);

            s_EditorToolTypes.AddRange(editorToolTypes);
            s_EditorToolTypes.Sort((x, y) =>
            {
                var xOrder = x.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                var yOrder = y.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                return xOrder.CompareTo(yOrder);
            });
        }
    }
}
