namespace CSharpWolfenstein.Extensions;

public static class ByteArrayExtensions
{
    public static ushort GetUint16(this byte[] bytes, int offset) => BitConverter.ToUInt16(bytes, offset);
    
    public static uint GetUint32(this byte[] bytes, int offset) => BitConverter.ToUInt32(bytes, offset);
    
    public static byte[] Set(this byte[] bytes, int offset, ushort value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        bytes[offset] = valueBytes[0];
        bytes[offset + 1] = valueBytes[1];
        return bytes;
    }
}