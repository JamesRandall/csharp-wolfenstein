using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine.RayCasting;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace CSharpWolfenstein.Engine;

public class RayCastDemonstrationEngine : AbstractGameEngine
{
    private const int StartingStripToDraw = 0;
    const int GridSize = 32;
    private ViewportRenderer _viewportRenderer;
    private double _speed;
    private double _timeUntilNextStep;
    private readonly FrameTimer _frameTimer = new();
    private readonly SKBitmap _bitmap = new(Constants.WolfViewportWidth, Constants.WolfViewportHeight);
    private int _stripToDraw = StartingStripToDraw;
    private bool _isFirstRender = true;
    private double _rayHitX = 0.0;
    private double _rayHitY = 0.0;
    private int _holdForFrame = 0;
    private bool _moveToNextRay = false;
    
    public RayCastDemonstrationEngine(GameEngine gameEngine, ViewportRenderer viewportRenderer)
    {
        _viewportRenderer = viewportRenderer;
        GameState = gameEngine.GameState;
        AssetPack = gameEngine.AssetPack;
        _timeUntilNextStep = _speed = viewportRenderer.RayCaster is StepRayCaster ? 500.0 : 0.0;
    }
    
    public GameState GameState { get; }

    public AssetPack AssetPack { get; }

    public override void NewFrame(SKPaintSurfaceEventArgs e)
    {
        var (delta,fps) = _frameTimer.GetCurrentTimings();
        Render(e, delta, fps);
    }

    private (float,float) GetViewportPosition()
    {
        var left = 0.0;
        var top = 0.0;
        return ((float)left, (float)top);
    }
    
    private ((float x, float y) mapPosition, (int x, int y) mapFrom, (int x, int y) mapTo)
        GetMapMetrics(float viewportLeft, float viewportTop, SKImageInfo info)
    {
        var mapPosition = (x: viewportLeft, y: viewportTop + Constants.WolfViewportHeight + 2.0f);
        var tilesAcross = (info.Width - 2) / GridSize;
        var tilesDown = (info.Height - Constants.WolfViewportHeight - 2) / GridSize;
        var playerPos = GameState.Camera.Position.ToMap();
        var mapFrom = (x: playerPos.x - tilesAcross / 2, y: playerPos.y - tilesDown / 2);
        var mapTo = (x: mapFrom.x + tilesAcross, y: mapFrom.y + tilesDown);
        return (mapPosition, mapFrom, mapTo);
    }

    private void ConfigureDirectRay(float viewportLeft, float viewportTop, SKImageInfo info)
    {
        var (mapPosition, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportTop, info);
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
        var rayCastResult = rayCaster.Cast(GameState, parameters, WallRenderer.ShouldContinueCast);
        var doorDistanceModifier = GameState.Map[rayCastResult.MapHit.y][rayCastResult.MapHit.x] switch
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
        
        float x1 = (rayCastResult.MapHit.x - mapFrom.x) * GridSize + (int)mapPosition.x;
        float y1 = (rayCastResult.MapHit.y - mapFrom.y) * GridSize + (int) mapPosition.y;
        
        if (rayCastResult.Side == Side.EastWest)
        {
            _rayHitX = x1 + clampedWallX * GridSize;
            _rayHitY = rayDirection.y >= 0 ? y1 + (GridSize*doorDistanceModifier) : y1 - GridSize*doorDistanceModifier + GridSize;
        }
        else
        {
            _rayHitY = y1 + clampedWallX * GridSize;
            _rayHitX = rayDirection.x <= 0 ? x1 + (GridSize - GridSize*doorDistanceModifier) : x1 + GridSize*doorDistanceModifier;
        }
    }
    
    private void Render(SKPaintSurfaceEventArgs e, double delta, double fps)
    {
        var (viewportLeft, viewportTop) = GetViewportPosition();
        var canvas = e.Surface.Canvas;
        bool movedToNextRay = _isFirstRender;
        if (_isFirstRender)
        {
            RenderBackground(canvas, viewportLeft, viewportTop);
            _isFirstRender = false;
        }
        
        _timeUntilNextStep -= delta;
        if (_timeUntilNextStep < 0.0)
        {
            StepRayCaster? stepRayCaster = _viewportRenderer.RayCaster as StepRayCaster;
            
            if (_holdForFrame > 0)
            {
                _holdForFrame--;
                if (_holdForFrame == 0)
                {
                    _moveToNextRay = true;
                }
            }
            
            if (_holdForFrame == 0)
            {
                if (_moveToNextRay)
                {
                    _moveToNextRay = false;
                    stepRayCaster?.Stop();
                    if (_stripToDraw < Constants.WolfViewportWidth)
                    {
                        _stripToDraw++;
                        movedToNextRay = true;
                    }
                }
                
                // we only advance the ray caster on a timed basis
                _timeUntilNextStep += _speed;
                {
                    unsafe
                    {
                        fixed (uint* ptr = _viewportRenderer.UpdateFrameBuffer(AssetPack, GameState,
                                   (Constants.WolfViewportWidth, Constants.WolfViewportHeight), _stripToDraw))
                        {
                            _bitmap.SetPixels((IntPtr) ptr);
                        }
                    }
                    if (movedToNextRay)
                    {
                        RenderMap(viewportLeft, viewportTop, e.Info, canvas);
                        ConfigureDirectRay(viewportLeft, viewportLeft, e.Info);
                    }
                    RenderRay(viewportLeft, viewportTop, e.Info, canvas);
                    
                    if (stepRayCaster != null) RenderMapSteps(viewportLeft, viewportTop, stepRayCaster.Result!, e.Info, canvas);
                    canvas.DrawBitmap(_bitmap,
                        new SKRect(viewportLeft, viewportTop, viewportLeft + Constants.WolfViewportWidth,
                            viewportTop + Constants.WolfViewportHeight));

                    if (stepRayCaster?.Result is not null)
                    { 
                        if (stepRayCaster.Result.IsComplete)
                        {
                            _holdForFrame = 4;
                        }
                    }
                    else
                    {
                        _moveToNextRay = true;
                    }
                }
            }

        }
    }

