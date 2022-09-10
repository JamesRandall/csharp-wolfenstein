using CSharpWolfenstein.Engine;
using CSharpWolfenstein.Extensions;

namespace CSharpWolfenstein.Assets;

public record Texture(uint[] Pixels, int Width, int Height)
{
    public Pixel Get(int x, int y) => Pixel.FromUint(Pixels[y * Width + x]);

    public Texture Scale(double scaleFactor)
    {
        var newWidth = (int)(Width * scaleFactor);
        var newHeight = (int) (Height * scaleFactor);
        var pixels = new uint[newHeight * newWidth];
        Enumerable.Range(0, newHeight)
            .Iter(row =>
            {
                var srcRow = (int) (row / scaleFactor);
                Enumerable.Range(0, newWidth).Iter(col =>
                    {
                        var srcCol = (int) (col / scaleFactor);
                        pixels[row * newWidth + col] = Pixels[srcRow * Width + srcCol];
                    }
                );
            });
        return new(Pixels: pixels, Width: newWidth, Height: newHeight);
    }
}

public static class TextureExtensions
{
    public static Texture[] Scale(this Texture[] textures, double scaleFactor)
    {
        return textures.Select(texture => texture.Scale(scaleFactor)).ToArray();
    }
}