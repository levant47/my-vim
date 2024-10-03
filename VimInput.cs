public static class VimInput
{
    public static bool IsTestMode = false;
    public static KeyboardKey TestPressedKey;
    public static bool TestShift = false;
    public static bool TestControl = false;
    public static string TestText = "";
    public static int TestTextI = 0;

    public static bool Pressed(KeyboardKey key) => !IsTestMode
        ? Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key)
        : TestPressedKey == key;

    public static int GetPressed() => !IsTestMode ? Raylib.GetKeyPressed() : (int)TestPressedKey;

    public static int GetCharPressed()
    {
        if (!IsTestMode) { return Raylib.GetCharPressed(); }

        if (TestTextI == TestText.Length) { return 0; }
        var result = char.ConvertToUtf32(TestText, TestTextI);
        TestTextI++;
        return result;
    }

    public static bool PressedRepeat(KeyboardKey key) => !IsTestMode && Raylib.IsKeyPressedRepeat(key);

    public static bool IsShift() => !IsTestMode
        ? Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)
        : TestShift;

    public static bool IsControl() => !IsTestMode
        ? Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)
        : TestControl;

    public static void ResetTestValues()
    {
        TestPressedKey = default;
        TestShift = false;
        TestControl = false;
        TestText = "";
        TestTextI = 0;
    }
}
