using System;
using System.Collections.Generic;
using System.Reflection;

namespace WiseOwlChat
{
    public class MethodSignature
    {
        public string? Name { get; set; }
        public Type? ReturnType { get; set; }
        public List<Type> Parameters { get; set; } = new List<Type>();
    }

    public class MethodCaller
    {
        private Type classType;
        private object? classObject;
        private Dictionary<string, MethodInfo> methos = new();

        public MethodCaller(Type classType)
        {
            this.classType = classType;
            classObject = Activator.CreateInstance(classType);
        }

        public void RegistMethod(string methodName, MethodInfo method) 
        {
            methos.Add(methodName, method);
        }

        public object? CallFunction(string methodName, object[] args)
        {
            var method = methos[methodName];
            object? result = null;

            if (method != null)
            {
                try
                {
                    result = method.Invoke(classObject, args.Length == 0 ? null : args);
                }
                catch
                {
                    // 特に何もしない
                }
            }

            return result;
        }
    }

    public class FunctionAnalyzer
    {
        public List<MethodSignature> Analyze(Type type)
        {
            List<MethodSignature> detailsList = new List<MethodSignature>();

            MethodInfo[] methods = type.GetMethods();

            foreach (MethodInfo method in methods)
            {
                MethodSignature details = new MethodSignature
                {
                    Name = method.Name,
                    ReturnType = method.ReturnType
                };

                ParameterInfo[] parameters = method.GetParameters();
                foreach (ParameterInfo parameter in parameters)
                {
                    details.Parameters.Add(parameter.ParameterType);
                }

                detailsList.Add(details);
            }

            return detailsList;
        }

        public static MethodCaller? IsImplementingInterface(Type classType, List<MethodSignature> methodSignatures)
        {
            MethodCaller? methodCaller = null;

            foreach (MethodSignature methodSignature in methodSignatures)
            {
                if (methodSignature.Name == null)
                    continue;

                MethodInfo? method = null;
                string name = methodSignature.Name;

                if (methodSignature.Parameters.Count == 0 && methodSignature.Name.StartsWith("get_"))
                {
                    name = methodSignature.Name.Replace("get_", "");
                    PropertyInfo? property = classType.GetProperty(name);
                    method = property?.GetGetMethod();
                    if (method == null)
                    {
                        name = methodSignature.Name;
                    }
                }

                method ??= classType.GetMethod(name, methodSignature.Parameters.ToArray());
        
                if (method == null)
                {
                    return null;
                }
        
                methodCaller ??= new MethodCaller(classType);
                methodCaller.RegistMethod(name, method);
            }

            return methodCaller;
        }
    }
}
