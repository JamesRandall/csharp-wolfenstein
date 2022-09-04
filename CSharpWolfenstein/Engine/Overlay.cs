namespace CSharpWolfenstein.Engine;

public record OverlayAnimation(
    byte Red,
    byte Green,
    byte Blue,
    double Opacity,
    double MaxOpacity,
    double OpacityDelta,
    double FrameLength,
    double TimeRemainingUntilNextFrame)
{
    private static OverlayAnimation WithColor(byte red, byte green, byte blue, double maxOpacity)
    {
        var clampedMaxOpacity = Math.Max(maxOpacity, 0.2);
        const double totalAnimationTime = 75.0;
        const double totalFrames = 10.0;
        var opacityDelta = clampedMaxOpacity / (totalFrames / 2.0); // we go in and out     
        var frameLength = totalAnimationTime / totalFrames;
        return new OverlayAnimation
        (
            Red: red,
            Green: green,
            Blue: blue,
            Opacity: opacityDelta,
            OpacityDelta: opacityDelta,
            MaxOpacity: maxOpacity,
            FrameLength: frameLength,
            TimeRemainingUntilNextFrame: frameLength
        );
    }

    public static OverlayAnimation Blood(double maxOpacity) => WithColor(0xFF, 0x0, 0x0, maxOpacity);
    public static OverlayAnimation Pickup => WithColor(0xFF, 0xD7, 0x0, 0.4);
}