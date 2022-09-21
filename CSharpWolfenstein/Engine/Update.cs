using System.Collections.Immutable;
using System.Numerics;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;
using Microsoft.AspNetCore.Components.Forms;

namespace CSharpWolfenstein.Engine
{
    using Extensions;
    
    namespace Extensions
    {
        public static class Extensions
        {
            public static GameState Update(this List<Func<(GameState, double), GameState>> actions, GameState gameState,
                double delta)
            {
                //TODO: We need a C# fold impl
                var updatedGameState = gameState;
                foreach (var action in actions)
                {
                    updatedGameState = action((updatedGameState, delta));
                }
                return updatedGameState;
            }

            public static bool KeyPressed(this (GameState gameState,double _) args, ControlState controlState)
            {
                return (args.gameState.ControlState & controlState) > 0;
            }
        }
    }
    
    public static class GameStateExtensions
    {
        private const double DoorOpeningTime = 1000.0;
        private const double DoorOpenTime = 5000.0;
        
        private static GameState Move((GameState input, double delta) args, double speed)
        {
            var posX = args.input.Camera.Position.X;
            var posY = args.input.Camera.Position.Y;
            var dirX = args.input.Camera.Direction.X;
            var dirY = args.input.Camera.Direction.Y;
            var newMapX = (int) (posX + dirX * speed);
            var newMapY = (int) (posY + dirY * speed);
            
            // By checking if you can move into new x and y cells independently we get the "slide along the walls"
            // effect from the original game. Otherwise you would stop dead which would feel very weird indeed.
            var newPosition = new Vector2D(
                X: args.input.CanPlayerTraverse((newMapX, (int)posY)) ? (float) (posX + (dirX * speed)) : posX,
                Y: args.input.CanPlayerTraverse(((int) posX, newMapY)) ? (float) (posY + (dirY * speed)) : posY
            );
            return args.input with { Camera = args.input.Camera with { Position = newPosition}};
        }

        private static GameState Rotate((GameState input, double delta) args, double rotationMultiplier)
        {
            var rotationSpeed = 4.0 * args.delta / 1000.0;
            
            var dirX = args.input.Camera.Direction.X;
            var dirY = args.input.Camera.Direction.Y;
            var planeX = args.input.Camera.Plane.X;
            var planeY = args.input.Camera.Plane.Y;
            var newDirX = dirX * Math.Cos(rotationMultiplier * rotationSpeed) -
                          dirY * Math.Sin(rotationMultiplier * rotationSpeed);
            var newDirY = dirX * Math.Sin(rotationMultiplier * rotationSpeed) +
                          dirY * Math.Cos(rotationMultiplier * rotationSpeed);
            var newPlaneX = planeX * Math.Cos(rotationMultiplier * rotationSpeed) -
                            planeY * Math.Sin(rotationMultiplier * rotationSpeed);
            var newPlaneY = planeX * Math.Sin(rotationMultiplier * rotationSpeed) +
                            planeY * Math.Cos(rotationMultiplier * rotationSpeed);
            return args.input with
            {
                Camera = args.input.Camera with
                {
                    Direction = new Vector2D((float)newDirX, (float)newDirY),
                    Plane = new Vector2D((float)newPlaneX, (float)newPlaneY)
                }
            };
        }

        private static GameState DoAction((GameState input, double delta) args, WallRenderingResult wallRenderingResult)
        {
            GameState TryOpenDoor(int doorIndex)
            {
                var doorState = args.input.Doors[doorIndex];
                if (doorState.Status == DoorStatus.Closed)
                {
                    var newDoorState = doorState with
                    {
                        Status = DoorStatus.Opening,
                        TimeRemainingInAnimation = DoorOpeningTime
                    };
                    return args.input with
                    {
                        Doors = ImmutableArray.Create(
                            args.input.Doors.Select((door,index) => index==doorIndex ? newDoorState : door).ToArray())
                        ,CompositeAreas = UpdateCompositeAreas(args.input.CompositeAreas, newDoorState)
                    };
                }

                return args.input;
            }
            
            const double actionDistanceTolerance = 0.75;
            // doors are 0.5 recessed which means we need to extend the action activation distance
            const double actionDoorDistanceTolerance = actionDistanceTolerance + 0.5;
            if (wallRenderingResult.IsDoorInFrontOfPlayer &&
                wallRenderingResult.DistanceToWallInFrontOfPlayer <= actionDoorDistanceTolerance)
            {
                var (actionWallX, actionWallY) = wallRenderingResult.WallInFrontOfPlayer;
                return
                    args.input.Map[actionWallY][actionWallX] switch
                    {
                        Door door => TryOpenDoor(door.DoorIndex),
                        _ => args.input
                    };
            }
            return args.input;
        }

