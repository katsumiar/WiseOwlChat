using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System;
using System.Globalization;
using static PythonInterpreterAPI.PythonInterpreterAPI;
using Python.Runtime;

namespace PythonInterpreterAPI
{
    public class PythonInterpreterAPI
    {
        const int max_length = 2000;

        public string FunctionName => nameof(PythonInterpreterAPI);// API名
        public string Description => "Runs a Python script and returns results. It is used for various purposes such as calculation and aggregation.";// APIの説明（LLMが理解できる内容）
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
                            script = new
                            {
                                type = "string",// 引数の型
                                description = "Python script to find the solution in the result variable.(passed to `dynamic result = PythonEngine.Exec(script)`)"// 引数の説明（LLMが理解できる内容）
                            },
                        },
                        required = new[] { "script" }// 最低限必要な引数
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
                result = "**Python Interpreter...**";// 処理中メッセージ
                if (param != null)
                {
                    try
                    {
                        string script = paramData.script;
                        script = script.Trim();

                        string pythonResult = await ExecutePythonCodeAsync(script);

                        addContent(pythonResult);// LLMに送る
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

        public static async Task<string> ExecutePythonCodeAsync(string script)
        {
            return await Task.Run(() =>
            {
                string result = "";

                try
                {
                    PythonEngine.Initialize();
                    using (Py.GIL()) // GILを取得
                    {
                        script = script.Replace("\n", "  \n");
                        try
                        {
                            PyModule py = Py.CreateScope();
                            dynamic locals = new PyDict();

                            py.Exec(script, locals);

                            var lines = script.Split("\n");
                            var lastLine = lines[^1];
                            if (lastLine.Contains("="))
                            {
                                lastLine = lastLine.Split('=')[0];
                            }
                            if (lastLine.Contains(","))
                            {
                                var values = lastLine.Split(",");
                                result = $"## script\n```python\n{script}\n```  \n  \n## Result\n";
                                foreach (var _val in values)
                                {
                                    var val = _val.Trim();
                                    result += $"{val} : {locals[val].ToString()}";
                                }

                            }
                            else
                            {
                                lastLine = lastLine.Trim();
                                result = locals[lastLine].ToString();
                                result = $"## script\n```python\n{script}\n```  \n  \n## Result\n{lastLine} : {result}";
                            }
                        }
                        catch (PythonException ex)
                        {
                            result = "Python error: " + ex.Message + $"  \nScript:  \n```python\n{script}\n```\n  \n**Finally, assign the solution to the variable called `result`.**";
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = "Python error: " + ex.Message;
                }
                finally
                {
                    PythonEngine.Shutdown();
                }

                return result;
            });
        }
    }
}