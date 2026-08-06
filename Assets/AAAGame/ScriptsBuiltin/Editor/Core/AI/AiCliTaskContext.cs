using System;
using System.Diagnostics;

namespace UGF.EditorTools
{
    internal sealed class AiCliTaskContext
    {
        public string TaskName;
        public AiCliProvider Provider;
        public string WorkingDirectory;
        public string OutputDirectory;
        public string PromptPath;
        public string StdoutPath;
        public string StderrPath;
        public string DisplayLogPath;
        public string DebugConsoleCloseSignalPath;
        public bool ShowDebugCommandWindow;
        public AiCliDebugCommandWindow.Handle DebugConsoleHandle;
        public Process Process;
        public object Payload;
        public bool Finalized;
        public bool HasTerminalSuccessEvent;
        public bool HasTerminalFailureEvent;
        public string TerminalSuccessMessage;
        public string TerminalFailureMessage;
        public string LastProgressDetail;
        public float Progress01;
        public double LastProgressTimestamp;
        public int LastCompletedUnits;
        public Action<AiCliTaskState, string, string, int?, int?, float?> ReportStatus;
    }
}
