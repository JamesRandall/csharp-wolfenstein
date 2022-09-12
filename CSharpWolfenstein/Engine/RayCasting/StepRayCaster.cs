using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public class StepRayCaster : AbstractRayCaster
{
    private RayCastResult? _result;

    private bool _canStep = true;

    public void Stop()
    {
        _result = null;
        _canStep = true;
    }
    
    public void Start(RayCastParameters parameters)
    {
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        var deltaDistX = parameters.Direction.x == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.x);
        var deltaDistY = parameters.Direction.y == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.y);
        var initialSideDistX =
            parameters.Direction.x < 0.0
                ? (parameters.From.x - initialMapX) * deltaDistX
                : (initialMapX + 1.0 - parameters.From.x) * deltaDistX;
        var initialSideDistY =
            parameters.Direction.y < 0.0
                ? (parameters.From.y - initialMapY) * deltaDistY
                : (initialMapY + 1.0 - parameters.From.y) *deltaDistY;
        _result = new RayCastResult(
            IsComplete: false,
            IsHit: false,
            DeltaDistance: (deltaDistX, deltaDistY),
            TotalDistance: (initialSideDistX, initialSideDistY),
            MapHit: (initialMapX, initialMapY),
            Side.NorthSouth
        );
    }

    public RayCastResult? Result => _result;

    public void Tick()
    {
        _canStep = true;
    }
    
    public override RayCastResult Cast(RayCastParameters parameters, Func<RayCastResult, bool> shouldContinueFunc, GameState game)
    {
        if (_result == null) Start(parameters);
        if (!_canStep) return _result!;
        
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
        
        //while (shouldContinueFunc(_result))
        if (shouldContinueFunc(_result))
        {
            var (newMapX, newMapY, newSide, newSideDistX, newSideDistY) =
                _result.TotalDistance.x < _result.TotalDistance.y
                    ? (_result.MapHit.x + stepX, _result.MapHit.y, Side.NorthSouth, _result.TotalDistance.x + _result.DeltaDistance.x, _result.TotalDistance.y)
                    : (_result.MapHit.x, _result.MapHit.y + stepY, Side.EastWest, _result.TotalDistance.x, _result.TotalDistance.y + _result.DeltaDistance.y);
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
                TotalDistance = (newSideDistX, newSideDistY),
                MapHit = (newMapX, newMapY),
                Side = newSide,
            };
            _result = _result with {IsComplete = !shouldContinueFunc(_result)};
        }

        return _result;
    }
}