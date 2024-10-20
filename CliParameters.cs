public class CliParameters
{
    public bool JustTests;
    public string? InputPath;

    public static CliParameters Parse(string[] source)
    {
        var result = new CliParameters();
        foreach (var item in source)
        {
            if (item == "--just-tests")
            {
                if (result.JustTests) { Usage(); }
                result.JustTests = true;
            }
            else
            {
                if (result.InputPath != "") { Usage(); }
                result.InputPath = item;
            }
        }
        return result;
    }

    private static void Usage() { Panic($"Usage: {Environment.GetCommandLineArgs()[0]} [<input path>] [--just-tests]"); }
}
