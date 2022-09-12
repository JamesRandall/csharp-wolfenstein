using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine.RayCasting;
using CSharpWolfenstein.Game;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace CSharpWolfenstein.Engine;

public class RayCastDemonstrationEngine : AbstractGameEngine
{
    private readonly ViewportRenderer _viewportRenderer;
    private double _speed = 150.0;
    private double _timeUntilNextStep = 150.0;
    private readonly FrameTimer _frameTimer = new();
    private readonly SKBitmap _bitmap = new(Constants.WolfViewportWidth, Constants.WolfViewportHeight);
    private int _stripToDraw = 0;
    
    public RayCastDemonstrationEngine(GameEngine gameEngine, ViewportRenderer viewportRenderer)
    {
        _viewportRenderer = viewportRenderer;
        GameState = gameEngine.GameState;
        AssetPack = gameEngine.AssetPack;
    }
    
    public GameState GameState { get; }

    public AssetPack AssetPack { get; }

    public override void NewFrame(SKPaintSurfaceEventArgs e)
    {
        var (delta,fps) = _frameTimer.GetCurrentTimings();
        Render(e, fps);
        Update(delta);
    }

    private void Update(double delta)
    {
        _timeUntilNextStep -= delta;
        if (_timeUntilNextStep < 0.0)
        {
            _timeUntilNextStep += _speed;
            StepRayCaster rayCaster = (StepRayCaster) _viewportRenderer.RayCaster;
            rayCaster.Tick();
            if (_stripToDraw < Constants.WolfViewportWidth)
            {
                _stripToDraw++;
            }
        }
    }
    
    private (float,float) GetViewportPosition(SKSizeI surfaceSize)
    {
        var left = 0.0; // surfaceSize.Width / 2.0 - Constants.WolfViewportWidth / 2.0;
        var top = 0.0;
        return ((float)left, (float)top);
    }

    private void Render(SKPaintSurfaceEventArgs e, double fps)
    {
        var canvas = e.Surface.Canvas;
        unsafe
        {
            // Their is a good article on the different ways to update pixel data here:
            //  https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/bitmaps/pixel-bits
            // Having tried them all maintaining and then setting the pixel byte array is the most performant for us.
            fixed (uint* ptr = _viewportRenderer.UpdateFrameBuffer(AssetPack, GameState, (Constants.WolfViewportWidth,Constants.WolfViewportHeight), _stripToDraw))
            {
                _bitmap.SetPixels((IntPtr)ptr);
            }
        }

        var (viewportLeft, viewportTop) = GetViewportPosition(e.Info.Size);
        if (_stripToDraw == 0)
        {
            canvas.Clear(new SKColor(0xF0, 0xF0, 0xF0));
            SKColor ceiling = new SKColor(0x39, 0x39, 0x39);
            SKColor floor = new SKColor(0x73, 0x73, 0x73);
            canvas.DrawRect(new SKRect(viewportLeft, viewportTop, viewportLeft + Constants.WolfViewportWidth, viewportTop + Constants.WolfViewportHeight), new SKPaint { Style = SKPaintStyle.Fill, Color = ceiling });
            canvas.DrawRect(
                new SKRect(
                    viewportLeft, 
                    viewportTop + Constants.WolfViewportHeight / 2.0f, 
                    viewportLeft + Constants.WolfViewportWidth, 
                    viewportTop + Constants.WolfViewportHeight
                ),
                new SKPaint { Style = SKPaintStyle.Fill, Color = floor });
        }
        
        canvas.DrawBitmap(_bitmap, new SKRect(viewportLeft, viewportTop, viewportLeft+Constants.WolfViewportWidth, viewportTop + Constants.WolfViewportHeight));
        
        StepRayCaster rayCaster = (StepRayCaster) _viewportRenderer.RayCaster;
        if (rayCaster.Result?.IsComplete ?? false)
        {
            rayCaster.Stop();
        }
    }

    public override void OnKeyDown(ControlState controlState)
    {
        
    }

    public override void OnKeyUp(ControlState controlState)
    {
        if (controlState == ControlState.Forward)
        {
            _speed = _speed - 100.0;
            if (_speed <= 50.0) _speed = 50.0;
        }
        else if (controlState == ControlState.Backward)
        {
            _speed = _speed + 100.0;
        }
    }
}