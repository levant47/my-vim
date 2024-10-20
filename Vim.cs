public class Vim
{
    public enum Mode
    {
        Normal,
        Insert,
        GMode,
    }

    public class HistoryEntry
    {
        public int StartX;
        public int StartY;
        public string AddedText = "";
        public string RemovedTextToTheLeft = "";
        public string RemovedTextToTheRight = "";

        public void AddText(string text) { AddedText += text; }

        public void RemoveTextToTheLeft(char c)
        {
            if (AddedText != "") { AddedText = AddedText[..^1]; }
            else { RemovedTextToTheLeft = c + RemovedTextToTheLeft; }
        }

        public void RemoveTextToTheRight(char c) { RemovedTextToTheRight += c; }

        public bool IsEmpty() => AddedText == "" && RemovedTextToTheLeft == "" && RemovedTextToTheRight == "";
    }

    public const int FontHeight = 32;
    public const int FontWidth = 14;

    public List<string> Lines;
    public int CursorX;
    public int CursorY;
    public Mode _mode = Mode.Normal;
    public bool IsCursorGluedToEndOfLine;
    private List<HistoryEntry> _history = new();
    private int _historyIndex;
    private HistoryEntry _currentHistoryEntry = new();

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
        if (input != "") { Process(new() { Text = input }); }

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
        var commands = new List<Command>();
        if (_mode == Mode.Insert && input.Text != "")
        {
            commands.Add(new()
            {
                Type = CommandType.AppendText,
                Text = input.Text,
            });
        }
        else if (input.Key != KeyboardKey.Null)
        {
            commands.AddRange((_mode, input.Key, input.Modifier) switch
            {
                (Mode.Normal, KeyboardKey.J, VimInputModifier.None) or (_, KeyboardKey.Down, VimInputModifier.None)
                    => [new() { Type = CommandType.RelativeNavigation, DeltaY = +1 }],
                (Mode.Normal, KeyboardKey.K, VimInputModifier.None) or (_, KeyboardKey.Up, VimInputModifier.None)
                    => [new() { Type = CommandType.RelativeNavigation, DeltaY = -1 }],
                (Mode.Normal, KeyboardKey.H, VimInputModifier.None) or (_, KeyboardKey.Left, VimInputModifier.None)
                    => [new() { Type = CommandType.RelativeNavigation, DeltaX = -1 }],
                (Mode.Normal, KeyboardKey.L, VimInputModifier.None) or (_, KeyboardKey.Right, VimInputModifier.None)
                    => [new() { Type = CommandType.RelativeNavigation, DeltaX = +1 }],
                (Mode.Normal, KeyboardKey.Zero, VimInputModifier.None) or (_, KeyboardKey.Home, VimInputModifier.None)
                    => [new() { Type = CommandType.GoToLineStart }],
                (Mode.Normal, KeyboardKey.Four, VimInputModifier.Shift) or (_, KeyboardKey.End, VimInputModifier.None)
                    => [new() { Type = CommandType.GoToLineEnd }],
                (Mode.Normal, KeyboardKey.I, VimInputModifier.None)
                    => [new() { Type = CommandType.ChangeMode, TargetMode = Mode.Insert }],
                (Mode.Normal, KeyboardKey.A, VimInputModifier.None) => [
                    new() { Type = CommandType.ChangeMode, TargetMode = Mode.Insert },
                    new() { Type = CommandType.RelativeNavigation, DeltaX = 1 },
                ],
                (Mode.Normal, KeyboardKey.A, VimInputModifier.Shift) => [
                    new() { Type = CommandType.ChangeMode, TargetMode = Mode.Insert },
                    new() { Type = CommandType.GoToLineEnd },
                ],
                (Mode.Normal, KeyboardKey.G, VimInputModifier.None) => [new() { Type = CommandType.ChangeMode, TargetMode = Mode.GMode }],
                (Mode.Normal, KeyboardKey.X, VimInputModifier.None) or (_, KeyboardKey.Delete, VimInputModifier.None)
                    => [new() { Type = CommandType.DeleteRight }],
                (Mode.Normal, KeyboardKey.U, VimInputModifier.None)
                    => [new() { Type = CommandType.Undo }],
                (Mode.Normal, KeyboardKey.R, VimInputModifier.Control)
                    => [new() { Type = CommandType.Redo }],
                (Mode.Insert, KeyboardKey.Escape, VimInputModifier.None) => [
                    new() { Type = CommandType.RelativeNavigation, DeltaX = -1 },
                    new() { Type = CommandType.ChangeMode, TargetMode = Mode.Normal },
                ],
                (Mode.Insert, KeyboardKey.Enter, VimInputModifier.None)
                    => [new() { Type = CommandType.InsertNewLine }],
                (Mode.Insert, KeyboardKey.Backspace, VimInputModifier.None)
                    => [new() { Type = CommandType.DeleteLeft }],
                (Mode.GMode, KeyboardKey.G, VimInputModifier.None) => [
                    new() { Type = CommandType.GoToFileStart },
                    new() { Type = CommandType.ChangeMode, TargetMode = Mode.Normal },
                ],
                (Mode.GMode, _, _) => [new() { Type = CommandType.ChangeMode, TargetMode = Mode.Normal }],
                _ => Array.Empty<Command>(),
            });
        }

        foreach (var command in commands)
        {
            Execute(command);
        }
    }

    public enum CommandType
    {
        RelativeNavigation,
        GoToLineStart,
        GoToLineEnd,
        ChangeMode,
        AppendText,
        InsertNewLine,
        DeleteLeft,
        DeleteRight,
        GoToFileStart,
        Undo,
        Redo,
    }

    public class Command
    {
        public CommandType Type;

        public bool NotUserInput;

        // RelativeNavigation
        public int DeltaX;
        public int DeltaY;

        // ChangeMode
        public Mode TargetMode;

        // AppendText
        public string Text = "";
    }

    public void Execute(Command command)
    {
        var unsetCursorGlue = true;
        var commitHistoryEntry = true;
        switch (command.Type)
        {
            case CommandType.ChangeMode:
            {
                _mode = command.TargetMode;
                RecomputeCursor();
                break;
            }
            case CommandType.AppendText:
            {
                Lines[CursorY] = Lines[CursorY].Insert(CursorX, command.Text);
                CursorX += command.Text.Length;
                _currentHistoryEntry.AddText(command.Text);
                commitHistoryEntry = false;
                break;
            }
            case CommandType.InsertNewLine:
            {
                var left = Lines[CursorY][..CursorX];
                var right = Lines[CursorY][CursorX..];
                Lines[CursorY] = left;
                Lines.Insert(CursorY + 1, right);
                CursorX = 0;
                CursorY++;
                _currentHistoryEntry.AddText("\n");
                commitHistoryEntry = false;
                break;
            }
            case CommandType.DeleteLeft:
            {
                if (CursorX == 0)
                {
                    if (CursorY != 0)
                    {
                        var previousLineLength = Lines[CursorY - 1].Length; 
                        Lines[CursorY - 1] += Lines[CursorY];
                        Lines.RemoveAt(CursorY);
                        CursorY--;
                        CursorX = previousLineLength;
                        _currentHistoryEntry.RemoveTextToTheLeft('\n');
                    }
                }
                else
                {
                    _currentHistoryEntry.RemoveTextToTheLeft(Lines[CursorY][CursorX - 1]);
                    Lines[CursorY] = Lines[CursorY].Remove(CursorX - 1, 1);
                    CursorX--;
                }
                commitHistoryEntry = false;
                break;
            }
            case CommandType.DeleteRight:
            {
                if (CursorX == Lines[CursorY].Length)
                {
                    if (CursorY != Lines.Count - 1)
                    {
                        Lines[CursorY] += Lines[CursorY + 1];
                        Lines.RemoveAt(CursorY + 1);
                        _currentHistoryEntry.RemoveTextToTheRight('\n');
                    }
                }
                else
                {
                    _currentHistoryEntry.RemoveTextToTheRight(Lines[CursorY][CursorX]);
                    Lines[CursorY] = Lines[CursorY].Remove(CursorX, 1);
                }
                commitHistoryEntry = false;
                break;
            }
            case CommandType.RelativeNavigation:
            {
                CursorY = Math.Clamp(CursorY + command.DeltaY, 0, Lines.Count - 1);
                if (_mode == Mode.Normal && command.DeltaX >= 0 && IsCursorGluedToEndOfLine)
                {
                    unsetCursorGlue = false;
                    CursorX = Math.Max(0, Lines[CursorY].Length - 1);
                }
                else if (Lines[CursorY] == "") { CursorX = 0; }
                else if (_mode == Mode.Normal) { CursorX = Math.Clamp(CursorX + command.DeltaX, 0, Lines[CursorY].Length - 1); }
                else if (_mode == Mode.Insert) { CursorX = Math.Clamp(CursorX + command.DeltaX, 0, Lines[CursorY].Length); }
                break;
            }
            case CommandType.GoToLineStart:
            {
                CursorX = 0;
                break;
            }
            case CommandType.GoToLineEnd:
            {
                if (Lines[CursorY] == "") { CursorX = 0; }
                else if (_mode == Mode.Normal) { CursorX = Lines[CursorY].Length - 1; }
                else if (_mode == Mode.Insert) { CursorX = Lines[CursorY].Length; }
                IsCursorGluedToEndOfLine = true;
                unsetCursorGlue = false;
                break;
            }
            case CommandType.GoToFileStart:
            {
                CursorX = 0;
                CursorY = 0;
                break;
            }
            case CommandType.Undo:
            {
                HistoryEntry entryToUndo;
                if (!_currentHistoryEntry.IsEmpty()) { entryToUndo = _currentHistoryEntry; }
                else if (_historyIndex == 0) { break; }
                else
                {
                    entryToUndo = _history[_historyIndex - 1];
                    _historyIndex--;
                }
                Undo(entryToUndo);
                break;
            }
            case CommandType.Redo:
            {
                // TODO!
                break;
            }
            default: throw new ArgumentOutOfRangeException();
        }
        if (unsetCursorGlue) { IsCursorGluedToEndOfLine = false; }
        if (commitHistoryEntry)
        {
            if (!_currentHistoryEntry.IsEmpty())
            {
                if (_historyIndex != _history.Count) { _history = _history[.._historyIndex]; }
                _history.Add(_currentHistoryEntry);
                _currentHistoryEntry = new();
                _historyIndex++;
            }
            _currentHistoryEntry.StartX = CursorX;
            _currentHistoryEntry.StartY = CursorY;
        }
    }

    private void RecomputeCursor()
    {
        CursorY = Math.Clamp(CursorY, 0, Lines.Count - 1);
        CursorX = Math.Clamp(CursorX, 0, Math.Max(0, Lines[CursorY].Length - (_mode == Mode.Insert ? 0 : 1)));
    }

    private void Undo(HistoryEntry entry)
    {
        if (entry.AddedText != "")
        {
            var isFirstSegment = true;
            foreach (var segment in entry.AddedText.Split('\n'))
            {
                if (!isFirstSegment)
                {
                    Lines[entry.StartY] += Lines[entry.StartY + 1];
                    Lines.RemoveAt(entry.StartY + 1);
                }
                Lines[entry.StartY] = Lines[entry.StartY].Remove(entry.StartX, segment.Length);
                isFirstSegment = false;
            }
        }
        var textToRestore = entry.RemovedTextToTheLeft + entry.RemovedTextToTheRight;
        if (textToRestore  != "")
        {
            var x = entry.StartX;
            var y = entry.StartY;
            var isFirst = true;
            foreach (var segment in textToRestore.Split('\n'))
            {
                if (!isFirst)
                {
                    var textToCarryOver = Lines[y - 1][x..];
                    Lines[y - 1] = Lines[y - 1][..x];
                    Lines.Insert(y, textToCarryOver);
                    x = 0;
                }
                Lines[y] = Lines[y].Insert(x, segment);
                x += segment.Length;
                y++;
                isFirst = false;
            }
        }
        CursorX = entry.StartX;
        CursorY = entry.StartY;
        RecomputeCursor();
    }
}
