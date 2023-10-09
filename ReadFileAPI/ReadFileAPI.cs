using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ReadFileAPI
{
    public class ReadFileAPI
    {
        const int max_length = 4000;

        public string FunctionName => nameof(ReadFileAPI);// API名
        public string Description => "Get information by accessing local files (you can also narrow down the search).";// APIの説明（LLMが理解できる内容）
        public object Function
        {
            get
            {
                return new
                {
                    name = FunctionName,// API名
                    description = Description,// APIの説明
                    parameters = new
                    {
                        type = "object",// object
                        properties = new
                        {
                            // 必要な引数を列挙する
                            path = new
                            {
                                type = "string",// 引数の型
                                description = "File path (no information other than file path required)"// 引数の説明（LLMが理解できる内容）
                            },
                            regularExpression = new
                            {
                                type = "string",// 引数の型
                                description = "Regular expression(When you want to narrow down conditions, use regular expressions to limit the locations to be retrieved.)"// 引数の説明（LLMが理解できる内容）
                            },
                        },
                        required = new[] { "path" }// 最低限必要な引数
                    },
                };
            }
        }

        public async Task<string> ExecAsync(Action<string> addContent, string param, Func<string, bool> confirm)
        {
            string result;
            var paramData = JsonConvert.DeserializeObject<dynamic>(param);
            if (paramData == null)
            {
                result = "param error.";
                addContent(result);// LLMに送る
            }
            else
            {
                result = "**Load the file...**";// 処理中メッセージ
                if (param != null)
                {
                    try
                    {
                        string path = paramData.path;
                        string? regularExpression = paramData.regularExpression;
                        path = path.Trim();
                        if (confirm($"WiseOwlChat is asking for permission to read the file.\n[ {path} ]") && path != null)
                        {
                            string fileContents = await ReadFileAPI.FetchFile(path, regularExpression);
                            if (fileContents != null)
                            {
                                addContent(fileContents);// LLMに送る
                            }
                        }
                        else
                        {
                            result = "File could not be read.";
                            addContent(result);// LLMに送る
                        }
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        addContent(result);// LLMに送る
                    }
                }
            }
            return result;
        }

        public static async Task<string> FetchFile(string path, string? regularExpression)
        {
            if (!File.Exists(path))
            {
                return $"file not found({path}).";
            }

            Regex? regex = null;
            if (!string.IsNullOrEmpty(regularExpression))
            {
                regex = new Regex(regularExpression);
            }

            string result;
            string contents = await FetchFileContent(path);
            if (IsValidTextContent(contents))
            {
                result = "";
                if (regex != null)
                {
                    var lines = contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    bool isHit = false;
                    foreach (var line in lines)
                    {
                        if (isHit)
                        {
                            contents += line + Environment.NewLine;
                            continue;
                        }
                        if (regex.IsMatch(line))
                        {
                            isHit = true;
                            result = $"Found a match for {regularExpression}.\n";
                            contents = line + Environment.NewLine;
                        }
                    }
                }

                if (contents.Length > max_length)
                {
                    var _test = contents.ToString().Substring(0, max_length);
                    result += $"The content was so long that I couldn't read it all, but the content of the text file at `{path}` is as follows. \n```\n{_test}\n````";
                }
                else
                {
                    result += $"The contents of the text file at \"{path}\" are as follows:\n```\n{contents}\n```";
                }
            }
            else
            {
                result = $"{path} was not a text file.";
            }
            if (string.IsNullOrEmpty(result))
            {
                result = "File could not be read.";
            }
            return result;
        }

        private static async Task<string> FetchFileContent(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                return "An error has occurred:" + ex.Message;
            }
        }

        private static bool IsValidTextContent(string textContent)
        {
            return !textContent.Contains('\0');
        }
    }
}