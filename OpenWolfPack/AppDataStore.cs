using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenWolfPack
{
    public class AppDataStore
    {
        private static readonly Lazy<AppDataStore> _instance = new Lazy<AppDataStore>(() => new AppDataStore());

        public static AppDataStore Instance => _instance.Value;

        private readonly string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_data.json");

        public string SelectedLanguage { get; set; } = "English";
        public string Address { get; set; } = "http://localhost";
        public int Port { get; set; } = 55553;

        public double? WindowPosX { get; set; }
        public double? WindowPosY { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }

        public AppDataStore()
        {
        }

        public string getFullAddress()
        {
            return $"{Address}:{Port}/";
        }

        public void Load()
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<AppDataStore>(json);

                if (data != null)
                {
                    SelectedLanguage = data.SelectedLanguage;

                    if (MainWindow.Instance != null)
                    {
                        if (data.WindowPosX.HasValue)
                            MainWindow.Instance.Left = data.WindowPosX.Value;
                        if (data.WindowPosY.HasValue)
                            MainWindow.Instance.Top = data.WindowPosY.Value;
                        if (data.WindowWidth.HasValue)
                            MainWindow.Instance.Width = data.WindowWidth.Value;
                        if (data.WindowHeight.HasValue)
                            MainWindow.Instance.Height = data.WindowHeight.Value;
                    }
                }
            }
        }

        public void Save()
        {
            WindowPosX = MainWindow.Instance?.Left;
            WindowPosY = MainWindow.Instance?.Top;
            WindowWidth = MainWindow.Instance?.Width;
            WindowHeight = MainWindow.Instance?.Height;

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
    }
}
