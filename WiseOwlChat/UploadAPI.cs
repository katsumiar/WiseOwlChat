using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Text;

namespace WiseOwlChat
{
    public class UploadAPI : IFunctionCalling
    {
        const int max_length = 2000;

        public string FunctionName => nameof(UploadAPI);// API名
        public string Description => "Upload files to WiseOwlChat dedicated storage and serve them to users.";// APIの説明（LLMが理解できる内容）
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
                            fileName = new
                            {
                                type = "string",// 引数の型
                                description = "file name"// 引数の説明（LLMが理解できる内容）
                            },
                            text = new
                            {
                                type = "string",// 引数の型
                                description = "Contents of the file to be uploaded"// 引数の説明（LLMが理解できる内容）
                            },
                        },
                        required = new[] { "fileName", "text" }// 最低限必要な引数
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
            string result = "**Upload the file...**";// 処理中メッセージ
            if (param != null)
            {
                try
                {
                    await Task.Run(() =>{});
                    string fileName = paramData.fileName;
                    string text = paramData.text;
                    (var url, var sucsess) = HttpListenerSingleton.Instance.RegisterFile(fileName, Encoding.UTF8.GetBytes(text));
                    if (sucsess)
                    {
                        addContent($"Uploaded a {text.Length} byte file to storage. It can be downloaded from `{url}`.");// LLMに送る
                    }
                    else
                    {
                        addContent($"Upload failed.");// LLMに送る
                    }
                }
                catch (Exception ex)
                {
                    return "An error has occurred:" + ex.Message;
                }
            }
            return result;
        }
    }
}