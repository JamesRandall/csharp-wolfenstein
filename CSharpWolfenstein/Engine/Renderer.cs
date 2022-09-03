using System.Runtime.CompilerServices;

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
    
    public uint[,] UpdateFrameBuffer()
    {
        for (int row = 0; row < _height; row++)
            for (int col = 0; col < _width; col++)
            {
                _buffer[row, col] = MakePixel((byte)col, 0, (byte)row, 0xFF);
            }
        return _buffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
}