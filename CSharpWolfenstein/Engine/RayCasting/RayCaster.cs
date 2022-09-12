using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public class RayCaster : AbstractRayCaster
{
    public override RayCastResult Cast(RayCastParameters parameters, Func<RayCastResult, bool> shouldContinueFunc, GameState game)
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
            TotalDistance: (initialSideDistX, initialSideDistY),
            MapHit: (initialMapX, initialMapY),
            Side.NorthSouth
        );
        while (shouldContinueFunc(result))
        {
            var (newMapX, newMapY, newSide, newSideDistX, newSideDistY) =
                result.TotalDistance.x < result.TotalDistance.y
                    ? (result.MapHit.x + stepX, result.MapHit.y, Side.NorthSouth, result.TotalDistance.x + deltaDistX, result.TotalDistance.y)
                    : (result.MapHit.x, result.MapHit.y + stepY, Side.EastWest, result.TotalDistance.x, result.TotalDistance.y + deltaDistY);
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
            result = new RayCastResult(
                IsComplete: false,
                IsHit: newIsHit,
                DeltaDistance: (deltaDistX, deltaDistY),
                TotalDistance: (newSideDistX, newSideDistY),
                MapHit: (newMapX, newMapY),
                Side: newSide
            );
        }

        return result with { IsComplete = true };
    }

    private static bool IsDoorHit(
        (double x, double y) halfStepDelta,
        (int x, int y) stepDelta,
        RayCastParameters parameters,
        (int x, int y) newMap,
        Side newSide,
        DoorState door)
    {
        const double tolerance = 0.0001;
        var (posX, posY) = parameters.From;
        var mapX2 = posX < newMap.x ? newMap.x - 1 : newMap.x;
        var mapY2 = posY < newMap.y ? newMap.x + 1 : newMap.y;
        var adjacent = newSide == Side.EastWest ? (double) mapY2 - posY : (double) mapX2 - posX + 1.0;
        var rayMultiplier = newSide == Side.EastWest
            ? adjacent / parameters.Direction.y
            : adjacent / parameters.Direction.x;
        var (rayPositionX, rayPositionY) =
            (posX + parameters.Direction.x * rayMultiplier, posY + parameters.Direction.y * rayMultiplier);
        var trueDeltaX = halfStepDelta.x < tolerance ? 100.0 : halfStepDelta.x;
        var trueDeltaY = halfStepDelta.y < tolerance ? 100.0 : halfStepDelta.y;

        if (newSide == Side.NorthSouth)
        {
            var trueYStep = Math.Sqrt(trueDeltaX * trueDeltaX - 1.0);
            var halfStepInY = rayPositionY + (stepDelta.y * trueYStep) / 2.0;
            return (Math.Abs(Math.Floor(halfStepInY) - newMap.y) < tolerance) && (halfStepInY - newMap.y) < (1.0 - door.Offset / 64.0);
        }
        // Side.EastWest
        var trueXStep = Math.Sqrt(trueDeltaY * trueDeltaY - 1.0);
        var halfStepInX = rayPositionX + (stepDelta.x * trueXStep) / 2.0;
        return (Math.Abs(Math.Floor(halfStepInX) - newMap.x) < tolerance) && (halfStepInX - newMap.x) < (1.0 - door.Offset / 64.0);
    }
}