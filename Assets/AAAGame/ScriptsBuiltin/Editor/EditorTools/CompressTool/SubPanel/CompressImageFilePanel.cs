using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("图片文件压缩", typeof(CompressToolEditor), 1)]
    public class CompressImageFilePanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:sprite t:texture2d t:folder";
        public override string DragAreaTips => "拖拽到此处添加文件夹或jpg/png";
        public override string ReadmeText => "压缩图片原文件,支持在线/离线压缩,只支持jpg/png";

        private readonly string[] mSupportAssetFormats = { ".png", ".jpg" }; //支持压缩的格式;
        private readonly Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;


        ReorderableList tinypngKeyScrollList;
        Vector2 tinypngScrollListPos;
        public override void OnEnter()
        {
            tinypngKeyScrollList = new ReorderableList(EditorToolSettings.Instance.CompressImgToolKeys, typeof(string), true, true, true, true);
            tinypngKeyScrollList.drawHeaderCallback = DrawTinypngKeyScrollListHeader;
            tinypngKeyScrollList.drawElementCallback = DrawTinypngKeyItem;
        }

        public override bool IsSupportAsset(string assetPath)
        {
            var format = Path.GetExtension(assetPath).ToLower();
            return mSupportAssetFormats.Contains(format);
        }
        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    StartCompress();
                }
                if (GUILayout.Button("备份图片", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    BackupImages();
                }
                if (GUILayout.Button("还原备份", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    RecoveryImages();
                }
                if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    SaveSettings();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        public override void DrawSettingsPanel()
        {
            EditorGUI.BeginDisabledGroup(EditorToolSettings.Instance.CompressImgToolOffline);
            {
                tinypngScrollListPos = EditorGUILayout.BeginScrollView(tinypngScrollListPos, GUILayout.Height(110));
                {
                    tinypngKeyScrollList.DoLayoutList();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.BeginHorizontal("box");
            {
                EditorToolSettings.Instance.CompressImgToolOffline = EditorGUILayout.ToggleLeft("离线压缩", EditorToolSettings.Instance.CompressImgToolOffline, GUILayout.Width(100));
                EditorToolSettings.Instance.CompressImgToolCoverRaw = EditorGUILayout.ToggleLeft("覆盖原图片", EditorToolSettings.Instance.CompressImgToolCoverRaw, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CompressImgToolOffline);
                {
                    EditorGUILayout.MinMaxSlider(Utility.Text.Format("压缩质量({0}%-{1}%)", (int)EditorToolSettings.Instance.CompressImgToolQualityMinLv, (int)EditorToolSettings.Instance.CompressImgToolQualityLv), ref EditorToolSettings.Instance.CompressImgToolQualityMinLv, ref EditorToolSettings.Instance.CompressImgToolQualityLv, 0, 100);

                    EditorToolSettings.Instance.CompressImgToolFastLv = EditorGUILayout.IntSlider(Utility.Text.Format("快压等级({0})", EditorToolSettings.Instance.CompressImgToolFastLv), EditorToolSettings.Instance.CompressImgToolFastLv, 1, 10);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUI.BeginDisabledGroup(EditorToolSettings.Instance.CompressImgToolCoverRaw);
                {
                    EditorGUILayout.LabelField("输出路径:", GUILayout.Width(80));
                    EditorGUILayout.SelectableLabel(EditorToolSettings.Instance.CompressImgToolOutputDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("选择", GUILayout.Width(80)))
                    {
                        var backupPath = EditorUtilityExtension.OpenRelativeFolderPanel("选择图片输出路径", EditorToolSettings.Instance.CompressImgToolOutputDir);
                        EditorToolSettings.Instance.CompressImgToolOutputDir = backupPath;
                        EditorToolSettings.Save();
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("打开", GUILayout.Width(80)))
                    {
                        EditorUtility.RevealInFinder(Path.Combine(Directory.GetParent(Application.dataPath).FullName, EditorToolSettings.Instance.CompressImgToolOutputDir));
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("备份路径:", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(EditorToolSettings.Instance.CompressImgToolBackupDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("选择", GUILayout.Width(80)))
                {
                    var backupPath = EditorUtilityExtension.OpenRelativeFolderPanel("选择备份路径", EditorToolSettings.Instance.CompressImgToolBackupDir);

                    EditorToolSettings.Instance.CompressImgToolBackupDir = backupPath;
                    EditorToolSettings.Save();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("打开", GUILayout.Width(80)))
                {
                    EditorUtility.RevealInFinder(Path.Combine(Directory.GetParent(Application.dataPath).FullName, EditorToolSettings.Instance.CompressImgToolBackupDir));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTinypngKeyItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorToolSettings.Instance.CompressImgToolKeys[index] = EditorGUI.PasswordField(rect, EditorToolSettings.Instance.CompressImgToolKeys[index]);
        }

        private void DrawTinypngKeyScrollListHeader(Rect rect)
        {
            if (EditorGUI.LinkButton(rect, "添加TinyPng Keys(默认使用第一行Key):\t点击跳转到key获取地址..."))
            {
                Application.OpenURL("https://tinify.com/dashboard/api");
            }
        }
        private void StartCompress()
        {
            if (EditorToolSettings.Instance.CompressImgToolCoverRaw && string.IsNullOrWhiteSpace(EditorToolSettings.Instance.CompressImgToolOutputDir))
            {
                EditorUtility.DisplayDialog("错误", "图片输出路径无效!", "OK");
                return;
            }
            var imgList = GetSelectedAssets();
            CompressImages(imgList);
        }
        private void BackupImages()
        {
            var itmList = GetSelectedAssets();
            int totalImgCount = itmList.Count;
            if (0 != EditorUtility.DisplayDialogComplex("提示", $"确认开始备份已选 {totalImgCount} 张图片吗?", "确定备份", "取消", null))
            {
                return;
            }
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var backupDir = UtilityBuiltin.AssetsPath.GetCombinePath(projectRoot, EditorToolSettings.Instance.CompressImgToolBackupDir);

            if (string.IsNullOrWhiteSpace(EditorToolSettings.Instance.CompressImgToolBackupDir))
            {
                EditorUtility.DisplayDialog("错误", $"当前选择的备份路径无效:{Environment.NewLine}{EditorToolSettings.Instance.CompressImgToolBackupDir}", "OK");
                return;
            }
            var backupPath = UtilityBuiltin.AssetsPath.GetCombinePath(backupDir, DateTime.Now.ToString("yyyy-MM-dd-HHmmss"));

            int successCount = 0;
            for (int i = 0; i < itmList.Count; i++)
            {
                var imgFile = itmList[i];
                var srcImg = Path.GetFullPath(imgFile, projectRoot);
                var desImg = Path.GetFullPath(imgFile, backupPath);
                try
                {
                    if (EditorUtility.DisplayCancelableProgressBar($"备份进度({i}/{totalImgCount})", $"正在备份:{Environment.NewLine}{imgFile}", i / (float)totalImgCount))
                    {
                        break;
                    }
                    string desFilePath = Path.GetDirectoryName(desImg);
                    if (!Directory.Exists(desFilePath))
                    {
                        Directory.CreateDirectory(desFilePath);
                    }
                    File.Copy(srcImg, desImg, true);
                    successCount++;
                }
                catch (Exception e)
                {
                    Debug.LogWarningFormat("---------备份图片{0}失败:{1}", imgFile, e.Message);
                }
            }

            EditorUtility.ClearProgressBar();

            if (0 == EditorUtility.DisplayDialogComplex("备份结束", $"共 {totalImgCount} 张图片{Environment.NewLine}成功备份  {successCount} 张{Environment.NewLine}备份失败 {totalImgCount - successCount} 张", "打开备份目录", "关闭", null))
            {
                EditorUtility.RevealInFinder(backupPath);
                GUIUtility.ExitGUI();
            }
        }
        private void RecoveryImages()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var backupRoot = UtilityBuiltin.AssetsPath.GetCombinePath(projectRoot, EditorToolSettings.Instance.CompressImgToolBackupDir);
            if (!Directory.Exists(backupRoot))
            {
                EditorUtility.DisplayDialog("提示", $"备份路径不存在:{backupRoot}", "OK");
                return;
            }
            var backupItems = Directory.GetDirectories(backupRoot, "*", SearchOption.TopDirectoryOnly);
            if (backupItems.Length < 1)
            {
                EditorUtility.DisplayDialog("提示", "没有备份记录", "OK");
                return;
            }
            var contents = new GUIContent[backupItems.Length];

            for (int i = 0; i < backupItems.Length; i++)
            {
                var item = Path.GetRelativePath(backupRoot, backupItems[i]);
                contents[i] = new GUIContent(item);
            }
            var dialogRect = new Rect(UnityEngine.Event.current.mousePosition, Vector2.zero);

            EditorUtility.DisplayCustomMenu(dialogRect, contents, -1, (object userData, string[] options, int selected) =>
            {
                string backupName = options[selected];
                if (0 != EditorUtility.DisplayDialogComplex("还原备份", $"是否还原此备份:[{backupName}]?", "还原备份", "取消", null))
                {
                    return;
                }
                var recoveryDir = UtilityBuiltin.AssetsPath.GetCombinePath(backupRoot, backupName);
                var imgList = GetAllBackupFilesByDir(recoveryDir, recoveryDir);
                CopyFilesTo(imgList, recoveryDir, projectRoot);
            }, null);
        }
        
        private void CopyFilesTo(List<string> imgList, string srcRoot, string desRoot)
        {
            int totalCount = imgList.Count;
            int successCount = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var item = imgList[i];
                var desFile = UtilityBuiltin.AssetsPath.GetCombinePath(desRoot, item);
                var desFileDir = Path.GetDirectoryName(desFile);
                if (!Directory.Exists(desFileDir))
                {
                    Directory.CreateDirectory(desFileDir);
                }
                var srcFile = UtilityBuiltin.AssetsPath.GetCombinePath(srcRoot, item);
                if (!EditorUtility.DisplayCancelableProgressBar("还原进度", $"还原文件:{item}", i / (float)totalCount))
                {
                    try
                    {
                        File.Copy(srcFile, desFile, true);
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarningFormat("--------还原文件{0}失败:{1}", srcFile, e.Message);
                    }
                }
                else
                {
                    break;
                }
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("还原备份结束", $"共 {totalCount} 张图片{Environment.NewLine}成功还原 {successCount} 张{Environment.NewLine}还原失败 {totalCount - successCount} 张", "OK");
            AssetDatabase.Refresh();
        }
        private async void CompressImages(List<string> imgList)
        {
            if (imgList.Count < 1) return;
            string tinypngKey = null;
            if (EditorToolSettings.Instance.CompressImgToolKeys != null && EditorToolSettings.Instance.CompressImgToolKeys.Count > 0 && !string.IsNullOrWhiteSpace(EditorToolSettings.Instance.CompressImgToolKeys[0]))
            {
                tinypngKey = EditorToolSettings.Instance.CompressImgToolKeys[0];
            }

            if (!EditorToolSettings.Instance.CompressImgToolOffline && string.IsNullOrWhiteSpace(tinypngKey))
            {
                EditorUtility.DisplayDialog("错误", "TinyPng Key无效,可前往tinypng.com获取.", "OK");
                return;
            }
            int clickBtIdx = EditorUtility.DisplayDialogComplex("请确认", Utility.Text.Format("共 {0} 张图片待压缩, 是否开始压缩?", imgList.Count), "开始压缩", "取消", null);
            if (clickBtIdx != 0)
            {
                //用户取消压缩
                return;
            }

            imgList.Reverse();

            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputPath;
            if (EditorToolSettings.Instance.CompressImgToolCoverRaw)
            {
                outputPath = rootPath;
            }
            else
            {
                outputPath = Path.GetFullPath(EditorToolSettings.Instance.CompressImgToolOutputDir, rootPath);
            }

            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                    EditorUtility.DisplayDialog("错误", Utility.Text.Format("创建路径失败,请检查路径是否有效:{0}", outputPath), "OK");
                    return;
                }
            }

            int totalCount = imgList.Count;
            for (int i = totalCount - 1; i >= 0; i--)
            {
                var imgName = imgList[i];
                var imgFileName = Utility.Path.GetRegularPath(Path.GetFullPath(imgName, rootPath));
                var outputFileName = Utility.Path.GetRegularPath(Path.GetFullPath(imgName, outputPath));
                var outputFilePath = Path.GetDirectoryName(outputFileName);
                if (!Directory.Exists(outputFilePath))
                {
                    Directory.CreateDirectory(outputFilePath);
                }
                if (EditorUtility.DisplayCancelableProgressBar(Utility.Text.Format("压缩进度({0}/{1})", totalCount - imgList.Count, totalCount), Utility.Text.Format("正在压缩:{0}", imgName), (totalCount - i) / (float)totalCount))
                {
                    break;
                }

                if (EditorToolSettings.Instance.CompressImgToolOffline)
                {
                    if (CompressTool.CompressImageOffline(imgFileName, outputFileName))
                    {
                        imgList.RemoveAt(i);
                    }
                }
                else
                {
                    if (await CompressTool.CompressOnlineAsync(imgFileName, outputFileName, tinypngKey))
                    {
                        imgList.RemoveAt(i);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            OnCompressCompleted(imgList);
        }

        private void OnCompressCompleted(List<string> imgList)
        {
            AssetDatabase.Refresh();
            if (imgList.Count <= 0)
            {
                EditorUtility.DisplayDialog("压缩完成!", "全部文件已压缩完成", "OK");
                return;
            }
            //提示是否再次压缩所有失败的图片
            var clickBtIdx = EditorUtility.DisplayDialogComplex("警告", Utility.Text.Format("有 {0} 张图片压缩失败, 是否继续压缩?", imgList.Count), "继续压缩", "取消", null);
            if (clickBtIdx == 0)
            {
                CompressImages(imgList);
            }
        }
    }
}

