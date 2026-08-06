namespace UGF.EditorTools
{
    internal enum AiCliTaskState
    {
        Idle = 0,
        Preparing = 1,
        Running = 2,
        Validating = 3,
        Applying = 4,
        Completed = 5,
        Failed = 6
    }

    internal sealed class AiCliTaskStatusSnapshot
    {
        public bool IsRunning;
        public AiCliProvider Provider;
        public AiCliTaskState State;
        public string Message;
        public string Detail;
        public string ErrorMessage;
        public string WorkingDirectory;
        public string LastStdout;
        public string LastStderr;
        public int CompletedUnits;
        public int TotalUnits;
        public float Progress01;
    }

    internal sealed class AiCliTaskProgressInfo
    {
        public string Message;
        public string Detail;
        public int CompletedUnits;
        public int TotalUnits;
        public float Progress01;
    }
}
