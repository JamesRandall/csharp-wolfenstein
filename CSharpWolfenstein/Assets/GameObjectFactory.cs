using System.Collections.Immutable;
using CSharpWolfenstein.Assets.GameObjectFactoryExtensions;
using CSharpWolfenstein.Extensions;

namespace CSharpWolfenstein.Assets
{
    namespace GameObjectFactoryExtensions
    {
        public static class StaticGameObjectExtensions
        {
            private static readonly ImmutableHashSet<ushort> BlockingObjects = ImmutableHashSet.Create(new ushort[]
            {
                0x19, 0x1A, 0x1E, 0x22, 0x23, 0x24,
                0x26, 0x27, 0x28, 0x29, 0x3B,
                0x3C, 0x44, 0x45, 0x46, 0x47
            });
            
            public static StaticGameObject WithHitPoints(this StaticGameObject sgo, int points) =>
                sgo with {CommonProperties = sgo.CommonProperties with {HitPointsRestored = points, Pickupable = true}};
            public static StaticGameObject WithScore(this StaticGameObject sgo, int points) =>
                sgo with {CommonProperties = sgo.CommonProperties with {Score = points, Pickupable = true}};
            public static StaticGameObject WithBullets(this StaticGameObject sgo, int bullets) =>
                sgo with {CommonProperties = sgo.CommonProperties with {AmmoRestored = bullets, Pickupable = true}};
            public static StaticGameObject WithBlockingIfOfBlockingType(this StaticGameObject sgo, ushort objectValue) =>
                BlockingObjects.Contains(objectValue)
                    ? sgo with {CommonProperties = sgo.CommonProperties with {Blocking = true} }
                    : sgo;
            public static StaticGameObject WithExtraLife(this StaticGameObject sgo) =>
                sgo with {CommonProperties = sgo.CommonProperties with {LivesRestored = 1, Pickupable = true}};
        }
    }

    public class GameObjectFormatException : Exception
    {
        public GameObjectFormatException(string message) : base(message) { }
    }

    public static class GameObjectFactory
    {
        private const int NoMapDirection = 99;
        private static MapDirection StartingMapDirectionFromInt(int direction) =>
            direction switch
            {
                0 => MapDirection.East,
                1 => MapDirection.North,
                2 => MapDirection.West,
                3 => MapDirection.South,
                NoMapDirection => MapDirection.None,
                _ => throw new GameObjectFormatException($"Direction not recognised {direction}")
            };

