namespace CSharpWolfenstein.Engine;

public static class Barycentric
{
    public static bool IsPointInTriangle(Vector2D p1, Vector2D p2, Vector2D p3, Vector2D testPoint)
    {
        // barycentric coordinate approach
        // https://stackoverflow.com/questions/40959754/c-sharp-is-the-point-in-triangle
        var a = 
            ((p2.Y - p3.Y) * (testPoint.X - p3.X) + (p3.X - p2.X) * (testPoint.Y - p3.Y)) /
            ((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
        var b =
            ((p3.Y - p1.Y) * (testPoint.X - p3.X) + (p1.X - p3.X) * (testPoint.Y - p3.Y)) /
            ((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
        var c = 1.0 - a - b;
        return a >= 0.0 && a <= 1.0 && b >= 0.0 && b <= 1.0 && c >= 0.0 && c <= 1.0;
    }
}