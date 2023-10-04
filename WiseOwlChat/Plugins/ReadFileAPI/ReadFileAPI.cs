using Newtonsoft.Json;

namespace FetchFileAPI
{
    public class FetchFileAPI
    {
        public string FunctionName => nameof(FetchFileAPI);// API名
        public string Description => "Load a file from local.";// APIの説明（LLMが理解できる内容）
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
                        },
                        required = new[] { "path" }// 最低限必要な引数
                    },
                };
            }
        }

        public async Task<string> ExecAsync(Action<string> addContent, string param, Func<string, bool> confirm)
        {
            var paramData = JsonConvert.DeserializeObject<dynamic>(param);
            if (paramData == null)
            {
                return "param error.";
            }
            string result = "**Load the file...**";// 処理中メッセージ
            if (param != null)
            {
                string path = paramData.path;
                if (confirm($"WiseOwlChat is asking for permission to read the file.\n[ {path} ]") && path != null)
                {
                    string fileContents = await FetchFileAPI.FetchFile(path);
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
            return result;
        }

        public static async Task<string> FetchFile(string path)
        {
            if (!File.Exists(path))
            {
                return $"file not found({path}).";
            }

            string result;
            string contents = await FetchFileContent(path);
            if (IsValidTextContent(contents))
            {
                result = $"The contents of \"{path}\" are as follows. Please respond to the request.\nIt is not necessary to present the contents unless otherwise specified.\n```\n{contents}\n```";
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