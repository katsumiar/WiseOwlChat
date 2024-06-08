using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WiseOwlChat
{
    public class OpenAIChat
    {
        private string? _apiKey;
        private Func<string?>? _persona;
        private string _endpoint = "https://api.openai.com/v1/chat/completions";
        private ConversationInfo? _conversation;
        private ObservableCollection<ConversationInfo>? ConversationInfos;
        private bool is_addCollection = false;
        public MODEL_TYPE ModelType { get; set; } = MODEL_TYPE.GPT_4o;
        private ForbiddenExpressionChecker forbiddenExpressionChecker = new();
        public APIFunctionRegistry FunctionCallingRegistry = new();

        private readonly bool isQueryLogout = false;

        private string? _memo = null;
        private string? memo
        {
            get => _memo;
            set
            {
                if (value == null)
                {
                    _memo = null;
                }
                else
                {
                    _memo = value;
                }
            }
        }

        public enum MODEL_TYPE
        {
            [Description("gpt-4o")]
            GPT_4o,

            [Description("gpt-3.5-turbo")]
            GPT_35_TURBO,

            [Description("gpt-4-turbo")]
            GPT_4_TURBO,

            [Description("gpt-4")]
            GPT_4,
            
            [Description("gpt-4-32k")]
            GPT_4_32K,
        }

        public static class EnumHelper
        {
            public static string? GetDescription(Enum value)
            {
                var fieldInfo = value.GetType().GetField(value.ToString());

                if (fieldInfo == null) return null;

                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                return attributes.Length > 0 ? attributes[0].Description : value.ToString();
            }
        }

        private JsonSerializerSettings jsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly HttpClient httpClientSendMessageAsync = new HttpClient();
        private static readonly HttpClient httpClientSendMessageStreamAsync = new HttpClient();
        private static readonly HttpClient httpClientSendMessageMainStreamAsync = new HttpClient();

        public OpenAIChat(string? key, Func<string?>? persona)
        {
            _apiKey = key;

            if (persona != null)
            {
                _persona = persona;
            }

            LoadDLLsFromPluginsDirectory();

            httpClientSendMessageAsync.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            httpClientSendMessageStreamAsync.DefaultRequestHeaders.Add("accept", "text/event-stream");
            httpClientSendMessageStreamAsync.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            httpClientSendMessageMainStreamAsync.DefaultRequestHeaders.Add("accept", "text/event-stream");
            httpClientSendMessageMainStreamAsync.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public void LoadDLLsFromPluginsDirectory()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string importDirectory = System.IO.Path.Combine(currentDirectory, "Plugins");

            FunctionAnalyzer analyzer = new FunctionAnalyzer();
            List<MethodSignature> methodSignatures = analyzer.Analyze(typeof(IFunctionCalling));

            if (Directory.Exists(importDirectory))
            {
                string[] dllFiles = Directory.GetFiles(importDirectory, "*.dll");

                foreach (string dllPath in dllFiles)
                {
                    var assembly = Assembly.LoadFrom(dllPath);

                    foreach (Type classType in assembly.GetTypes())
                    {
                        if (classType.IsInterface)
                        {
                            continue;
                        }

                        MethodCaller? methodCaller = FunctionAnalyzer.IsImplementingInterface(classType, methodSignatures);
                        if (methodCaller == null)
                        {
                            continue;
                        }

                        RegistAPIFunction(new FunctionCallingMediator(methodCaller));
                    }
                }
            }
        }

        public void RegistAPIFunction(IFunctionCalling functionCalling)
        {
            FunctionCallingRegistry.RegistAPIFunction(functionCalling);
        }

        public void ClearMessage(ObservableCollection<ConversationInfo> ConversationInfos)
        {
            _conversation = null;
            is_addCollection = false;
            this.ConversationInfos = ConversationInfos;
        }

        public void SetupMessage(ConversationInfo conversation)
        {
            _conversation = conversation;
            is_addCollection = true;
        }

        public void PartnerReflection()
        {
            string set_memo;

            if (memo == null)
            {
                set_memo = "No information is available yet.";
            }
            else
            {
                set_memo = memo;
            }

            string partnerReflectionGuidelines = DirectionsFileManager.Instance.GetContent(Directions.Partner_Reflection_Guidelines) + $"{set_memo}";

            SystemRequest((result) =>
            {
                if (result != null)
                {
                    memo = result;
                }

                return result;

            }, MODEL_TYPE.GPT_35_TURBO, true, partnerReflectionGuidelines, (role) => role == ConversationEntry.ROLE_SYSTEM);
        }

        public void Remove(ConversationEntry conversation) 
        {
            _conversation?.Conversation.Remove(conversation);
        }

        public ConversationEntry? AppendMessage(string role, string content)
        {
            return commonAppendMessage(role, content, null, false);
        }

        public ConversationEntry? PrependMessage(string role, string content)
        {
            return commonAppendMessage(role, content, null, true);
        }

        public ConversationEntry? PrependFunctionResult(string role, string content, string? name)
        {
            return commonAppendMessage(role, content, name, true);
        }

        private ConversationEntry? commonAppendMessage(string role, string content, string? name, bool isPrepend)
        {
            if (_conversation == null)
            {
                if (name != null)
                {
                    return null;
                }

                _conversation = new ConversationInfo();
                addBasicInstructions(_conversation.Conversation);
            }

            ConversationEntry node;
            if (name == null)
            {
                node = new() { role = role, content = content };
            }
            else
            {
                node = new() { role = role, content = content, name = name };
            }

            if (isPrepend && _conversation.Conversation.Count > 0)
            {
                _conversation.Conversation.Insert(_conversation.Conversation.Count - 1, node);
            }
            else
            {
                _conversation.Conversation.Add(node);
            }

            return node;
        }

        private void addBasicInstructions(List<ConversationEntry> conversations, string? instructions = null)
        {
            string basicInstructions = instructions ?? makeBasicInstructions();
            conversations.Add(
                new ConversationEntry
                {
                    role = ConversationEntry.ROLE_SYSTEM,
                    content = basicInstructions
                });
        }

        private string makeBasicInstructions()
        {
            string basicInstructions;

            if (memo != null)
            {
                basicInstructions = $"{_persona?.Invoke()}\n" +
                        $"# The next log is the information I got from the previous conversation, which I was recording.\n{memo}\n";
            }
            else
            {
                basicInstructions = $"{_persona?.Invoke()}";
            }

            return basicInstructions;
        }

        public async Task<string?> SendMessageAsync(MODEL_TYPE? modelType = null, List<ConversationEntry>? conversation = null)
        {
            if (modelType == null)
            {
                modelType = ModelType;
            }

            if (conversation == null)
            {
                conversation = _conversation?.Conversation;
            }

            List<ConversationEntry>? filteredConversation = new List<ConversationEntry>();
            makeFilteredConversation(conversation, filteredConversation);

            var requestBody = new
            {
                model = EnumHelper.GetDescription(modelType),
                messages = filteredConversation?.ToArray()
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);

            try
            {
                var response = await httpClientSendMessageAsync.PostAsync(_endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));
                string contentResult = await response.Content.ReadAsStringAsync();

                string? result = null;
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(contentResult);

                if (jsonResponse?.choices?.Count > 0)
                {
                    result = jsonResponse.choices[jsonResponse.choices.Count - 1].message.content?.ToString();
                }

                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {

            }
        }

        public async Task<string?> SendMessageStreamAsync(
            Action update,
            Action<string?>? callback,
            Action<string> setMessage,
            MODEL_TYPE? modelType = null,
            List<ConversationEntry>? conversation = null)
        {
            if (modelType == null)
            {
                modelType = ModelType;
            }

            if (conversation == null)
            {
                conversation = _conversation?.Conversation;
            }

            List<ConversationEntry>? filteredConversation = new List<ConversationEntry>();
            makeFilteredConversation(conversation, filteredConversation);

            var requestBody = new
            {
                model = EnumHelper.GetDescription(modelType),
                messages = filteredConversation?.ToArray(),
                stream = true,
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);

            if (isQueryLogout)
            {
                LogWindow.Instance
                    .AppendLog(message: "--------------- Sub", color: Brushes.Green, isNewLine: true)
                    .AppendLog(message: jsonBody, color: Brushes.White, isNewLine: true);
            }

            try
            {
                var response = await httpClientSendMessageStreamAsync.PostAsync(_endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                // 応答ストリームを取得
                using var responseStream = await response.Content.ReadAsStreamAsync();

                bool isError = false;
                string? result = "";

                {
                    FunctionDetail functionDetail = new();

                    using var reader = new StreamReader(responseStream, Encoding.UTF8);

                    while (!reader.EndOfStream)
                    {
                        string? contentResult = await reader.ReadLineAsync();

                        if (contentResult != null)
                        {
                            if (string.IsNullOrEmpty(contentResult))
                                continue;

                            if (contentResult.StartsWith("data: [DONE]"))
                            {
                                // API呼び出し完了
                                break;
                            }

                            if (contentResult.StartsWith("data: "))
                            {
                                contentResult = contentResult.Substring("data: ".Length);

                                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(contentResult);

                                if (jsonResponse?.choices?.Count > 0)
                                {
                                    var choices = jsonResponse.choices[jsonResponse.choices.Count - 1];

                                    if (choices == null)
                                        continue;

                                    var finish_reason = choices.finish_reason;

                                    if (finish_reason == null)
                                    {
                                        var delta = choices.delta;

                                        if (delta == null)
                                            continue;

                                        // 返答の差分
                                        string? assistantContent = delta.content?.ToString();
                                        result = addDeltaContent(update, setMessage, result, assistantContent);
                                    }
                                    else
                                    {
                                        var finish_reason_type = finish_reason.ToString();

                                        if (finish_reason_type == "stop")
                                        {
                                            // 返答終了

                                            break;
                                        }
                                    }
                                }
                            }
                            else if (contentResult != "")
                            {
                                isError = true;
                                result += contentResult;
                            }
                        }
                    }
                }

                if (isError)
                {
                    result = processReceiveErrorInSendMessageStream(setMessage, response, result);
                }

                callback?.Invoke(result);
                return result; // GPTの返答を返す
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                setMessage(message);

                AppendMessage(ConversationEntry.ROLE_ASSISTANT, message);
            }

            callback?.Invoke(null);

            return null;
        }

        public class FunctionDetail
        {
            public string? FuncName { get; private set; } = null;
            public string FuncParam { get; private set; } = "";

            public void SetFuncName(string name)
            {
                FuncName = name.Trim();
            }

            public void AppendToFuncArguments(string additionalParam)
            {
                FuncParam += additionalParam;
            }

            public bool IsFuncNameSet()
            {
                return FuncName != null;
            }
        }

        public async Task<(string?, bool)> SendMessageMainStreamAsync(
            Action update, 
            Action<string?>? callback,
            Action<string> setMessage,
            Action<string, string, string> addMessage,
            bool isFunction)
        {
            bool reQuery = false;
            MODEL_TYPE modelType = ModelType;

            List<ConversationEntry>? conversation = _conversation?.Conversation;
            List<ConversationEntry>? filteredConversation = new List<ConversationEntry>();
            makeFilteredConversation(conversation, filteredConversation);

            string jsonBody = SerializeSendMessageStreamRequest(isFunction, modelType, filteredConversation);

            if (isQueryLogout)
            {
                LogWindow.Instance
                    .AppendLog(message: "--------------- Main", color: Brushes.Green, isNewLine: true)
                    .AppendLog(message: jsonBody, color: Brushes.White, isNewLine: true);
            }

            try
            {
                var response = await httpClientSendMessageMainStreamAsync.PostAsync(_endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                // 応答ストリームを取得
                using var responseStream = await response.Content.ReadAsStreamAsync();

                bool isError = false;
                string? result = "";
                    
                {
                    FunctionDetail functionDetail = new();
                        
                    using var reader = new StreamReader(responseStream, Encoding.UTF8);

                    while (!reader.EndOfStream)
                    {
                        string? contentResult = await reader.ReadLineAsync();

                        if (contentResult != null)
                        {
                            if (string.IsNullOrEmpty(contentResult))
                                continue;

                            if (contentResult.StartsWith("data: [DONE]"))
                            {
                                // API呼び出し完了
                                if (callback != null)
                                {
                                    reQuery = true;
                                    callback = null;
                                }
                                break;
                            }

                            if (contentResult.StartsWith("data: "))
                            {
                                contentResult = contentResult.Substring("data: ".Length);

                                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(contentResult);

                                if (jsonResponse?.choices?.Count > 0)
                                {
                                    var choices = jsonResponse.choices[jsonResponse.choices.Count - 1];

                                    if (choices == null)
                                        continue;

                                    var finish_reason = choices.finish_reason;

                                    if (finish_reason == null)
                                    {
                                        var delta = choices.delta;

                                        if (delta == null)
                                            continue;

                                        var function_call = delta.function_call;
                                        if (function_call != null)
                                        {
                                            // API呼び出し依頼の差分

                                            constructApiRequest(functionDetail, function_call);
                                        }
                                        else
                                        {
                                            // 返答の差分

                                            string? assistantContent = delta.content?.ToString();
                                            result = addDeltaContent(update, setMessage, result, assistantContent);
                                        }
                                    }
                                    else
                                    {
                                        var finish_reason_type = finish_reason.ToString();

                                        if (finish_reason_type == "stop")
                                        {
                                            // 返答終了
                                                
                                            break;
                                        }

                                        if (finish_reason_type == "function_call")
                                        {
                                            // APIを呼び出し

                                            IFunctionCalling? matchingFunction = FunctionCallingRegistry.FirstOrDefaultFunction(functionDetail.FuncName);
                                            result = await CallAPI(update, setMessage, addMessage, functionDetail.FuncName, functionDetail.FuncParam, matchingFunction);
                                        }
                                    }
                                }
                            }
                            else if (contentResult != "")
                            {
                                isError = true;
                                result += contentResult;
                            }
                        }
                    }
                }

                if (isError)
                {
                    result = processReceiveErrorInSendMessageStream(setMessage, response, result);
                }
                    
                if (!is_addCollection && _conversation != null)
                {
                    // 会話タイトルを作成
                    await setConversationTitle(MODEL_TYPE.GPT_35_TURBO, update);
                }

                callback?.Invoke(result);
                return (result, reQuery); // GPTの返答を返す
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                setMessage(message);

                AppendMessage(ConversationEntry.ROLE_ASSISTANT, message);
            }

            callback?.Invoke(null);

            return (null, reQuery);
        }

        private static void constructApiRequest(FunctionDetail functionDetail, dynamic function_call)
        {
            if (function_call.name != null)
            {
                functionDetail.SetFuncName((string)function_call.name);
            }

            if (functionDetail.IsFuncNameSet())
            {
                if (function_call.arguments != null)
                {
                    functionDetail.AppendToFuncArguments((string)function_call.arguments);
                }
            }
        }

        private static string? addDeltaContent(Action update, Action<string> setMessage, string? result, string? assistantContent)
        {
            if (!string.IsNullOrEmpty(assistantContent))
            {
                result += assistantContent;

                setMessage(result);
                update();
            }

            return result;
        }

        public async Task setConversationTitle(MODEL_TYPE? modelType, Action update)
        {
            if (_conversation == null || modelType  == null)
            {
                return;
            }

            string request = $"Decide on a short title for this conversation. Just the title is enough for the answer. No need for extra conversation. {ConversationEntry.SelectedLanguage}";

            List<ConversationEntry> tempRequest = MakeRequest(true, request, (role) => role == ConversationEntry.ROLE_FUNCTION);
            ConversationInfos?.Insert(0, _conversation);
            await SendMessageStreamAsync(update, null, (msg) =>
                    {
                        _conversation.Title = msg.Split(Environment.NewLine)[0];
                        _conversation.ModelType = EnumHelper.GetDescription(modelType);
                    }, MODEL_TYPE.GPT_35_TURBO, tempRequest);

            is_addCollection = true;
        }

        private string? processReceiveErrorInSendMessageStream(Action<string> setMessage, HttpResponseMessage response, string? result)
        {
            if (result != null)
            {
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);
                var message = jsonResponse?.error.message.ToString();
                var code = jsonResponse?.error.code.ToString();

                result = $"[{code}] {message}  " + Environment.NewLine + "```" + Environment.NewLine + response + Environment.NewLine + "```";

                setMessage(result);

                if (result != null)
                {
                    AppendMessage(ConversationEntry.ROLE_ASSISTANT, result);
                }
            }
            else
            {
                AppendMessage(ConversationEntry.ROLE_ASSISTANT, "unknown error.");
            }

            result = null;
            return result;
        }

        private string SerializeSendMessageStreamRequest(bool isFunction, MODEL_TYPE modelType, List<ConversationEntry> filteredConversation)
        {
            string jsonBody;

            if (FunctionCallingRegistry.FuncionsCount() > 0)
            {
                var requestBody = new
                {
                    model = EnumHelper.GetDescription(modelType),
                    messages = filteredConversation?.ToArray(),
                    stream = true,
                    functions = FunctionCallingRegistry.FunctionsSelect().ToArray(),
                    function_call = isFunction ? "auto" : "none"
                };

                jsonBody = JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);
            }
            else
            {
                var requestBody = new
                {
                    model = EnumHelper.GetDescription(modelType),
                    messages = filteredConversation?.ToArray(),
                    stream = true,
                };

                jsonBody = JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);
            }

            return jsonBody;
        }

        private async Task<string?> CallAPI(Action update, Action<string> setMessage, Action<string, string, string> addMessage, string? funcName, string funcParam, IFunctionCalling? matchingFunction)
        {
            string? result = null;

            if (matchingFunction != null && matchingFunction.FunctionName == funcName)
            {
                var addContent = new Action<string>((content) =>
                {
                    var error = forbiddenExpressionChecker.Check(content);
                    if (error != null)
                    {
                        addMessage(ConversationEntry.ROLE_FUNCTION, $"User attempted to publish {error} but was restricted by the WiseOwlChat system. Publishing {error} is prohibited. You must avoid prohibited activities.", funcName);
                    }
                    else
                    {
                        addMessage(ConversationEntry.ROLE_FUNCTION, content, funcName);
                    }
                });

                result = await matchingFunction.ExecAsync(addContent, funcParam,
                    (mes) => {
                        return MessageWindow.MessageDialog(mes, "Confirmation", true);
                    });
                
                setMessage(result);
                update();
            }

            return result;
        }

        private void makeFilteredConversation(List<ConversationEntry>? conversation, List<ConversationEntry> filteredConversation, Predicate<string?>? IsCancel = null)
        {
            List<ConversationEntry> tempConversation = new();
            if (conversation != null)
            {
                foreach (var conv in conversation)
                {
                    if (IsCancel != null && IsCancel(conv.role))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(conv.content))
                    {
                        continue;
                    }

                    if (ConversationEntry.IsSend(conv.role) && conv.content != null)
                    {
                        tempConversation.Add(
                            new ConversationEntry()
                            {
                                role = ConversationEntry.TransRole(conv.role),
                                content = conv.GetAlternativeContent() ?? conv.content,
                                name = conv.name,
                            });
                    }
                }
            }

            filteredConversation.Clear();

            const int topLength = 3;
            const int bottomLength = 5;

            if (tempConversation.Count <= topLength + bottomLength)
            {
                filteredConversation.AddRange(tempConversation);
            }
            else
            {
                int count = 0;

                for (int i = 0; i < tempConversation.Count && count < topLength; i++)
                {
                    if (string.IsNullOrEmpty(tempConversation[i].content))
                        continue;

                    filteredConversation.Add(tempConversation[i]);

                    if (tempConversation[i].role != ConversationEntry.ROLE_SYSTEM)
                    {
                        count++;
                    }
                }

                tempConversation.RemoveRange(0, filteredConversation.Count);

                tempConversation.Reverse();

                List<ConversationEntry> tempReversConversation = new();
                count = 0;
                for (int i = 0; i < tempConversation.Count && count < bottomLength; i++)
                {
                    if (string.IsNullOrEmpty(tempConversation[i].content))
                        continue;

                    tempReversConversation.Add(tempConversation[i]);
                    if (tempConversation[i].role != ConversationEntry.ROLE_SYSTEM)
                    {
                        count++;
                    }
                }

                tempReversConversation.Reverse();
                filteredConversation.AddRange(tempReversConversation);
            }
        }

        public async Task<string?> SystemRequestStreamForPipeline(Action update, Action<string?>? callback, MarkdownViewerViewModel? viewMessage, List<ConversationEntry>? tempRequest, MODEL_TYPE? modelType)
        {
            if (viewMessage == null)
            {
                return null;
            }
            var result = await SendMessageStreamAsync(update, callback, (msg) => { viewMessage.MarkdownText = msg; }, modelType, tempRequest);
            _conversation?.Conversation.RemoveAll((n) => n.role == ConversationEntry.ROLE_QUESTION);
            return result;
        }

        public async Task<string?> SystemRequest(MODEL_TYPE modelType, bool addConversation, string? request, Predicate<string?>? IsCancel)
        {
            List<ConversationEntry> tempRequest = MakeRequest(addConversation, request, IsCancel);
            return await SystemRequest(modelType, tempRequest);
        }

        public async void SystemRequest(Func<string?, string?> func, MODEL_TYPE modelType, bool addConversation, string? request, Predicate<string?>? IsCancel, int limitCount = 0)
        {
            List<ConversationEntry> log = MakeRequest(addConversation, request, IsCancel);

            if (log.Count > limitCount)
            {
                _ = func(await SystemRequest(modelType, log));
            }
        }

        public async Task<string?> SystemRequest(MODEL_TYPE modelType, List<ConversationEntry> tempRequest)
        {
            try
            {
                return await SendMessageAsync(modelType, tempRequest);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {

            }
        }

        public List<ConversationEntry> MakeRequest(bool addConversation, string? request, Predicate<string?>? IsCancel, bool isAddBasicInstructions = false, string? basicInstructions = null)
        {
            List<ConversationEntry> tempRequest = new List<ConversationEntry>();

            if (isAddBasicInstructions)
            {
                addBasicInstructions(tempRequest, basicInstructions);
            }

            if (addConversation && _conversation != null)
            {
                makeFilteredConversation(_conversation.Conversation, tempRequest, IsCancel);
            }
            if (request != null)
            {
                tempRequest.Add(new ConversationEntry { role = ConversationEntry.ROLE_USER, content = request });
            }

            return tempRequest;
        }
    }
}
