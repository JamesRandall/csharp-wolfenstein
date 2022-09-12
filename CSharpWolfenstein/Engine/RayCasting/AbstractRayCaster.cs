using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine;

public record RayCastParameters(
    bool IncludeTurningPoints,
    (double x, double y) From,
    (double x, double y) Direction
);

public record RayCastResult(
    bool IsComplete, // TODO: need to revist this, put in place to allow the wall drawing to essentially be async
    bool IsHit,
    (double x, double y) DeltaDistance,
    (double x, double y) TotalDistance,
    (int x, int y) MapHit,
    Side Side);

public abstract class AbstractRayCaster
{
    public abstract RayCastResult Cast(RayCastParameters parameters, Func<RayCastResult, bool> shouldContinueFunc,
        GameState game);
    
    protected static bool IsDoorHit(
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