using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace WiseOwlChat
{
    public enum Directions
    {
        [Description("user_intent_classification_guide")]
        user_intent_classification_guide,

        [Description("user_intent_text_formatting_guide")]
        user_intent_text_formatting_guide,

        [Description("persona_instructions_guide")]
        persona_instructions_guide,

        [Description("Partner_Reflection_Guidelines")]
        Partner_Reflection_Guidelines,

        [Description("URL_Explanation_Prompt")]
        URL_Explanation_Prompt,

        [Description("_Advice")]// pipeline
        Advice,
    }

    public class DirectionsFileManager
    {
        public enum AttributeType
        {
            Advice,
            Strategy
        }

        public class PipelineInfo
        {
            public string? DirectionName;
            public Func<string?>? DirectionFunc { get; set; }
            public string? Direction 
            {
                get
                {
                    string? result = DirectionFunc?.Invoke();
                    return result;
                }
            }
            public AttributeType Attribute { get; set; }
        }

        private readonly string rootDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Directions");
        private const string directionFileExtension = ".ida";
        private const string pipelineFileExtension = ".pip";

        private static readonly Lazy<DirectionsFileManager> instance = new(() => new DirectionsFileManager());

        public static DirectionsFileManager Instance => instance.Value;

        private Dictionary<string, string?> directionsContent = new();
        private Dictionary<string, string?> pipelinesContent = new();

        private DirectionsFileManager()
        {
            if (Directory.Exists(rootDir))
            {
                fetchContents(rootDir, directionsContent, $"*{directionFileExtension}");
            }

            foreach (Directions value in Enum.GetValues(typeof(Directions)))
            {
                // チェック

                if (value == Directions.Advice)
                {
                    // pipelineなので例外

                    continue;
                }

                string? direction = GetDescription(value);
                if (direction == null || GetContent(direction) == null)
                {
                    LogWindow.Instance
                        .AppendLog(message: $"direction not found: {direction}", color: Brushes.Red, isNewLine: false);
                }
            }

            if (Directory.Exists(rootDir))
            {
                fetchContents(rootDir, pipelinesContent, $"*{pipelineFileExtension}");
                foreach (var pipeline in pipelinesContent)
                {
                    // チェック

                    FetchPipeline(pipeline.Key);
                }
            }
        }

        private void fetchContents(string rootPath, Dictionary<string, string?> valuePairs, string extend)
        {
            string[] txtFiles = Directory.GetFiles(rootPath, extend);

            foreach (string filePath in txtFiles)
            {
                string keyName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                tryFetchContent(valuePairs, keyName, filePath);
            }
        }

        private static void tryFetchContent(Dictionary<string, string?> valuePairs, string keyName, string filePath)
        {
            if (!valuePairs.ContainsKey(keyName))
            {
                string? fileContent = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
                if (fileContent != null)
                {
                    fileContent = fileContent.Trim();
                }
                valuePairs[keyName] = fileContent;
            }
        }

        public void SetPipelines(ObservableCollection<string> Pipelines)
        {
            foreach (var pipelineContent in pipelinesContent)
            {
                if (!pipelineContent.Key.StartsWith("_"))
                {
                    Pipelines.Add(pipelineContent.Key);
                }
            }
        }

        public PipelineInfo CreateContentPipelineInfo(Directions fileName)
        {
            string? filePath = GetDescription(fileName);
            var info = new PipelineInfo() 
            {
                DirectionName = filePath,
                Attribute = AttributeType.Advice,
                DirectionFunc = () => GetContent(filePath),
            };

            return info;
        }

        public string? GetContent(Directions? fileName)
        {
            if (fileName == null)
            {
                return null;
            }
            string? filePath = GetDescription(fileName);
            return GetContent(filePath);
        }

        public string? GetContent(string? filePath)
        {
            if (filePath == null)
            {
                return null;
            }
            return directionsContent.TryGetValue(filePath, out var content) ? content : null;
        }

        public string? GetPipelineContent(Directions fileName)
        {
            string? filePath = GetDescription(fileName);
            return GetPipelineContent(filePath);
        }

        public string? GetPipelineContent(string? filePath)
        {
            if (filePath == null)
            {
                return null;
            }
            return pipelinesContent.TryGetValue(filePath, out var content) ? content : null;
        }

        public IEnumerable<PipelineInfo>? FetchPipeline(Directions pipelineName)
        {
            string? filePath = GetDescription(pipelineName);
            return FetchPipeline(filePath);
        }

        public IEnumerable<PipelineInfo>? FetchPipeline(string? pipelineName)
        {
            if (pipelineName == null)
            {
                return null;
            }

            List<PipelineInfo> pipelineInfos = new List<PipelineInfo>();

            if (pipelinesContent.TryGetValue(pipelineName, out string? pipelineData) && pipelineData != null)
            {
                string[] lines = pipelineData.Split(Environment.NewLine);
                foreach (var line in lines)
                {
                    if (line.StartsWith(":"))
                    {
                        continue;
                    }
                    if (line.Length > 2 && (line.StartsWith("@") || line.StartsWith("-")))
                    {
                        string? direction = line.Substring(1)?.Trim();
                        if (!string.IsNullOrEmpty(direction))
                        {
                            AttributeType attributeType = line.StartsWith("-") ? AttributeType.Advice : AttributeType.Strategy;

                            string filePath = System.IO.Path.Combine(rootDir, direction);
                            tryFetchContent(directionsContent, direction, filePath + directionFileExtension);

                            if (GetContent(direction) == null)
                            {
                                LogWindow.Instance
                                    .AppendLog(message: $"direction not found: {direction}", color: Brushes.Red, isNewLine: false);
                            }

                            pipelineInfos.Add(new PipelineInfo
                            {
                                DirectionName = direction,
                                DirectionFunc = () => GetContent(direction),
                                Attribute = attributeType
                            });
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(File.Exists(pipelineName), $"The pipeline file \"{pipelineName}\" was not found.");
            }

            return pipelineInfos;
        }

        private static string? GetDescription(Enum value)
        {
            string? valueString = value.ToString();

            if (valueString != null)
            {
                FieldInfo? fi = value.GetType().GetField(valueString);

                if (fi != null)
                {
                    DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    return attributes.Length > 0 ? attributes[0].Description : null;
                }
            }

            return null;
        }
    }
}
