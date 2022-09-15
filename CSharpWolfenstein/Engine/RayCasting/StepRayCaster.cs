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
    ImmutableArray<(double x, double y)> Steps) : RayCastResult(IsComplete, IsHit, DeltaDistance, TotalSideDistance, MapHit,
    Side);

public class StepRayCaster : AbstractRayCaster
{
    private TrackingRayCastResult? _result;
    
    public void Stop()
    {
        _result = null;
    }
    
    public void Start(RayCastParameters parameters)
    {
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        var deltaDistX = parameters.Direction.x == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.x);
        //var deltaDistX = Math.Sqrt(1 + parameters.Direction.y * parameters.Direction.y / (parameters.Direction.x * parameters.Direction.x));
        var deltaDistY = parameters.Direction.y == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.y);
        //var deltaDistY = Math.Sqrt(1 + (parameters.Direction.x * parameters.Direction.x) / (parameters.Direction.y * parameters.Direction.y)); 
        var initialSideDistX =
            parameters.Direction.x < 0.0
                ? (parameters.From.x - initialMapX) * deltaDistX
                : (initialMapX + 1.0 - parameters.From.x) * deltaDistX;
        var initialSideDistY =
            parameters.Direction.y < 0.0
                ? (parameters.From.y - initialMapY) * deltaDistY
                : (initialMapY + 1.0 - parameters.From.y) *deltaDistY;
        
        _result = new TrackingRayCastResult(
            IsComplete: false,
            IsHit: false,
            DeltaDistance: (deltaDistX, deltaDistY),
            TotalSideDistance: (initialSideDistX, initialSideDistY),
            MapHit: (initialMapX, initialMapY),
            Side: Side.NorthSouth,
            Steps: ImmutableArray<(double,double)>.Empty);
    }

    public TrackingRayCastResult? Result => _result;

    
    public override RayCastResult Cast(RayCastParameters parameters, Func<RayCastResult, bool> shouldContinueFunc, GameState game)
    {
        if (_result == null) Start(parameters);
        //if (!_canStep) return _result!;
        
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        
        var halfStepDeltaDistX =
            parameters.Direction.x == 0.0
                ? double.MaxValue
                : Math.Sqrt(1.0 + parameters.Direction.y * parameters.Direction.y / parameters.Direction.x *
                    parameters.Direction.x);
        var halfStepDeltaDistY =
            parameters.Direction.y == 0.0
                ? double.MaxValue
                : Math.Sqrt(1.0 + parameters.Direction.x * parameters.Direction.x / parameters.Direction.y *
                    parameters.Direction.y);
        var stepX = parameters.Direction.x < 0.0 ? -1 : 1;
        var stepY = parameters.Direction.y < 0.0 ? -1 : 1;
        
        if (shouldContinueFunc(_result!))
        {
            var xIsLessThanY = _result!.TotalSideDistance.x < _result.TotalSideDistance.y;
            var (newMapX, newMapY, newSide, newSideDistX, newSideDistY) =
                xIsLessThanY
                    ? (_result.MapHit.x + stepX, _result.MapHit.y, Side.NorthSouth, _result.TotalSideDistance.x + _result.DeltaDistance.x, _result.TotalSideDistance.y)
                    : (_result.MapHit.x, _result.MapHit.y + stepY, Side.EastWest, _result.TotalSideDistance.x, _result.TotalSideDistance.y + _result.DeltaDistance.y);
            var newIsHit = game.Map[newMapY, newMapX] switch
            {
                TurningPoint => parameters.IncludeTurningPoints,
                Empty => false,
                Wall => true,
                Door door => IsDoorHit(
                    (halfStepDeltaDistX,halfStepDeltaDistY),
                    (stepX,stepY),
                    parameters,
                    (newMapX,newMapY),
                    newSide,
                    game.Doors[door.DoorIndex]
                ),
                _ => false
            };
            _result = _result with
            {
                IsHit = newIsHit,
                TotalSideDistance = (newSideDistX, newSideDistY),
                MapHit = (newMapX, newMapY),
                Side = newSide,
                Steps =
                    xIsLessThanY
                    ? _result.Steps.Add((_result.DeltaDistance.x * stepX * -1, 0.0))
                    : _result.Steps.Add((0.0, _result.DeltaDistance.y * stepY))
            };
            _result = _result with {IsComplete = !shouldContinueFunc(_result)};
        }

        return _result!;
    }
}