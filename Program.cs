VimTests.RunAllTests();
if (args[0] == "--just-tests") { return; }

var SHOW_FPS = false;

var inputFilePath = args[0];
var inputFileContents = File.ReadAllText(inputFilePath);

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.InitWindow(0, 0, "Test");
var currentMonitor = Raylib.GetCurrentMonitor();
var (monitorWidth, monitorHeight) = (Raylib.GetMonitorWidth(currentMonitor), Raylib.GetMonitorHeight(currentMonitor));
var (windowWidth, windowHeight) = (monitorWidth / 2, monitorHeight / 2);
Raylib.SetWindowSize(windowWidth,  windowHeight);
Raylib.SetWindowPosition((monitorWidth - windowWidth) / 2, (monitorHeight - windowHeight) / 2); // center the window
Raylib.SetTargetFPS(60);
Raylib.SetExitKey(KeyboardKey.Null);

var fpsCounter = new FpsCounter();
var bufferRenderer = new Vim(inputFileContents);
bufferRenderer.InitializeForRendering();
while (!Raylib.WindowShouldClose())
{
    var frameTime = Raylib.GetFrameTime();
    fpsCounter.AddFrameTime(frameTime);

    Raylib.BeginDrawing();

    Raylib.ClearBackground(Color.White);

    bufferRenderer.Render();

    var fps = fpsCounter.CalculateFps();
    if (SHOW_FPS && fps != 0) { Raylib.DrawText($"FPS: {fps:F2}", 0, 0, 20, Color.Red); }

    Raylib.EndDrawing();
}
Raylib.CloseWindow();
