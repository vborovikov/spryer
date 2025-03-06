namespace Spryer.CodeGen;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: SpryerGen <script file path>");
            return 1;
        }

        var scriptFileName = Path.GetFullPath(args[0]);
        var scriptMapLoader = new DbScriptMap.Loader
        {
            FileName = scriptFileName
        };
        if (!scriptMapLoader.TryLoadScripts())
        {
            Console.Error.WriteLine($"No scripts found in '{scriptFileName}'");
            return 2;
        }

        var scriptMap = scriptMapLoader.GetScriptMap();
        Console.Error.WriteLine($"{scriptMap.Count} scripts loaded from '{scriptMap.Source}'");

        return 0;
    }
}
