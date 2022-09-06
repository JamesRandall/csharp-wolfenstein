using CSharpWolfenstein.Engine;

namespace CSharpWolfenstein.Tests.Engine;

public class PixelTests
{
    [Fact]
    public void PackAndUnpackAreEquivelant()
    {
        var pixel = new Pixel(0x20, 0x21, 0x22, 0x23);
        uint packed = pixel.ToUint();
        var unpacked = Pixel.FromUint(packed);
        Assert.Equal(pixel, unpacked);
    }
}