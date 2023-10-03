using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OpenWolfPack.Control;
using System.Linq;
using System.Data;
using static OpenWolfPack.OpenAIChat;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ControlzEx.Standard;
using System.Text.RegularExpressions;
using System.Net.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Threading;
using System.Windows.Interop;
using System.Diagnostics.Metrics;
using Markdig;
using System.Security.Policy;
using Microsoft.VisualBasic;
using static OpenWolfPack.DirectionsFileManager;
using System.Xml.Linq;
using System.Windows.Input;

namespace OpenWolfPack
{
    public partial class ChatViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MarkdownViewerViewModel> MarkdownViewers { get; set; }
        public ObservableCollection<string> ModelTypeItems { get; set; } = new();
        private OpenAIChat openAIChat;
        private ForbiddenExpressionChecker forbiddenExpressionChecker;

        public bool IsStop { get; set; } = false;
        public ICommand? StopCommand { get; set; }
        private Visibility _stopVisibility = Visibility.Collapsed;
        public Visibility StopVisibility
        {
            get=> _stopVisibility;
            set
            {
                if (_stopVisibility != value) 
                {
                    _stopVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<PluginInfo> PluginInfos 
        { 
            get => openAIChat.FunctionCallingRegistry.PluginInfos;
        }

        public MODEL_TYPE ModelType
        {
            get { return openAIChat.ModelType; }
            set
            {
                openAIChat.ModelType = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Pipelines { get; set; } = new();
        public int _selectedPipelineIndex = 0;
        public int SelectedPipelineIndex 
        {
            get
            {
                return _selectedPipelineIndex;
            }
            set
            {
                if (_selectedPipelineIndex != value)
                {
                    _selectedPipelineIndex = value;
                    OnPropertyChanged();
                    PipelineMode = true;
                }
            }
        }

        public string? UIAnalyzer_Contents => DirectionsFileManager.Instance.GetContent(Directions.user_intent_classification_guide);
        public string? Advice_Contents => DirectionsFileManager.Instance.GetContent(Directions.Advice);

        private bool uiaMode = false;// User Intent Analyzer モード
        public bool UIAMode
        {
            get { return uiaMode; }
            set
            {
                if (uiaMode != value)
                {
                    uiaMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool adviceMode = false;
        public bool AdviceMode
        {
            get { return adviceMode; }
            set
            {
                if (adviceMode != value)
                {
                    adviceMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool pipelineMode = false;
        public bool PipelineMode
        {
            get { return pipelineMode; }
            set
            {
                if (pipelineMode != value)
                {
                    if (value)
                    {
                        UIAMode = false;
                        AdviceMode = false;
                        ReActMode = false;
                        TrainingMode = false;
                    }
                    pipelineMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool functionMode = false;
        public bool FunctionMode
        {
            get { return functionMode; }
            set
            {
                if (functionMode != value)
                {
                    bool mustNotAccept = value && !IsAnyPluginEnabled();

                    if (!mustNotAccept)
                    {
                        functionMode = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        private bool trainingMode = false;
        public bool TrainingMode
        {
            get { return trainingMode; }
            set
            {
                if (trainingMode != value)
                {
                    trainingMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool reActMode = false;
        public bool ReActMode
        {
            get { return reActMode; }
            set
            {
                if (reActMode != value)
                {
                    reActMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public Action<string> popupMessageAction { get; set; } = (msg) => {};

        public ChatViewModel()
        {
            openAIChat = new OpenAIChat(App.API_Key, () =>
            {
                string? instruction = DirectionsFileManager.Instance.GetContent(Directions.persona_instructions_guide);
                if (instruction != null)
                {
                    instruction = QueryManager.Instance.ProcessInputText(instruction);
                }
                return instruction;
            });

            MarkdownViewers = new ObservableCollection<MarkdownViewerViewModel>();

            foreach (MODEL_TYPE model in Enum.GetValues(typeof(MODEL_TYPE)))
            {
                ModelTypeItems.Add(model.ToString());
            }

            forbiddenExpressionChecker = new ForbiddenExpressionChecker();

            openAIChat.RegistAPIFunction(new UploadAPI());

            DirectionsFileManager.Instance.SetPipelines(Pipelines);

            foreach (var pluginInfo in PluginInfos)
            {
                pluginInfo.PropertyChanged += PluginInfo_PropertyChanged;
            }
        }

        private void PluginInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Enabled")
            {
                FunctionMode = IsAnyPluginEnabled();
                if (FunctionMode)
                {
                    PipelineMode = false;
                }
            }
        }

        private bool IsAnyPluginEnabled()
        {
            bool isEnabled = false;
            foreach (var pluginInfo in PluginInfos)
            {
                if (pluginInfo.Enabled)
                {
                    isEnabled = true;
                }
            }

            return isEnabled;
        }

        public void ClearMessage(ObservableCollection<ConversationInfo> ConversationInfos)
        {
            MarkdownViewers.Clear();

            openAIChat.ClearMessage(ConversationInfos);
        }

        public void SetupMessage(ConversationInfo conversation)
        {
            MarkdownViewers.Clear();

            openAIChat.SetupMessage(conversation);

            foreach (var item in conversation.Conversation)
            {
                if (item.content != null && item.role != null)
                {
                    if (ConversationEntry.IsView(item.role))
                    {
                        appendViewMessage(item.role, item.content);
                    }
                }
            }
        }

        public void PartnerReflection()
        {
            openAIChat.PartnerReflection();
        }

        public async void Request(Action update, object? message)
        {
            if (message is MessageEventArgs _message && _message.Message != null)
            {
                IsStop = false;
                StopVisibility = Visibility.Visible;

                LogWindow.Instance.ClearLog();

                _message.Lock?.Invoke(); // メッセージ送信をロック

                await preQuery(update,
                    (res) =>
                    {
                        _message.Unlock?.Invoke(); // メッセージ送信をアンロック
                        StopVisibility = Visibility.Collapsed;
                        PipelineMode = false;
                    }, _message.Message, _message.Direction);
            }
        }

        private async void appendMessage(List<Task> delayedTaskList, Action update, Action<string?>? callback, MarkdownViewerViewModel? viewMessage = null)
        {
            bool reQuery;
            string? result = null;
            do
            {
                if (viewMessage == null)
                {
                    viewMessage = createViewMessage(ConversationEntry.ROLE_ASSISTANT, "");
                    viewMessage.MarkdownText = "please wait...";
                }

                var logMessage = openAIChat.AppendMessage(ConversationEntry.ROLE_ASSISTANT, "");

                (result, reQuery) = await openAIChat.SendMessageMainStreamAsync(
                    update,
                    callback, 
                    (msg) => 
                    { 
                        viewMessage.MarkdownText = msg; 
                    },
                    (role, msg, name) =>
                    {
                        PrepnedFunctionResultToOpenAIChat(role, msg, name);
                    },
                    FunctionMode);

                if (logMessage != null)
                {
                    if (!reQuery)
                    {
                        if (result != null)
                        {
                            logMessage.content = result;
                            delayedTranslation(delayedTaskList, logMessage);
                        }
                        else
                        {
                            openAIChat.Remove(logMessage);
                        }
                    }
                    else
                    {
                        openAIChat.Remove(logMessage);
                    }
                }
            }
            while (reQuery && !IsStop);
            if (IsStop)
            {
                viewMessage.MarkdownText += "(Canceled.)";
                callback?.Invoke(result);
            }
        }

        private void delayedTranslation(List<Task> delayedTaskList, ConversationEntry logMessage)
        {
            delayedTaskList.Add(
                Task.Run(() =>
                    logMessage.SetAlternativeContentAsync(
                        (content) =>
                        {
                            string instructions = "Please translate the content below into English.  " + Environment.NewLine
                                        + "---  " + Environment.NewLine + content;
                            return openAIChat.SystemRequest(MODEL_TYPE.GPT_35_TURBO_16K, false, instructions, (role) => role == ConversationEntry.ROLE_SYSTEM);
                        })
                ));
        }

        private async Task preQuery(Action update, Action<string?>? callback, string message, Directions? direction)
        {
            var error = forbiddenExpressionChecker.Check(message);
            if (error != null)
            {
                // NGワード

                string errorMessage = $"**!warning!** **{error}** *Restricted disclosure of information.*";

                AppendMessageToOpenAIChat(ConversationEntry.ROLE_WARNING, errorMessage); // 保存用で送られない
                AppendMessageToOpenAIChat(ConversationEntry.ROLE_SYSTEM, $"The user tried to disclose {error}, but it was restricted by the OpenWolfPack system. The disclosure of {error} is prohibited. Please guide the user to avoid the prohibited action (speak in Japanese).");

                List<Task> delayedTaskList = new();
                appendMessage(delayedTaskList, update, callback);
            }
            else
            {
                if (ReActMode && direction == null)
                {
                    message = message + "  \nThought :  \nAction :  \nObservation :";
                    ReActMode = false;
                }

                await query(update, callback, message, direction);
            }
        }

        private async Task query(Action update, Action<string?>? callback, string message, Directions? direction)
        {
            QueryManager.Instance.Clear();

            List<Task> delayedTaskList = new();

            QueryManager.Instance.SetNameAndText("TOPIC", message);
            
            if (direction != null)
            {
                string? prompt = DirectionsFileManager.Instance.GetContent(direction);
                if (prompt != null)
                {
                    if (direction == Directions.URL_Explanation_Prompt)
                    {
                        EnablePlugin("FetchUrlAPI");
                        FunctionMode = true;
                    }

                    message = QueryManager.Instance.ProcessInputText(prompt);
                    AppendMessageToOpenAIChat(ConversationEntry.ROLE_USER, message);
                }
            }
            else if (UIAMode || AdviceMode || PipelineMode)
            {
                if (message.StartsWith("\r") || message.StartsWith("\n") || message.StartsWith("\r\n"))
                {
                    AppendMessageToOpenAIChat(ConversationEntry.ROLE_USER, "TOPIC:" + message);
                }
                else
                {
                    AppendMessageToOpenAIChat(ConversationEntry.ROLE_USER, "TOPIC:" + Environment.NewLine + message);
                }
            }
            else
            {
                AppendMessageToOpenAIChat(ConversationEntry.ROLE_USER, message);
            }

            if (FunctionMode)
            {
                if (ModelType == MODEL_TYPE.GPT_35_TURBO)
                {
                    ModelType = MODEL_TYPE.GPT_35_TURBO_16K;
                    popupMessageAction("Changed MODEL_TYPE to GPT_35_TURBO_16K.");
                }
            }

            var viewMessage = createViewMessage(ConversationEntry.ROLE_ASSISTANT, "");
            viewMessage.MarkdownText = "please wait...";

            List<(Func<string, Task>?, PipelineInfo?)> querys = new();

            if (PipelineMode)
            {
                MarkdownViewers.Remove(viewMessage);

                await callPipeline(Pipelines[SelectedPipelineIndex], update, callback, delayedTaskList, querys);

                PipelineMode = false;
            }
            else
            {
                if (UIAMode && !IsStop)
                {
                    // ユーザーの意図を考える

                    querys.Add(new(null, DirectionsFileManager.Instance.CreateContentPipelineInfo(Directions.user_intent_classification_guide)));

                    await ProcessQuerysAsync(querys, true, ConversationEntry.ROLE_ADVICE);
                }

                if (AdviceMode && !IsStop)
                {
                    // アドバイスを付加する

                    await callPipeline(Directions.Advice, update, callback, delayedTaskList, querys);

                }

                if (UIAMode || AdviceMode)
                {
                    AppendMessageToOpenAIChat(ConversationEntry.ROLE_SYSTEM, "Please answer the topic." + ConversationEntry.SelectedLanguage);
                }

                if (!IsStop)
                {
                    appendMessage(delayedTaskList, update, callback, viewMessage);
                }
                else
                {
                    viewMessage.MarkdownText += "(Canceled.)";
                    callback?.Invoke(null);
                }

                AdviceMode = false;
                UIAMode = false;
            }

            await Task.WhenAll(delayedTaskList);

            QueryManager.Instance.Clear();
        }

        public void EnablePlugin(string name)
        {
            foreach (var pluginInfo in PluginInfos)
            {
                if (pluginInfo.Name == name)
                {
                    pluginInfo.Enabled = true;
                    break;
                }
            }
        }

        private async Task callPipeline(Directions path, Action update, Action<string?>? callback, List<Task> delayedTaskList, List<(Func<string, Task>?, PipelineInfo?)> querys)
        {
            var pipeline = DirectionsFileManager.Instance.FetchPipeline(path);

            if (pipeline == null)
            {
                return;
            }
            await _callPipeline(update, callback, delayedTaskList, querys, pipeline);
        }

        private async Task callPipeline(string path, Action update, Action<string?>? callback, List<Task> delayedTaskList, List<(Func<string, Task>?, PipelineInfo?)> querys)
        {
            var pipeline = DirectionsFileManager.Instance.FetchPipeline(path);

            if (pipeline == null)
            {
                return;
            }
            await _callPipeline(update, callback, delayedTaskList, querys, pipeline);
        }

        private async Task _callPipeline(Action update, Action<string?>? callback, List<Task> delayedTaskList, List<(Func<string, Task>?, PipelineInfo?)> querys, IEnumerable<DirectionsFileManager.PipelineInfo> pipeline)
        {
            string[]? pipelineRequests = null;
            bool isAdvice = false;
            foreach (var direction in pipeline)
            {
                switch (direction.Attribute)
                {
                    case DirectionsFileManager.AttributeType.Advice:
                        querys.Add(new(null, direction));
                        isAdvice = true;
                        break;

                    case DirectionsFileManager.AttributeType.Strategy:
                        if (isAdvice)
                        {
                            await ProcessQuerysAsync(querys, false);
                            isAdvice = false;
                        }

                        string? dirc = direction.Direction;
                        if (dirc != null)
                        {
                            string? instruction = ExtractBetweenTags(dirc, "$Instruction:");
                            string? constraints = ExtractBetweenTags(dirc, "$Constraints:");
                            QueryManager.Instance.SetNameAndText("$Constraints:", constraints);
                            string? format = ExtractBetweenTags(dirc, "$Each Request Format:");
                            string? elaboration = ExtractBetweenTags(dirc, "$Elaboration:");
                            QueryManager.Instance.SetNameAndText("$Elaboration:", elaboration);

                            if (instruction != null)
                            {
                                instruction = QueryManager.Instance.ProcessInputText(instruction);
                            }
                            string? result = await openAIChat.SystemRequest(MODEL_TYPE.GPT_35_TURBO_16K, false, instruction, null);

                            if (direction.DirectionName != null)
                            {
                                QueryManager.Instance.SetNameAndText(direction.DirectionName, result ?? "none");
                            }
                            AppendMessageToOpenAIChat(ConversationEntry.ROLE_MEMO, result);

                            pipelineRequests = extractListItems(result, format);
                            if (pipelineRequests != null)
                            {
                                AppendMessageToOpenAIChat(ConversationEntry.ROLE_SYSTEM, "Please answer the topic." + ConversationEntry.SelectedLanguage);
                                await queryPipeline(delayedTaskList, update, callback, pipelineRequests);
                            }
                            else
                            {
                                AppendMessageToOpenAIChat(ConversationEntry.ROLE_SYSTEM, "What information do you need in order to respond?" + ConversationEntry.SelectedLanguage);
                                appendMessage(delayedTaskList, update, callback);
                            }
                        }
                        break;
                }
                if (IsStop)
                {
                    break;
                }
            }
            if (isAdvice && !IsStop)
            {
                await ProcessQuerysAsync(querys, true);
                isAdvice = false;
            }
        }

        private static string[]? extractListItems(string? text, string? instruction)
        {
            if (text == null)
            {
                return null;
            }

            List<string>? extractedList;

            extractedList = extractChapters(text);
            extractedList ??= extractBulletPoints(text);

            if (extractedList == null)
            {
                return null;
            }

            buildInstruction(extractedList, instruction);

            return extractedList.ToArray();
        }

        static List<string>? buildInstruction(List<string>? extractedList, string? instruction)
        {
            if (extractedList == null || instruction == null)
            {
                return null;
            }

            for (int i = 0; i < extractedList.Count; i++)
            {
                extractedList[i] = instruction.Replace("{individual instructions}", extractedList[i]);
            }
            return extractedList;
        }

        static string? ExtractBetweenTags(string text, string startTag, string? endTag = "$end")
        {
            Regex regex;
            if (endTag != null)
            {
                regex = new Regex(Regex.Escape(startTag) + "(.*?)" + Regex.Escape(endTag), RegexOptions.Singleline);
            }
            else
            {
                regex = new Regex(Regex.Escape(startTag) + "(.*)", RegexOptions.Singleline);
            }

            Match match = regex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static List<string>? extractChapters(string? text)
        {
            if (text == null)
            {
                return null;
            }

            List<string> result = extractChapterDetails(text);

            if (result.Count == 0)
            {
                return null;
            }

            return result;
        }

        private static List<string> extractChapterDetails(string inputText)
        {
            List<string> chapters = new List<string>();
            Regex regex = new Regex(@"Chapter\s+(\d+):\s+(.*?)(?=Chapter\s+\d+:|end:)", RegexOptions.Singleline);

            foreach (Match match in regex.Matches(inputText))
            {
                chapters.Add(match.Value.Trim());
            }

            return chapters;
        }

        private static List<string>? extractBulletPoints(string? text)
        {
            if (text == null)
            {
                return null;
            }

            List<string> extractedList = new();

            Regex regex = new Regex(@"(\-|\•|\d+\.)\s*(.*?)(?=\n|$)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                extractedList.Add(
                    ConversationEntry.PolicyHeader + match.Groups[2].Value
                    );
            }

            if (extractedList.Count == 0)
            {
                return null;
            }
            return extractedList;
        }

        private async Task queryPipeline(List<Task> delayedTaskList, Action update, Action<string?>? callback, string[]? pipelineRequests)
        {
            if (pipelineRequests != null && pipelineRequests.Length > 0)
            {
                await queryPipeline(delayedTaskList, update, callback, pipelineRequests, ConversationEntry.ROLE_QUESTION);
            }
        }

        private async Task queryPipeline(List<Task> delayedTaskList, Action update, Action<string?>? callback, string[] pipelineRequests, string role)
        {
            string? previous = "none";
            string? basicInstructions = QueryManager.Instance.GetNameAndText("$Constraints:");

            for (int index = 0; index < pipelineRequests.Length; index++)
            {
                string request = pipelineRequests[index];
                request = QueryManager.Instance.ProcessInputText(request);
                request = request.Replace("{Previous}", previous);

                List<ConversationEntry> tempRequest = openAIChat.MakeRequest(
                    false,
                    request,
                    null, true, basicInstructions);

                (MarkdownViewerViewModel viewThinkingMessage, ConversationEntry? logMessage) = createRequest(index, role);

                if (logMessage == null)
                {
                    continue;
                }

                await openAIChat.SystemRequestStreamForPipeline(update,
                    (content) =>
                    {
                        if (content != null)
                        {
                            logMessage.content = content;
                            delayedTranslation(delayedTaskList, logMessage);
                        }
                    }, viewThinkingMessage, tempRequest, ModelType);

                string? elaboration = QueryManager.Instance.GetNameAndText("$Elaboration:");
                if (elaboration != null)
                {
                    // 推敲
                    List<ConversationEntry> elaborationRequest = openAIChat.MakeRequest(
                        false,
                        elaboration.Replace("{response}", logMessage.content),
                        null, true);

                    await openAIChat.SystemRequestStreamForPipeline(update,
                        (content) =>
                        {
                            if (content != null)
                            {
                                logMessage.content = content;
                                delayedTranslation(delayedTaskList, logMessage);
                            }
                        }, viewThinkingMessage, elaborationRequest, ModelType);
                }

                if (logMessage == null || logMessage.content == null)
                {
                    previous = "none";
                }
                else
                {
                    previous = logMessage.content;
                }

                if (index == 0)
                {
                    await openAIChat.setConversationTitle(MODEL_TYPE.GPT_35_TURBO, update);
                }

                if (IsStop)
                {
                    break;
                }
            }

            callback?.Invoke(null);
        }

        private (MarkdownViewerViewModel viewThinkingMessage, ConversationEntry? logMessage) createRequest(int index, string role)
        {
            string answerRole = ConversationEntry.ROLE_ASSISTANT;
            switch (role)
            {
                case ConversationEntry.ROLE_USER:// 使うべきでない（代わりに ROLE_QUESTION を使う）
                case ConversationEntry.ROLE_QUESTION:
                    answerRole = ConversationEntry.ROLE_ASSISTANT;
                    break;

                case ConversationEntry.ROLE_THINKING:
                    answerRole = ConversationEntry.ROLE_CONSIDERATION;
                    break;
            }

            MarkdownViewerViewModel viewThinkingMessage;
            ConversationEntry? logMessage;

            viewThinkingMessage = createViewMessage(ConversationEntry.ROLE_ASSISTANT, "");
            viewThinkingMessage.MarkdownText = "wait...";

            logMessage = openAIChat.AppendMessage(answerRole, "");

            return (viewThinkingMessage, logMessage);
        }

        public async Task ProcessQuerysAsync(List<(Func<string, Task>?, PipelineInfo?)> querys, bool isPrepend, string role = ConversationEntry.ROLE_ADVICE)
        {
            var taskList = new List<Task<string?>>();

            foreach (var (action, message) in querys)
            {
                if (message == null)
                {
                    continue;
                }

                var task = openAIChat.SystemRequest(MODEL_TYPE.GPT_35_TURBO_16K, action == null, message.Direction, (role) => role == ConversationEntry.ROLE_SYSTEM);
                taskList.Add(task);
            }

            // すべてのタスクが完了するまで待機
            string?[] results = await Task.WhenAll(taskList);

            for (int i = 0; i < results.Length; i++)
            {
                var (action, message) = querys[i];
                var result = results[i];

                if (result != null)
                {
                    if (action != null)
                    {
                        await action(result);
                    }
                    else
                    {
                        if (message?.DirectionName != null)
                        {
                            QueryManager.Instance.SetNameAndText(message.DirectionName, result);
                        }

                        if (isPrepend)
                        {
                            PrependMessageToOpenAIChat(role, "Supplementary note:  " + Environment.NewLine + result);
                        }
                        else
                        {
                            AppendMessageToOpenAIChat(role, "Supplementary note:  " + Environment.NewLine + result);
                        }
                    }
                }

                if (IsStop)
                {
                    break;
                }
            }

            querys.Clear();
        }

        public void AppendMessageToOpenAIChat(string role, string? content, string? addViewTitle = null)
        {
            commonAppendMessageToOpenAIChat(appendViewMessage, openAIChat.AppendMessage, role, content, addViewTitle);
        }

        public void PrependMessageToOpenAIChat(string role, string? content, string? addViewTitle = null)
        {
            commonAppendMessageToOpenAIChat(prependMessage, openAIChat.AppendMessage, role, content, addViewTitle);
        }

        private void commonAppendMessageToOpenAIChat(
            Action<string, string> appendViewMessage,
            Func<string, string, ConversationEntry?> AppendMessage,
            string role, 
            string? content,
            string? addViewTitle = null)
        {
            if (content == null)
            {
                return;
            }

            AppendMessage(role, content);

            if (ConversationEntry.IsView(role))
            {
                if (!string.IsNullOrEmpty(addViewTitle))
                {
                    appendViewMessage(role, addViewTitle + "  \n  \n" + content);
                }
                else
                {
                    appendViewMessage(role, content);
                }
            }
        }

        public void PrepnedFunctionResultToOpenAIChat(string role, string content, string name)
        {
            if (content == null)
            {
                return;
            }

            openAIChat.PrependFunctionResult(role, content, name);

            if (ConversationEntry.IsView(role))
            {
                prependMessage(role, content);
            }
        }

        private void appendViewMessage(string role, string message)
        {
            commonAppendMessage(role, message, false);
        }

        private void prependMessage(string role, string message)
        {
            commonAppendMessage(role, message, true);
        }

        private void commonAppendMessage(string role, string message, bool isPrepend)
        {
            if (role == ConversationEntry.ROLE_USER || role == ConversationEntry.ROLE_THINKING)
            {
                // ユーザーの入力は自動改行
                message = message.Replace(Environment.NewLine, "  " + Environment.NewLine);
            }

            var viewer = new MarkdownViewerViewModel
            {
                Role = role,
                MarkdownText = message
            };

            if (isPrepend && MarkdownViewers.Count > 0)
            {
                MarkdownViewers.Insert(MarkdownViewers.Count - 1, viewer);
            }
            else
            {
                MarkdownViewers.Add(viewer);
            }
        }

        private MarkdownViewerViewModel createViewMessage(string role, string message)
        {
            var viewer = new MarkdownViewerViewModel
            {
                Role = role,
                MarkdownText = message
            };

            MarkdownViewers.Add(viewer);

            return viewer;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
