using System.Collections.Immutable;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public record TrackingRayCastResult(
    bool IsComplete,
    bool IsHit, (double x, double y) DeltaDistance,
    (double x, double y) TotalSideDistance,
    (int x, int y) MapHit,
    Side Side,
    ImmutableArray<(int x, int y)> MapSquaresTested)
        : RayCastResult(IsComplete, IsHit, DeltaDistance, TotalSideDistance, MapHit, Side);

public class StepRayCaster : AbstractRayCaster
{
    private TrackingRayCastResult? _result;
    
    public void Stop()
    {
        _result = null;
    }

    private void Start(RayCastParameters parameters)
    {
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        var deltaDistX = parameters.Direction.X == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.X);
        var deltaDistY = parameters.Direction.Y == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.Y);
        var initialSideDistX =
            parameters.Direction.X < 0.0
                ? (parameters.From.X - initialMapX) * deltaDistX
                : (initialMapX + 1.0 - parameters.From.X) * deltaDistX;
        var initialSideDistY =
            parameters.Direction.Y < 0.0
                ? (parameters.From.Y - initialMapY) * deltaDistY
                : (initialMapY + 1.0 - parameters.From.Y) *deltaDistY;
        
        _result = new TrackingRayCastResult(
            IsComplete: false,
            IsHit: false,
            DeltaDistance: (deltaDistX, deltaDistY),
            TotalSideDistance: (initialSideDistX, initialSideDistY),
            MapHit: (initialMapX, initialMapY),
            Side: Side.NorthSouth,
            MapSquaresTested: ImmutableArray.Create((initialMapX, initialMapY))
        );
    }
    
    public TrackingRayCastResult? Result => _result;
    
    public override RayCastResult Cast(GameState game, RayCastParameters parameters,
        Func<RayCastResult, bool> shouldContinueFunc)
    {
        if (_result == null) Start(parameters);
        
        var stepX = parameters.Direction.X < 0.0 ? -1 : 1;
        var stepY = parameters.Direction.Y < 0.0 ? -1 : 1;
        
        if (shouldContinueFunc(_result!))
        {
            var xIsLessThanY = _result!.TotalSideDistance.x < _result.TotalSideDistance.y;
            var (newMapX, newMapY, newSide, newSideDistX, newSideDistY) =
                xIsLessThanY
                    ? (_result.MapHit.x + stepX, _result.MapHit.y, Side.NorthSouth, _result.TotalSideDistance.x + _result.DeltaDistance.x, _result.TotalSideDistance.y)
                    : (_result.MapHit.x, _result.MapHit.y + stepY, Side.EastWest, _result.TotalSideDistance.x, _result.TotalSideDistance.y + _result.DeltaDistance.y);
            var newIsHit = game.Map[newMapY][newMapX] switch
            {
                TurningPoint => parameters.IncludeTurningPoints,
                Empty => false,
                Wall => true,
                Door door => IsDoorHit(
                    (stepX,stepY),
                    parameters,
                    (newMapX,newMapY),
                    newSide,
                    game.Doors[door.DoorIndex]
                ),
                _ => false
            };
            var updatedResult =
                _result with
                {
                    IsHit = newIsHit,
                    TotalSideDistance = (newSideDistX, newSideDistY),
                    MapHit = (newMapX, newMapY),
                    Side = newSide,
                    MapSquaresTested = _result.MapSquaresTested.Add((newMapX, newMapY))
                };
            _result = updatedResult with {IsComplete = !shouldContinueFunc(updatedResult)};
        }

        return _result!;
    }
}