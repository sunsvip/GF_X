using GameFramework.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro.EditorUtilities;

namespace UGF.EditorTools
{
    internal static class LocalizationTranslationService
    {
        private static LocalizationTranslationStatusSnapshot s_LastStatus = BuildIdleStatus();
        private static bool s_IsBaiduTranslationRunning;

        // 百度免费翻译API限 1 QPS, 请求间隔需 >=1s(留余量取1.1s); 失败/被限流(54003)时有限重试。
        private const double BaiduRequestIntervalSeconds = 1.1d;
        private const int BaiduMaxRequestAttempts = 3;
        private const char BaiduBatchSeparator = '↕';
        private static readonly int s_BaiduBatchSeparatorByteLength = Encoding.UTF8.GetByteCount(BaiduBatchSeparator.ToString());

        internal static LocalizationTranslationStatusSnapshot GetStatusSnapshot()
        {
            if (IsAiProviderSelected())
            {
                return LocalizationAiTranslationService.GetStatusSnapshot();
            }

            if (s_LastStatus == null)
            {
                s_LastStatus = BuildIdleStatus();
            }

            if (!s_IsBaiduTranslationRunning && s_LastStatus.State == LocalizationTranslationRunState.Idle)
            {
                s_LastStatus.Provider = LocalizationTranslationProvider.Baidu;
                s_LastStatus.TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0);
            }

            return s_LastStatus;
        }

        public static bool TranslateAllLanguages(bool forceAll = false, Action<string, int, int> onProgressUpdate = null, Action onComplete = null)
        {
            if (EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 2)
            {
                SetLocalFailure("多语言列表至少需要 2 种语言。", null);
                return false;
            }

            if (IsAiProviderSelected())
            {
                return LocalizationAiTranslationService.TranslateAllLanguages(forceAll, onProgressUpdate, onComplete);
            }

            if (string.IsNullOrWhiteSpace(EditorToolSettings.Instance.BaiduTransAppId) || string.IsNullOrWhiteSpace(EditorToolSettings.Instance.BaiduTransSecretKey))
            {
                GFBuiltin.LogError("百度翻译AppID/密钥无效, 请重新设置!");
                SetLocalFailure("百度翻译AppID/密钥无效。", "请在翻译设置中重新配置 Baidu API Key。");
                return false;
            }

            if (s_IsBaiduTranslationRunning)
            {
                SetLocalFailure("已有百度翻译任务在运行中。", null);
                return false;
            }

            s_IsBaiduTranslationRunning = true;
            UpdateStatus(
                LocalizationTranslationRunState.Preparing,
                "准备启动百度翻译。",
                null,
                0,
                Math.Max(0, EditorToolSettings.Instance.LanguagesSupport.Count - 1),
                0f);
            TMP_EditorCoroutine.StartCoroutine(TranslateAllLanguagesCoroutine(forceAll, onProgressUpdate, onComplete));
            return true;
        }

        private static IEnumerator TranslateAllLanguagesCoroutine(bool forceAll, Action<string, int, int> onProgressUpdate, Action onComplete)
        {
            IEnumerator routine = null;
            Exception failureException = null;

            try
            {
                routine = TranslateAllLanguagesCoroutineInternal(forceAll, onProgressUpdate, onComplete);
            }
            catch (Exception exception)
            {
                failureException = exception;
            }

            while (failureException == null && routine != null)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        yield break;
                    }

