using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine.RayCasting;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace CSharpWolfenstein.Engine;

public class RayCastDemonstrationEngine : AbstractGameEngine
{
    private const int StartingStripToDraw = 0; // 308; //Constants.WolfViewportWidth/2; // 0
    const int GridSize = 32;
    private readonly ViewportRenderer _viewportRenderer;
    private double _speed;
    private double _timeUntilNextStep;
    private readonly FrameTimer _frameTimer = new();
    private readonly SKBitmap _bitmap = new(Constants.WolfViewportWidth, Constants.WolfViewportHeight);
    private int _stripToDraw = StartingStripToDraw;
    private bool _isFirstRender = true;
    private double _rayHitX = 0.0;
    private double _rayHitY = 0.0;
    private bool _showDda = true;
    
    public RayCastDemonstrationEngine(GameEngine gameEngine, ViewportRenderer viewportRenderer)
    {
        _viewportRenderer = viewportRenderer;
        GameState = gameEngine.GameState;
        AssetPack = gameEngine.AssetPack;
        _timeUntilNextStep = _speed = _showDda ? 500.0 : 0.0;
    }
    
    public GameState GameState { get; }

    public AssetPack AssetPack { get; }

    public override void NewFrame(SKPaintSurfaceEventArgs e)
    {
        var (delta,fps) = _frameTimer.GetCurrentTimings();
        Render(e, delta, fps);
        Update(delta);
    }

    private void Update(double delta)
    {
        
    }
    
    private (float,float) GetViewportPosition(SKSizeI surfaceSize)
    {
        var left = 0.0; // surfaceSize.Width / 2.0 - Constants.WolfViewportWidth / 2.0;
        var top = 0.0;
        return ((float)left, (float)top);
    }
    
    private ((float x, float y) mapPosition, (int x, int y) mapFrom, (int x, int y) mapTo)
        GetMapMetrics(float viewportLeft, float viewportTop, SKImageInfo info)
    {
        var mapPosition = (x: viewportLeft, y: viewportTop + Constants.WolfViewportHeight + 2.0f);
        var tilesAcross = (info.Width - 2) / GridSize;
        var tilesDown = (info.Height - Constants.WolfViewportHeight - 2) / GridSize;
        var playerPos = GameState.Camera.Position.ToMap().FlipHorizontal();
        var mapFrom = (x: playerPos.x - tilesAcross / 2, y: playerPos.y - tilesDown / 2);
        var mapTo = (x: mapFrom.x + tilesAcross, y: mapFrom.y + tilesDown);
        return (mapPosition, mapFrom, mapTo);
    }

    private void ConfigureDirectRay(float viewportLeft, float viewportTop, SKImageInfo info)
    {
        var (mapPosition, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportLeft, info);
        // The cast needs to take place in game co-ordinates
        var rayCaster = new RayCaster();
        var cameraX = 2.0 * _stripToDraw / Constants.WolfViewportWidth - 1.0;
        var rayDirection = (
            x:GameState.Camera.Direction.X + GameState.Camera.Plane.X * cameraX,
            y:GameState.Camera.Direction.Y + GameState.Camera.Plane.Y * cameraX
        );
        var parameters = new RayCastParameters(
            IncludeTurningPoints: false,
            From: (GameState.Camera.Position.X, GameState.Camera.Position.Y),
            Direction: rayDirection
        );
        var rayCastResult = rayCaster.Cast(parameters, WallRenderer.ShouldContinueCast, GameState);
        var doorDistanceModifier = GameState.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] switch
        {
            Door => 0.5,
            _ => 0.0
        };
        var perpendicularWallDistance =
            (rayCastResult.Side == Side.NorthSouth
                ? rayCastResult.TotalSideDistance.x - rayCastResult.DeltaDistance.x
                : rayCastResult.TotalSideDistance.y - rayCastResult.DeltaDistance.y) + doorDistanceModifier;
        var wallX = rayCastResult.Side == Side.NorthSouth
            ? GameState.Camera.Position.Y + perpendicularWallDistance * rayDirection.y
            : GameState.Camera.Position.X + perpendicularWallDistance * rayDirection.x;
        var clampedWallX = wallX - Math.Floor(wallX);
        
