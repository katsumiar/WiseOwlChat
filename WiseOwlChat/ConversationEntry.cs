using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using static WiseOwlChat.OpenAIChat;

namespace WiseOwlChat
{
    public class ConversationEntry : INotifyPropertyChanged
    {
        public static string SelectedLanguage => $"(Respond in {AppDataStore.Instance.SelectedLanguage})";
        public static string SupplementaryAnalysis => "";
        public static string PolicyHeader = "# policy" + Environment.NewLine;

        public const string ROLE_SYSTEM = "system";
        public const string ROLE_USER = "user";
        public const string ROLE_ASSISTANT = "assistant";
        public const string ROLE_FUNCTION = "function"; // Function Calling用Role

        public const string ROLE_QUESTION = "question"; // 自動での疑問点（ROLE_USER）※非表示（pipeline用）
        public const string ROLE_THINKING = "thinking"; // 考察（ROLE_USER）※非表示（pipeline用）
        public const string ROLE_MEMO = "memo"; // 記録用（送信しない）
        public const string ROLE_CONSIDERATION = "consideration"; // 考察結果(ROLE_ASSISTANT)
        public const string ROLE_ADVICE = "advice"; // 付記（ROLE_USER）
        public const string ROLE_WARNING = "warning"; // システム警告（送信しない）

        /// <summary>
        /// 会話表示対象か？
        /// </summary>
        /// <param name="role">役割(ROLE_xxx)</param>
        /// <returns>true==会話表示対象</returns>
        public static bool IsView(string? role)
        {
            HashSet<string> ValidRoles = new HashSet<string>
            {
                  ConversationEntry.ROLE_USER
                , ConversationEntry.ROLE_ASSISTANT
                , ConversationEntry.ROLE_MEMO
                , ConversationEntry.ROLE_CONSIDERATION
                , ConversationEntry.ROLE_ADVICE
                , ConversationEntry.ROLE_FUNCTION
                , ConversationEntry.ROLE_WARNING
            };

            return role != null && ValidRoles.Contains(role);
        }

        /// <summary>
        /// 会話表示対象のものからアドバイスとして表示する対象か？
        /// </summary>
        /// <param name="role">会話表示対象</param>
        /// <returns>true==アドバイスとして表示する対象</returns>
        public static bool IsAdvice(string? role)
        {
            HashSet<string> ValidRoles = new HashSet<string>
            {
                  ConversationEntry.ROLE_MEMO
                , ConversationEntry.ROLE_CONSIDERATION
                , ConversationEntry.ROLE_ADVICE
                , ConversationEntry.ROLE_FUNCTION
            };

            return role != null && ValidRoles.Contains(role);
        }

        /// <summary>
        /// APIへの送信対象か？
        /// </summary>
        /// <param name="role">役割(ROLE_xxx)</param>
        /// <returns>true==APIへの送信対象</returns>
        public static bool IsSend(string? role)
        {
            HashSet<string> InvalidRoles = new HashSet<string>
            {
                  ConversationEntry.ROLE_WARNING
                , ConversationEntry.ROLE_MEMO
            };

            return role != null && !InvalidRoles.Contains(role);
        }

        /// <summary>
        /// API送信時の役割（ROLE_xxx）変換
        /// </summary>
        /// <param name="role">役割(ROLE_xxx)</param>
        /// <returns>APIのrole</returns>
        public static string? TransRole(string? role)
        {
            switch (role)
            {
                case ConversationEntry.ROLE_CONSIDERATION:
                    return ConversationEntry.ROLE_ASSISTANT;

                case ConversationEntry.ROLE_QUESTION:
                case ConversationEntry.ROLE_THINKING:
                case ConversationEntry.ROLE_ADVICE:
                    return ConversationEntry.ROLE_USER;

                case ConversationEntry.ROLE_FUNCTION:
                    return ConversationEntry.ROLE_FUNCTION;
            }

            return role;
        }

        /// <summary>
        /// 会話パネルの色
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static object BrushesConvert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                switch (role)
                {
                    case ConversationEntry.ROLE_USER:
                        return Brushes.Gainsboro;
                    case ConversationEntry.ROLE_THINKING:
                        return Brushes.Pink;
                    case ConversationEntry.ROLE_ASSISTANT:
                        return Brushes.AntiqueWhite;
                    case ConversationEntry.ROLE_CONSIDERATION:
                        return Brushes.Aquamarine;
                    case ConversationEntry.ROLE_WARNING:
                        return Brushes.OrangeRed;
                    case ConversationEntry.ROLE_SYSTEM:
                    case ConversationEntry.ROLE_ADVICE:
                    case ConversationEntry.ROLE_FUNCTION:
                        return Brushes.MintCream;
                    case ConversationEntry.ROLE_MEMO:
                        return Brushes.LightSalmon;
                    default:
                        return Brushes.White;
                }
            }

            return Brushes.Transparent;
        }

        public string? role { get; set; }
        private string? _content = null;
        public string? content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }
        public string? name { get; set; }

        private string? alternativeContent = null;
        public string? GetAlternativeContent()
        {
            return alternativeContent;
        }

        public async Task SetAlternativeContentAsync(Func<string?, Task<String?>> getter)
        {
            alternativeContent = await getter(content);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
