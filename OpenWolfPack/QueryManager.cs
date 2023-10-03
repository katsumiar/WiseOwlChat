using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenWolfPack
{
    public class QueryManager
    {
        private static readonly Lazy<QueryManager> lazyInstance = new(() => new QueryManager());

        private readonly Dictionary<string, string?> keyValueStore;

        private QueryManager()
        {
            keyValueStore = new Dictionary<string, string?>();
        }

        public static QueryManager Instance => lazyInstance.Value;

        public void Clear()
        {
            keyValueStore.Clear();
        }

        public void SetNameAndText(string name, string? text)
        {
            keyValueStore[name] = text;
        }

        public string? GetNameAndText(string name)
        {
            string? result = keyValueStore.TryGetValue(name, out var value) ? value : null;

            if (result != null)
            {
                result = ProcessInputText(result);
            }

            return result?.Replace("{Language}", AppDataStore.Instance.SelectedLanguage);
        }

        public string ProcessInputText(string inputText)
        {
            var pattern = @"\{fetch: (.+?)\}";
            var regex = new Regex(pattern);

            string result = regex.Replace(inputText, match =>
            {
                var name = match.Groups[1].Value;
                var fetchedText = GetNameAndText(name);
                if (fetchedText == null)
                {
                    MessageWindow.MessageDialog($"`{name}`\nwas not found.");
                }
                return fetchedText ?? "none.";
            });

            DateTime now = DateTime.Now;
            string dateStr = now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            string dayOfWeekStr = now.DayOfWeek.ToString();
            string timeStr = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

            result = result.Replace("{Now}", $"{dateStr}({dayOfWeekStr}) {timeStr}");
            result = result.Replace("{Language}", AppDataStore.Instance.SelectedLanguage);

            return result;
        }
    }
}
