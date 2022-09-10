using System.Numerics;

namespace CSharpWolfenstein;

public static class Vector2Extensions
{
    public static Vector2 CrossProduct(this Vector2 value) => new Vector2(value.Y, value.X);
}