public static class VimTests
{
    private class TestFailedException(string message) : Exception(message);

    private static Vim vim = null!;
    private const string DefaultInitialBuffer = """
    var x = 10;
    var y = 20;
    var z = 30;
    """;

    public static void RunAllTests()
    {
        var tests = typeof(VimTests).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Test"))
            .ToList();
        var passed = new List<string>();
        var failed = new List<string>();
        foreach (var test in tests)
        {
            vim = new(DefaultInitialBuffer);
            try
            {
                test.Invoke(obj: null, parameters: null);
                passed.Add(test.Name);
            }
            catch (TargetInvocationException invocationException)
            {
                var exception = invocationException.InnerException!;
                Console.WriteLine($"{test.Name} failed: {exception.Message}");
                failed.Add(test.Name);
            }
        }

        Console.WriteLine();
        if (failed.Count == 0) { Console.WriteLine("All tests passed!"); }
        else
        {
            var longestTestName = tests.Max(test => test.Name.Length);
            foreach (var test in tests)
            {
                Console.WriteLine($"{(test.Name + ":").PadRight(longestTestName + 1)} {(passed.Contains(test.Name) ? "✓" : "×")}");
            }
            Console.WriteLine($"Failed: {failed.Count}, passed: {passed.Count}, total: {failed.Count + passed.Count}");
        }
    }

