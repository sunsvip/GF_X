using GameFramework.Localization;
using System;
using System.Collections.Generic;
using System.IO;

namespace UGF.EditorTools
{
    internal static class LocalizationAiTranslationService
    {
        private static LocalizationTranslationStatusSnapshot s_LastStatus = BuildIdleStatus();

        internal static bool IsRunning => AiCliTaskExecutor.IsRunning;

        internal static bool CancelCurrentTask()
        {
            return AiCliTaskExecutor.Cancel("用户取消本地化 AI 翻译任务。");
        }

        internal static LocalizationTranslationStatusSnapshot GetStatusSnapshot()
        {
            AiCliTaskStatusSnapshot taskStatus = AiCliTaskExecutor.GetStatusSnapshot();
            if (taskStatus.IsRunning || taskStatus.State != AiCliTaskState.Idle)
            {
                s_LastStatus = MapStatus(taskStatus);
                return s_LastStatus;
            }

            if (s_LastStatus == null)
            {
                s_LastStatus = BuildIdleStatus();
            }

            if (!s_LastStatus.IsRunning && s_LastStatus.State == LocalizationTranslationRunState.Idle)
            {
                s_LastStatus.Provider = (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider;
                s_LastStatus.TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0);
            }

            return s_LastStatus;
        }

