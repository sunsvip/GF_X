using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools.AIAssistant
{
    [EditorToolMenu("AI助手/ChatGPT", null, 5, true)]
    public class ChatGPTWindow : EditorToolBase
    {
        public override string ToolName => "ChatGPT";
        public override Vector2Int WinSize => new Vector2Int(600, 800);
        Vector2 scrollPos = Vector2.zero;

        ChatGPT ai;
        private bool settingFoldout = false;
        string message;
        const string aiRoleName = "AI";
        private float iconMaxSize = 80f;
        private float chatBoxPadding = 20;
        private float chatBoxEdgePadding = 10;

        GUIStyle myChatStyle;
        GUIStyle aiChatStyle;

        GUIStyle aiIconStyle;
        GUIStyle myIconStyle;
        GUIStyle txtAreaStyle;

        GUIContent chatContent;

        bool isEditorInitialized = false;
        private float scrollViewHeight;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            ai = new ChatGPT(EditorToolSettings.Instance.ChatGPTKey);
            ai.ChatGPTRandomness = EditorToolSettings.Instance.ChatGPTRandomness;
            ai.RequestTimeout = EditorToolSettings.Instance.ChatGPTTimeout;
            chatContent = new GUIContent();
            ai.RestoreChatHistory();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            try
            {
                InitGUIStyles();
                isEditorInitialized = true;
                EditorApplication.update -= OnEditorUpdate;
            }
            catch (Exception)
            {

            }
        }

        private void InitGUIStyles()
        {
            aiChatStyle = new GUIStyle(EditorStyles.selectionRect);
            aiChatStyle.wordWrap = true;
            aiChatStyle.normal.textColor = Color.white;
            aiChatStyle.fontSize = 18;
            aiChatStyle.alignment = TextAnchor.MiddleLeft;

            myChatStyle = new GUIStyle(EditorStyles.helpBox);
            myChatStyle.wordWrap = true;
            myChatStyle.normal.textColor = Color.white;
            myChatStyle.fontSize = 18;
            myChatStyle.alignment = TextAnchor.MiddleLeft;
            myChatStyle.focused.textColor = myChatStyle.normal.textColor;


            txtAreaStyle = new GUIStyle(EditorStyles.textArea);
            txtAreaStyle.fontSize = 18;

            aiIconStyle = new GUIStyle();
            aiIconStyle.wordWrap = true;
            aiIconStyle.padding = new RectOffset(10, 10, 10, 10);
            aiIconStyle.alignment = TextAnchor.MiddleCenter;
            aiIconStyle.fontSize = 18;
            aiIconStyle.fontStyle = FontStyle.Bold;
            aiIconStyle.normal.textColor = Color.black;
            aiIconStyle.normal.background = EditorGUIUtility.FindTexture("sv_icon_dot5_pix16_gizmo");

            myIconStyle = new GUIStyle(aiIconStyle);
            myIconStyle.normal.background = EditorGUIUtility.FindTexture("sv_icon_dot2_pix16_gizmo");
        }

        private void OnDisable()
        {
            ai.SaveChatHistory();
            EditorToolSettings.Save();
        }
        private void OnGUI()
        {
            if (!isEditorInitialized) return;
            EditorGUILayout.BeginVertical();
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                {
                    scrollViewHeight = 0;
                    for (int i = 0; i < ai.MessageHistory.Count; i++)
                    {
                        var msg = ai.MessageHistory[i];
                        var msgRect = EditorGUILayout.BeginVertical();
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                bool isMyMsg = ai.IsSelfMessage(msg);
                                var labelStyle = isMyMsg ? myChatStyle : aiChatStyle;
                                chatContent.text = msg.content;
                                float chatBoxWidth = this.position.width - iconMaxSize * 2f;
                                float chatBoxHeight = Mathf.Max(iconMaxSize, chatBoxEdgePadding + labelStyle.CalcHeight(chatContent, chatBoxWidth - chatBoxEdgePadding));

                                ChatGPTCodeBlock[] codeBlocks = null;
                                if (isMyMsg) { GUILayout.FlexibleSpace(); }
                                else
                                {
                                    codeBlocks = ai.GetCodeBlocksByIdx(i);
                                    if (codeBlocks != null)
                                    {
                                        chatBoxWidth -= 50;
                                    }

                                    EditorGUILayout.LabelField(aiRoleName, aiIconStyle, GUILayout.Width(iconMaxSize), GUILayout.Height(iconMaxSize));
                                }
                                EditorGUILayout.SelectableLabel(msg.content, labelStyle, GUILayout.Width(chatBoxWidth), GUILayout.Height(chatBoxHeight));
                                if (!isMyMsg)
                                {

                                    if (codeBlocks != null)
                                    {
                                        for (int blockIdx = 0; blockIdx < codeBlocks.Length; blockIdx++)
                                        {
                                            var cBlock = codeBlocks[blockIdx];
                                            EditorGUILayout.BeginVertical("box");
                                            {
                                                if (GUILayout.Button($"保存{cBlock.FileExtension}文件({blockIdx})"))
                                                {
                                                    var fileName = EditorUtility.SaveFilePanel("保存文件", EditorPrefs.GetString("LAST_SELECT_PATH"), null, cBlock.FileExtension);
                                                    if (!string.IsNullOrWhiteSpace(fileName))
                                                    {
                                                        try
                                                        {
                                                            System.IO.File.WriteAllText(fileName, cBlock.Content, System.Text.Encoding.UTF8);
                                                            EditorPrefs.SetString("LAST_SELECT_PATH", Path.GetFullPath(fileName));
                                                            AssetDatabase.Refresh();
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Debug.LogError($"保存{fileName}文件失败:{e.Message}");
                                                        }
                                                    }
                                                }
                                                EditorGUILayout.EndVertical();
                                            }
                                        }
                                    }
                                    GUILayout.FlexibleSpace();
                                }
                                else
                                {
                                    EditorGUILayout.LabelField(msg.role, myIconStyle, GUILayout.Width(iconMaxSize), GUILayout.Height(iconMaxSize));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.Space(chatBoxPadding);
                        scrollViewHeight += msgRect.height;
                    }
                    EditorGUILayout.EndScrollView();
                }

                if (ai.IsRequesting)
                {
                    var barWidth = position.width * 0.8f;
                    var pBarRect = new Rect((position.width - barWidth) * 0.5f, (position.height - 30f) * 0.5f, barWidth, 30f);
                    EditorGUI.ProgressBar(pBarRect, ai.RequestProgress, $"请求进度:{ai.RequestProgress:P2}");
                }
                GUILayout.FlexibleSpace();
                if (settingFoldout = EditorGUILayout.Foldout(settingFoldout, "展开设置项:"))
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("ChatGPT API Key:", GUILayout.Width(170));
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorToolSettings.Instance.ChatGPTKey = EditorGUILayout.PasswordField(EditorToolSettings.Instance.ChatGPTKey);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    ai.SetAPIKey(EditorToolSettings.Instance.ChatGPTKey);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("结果随机性:", GUILayout.Width(170));
                            ai.ChatGPTRandomness = EditorToolSettings.Instance.ChatGPTRandomness = EditorGUILayout.Slider(EditorToolSettings.Instance.ChatGPTRandomness, 0, 2);
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("请求超时时长:", GUILayout.Width(170));
                            ai.RequestTimeout = EditorToolSettings.Instance.ChatGPTTimeout = EditorGUILayout.IntSlider(EditorToolSettings.Instance.ChatGPTTimeout, 30, 120);
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }

                }
                //EditorGUILayout.LabelField(scrollPos.ToString());
                EditorGUILayout.BeginHorizontal();
                {
                    message = EditorGUILayout.TextArea(message, txtAreaStyle, GUILayout.MinHeight(80));

                    EditorGUI.BeginDisabledGroup(ai.IsRequesting);
                    {
                        if (GUILayout.Button("发送消息", GUILayout.MaxWidth(120), GUILayout.Height(80)))
                        {
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                ai.Send(message, OnChatGPTMessage);
                            }
                        }
                        if (GUILayout.Button("新话题", GUILayout.MaxWidth(80), GUILayout.Height(80)))
                        {
                            ai.NewChat();
                        }
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void OnChatGPTMessage(bool success, string aiMsg)
        {
            scrollPos.y = scrollViewHeight;
            if (success)
            {
                message = string.Empty;
            }
            Repaint();
        }
    }
}

