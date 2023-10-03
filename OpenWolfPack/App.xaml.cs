using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OpenWolfPack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string? API_Key =>
            Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
    }
}
