using SkiaSharp.Views.Blazor;

namespace CSharpWolfenstein.Engine;

public class GameEngineNotRecognizedException : Exception { }

public abstract class AbstractGameEngine
{
    public abstract void NewFrame(SKPaintSurfaceEventArgs e);
    public abstract void OnKeyDown(ControlState controlState);
    public abstract void OnKeyUp(ControlState controlState);
}