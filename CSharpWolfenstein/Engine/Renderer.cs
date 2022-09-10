using System.Runtime.CompilerServices;
using CSharpWolfenstein.Assets;

namespace CSharpWolfenstein.Engine;

public class Renderer
{
    private readonly int _width;
    private readonly int _height;
    private readonly uint[,] _buffer;
    
    public Renderer(int width, int height)
    {
        _width = width;
        _height = height;
        _buffer = new uint[height, width];
    }

    private void RenderTexture(Texture texture, int x, int y)
    {
        Enumerable.Range(0, texture.Height).Iter(row =>
        {
            var targetY = y + row;
            Enumerable.Range(0, texture.Width).Iter(col =>
                _buffer[targetY,x+col] = texture.Pixels[row,col]
            );
        });
    }
    
    public uint[,] UpdateFrameBuffer(AssetPack assetPack)
    {
        RenderTexture(assetPack.Walls[0], 0, 0);
        RenderTexture(assetPack.StatusBar.Grin, 70, 0);
        RenderTexture(assetPack.Sprites[0], 140, 0);
        RenderTexture(assetPack.Weapons[WeaponType.Pistol].CurrentSprite, 0, 0);
        return _buffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
}