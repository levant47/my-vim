public class Vim
{
    public enum Mode
    {
        Normal,
        Insert,
    }

    public const int FontHeight = 32;
    public const int FontWidth = 14;

    public List<string> Lines;
    public int CursorX;
    public int CursorY;
    public Mode _mode = Mode.Normal;
    public bool IsCursorGluedToEndOfLine;

    // rendering state
    public Font _font;

    public Vim(string buffer) => Lines = buffer.Replace("\r", "").Split('\n').ToList();

    public void InitializeForRendering() => _font = Raylib.LoadFont("assets\\RobotoMono.ttf");

    public void Render()
    {
        var xPadding = 10;
        var yPadding = 10;
        var x = xPadding;
        var y = yPadding;

        var input = "";
        while (true)
        {
            var c = Raylib.GetCharPressed();
            if (c == 0) { break; }
            input += char.ConvertFromUtf32(c);
        }
        Process(new() { Text = input });

        var pressedKeys = Enum.GetValues<KeyboardKey>().Where(key => Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key)).ToList();
        foreach (var pressedKey in pressedKeys)
        {
            Process(new()
            {
                Key = pressedKey,
                Modifier = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.LeftShift)
                    ? VimInputModifier.Shift
                    : Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)
                    ? VimInputModifier.Control
                    : VimInputModifier.None
            });
        }

        // text
        foreach (var line in Lines)
        {
            Raylib.DrawTextEx(_font, line, new(x, y), FontHeight, 0, Color.Black);
            y += FontHeight;
        }

        // cursor
        var upperBound = Lines[CursorY].Length - 1;
        if (_mode == Mode.Insert) { upperBound++; }
        var displayCursorX = Math.Min(CursorX, Math.Max(0, upperBound));
        var displayCursorY = CursorY;
        var cursorPixelY = displayCursorY * FontHeight + yPadding;
        var cursorPrefixText = Lines[displayCursorY][..displayCursorX];
        var cursorPixelX = cursorPrefixText.Length * FontWidth + xPadding;
        if (_mode == Mode.Normal)
        {
            var characterUnderCursor = Lines[displayCursorY].ElementAtOrDefault(displayCursorX);
            Raylib.DrawRectangle(cursorPixelX, cursorPixelY, FontWidth, FontHeight, Color.Black);
            if (characterUnderCursor != default)
            {
                Raylib.DrawTextEx(_font, characterUnderCursor.ToString(), new(cursorPixelX, cursorPixelY), FontHeight, 0, Color.White);
            }
        }
        else if (_mode == Mode.Insert)
        {
            Raylib.DrawLine(cursorPixelX, cursorPixelY, cursorPixelX, cursorPixelY + FontHeight, Color.Black);
        }
    }

    public void Process(VimInput input)
    {
        Command? command;
        if (_mode == Mode.Insert && input.Text != "")
        {
            command = new()
            {
                Type = CommandType.AppendText | CommandType.Navigation,
                Text = input.Text,
                NavigationType = NavigationCommandType.Relative,
                DeltaX = input.Text.Length,
            };
        }
        else
        {
            command = (_mode, input.Key, input.Modifier) switch
            {
                (Mode.Normal, KeyboardKey.J, VimInputModifier.None) or (_, KeyboardKey.Down, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation, NavigationType = NavigationCommandType.Relative, DeltaY = +1 },
                (Mode.Normal, KeyboardKey.K, VimInputModifier.None) or (_, KeyboardKey.Up, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation, NavigationType = NavigationCommandType.Relative, DeltaY = -1 },
                (Mode.Normal, KeyboardKey.H, VimInputModifier.None) or (_, KeyboardKey.Left, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation, NavigationType = NavigationCommandType.Relative, DeltaX = -1 },
                (Mode.Normal, KeyboardKey.L, VimInputModifier.None) or (_, KeyboardKey.Right, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation, NavigationType = NavigationCommandType.Relative, DeltaX = +1 },
                (Mode.Normal, KeyboardKey.Zero, VimInputModifier.None) or (_, KeyboardKey.Home, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation, NavigationType = NavigationCommandType.AbsoluteOnLine, AbsoluteOnLineNavigationType = AbsoluteOnLineNavigationCommandType.ToStart },
                (Mode.Normal, KeyboardKey.Four, VimInputModifier.Shift) or (_, KeyboardKey.End, VimInputModifier.None)
                    => new() { Type = CommandType.Navigation | CommandType.GlueCursorToEndOfLine, NavigationType = NavigationCommandType.AbsoluteOnLine, AbsoluteOnLineNavigationType = AbsoluteOnLineNavigationCommandType.ToEnd },
                (Mode.Normal, KeyboardKey.I, VimInputModifier.None)
                    => new() { Type = CommandType.ChangeMode, TargetMode = Mode.Insert },
                (Mode.Normal, KeyboardKey.A, VimInputModifier.None)
                    => new() { Type = CommandType.ChangeMode | CommandType.Navigation, TargetMode = Mode.Insert, NavigationType = NavigationCommandType.Relative, DeltaX = 1 },
                (Mode.Normal, KeyboardKey.A, VimInputModifier.Shift)
                    => new() { Type = CommandType.ChangeMode | CommandType.Navigation, TargetMode = Mode.Insert, NavigationType = NavigationCommandType.AbsoluteOnLine, AbsoluteOnLineNavigationType = AbsoluteOnLineNavigationCommandType.ToEnd },
                (Mode.Insert, KeyboardKey.Escape, VimInputModifier.None)
                    => new() { Type = CommandType.ChangeMode | CommandType.Navigation, TargetMode = Mode.Normal, NavigationType = NavigationCommandType.Relative, DeltaX = -1 },
                (Mode.Insert, KeyboardKey.Enter, VimInputModifier.None)
                    => new() { Type = CommandType.InsertNewLine | CommandType.Navigation, NavigationType = NavigationCommandType.Relative, DeltaY = 1 },
                _ => null,
            };
        }
        if (command != null) { Execute(command); }
    }

    [Flags]
    public enum CommandType
    {
        Navigation = 1 << 0,
        ChangeMode = 1 << 1,
        GlueCursorToEndOfLine = 1 << 2,
        AppendText = 1 << 3,
        InsertNewLine = 1 << 4,
    }

    public enum NavigationCommandType
    {
        Relative,
        AbsoluteOnLine,
    }

    public enum AbsoluteOnLineNavigationCommandType
    {
        ToStart,
        ToEnd,
    }

    public class Command
    {
        public CommandType Type;

        // Navigation
        public NavigationCommandType NavigationType;
        // Relative
        public int DeltaX;
        public int DeltaY;
        // AbsoluteOnLine
        public AbsoluteOnLineNavigationCommandType AbsoluteOnLineNavigationType;

        // ChangeMode
        public Mode TargetMode;

        // AppendText
        public string Text = "";
    }

    public void Execute(Command command)
    {
        var resetCursorGlue = true;
        if (command.Type.HasFlag(CommandType.ChangeMode)) { _mode = command.TargetMode; }
        if (command.Type.HasFlag(CommandType.AppendText)) { Lines[CursorY] = Lines[CursorY].Insert(CursorX, command.Text); }
        if (command.Type.HasFlag(CommandType.InsertNewLine))
        {
            var left = Lines[CursorY][..CursorX];
            var right = Lines[CursorY][CursorX..];
            Lines[CursorY] = left;
            Lines.Insert(CursorY + 1, right);
        }
        if (command.Type.HasFlag(CommandType.Navigation))
        {
            switch (command.NavigationType)
            {
                case NavigationCommandType.Relative:
                {
                    CursorY = Math.Clamp(CursorY + command.DeltaY, 0, Lines.Count - 1);
                    if (_mode == Mode.Normal && command.DeltaX >= 0 && IsCursorGluedToEndOfLine)
                    {
                        resetCursorGlue = false;
                        CursorX = Math.Max(0, Lines[CursorY].Length - 1);
                    }
                    else if (Lines[CursorY] == "") { CursorX = 0; }
                    else if (_mode == Mode.Normal) { CursorX = Math.Clamp(CursorX + command.DeltaX, 0, Lines[CursorY].Length - 1); }
                    else if (_mode == Mode.Insert) { CursorX = Math.Clamp(CursorX + command.DeltaX, 0, Lines[CursorY].Length); }
                    break;
                }
                case NavigationCommandType.AbsoluteOnLine:
                {
                    switch (command.AbsoluteOnLineNavigationType)
                    {
                        case AbsoluteOnLineNavigationCommandType.ToStart:
                        {
                            CursorX = 0;
                            break;
                        }
                        case AbsoluteOnLineNavigationCommandType.ToEnd:
                        {
                            if (Lines[CursorY] == "") { CursorX = 0; }
                            else if (_mode == Mode.Normal) { CursorX = Lines[CursorY].Length - 1; }
                            else if (_mode == Mode.Insert) { CursorX = Lines[CursorY].Length; }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
        if (command.Type.HasFlag(CommandType.GlueCursorToEndOfLine)) { IsCursorGluedToEndOfLine = true; }
        else if (resetCursorGlue) { IsCursorGluedToEndOfLine = false; }
    }
}
