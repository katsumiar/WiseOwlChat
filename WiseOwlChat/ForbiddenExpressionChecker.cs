using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WiseOwlChat
{
    public class ForbiddenExpressionChecker
    {
        private class ForbiddenExpression
        {
            public string? Title { get; set; }
            public string? Pattern { get; set; }
        }

        private readonly List<ForbiddenExpression>? _expressions = null;

        public ForbiddenExpressionChecker()
        {
            if (_expressions == null)
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "forbiddenExpressions.json");

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // プロパティ名の大文字・小文字を区別しない
                    };

                    if (json != null)
                    {
                        _expressions = JsonSerializer.Deserialize<List<ForbiddenExpression>>(json, options);
                    }
                }
            }
        }

        public string? Check(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_expressions == null) return null;

            foreach (var expression in _expressions)
            {
                if (expression.Pattern != null)
                {
                    if (Regex.IsMatch(text, expression.Pattern, RegexOptions.IgnoreCase))
                    {
                        return expression.Title; // タイトルを返す
                    }
                }
            }

            return null; // 禁止表現が見つからなかった場合
        }
    }
}
