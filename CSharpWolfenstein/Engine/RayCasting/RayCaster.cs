using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public class RayCaster : AbstractRayCaster
{
    public override RayCastResult Cast(GameState game, RayCastParameters parameters,
        Func<RayCastResult, bool> shouldContinueFunc)
    {
        var (initialMapX, initialMapY) = parameters.From.ToMap();
        var deltaDistX = parameters.Direction.X == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.X);
        var deltaDistY = parameters.Direction.Y == 0.0 ? double.MaxValue : Math.Abs(1.0 / parameters.Direction.Y);
        var (stepX, initialSideDistX) =
            parameters.Direction.X < 0.0
                ? (-1, (parameters.From.X - initialMapX) * deltaDistX)
                : (1, (initialMapX + 1.0 - parameters.From.X) * deltaDistX);
        var (stepY, initialSideDistY) =
            parameters.Direction.Y < 0.0
                ? (-1, (parameters.From.Y - initialMapY) * deltaDistY)
                : (1, (initialMapY + 1.0 - parameters.From.Y) * deltaDistY);
        
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