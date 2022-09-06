namespace CSharpWolfenstein.Engine;

public record Pixel(byte Red, byte Green, byte Blue, byte Alpha)
{
    public static Pixel FromUint(uint data) =>
        new(
            Red: (byte) (data & 0xFF),
            Green: (byte) ((data >> 8) & 0xFF),
            Blue: (byte) ((data >> 16) & 0xFF),
            Alpha: (byte) (data >> 24)
        );
    public uint ToUint() => (uint)((Alpha << 24) | (Blue << 16) | (Green << 8) | Red);
};