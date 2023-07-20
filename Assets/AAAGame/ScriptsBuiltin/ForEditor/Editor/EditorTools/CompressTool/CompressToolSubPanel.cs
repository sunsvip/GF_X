using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace UGF.EditorTools
{
    public abstract class CompressToolSubPanel
    {
        public abstract string AssetSelectorTypeFilter { get; }//"t:sprite t:texture2d t:folder"
        public abstract string DragAreaTips { get; }
        public virtual string ReadmeText { get;} = string.Empty;
        protected abstract Type[] SupportAssetTypes { get; }

        public virtual void OnEnter() { }
        public virtual void OnExit() { SaveSettings(); }
        public abstract void DrawSettingsPanel();
        public abstract void DrawBottomButtonsPanel();
        /// <summary>
        /// 通过AssetDatabase判断是否支持, 注意如果是Assets之外的文件判断需要重写此方法
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public virtual bool IsSupportAsset(string assetPath)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return SupportAssetTypes.Contains(assetType);
        }

        /// <summary>
        /// 获取当前选择的资源文件列表
        /// </summary>
        /// <returns></returns>
        public virtual List<string> GetSelectedAssets()
        {
            List<string> images = new List<string>();
            foreach (var item in EditorToolSettings.Instance.CompressImgToolItemList)
            {
                if (item == null) continue;

                var assetPath = AssetDatabase.GetAssetPath(item);
                var itmTp = GetSelectedItemType(assetPath);
                if (itmTp == ItemType.File)
                {
                    string imgFileName = Utility.Path.GetRegularPath(assetPath);
                    if (IsSupportAsset(imgFileName) && !images.Contains(imgFileName))
                    {
                        images.Add(imgFileName);
                    }
                }
                else if (itmTp == ItemType.Folder)
                {
                    string imgFolder = AssetDatabase.GetAssetPath(item);
                    var assets = AssetDatabase.FindAssets(GetFindAssetsFilter(), new string[] { imgFolder });
                    for (int i = assets.Length - 1; i >= 0; i--)
                    {
                        assets[i] = AssetDatabase.GUIDToAssetPath(assets[i]);
                    }
                    images.AddRange(assets);
                }
            }

            return images.Distinct().ToList();//把结果去重处理
        }
        protected string GetFindAssetsFilter()
        {
            string filter = "";
            foreach (var item in SupportAssetTypes)
            {
                filter += $"t:{item.Name} ";
            }
            filter.Trim(' ');
            return filter;
        }
        public virtual void SaveSettings()
        {
            if (EditorToolSettings.Instance)
            {
                EditorToolSettings.Save();
            }
        }

        internal ItemType GetSelectedItemType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return ItemType.NoSupport;

            if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory) return ItemType.Folder;

            if (IsSupportAsset(assetPath)) return ItemType.File;

            return ItemType.NoSupport;
        }

        /// <summary>
        /// 获取备份图片,备份图片在外部目录,不能使用AssetDatabase读取
        /// </summary>
        /// <param name="imgFolder"></param>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        internal List<string> GetAllBackupFilesByDir(string imgFolder, string baseFolder)
        {
            var images = new List<string>();
            if (!string.IsNullOrWhiteSpace(imgFolder) && Directory.Exists(imgFolder))
            {
                var allFiles = Directory.GetFiles(imgFolder, "*.*", SearchOption.AllDirectories);
                foreach (var item in allFiles)
                {
                    var fileName = Utility.Path.GetRegularPath(Path.GetRelativePath(baseFolder, item));
                    if (IsSupportAsset(fileName) && !images.Contains(fileName))
                    {
                        images.Add(fileName);
                    }
                }
            }
            return images;
        }
    }
}