        private static int StartingHitPoints(DifficultyLevel difficulty, EnemyType enemyType) =>
            (difficulty, enemyType) switch
            {
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Guard) => 25,
                (DifficultyLevel.DontHurtMe, EnemyType.Guard) => 25,
                (DifficultyLevel.BringEmOn, EnemyType.Guard) => 25,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Guard) => 25,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Officer) => 50,
                (DifficultyLevel.DontHurtMe, EnemyType.Officer) => 25,
                (DifficultyLevel.BringEmOn, EnemyType.Officer) => 50,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Officer) => 50,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.SS) => 100,
                (DifficultyLevel.DontHurtMe, EnemyType.SS) => 100,
                (DifficultyLevel.BringEmOn, EnemyType.SS) => 100,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.SS) => 100,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Dog) => 1,
                (DifficultyLevel.DontHurtMe, EnemyType.Dog) => 1,
                (DifficultyLevel.BringEmOn, EnemyType.Dog) => 1,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Dog) => 1,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Zombie) => 45,
                (DifficultyLevel.DontHurtMe, EnemyType.Zombie) => 55,
                (DifficultyLevel.BringEmOn, EnemyType.Zombie) => 55,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Zombie) => 65,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.FakeAdolf) => 200,
                (DifficultyLevel.DontHurtMe, EnemyType.FakeAdolf) => 300,
                (DifficultyLevel.BringEmOn, EnemyType.FakeAdolf) => 400,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.FakeAdolf) => 500,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Adolf) => 800,
                (DifficultyLevel.DontHurtMe, EnemyType.Adolf) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Adolf) => 1050,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Adolf) => 1200,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Fettgesicht) => 850,
                (DifficultyLevel.DontHurtMe, EnemyType.Fettgesicht) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Fettgesicht) => 1050,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Fettgesicht) => 1200,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Schabbs) => 850,
                (DifficultyLevel.DontHurtMe, EnemyType.Schabbs) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Schabbs) => 1550,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Schabbs) => 2400,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Gretel) => 850,
                (DifficultyLevel.DontHurtMe, EnemyType.Gretel) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Gretel) => 1050,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Gretel) => 1200,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Hans) => 850,
                (DifficultyLevel.DontHurtMe, EnemyType.Hans) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Hans) => 1050,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Hans) => 1200,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Otto) => 850,
                (DifficultyLevel.DontHurtMe, EnemyType.Otto) => 950,
                (DifficultyLevel.BringEmOn, EnemyType.Otto) => 1050,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Otto) => 1200,
                (DifficultyLevel.CanIPlayDaddy, EnemyType.Ghost) => 25,
                (DifficultyLevel.DontHurtMe, EnemyType.Ghost) => 25,
                (DifficultyLevel.BringEmOn, EnemyType.Ghost) => 25,
                (DifficultyLevel.IAmDeathIncarnate, EnemyType.Ghost) => 25,
                _ => throw new GameObjectFormatException("Difficulty level and enemy type pair not recognised")
            };

        private static EnemyGameObject CreateEnemy(
            DifficultyLevel difficulty,
            int spriteIndex,
            int spriteBlocks,
            int framesPerBlock,
            IEnumerable<int> deathSprites,
            int hurtSpriteIndex,
            IEnumerable<int> attackingSprites,
            int score,
            double patrolSpeed,
            double chaseSpeed,
            EnemyType enemyType,
            int x,
            int y,
            ushort directionAsInt,
            EnemyState startingState
        )
        {
            return new EnemyGameObject(
                CommonProperties: new BasicGameObjectProperties(
                    Position: new Vector2D(x + 0.5, y + 0.5),
                    PlayerRelativePosition: new Vector2D(0.0, 0.0),
                    UnsquaredDistanceFromPlayer: 0.0,
                    SpriteIndex: spriteIndex,
                    CollidesWithBullets: true,
                    Pickupable: false,
                    HitPointsRestored: 0,
                    AmmoRestored: 0,
                    LivesRestored: 0,
                    Score: score,
                    Blocking: true
                ),
                EnemyProperties: new EnemyProperties(
                    EnemyType: enemyType,
                    Direction: StartingMapDirectionFromInt(directionAsInt),
                    DeathSpriteIndexes: ImmutableArray.Create(deathSprites.ToArray()),
                    AttackSpriteIndexes: ImmutableArray.Create(attackingSprites.ToArray()),
                    SpriteBlocks: spriteBlocks,
                    FramesPerBlock: framesPerBlock,
                    CurrentAnimationFrame: 0,
                    TimeUntilNextAnimationFrame: EnemyGameObject.AnimationTimeForState(startingState),
                    IsFirstAttack: true,
                    State: startingState,
                    FireAtPlayerRequired: false,
                    MoveToChaseRequired: false,
                    HitPoints: StartingHitPoints(difficulty, enemyType),
                    HurtSpriteIndex: hurtSpriteIndex,
                    PatrolSpeed: patrolSpeed,
                    ChaseSpeed: chaseSpeed
                )
            );
        }

        private static EnemyGameObject CreateGuard(DifficultyLevel difficultyLevel, int x, int y, ushort directionAsInt,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 50, 4, 8, new[] {90, 91, 92, 93, 95}, 94,
                new[] {96, 97, 98}, 100, 0.5, 1.0, EnemyType.Guard, x, y, directionAsInt, startingState);

        private static EnemyGameObject CreateDeadGuard(int x, int y)
        {
            var guard = CreateGuard(DifficultyLevel.CanIPlayDaddy, x, y, (124 - 108) % 4, EnemyState.Dead);
            return guard with
            {
                CommonProperties = guard.CommonProperties with {Blocking = false},
                EnemyProperties = guard.EnemyProperties with
                {
                    CurrentAnimationFrame = guard.EnemyProperties.DeathSpriteIndexes.Length - 1
                }
            };
        }
        
        
        private static EnemyGameObject CreateDog(DifficultyLevel difficultyLevel, int x, int y, ushort directionAsInt,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 99, 3, 8, new[] {131, 132, 133, 134}, 131, new[] {135, 136, 137}, 200, 1.0,
                1.5,
                EnemyType.Dog, x, y, directionAsInt, startingState);

        private static EnemyGameObject CreateOfficer(DifficultyLevel difficultyLevel, int x, int y,
            ushort directionAsInt,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 138, 4, 8, new[] {179, 180, 181, 183}, 182, new[] {184, 185, 186}, 500, 0.5,
                1.25,
                EnemyType.SS, x, y, directionAsInt, startingState);

        private static EnemyGameObject CreateZombie(DifficultyLevel difficultyLevel, int x, int y,
            ushort directionAsInt,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 187, 4, 8, new[] {228, 229, 230, 232, 233}, 231, new[] {234, 235, 236, 237},
                700,
                0.5, 1.0, EnemyType.Zombie, x, y, directionAsInt, startingState);

        private static EnemyGameObject CreateLeon(DifficultyLevel difficultyLevel, int x, int y, ushort directionAsInt,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 238, 4, 8, new[] {279, 280, 281, 283, 284}, 282, new[] {285, 286, 287}, 400,
                0.5,
                1.25, EnemyType.Officer, x, y, directionAsInt, startingState);

        private static EnemyGameObject CreateHans(DifficultyLevel difficultyLevel, int x, int y, EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 296, 1, 4, new[] {304, 305, 306, 303}, 304, new[] {300, 301, 302}, 2000, 0.5,
                1.0,
                EnemyType.Hans, x, y, NoMapDirection, startingState);

        private static EnemyGameObject CreateSchabbs(DifficultyLevel difficultyLevel, int x, int y, EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 307, 1, 4, new[] {313, 314, 315, 316}, 313, new[] {311, 312}, 2000, 0.5, 1.0,
                EnemyType.Schabbs, x, y, NoMapDirection, startingState);

        private static EnemyGameObject CreateFakeAdolf(DifficultyLevel difficultyLevel, int x, int y,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 321, 1, 4, new[] {328, 329, 330, 331, 332, 333}, 328, new[] {325}, 2000, 0.5,
                1.0,
                EnemyType.FakeAdolf, x, y, NoMapDirection, startingState);

        // Note Hitler has two states - robot adolf and plain adolf, this only deals with plain hitler
        private static EnemyGameObject CreateAdolf(DifficultyLevel difficultyLevel, int x, int y, 
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 345, 1, 4, new[] {353, 354, 355, 356, 357, 358, 359, 352}, 353,
                new[] {349, 350, 351}, 5000, 0.5, 1.0, EnemyType.Adolf, x, y, NoMapDirection, startingState);

        private static EnemyGameObject CreateOtto(DifficultyLevel difficultyLevel, int x, int y, EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 360, 1, 4, new[] {366, 367, 368, 369}, 366, new[] {364, 365}, 5000, 0.5, 1.0,
                EnemyType.Otto, x, y, NoMapDirection, startingState);

        private static EnemyGameObject CreateGretel(DifficultyLevel difficultyLevel, int x, int y,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 385, 1, 4, new[] {393, 394, 395, 392}, 393, new[] {389, 390, 391}, 5000, 0.5,
                1.0,
                EnemyType.Gretel, x, y, NoMapDirection, startingState);

        private static EnemyGameObject CreateFettgesicht(DifficultyLevel difficultyLevel, int x, int y,
            EnemyState startingState) =>
            CreateEnemy(difficultyLevel, 396, 1, 4, new[] {404, 405, 406, 407}, 404, new[] {400, 401, 402, 403}, 5000,
                0.5,
                1.0, EnemyType.Fettgesicht, x, y, NoMapDirection, startingState);
        
        private static IEnumerable<(int x, int y)> EnumerateMap()
        {
            for (int rowIndex = 0; rowIndex < Constants.MapSize; rowIndex++)
            {
                for (int colIndex = 0; colIndex < Constants.MapSize; colIndex++)
                {
                    yield return (colIndex, rowIndex);
                }
            }
        }
        
        public static ImmutableArray<AbstractGameObject> Create(DifficultyLevel difficultyLevel, byte[] plane0, byte[] plane1)
        {
            EnemyState StandingOrMoving(ushort baseValue, ushort planeValue) =>
                (baseValue - planeValue) < 4 ? EnemyState.Standing : EnemyState.Path(PathState.Empty);
                
            ushort GetPlaneValue(byte[] plane, (int colIndex, int rowIndex) mapPosition) =>
                plane.GetUint16(2 * (mapPosition.colIndex + Constants.MapSize * mapPosition.rowIndex));
            const ushort ambushingValue = 0x6a;

            return EnumerateMap()
                .Aggregate(ImmutableArray<AbstractGameObject>.Empty, (gameObjects, mapPosition) =>
                {
                    var planeValue = GetPlaneValue(plane1, mapPosition);
                    if (planeValue >= 23 && planeValue <= 70)
                    {
                        var bgo = new StaticGameObject(new BasicGameObjectProperties(
                            Position: new Vector2D(mapPosition.x + 0.5, mapPosition.y + 0.5),
                            PlayerRelativePosition: new Vector2D(0.0, 0.0),
                            UnsquaredDistanceFromPlayer: 0.0,
                            SpriteIndex: planeValue-21,
                            CollidesWithBullets: false,
                            Pickupable: false,
                            HitPointsRestored: 0,
                            AmmoRestored: 0,
                            LivesRestored: 0,
                            Score: 0,
                            Blocking: false));
                        return gameObjects.Add(
                            planeValue switch
                            {
                                0x1d => bgo.WithHitPoints(4), // dog food
                                0x2f => bgo.WithHitPoints(10), // food
                                0x30 => bgo.WithHitPoints(25), // medpack
                                0x31 => bgo.WithBullets(8),
                                0x34 => bgo.WithScore(100),
                                0x35 => bgo.WithScore(500),
                                0x36 => bgo.WithScore(1000),
                                0x37 => bgo.WithScore(5000),
                                0x38 => bgo.WithExtraLife(),
                                _ => bgo.WithBlockingIfOfBlockingType(planeValue)
                            }
                        );
                    }
                    else if (planeValue >= 108)
                    {
                        var gameObjectOption = planeValue switch
                        {
                            // bosses
                            160 => Option<EnemyGameObject>.Some(CreateFakeAdolf(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            178 => Option<EnemyGameObject>.Some(CreateAdolf(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            179 => Option<EnemyGameObject>.Some(CreateFettgesicht(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            196 => Option<EnemyGameObject>.Some(CreateSchabbs(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            197 => Option<EnemyGameObject>.Some(CreateGretel(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            214 => Option<EnemyGameObject>.Some(CreateHans(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            215 => Option<EnemyGameObject>.Some(CreateOtto(difficultyLevel, mapPosition.x, mapPosition.y, EnemyState.Ambushing)),
                            // guards
                            124 =>
                                Option<EnemyGameObject>.Some(CreateDeadGuard(mapPosition.x,  mapPosition.y)),
                            >= 108 and < 116 =>
                                Option<EnemyGameObject>.Some(CreateGuard(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-108) % 4), StandingOrMoving(108,planeValue))),
                            >= 144 and < 152 =>
                                difficultyLevel >= DifficultyLevel.BringEmOn
                                    ? Option<EnemyGameObject>.Some(CreateGuard(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-144) % 4), StandingOrMoving(144,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            >= 180 and < 188 =>
                                difficultyLevel == DifficultyLevel.IAmDeathIncarnate
                                    ? Option<EnemyGameObject>.Some(CreateGuard(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-180) % 4), StandingOrMoving(188,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            // leon
                            >= 116 and < 124 =>
                                Option<EnemyGameObject>.Some(CreateLeon(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-116) % 4), StandingOrMoving(116,planeValue))),
                            >= 152 and < 160 =>
                                difficultyLevel >= DifficultyLevel.BringEmOn
                                    ? Option<EnemyGameObject>.Some(CreateLeon(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-152) % 4), StandingOrMoving(152,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            >= 188 and < 196 =>
                                difficultyLevel >= DifficultyLevel.BringEmOn
                                    ? Option<EnemyGameObject>.Some(CreateLeon(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-188) % 4), StandingOrMoving(188,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            // officer
                            >= 126 and < 134 =>
                                Option<EnemyGameObject>.Some(CreateOfficer(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-126) % 4), StandingOrMoving(126,planeValue))),
                            >= 162 and < 170 =>
                                difficultyLevel >= DifficultyLevel.BringEmOn
                                    ? Option<EnemyGameObject>.Some(CreateOfficer(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-162) % 4), StandingOrMoving(162,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            >= 198 and < 206 =>
                                difficultyLevel >= DifficultyLevel.IAmDeathIncarnate
                                    ? Option<EnemyGameObject>.Some(CreateOfficer(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-198) % 4), StandingOrMoving(198,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            // dogs
                            >= 138 and < 142 =>
                                Option<EnemyGameObject>.Some(CreateDog(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-138) % 4), EnemyState.Path(PathState.Empty))),
                            >= 174 and < 178 =>
                                difficultyLevel >= DifficultyLevel.BringEmOn
                                ? Option<EnemyGameObject>.Some(CreateDog(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-174) % 4), EnemyState.Path(PathState.Empty)))
                                : Option<EnemyGameObject>.None,
                            >= 210 and < 214 =>
                                difficultyLevel >= DifficultyLevel.IAmDeathIncarnate
                                    ? Option<EnemyGameObject>.Some(CreateDog(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-210) % 4), EnemyState.Path(PathState.Empty)))
                                    : Option<EnemyGameObject>.None,
                            // zombies
                            >= 216 and < 224 =>
                                Option<EnemyGameObject>.Some(CreateZombie(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-216) % 4), StandingOrMoving(216,planeValue))),
                            >= 234 and < 242 =>
                                difficultyLevel >= DifficultyLevel.IAmDeathIncarnate
                                    ? Option<EnemyGameObject>.Some(CreateZombie(difficultyLevel, mapPosition.x, mapPosition.y, (ushort)((planeValue-234) % 4), StandingOrMoving(234,planeValue)))
                                    : Option<EnemyGameObject>.None,
                            _ => throw new GameObjectFormatException($"Game object {planeValue} not recognised")
                        };
                        return
                            gameObjectOption.Match(
                                // ReSharper disable once ConvertClosureToMethodGroup - less clear
                                someValue => gameObjects.Add(someValue),
                                _ => gameObjects
                            );
                    }
                    return gameObjects;
                })
                .Select(ago =>
                    ago switch
                    {
                        EnemyGameObject ego => ego with
                        {
                            EnemyProperties = ego.EnemyProperties with
                            {
                                State = EnemyState.Path(new PathState(
                                  ego.CommonProperties.MapPosition.x + ego.EnemyProperties.Direction.ToDelta().x,
                                  ego.CommonProperties.MapPosition.y + ego.EnemyProperties.Direction.ToDelta().y,
                                  false
                                ))
                            }
                        },
                        _ => ago
                    }
                )
                .ToImmutableArray();
        }
    }
}