    private static void TestJ()
    {
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, 0);
        Assert(vim.CursorY, 1);
    }

    private static void TestJBeyondLastLine()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, 0);
        Assert(vim.CursorY, vim.Lines.Count - 1);
    }

    private static void TestKOnFirstLine()
    {
        vim.Process(KeyboardKey.K);
        Assert(vim.CursorY, 0);
    }

    private static void TestJK()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.K);
        Assert(vim.CursorY, 0);
    }

    private static void TestJJK()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.K);
        Assert(vim.CursorY, 1);
    }

    private static void TestHAtLineStart()
    {
        vim.Process(KeyboardKey.H);
        Assert(vim.CursorX, 0);
    }

    private static void TestLH()
    {
        vim.Process(KeyboardKey.L);
        vim.Process(KeyboardKey.H);
        Assert(vim.CursorX, 0);
    }

    private static void TestLLH()
    {
        vim.Process(KeyboardKey.L);
        vim.Process(KeyboardKey.L);
        vim.Process(KeyboardKey.H);
        Assert(vim.CursorX, 1);
    }

    private static void TestL()
    {
        vim.Process(KeyboardKey.L);
        Assert(vim.CursorX, 1);
    }

    private static void TestShiftFourL()
    {
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.L);
        Assert(vim.CursorX, "var x = 10;".Length - 1);
    }

    private static void TestLZero()
    {
        vim.Process(KeyboardKey.L);
        vim.Process(KeyboardKey.Zero);
        Assert(vim.CursorX, 0);
    }

    private static void TestShiftFourZero()
    {
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.Zero);
        Assert(vim.CursorX, 0);
    }

    private static void TestShiftFour()
    {
        vim.Process(KeyboardKey.Four, isShift: true);
        Assert(vim.CursorX, "var x = 10;".Length - 1);
    }

    private static void TestShiftFourJ()
    {
        var vim = new Vim("""
        var x = 12;
        var y = 1;
        var z = 123;
        """);
        vim.Process(KeyboardKey.Four, isShift: true);
        Assert(vim.CursorX, "var x = 12;".Length - 1);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, "var y = 1;".Length - 1);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, "var z = 123;".Length - 1);
    }

    private static void TestShiftFourOnEmptyLineJ()
    {
        var vim = new Vim("""
        
        var x = 10;
        """);
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, "var x = 10;".Length - 1);
    }

    private static void TestJShiftFourOnEmptyLineJ()
    {
        var vim = new Vim("""
        var x = 10;
        
        var y = 20;
        """);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, "var y = 20;".Length - 1);
    }

    private static void TestShiftFourLJ()
    {
        var vim = new Vim("""
        var x = 1;
        var y = 12;
        """);
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.L);
        vim.Process(KeyboardKey.J);
        Assert(vim.CursorX, "var y = 12;".Length - 1);
    }

    private static void TestA()
    {
        vim.Process(KeyboardKey.A);
        Assert(vim.CursorX, 1);
    }

    private static void TestShiftFourA()
    {
        vim.Process(KeyboardKey.Four, isShift: true);
        vim.Process(KeyboardKey.A);
        Assert(vim.CursorX, "var x = 10;".Length);
    }

    private static void TestShiftA()
    {
        vim.Process(KeyboardKey.A, isShift: true);
        Assert(vim.CursorX, "var x = 10;".Length);
    }

    private static void TestIText()
    {
        var vim = new Vim("");
        vim.Process(KeyboardKey.I);
        vim.Process(input: "Hello, world!");
        Assert(vim.GetBuffer(), "Hello, world!");
    }

    private static void TestITextEscapeIText()
    {
        var vim = new Vim("");
        vim.Process(KeyboardKey.I);
        vim.Process(input: "hello");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.I);
        vim.Process(input: "hello");
        Assert(vim.GetBuffer(), "hellhelloo");
    }

    public static void TestDDUndo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.D);
        vim.Process(KeyboardKey.D);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, DefaultInitialBuffer);
    }

    public static void TestDDUndoRedo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.D);
        vim.Process(KeyboardKey.D);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        var z = 30;
        """);
    }
    public static void TestDeleteUndo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.I);
        foreach (var _ in "var y = 20;") { vim.Process(KeyboardKey.Delete); }
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, DefaultInitialBuffer);
    }

    public static void TestDeleteUndoRedo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.I);
        foreach (var _ in "var y = 20;") { vim.Process(KeyboardKey.Delete); }
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        
        var z = 30;
        """);
    }

    public static void TestBackspaceUndo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A, isShift: true);
        foreach (var _ in "var y = 20;") { vim.Process(KeyboardKey.Backspace); }
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, DefaultInitialBuffer);
    }

    public static void TestBackspaceUndoRedo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A, isShift: true);
        foreach (var _ in "var y = 20;") { vim.Process(KeyboardKey.Backspace); }
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        
        var z = 30;
        """);
    }

    public static void TestAddTextUndo()
    {
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A, isShift: true);
        vim.Process(KeyboardKey.Enter);
        vim.Process(input: "var z = 30;");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, DefaultInitialBuffer);
    }

    public static void TestAddTextUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        """;
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A, isShift: true);
        vim.Process(KeyboardKey.Enter);
        vim.Process(input: "var z = 30;");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        var y = 20;
        var z = 30;
        """);
    }

    public static void TestBackspaceAddTextDeleteUndo()
    {
        var originalBuffer = """
        var x = 10;
        
        var y = 20;
        """;
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A);
        foreach (var _ in "var x = 10;\n") { vim.Process(KeyboardKey.Backspace); }
        foreach (var _ in "\nvar y = 20;") { vim.Process(KeyboardKey.Delete); }
        vim.Process(input: "var z = 30;");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestBackspaceAddTextDeleteUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        
        var y = 20;
        """;
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.J);
        vim.Process(KeyboardKey.A);
        foreach (var _ in "var x = 10;\n") { vim.Process(KeyboardKey.Backspace); }
        foreach (var _ in "\nvar y = 20;") { vim.Process(KeyboardKey.Delete); }
        vim.Process(input: "var z = 30;");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var z = 30;
        """);
    }

    public static void TestXUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestXUndoRedo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.R, isControl: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, "ar x = 10;");
    }

    public static void TestMultiXUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, "ar x = 10;");
    }

    public static void TestMultiXMultiUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.X);
        vim.Process(KeyboardKey.U);
        vim.Process(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestCursor()
    {
        var vim = new Vim("");
        vim.Process(KeyboardKey.I);
        vim.Process(input: "hello");
        vim.Process(KeyboardKey.Escape);
        Assert(vim.CursorX, "hello".Length - 1);
    }

    public static void TestIRepeat()
    {
        var vim = new Vim("");
        vim.Process(KeyboardKey.I);
        vim.Process(input: "hello");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.Period);
        Assert(vim.Lines[0], "hellhelloo");
    }

    public static void TestARepeat()
    {
        var vim = new Vim("");
        vim.Process(KeyboardKey.A);
        vim.Process(input: "hello");
        vim.Process(KeyboardKey.Escape);
        vim.Process(KeyboardKey.Period);
        Assert(vim.Lines[0], "hellohello");
    }

    private static void Assert(object actual, object expected)
    {
        if (!actual.Equals(expected)) { throw new TestFailedException($"Expected '{expected}', got '{actual}'"); }
    }

    private static string GetBuffer(this Vim vim) => vim.Lines.Join("\n");
}
