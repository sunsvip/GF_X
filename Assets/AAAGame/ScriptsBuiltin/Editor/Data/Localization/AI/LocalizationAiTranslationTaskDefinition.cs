using GameFramework.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class LocalizationAiTranslationTaskDefinition : IAiCliTaskDefinition
    {
        private const string SourceFileName = "source.json";
        private static readonly Regex PlaceholderRegex = new Regex(@"\{[^{}]+\}", RegexOptions.Compiled);
        private static readonly Regex RichTextRegex = new Regex(@"<[^<>]+>", RegexOptions.Compiled);
        private sealed class LocalizationAgentLanguageDocument
        {
            [JsonProperty("language")]
            public string Language;

            [JsonProperty("items")]
            public List<LocalizationAgentTextItem> Items;
        }

        private sealed class LocalizationAgentTextItem
        {
            [JsonProperty("key")]
            public string Key;

            [JsonProperty("text")]
            public string Text;
        }

        private sealed class Payload
        {
            public bool ForceAll;
            public string PromptTemplatePath;
            public Language SourceLanguage;
            public List<LocalizationText> SourceTexts;
            public List<Language> TargetLanguages;
            public string SourceJsonPath;
        }

        private readonly AiCliProvider _provider;
        private readonly bool _forceAll;
        private readonly string _promptTemplatePath;
        private readonly Language _sourceLanguage;
        private readonly List<LocalizationText> _sourceTexts;
        private readonly List<Language> _targetLanguages;

        public LocalizationAiTranslationTaskDefinition(
            AiCliProvider provider,
            bool forceAll,
            string promptTemplatePath,
            Language sourceLanguage,
            List<LocalizationText> sourceTexts,
            List<Language> targetLanguages)
        {
            _provider = provider;
            _forceAll = forceAll;
            _promptTemplatePath = promptTemplatePath;
            _sourceLanguage = sourceLanguage;
            _sourceTexts = sourceTexts;
            _targetLanguages = targetLanguages;
        }

        public string TaskName => "本地化 AI 翻译";
        public AiCliProvider Provider => _provider;
        public string WorkingDirectoryName => "Library/LocalizationTranslate";

        public void PrepareInputs(AiCliTaskContext context)
        {
            var payload = new Payload
            {
                ForceAll = _forceAll,
                PromptTemplatePath = _promptTemplatePath,
                SourceLanguage = _sourceLanguage,
                SourceTexts = _sourceTexts,
                TargetLanguages = _targetLanguages,
                SourceJsonPath = Path.Combine(context.WorkingDirectory, SourceFileName)
            };
            context.Payload = payload;
            WriteSourceJson(payload.SourceJsonPath, payload.SourceLanguage, payload.SourceTexts);
        }

        public string BuildPrompt(AiCliTaskContext context)
        {
            Payload payload = GetPayload(context);
            string template = File.ReadAllText(payload.PromptTemplatePath, Encoding.UTF8);
            return template
                .Replace("[SOURCE_JSON_PATH]", payload.SourceJsonPath)
                .Replace("[SOURCE_LANGUAGE]", payload.SourceLanguage.ToString())
                .Replace("[TARGET_LANGUAGES]", BuildTargetLanguagesTag(payload.TargetLanguages))
                .Replace("[LANGUAGE_ENUM_CONSTRAINT]", BuildLanguageEnumConstraintTag())
                .Replace("[OUTPUT_DIR]", context.OutputDirectory);
        }

        public AiCliTaskProgressInfo BuildRunningProgress(AiCliTaskContext context)
        {
            Payload payload = GetPayload(context);
            int completedLanguages = CountCompletedLanguages(context.OutputDirectory, payload.TargetLanguages);
            string pendingLanguage = completedLanguages < payload.TargetLanguages.Count
                ? payload.TargetLanguages[completedLanguages].ToString()
                : string.Empty;
            string detail = !string.IsNullOrWhiteSpace(context.LastProgressDetail)
                ? context.LastProgressDetail
                : string.IsNullOrWhiteSpace(pendingLanguage)
                    ? "等待全部输出文件完成写入。"
                    : $"等待生成 {pendingLanguage}.json";

            return new AiCliTaskProgressInfo
            {
                Message = $"AI 翻译运行中 ({completedLanguages}/{payload.TargetLanguages.Count})",
                Detail = detail,
                CompletedUnits = completedLanguages,
                TotalUnits = payload.TargetLanguages.Count,
                Progress01 = CalculateRunningProgress(context, completedLanguages, payload.TargetLanguages.Count)
            };
        }

        public bool TryFinalize(AiCliTaskContext context, bool failOnValidationError, out string completionMessage, out string errorMessage)
        {
            completionMessage = null;
            errorMessage = null;
            Payload payload = GetPayload(context);
            if (!HaveAllOutputFiles(context.OutputDirectory, payload.TargetLanguages))
            {
                return false;
            }

            context.Progress01 = Mathf.Max(context.Progress01, 0.9f);
            context.ReportStatus?.Invoke(AiCliTaskState.Validating, "AI 输出已生成，开始验证。", null, payload.TargetLanguages.Count, payload.TargetLanguages.Count, context.Progress01);

            var documents = new List<LocalizationAgentLanguageDocument>(payload.TargetLanguages.Count);
            for (int i = 0; i < payload.TargetLanguages.Count; i++)
            {
                Language targetLanguage = payload.TargetLanguages[i];
                string outputFilePath = Path.Combine(context.OutputDirectory, targetLanguage + ".json");
                LocalizationAgentLanguageDocument document = LoadAndValidateOutputDocument(targetLanguage, outputFilePath, payload.SourceTexts, out errorMessage);
                if (document == null)
                {
                    if (failOnValidationError)
                    {
                        return false;
                    }

                    errorMessage = null;
                    return false;
                }

                documents.Add(document);
            }

            int importedLanguages = 0;
            for (int i = 0; i < payload.TargetLanguages.Count; i++)
            {
                Language targetLanguage = payload.TargetLanguages[i];
                float syncingProgress = CalculateSyncingProgress(context, importedLanguages, payload.TargetLanguages.Count);
                context.ReportStatus?.Invoke(AiCliTaskState.Applying, $"同步语言 Excel: {targetLanguage}", null, importedLanguages, payload.TargetLanguages.Count, syncingProgress);
                ImportLanguageDocument(targetLanguage, documents[i], payload.SourceTexts, payload.ForceAll);
                importedLanguages++;
                syncingProgress = CalculateSyncingProgress(context, importedLanguages, payload.TargetLanguages.Count);
                context.ReportStatus?.Invoke(AiCliTaskState.Applying, $"已同步语言: {targetLanguage}", null, importedLanguages, payload.TargetLanguages.Count, syncingProgress);
            }

            completionMessage = !string.IsNullOrWhiteSpace(context.TerminalSuccessMessage)
                ? context.TerminalSuccessMessage
                : $"AI 翻译完成，已同步 {importedLanguages} 种语言。";
            return true;
        }

        private static Payload GetPayload(AiCliTaskContext context)
        {
            return context.Payload as Payload ?? throw new InvalidOperationException("Localization AI payload is missing.");
        }

        private static void WriteSourceJson(string sourceJsonPath, Language sourceLanguage, List<LocalizationText> sourceTexts)
        {
            var document = new LocalizationAgentLanguageDocument
            {
                Language = sourceLanguage.ToString(),
                Items = new List<LocalizationAgentTextItem>(sourceTexts.Count)
            };
            for (int i = 0; i < sourceTexts.Count; i++)
            {
                document.Items.Add(new LocalizationAgentTextItem
                {
                    Key = sourceTexts[i].Key,
                    Text = sourceTexts[i].Value ?? string.Empty
                });
            }

            File.WriteAllText(sourceJsonPath, JsonConvert.SerializeObject(document, Formatting.Indented), new UTF8Encoding(false));
        }

        private static string BuildTargetLanguagesTag(List<Language> targetLanguages)
        {
            var builder = new StringBuilder(targetLanguages.Count * 32);
            for (int i = 0; i < targetLanguages.Count; i++)
            {
                Language language = targetLanguages[i];
                builder.Append("- ");
                builder.Append(language);
                builder.Append(" -> ");
                builder.Append(language);
                builder.Append(".json");
                if (i + 1 < targetLanguages.Count)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string BuildLanguageEnumConstraintTag()
        {
            Language[] allLanguages = (Language[])Enum.GetValues(typeof(Language));
            var builder = new StringBuilder(allLanguages.Length * 24);
            for (int i = 0; i < allLanguages.Length; i++)
            {
                builder.Append("- ");
                builder.Append(allLanguages[i]);
                if (i + 1 < allLanguages.Length)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static int CountCompletedLanguages(string outputDirectory, List<Language> targetLanguages)
        {
            int completedLanguages = 0;
            for (int i = 0; i < targetLanguages.Count; i++)
            {
                string outputFilePath = Path.Combine(outputDirectory, targetLanguages[i] + ".json");
                if (File.Exists(outputFilePath))
                {
                    completedLanguages++;
                }
            }

            return completedLanguages;
        }

        private static bool HaveAllOutputFiles(string outputDirectory, List<Language> targetLanguages)
        {
            return CountCompletedLanguages(outputDirectory, targetLanguages) == targetLanguages.Count;
        }

        private static float CalculateRunningProgress(AiCliTaskContext context, int completedLanguages, int totalLanguages)
        {
            const float runningStart = 0.10f;
            const float runningEnd = 0.88f;
            int total = Math.Max(1, totalLanguages);
            int completed = Mathf.Clamp(completedLanguages, 0, total);
            float segmentSize = (runningEnd - runningStart) / total;
            float segmentStart = runningStart + completed * segmentSize;
            float segmentEnd = completed >= total
                ? runningEnd
                : runningStart + (completed + 1) * segmentSize - Mathf.Min(0.01f, segmentSize * 0.25f);
            segmentEnd = Mathf.Max(segmentStart, segmentEnd);

            double now = EditorApplication.timeSinceStartup;
            double deltaTime = Math.Max(0d, now - context.LastProgressTimestamp);
            context.LastProgressTimestamp = now;

            if (context.Progress01 < segmentStart)
            {
                context.Progress01 = segmentStart;
            }

            float progressed = context.Progress01 + (float)(deltaTime * 0.015d);
            context.Progress01 = Mathf.Clamp(progressed, context.Progress01, segmentEnd);
            return context.Progress01;
        }

        private static float CalculateSyncingProgress(AiCliTaskContext context, int importedLanguages, int totalLanguages)
        {
            const float syncingStart = 0.94f;
            const float syncingEnd = 0.99f;
            int total = Math.Max(1, totalLanguages);
            float ratio = Mathf.Clamp01(importedLanguages / (float)total);
            context.Progress01 = Mathf.Max(context.Progress01, Mathf.Lerp(syncingStart, syncingEnd, ratio));
            return context.Progress01;
        }

        private static LocalizationAgentLanguageDocument LoadAndValidateOutputDocument(Language targetLanguage, string outputFilePath, List<LocalizationText> sourceTexts, out string error)
        {
            error = null;
            if (!File.Exists(outputFilePath))
            {
                error = $"缺少目标语言输出文件: {outputFilePath}";
                return null;
            }

            LocalizationAgentLanguageDocument document;
            try
            {
                document = JsonConvert.DeserializeObject<LocalizationAgentLanguageDocument>(File.ReadAllText(outputFilePath, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                error = $"解析目标语言 JSON 失败({targetLanguage}): {exception.Message}";
                return null;
            }

            if (document == null)
            {
                error = $"目标语言 JSON 为空: {targetLanguage}";
                return null;
            }

            string fileLanguageName = Path.GetFileNameWithoutExtension(outputFilePath);
            if (!Enum.TryParse(document.Language, false, out Language parsedLanguage))
            {
                error = $"language 字段不是合法 Language 枚举名: {document.Language}";
                return null;
            }

            if (!string.Equals(document.Language, fileLanguageName, StringComparison.Ordinal))
            {
                error = $"language 字段与文件名不一致: {document.Language} != {fileLanguageName}";
                return null;
            }

            if (parsedLanguage != targetLanguage)
            {
                error = $"language 字段与目标语言不一致: {document.Language} != {targetLanguage}";
                return null;
            }

            if (document.Items == null)
            {
                error = $"items 字段为空: {targetLanguage}";
                return null;
            }

            if (document.Items.Count != sourceTexts.Count)
            {
                error = $"items 数量与母语不一致({targetLanguage}): {document.Items.Count} != {sourceTexts.Count}";
                return null;
            }

            for (int i = 0; i < sourceTexts.Count; i++)
            {
                LocalizationText sourceText = sourceTexts[i];
                LocalizationAgentTextItem targetItem = document.Items[i];
                if (targetItem == null)
                {
                    error = $"items[{i}] 为空: {targetLanguage}";
                    return null;
                }

                if (!string.Equals(targetItem.Key, sourceText.Key, StringComparison.Ordinal))
                {
                    error = $"items[{i}].key 与母语不一致({targetLanguage}): {targetItem.Key} != {sourceText.Key}";
                    return null;
                }

                string sourceValue = sourceText.Value ?? string.Empty;
                string targetValue = NormalizeLineBreakRepresentation(sourceValue, targetItem.Text ?? string.Empty);
                targetItem.Text = targetValue;
                if (!CompareTokenSequence(PlaceholderRegex, sourceValue, targetValue))
                {
                    error = $"占位符序列被破坏({targetLanguage}): {sourceText.Key}";
                    return null;
                }

                if (!CompareTokenSequence(RichTextRegex, sourceValue, targetValue))
                {
                    error = $"富文本标签序列被破坏({targetLanguage}): {sourceText.Key}";
                    return null;
                }

                if (CountLineBreakUnits(sourceValue) != CountLineBreakUnits(targetValue))
                {
                    error = $"换行数量被破坏({targetLanguage}): {sourceText.Key}";
                    return null;
                }

            }

            return document;
        }

        private static bool CompareTokenSequence(Regex regex, string source, string target)
        {
            MatchCollection sourceMatches = regex.Matches(source ?? string.Empty);
            MatchCollection targetMatches = regex.Matches(target ?? string.Empty);
            if (sourceMatches.Count != targetMatches.Count)
            {
                return false;
            }

            for (int i = 0; i < sourceMatches.Count; i++)
            {
                if (!string.Equals(sourceMatches[i].Value, targetMatches[i].Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountLineBreakUnits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (current == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    count++;
                    continue;
                }

                if (current == '\n')
                {
                    count++;
                    continue;
                }

                if (current != '\\' || i + 1 >= text.Length)
                {
                    continue;
                }

                if (text[i + 1] == 'n')
                {
                    count++;
                    i++;
                    continue;
                }

                if (text[i + 1] == 'r' && i + 3 < text.Length && text[i + 2] == '\\' && text[i + 3] == 'n')
                {
                    count++;
                    i += 3;
                }
            }

            return count;
        }

        private static string NormalizeLineBreakRepresentation(string source, string target)
        {
            source = source ?? string.Empty;
            target = target ?? string.Empty;

            bool sourceHasActualLineBreak = source.IndexOf('\n') >= 0 || source.IndexOf('\r') >= 0;
            bool sourceHasEscapedCrLf = source.IndexOf("\\r\\n", StringComparison.Ordinal) >= 0;
            bool sourceHasEscapedLf = source.IndexOf("\\n", StringComparison.Ordinal) >= 0;

            if (!sourceHasActualLineBreak && (sourceHasEscapedCrLf || sourceHasEscapedLf))
            {
                string escapedLineBreak = sourceHasEscapedCrLf ? "\\r\\n" : "\\n";
                string normalizedTarget = target.Replace("\r\n", "\n").Replace("\r", "\n");
                return normalizedTarget.Replace("\n", escapedLineBreak);
            }

            if (sourceHasActualLineBreak && !sourceHasEscapedCrLf && !sourceHasEscapedLf)
            {
                return target
                    .Replace("\\r\\n", "\n")
                    .Replace("\\n", "\n")
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
            }

            return target;
        }

        private static string ApplySourcePadding(string source, string translated)
        {
            source = source ?? string.Empty;
            translated = translated ?? string.Empty;
            int prefixLength = 0;
            while (prefixLength < source.Length && char.IsWhiteSpace(source[prefixLength]))
            {
                prefixLength++;
            }

            int suffixLength = 0;
            while (suffixLength < source.Length - prefixLength && char.IsWhiteSpace(source[source.Length - 1 - suffixLength]))
            {
                suffixLength++;
            }

            string prefix = prefixLength > 0 ? source.Substring(0, prefixLength) : string.Empty;
            string suffix = suffixLength > 0 ? source.Substring(source.Length - suffixLength, suffixLength) : string.Empty;
            return prefix + translated.Trim() + suffix;
        }

        private static void ImportLanguageDocument(Language targetLanguage, LocalizationAgentLanguageDocument document, List<LocalizationText> sourceTexts, bool forceAll)
        {
            var targetTexts = new List<LocalizationText>();
            LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(targetLanguage, ref targetTexts);
            LocalizationMergeService.MergeTexts(sourceTexts, ref targetTexts);

            var targetMap = new Dictionary<string, LocalizationText>(targetTexts.Count, StringComparer.Ordinal);
            for (int i = 0; i < targetTexts.Count; i++)
            {
                targetMap[targetTexts[i].Key] = targetTexts[i];
            }

            for (int i = 0; i < document.Items.Count; i++)
            {
                LocalizationAgentTextItem item = document.Items[i];
                if (!targetMap.TryGetValue(item.Key, out LocalizationText targetText))
                {
                    continue;
                }

                if (!forceAll && !string.IsNullOrWhiteSpace(targetText.Value))
                {
                    continue;
                }

                string translatedText = item.Text ?? string.Empty;
                translatedText = ApplySourcePadding(sourceTexts[i].Value ?? string.Empty, translatedText);
                targetText.Value = translatedText;
            }

            LocalizationLanguageExcelRepository.SaveLanguage(targetLanguage, targetTexts);
        }
    }
}
