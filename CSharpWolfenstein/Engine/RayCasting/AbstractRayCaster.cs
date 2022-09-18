using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine.RayCasting;

public record RayCastParameters(
    bool IncludeTurningPoints,
    Vector2D From,
    Vector2D Direction
);

public record RayCastResult(
    bool IsComplete, // TODO: need to revist this, put in place to allow the wall drawing to essentially be async
    bool IsHit,
    (double x, double y) DeltaDistance,
    (double x, double y) TotalSideDistance,
    (int x, int y) MapHit,
    Side Side);

public abstract class AbstractRayCaster
{
    public abstract RayCastResult Cast(GameState game, RayCastParameters parameters,
        Func<RayCastResult, bool> shouldContinueFunc);
    
    protected static bool IsDoorHit(
        (int x, int y) stepDelta,
        RayCastParameters parameters,
        (int x, int y) newMap,
        Side newSide,
        DoorState door)
    {
        var halfStepDeltaX =
            parameters.Direction.X == 0.0
                ? double.MaxValue
                : Math.Sqrt(1.0 + parameters.Direction.Y * parameters.Direction.Y / parameters.Direction.X *
                    parameters.Direction.X);
        var halfStepDeltaY =
            parameters.Direction.Y == 0.0
                ? double.MaxValue
                : Math.Sqrt(1.0 + parameters.Direction.X * parameters.Direction.X / parameters.Direction.Y *
                    parameters.Direction.Y);
        
        const double tolerance = 0.0001;
        var (posX, posY) = parameters.From;
        var mapX2 = posX < newMap.x ? newMap.x - 1 : newMap.x;
        var mapY2 = posY > newMap.y ? newMap.y + 1 : newMap.y;
        var adjacent = newSide == Side.EastWest ? mapY2 - posY : mapX2 - posX + 1.0;
        var rayMultiplier = newSide == Side.EastWest
            ? adjacent / parameters.Direction.Y
            : adjacent / parameters.Direction.X;
        var (rayPositionX, rayPositionY) =
            (posX + parameters.Direction.X * rayMultiplier, posY + parameters.Direction.Y * rayMultiplier);
        var trueDeltaX = halfStepDeltaX < tolerance ? 100.0 : halfStepDeltaX;
        var trueDeltaY = halfStepDeltaY < tolerance ? 100.0 : halfStepDeltaY;
        

        if (newSide == Side.NorthSouth)
        {
            var trueYStep = Math.Sqrt(trueDeltaX * trueDeltaX - 1.0);
            var halfStepInY = rayPositionY + (stepDelta.y * trueYStep) / 2.0;
            //return halfStepInY == newMap.y && (halfStepInY - newMap.y) < (1.0 - door.Offset / 64.0);
            return (Math.Abs(Math.Floor(halfStepInY) - newMap.y) < tolerance) && (halfStepInY - newMap.y) < (1.0 - door.Offset / 64.0);
        }
        // Side.EastWest
        var trueXStep = Math.Sqrt(trueDeltaY * trueDeltaY - 1.0);
        var halfStepInX = rayPositionX + (stepDelta.x * trueXStep) / 2.0;
        return (Math.Abs(Math.Floor(halfStepInX) - newMap.x) < tolerance) && (halfStepInX - newMap.x) < (1.0 - door.Offset / 64.0);
    }
}