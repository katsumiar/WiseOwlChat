using System.Collections.Generic;

namespace WiseOwlChat
{
    class History<T>
    {
        public delegate bool HistoryDelegate(out T? obj);

        private int historyIndex = -1;
        private List<T> history = new();
        private T? current = default(T);

        public void AddHistory(T obj)
        {
            history.Add(obj);
            historyIndex = -1;
            current = default(T);
        }

        public void SetCurrentHistory(T obj)
        {
            if (historyIndex == -1)
            {
                current = obj;
            }
        }

        public bool TryPreviousHistory(out T? obj)
        {
            bool result = false;
            if (historyIndex + 1 < history.Count)
            {
                historyIndex++;
                obj = history[historyIndex];
                result = true;
            }
            else
            {
                obj = default(T);
            }
            return result;
        }

        public bool TryNextHistory(out T? obj)
        {
            bool result;
            if (historyIndex > -1)
            {
                historyIndex--;
                obj = historyIndex == -1 ? current : history[historyIndex];
                result = true;
            }
            else
            {
                obj = default(T);
                result = false;
            }
            return result;
        }
    }
}
