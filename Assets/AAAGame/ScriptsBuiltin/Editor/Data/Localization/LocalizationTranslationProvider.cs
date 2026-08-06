namespace UGF.EditorTools
{
    internal enum LocalizationTranslationProvider
    {
        Baidu = 0,
        CodexCli = 1,
        ClaudeCodeCli = 2,
        OpenCodeCli = 3
    }

    internal enum LocalizationTranslationRunState
    {
        Idle = 0,
        Preparing = 1,
        Running = 2,
        Validating = 3,
        Syncing = 4,
        Completed = 5,
        Failed = 6
    }

    internal sealed class LocalizationTranslationStatusSnapshot
    {
        public bool IsRunning;
        public LocalizationTranslationProvider Provider;
        public LocalizationTranslationRunState State;
        public string Message;
        public string Detail;
        public string ErrorMessage;
        public string LastStdout;
        public string LastStderr;
        public string WorkingDirectory;
        public int CompletedLanguages;
        public int TotalLanguages;
        public float Progress01;
    }
}