        internal static bool TranslateAllLanguages(bool forceAll = false, Action<string, int, int> onProgressUpdate = null, Action onComplete = null)
        {
            if (AiCliTaskExecutor.IsRunning)
            {
                SetLocalFailure("已有 AI 翻译任务在运行中。", null);
                return false;
            }

            if (EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 2)
            {
                SetLocalFailure("多语言列表至少需要 2 种语言。", null);
                return false;
            }

            string promptTemplatePath = ResolvePromptTemplatePath();
            if (string.IsNullOrWhiteSpace(promptTemplatePath) || !File.Exists(promptTemplatePath))
            {
                SetLocalFailure($"Prompt 模板不存在: {promptTemplatePath}", null);
                return false;
            }

            Language sourceLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
            if (sourceLanguage == Language.Unspecified)
            {
                SetLocalFailure("母语无效，LanguagesSupport[0] 不能是 Unspecified。", null);
                return false;
            }

            var sourceTexts = new List<LocalizationText>();
            LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(sourceLanguage, ref sourceTexts);
            if (sourceTexts.Count < 1)
            {
                SetLocalFailure($"母语 Excel 为空: {sourceLanguage}", null);
                return false;
            }

            var targetLanguages = new List<Language>(EditorToolSettings.Instance.LanguagesSupport.Count - 1);
            for (int i = 1; i < EditorToolSettings.Instance.LanguagesSupport.Count; i++)
            {
                Language language = (Language)EditorToolSettings.Instance.LanguagesSupport[i];
                if (language == Language.Unspecified)
                {
                    continue;
                }

                targetLanguages.Add(language);
            }

            if (targetLanguages.Count < 1)
            {
                SetLocalFailure("没有可翻译的目标语言。", null);
                return false;
            }

            var taskDefinition = new LocalizationAiTranslationTaskDefinition(
                MapProvider((LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider),
                forceAll,
                promptTemplatePath,
                sourceLanguage,
                sourceTexts,
                targetLanguages);

            bool started = AiCliTaskExecutor.Start(
                taskDefinition,
                EditorToolSettings.Instance.LocalizationAiShowDebugCommandWindow,
                onProgressUpdate,
                onComplete);
            if (started)
            {
                s_LastStatus = MapStatus(AiCliTaskExecutor.GetStatusSnapshot());
                return true;
            }

            s_LastStatus = MapStatus(AiCliTaskExecutor.GetStatusSnapshot());
            return false;
        }

        private static string ResolvePromptTemplatePath()
        {
            string relativePath = EditorToolSettings.Instance.LocalizationAiPromptTemplatePath;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            return Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.GetFullPath(Path.Combine(ConstEditor.ProjectRootPath, relativePath));
        }

        private static void SetLocalFailure(string message, string detail)
        {
            s_LastStatus = new LocalizationTranslationStatusSnapshot
            {
                IsRunning = false,
                Provider = (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider,
                State = LocalizationTranslationRunState.Failed,
                Message = message ?? string.Empty,
                Detail = detail ?? string.Empty,
                ErrorMessage = !string.IsNullOrWhiteSpace(detail) ? detail : message ?? string.Empty,
                LastStdout = string.Empty,
                LastStderr = string.Empty,
                WorkingDirectory = Path.Combine(ConstEditor.ProjectRootPath, "Library/LocalizationTranslate"),
                CompletedLanguages = 0,
                TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0),
                Progress01 = 0f
            };
        }

        private static LocalizationTranslationStatusSnapshot BuildIdleStatus()
        {
            return new LocalizationTranslationStatusSnapshot
            {
                IsRunning = false,
                Provider = (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider,
                State = LocalizationTranslationRunState.Idle,
                Message = "待命",
                Detail = string.Empty,
                ErrorMessage = string.Empty,
                LastStdout = string.Empty,
                LastStderr = string.Empty,
                WorkingDirectory = Path.Combine(ConstEditor.ProjectRootPath, "Library/LocalizationTranslate"),
                CompletedLanguages = 0,
                TotalLanguages = Math.Max(0, EditorToolSettings.Instance.LanguagesSupport != null ? EditorToolSettings.Instance.LanguagesSupport.Count - 1 : 0),
                Progress01 = 0f
            };
        }

        private static LocalizationTranslationStatusSnapshot MapStatus(AiCliTaskStatusSnapshot status)
        {
            if (status == null)
            {
                return BuildIdleStatus();
            }

            return new LocalizationTranslationStatusSnapshot
            {
                IsRunning = status.IsRunning,
                Provider = MapProvider(status.Provider),
                State = MapState(status.State),
                Message = status.Message ?? string.Empty,
                Detail = status.Detail ?? string.Empty,
                ErrorMessage = status.ErrorMessage ?? string.Empty,
                LastStdout = status.LastStdout ?? string.Empty,
                LastStderr = status.LastStderr ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(status.WorkingDirectory)
                    ? Path.Combine(ConstEditor.ProjectRootPath, "Library/LocalizationTranslate")
                    : status.WorkingDirectory,
                CompletedLanguages = status.CompletedUnits,
                TotalLanguages = status.TotalUnits,
                Progress01 = status.Progress01
            };
        }

        private static AiCliProvider MapProvider(LocalizationTranslationProvider provider)
        {
            switch (provider)
            {
                case LocalizationTranslationProvider.ClaudeCodeCli:
                    return AiCliProvider.ClaudeCodeCli;
                case LocalizationTranslationProvider.OpenCodeCli:
                    return AiCliProvider.OpenCodeCli;
                default:
                    return AiCliProvider.CodexCli;
            }
        }

        private static LocalizationTranslationProvider MapProvider(AiCliProvider provider)
        {
            switch (provider)
            {
                case AiCliProvider.ClaudeCodeCli:
                    return LocalizationTranslationProvider.ClaudeCodeCli;
                case AiCliProvider.OpenCodeCli:
                    return LocalizationTranslationProvider.OpenCodeCli;
                default:
                    return LocalizationTranslationProvider.CodexCli;
            }
        }

        private static LocalizationTranslationRunState MapState(AiCliTaskState state)
        {
            switch (state)
            {
                case AiCliTaskState.Preparing:
                    return LocalizationTranslationRunState.Preparing;
                case AiCliTaskState.Running:
                    return LocalizationTranslationRunState.Running;
                case AiCliTaskState.Validating:
                    return LocalizationTranslationRunState.Validating;
                case AiCliTaskState.Applying:
                    return LocalizationTranslationRunState.Syncing;
                case AiCliTaskState.Completed:
                    return LocalizationTranslationRunState.Completed;
                case AiCliTaskState.Failed:
                    return LocalizationTranslationRunState.Failed;
                default:
                    return LocalizationTranslationRunState.Idle;
            }
        }
    }
}
