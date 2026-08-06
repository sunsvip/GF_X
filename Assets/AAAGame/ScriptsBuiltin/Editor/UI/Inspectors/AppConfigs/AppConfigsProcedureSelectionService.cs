using GameFramework;
using GameFramework.Procedure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class AppConfigsProcedureSelectionService
    {
        internal static AppConfigsSelectableItem[] LoadProcedures(AppConfigs config)
        {
            List<AppConfigsSelectableItem> result = new List<AppConfigsSelectableItem>();
            string[] enabledProcedures = config != null && config.Procedures != null
                ? config.Procedures
                : Array.Empty<string>();
            var hotfixAssemblies = Utility.Assembly.GetAssemblies()
                .Where(assembly => HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.Contains(assembly.GetName().Name))
                .ToArray();

            foreach (var assembly in hotfixAssemblies)
            {
                Type[] procedureTypes = assembly.GetTypes()
                    .Where(type => typeof(ProcedureBase).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(ProcedureBase))
                    .ToArray();

                foreach (Type procedureType in procedureTypes)
                {
                    string procedureName = procedureType.FullName;
                    result.Add(new AppConfigsSelectableItem(enabledProcedures.Contains(procedureName), procedureName));
                }
            }

            return result.ToArray();
        }
    }
}
