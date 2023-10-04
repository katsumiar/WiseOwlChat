using System;
using System.Threading.Tasks;

namespace WiseOwlChat
{
    public interface IFunctionCalling
    {
        delegate string? CheckDelegate(string text);
        string FunctionName { get; }
        string Description { get; }
        object Function { get; }
        Task<string> ExecAsync(Action<string> addContent, string param, Func<string, bool> confirm);
    }
}
