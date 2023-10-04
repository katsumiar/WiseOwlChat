using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Net;
using System.Text;
using System.Net.Http;
using System;
using System.Reflection.PortableExecutable;
using UglyToad.PdfPig;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualBasic;

namespace FetchUrlAPI
{
    namespace FetchUrlAPI
    {
        public class FetchUrlAPI
        {
            const int max_length = 4000;

            public string FunctionName => nameof(FetchUrlAPI);// API名
            public string Description => "Access the URL and retrieve the information(you can also search by narrowing down the conditions).";// APIの説明（LLMが理解できる内容）
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
                                url = new
                                {
                                    type = "string",// 引数の型
                                    description = "URL"// 引数の説明（LLMが理解できる内容）
                                },
                                regularExpression = new
                                {
                                    type = "string",// 引数の型
                                    description = "Regular expression(When you want to narrow down conditions, use regular expressions to limit the locations to be retrieved.)"// 引数の説明（LLMが理解できる内容）
                                },
                            },
                            required = new[] { "url" }// 最低限必要な引数
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
                    result = "**Access the URL...**";// 処理中メッセージ
                    if (param != null)
                    {
                        try
                        {
                            string url = paramData.url;
                            string? regularExpression = paramData.regularExpression;
                            url = url.Trim();
                            if (confirm($"WiseOwlChat is asking for permission to access the URL.\n[ {url} ]") && url != null)
                            {
                                // HTTPクライアントを生成
                                using HttpClient httpClient = new HttpClient();

                                // HTTPヘッダーを取得するだけなので、HttpMethod.Headを使用
                                var httpRequest = new HttpRequestMessage(HttpMethod.Head, new Uri(url));
                                var response = await httpClient.SendAsync(httpRequest);

                                string contents = "Unsupported content type.";
                                if (response.IsSuccessStatusCode)
                                {
                                    // Content-Typeヘッダーを取得
                                    if (response.Content.Headers.TryGetValues("Content-Type", out var values))
                                    {
                                        string contentType = values.FirstOrDefault() ?? "";

                                        // ファイルタイプに応じて処理を分岐
                                        if (contentType.StartsWith("application/pdf"))
                                        {
                                            // PDFファイルの場合の処理
                                            contents = await FetchPdfFromUrlAsync(url, regularExpression);
                                            addContent(contents);// LLMに送る
                                        }
                                        else if (contentType.StartsWith("text/html"))
                                        {
                                            // HTMLファイルの場合の処理
                                            contents = await FetchWebPageFromUrlAsync(url, regularExpression);
                                            addContent(contents);// LLMに送る
                                        }
                                        else if (contentType.StartsWith("text/plain") || contentType.StartsWith("text/css") || contentType.StartsWith("text/javascript")
                                            || contentType.StartsWith("application/json") || contentType.StartsWith("application/xml"))
                                        {
                                            // TXTファイルの場合の処理
                                            contents = await FetchTxtFromUrlAsync(url, regularExpression);
                                            addContent(contents);// LLMに送る
                                        }
                                        else
                                        {
                                            // その他の場合
                                            addContent(contents);// LLMに送る
                                        }
                                    }
                                }
                                else
                                {
                                    addContent(contents);// LLMに送る
                                }
                            }
                            else
                            {
                                result = "Could not access the URL.";
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

            /// <summary>
            /// Asynchronously fetches the contents of a web page from a given URL.
            /// </summary>
            /// <param name="url">The URL of the web page to fetch.</param>
            /// <returns>A task representing the asynchronous operation. The task result contains the HTML content of the web page.</returns>
            public static async Task<string> FetchWebPageFromUrlAsync(string url, string? regularExpression)
            {
                string? result;
                using HttpClient httpClient = new HttpClient();

                try
                {
                    string pageContent = await httpClient.GetStringAsync(url);

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(pageContent);

                    foreach (var node in htmlDocument.DocumentNode.SelectNodes("//script|//style|//img"))
                    {
                        node.Remove();
                    }

                    string? mainText = null;

                    HtmlNode bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
                    mainText = bodyNode.InnerText;

                    mainText = Regex.Replace(mainText, @"\s+", " ");
                    mainText = Regex.Replace(mainText, @"\r\n+", "\r");
                    mainText = Regex.Replace(mainText, @"\r+", "\r");
                    mainText = Regex.Replace(mainText, @"\n+", "\r");

                    Regex? regex = null;
                    if (!string.IsNullOrEmpty(regularExpression))
                    {
                        regex = new Regex(regularExpression);
                    }

                    if (mainText != null)
                    {
                        result = "";
                        if (regex != null)
                        {
                            var lines = mainText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            bool isHit = false;
                            foreach (var line in lines)
                            {
                                if (isHit)
                                {
                                    mainText += line + Environment.NewLine;
                                    continue;
                                }
                                if (regex.IsMatch(line))
                                {
                                    isHit = true;
                                    result = $"Found a match for {regularExpression}.\n";
                                    mainText = line + Environment.NewLine;
                                }
                            }
                        }

                        if (mainText.Length > max_length)
                        {
                            var _test = mainText.ToString().Substring(0, max_length);
                            result += $"The content was so long that I couldn't read it all, but the content of the web contents at `{url}` is as follows. \n```\n{_test}\n````";
                        }
                        else
                        {
                            result += $"The contents of the web contents at \"{url}\" are as follows:\n```\n{mainText}\n```";
                        }
                    }
                    else
                    {
                        result = "Content could not be read.";
                    }
                }
                catch (Exception ex)
                {
                    result = $"An error has occurred: {ex.Message}";
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = "Content could not be read.";
                }

                return result;
            }

            public static void RemoveClassAttributes(HtmlNode node)
            {
                // class属性があれば削除
                if (node.Attributes["class"] != null)
                {
                    node.Attributes["class"].Remove();
                }

                // 子ノードに対しても同じ操作を適用
                foreach (var child in node.ChildNodes)
                {
                    RemoveClassAttributes(child);
                }
            }

            public static void RemoveEmptyNodes(HtmlNode node)
            {
                // ノードリストを事前に作成しておく（リストが動的に変わると問題が生じるため）
                var childNodes = node.ChildNodes.ToList();

                foreach (var child in childNodes)
                {
                    RemoveEmptyNodes(child);
                }

                if (string.IsNullOrWhiteSpace(node.InnerText) && node.Name != "img" && node.Name != "br")
                {
                    node.Remove();
                }
            }

            private static void removeNodes(HtmlNodeCollection sectionNodes)
            {
                if (sectionNodes != null)
                {
                    foreach (var node in sectionNodes)
                    {
                        node.Remove();
                    }
                }
            }

            /// <summary>
            /// Fetches and extracts the text content from a given PDF file URL.
            /// </summary>
            /// <param name="url">The URL of the PDF file to fetch and extract text content from.</param>
            /// <returns>
            /// A Task that represents the asynchronous operation.
            /// The task result contains a string that either holds the extracted text content from the given PDF URL,
            /// or an error message if the operation fails.
            /// </returns>
            public static async Task<string> FetchPdfFromUrlAsync(string url, string? regularExpression)
            {
                string? result;
                try
                {
                    using HttpClient httpClient = new HttpClient();
                    using Stream httpStream = await httpClient.GetStreamAsync(url);
                    using var memoryStream = new MemoryStream();
                    await httpStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0; // ストリームの先頭に戻す

                    using var pdf = PdfDocument.Open(memoryStream);
                    var text = new StringBuilder();

                    Regex? regex = null;
                    if (!string.IsNullOrEmpty(regularExpression))
                    {
                        regex = new Regex(regularExpression);
                    }

                    bool isHit = false;
                    for (var i = 1; i <= pdf.NumberOfPages && text.Length < max_length; i++)
                    {
                        var page = pdf.GetPage(i);

                        if (regex != null)
                        {
                            if (regex.IsMatch(page.Text))
                            {
                                text.AppendLine(page.Text);
                                isHit = true;
                            }
                        }
                        else
                        {
                            text.AppendLine(page.Text);
                        }
                    }

                    if (isHit && regularExpression != null)
                    {
                        result = $"Found a match for {regularExpression}.\n";
                    }
                    else
                    {
                        result = "";
                    }

                    if (text.Length > max_length)
                    {
                        var _test = text.ToString().Substring(0, max_length);
                        result += $"The content was so long that I couldn't read it all, but the content of the PDF at `{url}` is as follows. \n```\n{_test}\n````";
                    }
                    else
                    {
                        result += $"The contents of the PDF at `{url}` are as follows.\n```\n{text}\n```";
                    }
                }
                catch (Exception ex)
                {
                    result = $"An error has occurred: {ex.Message}";
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = "Content could not be read.";
                }

                return result;
            }

            /// <summary>
            /// Fetches and returns the text content from a given .txt file URL.
            /// </summary>
            /// <param name="url">The URL of the .txt file to fetch the text content from.</param>
            /// <returns>
            /// A Task that represents the asynchronous operation. 
            /// The task result contains a string that either holds the text content fetched from the given URL 
            /// or an error message if the operation fails.
            /// </returns>
            public static async Task<string> FetchTxtFromUrlAsync(string url, string? regularExpression)
            {
                using HttpClient httpClient = new HttpClient();
                string? result = null;

                try
                {
                    Regex? regex = null;
                    if (!string.IsNullOrEmpty(regularExpression))
                    {
                        regex = new Regex(regularExpression);
                    }

                    string textContent = await httpClient.GetStringAsync(url);

                    result = "";
                    if (regex != null)
                    {
                        var lines = textContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        bool isHit = false;
                        textContent = "";
                        foreach (var line in lines)
                        {
                            if (isHit)
                            {
                                textContent += line + Environment.NewLine;
                                continue;
                            }
                            if (regex.IsMatch(line))
                            {
                                isHit = true;
                                result = $"Found a match for {regularExpression}.\n";
                                textContent = line + Environment.NewLine;
                            }
                        }
                    }

                    if (textContent.Length > max_length)
                    {
                        var _test = textContent.ToString().Substring(0, max_length);
                        result += $"The content was so long that I couldn't read it all, but the content of the text file at `{url}` is as follows. \n```\n{_test}\n````";
                    }
                    else
                    {
                        result += $"The contents of the text file at \"{url}\" are as follows:\n```\n{textContent}\n```";
                    }
                }
                catch (Exception ex)
                {
                    result = $"An error has occurred: {ex.Message}";
                }

                return result ?? "Content could not be read.";
            }
        }
    }
}