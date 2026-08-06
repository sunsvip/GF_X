using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class AssetSelectionToolSessionState
    {
        private readonly string _activePanelSessionKey;
        private readonly string _selectedAssetGuidSessionKeyPrefix;

        internal AssetSelectionToolSessionState(string sessionStatePrefix)
        {
            _activePanelSessionKey = $"{sessionStatePrefix}.ActivePanelIndex";
            _selectedAssetGuidSessionKeyPrefix = $"{sessionStatePrefix}.SelectedAssetGuids";
        }

        internal int LoadActivePanelIndex()
        {
            return SessionState.GetInt(_activePanelSessionKey, 0);
        }

        internal void SaveActivePanelIndex(int activePanelIndex)
        {
            SessionState.SetInt(_activePanelSessionKey, activePanelIndex);
        }

        internal void LoadSelectedObjects(string selectionId, List<UnityEngine.Object> selectedObjects)
        {
            selectedObjects.Clear();

            var guidText = SessionState.GetString(GetSelectedAssetGuidSessionKey(selectionId), string.Empty);
            if (string.IsNullOrWhiteSpace(guidText))
            {
                return;
            }

            var guids = guidText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null && !selectedObjects.Contains(asset))
                {
                    selectedObjects.Add(asset);
                }
            }
        }

        internal void SaveSelectedObjects(string selectionId, List<UnityEngine.Object> selectedObjects)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < selectedObjects.Count; i++)
            {
                var item = selectedObjects[i];
                if (item == null)
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(item);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(assetGuid))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(assetGuid);
            }

            SessionState.SetString(GetSelectedAssetGuidSessionKey(selectionId), builder.ToString());
        }

        private string GetSelectedAssetGuidSessionKey(string selectionId)
        {
            return $"{_selectedAssetGuidSessionKeyPrefix}.{selectionId}";
        }
    }
}
