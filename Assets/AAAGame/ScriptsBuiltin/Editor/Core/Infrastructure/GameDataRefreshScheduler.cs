namespace UGF.EditorTools
{
    internal sealed class GameDataRefreshScheduler
    {
        private readonly double _debounceSeconds;
        private double _nextRefreshTime;

        public GameDataRefreshScheduler(double debounceSeconds)
        {
            _debounceSeconds = debounceSeconds;
        }

        public bool ShouldDefer(bool hasPendingChanges, double currentTime)
        {
            return hasPendingChanges && currentTime < _nextRefreshTime;
        }

        public void NotifyChange(double currentTime)
        {
            _nextRefreshTime = currentTime + _debounceSeconds;
        }
    }
}
