﻿public class Vim
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
    private Font _font;

    private List<HistoryEntry> _history = [];
    private HistoryEntry _currentHistoryEntry = new();
    private int _currentHistoryIndex = 0;

    public Vim(string buffer)
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

        ProcessEvents();

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

    public void ProcessEvents()
    {
        bool resetLastPressedKey = false;
        var prevMode = _mode;
        var isShift = VimInput.IsShift();
        var isControl = VimInput.IsControl();
        if (VimInput.Pressed(KeyboardKey.Down))
        {
            SetCursor(y: CursorY + 1);
            CommitCurrentHistoryEntry();
        }
        if (VimInput.Pressed(KeyboardKey.Up))
        {
            SetCursor(y: CursorY - 1);
            CommitCurrentHistoryEntry();
        }
        if (VimInput.Pressed(KeyboardKey.Left))
        {
            SetCursor(x: CursorX - 1);
            CommitCurrentHistoryEntry();
        }
        if (VimInput.Pressed(KeyboardKey.Right))
        {
            SetCursor(x: CursorX + 1);
            CommitCurrentHistoryEntry();
        }
        if (_mode == Mode.Normal)
        {
            if (VimInput.Pressed(KeyboardKey.J) || VimInput.Pressed(KeyboardKey.Enter)) { SetCursor(y: CursorY + 1); }
            if (VimInput.Pressed(KeyboardKey.K)) { SetCursor(y: CursorY - 1); }
            if (VimInput.Pressed(KeyboardKey.H)) { SetCursor(x: CursorX - 1); }
            if (VimInput.Pressed(KeyboardKey.L)) { SetCursor(x: CursorX + 1); }
            if (VimInput.Pressed(KeyboardKey.Backspace))
            {
                if (CursorX != 0) { SetCursor(x: CursorX - 1); }
                else if (CursorY != 0) { SetCursor(x: Lines[CursorY - 1].Length, y: CursorY - 1); }
            }
            if (VimInput.Pressed(KeyboardKey.Zero)) { SetCursor(x: 0); }
            if (isShift && VimInput.Pressed(KeyboardKey.Four)) { SetCursor(x: Lines[CursorY].Length - 1); }
            if (VimInput.Pressed(KeyboardKey.I)) { SetMode(Mode.Insert); }
            if (isShift && VimInput.Pressed(KeyboardKey.A))
            {
                SetMode(Mode.Insert);
                SetCursor(x: Lines[CursorY].Length);
            }
            else if (VimInput.Pressed(KeyboardKey.A))
            {
                SetMode(Mode.Insert);
                SetCursor(x: CursorX + 1);
            }
            if (VimInput.Pressed(KeyboardKey.D) && _lastPressedKey == (int)KeyboardKey.D || VimInput.PressedRepeat(KeyboardKey.D))
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
            if (VimInput.Pressed(KeyboardKey.X))
            {
                if (Lines[CursorY].Length != 0)
                {
                    _currentHistoryEntry = new() { StartX = CursorX, StartY = CursorY, RemovedTextToTheRight = Lines[CursorY][CursorX].ToString() };
                    CommitCurrentHistoryEntry();

                    Lines[CursorY] = Lines[CursorY].Remove(CursorX, 1);
                    SetCursor(x: CursorX);
                }
            }
            if (VimInput.Pressed(KeyboardKey.U))
            {
                if (_currentHistoryIndex != 0)
                {
                    _currentHistoryIndex--;
                    Undo(_history[_currentHistoryIndex]);
                }
            }
            if (isControl && VimInput.Pressed(KeyboardKey.R))
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
            if (VimInput.Pressed(KeyboardKey.Escape)) { SetMode(Mode.Normal); }
            if (VimInput.Pressed(KeyboardKey.Delete))
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
            if (VimInput.Pressed(KeyboardKey.Backspace))
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
            if (VimInput.Pressed(KeyboardKey.Enter))
            {
                Lines.Insert(CursorY + 1, Lines[CursorY][CursorX..]);
                Lines[CursorY] = Lines[CursorY][..CursorX];
                SetCursor(x: 0, y: CursorY + 1);
                _currentHistoryEntry.Add("\n");
            }
            while (true)
            {
                var codePoint = VimInput.GetCharPressed();
                if (codePoint == 0) { break; }
                var text = char.ConvertFromUtf32(codePoint);
                Lines[CursorY] = Lines[CursorY].Insert(CursorX, text);
                _currentHistoryEntry.Add(text);
                SetCursor(x: CursorX + text.Length);
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
            var keyPressed = VimInput.GetPressed();
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