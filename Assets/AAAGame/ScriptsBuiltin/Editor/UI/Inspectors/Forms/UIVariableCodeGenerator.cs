#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class UIVariableCodeGenerator
    {
        internal static void Generate(MonoBehaviour uiBehaviour, string outputDirectory, string refreshBindKey)
        {
            if (uiBehaviour == null || uiBehaviour is not ISerializeFieldTool serializeFieldTool)
            {
                return;
            }

            var monoScript = MonoScript.FromMonoBehaviour(uiBehaviour);
            if (monoScript == null)
            {
                Debug.LogError($"生成UI变量失败，无法获取脚本资源: {uiBehaviour.name}");
                return;
            }

            var classType = monoScript.GetClass();
            if (classType == null)
            {
                EditorUtility.DisplayDialog("生成UI变量失败!", $"无法解析脚本类型，请先修复脚本编译错误或确认脚本类与文件绑定正常。\n{AssetDatabase.GetAssetPath(monoScript)}", "OK");
                return;
            }

            var className = classType.Name;
            var scriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(outputDirectory, Utility.Text.Format("{0}.Variables.cs", className));
            var fields = serializeFieldTool.SerializeFieldArr;
            if (fields == null || fields.Length <= 0)
            {
                AssetDatabase.DeleteAsset(scriptFile);
                return;
            }

            var matchResult = Regex.Match(monoScript.text, Utility.Text.Format("partial[\\s]+class[\\s]+{0}", className));
            var sourceScriptPath = AssetDatabase.GetAssetPath(monoScript);
            if (!matchResult.Success)
            {
                EditorUtility.DisplayDialog("生成UI变量失败!", Utility.Text.Format("请先手动为{0}类添加'partial'修饰符!\n{1}", className, sourceScriptPath), "OK");
                return;
            }

            var namespaceList = new List<string> { "UnityEngine" };
            var fieldList = new List<string>();
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.Targets == null || string.IsNullOrWhiteSpace(field.VarType) || string.IsNullOrWhiteSpace(field.VarName))
                {
                    continue;
                }

                var variableType = UISerializeFieldBindingService.GetSampleType(field.VarType);
                if (variableType == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(variableType.Namespace) && !namespaceList.Contains(variableType.Namespace))
                {
                    namespaceList.Add(variableType.Namespace);
                }

                var isArray = field.Targets.Length > 1;
                var varPrefix = UISerializeFieldBindingService.GetVarPrefix(field.VarPrefix);
                var serializeFieldPrefix = "[SerializeField] ";
                fieldList.Add(isArray
                    ? Utility.Text.Format("{0}{1} {2}[] {3} = null;", serializeFieldPrefix, varPrefix, variableType.Name, field.VarName)
                    : Utility.Text.Format("{0}{1} {2} {3} = null;", serializeFieldPrefix, varPrefix, variableType.Name, field.VarName));
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("//---------------------------------");
            stringBuilder.AppendLine("//此文件由工具自动生成,请勿手动修改");
            stringBuilder.AppendLine($"//更新自:{SystemInfo.deviceName}");
            stringBuilder.AppendLine("//---------------------------------");
            for (var i = 0; i < namespaceList.Count; i++)
            {
                stringBuilder.AppendLine(Utility.Text.Format("using {0};", namespaceList[i]));
            }

            var classNamespace = classType.Namespace;
            var hasNamespace = !string.IsNullOrWhiteSpace(classNamespace);
            if (hasNamespace)
            {
                stringBuilder.AppendLine(Utility.Text.Format("namespace {0}", classNamespace));
                stringBuilder.AppendLine("{");
            }

            stringBuilder.AppendLine(Utility.Text.Format("public partial class {0}", className));
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("\t[Space(10)]");
            stringBuilder.AppendLine("\t[Header(\"UI Variables:\")]");
            for (var i = 0; i < fieldList.Count; i++)
            {
                stringBuilder.AppendLine("\t" + fieldList[i]);
            }

            stringBuilder.AppendLine("}");
            if (hasNamespace)
            {
                stringBuilder.AppendLine("}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            try
            {
                File.WriteAllText(scriptFile, stringBuilder.ToString(), new UTF8Encoding(false));
                EditorPrefs.SetBool(refreshBindKey, true);
                AssetDatabase.Refresh();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"生成UI变量代码失败: {scriptFile}, Error:{exception.Message}");
                throw;
            }
        }
    }
}
#endif
