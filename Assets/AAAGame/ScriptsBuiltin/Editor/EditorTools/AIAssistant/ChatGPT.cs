using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace UGF.EditorTools.AIAssistant
{
    public class ChatGPT
    {
        private static readonly Dictionary<string, string> SupportCodeLanguages = new Dictionary<string, string>
        {
            ["python"] ="py",
            ["csharp"] = "cs",
            ["json"] = "json",
            ["cpp"] = "cpp",
            ["c++"] = "cpp",
            ["java"] = "java",
            ["javascript"] = "js",
            ["html"] = "html",
            ["css"] = "css",
            ["xml"] = "xml",
            ["markdown"] = "md",
            ["typescript"] = "ts"
        };
        const string ChatgptUrl = "https://api.openai.com/v1/chat/completions";
        const string DefaultModel = "gpt-3.5-turbo";
        const float DefaultTemperature = 0;
        const string DefaultUserId = "user";
        string ApiKey = "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";
        string UserId;
        List<ChatGPTMessage> messageHistory;

        Dictionary<int, ChatGPTCodeBlock[]> codeBlocksDic; //解析AI回答中的代码块
        public List<ChatGPTMessage> MessageHistory => messageHistory;
        ChatGPTRequestData requestData;
        UnityWebRequest webRequest;

        int mRequestTimeout;
        public int RequestTimeout { get => mRequestTimeout; set => mRequestTimeout = Mathf.Max(value, 30); }
        public float ChatGPTRandomness { get => requestData.temperature; set { requestData.temperature = Mathf.Clamp(value, 0, 2); } }
        public bool IsRequesting => webRequest != null && !webRequest.isDone;
        public float RequestProgress => IsRequesting ? (webRequest.uploadProgress + webRequest.downloadProgress) / 2f : 0f;
        public ChatGPT(string apiKey, string userId = DefaultUserId, string model = DefaultModel, float temperature = DefaultTemperature)
        {
            this.ApiKey = apiKey;
            this.UserId = string.IsNullOrWhiteSpace(userId) ? DefaultUserId : userId;
            messageHistory = new List<ChatGPTMessage>();
            requestData = new ChatGPTRequestData(model, temperature);
            codeBlocksDic = new Dictionary<int, ChatGPTCodeBlock[]>();
        }
        public void SetAPIKey(string str)
        {
            this.ApiKey = str;
        }
        /// <summary>
        /// 接着上次的话题
        /// </summary>
        public void RestoreChatHistory()
        {
            var chatHistoryJson = EditorPrefs.GetString("ChatGPT.Settings.ChatHistory", string.Empty);
            var requestDataJson = EditorPrefs.GetString("ChatGPT.Settings.RequestData", string.Empty);
            if (!string.IsNullOrEmpty(requestDataJson))
            {
                var jsonObj = UtilityBuiltin.Json.ToObject<ChatGPTRequestData>(requestDataJson);
                if (jsonObj != null)
                {
                    requestData.messages = jsonObj.messages;
                }
            }
            if (!string.IsNullOrEmpty(chatHistoryJson))
            {
                var jsonObj = UtilityBuiltin.Json.ToObject<List<ChatGPTMessage>>(chatHistoryJson);
                if (jsonObj != null)
                {
                    messageHistory = jsonObj;
                    ParseAllMessageCodeBlocks(messageHistory);
                }
            }
        }

        private void ParseAllMessageCodeBlocks(List<ChatGPTMessage> messageHistory, bool forceAll = false)
        {
            if (messageHistory == null || messageHistory.Count < 1)
            {
                return;
            }
            if (forceAll)
            {
                codeBlocksDic.Clear();
                for (int i = 0; i < messageHistory.Count; i++)
                {
                    var msg = messageHistory[i];
                    if (IsSelfMessage(msg)) continue;

                    var codeBlocks = ParseCodeBlocks(msg.content);
                    if (codeBlocks != null)
                    {
                        codeBlocksDic.Add(i, codeBlocks);
                    }
                }
            }
            else
            {
                for (int i = 0; i < messageHistory.Count; i++)
                {
                    var msg = messageHistory[i];
                    if (IsSelfMessage(msg)) continue;
                    if (codeBlocksDic.ContainsKey(i)) continue;

                    var codeBlocks = ParseCodeBlocks(msg.content);
                    if (codeBlocks != null)
                    {
                        codeBlocksDic.Add(i, codeBlocks);
                    }
                }
            }
        }
        private ChatGPTCodeBlock[] ParseCodeBlocks(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            Regex regex = new Regex(@"```(?<language>[^\n]+)?\n(?<code>.*?)\n```", RegexOptions.Singleline);
            MatchCollection matches = regex.Matches(message);
            List<ChatGPTCodeBlock> codeBlocks = new List<ChatGPTCodeBlock>();
            foreach (Match match in matches)
            {
                string codeBlock = match.Groups["code"].Value;
                string codeTag = match.Groups["language"].Value.ToLower();
                if (!SupportCodeLanguages.ContainsKey(codeTag))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(codeBlock) && !string.IsNullOrWhiteSpace(codeTag))
                {
                    var cBlock = new ChatGPTCodeBlock()
                    {
                        Tag = codeTag,
                        Content = codeBlock,
                        FileExtension = SupportCodeLanguages[codeTag]
                    };
                    codeBlocks.Add(cBlock);
                }
            }
            return codeBlocks.Count > 0 ? codeBlocks.ToArray() : null;
        }
        public void SaveChatHistory()
        {
            if (messageHistory != null && messageHistory.Count > 0)
            {
                var chatHistoryJson = UtilityBuiltin.Json.ToJson(messageHistory);
                EditorPrefs.SetString("ChatGPT.Settings.ChatHistory", chatHistoryJson);
            }
            if (requestData != null)
            {
                var requestDataJson = UtilityBuiltin.Json.ToJson(requestData);
                EditorPrefs.SetString("ChatGPT.Settings.RequestData", requestDataJson);
            }
        }
        public void Send(string message, Action<bool, string> onComplete = null, Action<float> onProgressUpdate = null)
        {
            TMP_EditorCoroutine.StartCoroutine(Request(message, onComplete, onProgressUpdate));
        }

        public async Task<string> SendAsync(string message)
        {
            bool isCompleted = false;
            string result = string.Empty;
            Action<bool, string> onComplete = (success, str) =>
            {
                isCompleted = true;
                if (success) result = str;
            };

            TMP_EditorCoroutine.StartCoroutine(Request(message, onComplete, null));
            while (!isCompleted)
            {
                await Task.Delay(10);
            }
            return result;
        }
        private IEnumerator Request(string input, Action<bool, string> onComplete, Action<float> onProgressUpdate)
        {
            var msg = new ChatGPTMessage()
            {
                role = UserId,
                content = input,
            };
            requestData.AppendChat(msg);
            messageHistory.Add(msg);

            using (webRequest = new UnityWebRequest(ChatgptUrl, "POST"))
            {
                var jsonDt = UtilityBuiltin.Json.ToJson(requestData);
                Debug.Log(jsonDt);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonDt);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {this.ApiKey}");
                webRequest.certificateHandler = new WebRequestCertNoValidate();
                webRequest.timeout = RequestTimeout;
                var req = webRequest.SendWebRequest();
                while (!webRequest.isDone)
                {
                    onProgressUpdate?.Invoke((webRequest.downloadProgress + webRequest.uploadProgress) / 2f);
                    yield return null;
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"---------ChatGPT请求失败:{webRequest.error}---------");
                    onComplete?.Invoke(false, string.Empty);
                }
                else
                {
                    var json = webRequest.downloadHandler.text;
                    Debug.Log(json);
                    try
                    {
                        ChatCompletion result = UtilityBuiltin.Json.ToObject<ChatCompletion>(json);
                        int lastChoiceIdx = result.choices.Count - 1;
                        var replyMsg = result.choices[lastChoiceIdx].message;
                        replyMsg.content = replyMsg.content.Trim();
                        messageHistory.Add(replyMsg);
                        var codeBlock = ParseCodeBlocks(replyMsg.content);
                        if (codeBlock != null)
                        {
                            codeBlocksDic.Add(messageHistory.Count - 1, codeBlock);
                        }
                        onComplete?.Invoke(true, replyMsg.content);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"---------ChatGPT返回数据解析失败:{e.Message}---------");
                        onComplete?.Invoke(false, e.Message);
                    }
                }
                webRequest.Dispose();
                webRequest = null;
            }
        }
        public ChatGPTCodeBlock[] GetCodeBlocksByIdx(int msgIdx)
        {
            if (codeBlocksDic.TryGetValue(msgIdx, out var codeBlocks)) return codeBlocks;
            return null;
        }
        public void NewChat()
        {
            requestData.ClearChat();
            messageHistory.Clear();
            codeBlocksDic.Clear();
        }
        public bool IsSelfMessage(ChatGPTMessage msg)
        {
            return this.UserId.CompareTo(msg.role) == 0;
        }
    }

    class ChatGPTRequestData
    {
        public List<ChatGPTMessage> messages;
        public string model;
        public float temperature;

        public ChatGPTRequestData(string model, float temper)
        {
            this.model = model;
            this.temperature = temper;
            this.messages = new List<ChatGPTMessage>();
        }

        /// <summary>
        /// 同一话题追加会话内容
        /// </summary>
        /// <param name="chatMsg"></param>
        /// <returns></returns>
        public ChatGPTRequestData AppendChat(ChatGPTMessage msg)
        {
            this.messages.Add(msg);
            return this;
        }
        /// <summary>
        /// 清除聊天历史(结束一个话题), 相当于新建一个聊天话题
        /// </summary>
        public void ClearChat()
        {
            this.messages.Clear();
        }
    }
    public class ChatGPTCodeBlock
    {
        public string Tag;
        public string Content;

        public string FileExtension;
    }
    class ChatGPTUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    public class ChatGPTMessage
    {
        public string role;
        public string content;
    }

    class Choice
    {
        public ChatGPTMessage message;
        public string finish_reason;
        public int index;
    }

    class ChatCompletion
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public ChatGPTUsage usage;
        public List<Choice> choices;
    }
}

