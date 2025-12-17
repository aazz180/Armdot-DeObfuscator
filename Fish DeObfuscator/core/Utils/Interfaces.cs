using System.Collections.Generic;
using dnlib.DotNet;

namespace Fish_DeObfuscator.core.Utils
{
    public enum DeobfuscationType
    {
        DotNet,
        PowerShell,
        Lua,
    }

    public interface IContext
    {
        bool IsInitialized();
        void SaveContext(bool log = true);
        IOptions Options { get; }
        ModuleDefMD ModuleDefinition { get; set; }
    }

    public interface IOptions
    {
        string AssemblyPath { get; set; }
        string AssemblyName { get; set; }
        string AssemblyExtension { get; set; }
        string AssemblyOutput { get; set; }
        string AssemblyDirectory { get; set; }
        List<IStage> Stages { get; }
    }

    public interface IStage
    {
        void obf(IContext context);
    }
}
