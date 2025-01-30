public static class VimTests
{
    private class TestFailedException(string message) : Exception(message);

    private record ShortVimTest(string InitialBuffer, string Commands, string ExpectedBuffer);

    private record ShortVimCursorTest(string InitialBuffer, string Commands, int ExpectedCursorX, int ExpectedCursorY);

    private record TestResult(string Name, bool Success, string ErrorMessage = "");

    private static Vim vim = null!;
    private const string DefaultInitialBuffer = """
    var x = 10;
    var y = 20;
    var z = 30;
    """;

    public static void RunAllTests()
    {
        var testFunctions = typeof(VimTests).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Test"))
            .ToList();
        var tests = new List<TestResult>();
        foreach (var test in testFunctions)
        {
            vim = new(DefaultInitialBuffer);
            try
            {
                test.Invoke(obj: null, parameters: null);
                tests.Add(new(test.Name, Success: true));
            }
            catch (TargetInvocationException invocationException)
            {
                var exception = invocationException.InnerException!;
                tests.Add(new(test.Name, Success: false, exception.Message));
            }
        }

        RunShortTest(tests, "", "i 'Hello, world!'", "Hello, world!");
        RunShortTest(tests, "", "i 'hello' escape i 'hello'", "hellhelloo");
        RunShortTest(tests, "", "i enter", "\n");
        RunShortTest(tests, "ab", "right i enter", "a\nb");
        RunShortTest(tests, "abc\ndef\nghi", "j d d u", "abc\ndef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j d d u ^r", "abc\ndef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j i delete delete delete escape u", "abc\ndef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j i delete delete delete escape u ^r", "abc\n\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j i backspace backspace backspace escape u", "abc\ndef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j i backspace backspace backspace escape u ^r", "adef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j j A enter 'jkl' escape u", "abc\ndef\nghi");
        RunShortTest(tests, "abc\ndef\nghi", "j j A enter 'jkl' escape u ^r", "abc\ndef\nghi\njkl");
        RunShortTest(tests, "abc\n\nghi", "j a backspace backspace backspace backspace delete delete delete delete 'def' escape u", "abc\n\nghi");
        RunShortTest(tests, "abc\n\nghi", "j a backspace backspace backspace backspace delete delete delete delete 'def' escape u ^r", "def");
        RunShortTest(tests, "abc", "x u" ,"abc");
        RunShortTest(tests, "abc", "x u ^r" ,"bc");
        RunShortTest(tests, "abc", "x x u" ,"bc");
        RunShortTest(tests, "abc", "x x u u" ,"abc");
        RunShortTest(tests, "", "i 'hello' escape period", "hellhelloo");
        RunShortTest(tests, "", "a 'hello' escape period", "hellohello");
        RunShortTest(tests, "abc", "i backspace", "abc");
        RunShortTest(tests, "abc", "l i backspace", "bc");
        RunShortTest(tests, "abc\ndef", "j i backspace", "abcdef");
        RunShortTest(tests, "abc\ndef", "A ' hello' escape j 0 period u u", "abc\ndef");
        RunShortTest(tests, "abc\ndef", "A ' hello' escape j 0 period u u ^r ^r", "abc hello\ndef hello");
        RunShortTest(tests, "var x = 42;", "f 'x' null D", "var ");
        RunShortTest(tests, "var x = 42;", "$ D", "var x = 42");
        RunShortTest(tests, "var x = 42;", "f 'x' D u", "var x = 42;");

        RunShortCursorTest(tests, "a\nb", "j", 0, 1);
        RunShortCursorTest(tests, "a\nb", "j j", 0, 1);
        RunShortCursorTest(tests, "a\nb", "k", 0, 0);
        RunShortCursorTest(tests, "a\nb", "j k", 0, 0);
        RunShortCursorTest(tests, "a\nb\nc", "j j k", 0, 1);
        RunShortCursorTest(tests, "ab", "h", 0, 0);
        RunShortCursorTest(tests, "ab", "l h", 0, 0);
        RunShortCursorTest(tests, "abc", "l l h", 1, 0);
        RunShortCursorTest(tests, "ab", "l", 1, 0);
        RunShortCursorTest(tests, "abc", "$ l", 2, 0);
        RunShortCursorTest(tests, "abc", "l 0", 0, 0);
        RunShortCursorTest(tests, "abc", "$ 0", 0, 0);
        RunShortCursorTest(tests, "abc", "$", 2, 0);
        RunShortCursorTest(tests, "ab\nc\ndef", "$ j", 0, 1);
        RunShortCursorTest(tests, "ab\nc\ndef", "$ j j", 2, 2);
        RunShortCursorTest(tests, "\nabc", "$ j", 2, 1);
        RunShortCursorTest(tests, "abc\n\ndef", "j $ j", 2, 2);
        RunShortCursorTest(tests, "a\nbcd", "$ l j", 2, 1);
        RunShortCursorTest(tests, "abc\n\ndef", "l j j", 1, 2);
        RunShortCursorTest(tests, "abc\n\ndefghi", "A right right right escape j j", 2, 2);
        RunShortCursorTest(tests, "abc", "a", 1, 0);
        RunShortCursorTest(tests, "abc", "$ a", 3, 0);
        RunShortCursorTest(tests, "abc", "A", 3, 0);
        RunShortCursorTest(tests, "", "i 'hello' escape", 4, 0);
        RunShortCursorTest(tests, "abc\ndef", "j i backspace", 3, 0);
        RunShortCursorTest(tests, "ab", "a enter", 0, 1);
        RunShortCursorTest(tests, "abc\ndef", "j $ g g", 0, 0);
        RunShortCursorTest(tests, "abcdef\n\nghijkl", "l l 0 j j", 0, 2);
        RunShortCursorTest(tests, "abc\ndefghi", "A 'abc' escape j", 5, 1);
        RunShortCursorTest(tests, "hello", "$ i escape i escape", 2, 0);
        RunShortCursorTest(tests, "ab", "a escape", 0, 0);
        RunShortCursorTest(tests, "var x = 42;", "f ';'", 10, 0);
        RunShortCursorTest(tests, "var x = 42;", "$ F 'r'", 2, 0);
        RunShortCursorTest(tests, "var x = 42;", "w", 4, 0);

        if (tests.All(test => test.Success)) { Console.WriteLine("All tests passed!"); }
        else
        {
            foreach (var test in tests)
            {
                if (test.Success) { continue; }
                Console.WriteLine($"Test '{test.Name}' failed: {test.ErrorMessage}");
            }
            Console.WriteLine();

            var longestTestNameLength = Math.Min(40, tests.Max(test => test.Name.Length));
            foreach (var test in tests)
            {
                Console.WriteLine($"{(test.Name + ":").PadRight(longestTestNameLength + 1)} {(test.Success ? "✓" : "×")}");
            }
            Console.WriteLine($"Failed: {tests.Count(test => !test.Success)}, passed: {tests.Count(test => test.Success)}, total: {tests.Count}");
        }
    }

    private static void RunShortTest(List<TestResult> tests, string initialBuffer, string commands, string expectedBuffer)
    {
        var vim = new Vim(initialBuffer);
        var testName = commands;
        try
        {
            foreach (var input in ParseShortTestInput(commands))
            {
                vim.Process(input);
            }
            tests.Add(new(
                testName,
                Success: vim.GetBuffer() == expectedBuffer,
                $"Expected '{expectedBuffer.Replace("\n", "\\n")}', got '{vim.GetBuffer().Replace("\n", "\\n")}'"
            ));
        }
        catch (Exception exception) { tests.Add(new(testName, Success: false, exception.Message)); }
    }

    private static void RunShortCursorTest(List<TestResult> tests, string initialBuffer, string commands, int expectedCursorX, int expectedCursorY)
    {
        var vim = new Vim(initialBuffer);
        var testName = commands;
        try
        {
            foreach (var input in ParseShortTestInput(commands))
            {
                vim.Process(input);
            }
            tests.Add(new(
                testName,
                Success: vim.CursorX == expectedCursorX && vim.CursorY == expectedCursorY,
                $"Expected X = {expectedCursorX}, Y = {expectedCursorY}, got X = {vim.CursorX}, Y = {vim.CursorY}"
            ));
        }
        catch (Exception exception) { tests.Add(new(testName, Success: false, exception.Message)); }
    }

    private static void Assert(object actual, object expected)
    {
        if (!actual.Equals(expected)) { throw new($"Expected '{expected}', got '{actual}'"); }
    }

    private static void Process(this Vim vim, KeyboardKey key = KeyboardKey.Null, bool isShift = false, bool isControl = false, string input = "")
    {
        vim.Process(new() { Key = key, Modifier = isShift ? VimInputModifier.Shift : isControl ? VimInputModifier.Control : VimInputModifier.None, Text = input });
    }

    private static string GetBuffer(this Vim vim) => vim.Lines.Join("\n");

    private static List<VimInput> ParseShortTestInput(string source)
    {
        var result = new List<VimInput>();
        var i = 0;
        var currentEntry = new VimInput();
        while (i != source.Length)
        {
            if (source[i] == ' ') { i++; }
            else if (source[i] == '\'')
            {
                i++;
                var text = new StringBuilder();
                while (i != source.Length && source[i] != '\'') { text.Append(source[i]); i++; }
                if (i != source.Length) { i++; }
                currentEntry.Text = text.ToString();
                result.Add(currentEntry);
                currentEntry = new();
            }
            else
            {
                var wordBuilder = new StringBuilder();
                while (i != source.Length && source[i] != ' ') { wordBuilder.Append(source[i]); i++; }
                var word = wordBuilder.ToString();
                if (word[0] == '^')
                {
                    word = word[1..];
                    currentEntry.Modifier = VimInputModifier.Control;
                }
                if (word == "$")
                {
                    currentEntry.Key = KeyboardKey.Four;
                    currentEntry.Modifier = VimInputModifier.Shift;
                }
                else if (word == "0") { currentEntry.Key = KeyboardKey.Zero; }
                else if (word == "1") { currentEntry.Key = KeyboardKey.One; }
                else if (word == "2") { currentEntry.Key = KeyboardKey.Two; }
                else if (word == "3") { currentEntry.Key = KeyboardKey.Three; }
                else if (word == "4") { currentEntry.Key = KeyboardKey.Four; }
                else if (word == "5") { currentEntry.Key = KeyboardKey.Five; }
                else if (word == "6") { currentEntry.Key = KeyboardKey.Six; }
                else if (word == "7") { currentEntry.Key = KeyboardKey.Seven; }
                else if (word == "8") { currentEntry.Key = KeyboardKey.Eight; }
                else if (word == "9") { currentEntry.Key = KeyboardKey.Nine; }
                else
                {
                    currentEntry.Key = Enum.Parse<KeyboardKey>(word, ignoreCase: true);
                    if (word.Length == 1 && char.IsUpper(word[0])) { currentEntry.Modifier = VimInputModifier.Shift; }
                }
                result.Add(currentEntry);
                currentEntry = new();
            }
        }
        return result;
    }
}
