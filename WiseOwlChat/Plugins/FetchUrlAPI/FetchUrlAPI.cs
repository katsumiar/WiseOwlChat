using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Net;
using System.Text;
using System.Net.Http;
using System;

namespace FetchUrlAPI
{
    namespace FetchUrlAPI
    {
        public class FetchUrlAPI
        {
            public string FunctionName => nameof(FetchUrlAPI);// API名
            public string Description => "Access the url and retrieve the information.";// APIの説明（LLMが理解できる内容）
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
                            },
                            required = new[] { "url" }// 最低限必要な引数
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
                string result = "**Access the URL...**";// 処理中メッセージ
                if (param != null)
                {
                    string url = paramData.url;
                    if (confirm($"WiseOwlChat is asking for permission to access the URL.\n[ {url} ]") && url != null)
                    {
                        string contents = await FetchUrlAPI.FetchFromUrlAsync(url);
                        if (contents != null)
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
                return result;
            }

            public static async Task<string> FetchFromUrlAsync(string url)
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
                    int maxTextLength = 0;

                    HtmlNode bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
                    if (bodyNode != null)
                    {
                        HtmlNode articleNode = bodyNode.SelectSingleNode(".//article");
                        HtmlNode mainNode = bodyNode.SelectSingleNode(".//main");

                        HtmlNode targetNode = articleNode ?? mainNode ?? bodyNode;

                        // sectionとfooterタグを取り除く
                        var nodesToRemove = targetNode.SelectNodes(".//section|.//footer");
                        removeNodes(nodesToRemove);

                        RemoveEmptyNodes(targetNode);
                        RemoveClassAttributes(targetNode);

                        foreach (var node in targetNode.DescendantsAndSelf())
                        {
                            if (node.NodeType == HtmlNodeType.Element)
                            {
                                int textLength = node.InnerText.Length;
                                if (textLength > maxTextLength)
                                {
                                    maxTextLength = textLength;
                                    mainText = node.InnerHtml;
                                }
                            }
                        }
                    }

                    if (mainText != null)
                    {
                        result = $"The contents of \"{url}\" are as follows. Please respond to the request.\nIt is not necessary to present the contents unless otherwise specified.\n```\n{mainText}\n```";
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
        }
    }
}