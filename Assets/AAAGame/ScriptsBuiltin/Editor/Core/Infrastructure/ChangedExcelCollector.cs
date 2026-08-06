using System.Collections.Generic;

namespace UGF.EditorTools
{
    internal sealed class ChangedExcelCollector
    {
        private readonly List<string> _paths = new List<string>();
        private readonly object _syncRoot = new object();

        public bool HasPending()
        {
            lock (_syncRoot)
            {
                return _paths.Count > 0;
            }
        }

        public bool TryConsume(out IList<string> changedPaths)
        {
            lock (_syncRoot)
            {
                if (_paths.Count <= 0)
                {
                    changedPaths = null;
                    return false;
                }

                changedPaths = new List<string>(_paths);
                _paths.Clear();
                return true;
            }
        }

        public void AddUnique(string fullPath)
        {
            lock (_syncRoot)
            {
                if (!_paths.Contains(fullPath))
                {
                    _paths.Add(fullPath);
                }
            }
        }
    }
}
