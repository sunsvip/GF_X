using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UGF.EditorTools
{
    internal sealed class ImageFontAiGenerateTaskDefinition : IAiCliTaskDefinition
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        internal const string WorkingDirectory = "Library/ImageFontAiGenerate";
        internal const string PromptTemplatePath = "Assets/AAAGame/ScriptsBuiltin/Editor/Tools/Batch/ImageFontAtlasGeneratePrompt.md";
        internal const string CharsFileName = "chars.txt";
        internal const string FailureFileName = "failure.txt";

        private readonly AiCliProvider _provider;
        private readonly IReadOnlyList<int> _unicodes;
        private readonly string _styleRequirement;
        private readonly string _outputFileName;
        private string _expectedOutputPath;

        internal ImageFontAiGenerateTaskDefinition(
            AiCliProvider provider,
            IReadOnlyList<int> unicodes,
            string styleRequirement,
            string outputFileName)
        {
            _provider = provider;
            _unicodes = unicodes;
            _styleRequirement = styleRequirement;
            _outputFileName = outputFileName;
        }

        public string TaskName => "艺术字 AI 图集生成";
        public AiCliProvider Provider => _provider;
        public string WorkingDirectoryName => WorkingDirectory;

        internal string OutputFileName => _outputFileName;

        public void PrepareInputs(AiCliTaskContext context)
        {
            string chars = BuildCharsString(_unicodes);
            _expectedOutputPath = Path.Combine(context.OutputDirectory, _outputFileName);
            File.WriteAllText(Path.Combine(context.WorkingDirectory, CharsFileName), chars, Utf8NoBom);
        }

        public string BuildPrompt(AiCliTaskContext context)
        {
            string templatePath = Path.GetFullPath(Path.Combine(ConstEditor.ProjectRootPath, PromptTemplatePath));
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("艺术字 AI Prompt 模板不存在。", templatePath);
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);
            return template
                .Replace("[CHARS]", BuildCharsString(_unicodes))
                .Replace("[STYLE_REQUIREMENT]", _styleRequirement ?? string.Empty)
                .Replace("[OUTPUT_DIR]", context.OutputDirectory)
                .Replace("[OUTPUT_FILE]", _outputFileName);
        }

        public AiCliTaskProgressInfo BuildRunningProgress(AiCliTaskContext context)
        {
            string outputPath = GetOutputPath(context);
            bool hasOutput = File.Exists(outputPath);
            return new AiCliTaskProgressInfo
            {
                Message = hasOutput ? "AI 已生成艺术字图集，等待校验。" : "AI 正在生成艺术字图集。",
                Detail = hasOutput ? outputPath : $"等待输出: {_outputFileName}",
                CompletedUnits = hasOutput ? 1 : 0,
                TotalUnits = 1,
                Progress01 = hasOutput ? 0.9f : 0.35f
            };
        }

        public bool TryFinalize(AiCliTaskContext context, bool failOnValidationError, out string completionMessage, out string errorMessage)
        {
            completionMessage = null;
            errorMessage = null;

            string failurePath = Path.Combine(context.OutputDirectory, FailureFileName);
            if (File.Exists(failurePath))
            {
                errorMessage = ReadFailure(failurePath);
                return false;
            }

            string outputPath = GetOutputPath(context);
            if (!File.Exists(outputPath))
            {
                if (failOnValidationError)
                {
                    errorMessage = $"AI 未生成艺术字图集: {outputPath}";
                }

                return false;
            }

            var fileInfo = new FileInfo(outputPath);
            if (fileInfo.Length <= 0)
            {
                if (failOnValidationError)
                {
                    errorMessage = $"AI 生成的艺术字图集为空: {outputPath}";
                }

                return false;
            }

            if (!failOnValidationError && DateTime.UtcNow - fileInfo.LastWriteTimeUtc < TimeSpan.FromSeconds(1d))
            {
                return false;
            }

            completionMessage = "艺术字 AI 图集生成完成。";
            return true;
        }

        internal string GetExpectedOutputPath()
        {
            return !string.IsNullOrWhiteSpace(_expectedOutputPath)
                ? _expectedOutputPath
                : Path.Combine(ConstEditor.ProjectRootPath, WorkingDirectory, "output", _outputFileName);
        }

        private string GetOutputPath(AiCliTaskContext context)
        {
            return Path.Combine(context.OutputDirectory, _outputFileName);
        }

        private static string BuildCharsString(IReadOnlyList<int> unicodes)
        {
            if (unicodes == null || unicodes.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(unicodes.Count);
            for (int i = 0; i < unicodes.Count; i++)
            {
                builder.Append(char.ConvertFromUtf32(unicodes[i]));
            }

            return builder.ToString();
        }

        private static string ReadFailure(string failurePath)
        {
            try
            {
                string text = File.ReadAllText(failurePath, Encoding.UTF8).Trim();
                return string.IsNullOrWhiteSpace(text) ? "AI 生成失败，failure.txt 未说明原因。" : text;
            }
            catch (Exception exception)
            {
                return $"AI 生成失败，且 failure.txt 读取失败: {exception.Message}";
            }
        }
    }
}
