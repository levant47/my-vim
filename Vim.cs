public class Vim
{
    public enum Mode
    {
        Normal,
        Insert,
    }

    public class HistoryEntry
    {
        public int StartX;
        public int StartY;
        public string AddedText = "";
        public string RemovedTextToTheLeft = "";
        public string RemovedTextToTheRight = "";

        public HistoryEntry Copy() => new()
        {
            StartX = StartX,
            StartY = StartY,
            AddedText = AddedText,
            RemovedTextToTheLeft = RemovedTextToTheLeft,
            RemovedTextToTheRight = RemovedTextToTheRight,
        };

        public void AddText(string text) { AddedText += text; }

        public void RemoveTextToTheLeft(char c, int x, int y)
        {
            if (AddedText != "") { AddedText = AddedText[..^1]; }
            else
            {
                RemovedTextToTheLeft = c + RemovedTextToTheLeft;
                StartX = x;
                StartY = y;
            }
        }

        public void RemoveTextToTheRight(char c) { RemovedTextToTheRight += c; }

        public bool IsEmpty() => AddedText == "" && RemovedTextToTheLeft == "" && RemovedTextToTheRight == "";
    }

    public const int FontHeight = 32;
    public const int FontWidth = 14;

    public List<string> Lines;
    public int CursorX;
    public int CursorY;
    public int GluedCursorX;
    public Mode _mode = Mode.Normal;
    public bool IsCursorGluedToEndOfLine;
    private List<HistoryEntry> _history = new();
    private int _historyIndex;
    private HistoryEntry _currentHistoryEntry = new();
    private bool _gCommand = false;
    private bool _fCommand = false;
    private bool _fReverseCommand = false;
    private Command _lastInsertionCommand = new() { Type = CommandType.Noop };

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
        if (_mode == Mode.Insert && input.Text != "")
        {
            Execute(new()
            {
                Type = CommandType.Text,
                Text = input.Text,
            });
        }
        else if (_gCommand && input.Text == "")
        {
            switch (input.Key, input.Modifier)
            {
                case (KeyboardKey.G, VimInputModifier.None):
                    Execute(new() { Type = CommandType.GoToFileStart });
                    break;
            }
            _gCommand = false;
        }
        else if (_fCommand && input.Text != "")
        {
            var characterIndex = Lines[CursorY].IndexOf(input.Text[0], CursorX);
            if (characterIndex != -1)
            {
                CursorX = characterIndex;
            }
            _fCommand = false;
        }
        else if (_fReverseCommand && input.Text != "")
        {
            var characterIndex = Lines[CursorY].IndexOf(input.Text[0], 0, CursorX);
            if (characterIndex != -1)
            {
                CursorX = characterIndex;
            }
            _fReverseCommand = false;
        }
        else if (input.Key != KeyboardKey.Null)
        {
            Command? command = null;
            switch (_mode, input.Key, input.Modifier)
            {
                case (Mode.Normal, KeyboardKey.J, VimInputModifier.None) or (_, KeyboardKey.Down, VimInputModifier.None):
                    command = new() { Type = CommandType.Down };
                    break;
                case (Mode.Normal, KeyboardKey.K, VimInputModifier.None) or (_, KeyboardKey.Up, VimInputModifier.None):
                    command = new() { Type = CommandType.Up };
                    break;
                case (Mode.Normal, KeyboardKey.H, VimInputModifier.None) or (_, KeyboardKey.Left, VimInputModifier.None):
                    command = new() { Type = CommandType.Left };
                    break;
                case (Mode.Normal, KeyboardKey.L, VimInputModifier.None) or (_, KeyboardKey.Right, VimInputModifier.None):
                    command = new() { Type = CommandType.Right };
                    break;
                case (Mode.Normal, KeyboardKey.Zero, VimInputModifier.None) or (_, KeyboardKey.Home, VimInputModifier.None):
                    command = new() { Type = CommandType.LineStart };
                    break;
                case (Mode.Normal, KeyboardKey.Four, VimInputModifier.Shift) or (_, KeyboardKey.End, VimInputModifier.None):
                    command = new() { Type = CommandType.LineEnd };
                    break;
                case (Mode.Normal, KeyboardKey.I, VimInputModifier.None):
                    command = new() { Type = CommandType.Insert };
                    _lastInsertionCommand = command;
                    break;
                case (Mode.Normal, KeyboardKey.A, VimInputModifier.None):
                    command = new() { Type = CommandType.Append };
                    _lastInsertionCommand = command;
                    break;
                case (Mode.Normal, KeyboardKey.A, VimInputModifier.Shift):
                    command = new() { Type = CommandType.AppendToEndOfLine };
                    _lastInsertionCommand = command;
                    break;
                case (Mode.Normal, KeyboardKey.G, VimInputModifier.None):
                    _gCommand = true;
                    break;
                case (Mode.Normal, KeyboardKey.X, VimInputModifier.None) or (Mode.Normal, KeyboardKey.Delete, VimInputModifier.None):
                    command = new() { Type = CommandType.Delete };
                    break;
                case (Mode.Normal, KeyboardKey.U, VimInputModifier.None):
                    command = new() { Type = CommandType.Undo };
                    break;
                case (Mode.Normal, KeyboardKey.R, VimInputModifier.Control):
                    command = new() { Type = CommandType.Redo };
                    break;
                case (Mode.Normal, KeyboardKey.Period, VimInputModifier.None):
                    if (_history.Count != 0)
                    {
                        Execute(_lastInsertionCommand);
                        ApplyHistoryEntry(_history.Last(), CursorX, CursorY);
                        var newEntry = _history.Last().Copy();
                        newEntry.StartX = CursorX;
                        newEntry.StartY = CursorY;
                        _history.Add(newEntry);
                        _historyIndex++;
                        Execute(new() { Type = CommandType.ExitInsertMode });
                    }
                    break;
                case (Mode.Normal, KeyboardKey.F, VimInputModifier.None):
                    _fCommand = true;
                    break;
                case (Mode.Normal, KeyboardKey.F, VimInputModifier.Shift):
                    _fReverseCommand = true;
                    break;
                case (Mode.Normal, KeyboardKey.W, VimInputModifier.None):
                    command = new() { Type = CommandType.GoToNextWord };
                    break;
                case (Mode.Insert, KeyboardKey.Escape, VimInputModifier.None):
                    command = new() { Type = CommandType.ExitInsertMode };
                    break;
                case (Mode.Insert, KeyboardKey.Enter, VimInputModifier.None):
                    command = new() { Type = CommandType.NewLine };
                    break;
                case (Mode.Insert, KeyboardKey.Backspace, VimInputModifier.None):
                    command = new() { Type = CommandType.Backspace };
                    break;
                case (Mode.Insert, KeyboardKey.Delete, VimInputModifier.None):
                    command = new() { Type = CommandType.Delete };
                    break;
            }
            if (command != null) { Execute(command); }
        }
    }

    public enum CommandType
    {
        Noop,
        Text,
        Left,
        Down,
        Up,
        Right,
        LineStart,
        LineEnd,
        Insert,
        Append,
        AppendToEndOfLine,
        GoToFileStart,
        Delete,
        Undo,
        Redo,
        ExitInsertMode,
        NewLine,
        Backspace,
        GoToNextWord
    }

    public class Command
    {
        public CommandType Type;

        // AppendText
        public string Text = "";
    }

    public void Execute(Command command)
    {
        var unsetCursorGlue = true;
        var ignoreGluedCursorX = false;
        var commitHistoryEntry = true;
        switch (command.Type)
        {
            case CommandType.Noop: break;
            case CommandType.Text:
                Lines[CursorY] = Lines[CursorY].Insert(CursorX, command.Text);
                CursorX += command.Text.Length;
                _currentHistoryEntry.AddText(command.Text);
                commitHistoryEntry = false;
                break;
            case CommandType.Left:
                CursorX--;
                break;
            case CommandType.Down:
                CursorY++;
                unsetCursorGlue = false;
                ignoreGluedCursorX = true;
                break;
            case CommandType.Up:
                CursorY--;
                unsetCursorGlue = false;
                ignoreGluedCursorX = true;
                break;
            case CommandType.Right:
                CursorX++;
                unsetCursorGlue = false;
                break;
            case CommandType.LineStart: CursorX = 0; break;
            case CommandType.LineEnd:
                LineEnd();
                IsCursorGluedToEndOfLine = true;
                unsetCursorGlue = false;
                break;
            case CommandType.Insert:
                _mode = Mode.Insert;
                break;
            case CommandType.Append:
                _mode = Mode.Insert;
                CursorX++;
                break;
            case CommandType.AppendToEndOfLine:
                _mode = Mode.Insert;
                LineEnd();
                break;
            case CommandType.GoToFileStart:
                CursorX = 0;
                CursorY = 0;
                break;
            case CommandType.Delete:
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
                commitHistoryEntry = _mode == Mode.Normal;
                break;
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
                if (_historyIndex != _history.Count)
                {
                    var entry = _history[_historyIndex];
                    Redo(entry);
                    _historyIndex++;
                    CursorX = entry.StartX;
                    CursorY = entry.StartY;
                }
                break;
            }
            case CommandType.ExitInsertMode:
                _mode = Mode.Normal;
                CursorX--;
                break;
            case CommandType.NewLine:
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
            case CommandType.Backspace:
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
                        _currentHistoryEntry.RemoveTextToTheLeft('\n', CursorX, CursorY);
                    }
                }
                else
                {
                    var removedCharacter = Lines[CursorY][CursorX - 1];
                    Lines[CursorY] = Lines[CursorY].Remove(CursorX - 1, 1);
                    CursorX--;
                    _currentHistoryEntry.RemoveTextToTheLeft(removedCharacter, CursorX, CursorY);
                }
                commitHistoryEntry = false;
                break;
            }
            case CommandType.GoToNextWord:
            {
                break;
            }
            default: throw new();
        }
        if (unsetCursorGlue) { IsCursorGluedToEndOfLine = false; }
        if (!ignoreGluedCursorX) { GluedCursorX = CursorX; }
        RecomputeCursor();
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
        var maxX = Math.Max(0, Lines[CursorY].Length - (_mode == Mode.Insert ? 0 : 1));
        CursorX = !IsCursorGluedToEndOfLine ? Math.Clamp(GluedCursorX, 0, maxX) : maxX;
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

    private void Redo(HistoryEntry entry) { ApplyHistoryEntry(entry, entry.StartX, entry.StartY); }

    private void ApplyHistoryEntry(HistoryEntry entry, int x0, int y0)
    {
        var textToRemove = entry.RemovedTextToTheLeft + entry.RemovedTextToTheRight;
        if (textToRemove != "")
        {
            var isFirst = true;
            foreach (var segment in textToRemove.Split('\n'))
            {
                if (!isFirst)
                {
                    Lines[y0] += Lines[y0 + 1];
                    Lines.RemoveAt(y0 + 1);
                }
                Lines[y0] = Lines[y0].Remove(x0, segment.Length);
                isFirst = false;
            }
        }
        if (entry.AddedText != "")
        {
            var segments = entry.AddedText.Split('\n');
            for (var i = 0; i < segments.Length; i++)
            {
                var x = i == 0 ? x0 : 0;
                Lines[y0 + i] = Lines[y0 + i].Insert(x, segments[i]);
                if (i != segments.Length - 1)
                {
                    Lines.Insert(y0 + i + 1, Lines[y0 + i][(x + segments[i].Length)..]);
                    Lines[y0 + i] = Lines[y0 + i][..(x + segments[i].Length)];
                }
            }
        }
    }

    private void LineEnd()
    {
        if (Lines[CursorY] == "") { CursorX = 0; }
        else if (_mode == Mode.Normal) { CursorX = Lines[CursorY].Length - 1; }
        else if (_mode == Mode.Insert) { CursorX = Lines[CursorY].Length; }
    }
}
