namespace UGF.EditorTools
{
    internal interface IAiCliTaskDefinition
    {
        string TaskName { get; }
        AiCliProvider Provider { get; }
        string WorkingDirectoryName { get; }

        void PrepareInputs(AiCliTaskContext context);
        string BuildPrompt(AiCliTaskContext context);
        AiCliTaskProgressInfo BuildRunningProgress(AiCliTaskContext context);
        bool TryFinalize(AiCliTaskContext context, bool failOnValidationError, out string completionMessage, out string errorMessage);
    }
}
