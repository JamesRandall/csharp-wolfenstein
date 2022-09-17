using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public class RayCaster : AbstractRayCaster
{
    public override RayCastResult Cast(GameState game, RayCastParameters parameters,
        Func<RayCastResult, bool> shouldContinueFunc)
    {
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        var deltaDistX = parameters.Direction.x == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.x);
        var deltaDistY = parameters.Direction.y == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.y);
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
        var (stepX, initialSideDistX) =
            parameters.Direction.x < 0.0
                ? (-1, (parameters.From.x - initialMapX) * deltaDistX)
                : (1, (initialMapX + 1.0 - parameters.From.x) * deltaDistX);
        var (stepY, initialSideDistY) =
            parameters.Direction.y < 0.0
                ? (-1, (parameters.From.y - initialMapY) * deltaDistY)
                : (1, (initialMapY + 1.0 - parameters.From.y) * deltaDistY);
        
        // TODO: the use of raycastresult like this is a dogs dinner at the moment but will get things going for now
        var result = new RayCastResult(
            IsComplete: false,
            IsHit: false,
            DeltaDistance: (deltaDistX, deltaDistY),
            TotalSideDistance: (initialSideDistX, initialSideDistY),
            MapHit: (initialMapX, initialMapY),
            initialSideDistX < initialSideDistY ? Side.NorthSouth : Side.EastWest
        );
        while (shouldContinueFunc(result))
        {
            var (newMapX, newMapY, newSide, newSideDistX, newSideDistY) =
                result.TotalSideDistance.x < result.TotalSideDistance.y
                    ? (result.MapHit.x + stepX, result.MapHit.y, Side.NorthSouth, result.TotalSideDistance.x + deltaDistX, result.TotalSideDistance.y)
                    : (result.MapHit.x, result.MapHit.y + stepY, Side.EastWest, result.TotalSideDistance.x, result.TotalSideDistance.y + deltaDistY);
            var newIsHit = game.Map[newMapY][newMapX] switch
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
            result = new RayCastResult(
                IsComplete: false,
                IsHit: newIsHit,
                DeltaDistance: (deltaDistX, deltaDistY),
                TotalSideDistance: (newSideDistX, newSideDistY),
                MapHit: (newMapX, newMapY),
                Side: newSide
            );
        }

        return result with { IsComplete = true };
    }
}