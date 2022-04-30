using System;
using System.Text.RegularExpressions;

namespace Mittons.Fixtures.Extensions
{
    public static class StringExtensions
    {
        internal static string ReplaceEnvironmentVariables(this string template)
        {
            return Regex.Replace(template, @"\$\{([^\}]+)\}", new MatchEvaluator(GetEnvironmentVariableOrDefault));
        }

        private static string GetEnvironmentVariableOrDefault(Match m)
        {
            var value = Environment.GetEnvironmentVariable(m.Groups[1].Value);

            return value is null ? string.Empty : value;
        }
    }
}