public enum VimInputModifier
{
    None,
    Shift,
    Control,
}

public class VimInput
{
    public KeyboardKey Key;
    public VimInputModifier Modifier;
    public string Text = "";
}
