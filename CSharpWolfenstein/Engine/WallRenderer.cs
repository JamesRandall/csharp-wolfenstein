using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine;

public static class WallRenderer
{
    public static bool ShouldContinueCast(RayCastResult result) => !result.IsHit && result.MapHit.InMap();
    
    public static WallRenderingResult RenderWalls(
        uint[] buffer,
        AssetPack assetPack,
        GameState game,
        (int width, int height) viewportSize,
        AbstractRayCaster rayCaster,
        int? stripToDraw=null)
    {
        unsafe void RenderTextureColumn(double lineHeight, double startY, int textureIndex, double endY, double textureX,
            uint* destPtr, int viewportX)
        {
            var step = 1.0 * Constants.TextureHeight / lineHeight;
            var texturePosition = (startY - viewportSize.height / 2.0 + lineHeight / 2.0) * step;
            var texture = assetPack.Walls[textureIndex];
            fixed (uint* baseTexturePtr = texture.Pixels)
            {
                for (int drawY = 0; drawY < (endY - startY); drawY++)
                {
                    var textureY = (int) (texturePosition + step * drawY) & ((int) Constants.TextureHeight - 1);
                    var color = *(baseTexturePtr + textureY * texture.Width + (int) textureX);
                    *(destPtr + (drawY + (int) startY) * viewportSize.width + viewportX) = color;
                }
            }
        }

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
            fixed (uint* destPtr = buffer)
            {
                var startViewportX = stripToDraw ?? 0;
                var endViewportX = stripToDraw.HasValue ? stripToDraw + 1 : viewportSize.width;
                for (int viewportX = startViewportX; viewportX < endViewportX; viewportX++)
                {
                    var cameraX = 2.0 * viewportX / viewportSize.width - 1.0;
                    (double x, double y) rayDirection = (
                        game.Camera.Direction.X + game.Camera.Plane.X * cameraX,
                        game.Camera.Direction.Y + game.Camera.Plane.Y * cameraX
                    );
                    var parameters = baseParameters with { Direction = rayDirection };
                    var rayCastResult = rayCaster.Cast(parameters, ShouldContinueCast, game);
                    if (rayCastResult.IsComplete) // this is to allow the step based ray caster to, errr, step
                    {
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
                        var startY = Math.Max(-lineHeight / 2.0 + viewportSize.height / 2.0, 0.0);
                        var endY = Math.Min(lineHeight / 2.0 + viewportSize.height / 2.0, viewportSize.height - 1.0);

                        if (game.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] is Wall wall)
                        {
                            var wallX =
                                rayCastResult.Side == Side.NorthSouth
                                    ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.y
                                    : game.Camera.Position.X + perpendicularWallDistance * rayDirection.x;
                            var clampedWallX = wallX - Math.Floor(wallX);
                            var rawTextureX = (int) (clampedWallX * Constants.TextureWidth);
                            var (textureX, textureIndex) =
                                rayCastResult.Side == Side.NorthSouth && rayDirection.x > 0.0
                                    ? (Constants.TextureWidth - rawTextureX - 1.0, wall.NorthSouthTextureIndex)
                                    : rayCastResult.Side == Side.EastWest && rayDirection.y < 0.0
                                        ? (Constants.TextureWidth - rawTextureX - 1.0, wall.EastWestTextureIndex)
                                        : ((double) rawTextureX, wall.TextureIndex(rayCastResult.Side));

                            RenderTextureColumn(lineHeight, startY, textureIndex, endY, textureX, destPtr, viewportX);
                        }
                        else if (game.Map[rayCastResult.MapHit.y, rayCastResult.MapHit.x] is Door doorCell)
                        {
                            var door = game.Doors[doorCell.DoorIndex];

                            var wallX = rayCastResult.Side == Side.NorthSouth
                                ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.y
                                : game.Camera.Position.X + perpendicularWallDistance * rayDirection.x;
                            var clampedWallX = wallX - Math.Floor(wallX);
                            var rawTextureX = (int) (clampedWallX * Constants.TextureWidth);
                            var textureX = Math.Max(0.0, Constants.TextureWidth - rawTextureX - 1.0 - door.Offset);
                            var textureIndex = door.TextureIndex;

                            RenderTextureColumn(lineHeight, startY, textureIndex, endY, textureX, destPtr, viewportX);
                        }
                    }
                }
            }
        }

        return initialWallRenderResult;
    }
}