using System.Reflection;

public static class VimTests
{
    private class TestFailedException(string message) : Exception(message);

    public static void RunAllTests()
    {
        VimInput.IsTestMode = true;

        var allTestsPassed = true;
        var tests = typeof(VimTests).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Test"))
            .ToList();
        foreach (var test in tests)
        {
            try { test.Invoke(obj: null, parameters: null); }
            catch (TargetInvocationException invocationException)
            {
                var exception = invocationException.InnerException!;
                Console.WriteLine($"Test {test.Name} failed: {exception.Message}");
                allTestsPassed = false;
            }
        }

        if (allTestsPassed)
        {
            Console.WriteLine("All tests passed!");
        }

        VimInput.IsTestMode = false;
    }

    private static void TestJ()
    {
        var vim = new Vim("""
        var x = 10;
        var y = 10;
        """);
        VimInput.TestPressedKey = KeyboardKey.J;
        vim.ProcessEvents();
        Assert(vim.CursorX, 0);
        Assert(vim.CursorY, 1);
    }

    public static void TestDDUndo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.D);
        vim.Simulate(KeyboardKey.D);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestDDUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.D);
        vim.Simulate(KeyboardKey.D);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        var z = 30;
        """);
    }

    public static void TestDeleteUndo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.I);
        foreach (var _ in "var y = 20;") { vim.Simulate(KeyboardKey.Delete); }
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestDeleteUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.I);
        foreach (var _ in "var y = 20;") { vim.Simulate(KeyboardKey.Delete); }
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        
        var z = 30;
        """);
    }

    public static void TestBackspaceUndo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A, shift: true);
        foreach (var _ in "var y = 20;") { vim.Simulate(KeyboardKey.Backspace); }
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestBackspaceUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        var z = 30;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A, shift: true);
        foreach (var _ in "var y = 20;") { vim.Simulate(KeyboardKey.Backspace); }
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var x = 10;
        
        var z = 30;
        """);
    }

    public static void TestAddTextUndo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A, shift: true);
        vim.Simulate(KeyboardKey.Enter);
        vim.Simulate("var z = 30;");
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestAddTextUndoRedo()
    {
        var originalBuffer = """
        var x = 10;
        var y = 20;
        """;
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A, shift: true);
        vim.Simulate(KeyboardKey.Enter);
        vim.Simulate("var z = 30;");
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

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

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A);
        foreach (var _ in "var x = 10;\n") { vim.Simulate(KeyboardKey.Backspace); }
        foreach (var _ in "\nvar y = 20;") { vim.Simulate(KeyboardKey.Delete); }
        vim.Simulate("var z = 30;");
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);

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

        vim.Simulate(KeyboardKey.J);
        vim.Simulate(KeyboardKey.A);
        foreach (var _ in "var x = 10;\n") { vim.Simulate(KeyboardKey.Backspace); }
        foreach (var _ in "\nvar y = 20;") { vim.Simulate(KeyboardKey.Delete); }
        vim.Simulate("var z = 30;");
        vim.Simulate(KeyboardKey.Escape);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, """
        var z = 30;
        """);
    }

    public static void TestXUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, originalBuffer);
    }

    public static void TestXUndoRedo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.R, control: true);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, "ar x = 10;");
    }

    public static void TestMultiXUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, "ar x = 10;");
    }

    public static void TestMultiXMultiUndo()
    {
        var originalBuffer = "var x = 10;";
        var vim = new Vim(originalBuffer);

        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.X);
        vim.Simulate(KeyboardKey.U);
        vim.Simulate(KeyboardKey.U);

        var newBuffer = string.Join(Environment.NewLine, vim.Lines);
        Assert(newBuffer, "var x = 10;");
    }

    public static void TestCursor()
    {
        var vim = new Vim("");
        vim.Simulate(KeyboardKey.I);
        vim.Simulate("hello");
        vim.Simulate(KeyboardKey.Escape);
        Assert(vim.CursorX, "hello".Length - 1);
    }

    public static void TestIRepeat()
    {
        var vim = new Vim("");
        vim.Simulate(KeyboardKey.I).Simulate("hello").Simulate(KeyboardKey.Escape).Simulate(KeyboardKey.Period);
        Assert(vim.Lines[0], "hellhelloo");
    }

    public static void TestARepeat()
    {
        var vim = new Vim("");
        vim.Simulate(KeyboardKey.A).Simulate("hello").Simulate(KeyboardKey.Escape).Simulate(KeyboardKey.Period);
        Assert(vim.Lines[0], "hellohello");
    }

    private static Vim Simulate(this Vim vim, KeyboardKey key, bool shift = false, bool control = false)
    {
        VimInput.TestPressedKey = key;
        VimInput.TestShift = shift;
        VimInput.TestControl = control;
        vim.ProcessEvents();
        VimInput.ResetTestValues();
        return vim;
    }

    private static Vim Simulate(this Vim vim, string text)
    {
        VimInput.TestText = text;
        vim.ProcessEvents();
        VimInput.ResetTestValues();
        return vim;
    }

    private static void Assert(object actual, object expected)
    {
        if (!actual.Equals(expected)) { throw new TestFailedException($"Expected '{expected}', got '{actual}'"); }
    }
}
