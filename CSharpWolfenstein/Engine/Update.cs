using System.Numerics;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine
{
    using Extensions;
    
    namespace Extensions
    {
        public static class Extensions
        {
            public static GameState Update(this List<Func<GameState, double, GameState>> actions, GameState gameState,
                double delta)
            {
                //TODO: We need a C# fold impl
                var updatedGameState = gameState;
                foreach (var action in actions)
                {
                    updatedGameState = action(updatedGameState, delta);
                }
                return updatedGameState;
            }

            public static bool KeyPressed(this GameState gameState, ControlState controlState)
            {
                return (gameState.ControlState & controlState) > 0;
            }
        }
    }
    
    public static class GameStateExtensions
    {
        public static GameState Update(this GameState game, double delta)
        {
            var frameMultiplier = delta / 1000.0;
            var movementSpeed = 6.0 * frameMultiplier;
            var rotationSpeed = 4.0 * frameMultiplier;
            var posX = game.Camera.Position.X;
            var posY = game.Camera.Position.Y;
            var dirX = game.Camera.Direction.X;
            var dirY = game.Camera.Direction.Y;

            GameState Move(GameState input, double speed)
            {
                var newMapX = (int) (posX + dirX * speed);
                var newMapY = (int) (posY + dirY * speed);
                var newPosition = input.Camera.Position with {
                    X = (float) (posX + (dirX * speed)),
                    Y = (float) (posY + (dirY * speed))
                };
                return input with { Camera = input.Camera with { Position = newPosition}};
            }

            GameState Rotate(GameState input, double rotationMultiplier)
            {
                var planeX = input.Camera.Plane.X;
                var planeY = input.Camera.Plane.Y;
                var newDirX = dirX * Math.Cos(rotationMultiplier * rotationSpeed) -
                              dirY * Math.Sin(rotationMultiplier * rotationSpeed);
                var newDirY = dirX * Math.Sin(rotationMultiplier * rotationSpeed) +
                              dirY * Math.Cos(rotationMultiplier * rotationSpeed);
                var newPlaneX = planeX * Math.Cos(rotationMultiplier * rotationSpeed) -
                                planeY * Math.Sin(rotationMultiplier * rotationSpeed);
                var newPlaneY = planeX * Math.Sin(rotationMultiplier * rotationSpeed) +
                                planeY * Math.Cos(rotationMultiplier * rotationSpeed);
                return input with
                {
                    Camera = input.Camera with
                    {
                        Direction = new Vector2D((float)newDirX, (float)newDirY),
                        Plane = new Vector2D((float)newPlaneX, (float)newPlaneY)
                    }
                };
            }
            
            return
                new List<Func<GameState, double, GameState>>
                {
                    (g,d) => g.KeyPressed(ControlState.Forward) ? Move(g, movementSpeed) : g,
                    (g, d) => g.KeyPressed(ControlState.Backward) ? Move(game, -movementSpeed / 2.0) : g,
                    (g, d) => g.KeyPressed(ControlState.TurningLeft) ? Rotate(game, -1.0) : g,
                    (g, d) => g.KeyPressed(ControlState.TurningRight) ? Rotate(game, 1.0) : g
                }.Update(game, delta);
        }
    }
}

