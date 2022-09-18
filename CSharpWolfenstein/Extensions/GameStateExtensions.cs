using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Extensions;

public static class GameStateExtensions
{
    private static bool CanTraverse(GameState input, (int x, int y) toPosition, bool checkPlayer)
    {
        if (!toPosition.InMap()) return false;
        if (checkPlayer && toPosition == input.Camera.Position.ToMap()) return false;
        var canPassCell = input.Map[toPosition.y][toPosition.x] switch
        {
            Door door => input.Doors[door.DoorIndex].Status == DoorStatus.Open,
            Wall _ => false,
            _ => true
        };
        if (canPassCell) return !input.GameObjects.Any(go => go.CommonProperties.MapPosition == toPosition);
        return false;
    }
    
    public static bool CanPlayerTraverse(this GameState input, (int x, int y) toPosition)
    {
        return CanTraverse(input, toPosition, false);
    }
}