    private static void RenderBackground(SKCanvas canvas, float viewportLeft, float viewportTop)
    {
        canvas.Clear(new SKColor(0xF0, 0xF0, 0xF0));
        var backgroundFill = new SKPaint
            {Color = new SKColor(0xF0, 0xF0, 0xF0), Style = SKPaintStyle.Fill};
        canvas.DrawRect(0.0f, 0.0f, Constants.WolfViewportWidth, Constants.WolfViewportHeight,
            backgroundFill);
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

    void RenderMapSteps(float viewportLeft, float viewportTop, TrackingRayCastResult rayCastResult, SKImageInfo info, SKCanvas canvas)
    {
        var (position, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportTop, info);
        
        var linePaint = new SKPaint {Color = SKColors.ForestGreen, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        foreach (var mapPosition in rayCastResult.MapSquaresTested.Skip(1))
        {
            var (x, y) = mapPosition;
            float x1 = (x - mapFrom.x) * GridSize + (int)position.x;
            float y1 = (y - mapFrom.y) * GridSize + (int) position.y;
            canvas.DrawRect(x1, y1, GridSize, GridSize, linePaint);
        }
    }

    void RenderRay(float viewportLeft, float viewportTop, SKImageInfo info, SKCanvas canvas)
    {
        var (position, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportTop, info);
        var cameraPosition = GameState.Camera.Position;
        var relativePlayerX = cameraPosition.X - mapFrom.x;
        var relativePlayerY = cameraPosition.Y - mapFrom.y;
        var centerX = position.x + relativePlayerX * GridSize;
        var centerY = position.y + relativePlayerY * GridSize;
        
        var toX = _rayHitX;
        var toY = _rayHitY;
        var linePaint = new SKPaint {Color = SKColors.LightSeaGreen, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f};
        
        canvas.DrawLine((float)centerX, (float)centerY, (float)toX, (float)toY, linePaint);
    }

    void RenderMap(float viewportLeft, float viewportTop, SKImageInfo info, SKCanvas canvas)
    {
        var (position, mapFrom, mapTo) = GetMapMetrics(viewportLeft, viewportTop, info);
        var map = GameState.Map;
        bool WallInDirection(MapDirection direction, (int x, int y) currentPosition)
        {
            var newX = direction.ToDelta().x + currentPosition.x;
            var newY = direction.ToDelta().y + currentPosition.y;
            if (newX < 0 || newX >= Constants.MapSize || newY < 0 || newY >= Constants.MapSize) return true;
            return map[newY][newX] switch
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
            position.x, 
            position.y, 
            (float)GridSize * (mapTo.x - mapFrom.x),
            (float)GridSize * (mapTo.y - mapFrom.y),
            backgroundFill
        );
        
        for (int y = mapFrom.y; y <= mapTo.y; y++)
        {
            for (int x = mapFrom.x; x <= mapTo.x; x++)
            {
                float x1 = (x - mapFrom.x) * GridSize + (int)position.x;
                float y1 = (y - mapFrom.y) * GridSize + (int) position.y;
                float x2 = x1 + GridSize;
                float y2 = y1 + GridSize;
                Cell cell =
                    x >= 0 && x <= Constants.MapSize && y >= 0 && y < Constants.MapSize
                        ? map[y][x]
                        : new Empty((x,y));
                
                if (cell is Wall)
                {
                    canvas.DrawRect(x1 + 1.0f, y1 + 1.0f, GridSize-2.0f, GridSize-2.0f, wallFill);
                    if (!WallInDirection(MapDirection.East, (x, y)))
                    {
                        canvas.DrawLine(x2,y1,x2,y2, linePaint);
                    }
                    if (!WallInDirection(MapDirection.North, (x, y)))
                    {
                        canvas.DrawLine(x1,y1,x2,y1, linePaint);
                    }
                    if (!WallInDirection(MapDirection.West, (x, y)))
                    {
                        canvas.DrawLine(x1,y1,x1,y2, linePaint);
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

        var cameraPosition = GameState.Camera.Position;
        var relativePlayerX = cameraPosition.X - mapFrom.x;
        var relativePlayerY = cameraPosition.Y - mapFrom.y;
        var centerX = position.x + relativePlayerX * GridSize;
        var centerY = position.y + relativePlayerY * GridSize;
        canvas.DrawCircle((float)centerX, (float)centerY, GridSize/4.0f, playerPaint);
    }

    public override void OnKeyDown(ControlState controlState)
    {
        if (controlState == ControlState.Action)
        {
            _viewportRenderer =
                _viewportRenderer.RayCaster is RayCaster
                    ? new ViewportRenderer(WallRenderer.RenderWalls, new StepRayCaster())
                    : new ViewportRenderer(WallRenderer.RenderWalls, new RayCaster());
            _timeUntilNextStep = _speed = _viewportRenderer.RayCaster is StepRayCaster ? 500.0 : 0.0;
        }
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