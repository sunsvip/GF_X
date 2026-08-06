using System;
using System.Collections.Generic;
using System.Reflection;

namespace UGF.EditorTools
{
    internal sealed class AssetSelectionSubToolRegistry<TPanel>
        where TPanel : AssetSelectionSubToolBase
    {
        private readonly List<Type> _panelTypes = new List<Type>();
        private string[] _panelTitles = Array.Empty<string>();
        private TPanel[] _panels = Array.Empty<TPanel>();

        internal int Count => _panelTypes.Count;
        internal string[] Titles => _panelTitles;

        internal void Reload(Type ownerType)
        {
            _panelTypes.Clear();

            var editorAssembly = ownerType.Assembly;
            foreach (var type in editorAssembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !type.IsSubclassOf(typeof(TPanel)))
                {
                    continue;
                }

                var toolMenuAttribute = type.GetCustomAttribute<EditorToolMenuAttribute>();
                if (toolMenuAttribute == null || toolMenuAttribute.OwnerType != ownerType)
                {
                    continue;
                }

                _panelTypes.Add(type);
            }

            _panelTypes.Sort(CompareByMenuOrder);
            _panels = new TPanel[_panelTypes.Count];
            _panelTitles = new string[_panelTypes.Count];
            for (var i = 0; i < _panelTypes.Count; i++)
            {
                _panelTitles[i] = _panelTypes[i].GetCustomAttribute<EditorToolMenuAttribute>().ToolMenuPath;
            }
        }

        internal string GetTitle(int index)
        {
            return _panelTitles[index];
        }

        internal TPanel GetOrCreatePanel(int index, Func<Type, TPanel> createPanel)
        {
            if (_panels[index] == null)
            {
                _panels[index] = createPanel(_panelTypes[index]);
            }

            return _panels[index];
        }

        internal void ForEachCreatedPanel(Action<TPanel> action)
        {
            for (var i = 0; i < _panels.Length; i++)
            {
                var panel = _panels[i];
                if (panel != null)
                {
                    action(panel);
                }
            }
        }

        private static int CompareByMenuOrder(Type left, Type right)
        {
            var leftOrder = left.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
            var rightOrder = right.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
            return leftOrder.CompareTo(rightOrder);
        }
    }
}
