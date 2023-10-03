using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Shapes;
using HttpMultipartParser;
using OpenWolfPack.Control;

namespace OpenWolfPack
{
    public sealed class HttpListenerSingleton
    {
        private static readonly Lazy<HttpListenerSingleton> lazy = new(() => new HttpListenerSingleton());
        public static HttpListenerSingleton Instance => lazy.Value;

        private readonly string rootDir = System.IO.Path.Combine(AppContext.BaseDirectory, "htmlRoot");

        private HttpListener? listener;
        private readonly ConcurrentDictionary<string, byte[]> files;

        private bool isRunning = false;

        private const string RemoveAPI = "remove";
        private const string UploadAPI = "upload";
        private const string QueryAPI = "query";
        private const string QueryUrlAPI = "queryUrl";

        private const string Storage = "storage";
        private const string IndexHtml = "index.html";

        public string Address => AppDataStore.Instance.getFullAddress();
        public int PortNo => AppDataStore.Instance.Port;

        private HttpListenerSingleton()
        {
            files = new ConcurrentDictionary<string, byte[]>();
        }

        public async void Initialize()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(Address);

            try
            {
                listener.Start();
                isRunning = true;
                OpenUrl(AppDataStore.Instance.getFullAddress());

                await StartListeningAsync();
            }
            catch (Exception e)
            {
                MessageWindow.MessageDialog($"The listener port({AppDataStore.Instance.Port}) could not be used.\n[{e.Message}]");
                MainWindow.Instance?.ForcedTermination();
            }
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // プラットフォームに応じたシェルを使用
                });
            }
            catch (Exception e)
            {
                MessageWindow.MessageDialog($"Could not open URL: {e.Message}");
            }
        }

        private async Task StartListeningAsync()
        {
            if (listener == null)
            {
                return;
            }

            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    await ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    if (!isRunning)
                    {
                        break;
                    }
                    // その他の状況での例外は、ここで処理。
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            await Task.Run(async () =>
            {
                if (request.RawUrl == null)
                {
                    notFound(response);
                }
                else if (request.HttpMethod == "GET")
                {
                    if (request.RawUrl.Contains("?"))
                    {
                        // コマンド

                        parseQuery(request.RawUrl, out var command, out var paramString);
                        var param = parseParam(paramString);

                        string? next = null;
                        if (param.TryGetValue("next", out var _next))
                        {
                            next = _next;
                        }

                        switch (command)
                        {
                            case RemoveAPI:
                                {
                                    if (param.TryGetValue("file", out var file) && file != null)
                                    {
                                        RemoveFile(file);
                                    }
                                    await outHtmlContents(response, next ?? IndexHtml, $"{file} file has been deleted from storage.");
                                }
                                break;

                            case QueryAPI:
                                {
                                    if (param.TryGetValue("request", out var request) && request != null)
                                    {
                                        ChatContainerControl.SetRequest(request);
                                    }
                                    await outHtmlContents(response, next ?? IndexHtml);
                                }
                                break;

                            case QueryUrlAPI:
                                {
                                    if (param.TryGetValue("url", out var url) && url != null)
                                    {
                                        ChatContainerControl.SetUrlRequest(url);
                                    }
                                    await outHtmlContents(response, next ?? IndexHtml);
                                }
                                break;
                        }
                    }
                    else
                    {
                        string url = request.RawUrl.Substring(1);
                        if (url.StartsWith($"{Storage}/"))
                        {
                            string filename = url.Split('/')[^1];
                            byte[]? content = FetchFile(filename);
                            if (content != null)
                            {
                                await getProcess(response, filename, content);
                            }
                            else
                            {
                                notFound(response);
                            }
                        }
                        else
                        {
                            url = complementHtmlExtension(url);
                            string localPath = System.IO.Path.Combine(rootDir, url);
                            if (File.Exists(localPath))
                            {
                                byte[]? fileData = File.ReadAllBytes(localPath);
                                await getProcess(response, localPath, fileData);
                            }
                            else
                            {
                                notFound(response);
                            }
                        }
                    }
                }
                else if (request.HttpMethod == "POST")
                {
                    switch (request.RawUrl.Substring(1))
                    {
                        case UploadAPI:
                            await postProcess(context, request);
                            break;

                        default:
                            notFound(response);
                            break;
                    }
                }
                else if (request.HttpMethod == "HEAD")
                {
                    string url = request.RawUrl.Substring(1);
                    if (url.StartsWith($"{Storage}/"))
                    {
                        string filename = url.Split('/')[^1];
                        byte[]? content = FetchFile(filename);
                        if (content != null)
                        {
                            setContentsType(response, content, System.IO.Path.GetExtension(url));
                        }
                    }
                    else
                    {
                        url = complementHtmlExtension(url);
                        string localPath = System.IO.Path.Combine(rootDir, url);
                        if (File.Exists(localPath))
                        {
                            byte[]? fileData = File.ReadAllBytes(localPath);
                            setContentsType(response, fileData, url);
                        }
                    }
                }
            });

            response.OutputStream.Close();
            response.Close();
        }

        private string complementHtmlExtension(string url)
        {
            if (url == "")
            {
                url = IndexHtml;
            }
            if (System.IO.Path.GetExtension(url) == "")
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(url);
                List<string> htmlExtensions = new() { ".htm", ".html" };
                foreach (string htmlExtension in htmlExtensions)
                {
                    url = filename + htmlExtension;
                    if (File.Exists(System.IO.Path.Combine(rootDir, url)))
                    {
                        break;
                    }
                }
            }

            return url;
        }

        private static bool isTextPlainExtension(string extension)
        {
            List<string> extensions = new() 
            {
                ".txt", ".rtf", ".log", ".yml", ".ini", ".md", ".csv",
                ".cs", ".c", ".cpp", ".cxx", ".cc", ".asm", ".py", ".php", ".js", ".pl", ".pm", ".lua",
                ".go", ".rb", ".m", ".swift", ".ts", ".rs", ".dart", ".kt", ".r", ".hs", ".groovy",
                ".bat", ".sh"
            };
            return extensions.Contains(extension);
        }

        private static bool isImageFileExtension(string extension)
        {
            List<string> extensions = new() { ".jpg", ".jpeg", ".bmp", ".gif", ".png", ".svg", ".tiff", ".webp" };
            return extensions.Contains(extension);
        }

        private static bool tryImageExtension(string extension, out string contentsType)
        {
            if (!isImageFileExtension(extension))
            {
                contentsType = "";
                return false;
            }
            
            if (extension == ".svg")
            {
                contentsType = "image/svg+xml";
            }
            else
            {
                contentsType = "image/" + extension.Substring(1);
            }

            return true;
        }

        private static void notFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.StatusDescription = "Not Found";
        }

        private static void parseQuery(string url, out string command, out string param)
        {
            var _command = url.Substring(1);
            var parames = _command.Split('?');
            command = parames[0];
            param = parames[1];
        }

        public static Dictionary<string, string?> parseParam(string queryString)
        {
            Dictionary<string, string?> parameters = new Dictionary<string, string?>();

            if (string.IsNullOrEmpty(queryString))
            {
                return parameters;
            }

            string[] pairs = queryString.Split('&');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');

                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                }
                else if (keyValue.Length == 1)
                {
                    parameters[keyValue[0]] = null;
                }
            }

            return parameters;
        }

        private async Task postProcess(HttpListenerContext context, HttpListenerRequest request)
        {
            bool isSuccess = false;

            using (Stream body = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                {
                    Stream inputStream = request.InputStream;
                    var parser = MultipartFormDataParser.Parse(inputStream);

                    foreach (var file in parser.Files)
                    {
                        if (!string.IsNullOrWhiteSpace(file.FileName) && file.Data.Length > 0)
                        {
                            byte[] fileData = readFully(file.Data);
                            RegisterFile(file.FileName, fileData);
                            isSuccess = true;
                        }
                    }
                }
            }

            // レスポンスを送ります。
            HttpListenerResponse response = context.Response;
            await outHtmlContents(response, IndexHtml, isSuccess ? "File received successfully." : "Failed to receive file.");
        }

        private static byte[] readFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task outHtmlContents(HttpListenerResponse response, string url, string? message = null)
        {
            if (getHtml(url, out var content))
            {
                await outIndexHtml(response, content, message);
            }
            else
            {
                notFound(response);
            }
        }

        private string getFileListHtml()
        {
            string fileListHtml = "";

            if (!files.IsEmpty)
            {
                fileListHtml += "<table>";
                fileListHtml += "<thead><tr><th>File Name</th><th>Inquiry</th><th>Delete</th></tr></thead>";
                fileListHtml += "<tbody>";

                foreach (var filename in files.Keys)
                {
                    string ext = System.IO.Path.GetExtension(filename);
                    if (isImageFileExtension(ext))
                    {
                        fileListHtml += $"<tr><td><a href='{Storage}/{filename}'>{filename} <img src=\"{Address}{Storage}/{filename}\" style=\"width:10%;\"></a></td><td><a href='{QueryUrlAPI}?url={Address}{Storage}/{filename}'> <i class=\"fas fa-comments\"></i></a></td><td><a href='{RemoveAPI}?file={filename}'><i class=\"fas fa-trash-alt\"></i></a></td></tr>";
                    }
                    else
                    {
                        fileListHtml += $"<tr><td><a href='{Storage}/{filename}'>{filename}</a></td><td><a href='{QueryUrlAPI}?url={Address}{Storage}/{filename}'><i class=\"fas fa-comments\"></i></a></td><td><a href='{RemoveAPI}?file={filename}'><i class=\"fas fa-trash-alt\"></i></a></td></tr>";
                    }
                }

                fileListHtml += "</tbody>";
                fileListHtml += "</table>";
            }
            else
            {
                fileListHtml += "There are no files saved.";
            }

            return fileListHtml;
        }

        private string getFileUploadForm()
        {
            string fileListHtml = "";
            fileListHtml += $"<form action=\"{UploadAPI}\" method=\"post\" enctype=\"multipart/form-data\">";
            fileListHtml += "<label for=\"filename\">Upload File:</label>";
            fileListHtml += "<input type=\"file\" name=\"upload\"></br>";
            fileListHtml += "<input type=\"submit\" value=\"Upload\">";
            fileListHtml += "</form>";

            return fileListHtml;
        }

        private async Task outIndexHtml(HttpListenerResponse response, string contents, string? message = null)
        {
            string _contents = contents;

            if (string.IsNullOrWhiteSpace(message))
            {
                _contents = _contents.Replace("{%message%}", "");
            }
            else
            {
                _contents = _contents.Replace("{%message%}", $"<p class=\"message\">{message}</p>");
            }

            _contents = _contents.Replace("{%fileList%}", getFileListHtml());
            _contents = _contents.Replace("{%uploadForm%}", getFileUploadForm());
            _contents = _contents.Replace("{%Address%}", Address);
            _contents = _contents.Replace("{%PortNo%}", PortNo.ToString());

            var buffer = System.Text.Encoding.UTF8.GetBytes(_contents);

            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private bool getHtml(string url, out string content)
        {
            string _url = url;

            if (string.IsNullOrEmpty(_url))
            {
                _url = IndexHtml;
            }

            if (System.IO.Path.GetExtension(_url) == null)
            {
                _url += ".html";
            }

            string filePath = System.IO.Path.Combine(rootDir, _url);

            content = File.Exists(filePath) ? File.ReadAllText(filePath) : "";

            return !string.IsNullOrEmpty(content);
        }

        private async Task getProcess(HttpListenerResponse response, string filename, byte[] fileData)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            if (extension == ".html" || extension == ".htm")
            {
                var contents = System.Text.Encoding.UTF8.GetString(fileData);
                await outIndexHtml(response, contents);
                return;
            }

            setContentsType(response, fileData, extension);

            await response.OutputStream.WriteAsync(fileData, 0, fileData.Length);
        }

        private static void setContentsType(HttpListenerResponse response, byte[] fileData, string extension)
        {
            if (extension == ".html" || extension == ".htm")
            {
                response.ContentType = "text/html; charset=utf-8";
            }
            else if (isTextPlainExtension(extension))
            {
                response.ContentType = "text/plain; charset=utf-8";
            }
            else if (tryImageExtension(extension, out var contentsType))
            {
                response.ContentType = contentsType;
            }
            else
            {
                switch (extension)
                {
                    case ".css":
                        response.ContentType = "text/css; charset=utf-8";
                        break;

                    case ".js":
                        response.ContentType = "text/javascript; charset=utf-8";
                        break;

                    case ".json":
                        response.ContentType = "application/json; charset=utf-8";
                        break;

                    case ".xml":
                        response.ContentType = "application/xml; charset=utf-8";
                        break;

                    case ".pdf":
                        response.ContentType = "application/pdf; charset=utf-8";
                        break;

                    case ".zip":
                        response.ContentType = "application/zip; charset=utf-8";
                        break;

                    default:
                        response.ContentType = "application/octet-stream";
                        break;
                }
            }
            response.ContentLength64 = fileData.Length;
        }

        public byte[]? FetchFile(string path)
        {
            if (files.TryGetValue(path, out var data))
            {
                return data;
            }

            return null;
        }

        public void RemoveFile(string path)
        {
            files.TryRemove(path, out var data);
        }

        public (string?, bool) RegisterFile(string path, byte[] data)
        {
            int number = 0;
            string ext = System.IO.Path.GetExtension(path);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            string _path = path;
            while (files.ContainsKey(_path))
            {
                number++;
                _path = $"{name}_({number}){ext}";
            }
            files[_path] = data;
            return ($"{AppDataStore.Instance.getFullAddress()}{Storage}/{_path}", true);
        }

        public (string?, bool) RegisterFileFromMyApp(string path)
        {
            string localPath = System.IO.Path.Combine(AppContext.BaseDirectory, path);
            return RegisterFile(localPath);
        }

        public (string?, bool) RegisterFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    byte[] fileData = File.ReadAllBytes(path);
                    string fileName = System.IO.Path.GetFileName(path);

                    return RegisterFile(fileName, fileData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return (null, false);
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
                listener?.Stop();
            }
        }
    }
}