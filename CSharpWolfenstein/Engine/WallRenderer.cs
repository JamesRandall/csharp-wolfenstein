using System.Collections.Immutable;
using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine.RayCasting;
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

        var wallRenderResult = new WallRenderingResult(
            ZIndexes: ImmutableArray<double>.Empty,
            WallInFrontOfPlayer: (-1, -1),
            IsDoorInFrontOfPlayer: false,
            DistanceToWallInFrontOfPlayer: -1.0,
            SpriteInFrontOfPlayerIndexOption: Option<int>.None);

        unsafe
        {
            var baseParameters = new RayCastParameters(
                IncludeTurningPoints: false,
                From: new (game.Camera.Position.X, game.Camera.Position.Y),
                Direction: new (0.0, 0.0)
            );
            fixed (uint* destPtr = buffer)
            {
                var startViewportX = stripToDraw ?? 0;
                var endViewportX = stripToDraw.HasValue ? stripToDraw + 1 : viewportSize.width;
                for (int viewportX = startViewportX; viewportX < endViewportX; viewportX++)
                {
                    var cameraX = 2.0 * viewportX / viewportSize.width - 1.0;
                    var rayDirection = new Vector2D(
                        game.Camera.Direction.X + game.Camera.Plane.X * cameraX,
                        game.Camera.Direction.Y + game.Camera.Plane.Y * cameraX
                    );
                    var parameters = baseParameters with { Direction = rayDirection };
                    var rayCastResult = rayCaster.Cast(game, parameters, ShouldContinueCast);
                    if (rayCastResult.IsComplete) // this is to allow the step based ray caster to, errr, step
                    {
                        var doorDistanceModifier = game.Map[rayCastResult.MapHit.y][rayCastResult.MapHit.x] switch
                        {
                            Door => 0.5,
                            _ => 0.0
                        };
                        var perpendicularWallDistance =
                            (rayCastResult.Side == Side.NorthSouth
                                ? rayCastResult.TotalSideDistance.x - rayCastResult.DeltaDistance.x
                                : rayCastResult.TotalSideDistance.y - rayCastResult.DeltaDistance.y) + doorDistanceModifier;
                        var lineHeight = viewportSize.height / perpendicularWallDistance;
                        var startY = Math.Max(-lineHeight / 2.0 + viewportSize.height / 2.0, 0.0);
                        var endY = Math.Min(lineHeight / 2.0 + viewportSize.height / 2.0, viewportSize.height - 1.0);

                        var cellHit = game.Map[rayCastResult.MapHit.y][rayCastResult.MapHit.x];
                        if (cellHit is Wall wall)
                        {
                            var wallX =
                                rayCastResult.Side == Side.NorthSouth
                                    ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.Y
                                    : game.Camera.Position.X + perpendicularWallDistance * rayDirection.X;
                            var clampedWallX = wallX - Math.Floor(wallX);
                            var rawTextureX = (int) (clampedWallX * Constants.TextureWidth);
                            var (textureX, textureIndex) =
                                rayCastResult.Side == Side.NorthSouth && rayDirection.X > 0.0
                                    ? (Constants.TextureWidth - rawTextureX - 1.0, wall.NorthSouthTextureIndex)
                                    : rayCastResult.Side == Side.EastWest && rayDirection.Y < 0.0
                                        ? (Constants.TextureWidth - rawTextureX - 1.0, wall.EastWestTextureIndex)
                                        : (rawTextureX, wall.TextureIndex(rayCastResult.Side));

                            RenderTextureColumn(lineHeight, startY, textureIndex, endY, textureX, destPtr, viewportX);
                        }
                        else if (cellHit is Door doorCell)
                        {
                            var door = game.Doors[doorCell.DoorIndex];

                            var wallX = rayCastResult.Side == Side.NorthSouth
                                ? game.Camera.Position.Y + perpendicularWallDistance * rayDirection.Y
                                : game.Camera.Position.X + perpendicularWallDistance * rayDirection.X;
                            var clampedWallX = wallX - Math.Floor(wallX);
                            var rawTextureX = (int) (clampedWallX * Constants.TextureWidth);
                            var textureX = Math.Max(0.0, Constants.TextureWidth - rawTextureX - 1.0 - door.Offset);
                            var textureIndex = door.TextureIndex;

                            RenderTextureColumn(lineHeight, startY, textureIndex, endY, textureX, destPtr, viewportX);
                        }

                        // Here we build up our z-index buffer that lets us clip sprites in a later rendering phase
                        // We also gather up some information about what is in front of the player that we will use
                        // later during game logic. We could cast another ray at that time, which might make for neater
                        // code, but doing this here saves us the expense of another cast. Probably not much in it but
                        // there you go...
                        wallRenderResult = wallRenderResult with
                        {
                            ZIndexes = wallRenderResult.ZIndexes.Add(perpendicularWallDistance),
                            WallInFrontOfPlayer =
                                viewportX == viewportSize.width/2
                                    ? (rayCastResult.MapHit.x,rayCastResult.MapHit.y)
                                    : wallRenderResult.WallInFrontOfPlayer,
                            DistanceToWallInFrontOfPlayer = 
                                viewportX == viewportSize.width/2
                                    ? perpendicularWallDistance
                                    : wallRenderResult.DistanceToWallInFrontOfPlayer,
                            IsDoorInFrontOfPlayer =
                                viewportX == viewportSize.width/2
                                    ? cellHit switch { Door => true, _ => false }
                                    : wallRenderResult.IsDoorInFrontOfPlayer
                        };
                    }
                }
            }
        }

        return wallRenderResult;
    }
}