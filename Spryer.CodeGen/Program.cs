namespace Spryer.CodeGen;

using Scripting;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: SpryerCodeGen <script file path> <code file path>");
            return 1;
        }

        try
        {
            var scriptFilePath = Path.GetFullPath(args[0]);
            var scriptMapLoader = new DbScriptMap.Loader
            {
                FileName = scriptFilePath,
                CollectsPragmas = true,
            };
            if (!scriptMapLoader.TryLoadScripts())
            {
                Console.Error.WriteLine($"No scripts found in '{scriptFilePath}'");
                return 2;
            }

            var scriptMap = scriptMapLoader.GetScriptMap();
            Console.Error.WriteLine($"{scriptMap.Count} scripts loaded from '{scriptMap.Source}'");

            var scriptClass = new ScriptClass(scriptMap);
            if (args.Length > 1)
            {
                scriptClass.Name = Path.GetFileName(args[1]);
            }
            var code = new CodeBuilder();
            scriptClass.Generate(code);

            var codeFilePath = args.Length > 1 ? Path.GetFullPath(args[1]) :
                Path.Combine(Path.GetDirectoryName(args[0]) ?? ".", Path.ChangeExtension(scriptClass.GetClassName(), ".cs"));
            await File.WriteAllTextAsync(codeFilePath, code.ToString());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 3;
        }

        return 0;
    }
}
