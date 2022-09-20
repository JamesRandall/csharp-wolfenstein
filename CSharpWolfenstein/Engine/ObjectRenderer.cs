using System.Collections.Immutable;
using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine;

public static class ObjectRenderer
{
    private static int GetOrientedSpritedIndex(GameState game, AbstractGameObject gameObject)
    {
        if (gameObject is StaticGameObject sgo) return sgo.CommonProperties.SpriteIndex;
        if (gameObject is EnemyGameObject ego)
        {
            if (!ego.IsAlive) return ego.EnemyProperties.DeathSpriteIndexes[ego.EnemyProperties.CurrentAnimationFrame];
            return ego.DirectionVector.Match(
                directionVector =>
                {
                    const int spriteQuadrants = 8;
                    var quadrantSize = (360.0 / spriteQuadrants).ToRadians();
                    var playerRelativePosition = (game.Camera.Position - ego.CommonProperties.Position).Normalize();
                    var vectors = Enumerable
                        .Range(0, spriteQuadrants)
                        .Select((quadrant,index) =>
                            {
                                var centerAngle = quadrant * quadrantSize;
                                var startAngle = centerAngle - quadrantSize / 2.0;
                                var endAngle = centerAngle + quadrantSize / 2.0;
                                var startVector = directionVector.Rotate(startAngle);
                                var endVector = directionVector.Rotate(endAngle);
                                return (index, startVector, endVector);
                            }
                        )
                        .ToImmutableArray();
                    // Make sure the player position is within the triangle by shortening its distance from the enemy -
                    // a magnitude point will not be in the triangle (the magnitude is essentially the radius on a circle
                    // and a triangle formed from two points on the circle will not encompass the radius)
                    var playerTestPoint = playerRelativePosition / 2.0;
                    var p1 = Vector2D.Zero;
                    var quadrantIndex =
                        vectors.FirstOrDefault(pair =>
                            Barycentric.IsPointInTriangle(p1, pair.startVector, pair.endVector, playerTestPoint)).index;
                    return ego.BaseSpriteIndexForState + quadrantIndex;
                },
                _ => ego.BaseSpriteIndexForState
            );
        }
        return gameObject.CommonProperties.SpriteIndex;
    }

    public static void RenderSpriteObjects(uint[] buffer,
        (int width, int height) viewportSize,
        AssetPack assetPack,
        GameState game,
        WallRenderingResult wallRenderingResult)
    {
        var spriteIndexInCenterOfViewportOption = game
            .GameObjects
            .Aggregate(Option<int>.None, (gameObjectHitIndex, gameObject) =>
                {
                    if (RenderObject(buffer, viewportSize, assetPack, game, gameObject, wallRenderingResult.ZIndexes))
                        return gameObjectHitIndex;
                    else
                        return gameObjectHitIndex;
                }
            );
    }

    private static unsafe bool RenderObject(uint[] buffer,
        (int width, int height) viewportSize,
        AssetPack assetPack,
        GameState game,
        AbstractGameObject gameObject,
        ImmutableArray<double> zIndexes
    )
    {
        var planeX = game.Camera.Plane.X;
        var planeY = game.Camera.Plane.Y;
        var dirX = game.Camera.Direction.X;
        var dirY = game.Camera.Direction.Y;
        var hitDetectionLeft = viewportSize.width / 2 - Constants.FiringTolerance / 2;
        var hitDetectionRight = hitDetectionLeft + Constants.FiringTolerance;
        var (spriteX,spriteY) = gameObject.CommonProperties.Position - game.Camera.Position;
        var invDet = 1.0 / (planeX * dirY - planeY * dirX);
        var transformX = invDet * (dirY * spriteX - dirX * spriteY);
        var transformY = invDet * (-planeY * spriteX + planeX * spriteY);
        var spriteScreenX = viewportSize.width / 2.0 * (1.0 + transformX / transformY);
        //using 'transformY' instead of the real distance prevents fisheye - must have been one of my many mistakes back in 92!
        var spriteHeight = (int) Math.Abs(viewportSize.height / transformY);
        var drawStartY = Math.Max(0, -spriteHeight / 2 + viewportSize.height / 2);
        var drawEndY = Math.Min(viewportSize.height - 1, spriteHeight / 2 + viewportSize.height / 2);
        var spriteWidth = (int) Math.Abs(viewportSize.height / transformY);
        var drawStartX = (int)Math.Max(0, (-spriteWidth / 2) + spriteScreenX);
        var drawEndX = (int)Math.Min(viewportSize.width - 1, spriteWidth / 2 + spriteScreenX);
        
        
        var lineHeight = viewportSize.height / transformY;
        var step = 1.0 * Constants.TextureHeight / lineHeight;
        if (drawStartX >= 0 && drawEndX < viewportSize.width)
        {
            var spriteIndex = GetOrientedSpritedIndex(game, gameObject);
            Texture? trySpriteTexture = null;
            try
            {
                trySpriteTexture = assetPack.Sprites[spriteIndex];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var spriteTexture = trySpriteTexture!;
            //var spriteTexture = assetPack.Sprites[spriteIndex];

            fixed (uint* destPtr = buffer)
            {
                fixed (uint* srcPtr = spriteTexture.Pixels)
                {
                    for (var stripe = drawStartX; stripe < drawEndX; stripe++)
                    {
                        if (transformY > 0.0 && stripe > 0 && stripe < viewportSize.width && transformY < zIndexes[stripe])
                        {
                            var textureX =
                                (int) (256.0 * (stripe - (-spriteWidth / 2.0 + spriteScreenX)) * 64.0 / spriteWidth) / 256;
                            for (int y = drawStartY; y < drawEndY; y++)
                            {
                                var texY = (int) ((y - viewportSize.height / 2.0 + lineHeight / 2.0) * step);
                                var color = *(srcPtr + texY * spriteTexture.Width + textureX);
                                if (!Pixel.IsTransparent(color))
                                {
                                    *(destPtr + y * viewportSize.width + stripe) = color;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}