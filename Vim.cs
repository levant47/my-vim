// old version
/*
public class Vim
{
    private enum Mode
    {
        Normal,
        Insert,
    }

    private class HistoryEntry
    {
        public int StartX;
        public int StartY;
        public string AddedText = "";
        public string RemovedTextToTheLeft = "";
        public string RemovedTextToTheRight = "";

        public void Add(string text) { AddedText += text; }

        public void RemoveLeft(char c, int newX, int newY)
        {
            if (AddedText != "") { AddedText = AddedText[..^1]; }
            else
            {
                StartX = newX;
                StartY = newY;
                RemovedTextToTheLeft = c + RemovedTextToTheLeft;
            }
        }

        public void RemoveRight(char c) { RemovedTextToTheRight += c; }
    }

    private const int FontHeight = 32;
    private const int FontWidth = 14;

    public int CursorX;
    public int CursorY;
    public List<string> Lines;
    private Mode _mode = Mode.Normal;
    private int _lastPressedKey;
    private KeyboardKey _lastExecutedKey;
    private bool _lastExecutedShift;
    private Font _font;

    private List<HistoryEntry> _history = [];
    private HistoryEntry _currentHistoryEntry = new();
    private int _currentHistoryIndex = 0;

    public Vim2(string buffer)
    {
        Lines = buffer.Replace("\r", "").Split('\n').ToList();
    }

    public void InitializeForRendering()
    {
        _font = Raylib.LoadFont("assets\\RobotoMono.ttf");
    }

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
        Process(input: input);

        var pressedKeys = Enum.GetValues<KeyboardKey>().Where(key => Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key)).ToList();
        foreach (var pressedKey in pressedKeys)
        {
            Process(
                pressedKey,
                isShift: Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.LeftShift),
                isControl: Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)
            );
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

    public void Process(KeyboardKey key = KeyboardKey.Null, bool isShift = false, bool isControl = false, string input = "")
    {
        bool resetLastPressedKey = false;
        var prevMode = _mode;
        if (key == KeyboardKey.Down)
        {
            SetCursor(y: CursorY + 1);
            CommitCurrentHistoryEntry();
        }
        else if (key == KeyboardKey.Up)
        {
            SetCursor(y: CursorY - 1);
            CommitCurrentHistoryEntry();
        }
        else if (key == KeyboardKey.Left)
        {
            SetCursor(x: CursorX - 1);
            CommitCurrentHistoryEntry();
        }
        else if (key == KeyboardKey.Right)
        {
            SetCursor(x: CursorX + 1);
            CommitCurrentHistoryEntry();
        }
        else if (_mode == Mode.Normal)
        {
            if (key is KeyboardKey.J or KeyboardKey.Enter) { Process(KeyboardKey.Down); }
            else if (key == KeyboardKey.K) { Process(KeyboardKey.Up); }
            else if (key == KeyboardKey.H) { Process(KeyboardKey.Left); }
            else if (key == KeyboardKey.L) { Process(KeyboardKey.Right); }
            else if (key == KeyboardKey.Backspace)
            {
                if (CursorX != 0) { SetCursor(x: CursorX - 1); }
                else if (CursorY != 0) { SetCursor(x: Lines[CursorY - 1].Length, y: CursorY - 1); }
            }
            else if (key == KeyboardKey.Zero) { SetCursor(x: 0); }
            else if (isShift && key == KeyboardKey.Four) { SetCursor(x: Lines[CursorY].Length - 1); }
            else if (key == KeyboardKey.I)
            {
                SetMode(Mode.Insert);
                _lastExecutedKey = KeyboardKey.I;
            }
            else if (isShift && key == KeyboardKey.A)
            {
                Process(KeyboardKey.Four, isShift: true);
                Process(KeyboardKey.A);
                _lastExecutedKey = KeyboardKey.A;
                _lastExecutedShift = true;
            }
            else if (key == KeyboardKey.A)
            {
                SetMode(Mode.Insert);
                SetCursor(x: CursorX + 1);
            }
            else if (key == KeyboardKey.D && _lastPressedKey == (int)KeyboardKey.D)
            {
                if (Lines is not [""])
                {
                    _currentHistoryEntry.StartX = 0;
                    _currentHistoryEntry.StartY = CursorY;
                    _currentHistoryEntry.RemovedTextToTheRight = Lines[CursorY] + "\n";
                    CommitCurrentHistoryEntry();

                    if (Lines.Count != 1) { Lines.RemoveAt(CursorY); }
                    else { Lines[0] = ""; }
                    SetCursor(y: CursorY);
                    resetLastPressedKey = true;
                }
            }
            else if (key is KeyboardKey.X or KeyboardKey.Delete)
            {
                if (Lines[CursorY].Length != 0)
                {
                    _currentHistoryEntry = new() { StartX = CursorX, StartY = CursorY, RemovedTextToTheRight = Lines[CursorY][CursorX].ToString() };
                    CommitCurrentHistoryEntry();

                    Lines[CursorY] = Lines[CursorY].Remove(CursorX, 1);
                    SetCursor(x: CursorX);
                }
            }
            else if (key == KeyboardKey.U)
            {
                if (_currentHistoryIndex != 0)
                {
                    _currentHistoryIndex--;
                    Undo(_history[_currentHistoryIndex]);
                }
            }
            else if (isControl && key == KeyboardKey.R)
            {
                if (_currentHistoryIndex != _history.Count)
                {
                    Redo(_history[_currentHistoryIndex]);
                    _currentHistoryIndex++;
                }
            }
        }
        else if (_mode == Mode.Insert)
        {
            if (key == KeyboardKey.Escape) { SetMode(Mode.Normal); }
            else if (key == KeyboardKey.Delete)
            {
                if (CursorX != Lines[CursorY].Length)
                {
                    _currentHistoryEntry.RemoveRight(Lines[CursorY][CursorX]);
                    Lines[CursorY] = Lines[CursorY].Remove(CursorX, 1);
                    SetCursor(x: CursorX);
                }
                else if (CursorY != Lines.Count - 1)
                {
                    _currentHistoryEntry.RemoveRight('\n');
                    Lines[CursorY] += Lines[CursorY + 1];
                    Lines.RemoveAt(CursorY + 1);
                }
            }
            else if (key == KeyboardKey.Backspace)
            {
                if (CursorX != 0)
                {
                    _currentHistoryEntry.RemoveLeft(Lines[CursorY][CursorX - 1], CursorX - 1, CursorY);
                    Lines[CursorY] = Lines[CursorY].Remove(CursorX - 1, 1);
                    SetCursor(x: CursorX - 1);
                }
                else if (CursorY != 0)
                {
                    var originalLineLength = Lines[CursorY - 1].Length;
                    _currentHistoryEntry.RemoveLeft('\n', originalLineLength, CursorY - 1);
                    Lines[CursorY - 1] += Lines[CursorY];
                    Lines.RemoveAt(CursorY);
                    SetCursor(x: originalLineLength, y: CursorY - 1);
                }
            }
            else if (key == KeyboardKey.Enter)
            {
                Lines.Insert(CursorY + 1, Lines[CursorY][CursorX..]);
                Lines[CursorY] = Lines[CursorY][..CursorX];
                SetCursor(x: 0, y: CursorY + 1);
                _currentHistoryEntry.Add("\n");
            }
            else if (input != "")
            {
                Lines[CursorY] = Lines[CursorY].Insert(CursorX, input);
                _currentHistoryEntry.Add(input);
                SetCursor(x: CursorX + input.Length);
            }
        }

        if (_mode == Mode.Insert && prevMode != Mode.Insert)
        {
            _currentHistoryEntry.StartX = CursorX;
            _currentHistoryEntry.StartY = CursorY;
        }
        else if (_mode != Mode.Insert && prevMode == Mode.Insert) { CommitCurrentHistoryEntry(); }
        if (!resetLastPressedKey)
        {
            var keyPressed = (int)key;
            if (keyPressed != 0) { _lastPressedKey = keyPressed; }
        }
        else { _lastPressedKey = 0; }
    }

    private void CommitCurrentHistoryEntry()
    {
        if (_currentHistoryEntry.AddedText != "" || _currentHistoryEntry.RemovedTextToTheLeft != "" || _currentHistoryEntry.RemovedTextToTheRight != "")
        {
            if (_currentHistoryIndex == _history.Count) { _history.Add(_currentHistoryEntry); }
            else { _history[_currentHistoryIndex] = _currentHistoryEntry; }
            _currentHistoryIndex++;

        }
        _currentHistoryEntry = new()
        {
            AddedText = "",
            RemovedTextToTheLeft = "",
            RemovedTextToTheRight = "",
        };
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
        SetCursor(entry.StartX, entry.StartY);
    }

    private void Redo(HistoryEntry entry)
    {
        ApplyHistoryEntry(entry.StartX, entry.StartY, entry);
        SetCursor(entry.StartX, entry.StartY);
    }

    private void ApplyHistoryEntry(int x0, int y0, HistoryEntry entry)
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

    private void SetMode(Mode newMode)
    {
        _mode = newMode;
        SetCursor(x: CursorX);
    }

    private void SetCursor(int? x = null, int? y = null)
    {
        if (y != null) { CursorY = Math.Clamp((int)y, 0, Lines.Count - 1); }
        if (x != null)
        {
            var upperBound = Lines[CursorY].Length - 1;
            if (_mode == Mode.Insert) { upperBound++; }
            CursorX = Math.Clamp((int)x, 0, Math.Max(0, upperBound));
        }
    }
}
*/
