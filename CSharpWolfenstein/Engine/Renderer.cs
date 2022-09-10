using System.Runtime.CompilerServices;
using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

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
    
    public uint[] UpdateFrameBuffer(AssetPack assetPack, GameState game, (int width, int height) viewportSize)
    {
        //const int mapSize = 64;
        const double textureWidth = 64.0;
        const double textureHeight = 64.0;
        //var ceilingColor = (new Pixel(0x39, 0x39, 0x39, 0xFF)).ToUint();

        bool ShouldContinueCast(RayCastResult result) =>
            !result.IsHit && result.MapHit.InMap();

        //int startX = (_width - viewportSize.width) / 2;
        
        
        var initialWallRenderResult = new WallRenderingResult(
            ZIndexes: Array.Empty<double>(),
            WallInFrontOfPlayer: (-1, -1),
            IsDoorInFrontOfPlayer: false,
            DistanceToWallInFrontOfPlayer: -1,
            SpriteInFrontOfPlayerIndexOption: Option<int>.None);

        unsafe
        {
            var baseParameters = new RayCastParameters(
                IncludeTurningPoints: false,
                From: (game.Camera.Position.X, game.Camera.Position.Y),
                Direction: (0.0, 0.0)
            );
            fixed (uint* destPtr = _buffer)
            {
                for (int viewportX = 0; viewportX < viewportSize.width; viewportX++)
                {
                    var cameraX = 2.0 * viewportX / viewportSize.width - 1.0;
                    (double x, double y) rayDirection = (
                        game.Camera.Direction.X + game.Camera.Plane.X * cameraX,
                        game.Camera.Direction.Y + game.Camera.Plane.Y * cameraX
                    );
                    var parameters = baseParameters with { Direction = rayDirection };
                    var rayCastResult = Ray.Cast(parameters, ShouldContinueCast, game);
                    var doorDistanceModifier = game.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] switch
                    {
                        Door => 0.5,
                        _ => 0.0
                    };
                    var perpendicularWallDistance =
                        (rayCastResult.Side == Side.NorthSouth
                            ? rayCastResult.TotalDistance.x - rayCastResult.DeltaDistance.x
                            : rayCastResult.TotalDistance.y - rayCastResult.DeltaDistance.y) + doorDistanceModifier;
                    var lineHeight = viewportSize.height / perpendicularWallDistance;
                    var startY = Math.Max(-lineHeight/2.0 + viewportSize.height/2.0, 0.0);
                    var endY = Math.Min(lineHeight / 2.0 + viewportSize.height / 2.0, viewportSize.height - 1.0);

                    if (game.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] is Wall wall)
                    {
                        var wallX =
                            rayCastResult.Side == Side.NorthSouth
                                ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.y
                                : game.Camera.Position.X + perpendicularWallDistance * rayDirection.x;
                        var clampedWallX = wallX - Math.Floor(wallX);
                        var rawTextureX = (int)(clampedWallX * textureWidth);
                        var (textureX, textureIndex) =
                            rayCastResult.Side == Side.NorthSouth && rayDirection.x > 0.0
                                ? (textureWidth - rawTextureX - 1.0, wall.NorthSouthTextureIndex)
                                : rayCastResult.Side == Side.EastWest && rayDirection.y < 0.0
                                    ? (textureWidth - rawTextureX - 1.0, wall.EastWestTextureIndex)
                                    : ((double) rawTextureX, wall.TextureIndex(rayCastResult.Side));
                        var step = 1.0 * textureHeight / lineHeight;
                        var texturePosition = (startY - viewportSize.height / 2.0 + lineHeight / 2.0) * step;
                        var texture = assetPack.Walls[textureIndex];
                        fixed (uint* baseTexturePtr = texture.Pixels)
                        {
                            for (int drawY = 0; drawY < (endY - startY); drawY++)
                            {
                                var textureY = (int) (texturePosition + step * drawY) & ((int)textureHeight-1);
                                var color = *(baseTexturePtr + textureY * texture.Width + (int) textureX);
                                *(destPtr + (drawY + (int) startY) * _width + viewportX) = color;
                            }
                        }
                    }
                    else if (game.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] is Door doorCell)
                    {
                        var door = game.Doors[doorCell.DoorIndex];
                        
                        var wallX = rayCastResult.Side == Side.NorthSouth
                            ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.y
                            : game.Camera.Position.X + perpendicularWallDistance * rayDirection.x;
                        var clampedWallX = wallX - Math.Floor(wallX);
                        var rawTextureX = (int)(clampedWallX * textureWidth);
                        var textureX = Math.Max(0.0, textureWidth - rawTextureX - 1.0 - door.Offset);
                        var textureIndex = door.TextureIndex;
                        var step = 1.0 * textureHeight / lineHeight;
                        var texturePosition = (startY - viewportSize.height / 2.0 + lineHeight / 2.0) * step;
                        var texture = assetPack.Walls[textureIndex];
                        fixed (uint* baseTexturePtr = texture.Pixels)
                        {
                            for (int drawY = 0; drawY < (endY - startY); drawY++)
                            {
                                var textureY = (int) (texturePosition + step * drawY) & ((int)textureHeight-1);
                                var color = *(baseTexturePtr + textureY * texture.Width + (int) textureX);
                                *(destPtr + (drawY + (int) startY) * _width + viewportX) = color;
                            }
                        }
                    }
                }
            }
        }

        //RenderTexture(assetPack.Walls[0], 0, 0);
        //RenderTexture(assetPack.StatusBar.Grin, 70, 0);
        //RenderTexture(assetPack.Sprites[0], 140, 0);
        //RenderTexture(assetPack.Weapons[WeaponType.Pistol].CurrentSprite, 0, 0);
        return _buffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
}