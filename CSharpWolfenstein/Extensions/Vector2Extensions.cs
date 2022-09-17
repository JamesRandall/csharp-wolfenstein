using System.Numerics;

namespace CSharpWolfenstein.Extensions;

public static class Vector2DExtensions
{
    private const int MapSize = 64;
    
    public static Vector2D CrossProduct(this Vector2D value) => new Vector2D(value.Y, -value.X);

    public static (int x, int y) ToMap(this (double x, double y) v) => ((int) v.x, (int) v.y);
    
    public static (int x, int y) ToMap(this Vector2D v) => ((int) v.X, (int) v.Y);
    
    public static Vector2D FlipHorizontal(this Vector2D v) => v with {X = MapSize - 1 - v.X};
    public static (int x, int y) FlipHorizontal(this (int x, int y) v) => (MapSize - 1 - v.x, v.y);

    public static bool InMap(this (int x, int y) v) =>
        v.x >= 0 && v.x < MapSize && v.y >= 0 && v.y < MapSize;
}