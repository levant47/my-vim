public static class Util
{
    public static string Join(this IEnumerable<string> source, string separator) => string.Join(separator, source);

    public static void Panic(string message)
    {
        Console.WriteLine(message);
        Environment.Exit(1);
    }
}