        // We then need to move these to flipped map co-ordinates
        float x1 = (rayCastResult.MapHit.FlipHorizontal().x + 1 - mapFrom.x) * GridSize + (int)mapPosition.x;
        float y1 = (rayCastResult.MapHit.y - mapFrom.y) * GridSize + (int) mapPosition.y;
        
        
        
        if (rayCastResult.Side == Side.EastWest)
        {
            _rayHitX = x1 - clampedWallX * GridSize;
            _rayHitY = rayDirection.y >= 0 ? y1 : y1 + GridSize;
        }
        else
        {
            _rayHitY = y1 + clampedWallX * GridSize;
            _rayHitX = rayDirection.x <= 0 ? x1 - (GridSize - GridSize*doorDistanceModifier) : x1 - GridSize*doorDistanceModifier; // <= because we're in flipped x mode
        }
    }

    private void Render(SKPaintSurfaceEventArgs e, double delta, double fps)
    {
        if (_isFirstRender)
        {
            e.Surface.Canvas.Clear(new SKColor(0xF0, 0xF0, 0xF0));
            _isFirstRender = false;
        }
        
        void RefreshMap(float viewportLeft, float viewportTop, SKCanvas skCanvas)
        {
            var (mapPosition, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportLeft, e.Info);
            RenderMap(skCanvas, mapPosition, mapFrom, mapTo);
        }

        void RefreshRay(float viewportLeft, float viewportTop, RayCastResult rayCastResult, SKCanvas skCanvas)
        {
            var (mapPosition, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportLeft, e.Info);
            RenderRay(skCanvas, mapPosition, mapFrom, mapTo, rayCastResult);
        }

        void RefreshDdaSteps(float viewportLeft, float viewportTop, TrackingRayCastResult rayCastResult, SKCanvas skCanvas)
        {
            var (mapPosition, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportLeft, e.Info);
            RenderDdaSteps(skCanvas, mapPosition, mapFrom, mapTo, rayCastResult);
        }
        
        _timeUntilNextStep -= delta;
        if (_timeUntilNextStep < 0.0)
        {
            var canvas = e.Surface.Canvas;
            StepRayCaster rayCaster = (StepRayCaster) _viewportRenderer.RayCaster;
            bool rayCasterInitialisedThisFrame = rayCaster.Result == null;
            // we only advance the ray caster on a timed basis
            _timeUntilNextStep += _speed;
            {
                
                unsafe
                {
                    // Their is a good article on the different ways to update pixel data here:
                    //  https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/bitmaps/pixel-bits
                    // Having tried them all maintaining and then setting the pixel byte array is the most performant for us.
                    fixed (uint* ptr = _viewportRenderer.UpdateFrameBuffer(AssetPack, GameState,
                               (Constants.WolfViewportWidth, Constants.WolfViewportHeight), _stripToDraw))
                    {
                        _bitmap.SetPixels((IntPtr) ptr);
                    }
                }

                var (viewportLeft, viewportTop) = GetViewportPosition(e.Info.Size);
                if (_stripToDraw == StartingStripToDraw)
                {
                    var backgroundFill = new SKPaint {Color = new SKColor(0xF0, 0xF0, 0xF0), Style = SKPaintStyle.Fill};
                    canvas.DrawRect(0.0f, 0.0f, Constants.WolfViewportWidth, Constants.WolfViewportHeight, backgroundFill);
                    SKColor ceiling = new SKColor(0x39, 0x39, 0x39);
                    SKColor floor = new SKColor(0x73, 0x73, 0x73);
                    canvas.DrawRect(
                        new SKRect(viewportLeft, viewportTop, viewportLeft + Constants.WolfViewportWidth,
                            viewportTop + Constants.WolfViewportHeight),
                        new SKPaint {Style = SKPaintStyle.Fill, Color = ceiling});
                    canvas.DrawRect(
                        new SKRect(
                            viewportLeft,
                            viewportTop + Constants.WolfViewportHeight / 2.0f,
                            viewportLeft + Constants.WolfViewportWidth,
                            viewportTop + Constants.WolfViewportHeight
                        ),
                        new SKPaint {Style = SKPaintStyle.Fill, Color = floor});
                }

                if (rayCasterInitialisedThisFrame)
                {
                    RefreshMap(viewportLeft, viewportTop, canvas);
                    ConfigureDirectRay(viewportLeft, viewportLeft, e.Info);
                }
                RefreshRay(viewportLeft, viewportTop, rayCaster.Result!, canvas);
                if (_showDda) RefreshDdaSteps(viewportLeft, viewportTop, rayCaster.Result!, canvas);
                canvas.DrawBitmap(_bitmap,
                    new SKRect(viewportLeft, viewportTop, viewportLeft + Constants.WolfViewportWidth,
                        viewportTop + Constants.WolfViewportHeight));

                if (rayCaster.Result?.IsComplete ?? false)
                {
                    //RefreshMap(viewportLeft, viewportTop, canvas);
                    rayCaster.Stop();
                    if (_stripToDraw < Constants.WolfViewportWidth)
                    {
                        _stripToDraw++;
                    }
                }
            }
        }
    }

    private void RenderDdaSteps(SKCanvas canvas, (float left, float top) position, (int x, int y) mapFrom, (int x, int y) mapTo, TrackingRayCastResult rayCastResult)
    {
        var cameraPosition = GameState.Camera.Position.FlipHorizontal();
        var relativePlayerX = cameraPosition.X - mapFrom.x + 1.0f;
        var relativePlayerY = cameraPosition.Y - mapFrom.y;
        var centerX = position.left + relativePlayerX * GridSize;
        var centerY = position.top + relativePlayerY * GridSize;
        
        var linePaint = new SKPaint {Color = SKColors.ForestGreen, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        foreach (var (stepX, stepY) in rayCastResult.Steps)
        {
            var pixelStepX = (float)stepX * GridSize;
            var pixelStepY = (float)stepY * GridSize;
            canvas.DrawLine(centerX, centerY, centerX + pixelStepX, centerY + pixelStepY, linePaint);
            centerX += pixelStepX;
            centerY += pixelStepY;
        }
    }

    private void RenderRay(SKCanvas canvas, (float left, float top) position, (int x, int y) mapFrom,
        (int x, int y) mapTo, RayCastResult rayCastResult)
    {
        var cameraPosition = GameState.Camera.Position.FlipHorizontal();
        var relativePlayerX = cameraPosition.X - mapFrom.x + 1.0f;
        var relativePlayerY = cameraPosition.Y - mapFrom.y;
        var centerX = position.left + relativePlayerX * GridSize;
        var centerY = position.top + relativePlayerY * GridSize;
        
        //var relativePlayerX = GameState.Camera.Position.X - mapFrom.x;
        //var relativePlayerY = GameState.Camera.Position.Y - mapFrom.y;
        //var centerX = position.left + relativePlayerX * GridSize;
        //var centerY = position.top + relativePlayerY * GridSize;
        // The ray caster uses max values to force comparison tests on near zero numbers where their is a precision risk
        /*var rayCastX = rayCastResult.TotalSideDistance.x > 128.0 ? 0.0 : rayCastResult.TotalSideDistance.x;
        var rayCastY = rayCastResult.TotalSideDistance.y > 128.0 ? 0.0 : rayCastResult.TotalSideDistance.y;
        var toX = centerX + rayCastX * -1.0 * GridSize;
        var toY = centerY + rayCastY * GridSize;*/
        var toX = _rayHitX;
        var toY = _rayHitY;
        var linePaint = new SKPaint {Color = SKColors.LightSeaGreen, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        
        canvas.DrawLine(centerX, centerY, (float)toX, (float)toY, linePaint);
    }

    private void RenderMap(SKCanvas canvas, (float left, float top) position, (int x, int y) mapFrom, (int x, int y) mapTo)
    {
        var map = GameState.Map.FlipHorizontal();
        bool WallInDirection(MapDirection direction, (int x, int y) currentPosition)
        {
            var newX = direction.ToDelta().x + currentPosition.x;
            var newY = direction.ToDelta().y + currentPosition.y;
            if (newX < 0 || newX >= Constants.MapSize || newY < 0 || newY >= Constants.MapSize) return true;
            return map[newY, newX] switch
            {
                Wall => true,
                _ => false
            };
        }

        var backgroundFill = new SKPaint {Color = new SKColor(0xF0, 0xF0, 0xF0), Style = SKPaintStyle.Fill};
        var wallFill = new SKPaint {Color = SKColors.White, Style = SKPaintStyle.Fill };
        var linePaint = new SKPaint {Color = SKColors.Orange, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        var doorPaint = new SKPaint {Color = SKColors.DodgerBlue, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        var playerPaint = new SKPaint {Color = SKColors.MediumSeaGreen, Style = SKPaintStyle.Fill};
        
        canvas.DrawRect(
            position.left, 
            position.top, 
            (float)GridSize * (mapTo.x - mapFrom.x),
            (float)GridSize * (mapTo.y - mapFrom.y),
            backgroundFill
        );
        
        for (int y = mapFrom.y; y <= mapTo.y; y++)
        {
            for (int x = mapFrom.x; x <= mapTo.x; x++)
            {
                float x1 = (x - mapFrom.x) * GridSize + (int)position.left;
                float y1 = (y - mapFrom.y) * GridSize + (int) position.top;
                float x2 = x1 + GridSize;
                float y2 = y1 + GridSize;
                Cell cell =
                    x >= 0 && x <= Constants.MapSize && y >= 0 && y < Constants.MapSize
                        ? map[y, x]
                        : new Empty((x,y));
                
                if (cell is Wall)
                {
                    canvas.DrawRect(x1 + 1.0f, y1 + 1.0f, GridSize-2.0f, GridSize-2.0f, wallFill);
                    //canvas.DrawLine(x1,y1,x1,y2, linePaint);
                    //canvas.DrawLine(x1,y1,x2,y1, linePaint);
                    //canvas.DrawLine(x1,y1,x2,y1, linePaint);
                    //canvas.DrawLine(x2,y1,x2,y2, linePaint);
                    
                    if (!WallInDirection(MapDirection.East, (x, y)))
                    {
                        canvas.DrawLine(x1,y1,x1,y2, linePaint);
                    }
                    if (!WallInDirection(MapDirection.North, (x, y)))
                    {
                        canvas.DrawLine(x1,y1,x2,y1, linePaint);
                    }
                    if (!WallInDirection(MapDirection.West, (x, y)))
                    {
                        canvas.DrawLine(x2,y1,x2,y2, linePaint);
                    }
                    if (!WallInDirection(MapDirection.South, (x, y)))
                    {
                        canvas.DrawLine(x1,y2,x2,y2, linePaint);
                    }
                }
                else if (cell is Door door)
                {
                    DoorState doorState = GameState.Doors[door.DoorIndex];
                    if (doorState.DoorDirection == DoorDirection.EastWest)
                    {
                        canvas.DrawLine(x1,y1+GridSize/2.0f,x2,y1+GridSize/2.0f, doorPaint);
                    }
                    else if (doorState.DoorDirection == DoorDirection.NorthSouth)
                    {
                        canvas.DrawLine(x1+GridSize/2.0f,y1,x1+GridSize/2.0f,y2, doorPaint);
                    }
                }

                var font = new SKFont {Size = 8.0f};
                var textPaint = new SKPaint {Color = SKColors.DimGray,Style = SKPaintStyle.StrokeAndFill };
                canvas.DrawText($"{x},{y}",x1+2.0f,y1+10.0f, font, textPaint);
            }
        }

        var cameraPosition = GameState.Camera.Position.FlipHorizontal();
        var relativePlayerX = cameraPosition.X - mapFrom.x + 1.0f;
        var relativePlayerY = cameraPosition.Y - mapFrom.y;
        var centerX = position.left + relativePlayerX * GridSize;
        var centerY = position.top + relativePlayerY * GridSize;
        canvas.DrawCircle(centerX, centerY, GridSize/4.0f, playerPaint);
    }

    public override void OnKeyDown(ControlState controlState)
    {
        
    }

    public override void OnKeyUp(ControlState controlState)
    {
        if (controlState == ControlState.Forward)
        {
            _speed -= 50.0;
            if (_speed <= 0.0) _speed = 0.0;
        }
        else if (controlState == ControlState.Backward)
        {
            _speed += 50.0;
        }
    }
}