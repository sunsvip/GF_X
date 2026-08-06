using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    public partial class ResourceRuleEditor : EditorWindow
    {
        private int _currentConfigIndex;
        [SerializeField] private string _currentConfigPath;
        private List<string> _allConfigPaths;
        private string[] _configNames;

        private void Load()
        {
            _allConfigPaths = ResourceRuleEditorConfigRepository.LoadAllConfigPaths();
            _configNames = ResourceRuleEditorConfigRepository.CreateConfigNames(_allConfigPaths);

            _configuration = ResourceRuleEditorConfigRepository.LoadConfig(_currentConfigPath);
            if (_configuration == null)
            {
                if (_allConfigPaths.Count == 0)
                {
                    _configuration = ResourceRuleEditorConfigRepository.CreateDefaultConfig();
                    _currentConfigPath = ResourceRuleCompiler.DefaultConfigurationPath;
                    _allConfigPaths = new List<string> { ResourceRuleCompiler.DefaultConfigurationPath };
                    _configNames = new[] { Path.GetFileNameWithoutExtension(ResourceRuleCompiler.DefaultConfigurationPath) };
                }
                else
                {
                    var safeIndex = _currentConfigIndex >= 0 && _currentConfigIndex < _allConfigPaths.Count ? _currentConfigIndex : 0;
                    _configuration = ResourceRuleEditorConfigRepository.LoadConfig(_allConfigPaths[safeIndex]);
                }

                _currentConfigIndex = 0;
            }
            else
            {
                _currentConfigIndex = _allConfigPaths.FindIndex(path => string.Equals(_currentConfigPath, path, System.StringComparison.Ordinal));
                if (_currentConfigIndex < 0)
                {
                    _currentConfigIndex = 0;
                }
            }

            _ruleList = null;
        }

        private void Add()
        {
            string path = SelectFolder();
            if (!string.IsNullOrEmpty(path))
            {
                var rule = new ResourceRule
                {
                    assetsDirectoryPath = path,
                    name = Path.GetFileName(path),
                    packed = false
                };
                _configuration.rules.Add(rule);
            }
        }

        private string SelectFolder()
        {
            string dataPath = Application.dataPath;
            string selectedPath = EditorUtility.OpenFolderPanel("Path", dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                string normalizedDataPath = Path.GetFullPath(dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedSelectedPath = Path.GetFullPath(selectedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                bool isInsideAssetsPath = string.Equals(normalizedSelectedPath, normalizedDataPath, System.StringComparison.OrdinalIgnoreCase)
                    || normalizedSelectedPath.StartsWith(normalizedDataPath + Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase)
                    || normalizedSelectedPath.StartsWith(normalizedDataPath + Path.AltDirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase);
                if (isInsideAssetsPath)
                {
                    if (string.Equals(normalizedSelectedPath, normalizedDataPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return "Assets";
                    }

                    return "Assets/" + normalizedSelectedPath.Substring(normalizedDataPath.Length + 1);
                }

                ShowNotification(new GUIContent("Can not be outside of 'Assets/'!"), 2);
            }

            return null;
        }

        private void Save()
        {
            ResourceRuleEditorConfigRepository.Save(_configuration, _currentConfigPath);
        }
    }
}

