using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AiCliTaskExecutor
    {
        private sealed class TaskExecution
        {
            public IAiCliTaskDefinition Definition;
            public AiCliTaskContext Context;
            public Action<string, int, int> OnProgressUpdate;
            public Action OnComplete;
        }

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly object s_StateLock = new object();
        // AppendDebugDisplay 被 stdout/stderr 两个读取线程并发调用, 各自以独立句柄追加同一 DisplayLogPath, 需串行化避免行交错/丢失.
        private static readonly object s_DisplayLogLock = new object();
        private static TaskExecution s_CurrentExecution;
        private static readonly AiCliTaskStatusSnapshot s_Status = new AiCliTaskStatusSnapshot
        {
            IsRunning = false,
            Provider = AiCliProvider.CodexCli,
            State = AiCliTaskState.Idle,
            Message = "待命",
            Detail = string.Empty,
            ErrorMessage = string.Empty,
            WorkingDirectory = string.Empty,
            LastStdout = string.Empty,
            LastStderr = string.Empty,
            CompletedUnits = 0,
            TotalUnits = 0,
            Progress01 = 0f
        };

        static AiCliTaskExecutor()
        {
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        internal static bool IsRunning => s_CurrentExecution != null;

        internal static AiCliTaskStatusSnapshot GetStatusSnapshot()
        {
            lock (s_StateLock)
            {
                return new AiCliTaskStatusSnapshot
                {
                    IsRunning = s_Status.IsRunning,
                    Provider = s_Status.Provider,
                    State = s_Status.State,
                    Message = s_Status.Message,
                    Detail = s_Status.Detail,
                    ErrorMessage = s_Status.ErrorMessage,
                    WorkingDirectory = s_Status.WorkingDirectory,
                    LastStdout = s_Status.LastStdout,
                    LastStderr = s_Status.LastStderr,
                    CompletedUnits = s_Status.CompletedUnits,
                    TotalUnits = s_Status.TotalUnits,
                    Progress01 = s_Status.Progress01
                };
            }
        }

        internal static bool Start(IAiCliTaskDefinition definition, bool showDebugCommandWindow = false, Action<string, int, int> onProgressUpdate = null, Action onComplete = null)
        {
            if (s_CurrentExecution != null)
            {
                UpdateStatus(AiCliTaskState.Failed, "已有 AI CLI 任务在运行中。", null, s_Status.CompletedUnits, s_Status.TotalUnits, s_Status.Progress01);
                return false;
            }

            if (definition == null)
            {
                UpdateStatus(AiCliTaskState.Failed, "AI CLI 任务定义无效。", null, 0, 0, 0f);
                return false;
            }

            AiCliTaskContext context = CreateContext(definition);
            context.ShowDebugCommandWindow = showDebugCommandWindow;
            var execution = new TaskExecution
            {
                Definition = definition,
                Context = context,
                OnProgressUpdate = onProgressUpdate,
                OnComplete = onComplete
            };
            context.ReportStatus = (state, message, detail, completed, total, progress01) =>
            {
                if (ReferenceEquals(s_CurrentExecution, execution))
                {
                    UpdateStatus(state, message, detail, completed, total, progress01);
                }
            };

            try
            {
                PrepareWorkingDirectory(context.WorkingDirectory, context.OutputDirectory);
                InitializeLogFiles(context);
                definition.PrepareInputs(context);
                File.WriteAllText(context.PromptPath, definition.BuildPrompt(context), Utf8NoBom);
            }
            catch (Exception exception)
            {
                UpdateStatus(AiCliTaskState.Failed, $"{definition.TaskName} 准备失败", exception.Message, 0, 0, 0f);
                onComplete?.Invoke();
                return false;
            }

            s_CurrentExecution = execution;
            s_Status.LastStdout = string.Empty;
            s_Status.LastStderr = string.Empty;
            s_Status.ErrorMessage = string.Empty;
            s_Status.CompletedUnits = 0;
            s_Status.TotalUnits = 0;
            s_Status.Progress01 = 0f;
            UpdateStatus(AiCliTaskState.Preparing, $"准备启动 AI 任务: {definition.TaskName}", $"输出目录: {context.OutputDirectory}", 0, 0, 0.03f);

            if (!TryStartProcess(context, out string errorMessage))
            {
                s_CurrentExecution = null;
                UpdateStatus(AiCliTaskState.Failed, errorMessage, null, 0, 0, 0f);
                onComplete?.Invoke();
                return false;
            }

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            context.Progress01 = Mathf.Max(context.Progress01, 0.1f);
            context.LastProgressTimestamp = EditorApplication.timeSinceStartup;
            UpdateProgress(execution);
            return true;
        }

        internal static bool Cancel(string detail = null)
        {
            if (s_CurrentExecution == null)
            {
                return false;
            }

            UpdateStatus(AiCliTaskState.Failed, "AI CLI 任务已取消", detail ?? "用户取消当前 AI 任务。", s_Status.CompletedUnits, s_Status.TotalUnits, s_Status.Progress01);
            CleanupExecution(killProcess: true, invokeOnComplete: true);
            return true;
        }

        private static void OnEditorUpdate()
        {
            TaskExecution execution = s_CurrentExecution;
            if (execution == null)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            UpdateProgress(execution);

            AiCliTaskContext context = execution.Context;
            if (context.Process == null)
            {
                FailExecution("AI CLI 进程无效。");
                return;
            }

            if (TryFinalize(execution, false))
            {
                return;
            }

            if (s_CurrentExecution == null)
            {
                return;
            }

            bool hasTerminalFailureEvent;
            string terminalFailureMessage;
            lock (s_StateLock)
            {
                hasTerminalFailureEvent = context.HasTerminalFailureEvent;
                terminalFailureMessage = context.TerminalFailureMessage;
            }

            if (hasTerminalFailureEvent)
            {
                FailExecution(string.IsNullOrWhiteSpace(terminalFailureMessage) ? "AI CLI 返回失败状态事件。" : terminalFailureMessage);
                return;
            }

            if (!context.Process.HasExited)
            {
                return;
            }

            if (context.Finalized)
            {
                return;
            }

            context.Finalized = true;
            try
            {
                if (TryFinalize(execution, true))
                {
                    return;
                }

                if (s_CurrentExecution != null)
                {
                    FailExecution(ResolveFailureMessage(context));
                }
            }
            catch (Exception exception)
            {
                if (s_CurrentExecution != null)
                {
                    FailExecution($"{execution.Definition.TaskName} 结果处理失败: {exception.Message}");
                }
            }
        }

        private static bool TryFinalize(TaskExecution execution, bool failOnValidationError)
        {
            if (execution == null)
            {
                return false;
            }

            if (execution.Definition.TryFinalize(execution.Context, failOnValidationError, out string completionMessage, out string errorMessage))
            {
                CompleteExecution(completionMessage);
                return true;
            }

            if (failOnValidationError && !string.IsNullOrWhiteSpace(errorMessage))
            {
                FailExecution(errorMessage);
            }

            return false;
        }

        private static void UpdateProgress(TaskExecution execution)
        {
            if (execution == null)
            {
                return;
            }

            AiCliTaskProgressInfo progress = execution.Definition.BuildRunningProgress(execution.Context);
            if (progress == null)
            {
                return;
            }

            execution.Context.Progress01 = Mathf.Clamp01(Mathf.Max(execution.Context.Progress01, progress.Progress01));
            UpdateStatus(
                AiCliTaskState.Running,
                string.IsNullOrWhiteSpace(progress.Message) ? $"AI 任务运行中: {execution.Definition.TaskName}" : progress.Message,
                progress.Detail,
                progress.CompletedUnits,
                progress.TotalUnits,
                execution.Context.Progress01);
            execution.Context.LastProgressTimestamp = EditorApplication.timeSinceStartup;

            if (progress.CompletedUnits != execution.Context.LastCompletedUnits)
            {
                execution.Context.LastCompletedUnits = progress.CompletedUnits;
                execution.OnProgressUpdate?.Invoke(
                    string.IsNullOrWhiteSpace(progress.Message) ? execution.Definition.TaskName : progress.Message,
                    progress.TotalUnits,
                    progress.CompletedUnits);
            }
        }

        private static AiCliTaskContext CreateContext(IAiCliTaskDefinition definition)
        {
            string workingDirectory = ResolveWorkingDirectoryPath(definition.WorkingDirectoryName);
            return new AiCliTaskContext
            {
                TaskName = definition.TaskName,
                Provider = definition.Provider,
                WorkingDirectory = workingDirectory,
                OutputDirectory = Path.Combine(workingDirectory, "output"),
                PromptPath = Path.Combine(workingDirectory, "prompt.md"),
                StdoutPath = Path.Combine(workingDirectory, "stdout.log"),
                StderrPath = Path.Combine(workingDirectory, "stderr.log"),
                DisplayLogPath = Path.Combine(workingDirectory, "display.log"),
                DebugConsoleCloseSignalPath = Path.Combine(workingDirectory, "debug-console.close"),
                Progress01 = 0f,
                LastProgressTimestamp = EditorApplication.timeSinceStartup
            };
        }

        private static bool TryStartProcess(AiCliTaskContext context, out string errorMessage)
        {
            errorMessage = null;
            string invocationPrompt = BuildInvocationPrompt(context);
            AiCliProviderRuntime.ProviderLaunchSpec providerLaunchSpec = AiCliProviderRuntime.BuildLaunchSpec(context.Provider, context.WorkingDirectory);
            if (string.IsNullOrWhiteSpace(providerLaunchSpec.CommandName))
            {
                errorMessage = $"不支持的 CLI Provider: {context.Provider}";
                return false;
            }

            string commandArguments = providerLaunchSpec.Arguments;

            if (!AiCliCommandResolver.TryResolve(providerLaunchSpec.CommandName, commandArguments, true, out AiCliCommandResolver.LaunchSpec launchSpec, out errorMessage))
            {
                return false;
            }

            Process process = null;
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = launchSpec.FileName,
                    Arguments = launchSpec.Arguments,
                    WorkingDirectory = context.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = launchSpec.UseStandardInput,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = false
                };
                process.OutputDataReceived += OnProcessOutput;
                process.ErrorDataReceived += OnProcessError;
                if (!process.Start())
                {
                    errorMessage = $"启动 AI CLI 失败: {launchSpec.FileName}";
                    return false;
                }

                context.Process = process;
                AppendStartupDiagnostics(context, launchSpec);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (launchSpec.UseStandardInput)
                {
                    process.StandardInput.Write(invocationPrompt);
                    process.StandardInput.Close();
                }

                if (context.ShowDebugCommandWindow)
                {
                    context.DebugConsoleHandle = AiCliDebugCommandWindow.TryLaunch(context.Provider.ToString(), context.DisplayLogPath, context.DebugConsoleCloseSignalPath, context.TaskName);
                }

                return true;
            }
            catch (Exception exception)
            {
                TryDisposeStartupProcess(process);
                errorMessage = $"启动 AI CLI 异常: {exception.Message}";
                return false;
            }
        }

        private static void AppendStartupDiagnostics(AiCliTaskContext context, AiCliCommandResolver.LaunchSpec launchSpec)
        {
            if (context == null)
            {
                return;
            }

            AppendDebugLine(context, "LaunchFile: " + launchSpec.FileName);
            AppendDebugLine(context, "LaunchArguments: " + launchSpec.Arguments);
            AppendDebugLine(context, "UseStandardInput: " + launchSpec.UseStandardInput);
            AppendDebugLine(context, "PromptPath: " + context.PromptPath);
        }

        private static void AppendDebugLine(AiCliTaskContext context, string line)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.DisplayLogPath))
            {
                return;
            }

            AppendLog(context.DisplayLogPath, "[Launcher] " + line);
        }

        private static string BuildInvocationPrompt(AiCliTaskContext context)
        {
            return "Read and execute the complete task prompt file at "
                + QuoteForPrompt(context.PromptPath)
                + ". Use working directory "
                + QuoteForPrompt(context.WorkingDirectory)
                + ". Do not ask for confirmation. The prompt file defines the required output files and validation rules. Write the requested result files directly as UTF-8 files.";
        }

        private static string QuoteForPrompt(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static void OnProcessOutput(object sender, DataReceivedEventArgs eventArgs)
        {
            string line = eventArgs.Data;
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            TaskExecution execution = s_CurrentExecution;
            AiCliTaskContext context = execution != null ? execution.Context : null;
            AppendLog(context != null ? context.StdoutPath : null, line);
            AppendDebugDisplay(context, line, false);
            if (context == null)
            {
                lock (s_StateLock)
                {
                    s_Status.LastStdout = line;
                }
                return;
            }

            lock (s_StateLock)
            {
                s_Status.LastStdout = line;
                // 此处运行在 BeginOutputReadLine 的线程池线程上，绝不能调用 EditorApplication 等
                // Unity API：非主线程调用会抛 UnityException，终止异步读取线程，导致 stdout 管道无人读取，
                // 子进程写满 64KB 管道后阻塞，任务永久卡死在 step_start。LastProgressTimestamp 已由主线程
                // 的 UpdateProgress 每帧更新，无需在此设置。
                string progressDetail = AiCliProviderRuntime.ExtractProgressDetail(context.Provider, line);
                if (!string.IsNullOrWhiteSpace(progressDetail))
                {
                    context.LastProgressDetail = progressDetail;
                }

                AiCliProviderRuntime.ParseTerminalEvent(context, line);
            }
        }

        private static void OnProcessError(object sender, DataReceivedEventArgs eventArgs)
        {
            string line = eventArgs.Data;
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            TaskExecution execution = s_CurrentExecution;
            AiCliTaskContext context = execution != null ? execution.Context : null;
            AppendLog(context != null ? context.StderrPath : null, line);
            AppendDebugDisplay(context, line, true);
            if (context == null)
            {
                lock (s_StateLock)
                {
                    s_Status.LastStderr = line;
                }
                return;
            }

            lock (s_StateLock)
            {
                s_Status.LastStderr = line;
                // 同 OnProcessOutput：线程池线程上不能调用 EditorApplication 等 Unity API，否则会杀死异步读取线程。
                if (context.HasTerminalFailureEvent)
                {
                    return;
                }

                if (LooksLikeFailureText(line))
                {
                    context.TerminalFailureMessage = AiCliProviderRuntime.NormalizeSingleLine(line, 320);
                }
            }
        }

        private static void PrepareWorkingDirectory(string workingDirectory, string outputDirectory)
        {
            string projectRoot = GetNormalizedProjectRoot();
            string normalizedWorkingDirectory = Path.GetFullPath(workingDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string projectRootWithSeparator = projectRoot + Path.DirectorySeparatorChar;
            if (string.Equals(normalizedWorkingDirectory, projectRoot, StringComparison.OrdinalIgnoreCase)
                || !normalizedWorkingDirectory.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"AI 工作目录越界: {normalizedWorkingDirectory}");
            }

            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }

            Directory.CreateDirectory(workingDirectory);
            Directory.CreateDirectory(outputDirectory);
        }

        private static void CompleteExecution(string message)
        {
            UpdateStatus(AiCliTaskState.Completed, message, null, s_Status.TotalUnits, s_Status.TotalUnits, 1f);
            CleanupExecution(killProcess: true, invokeOnComplete: true);
        }

        private static void FailExecution(string errorMessage)
        {
            UpdateStatus(AiCliTaskState.Failed, "AI CLI 任务失败", errorMessage, s_Status.CompletedUnits, s_Status.TotalUnits, s_Status.Progress01);
            CleanupExecution(killProcess: true, invokeOnComplete: true);
        }

        private static void CleanupExecution(bool killProcess, bool invokeOnComplete)
        {
            TaskExecution execution = s_CurrentExecution;
            s_CurrentExecution = null;
            EditorApplication.update -= OnEditorUpdate;

            if (execution != null)
            {
                try
                {
                    execution.Context?.DebugConsoleHandle?.Close();
                    if (execution.Context != null && execution.Context.Process != null)
                    {
                        if (killProcess)
                        {
                            TryKillProcessTree(execution.Context.Process);
                        }

                        execution.Context.Process.OutputDataReceived -= OnProcessOutput;
                        execution.Context.Process.ErrorDataReceived -= OnProcessError;
                        execution.Context.Process.Dispose();
                    }
                }
                catch
                {
                }

                if (invokeOnComplete)
                {
                    execution.OnComplete?.Invoke();
                }
            }
        }

        private static string ResolveWorkingDirectoryPath(string workingDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(workingDirectoryName))
            {
                throw new InvalidOperationException("AI 工作目录名称无效，不能为空。");
            }

            string trimmedWorkingDirectoryName = workingDirectoryName.Trim();
            if (Path.IsPathRooted(trimmedWorkingDirectoryName))
            {
                throw new InvalidOperationException($"AI 工作目录必须使用工程内相对路径: {trimmedWorkingDirectoryName}");
            }

            string projectRoot = GetNormalizedProjectRoot();
            string resolvedWorkingDirectory = Path.GetFullPath(Path.Combine(projectRoot, trimmedWorkingDirectoryName))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string projectRootWithSeparator = projectRoot + Path.DirectorySeparatorChar;
            if (string.Equals(resolvedWorkingDirectory, projectRoot, StringComparison.OrdinalIgnoreCase)
                || !resolvedWorkingDirectory.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"AI 工作目录越界: {resolvedWorkingDirectory}");
            }

            return resolvedWorkingDirectory;
        }

        private static string GetNormalizedProjectRoot()
        {
            return Path.GetFullPath(ConstEditor.ProjectRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void TryDisposeStartupProcess(Process process)
        {
            try
            {
                if (process != null)
                {
                    TryKillProcessTree(process);
                    process.OutputDataReceived -= OnProcessOutput;
                    process.ErrorDataReceived -= OnProcessError;
                    process.Dispose();
                }
            }
            catch
            {
            }
        }

        private static void OnEditorQuitting()
        {
            CleanupExecution(killProcess: true, invokeOnComplete: false);
        }

        private static void OnBeforeAssemblyReload()
        {
            CleanupExecution(killProcess: true, invokeOnComplete: false);
        }

        private static void UpdateStatus(AiCliTaskState state, string message, string detail, int? completedUnits = null, int? totalUnits = null, float? progress01 = null)
        {
            lock (s_StateLock)
            {
                s_Status.State = state;
                s_Status.IsRunning = state == AiCliTaskState.Preparing
                    || state == AiCliTaskState.Running
                    || state == AiCliTaskState.Validating
                    || state == AiCliTaskState.Applying;

                AiCliTaskContext context = s_CurrentExecution != null ? s_CurrentExecution.Context : null;
                if (context != null)
                {
                    s_Status.Provider = context.Provider;
                    s_Status.WorkingDirectory = context.WorkingDirectory;
                }

                s_Status.Message = message ?? string.Empty;
                s_Status.Detail = detail ?? string.Empty;
                s_Status.ErrorMessage = state == AiCliTaskState.Failed
                    ? ((!string.IsNullOrWhiteSpace(detail) ? detail : message) ?? string.Empty)
                    : string.Empty;

                if (completedUnits.HasValue)
                {
                    s_Status.CompletedUnits = completedUnits.Value;
                }

                if (totalUnits.HasValue)
                {
                    s_Status.TotalUnits = totalUnits.Value;
                }

                if (progress01.HasValue)
                {
                    s_Status.Progress01 = Mathf.Clamp01(Mathf.Max(state == AiCliTaskState.Preparing ? 0f : s_Status.Progress01, progress01.Value));
                }
                else if (state == AiCliTaskState.Idle)
                {
                    s_Status.Progress01 = 0f;
                }
            }
        }

        private static string ResolveFailureMessage(AiCliTaskContext context)
        {
            if (context != null && context.HasTerminalFailureEvent && !string.IsNullOrWhiteSpace(context.TerminalFailureMessage))
            {
                return context.TerminalFailureMessage;
            }

            string stderr = SafeReadAllText(context != null ? context.StderrPath : null);
            if (TryResolveFailureText(stderr, out string message))
            {
                return message;
            }

            string stdout = SafeReadAllText(context != null ? context.StdoutPath : null);
            if (TryResolveFailureText(stdout, out message))
            {
                return message;
            }

            if (context != null && context.Process != null)
            {
                try
                {
                    if (context.Process.HasExited && context.Process.ExitCode != 0)
                    {
                        return $"AI CLI 进程退出码非 0: {context.Process.ExitCode}";
                    }
                }
                catch
                {
                }
            }

            return "AI CLI 未生成有效结果文件。";
        }

        private static bool TryResolveFailureText(string text, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (!LooksLikeFailureText(line))
                {
                    continue;
                }

                message = AiCliProviderRuntime.NormalizeSingleLine(line, 320);
                return true;
            }

            return false;
        }

        private static bool LooksLikeFailureText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("failure", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SafeReadAllText(string path)
        {
            try
            {
                return string.IsNullOrWhiteSpace(path) || !File.Exists(path)
                    ? string.Empty
                    : ReadAllTextShared(path);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AppendLog(string path, string line)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, Utf8NoBom))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }

        private static void AppendDebugDisplay(AiCliTaskContext context, string line, bool isError)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.DisplayLogPath) || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            try
            {
                lock (s_DisplayLogLock)
                {
                    AiCliDebugDisplayFormatter.AppendLines(context.Provider, line, isError, context.DisplayLogPath, Utf8NoBom);
                }
            }
            catch
            {
            }
        }

        private static string ReadAllTextShared(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void InitializeLogFiles(AiCliTaskContext context)
        {
            File.WriteAllText(context.StdoutPath, string.Empty, Utf8NoBom);
            File.WriteAllText(context.StderrPath, string.Empty, Utf8NoBom);
            File.WriteAllText(
                context.DisplayLogPath,
                $"[AI CLI Debug]{Environment.NewLine}Provider: {context.Provider}{Environment.NewLine}Task: {context.TaskName}{Environment.NewLine}WorkingDirectory: {context.WorkingDirectory}{Environment.NewLine}{Environment.NewLine}",
                Utf8NoBom);

            if (File.Exists(context.DebugConsoleCloseSignalPath))
            {
                File.Delete(context.DebugConsoleCloseSignalPath);
            }
        }

        private static void TryKillProcessTree(Process process)
        {
            try
            {
                if (process == null || process.HasExited)
                {
                    return;
                }

                int processId = process.Id;
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        RunProcessKiller("taskkill", $"/PID {processId} /T /F", 4000);
                        break;

                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxEditor:
                        RunProcessKiller("/bin/bash", $"-lc \"pkill -TERM -P {processId} 2>/dev/null; kill -TERM {processId} 2>/dev/null; sleep 1; pkill -KILL -P {processId} 2>/dev/null; kill -KILL {processId} 2>/dev/null; true\"", 4000);
                        break;
                }

                if (!process.HasExited)
                {
                    process.Kill();
                }

                process.WaitForExit(2000);
            }
            catch
            {
            }
        }

        private static void RunProcessKiller(string fileName, string arguments, int waitMilliseconds)
        {
            using (var killer = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                if (killer == null)
                {
                    return;
                }

                if (!killer.WaitForExit(waitMilliseconds))
                {
                    try
                    {
                        killer.Kill();
                    }
                    catch
                    {
                    }
                }
            }

            Thread.Sleep(100);
        }
    }

    internal static class AiCliDebugCommandWindow
    {
        internal sealed class Handle
        {
            private readonly string _closeSignalPath;
            private readonly Process _process;
            private readonly string _macWindowTitle;
            private bool _closed;

            internal Handle(string closeSignalPath, Process process, string macWindowTitle)
            {
                _closeSignalPath = closeSignalPath;
                _process = process;
                _macWindowTitle = macWindowTitle;
            }

            internal void Close()
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
                try
                {
                    SignalClose(_closeSignalPath);
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        CloseMacTerminalWindow(_macWindowTitle);
                    }

                    if (_process == null || _process.HasExited)
                    {
                        return;
                    }

                    if (!_process.CloseMainWindow() || !_process.WaitForExit(800))
                    {
                        _process.Kill();
                    }
                }
                catch
                {
                }
            }
        }

        internal static Handle TryLaunch(string providerId, string logPath, string closeSignalPath, string titleSuffix)
        {
            if (string.IsNullOrWhiteSpace(logPath) || string.IsNullOrWhiteSpace(closeSignalPath))
            {
                return null;
            }

            try
            {
                TryDeleteCloseSignal(closeSignalPath);
                string directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                }

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return LaunchWindowsConsole(providerId, logPath, closeSignalPath, titleSuffix);
                    case RuntimePlatform.OSXEditor:
                        return LaunchMacTerminal(providerId, logPath, closeSignalPath, titleSuffix);
                    case RuntimePlatform.LinuxEditor:
                        return LaunchLinuxTerminal(providerId, logPath, closeSignalPath, titleSuffix);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static Handle LaunchWindowsConsole(string providerId, string logPath, string closeSignalPath, string titleSuffix)
        {
            string safeLogPath = logPath.Replace("'", "''");
            string safeCloseSignalPath = closeSignalPath.Replace("'", "''");
            string title = BuildWindowTitle(providerId, titleSuffix).Replace("'", "''");
            string command =
                $"$Host.UI.RawUI.WindowTitle = '{title}'; " +
                $"Write-Host 'Tailing: {safeLogPath}'; " +
                $"if (!(Test-Path -LiteralPath '{safeLogPath}')) {{ New-Item -ItemType File -Path '{safeLogPath}' -Force | Out-Null }}; " +
                $"$tailJob = Start-Job -ScriptBlock {{ param($path) Get-Content -LiteralPath $path -Encoding UTF8 -Tail 40 -Wait }} -ArgumentList '{safeLogPath}'; " +
                $"try {{ while (!(Test-Path -LiteralPath '{safeCloseSignalPath}')) {{ Receive-Job -Job $tailJob; Start-Sleep -Milliseconds 200; }} }} " +
                $"finally {{ Stop-Job -Job $tailJob -ErrorAction SilentlyContinue | Out-Null; Remove-Job -Job $tailJob -Force -ErrorAction SilentlyContinue | Out-Null; }}";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoLogo -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName
            };
            return new Handle(closeSignalPath, Process.Start(startInfo), null);
        }

        private static Handle LaunchMacTerminal(string providerId, string logPath, string closeSignalPath, string titleSuffix)
        {
            string safeLogPath = logPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string safeCloseSignalPath = closeSignalPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string title = BuildWindowTitle(providerId, titleSuffix).Replace("\"", "\\\"");
            string script =
                $"tell application \"Terminal\" to do script \"printf '\\\\e]1;{title}\\\\a'; touch \\\"{safeLogPath}\\\"; printf 'Tailing: {safeLogPath}\\\\n'; tail -n 40 -f \\\"{safeLogPath}\\\" & TAIL_PID=$!; while [ ! -f \\\"{safeCloseSignalPath}\\\" ]; do sleep 1; done; kill $TAIL_PID >/dev/null 2>&1; wait $TAIL_PID 2>/dev/null; exit\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return new Handle(closeSignalPath, Process.Start(startInfo), title);
        }

        private static Handle LaunchLinuxTerminal(string providerId, string logPath, string closeSignalPath, string titleSuffix)
        {
            string safeLogPath = logPath.Replace("\"", "\\\"");
            string safeCloseSignalPath = closeSignalPath.Replace("\"", "\\\"");
            string title = BuildWindowTitle(providerId, titleSuffix).Replace("\"", "\\\"");
            string command = $"touch \"{safeLogPath}\"; printf 'Tailing: {safeLogPath}\\n'; tail -n 40 -f \"{safeLogPath}\" & TAIL_PID=$!; while [ ! -f \"{safeCloseSignalPath}\" ]; do sleep 1; done; kill $TAIL_PID >/dev/null 2>&1; wait $TAIL_PID 2>/dev/null";
            string[] terminalCandidates = { "x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "xterm" };

            for (int i = 0; i < terminalCandidates.Length; i++)
            {
                string terminal = terminalCandidates[i];
                try
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = BuildLinuxTerminalArguments(terminal, title, command),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    return new Handle(closeSignalPath, process, null);
                }
                catch
                {
                }
            }

            return null;
        }

        private static string BuildLinuxTerminalArguments(string terminal, string title, string command)
        {
            switch (terminal)
            {
                case "gnome-terminal":
                    return $"--title=\"{title}\" -- bash -lc \"{command}\"";
                case "konsole":
                    return $"--new-tab -p tabtitle=\"{title}\" -e bash -lc \"{command}\"";
                case "xfce4-terminal":
                    return $"--title=\"{title}\" -e \"bash -lc '{command}'\"";
                case "xterm":
                    return $"-T \"{title}\" -e bash -lc \"{command}\"";
                default:
                    return $"-T \"{title}\" -e bash -lc \"{command}\"";
            }
        }

        private static string BuildWindowTitle(string providerId, string titleSuffix)
        {
            return string.IsNullOrWhiteSpace(titleSuffix)
                ? $"AI CLI Debug - {providerId}"
                : $"AI CLI Debug - {providerId} - {titleSuffix}";
        }

        private static void SignalClose(string closeSignalPath)
        {
            if (string.IsNullOrWhiteSpace(closeSignalPath))
            {
                return;
            }

            File.WriteAllText(closeSignalPath, DateTime.UtcNow.ToString("o"));
        }

        private static void TryDeleteCloseSignal(string closeSignalPath)
        {
            if (string.IsNullOrWhiteSpace(closeSignalPath) || !File.Exists(closeSignalPath))
            {
                return;
            }

            File.Delete(closeSignalPath);
        }

        private static void CloseMacTerminalWindow(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            string safeTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string script = $"tell application \"Terminal\" to close (every window whose name contains \"{safeTitle}\")";
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                process?.WaitForExit(1000);
            }
        }
    }

    internal static class AiCliDebugDisplayFormatter
    {
        internal static void AppendLines(AiCliProvider provider, string rawLine, bool isError, string displayLogPath, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(rawLine) || string.IsNullOrWhiteSpace(displayLogPath))
            {
                return;
            }

            var lines = BuildLines(provider, rawLine, isError);
            if (lines.Count < 1)
            {
                return;
            }

            using (var stream = new FileStream(displayLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream, encoding))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }
        }

        private static List<string> BuildLines(AiCliProvider provider, string rawLine, bool isError)
        {
            var lines = new List<string>(2);
            if (isError)
            {
                lines.Add("ERROR: " + AiCliProviderRuntime.NormalizeSingleLine(rawLine, 1200));
                return lines;
            }

            string trimmed = rawLine.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{')
            {
                lines.Add(AiCliProviderRuntime.NormalizeSingleLine(rawLine, 1200));
                return lines;
            }

            try
            {
                JObject obj = JObject.Parse(trimmed);
                switch (provider)
                {
                    case AiCliProvider.CodexCli:
                        AppendCodexLines(obj, lines);
                        break;
                    case AiCliProvider.ClaudeCodeCli:
                        AppendClaudeLines(obj, lines);
                        break;
                    case AiCliProvider.OpenCodeCli:
                        AppendOpenCodeLines(obj, lines);
                        break;
                }
            }
            catch
            {
                lines.Add(AiCliProviderRuntime.NormalizeSingleLine(rawLine, 1200));
            }

            return lines;
        }

        private static void AppendCodexLines(JObject obj, List<string> lines)
        {
            string type = obj.Value<string>("type");
            if (string.IsNullOrWhiteSpace(type))
            {
                return;
            }

            switch (type)
            {
                case "turn.started":
                    lines.Add("==> Phase: running task");
                    return;
                case "turn.completed":
                case "response.completed":
                case "session.completed":
                    lines.Add("==> Result: task completed");
                    return;
                case "turn.failed":
                case "response.failed":
                case "session.failed":
                case "error":
                    lines.Add("ERROR: " + ResolveBestMessage(obj, "Codex task failed."));
                    return;
            }

            JToken item = obj["item"];
            if (item == null)
            {
                return;
            }

            string itemType = item.Value<string>("type");
            string itemLabel = ResolveCodexItemLabel(item);
            if (string.Equals(itemType, "reasoning", StringComparison.Ordinal))
            {
                string text = ResolveCodexContentText(item["text"]) ?? ResolveCodexContentText(item["content"]);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add("--- Thinking ---");
                    lines.Add(AiCliProviderRuntime.NormalizeSingleLine(text, 1200));
                }
                else if (!string.IsNullOrWhiteSpace(itemLabel))
                {
                    lines.Add("[Item] " + itemLabel);
                }
                return;
            }

            if (string.Equals(itemType, "agent_message", StringComparison.Ordinal) && type.EndsWith(".completed", StringComparison.OrdinalIgnoreCase))
            {
                string resultText = ResolveCodexContentText(item["text"])
                    ?? ResolveCodexContentText(item["content"])
                    ?? ResolveCodexContentText(item.SelectToken("result.content"));
                lines.Add("--- Response ---");
                lines.Add(string.IsNullOrWhiteSpace(resultText) ? "assistant response generated" : AiCliProviderRuntime.NormalizeSingleLine(resultText, 1200));
                return;
            }

            if (type.EndsWith(".started", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add("[Item] " + itemLabel);
                return;
            }

            if (type.EndsWith(".completed", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add("[Done] " + itemLabel);
            }
        }

        private static void AppendClaudeLines(JObject obj, List<string> lines)
        {
            string type = obj.Value<string>("type");
            switch (type)
            {
                case "system":
                    if (string.Equals(obj.Value<string>("subtype"), "init", StringComparison.Ordinal))
                    {
                        lines.Add("==> Claude session started");
                    }
                    return;
                case "result":
                    lines.Add("==> Done.");
                    return;
                case "error":
                    lines.Add("ERROR: " + ResolveBestMessage(obj, "Claude task failed."));
                    return;
                case "user":
                    JToken toolResult = obj.SelectToken("message.content[0].content");
                    if (toolResult != null)
                    {
                        string toolResultText = toolResult.Type == JTokenType.String ? toolResult.ToString() : toolResult.ToString(Newtonsoft.Json.Formatting.None);
                        if (!string.IsNullOrWhiteSpace(toolResultText))
                        {
                            lines.Add("  -> " + AiCliProviderRuntime.NormalizeSingleLine(toolResultText, 240));
                        }
                    }
                    return;
                case "stream_event":
                    AppendClaudeStreamEventLines(obj["event"] as JObject, lines);
                    return;
            }
        }

        private static void AppendClaudeStreamEventLines(JObject ev, List<string> lines)
        {
            if (ev == null)
            {
                return;
            }

            string eventType = ev.Value<string>("type");
            switch (eventType)
            {
                case "content_block_start":
                    string blockType = ev.SelectToken("content_block.type")?.ToString();
                    if (string.Equals(blockType, "thinking", StringComparison.Ordinal))
                    {
                        lines.Add("--- Thinking ---");
                    }
                    else if (string.Equals(blockType, "tool_use", StringComparison.Ordinal))
                    {
                        lines.Add("[Tool] " + (ev.SelectToken("content_block.name")?.ToString() ?? "tool_use"));
                    }
                    else if (string.Equals(blockType, "text", StringComparison.Ordinal))
                    {
                        lines.Add("--- Response ---");
                    }
                    return;
                case "content_block_delta":
                    string deltaType = ev.SelectToken("delta.type")?.ToString();
                    if (string.Equals(deltaType, "thinking_delta", StringComparison.Ordinal))
                    {
                        string thinking = ev.SelectToken("delta.thinking")?.ToString();
                        if (!string.IsNullOrWhiteSpace(thinking))
                        {
                            lines.Add(AiCliProviderRuntime.NormalizeSingleLine(thinking, 1200));
                        }
                    }
                    else if (string.Equals(deltaType, "text_delta", StringComparison.Ordinal))
                    {
                        string text = ev.SelectToken("delta.text")?.ToString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            lines.Add(AiCliProviderRuntime.NormalizeSingleLine(text, 1200));
                        }
                    }
                    else if (string.Equals(deltaType, "input_json_delta", StringComparison.Ordinal))
                    {
                        string json = ev.SelectToken("delta.partial_json")?.ToString();
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            lines.Add("  -> " + AiCliProviderRuntime.NormalizeSingleLine(json, 1200));
                        }
                    }
                    return;
            }
        }

        private static void AppendOpenCodeLines(JObject obj, List<string> lines)
        {
            string type = obj.Value<string>("type");
            string partText = obj.SelectToken("part.text")?.ToString();
            switch (type)
            {
                case "step_start":
                    lines.Add("==> Phase: running task");
                    return;
                case "step_finish":
                    lines.Add("==> Result: " + ResolveOpenCodeSummary(obj["part"] as JObject));
                    return;
                case "reasoning":
                    if (!string.IsNullOrWhiteSpace(partText))
                    {
                        lines.Add("--- Thinking ---");
                        lines.Add(AiCliProviderRuntime.NormalizeSingleLine(partText, 1200));
                    }
                    return;
                case "text":
                    if (!string.IsNullOrWhiteSpace(partText))
                    {
                        lines.Add("--- Response ---");
                        lines.Add(AiCliProviderRuntime.NormalizeSingleLine(partText, 1200));
                    }
                    return;
                case "tool_use":
                    AppendOpenCodeToolUseLines(obj, lines);
                    return;
                case "error":
                    lines.Add("ERROR: " + ResolveBestMessage(obj, "OpenCode task failed."));
                    return;
            }

            string partType = obj.SelectToken("part.type")?.ToString();
            if ((string.Equals(partType, "tool", StringComparison.Ordinal) || string.Equals(partType, "tool-call", StringComparison.Ordinal))
                && !string.IsNullOrWhiteSpace(partText))
            {
                lines.Add("[Tool] " + AiCliProviderRuntime.NormalizeSingleLine(partText, 1200));
            }
        }

        private static void AppendOpenCodeToolUseLines(JObject obj, List<string> lines)
        {
            string tool = obj.SelectToken("part.tool")?.ToString();
            string status = obj.SelectToken("part.state.status")?.ToString();
            string title = obj.SelectToken("part.state.title")?.ToString()
                ?? obj.SelectToken("part.title")?.ToString();
            string output = obj.SelectToken("part.state.output")?.ToString();
            string error = obj.SelectToken("part.state.error")?.ToString();

            var builder = new StringBuilder(96);
            builder.Append("[Tool]");
            if (!string.IsNullOrWhiteSpace(tool))
            {
                builder.Append(' ');
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
                builder.Append(AiCliProviderRuntime.NormalizeSingleLine(title, 240));
            }

            lines.Add(builder.ToString());
            if (!string.IsNullOrWhiteSpace(error))
            {
                lines.Add("ERROR: " + AiCliProviderRuntime.NormalizeSingleLine(error, 1200));
                return;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                lines.Add("  -> " + AiCliProviderRuntime.NormalizeSingleLine(output, 1200));
            }
        }

        private static string ResolveCodexItemLabel(JToken item)
        {
            if (item == null)
            {
                return "event";
            }

            string command = item.Value<string>("command");
            if (!string.IsNullOrWhiteSpace(command))
            {
                return AiCliProviderRuntime.NormalizeSingleLine(command, 180);
            }

            string title = item.SelectToken("arguments.title")?.ToString();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return AiCliProviderRuntime.NormalizeSingleLine(title, 180);
            }

            string tool = item.Value<string>("tool");
            if (!string.IsNullOrWhiteSpace(tool))
            {
                string server = item.Value<string>("server");
                return string.IsNullOrWhiteSpace(server)
                    ? tool
                    : server + "/" + tool;
            }

            string type = item.Value<string>("type");
            return string.IsNullOrWhiteSpace(type) ? "event" : type;
        }

        private static string ResolveCodexContentText(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.String)
            {
                return token.ToString();
            }

            if (token.Type == JTokenType.Array)
            {
                JArray array = token as JArray;
                for (int i = 0; i < array.Count; i++)
                {
                    string nested = ResolveCodexContentText(array[i]?["text"]) ?? ResolveCodexContentText(array[i]?["content"]);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            if (token.Type == JTokenType.Object)
            {
                return ResolveCodexContentText(token["text"]) ?? ResolveCodexContentText(token["content"]);
            }

            return token.ToString();
        }

        private static string ResolveOpenCodeSummary(JObject part)
        {
            if (part == null)
            {
                return "step completed";
            }

            int outputTokens = part.SelectToken("tokens.output")?.Value<int>() ?? 0;
            int reasoningTokens = part.SelectToken("tokens.reasoning")?.Value<int>() ?? 0;
            if (outputTokens > 0 && reasoningTokens > 0)
            {
                return $"step completed (output={outputTokens}, reasoning={reasoningTokens})";
            }

            if (outputTokens > 0)
            {
                return $"step completed (output={outputTokens})";
            }

            return "step completed";
        }

        private static string ResolveBestMessage(JObject obj, string fallback)
        {
            return AiCliProviderRuntime.NormalizeSingleLine(
                obj.Value<string>("message")
                ?? obj.Value<string>("summary")
                ?? obj.Value<string>("result")
                ?? obj.SelectToken("error.message")?.ToString()
                ?? obj.SelectToken("part.text")?.ToString()
                ?? fallback,
                1200);
        }
    }
}
