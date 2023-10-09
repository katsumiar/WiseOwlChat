using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace WiseOwlChat
{
    public class APIFunctionRegistry
    {
        private ObservableCollection<PluginInfo> _functions = new();
        public ObservableCollection<PluginInfo> PluginInfos
        {
            get
            {
                return _functions;
            }
        }

        public void RegistAPIFunction(IFunctionCalling? functionCalling)
        {
            if (functionCalling == null)
            {
                return;
            }
            if (!_functions.Any(f => f.FunctionCalling?.FunctionName == functionCalling.FunctionName))
            {
                _functions.Add(new PluginInfo { FunctionCalling = functionCalling });
            }
        }

        public void LoadDLL(string dllPath)
        {
            var assembly = Assembly.LoadFile(dllPath);
            var types = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IFunctionCalling)));

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type) as IFunctionCalling;
                RegistAPIFunction(instance);
            }
        }

        public void CheckEnabled(string? name)
        {
            _functions.Where(f => f.FunctionCalling?.FunctionName == name)
                      .ToList()
                      .ForEach(f => f.Enabled = true);
        }

        public IFunctionCalling? FirstOrDefaultFunction(string? name)
        {
            return _functions.FirstOrDefault(f => f.FunctionCalling?.FunctionName == name)?.FunctionCalling;
        }

        public int FuncionsCount()
        {
            return _functions.Where(f => f.Enabled == true).Count();
        }

        public IEnumerable<object?> FunctionsSelect()
        {
            return _functions.Where(f => f.Enabled == true)
                                .Select(f => f.FunctionCalling?.Function);
        }
    }
}
