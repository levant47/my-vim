public class FpsCounter
{
    private float[] window;
    private int index;
    private int filledCount;

    public FpsCounter(int frameTimeWindowSize = 10)
    {
        window = new float[frameTimeWindowSize];
    }

    public void AddFrameTime(float frameTimeInSeconds)
    {
        if (frameTimeInSeconds == 0) { return; }
        window[index] = 1 / frameTimeInSeconds;
        index = (index + 1) % window.Length;
        filledCount = Math.Min(window.Length, filledCount + 1);
    }

    public float CalculateFps()
    {
        if (filledCount == 0) { return 0; }
        return window.Average();
    }
}