        private static GameState UpdateTransitioningDoors((GameState input, double delta) args)
        {
            (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) UpdateOpeningDoor(
                (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) state,
                double newTimeRemainingInAnimationState,
                DoorState doorState)
            {
                var newDoorState =
                    newTimeRemainingInAnimationState < 0.0
                        ? doorState with
                        {
                            Status = DoorStatus.Open, TimeRemainingInAnimation = DoorOpenTime, Offset = 64
                        }
                        : doorState with
                        {
                            TimeRemainingInAnimation = newTimeRemainingInAnimationState,
                            Offset = (DoorOpeningTime - newTimeRemainingInAnimationState) / DoorOpeningTime * 64.0
                        };
                return (state.doors.Add(newDoorState), state.compositeAreas);
            }

            (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) UpdateOpenDoor(
                (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) state,
                double newTimeRemainingInAnimationState,
                DoorState doorState)
            {
                var newDoorState =
                    newTimeRemainingInAnimationState < 0.0
                        ? args.input.Camera.Position.ToMap() == doorState.MapPosition
                            ? doorState with
                            {
                                TimeRemainingInAnimation = 1500.0
                            } // if the player is stood in the way of the door closing then wait a while longer
                            : doorState with {Status = DoorStatus.Closing, TimeRemainingInAnimation = DoorOpeningTime}
                        : doorState with {TimeRemainingInAnimation = newTimeRemainingInAnimationState};
                //if (newDoorState.Status == DoorStatus.Closing) PlaySoundEffect;
                return (state.doors.Add(newDoorState), state.compositeAreas);
            }
            
            (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) UpdateClosingDoor(
                (ImmutableArray<DoorState> doors, ImmutableArray<CompositeArea> compositeAreas) state,
                double newTimeRemainingInAnimationState,
                DoorState doorState)
            {
                var newDoorState =
                    newTimeRemainingInAnimationState < 0.0
                        ? doorState with
                        {
                            Status = DoorStatus.Closed, TimeRemainingInAnimation = DoorOpeningTime, Offset = 0.0
                        }
                        : doorState with
                        {
                            TimeRemainingInAnimation = newTimeRemainingInAnimationState,
                            Offset = 64.0 - (DoorOpeningTime - newTimeRemainingInAnimationState) / DoorOpeningTime * 64.0
                        };
                var newCompositeAreas =
                    newDoorState.Status == DoorStatus.Closed
                        ? UpdateCompositeAreas(state.compositeAreas, newDoorState)
                        : state.compositeAreas;
                return (state.doors.Add(newDoorState), newCompositeAreas);
            }

            var startingState = (doors: ImmutableArray<DoorState>.Empty, compositeAreas: args.input.CompositeAreas);
            var updates =
                args.input.Doors.Aggregate(startingState, (state, doorState) =>
                {
                    var newTimeRemainingInAnimationState = doorState.TimeRemainingInAnimation - args.delta;
                    return doorState.Status switch
                    {
                        DoorStatus.Opening => UpdateOpeningDoor(state, newTimeRemainingInAnimationState, doorState),
                        DoorStatus.Open => UpdateOpenDoor(state, newTimeRemainingInAnimationState, doorState),
                        DoorStatus.Closing => UpdateClosingDoor(state, newTimeRemainingInAnimationState, doorState),
                        _ => (state.doors.Add(doorState), state.compositeAreas)
                    };
                });
            return args.input with {Doors = updates.doors, CompositeAreas = updates.compositeAreas};
        }

        private static ImmutableArray<CompositeArea> UpdateCompositeAreas(ImmutableArray<CompositeArea> input, DoorState newDoorState)
        {
            return input;
        }

        private static GameState SortGameObjectsByDistance((GameState input, double delta) args) =>
            args.input with
            {
                GameObjects = args.input.GameObjects.OrderByDescending(go =>
                    (go.CommonProperties.Position - args.input.Camera.Position).Magnitude()
                ).ToImmutableArray()
            };


        public static GameState Update(this GameState game, double delta, WallRenderingResult wallRenderingResult)
        {
            var frameMultiplier = delta / 1000.0;
            var movementSpeed = 6.0 * frameMultiplier;
            
            return
                new List<Func<(GameState gameState, double delta), GameState>>
                {
                    // Player actions
                    g => g.KeyPressed(ControlState.Forward) ? Move(g, movementSpeed) : g.gameState,
                    g => g.KeyPressed(ControlState.Backward) ? Move(g, -movementSpeed / 2.0) : g.gameState,
                    g => g.KeyPressed(ControlState.TurningLeft) ? Rotate(g, -1.0) : g.gameState,
                    g => g.KeyPressed(ControlState.TurningRight) ? Rotate(g, 1.0) : g.gameState,
                    g => g.KeyPressed(ControlState.Action) ? DoAction(g, wallRenderingResult) : g.gameState,
                    // Doors and game objects
                    UpdateTransitioningDoors,
                    SortGameObjectsByDistance
                }.Update(game, delta);
        }
    }
}

