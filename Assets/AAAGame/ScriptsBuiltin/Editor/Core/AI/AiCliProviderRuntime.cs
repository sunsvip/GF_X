using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace UGF.EditorTools
{
    internal static class AiCliProviderRuntime
    {
        internal readonly struct ProviderLaunchSpec
        {
            internal readonly string CommandName;
            internal readonly string Arguments;

            internal ProviderLaunchSpec(string commandName, string arguments)
            {
                CommandName = commandName;
                Arguments = arguments ?? string.Empty;
            }
        }

        internal static ProviderLaunchSpec BuildLaunchSpec(AiCliProvider provider, string workingDirectory)
        {
            switch (provider)
            {
                case AiCliProvider.CodexCli:
                    // Codex on Windows will route shell work through its sandbox runner when sandboxing is enabled.
                    // In this project the CLI is expected to act as a local agent with direct workspace access,
                    // and workspace-write caused CreateProcessAsUserW(740) failures during normal tool use.
                    return new ProviderLaunchSpec("codex", JoinArguments(
                        "-a", "never",
                        "exec",
                        "-s", "danger-full-access",
                        "--skip-git-repo-check",
                        "--ephemeral",
                        "--json",
                        "-C", workingDirectory,
                        "-"));

                case AiCliProvider.ClaudeCodeCli:
                    return new ProviderLaunchSpec("claude", JoinArguments(
                        "--print",
                        "--input-format", "text",
                        "--output-format", "stream-json",
                        "--include-partial-messages",
                        "--verbose",
                        "--permission-mode", "bypassPermissions",
                        "--no-session-persistence",
                        "--add-dir", workingDirectory));

                case AiCliProvider.OpenCodeCli:
                    return new ProviderLaunchSpec("opencode", JoinArguments(
                        "run",
                        "--format", "json",
                        "--thinking",
                        "--auto",
                        "--no-replay",
                        "--dir", workingDirectory));

                default:
                    return new ProviderLaunchSpec(string.Empty, string.Empty);
            }
        }

        internal static void ParseTerminalEvent(AiCliTaskContext context, string rawLine)
        {
            if (context == null || string.IsNullOrWhiteSpace(rawLine))
            {
                return;
            }

            string line = rawLine.Trim();
            if (line.Length < 2 || line[0] != '{')
            {
                return;
            }

            try
            {
                JObject obj = JObject.Parse(line);
                switch (context.Provider)
                {
                    case AiCliProvider.CodexCli:
                        ParseCodexTerminalEvent(context, obj);
                        break;
                    case AiCliProvider.ClaudeCodeCli:
                        ParseClaudeTerminalEvent(context, obj);
                        break;
                    case AiCliProvider.OpenCodeCli:
                        ParseOpenCodeTerminalEvent(context, obj);
                        break;
                }
            }
            catch
            {
            }
        }

        internal static string ExtractProgressDetail(AiCliProvider provider, string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                return string.Empty;
            }

            string line = rawLine.Trim();
            if (line.Length < 2 || line[0] != '{')
            {
                return NormalizeSingleLine(line, 240);
            }

            try
            {
                JObject obj = JObject.Parse(line);
                switch (provider)
                {
                    case AiCliProvider.CodexCli:
                        return ExtractCodexProgressDetail(obj);
                    case AiCliProvider.ClaudeCodeCli:
                        return ExtractClaudeProgressDetail(obj);
                    case AiCliProvider.OpenCodeCli:
                        return ExtractOpenCodeProgressDetail(obj);
                    default:
                        return string.Empty;
                }
            }
            catch
            {
                return NormalizeSingleLine(line, 240);
            }
        }

        private static void ParseCodexTerminalEvent(AiCliTaskContext context, JObject obj)
        {
            string type = obj.Value<string>("type");
            if (string.IsNullOrWhiteSpace(type))
            {
                return;
            }

            switch (type)
            {
                case "turn.completed":
                case "response.completed":
                case "session.completed":
                    context.HasTerminalSuccessEvent = true;
                    context.TerminalSuccessMessage = NormalizeSingleLine(
                        obj.Value<string>("message")
                        ?? obj.Value<string>("summary")
                        ?? "Codex 任务完成。",
                        240);
                    return;

                case "turn.failed":
                case "response.failed":
                case "session.failed":
                case "error":
                    context.HasTerminalFailureEvent = true;
                    context.TerminalFailureMessage = NormalizeSingleLine(
                        obj.Value<string>("message")
                        ?? obj.Value<string>("summary")
                        ?? obj.Value<string>("content")
                        ?? obj.SelectToken("error.message")?.ToString()
                        ?? "Codex 返回失败事件。",
                        320);
                    return;
            }
        }

        private static void ParseClaudeTerminalEvent(AiCliTaskContext context, JObject obj)
        {
            string type = obj.Value<string>("type");
            if (string.IsNullOrWhiteSpace(type))
            {
                return;
            }

            switch (type)
            {
                case "result":
                    bool isError = obj.Value<bool?>("is_error") ?? false;
                    string subtype = obj.Value<string>("subtype");
                    string resultMessage = NormalizeSingleLine(
                        obj.Value<string>("result")
                        ?? obj.Value<string>("message")
                        ?? obj.Value<string>("summary")
                        ?? "Claude Code 任务完成。",
                        320);
                    if (isError || (!string.IsNullOrWhiteSpace(subtype) && !string.Equals(subtype, "success", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.HasTerminalFailureEvent = true;
                        context.TerminalFailureMessage = resultMessage;
                        return;
                    }

                    context.HasTerminalSuccessEvent = true;
                    context.TerminalSuccessMessage = resultMessage;
                    return;

                case "error":
                    context.HasTerminalFailureEvent = true;
                    context.TerminalFailureMessage = NormalizeSingleLine(
                        obj.Value<string>("message")
                        ?? obj.Value<string>("summary")
                        ?? obj.ToString(Formatting.None),
                        320);
                    return;
            }
        }

        private static void ParseOpenCodeTerminalEvent(AiCliTaskContext context, JObject obj)
        {
            string type = obj.Value<string>("type");
            if (string.IsNullOrWhiteSpace(type))
            {
                return;
            }

            switch (type)
            {
                case "step_finish":
                    context.HasTerminalSuccessEvent = true;
                    context.TerminalSuccessMessage = "OpenCode 任务完成。";
                    return;

                case "error":
                    context.HasTerminalFailureEvent = true;
                    context.TerminalFailureMessage = NormalizeSingleLine(
                        obj.SelectToken("part.text")?.ToString()
                        ?? obj.Value<string>("message")
                        ?? obj.ToString(Formatting.None),
                        320);
                    return;
            }
        }

        private static string ExtractCodexProgressDetail(JObject obj)
        {
            string type = obj.Value<string>("type");
            if (string.Equals(type, "turn.started", StringComparison.Ordinal))
            {
                return "Codex 开始执行任务。";
            }

            JToken item = obj["item"];
            if (item == null)
            {
                return string.Empty;
            }

            string command = item.Value<string>("command");
            if (!string.IsNullOrWhiteSpace(command))
            {
                return "Codex 正在执行: " + NormalizeSingleLine(command, 180);
            }

            string title = item.SelectToken("arguments.title")?.ToString();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return "Codex 正在处理: " + NormalizeSingleLine(title, 180);
            }

            string itemType = item.Value<string>("type");
            return !string.IsNullOrWhiteSpace(itemType) ? "Codex 事件: " + itemType : string.Empty;
        }

        private static string ExtractClaudeProgressDetail(JObject obj)
        {
            string type = obj.Value<string>("type");
            if (string.Equals(type, "system", StringComparison.Ordinal))
            {
                string subtype = obj.Value<string>("subtype");
                return string.IsNullOrWhiteSpace(subtype) ? "Claude Code 初始化中。" : "Claude Code 系统事件: " + subtype;
            }

            if (!string.Equals(type, "stream_event", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            JToken ev = obj["event"];
            if (ev == null)
            {
                return string.Empty;
            }

            string eventType = ev.Value<string>("type");
            if (string.Equals(eventType, "content_block_start", StringComparison.Ordinal))
            {
                string blockType = ev.SelectToken("content_block.type")?.ToString();
                string blockName = ev.SelectToken("content_block.name")?.ToString();
                if (string.Equals(blockType, "tool_use", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(blockName))
                {
                    return "Claude Code 正在调用工具: " + blockName;
                }

                if (!string.IsNullOrWhiteSpace(blockType))
                {
                    return "Claude Code 事件: " + blockType;
                }
            }

            return string.Empty;
        }

        private static string ExtractOpenCodeProgressDetail(JObject obj)
        {
            string type = obj.Value<string>("type");
            switch (type)
            {
                case "step_start":
                    return "OpenCode 开始执行任务。";
                case "reasoning":
                    return "OpenCode 正在推理。";
                case "text":
                    return "OpenCode 正在生成结果。";
                case "tool_use":
                    return ExtractOpenCodeToolProgressDetail(obj);
                case "step_finish":
                    return "OpenCode 返回完成事件。";
                case "error":
                    return "OpenCode 返回错误事件。";
                default:
                    return string.Empty;
            }
        }

        private static string ExtractOpenCodeToolProgressDetail(JObject obj)
        {
            string tool = obj.SelectToken("part.tool")?.ToString();
            string status = obj.SelectToken("part.state.status")?.ToString();
            string title = obj.SelectToken("part.state.title")?.ToString()
                ?? obj.SelectToken("part.title")?.ToString();

            var builder = new StringBuilder(64);
            builder.Append("OpenCode 正在调用工具");
            if (!string.IsNullOrWhiteSpace(tool))
            {
                builder.Append(": ");
                builder.Append(tool);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                builder.Append(" [");
                builder.Append(status);
                builder.Append(']');
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.Append(" - ");
                builder.Append(NormalizeSingleLine(title, 180));
            }

            return builder.ToString();
        }

        internal static string NormalizeSingleLine(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r", " ").Replace("\n", " ").Trim();
            while (normalized.IndexOf("  ", StringComparison.Ordinal) >= 0)
            {
                normalized = normalized.Replace("  ", " ");
            }

            if (maxLength > 0 && normalized.Length > maxLength)
            {
                normalized = normalized.Substring(0, maxLength) + "...";
            }

            return normalized;
        }

        private static string JoinArguments(params string[] arguments)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(QuoteArgument(arguments[i]));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            if (value.IndexOfAny(new[] { ' ', '\t', '"', '\n', '\r' }) < 0)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length + 8);
            builder.Append('"');
            int backslashCount = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (ch == '"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    builder.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                builder.Append(ch);
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount * 2);
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
