using System.Collections.Generic;
using System.Text;

interface IModification
{
    string[] PreInputs(string moduleName);
    string[] PostOutputs(string moduleName);
    string[] PreCodeLines(string moduleName);
    string[] BeforeEndModule(string moduleName);

    bool DropWire(string moduleName, string wireName);

    void ModulePreInputs(string moduleName, Module.Code c, ref StringBuilder b);

    void RegisterFunctions(Module module);

    Module.Code ReplaceCodeLine(string moduleName, Module.Code cl);

    HashSet<string> ExcludeModules { get; }
    HashSet<string> BidirectionalDrivers { get; }
}