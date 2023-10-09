using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WiseOwlChat
{
    class FunctionCallingMediator : IFunctionCalling
    {
        MethodCaller methodCaller;

        public FunctionCallingMediator(MethodCaller methodCaller)
        {
            this.methodCaller = methodCaller;
        }

        private object? call(object[] args, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null)
            {
                return null;
            }

            return this.methodCaller.CallFunction(propertyName, args);
        }

        public string FunctionName
        {
            get
            {
                var result = call(new object[] { });
                if (result == null)
                {
                    return string.Empty;
                }

                return (string)result;
            }
        }

        public string Description
        {
            get
            {
                var result = call(new object[] { });
                if (result == null)
                {
                    return string.Empty;
                }

                return (string)result;
            }
        }

        public object Function
        {
            get
            {
                var result = call(new object[] { });
                if (result == null)
                {
                    return string.Empty;
                }

                return result;
            }
        }

        public Task<string> ExecAsync(Action<string> addContent, string param, Func<string, bool> confirm)
        {
            var result = call(new object[] { addContent, param, confirm });
            if (result == null)
            {
                return new Task<string>(() => "");
            }

            return (Task<string>)result;
        }
    }
}
