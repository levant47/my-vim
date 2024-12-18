﻿public static class VimTests
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

        foreach (var shortTest in _shortTests)
        {
            var vim = new Vim(shortTest.InitialBuffer);
            var testName = $"{{{shortTest.Commands}}}";
            try
            {
                foreach (var input in ParseShortTestInput(shortTest.Commands))
                {
                    vim.Process(input);
                }
                tests.Add(new(
                    testName,
                    Success: vim.GetBuffer() == shortTest.ExpectedBuffer,
                    $"Expected '{shortTest.ExpectedBuffer.Replace("\n", "\\n")}', got '{vim.GetBuffer().Replace("\n", "\\n")}'"
                ));
            }
            catch (Exception exception) { tests.Add(new(testName, Success: false, exception.Message)); }
        }

        foreach (var shortTest in _shortCursorTests)
        {
            var vim = new Vim(shortTest.InitialBuffer);
            var testName = $"{{{shortTest.Commands}}}";
            try
            {
                foreach (var input in ParseShortTestInput(shortTest.Commands))
                {
                    vim.Process(input);
                }
                tests.Add(new(
                    testName,
                    Success: vim.CursorX == shortTest.ExpectedCursorX && vim.CursorY == shortTest.ExpectedCursorY,
                    $"Expected X = {shortTest.ExpectedCursorX}, Y = {shortTest.ExpectedCursorY}, got X = {vim.CursorX}, Y = {vim.CursorY}"
                ));
            }
            catch (Exception exception) { tests.Add(new(testName, Success: false, exception.Message)); }
        }

        Console.WriteLine();
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

    private static ShortVimCursorTest[] _shortCursorTests =
    [
        new("a\nb", "j", 0, 1),
        new("a\nb", "j j", 0, 1),
        new("a\nb", "k", 0, 0),
        new("a\nb", "j k", 0, 0),
        new("a\nb\nc", "j j k", 0, 1),
        new("ab", "h", 0, 0),
        new("ab", "l h", 0, 0),
        new("abc", "l l h", 1, 0),
        new("ab", "l", 1, 0),
        new("abc", "$ l", 2, 0),
        new("abc", "l 0", 0, 0),
        new("abc", "$ 0", 0, 0),
        new("abc", "$", 2, 0),
        new("ab\nc\ndef", "$ j", 0, 0),
        new("ab\nc\ndef", "$ j j", 2, 0),
        new("\nabc", "$ j", 2, 0),
        new("abc\n\ndef", "j $ j", 2, 0),
        new("a\nbcd", "$ l j", 2, 0),
        new("abc", "a", 1, 0),
        new("abc", "$ a", 3, 0),
        new("abc", "A", 3, 0),
        new("hello", "$ i escape i escape", 2, 0),
        new("", "i 'hello' escape", 4, 0),
        new("abc\ndef", "j i backspace", 3, 0),
        new("ab", "a enter", 0, 1),
        new("abc\ndef", "j $ g g", 0, 0),
    ];

    private static ShortVimTest[] _shortTests =
    [
        new("", "i 'Hello, world!'", "Hello, world!"),
        new("", "i 'hello' escape i 'hello'", "hellhelloo"),
        new("", "i enter", "\n"),
        new("ab", "right i enter", "a\nb"),
        new("abc\ndef\nghi", "j d d u", "abc\ndef\nghi"),
        new("abc\ndef\nghi", "j d d u ^r", "abc\ndef\nghi"),
        new("abc\ndef\nghi", "j i delete delete delete escape u", "abc\ndef\nghi"),
        new("abc\ndef\nghi", "j i delete delete delete escape u ^r", "abc\n\nghi"),
        new("abc\ndef\nghi", "j i backspace backspace backspace escape u", "abc\ndef\nghi"),
        new("abc\ndef\nghi", "j i backspace backspace backspace escape u ^r", "adef\nghi"),
        new("abc\ndef\nghi", "j j A enter 'jkl' escape u", "abc\ndef\nghi"),
        new("abc\ndef\nghi", "j j A enter 'jkl' escape u ^r", "abc\ndef\nghi\njkl"),
        new("abc\n\nghi", "j a backspace backspace backspace backspace delete delete delete delete 'def' escape u", "abc\n\nghi"),
        new("abc\n\nghi", "j a backspace backspace backspace backspace delete delete delete delete 'def' escape u ^r", "def"),
        new("abc", "x u" ,"abc"),
        new("abc", "x u ^r" ,"bc"),
        new("abc", "x x u" ,"bc"),
        new("abc", "x x u u" ,"abc"),
        new("", "i 'hello' escape period", "hellhelloo"),
        new("", "a 'hello' escape period", "hellohello"),
        new("abc", "i backspace", "abc"),
        new("abc", "l i backspace", "bc"),
        new("abc\ndef", "j i backspace", "abcdef"),
        new("abc\ndef", "A ' hello' escape j 0 period u u", "abc\ndef"),
        new("abc\ndef", "A ' hello' escape j 0 period u u ^r ^r", "abc hello\ndef hello"),
    ];

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
