using System.Runtime.CompilerServices;
using CSharpWolfenstein.Assets;

namespace CSharpWolfenstein.Engine;

public class Renderer
{
    private readonly int _width;
    private readonly int _height;
    private readonly uint[] _buffer;
    
    public Renderer(int width, int height)
    {
        _width = width;
        _height = height;
        _buffer = new uint[height * width];
    }

    private void RenderTexture(Texture texture, int x, int y)
    {
        // I am using pointer based code here as it is about 2x to 3x faster than using array indexers.
        // ConstrainedCopy (see commented out code below) is fastest of all but doesn't allow for the handling
        // of transparency
        // Additionally the "caching" of the row based pointers yields further performance gains.
        // Altogether on my machine that got rendering of the below code from about 30fps to 60fps in release mode.
        //    RenderTexture(assetPack.Walls[0], 0, 0);
        //    RenderTexture(assetPack.StatusBar.Grin, 70, 0);
        //    RenderTexture(assetPack.Sprites[0], 140, 0);
        //    RenderTexture(assetPack.Weapons[WeaponType.Pistol].CurrentSprite, 0, 0);
        unsafe
        {
            fixed (uint* destPtr = _buffer)
            {
                fixed (uint* fixedSrcPtr = texture.Pixels)
                {
                    uint* destRowPtr = destPtr + (y * _width) + x;
                    uint* srcPtr = fixedSrcPtr;
                    for (int row = 0; row < texture.Height; row++)
                    {
                        uint* drawPtr = destRowPtr;
                        for (int col = 0; col < texture.Width; col++)
                        {
                            uint color = *srcPtr++;
                            if (!Pixel.IsTransparent(color))
                                *drawPtr++ = color;
                            else
                                drawPtr++;
                        }
                        destRowPtr += _width;
                    }
                }
            }
        }

        /*
        Enumerable.Range(0, texture.Height).Iter(row =>
        {
            var targetY = y + row;
            Array.ConstrainedCopy(
                texture.Pixels,
                texture.Width * row,
                _buffer,
                targetY*_width + x, 
                texture.Width);
        });
        */
    }
    
    public uint[] UpdateFrameBuffer(AssetPack assetPack)
    {
        RenderTexture(assetPack.Walls[0], 0, 0);
        RenderTexture(assetPack.StatusBar.Grin, 70, 0);
        RenderTexture(assetPack.Sprites[0], 140, 0);
        RenderTexture(assetPack.Weapons[WeaponType.Pistol].CurrentSprite, 0, 0);
        return _buffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
}