                    current = routine.Current;
                }
                catch (Exception exception)
                {
                    failureException = exception;
                    break;
                }

                yield return current;
            }

            if (failureException != null)
            {
                s_IsBaiduTranslationRunning = false;
                UnityEngine.Debug.LogError($"百度翻译任务失败: {failureException}");
                UpdateStatus(
                    LocalizationTranslationRunState.Failed,
                    "百度翻译任务失败",
                    failureException.Message,
                    s_LastStatus != null ? s_LastStatus.CompletedLanguages : 0,
                    s_LastStatus != null ? s_LastStatus.TotalLanguages : 0,
                    s_LastStatus != null ? s_LastStatus.Progress01 : 0f);
                onComplete?.Invoke();
            }
        }

        private static IEnumerator TranslateAllLanguagesCoroutineInternal(bool forceAll, Action<string, int, int> onProgressUpdate, Action onComplete)
        {
            var mainLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
            var mainLangTexts = new List<LocalizationText>();
            LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(mainLanguage, ref mainLangTexts);
            mainLangTexts.RemoveAll(text => string.IsNullOrWhiteSpace(text.Value));

            var mainTextMap = new Dictionary<string, string>(mainLangTexts.Count, StringComparer.Ordinal);
            for (int i = 0; i < mainLangTexts.Count; i++)
            {
                var text = mainLangTexts[i];
                mainTextMap[text.Key] = text.Value;
            }

            var languageTasks = new List<LanguageTranslationTasks>(EditorToolSettings.Instance.LanguagesSupport.Count - 1);
            int totalTaskCount = 0;
            for (int i = 1; i < EditorToolSettings.Instance.LanguagesSupport.Count; i++)
            {
                var lang = (Language)EditorToolSettings.Instance.LanguagesSupport[i];
                if (lang == Language.Unspecified || BaiduTranslationService.GetBaiduLanguage(lang) == null)
                {
                    UnityEngine.Debug.LogWarning($"跳过百度翻译不支持的语言: {lang}");
                    continue;
                }

                var langTexts = new List<LocalizationText>();
                LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(lang, ref langTexts);
                var tasks = CreateTranslationTasks(mainTextMap, langTexts, forceAll);
                totalTaskCount += tasks.Count;
                languageTasks.Add(new LanguageTranslationTasks(lang, langTexts, tasks));
            }

            int completedTaskCount = 0;
            int completedLanguageCount = 0;
            int totalLanguageCount = languageTasks.Count;
            UpdateStatus(
                LocalizationTranslationRunState.Running,
                totalTaskCount > 0 ? "百度翻译运行中。" : "没有需要翻译的空白文本。",
                totalTaskCount > 0 ? "等待发送翻译请求。" : "所有目标语言已是最新状态。",
                0,
                totalLanguageCount,
                totalTaskCount > 0 ? 0.03f : 1f);
            onProgressUpdate?.Invoke("翻译多语言Excel", Math.Max(totalTaskCount, 1), completedTaskCount);

            double nextRequestAllowedTime = 0d;
            for (int i = 0; i < languageTasks.Count; i++)
            {
                var languageTask = languageTasks[i];
                bool hasChanged = false;
                if (languageTask.Tasks.Count == 0)
                {
                    completedLanguageCount++;
                    UpdateStatus(
                        LocalizationTranslationRunState.Running,
                        $"跳过语言: {languageTask.Language}",
                        "该语言没有需要翻译的空白文本。",
                        completedLanguageCount,
                        totalLanguageCount,
                        CalculateBaiduProgress(completedTaskCount, totalTaskCount, completedLanguageCount, totalLanguageCount));
                    continue;
                }

                var batchBuilder = new StringBuilder(EditorToolSettings.Instance.BaiduTransMaxLength);
                int taskIndex = 0;
                while (taskIndex < languageTask.Tasks.Count)
                {
                    int batchStartIndex = taskIndex;
                    int batchByteLength = 0;
                    batchBuilder.Clear();
                    while (taskIndex < languageTask.Tasks.Count)
                    {
                        var task = languageTask.Tasks[taskIndex];
                        int appendByteLength = batchByteLength == 0
                            ? task.SourceByteLength
                            : task.SourceByteLength + s_BaiduBatchSeparatorByteLength;
                        if (batchByteLength + appendByteLength > EditorToolSettings.Instance.BaiduTransMaxLength)
                        {
                            break;
                        }

                        if (batchByteLength > 0)
                        {
                            batchBuilder.Append(BaiduBatchSeparator);
                        }

                        batchBuilder.Append(task.SourceText);
                        batchByteLength += appendByteLength;
                        taskIndex++;
                    }

                    int batchTaskCount = taskIndex - batchStartIndex;
                    if (batchTaskCount == 0)
                    {
                        UnityEngine.Debug.LogError($"翻译跳过, 文本超过百度翻译批次字节限长({EditorToolSettings.Instance.BaiduTransMaxLength}).");
                        taskIndex++;
                        completedTaskCount++;
                        continue;
                    }

                    UpdateStatus(
                        LocalizationTranslationRunState.Running,
                        $"百度翻译: {languageTask.Language}",
                        $"批次文本 {batchStartIndex + 1}-{taskIndex}/{languageTask.Tasks.Count}",
                        completedLanguageCount,
                        totalLanguageCount,
                        CalculateBaiduProgress(completedTaskCount, totalTaskCount, completedLanguageCount, totalLanguageCount));

                    string[] translatedTexts = null;
                    for (int attempt = 0; attempt < BaiduMaxRequestAttempts; attempt++)
                    {
                        // 百度免费API限 1 QPS, 每次请求前按时间戳节流等待(TMP_EditorCoroutine 无 WaitForSeconds 支持, 以 yield null 逐帧等待)
                        while (UnityEditor.EditorApplication.timeSinceStartup < nextRequestAllowedTime)
                        {
                            yield return null;
                        }

                        TranslationResult result = null;
                        bool requestSuccess = false;
                        var request = BaiduTranslationService.TranslateCoroutine(batchBuilder.ToString(), mainLanguage, languageTask.Language, (success, trans, userDt) =>
                        {
                            requestSuccess = success;
                            result = trans;
                        }, null);
                        while (request.MoveNext())
                        {
                            yield return request.Current;
                        }

                        nextRequestAllowedTime = UnityEditor.EditorApplication.timeSinceStartup + BaiduRequestIntervalSeconds;
                        if (requestSuccess && TryGetBatchTranslatedTexts(result, batchTaskCount, out translatedTexts))
                        {
                            break;
                        }
                    }

                    if (translatedTexts != null)
                    {
                        for (int batchTaskIndex = 0; batchTaskIndex < batchTaskCount; batchTaskIndex++)
                        {
                            var task = languageTask.Tasks[batchStartIndex + batchTaskIndex];
                            languageTask.Texts[task.TargetIndex].Value = ApplySourcePadding(task.SourceText, translatedTexts[batchTaskIndex]);
                        }

                        hasChanged = true;
                    }

                    completedTaskCount += batchTaskCount;
                    float requestProgress = CalculateBaiduProgress(completedTaskCount, totalTaskCount, completedLanguageCount, totalLanguageCount);
                    UpdateStatus(
                        LocalizationTranslationRunState.Running,
                        $"百度翻译: {languageTask.Language}",
                        $"已完成文本 {taskIndex}/{languageTask.Tasks.Count}",
                        completedLanguageCount,
                        totalLanguageCount,
                        requestProgress);
                    onProgressUpdate?.Invoke($"翻译多语言:{languageTask.Language} ({taskIndex}/{languageTask.Tasks.Count})", Math.Max(totalTaskCount, 1), completedTaskCount);
                }

                UpdateStatus(
                    LocalizationTranslationRunState.Syncing,
                    $"同步语言 Excel: {languageTask.Language}",
                    hasChanged ? "保存翻译结果到语言 Excel。" : "该语言无新增翻译结果。",
                    completedLanguageCount,
                    totalLanguageCount,
                    CalculateSyncingProgress(completedLanguageCount, totalLanguageCount));

                if (hasChanged)
                {
                    LocalizationLanguageExcelRepository.SaveLanguage(languageTask.Language, languageTask.Texts);
                }

                completedLanguageCount++;
                UpdateStatus(
                    LocalizationTranslationRunState.Syncing,
                    $"已完成语言: {languageTask.Language}",
                    null,
                    completedLanguageCount,
                    totalLanguageCount,
                    CalculateSyncingProgress(completedLanguageCount, totalLanguageCount));
            }

            s_IsBaiduTranslationRunning = false;
            UpdateStatus(
                LocalizationTranslationRunState.Completed,
                $"百度翻译完成，已处理 {completedLanguageCount} 种语言。",
                null,
                completedLanguageCount,
                totalLanguageCount,
                1f);
            onComplete?.Invoke();
        }

        private static bool IsAiProviderSelected()
        {
            return (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider != LocalizationTranslationProvider.Baidu;
        }

        private static List<TranslationTask> CreateTranslationTasks(Dictionary<string, string> mainTextMap, List<LocalizationText> langTexts, bool forceAll)
        {
            var tasks = new List<TranslationTask>();
            int maxByteLength = EditorToolSettings.Instance.BaiduTransMaxLength;
            for (int i = 0; i < langTexts.Count; i++)
            {
                var text = langTexts[i];
                if (!forceAll && !string.IsNullOrWhiteSpace(text.Value))
                {
                    continue;
                }

                if (!mainTextMap.TryGetValue(text.Key, out string srcText) || string.IsNullOrWhiteSpace(srcText))
                {
                    continue;
                }

                if (srcText.IndexOf(BaiduBatchSeparator) >= 0)
                {
                    UnityEngine.Debug.LogError($"翻译跳过, 源文本包含百度翻译批次保留分隔符 '{BaiduBatchSeparator}'. key:{text.Key}");
                    continue;
                }

                if (Encoding.UTF8.GetByteCount(srcText) > maxByteLength)
                {
                    UnityEngine.Debug.LogError($"翻译跳过, 文本超过百度翻译批次字节限长({maxByteLength}). key:{text.Key}");
                    continue;
                }

                tasks.Add(new TranslationTask(i, srcText));
            }

            return tasks;
        }

        private static bool TryGetBatchTranslatedTexts(TranslationResult result, int expectedCount, out string[] translatedTexts)
        {
            translatedTexts = null;
            if (result == null || string.IsNullOrWhiteSpace(result.src) || string.IsNullOrWhiteSpace(result.dst))
            {
                return false;
            }

            string[] sourceTexts = result.src.Split(BaiduBatchSeparator);
            string[] destinationTexts = result.dst.Split(BaiduBatchSeparator);
            if (sourceTexts.Length != expectedCount || destinationTexts.Length != expectedCount)
            {
                UnityEngine.Debug.LogError($"百度翻译批次结果数量不一致. expected:{expectedCount}, src:{sourceTexts.Length}, dst:{destinationTexts.Length}");
                return false;
            }

            for (int i = 0; i < destinationTexts.Length; i++)
            {
                destinationTexts[i] = destinationTexts[i].Trim();
            }

            translatedTexts = destinationTexts;
            return true;
        }

        private static string ApplySourcePadding(string srcStr, string dstStr)
        {
            int leadingSpaces = srcStr.Length - srcStr.TrimStart().Length;
            int trailingSpaces = srcStr.Length - srcStr.TrimEnd().Length;
            dstStr = dstStr.PadLeft(dstStr.Length + leadingSpaces);
            return dstStr.PadRight(dstStr.Length + trailingSpaces);
        }

        private static LocalizationTranslationStatusSnapshot BuildIdleStatus()
        {
            return new LocalizationTranslationStatusSnapshot
            {
                IsRunning = false,
                Provider = LocalizationTranslationProvider.Baidu,
                State = LocalizationTranslationRunState.Idle,
                Message = "待命",
                Detail = string.Empty,
                ErrorMessage = string.Empty,
                LastStdout = string.Empty,
                LastStderr = string.Empty,
                WorkingDirectory = string.Empty,
                CompletedLanguages = 0,
                TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0),
                Progress01 = 0f
            };
        }

        private static void SetLocalFailure(string message, string detail)
        {
            s_IsBaiduTranslationRunning = false;
            s_LastStatus = new LocalizationTranslationStatusSnapshot
            {
                IsRunning = false,
                Provider = LocalizationTranslationProvider.Baidu,
                State = LocalizationTranslationRunState.Failed,
                Message = message ?? string.Empty,
                Detail = detail ?? string.Empty,
                ErrorMessage = !string.IsNullOrWhiteSpace(detail) ? detail : message ?? string.Empty,
                LastStdout = string.Empty,
                LastStderr = string.Empty,
                WorkingDirectory = string.Empty,
                CompletedLanguages = 0,
                TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0),
                Progress01 = 0f
            };
        }

        private static void UpdateStatus(LocalizationTranslationRunState state, string message, string detail, int completedLanguages, int totalLanguages, float progress01)
        {
            if (s_LastStatus == null)
            {
                s_LastStatus = BuildIdleStatus();
            }

            s_LastStatus.IsRunning = state == LocalizationTranslationRunState.Preparing
                || state == LocalizationTranslationRunState.Running
                || state == LocalizationTranslationRunState.Validating
                || state == LocalizationTranslationRunState.Syncing;
            s_LastStatus.Provider = LocalizationTranslationProvider.Baidu;
            s_LastStatus.State = state;
            s_LastStatus.Message = message ?? string.Empty;
            s_LastStatus.Detail = detail ?? string.Empty;
            s_LastStatus.ErrorMessage = state == LocalizationTranslationRunState.Failed
                ? ((!string.IsNullOrWhiteSpace(detail) ? detail : message) ?? string.Empty)
                : string.Empty;
            s_LastStatus.LastStdout = string.Empty;
            s_LastStatus.LastStderr = string.Empty;
            s_LastStatus.WorkingDirectory = string.Empty;
            s_LastStatus.CompletedLanguages = completedLanguages;
            s_LastStatus.TotalLanguages = totalLanguages;
            s_LastStatus.Progress01 = Clamp01(progress01);
        }

        private static float CalculateBaiduProgress(int completedTaskCount, int totalTaskCount, int completedLanguageCount, int totalLanguageCount)
        {
            const float runningStart = 0.03f;
            const float runningEnd = 0.9f;
            if (totalTaskCount > 0)
            {
                float ratio = completedTaskCount / (float)Math.Max(1, totalTaskCount);
                return runningStart + (runningEnd - runningStart) * Clamp01(ratio);
            }

            if (totalLanguageCount > 0)
            {
                float ratio = completedLanguageCount / (float)Math.Max(1, totalLanguageCount);
                return runningStart + (runningEnd - runningStart) * Clamp01(ratio);
            }

            return 1f;
        }

        private static float CalculateSyncingProgress(int completedLanguageCount, int totalLanguageCount)
        {
            const float syncingStart = 0.92f;
            const float syncingEnd = 0.99f;
            if (totalLanguageCount < 1)
            {
                return 1f;
            }

            float ratio = completedLanguageCount / (float)Math.Max(1, totalLanguageCount);
            return syncingStart + (syncingEnd - syncingStart) * Clamp01(ratio);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private readonly struct TranslationTask
        {
            public readonly int TargetIndex;
            public readonly string SourceText;
            public readonly int SourceByteLength;

            public TranslationTask(int targetIndex, string sourceText)
            {
                TargetIndex = targetIndex;
                SourceText = sourceText;
                SourceByteLength = Encoding.UTF8.GetByteCount(sourceText);
            }
        }

        private sealed class LanguageTranslationTasks
        {
            public readonly Language Language;
            public readonly List<LocalizationText> Texts;
            public readonly List<TranslationTask> Tasks;

            public LanguageTranslationTasks(Language language, List<LocalizationText> texts, List<TranslationTask> tasks)
            {
                Language = language;
                Texts = texts;
                Tasks = tasks;
            }
        }
    }
}
