using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Game;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace CSharpWolfenstein.Engine;

public class GameEngine : AbstractGameEngine
{
    private readonly AssetPack _assetPack;
    private readonly ViewportRenderer _viewportRenderer;
    private readonly FrameTimer _frameTimer = new();
    private readonly SKBitmap _bitmap = new(Constants.WolfViewportWidth, Constants.WolfViewportHeight);
    private GameState _game;
    
    public GameEngine(AssetPack assetPack, GameState game, ViewportRenderer viewportRenderer)
    {
        _assetPack = assetPack;
        _game = game;
        _viewportRenderer = viewportRenderer;
    }

    public GameEngine(RayCastDemonstrationEngine re, ViewportRenderer viewportRenderer)
    {
        _assetPack = re.AssetPack;
        _game = re.GameState;
        _viewportRenderer = viewportRenderer;
    }

    public GameState GameState => _game;

    public AssetPack AssetPack => _assetPack;
    
    private (float,float) GetViewportPosition(SKSizeI surfaceSize)
    {
        var left = surfaceSize.Width / 2.0 - Constants.WolfViewportWidth * Constants.CanvasZoom / 2.0;
        var statusBarHeight = 35.0 * Constants.CanvasZoom;
        var verticalSpacing = (surfaceSize.Height - statusBarHeight - Constants.WolfViewportHeight * Constants.CanvasZoom) / 3.0;
        return ((float)left, (float)verticalSpacing);
    }

    public override void NewFrame(SKPaintSurfaceEventArgs e)
    {
        var (delta,fps) = _frameTimer.GetCurrentTimings();
        Render(e, fps);
        _game = _game.Update(delta);
    }
    
    private void Render(SKPaintSurfaceEventArgs e, double fps)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(0x00,0x40,0x40));
        unsafe
        {
            // Their is a good article on the different ways to update pixel data here:
            //  https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/bitmaps/pixel-bits
            // Having tried them all maintaining and then setting the pixel byte array is the most performant for us.
            fixed (uint* ptr = _viewportRenderer.UpdateFrameBuffer(_assetPack, _game, (Constants.WolfViewportWidth,Constants.WolfViewportHeight)))
            {
                _bitmap.SetPixels((IntPtr)ptr);
            }
        }

        SKColor ceiling = new SKColor(0x39, 0x39, 0x39);
        SKColor floor = new SKColor(0x73, 0x73, 0x73);
        var (viewportLeft, viewportTop) = GetViewportPosition(e.Info.Size);
        var zoomedWolfViewportWidth = (float)(Constants.WolfViewportWidth * Constants.CanvasZoom);
        var zoomedWolfViewportHeight = (float)(Constants.WolfViewportHeight * Constants.CanvasZoom);
        canvas.DrawRect(new SKRect(viewportLeft, viewportTop, viewportLeft + zoomedWolfViewportWidth, viewportTop + zoomedWolfViewportHeight), new SKPaint { Style = SKPaintStyle.Fill, Color = ceiling });
        canvas.DrawRect(
            new SKRect(
                viewportLeft, 
                viewportTop + zoomedWolfViewportHeight / 2.0f, 
                viewportLeft + zoomedWolfViewportWidth, 
                viewportTop + zoomedWolfViewportHeight - (float)Constants.CanvasZoom
            ),
            new SKPaint { Style = SKPaintStyle.Fill, Color = floor });
        canvas.DrawBitmap(_bitmap, new SKRect(viewportLeft, viewportTop, viewportLeft+zoomedWolfViewportWidth, viewportTop + zoomedWolfViewportHeight));
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            StrokeWidth = 5f,
            StrokeCap = SKStrokeCap.Round,
            TextAlign = SKTextAlign.Center,
            TextSize = 24,
        };

        var surfaceSize = e.Info.Size;
        canvas.DrawText($"{fps:0.00}fps", surfaceSize.Width / 2f, surfaceSize.Height - 10f, paint);
    }
    
    public override void OnKeyDown(ControlState controlState)
    {
        if (controlState != ControlState.None)
        {
            _game = _game with { ControlState = _game.ControlState ^ controlState };
        }
    }

    public override void OnKeyUp(ControlState controlState)
    {
        if (controlState != ControlState.None)
        {
            _game = _game with { ControlState = _game.ControlState ^ controlState };
        }
    }
}