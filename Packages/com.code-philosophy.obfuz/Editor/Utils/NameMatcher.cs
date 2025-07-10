using System.Text.RegularExpressions;

namespace Obfuz.Utils
{
    public class NameMatcher
    {
        private readonly string _str;
        private readonly Regex _regex;

        public string NameOrPattern => _str;

        public bool IsWildcardPattern => _regex != null;

        public NameMatcher(string nameOrPattern)
        {
            if (string.IsNullOrEmpty(nameOrPattern))
            {
                nameOrPattern = "*";
            }
            _str = nameOrPattern;
            _regex = nameOrPattern.Contains("*") || nameOrPattern.Contains("?") ? new Regex(WildcardToRegex(nameOrPattern)) : null;
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        public bool IsMatch(string name)
        {
            if (_regex != null)
            {
                return _regex.IsMatch(name);
            }
            else
            {
                return _str == name;
            }
        }
    }
}
