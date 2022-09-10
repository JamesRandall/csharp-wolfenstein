namespace CSharpWolfenstein;

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
    
    public static byte[] CarmackDecode(this byte[] source)
    {
        const byte nearPointer = 0xA7;
        const byte farPointer = 0xA8;
        
        var size = source.GetUint16(0);
        var output = new byte[size];
        var inOffset = 2;
        var outOffset = 0;

        while (inOffset < source.Length)
        {
            var pointerCandidate = source[inOffset + 1];
            if (pointerCandidate == nearPointer || pointerCandidate == farPointer) // a possible pointer
            {
                var secondCandidate = source[inOffset];
                if (secondCandidate == 0)
                {
                    // its not a pointer
                    output[outOffset] = source[inOffset + 2];
                    output[outOffset + 1] = pointerCandidate;
                    inOffset += 3;
                    outOffset += 2;
                }
                else if (pointerCandidate == nearPointer)
                {
                    var pointerOffset = 2 * source[inOffset + 2];
                    for (int _ = 0; _ < secondCandidate; _++)
                    {
                        output.Set(outOffset, output.GetUint16(outOffset - pointerOffset));
                        outOffset += 2;
                    }

                    inOffset += 3;
                }
                else
                {
                    // far pointer
                    var pointerOffset = 2 * source.GetUint16(inOffset + 2);
                    for (var index = 0; index < secondCandidate; index++)
                    {
                        output.Set(outOffset, output.GetUint16(pointerOffset + 2 * index));
                        outOffset += 2;
                    }

                    inOffset += 4;
                }
            }
            else
            {
                output.Set(outOffset, source.GetUint16(inOffset));
                inOffset += 2;
                outOffset += 2;
            }
        }
        
        return output;
    }

    public static byte[] RlewDecode(this byte[] source, byte[] mapHeader)
    {
        var rlewTag = mapHeader.GetUint16(0);
        var size = source.GetUint16(0);
        var output = new byte[size];
        var inOffset = 2;
        var outOffset = 0;

        while (inOffset < source.Length)
        {
            var word = source.GetUint16(inOffset);
            inOffset += 2;
            if (word == rlewTag)
            {
                var length = source.GetUint16(inOffset);
                var value = source.GetUint16(inOffset + 2);
                inOffset += 4;
                for (var index = 0; index < length; index++)
                {
                    output.Set(outOffset, value);
                    outOffset += 2;
                }
            }
            else
            {
                output.Set(outOffset, word);
                outOffset += 2;
            }
        }
        
        return output;
    }
}