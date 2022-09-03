namespace CSharpWolfenstein.Engine;

public class FpsCounter
{
    private int _tickIndex = 0;
    private long _tickSum = 0;
    private readonly long[] _tickList = new long[100];
    private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    public double GetCurrentFps()
    {
        var newTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var delta = newTick - _lastTick;
        _lastTick = newTick;

        _tickSum -= _tickList[_tickIndex];
        _tickSum += delta;
        _tickList[_tickIndex] = delta;

        if (++_tickIndex == _tickList.Length)
            _tickIndex = 0;

        return 1000.0 / ((double)_tickSum / _tickList.Length);
    }
}