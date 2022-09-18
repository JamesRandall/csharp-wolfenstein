/*
 * This is all based on the core model file in the F# version but twisted into C#.
 * My expectation is that I iterate on this as I get further into the project... its a starting point for now.
 */

using System.Collections.Immutable;
using System.Numerics;
using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine;
using OneOf;
using OneOf.Types;

using MapArray = System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<Cell>>;

public record Vector2D(double X, double Y)
{
    public Vector2D CrossProduct => new Vector2D(Y, X);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator *(Vector2D a, double b) => new Vector2D(a.X * b, a.Y * b);
    public static Vector2D operator /(Vector2D a, double b) => new Vector2D(a.X / b, a.Y / b);
    public double UnsquaredDistanceFrom(Vector2D other) => (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y);
    public Vector2D Normalize()
    {
        var distance = Math.Sqrt(X * X + Y * Y);
        return new(X / distance, Y / distance);
    }
    public Vector2D Abs() => new(Math.Abs(X), Math.Abs(Y));
    public Vector2D Reverse() => new(X * -1.0, Y * -1.0);

    public Vector2D Rotate(double angleInRadians)
    {
        var ca = Math.Cos(angleInRadians);
        var sa = Math.Sin(angleInRadians);
        return new Vector2D(ca * X - sa * Y, sa * X + ca * Y);
    }
    public string StringValue => $"{{ vX: {X}, vY: {Y} }}";
    public bool IsBetween(Vector2D a, Vector2D b) =>
        (a.Y * b.X - a.X * b.Y) * (a.Y * X - a.X * Y) >= 0 &&
        (X * b.X - X * b.Y) * (Y * a.X - X * a.Y) >= 0;

    public double Magnitude() => Math.Sqrt(X * X + Y * Y);

    public double AngleBetween(Vector2D b) => Math.Acos((X * b.X + Y * b.Y) / Magnitude() * b.Magnitude());

    public Vector2D LimitToMapUnit() =>
        new(X > 1.0 ? 1.0 : X < -1.0 ? -1.0 : X,
            X > 1.0 ? 1.0 : X < -1.0 ? -1.0 : X);

    public static Vector2D CreateFromMapPosition((int x, int y) mapPosition) =>
        new Vector2D(mapPosition.x + 0.5, mapPosition.y + 0.5);

    public static Vector2D Zero => new(0.0, 0.0);
    public static Vector2D One => new(1.0, 1.0);
}

public class Option<T> : OneOfBase<T, OneOf.Types.None>
{
    protected Option(OneOf<T, None> input) : base(input)
    {
    }

    public static Option<T> Some(T item) => new(OneOf<T, None>.FromT0(item));

    public static Option<T> None => new(OneOf<T, None>.FromT1(new None()));
}


public enum DifficultyLevel
{
    CanIPlayDaddy,
    DontHurtMe,
    BringEmOn,
    IAmDeathIncarnate
}

// TODO: I figured out the unit based approach to movement in Wolf quite late on and ended up with a mix of vectors
// and enums, I am wandering if I could use just a vector based approach and rely on equality instead of the enum +
// the vector (though some caution needed as x is reversed in the renderer)
// This is using properties rather than readonly's as vector is not immutable (shame on you MS, shame on you)
public static class Direction
{
    public static Vector2D North => new(0.0, -1.0);
    public static Vector2D NorthEast => new Vector2D(1.0, -1.0).Normalize();
    public static Vector2D East => new(1.0f, -0.0f);
    public static Vector2D SouthEast => new Vector2D(1.0f, 1.0f).Normalize();
    public static Vector2D South => new(0.0f, 1.0f);
    public static Vector2D SouthWest => new Vector2D(-1.0f, 1.0f).Normalize();
    public static Vector2D West => new(-1.0f, -0.0f);
    public static Vector2D NorthWest => new Vector2D(-1.0f, -1.0f).Normalize();
    public static Vector2D None => new(0.0f, 0.0f);
}

public enum MapDirection
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
    None
}

public class MapDirectionException : Exception
{
    public MapDirectionException(string msg) : base(msg)
    {
    }
}

public static class MapDirectionExtensions
{
    public static MapDirection Diagonal(this Tuple<MapDirection, MapDirection> directions)
    {
        return directions switch
        {
            (MapDirection.North, MapDirection.East) or (MapDirection.East, MapDirection.North) => MapDirection
                .NorthEast,
            (MapDirection.East, MapDirection.South) or (MapDirection.South, MapDirection.East) => MapDirection
                .SouthEast,
            (MapDirection.South, MapDirection.West) or (MapDirection.West, MapDirection.South) => MapDirection
                .SouthWest,
            (MapDirection.West, MapDirection.North) or (MapDirection.North, MapDirection.West) => MapDirection
                .NorthWest,
            (_, _) => throw new MapDirectionException("No diagnoal for those directions")
        };
    }

    public static (int x, int y) ToDelta(this MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => (0, -1),
            MapDirection.NorthEast => (1, -1),
            MapDirection.East => (1, 0),
            MapDirection.SouthEast => (1, 1),
            MapDirection.South => (0, 1),
            MapDirection.SouthWest => (-1, 1),
            MapDirection.West => (-1, 0),
            MapDirection.NorthWest => (-1, -1),
            _ => (0, 0)
        };
    }

    public static Vector2D ToVector(this MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => Direction.North,
            MapDirection.NorthEast => Direction.NorthEast,
            MapDirection.East => Direction.East,
            MapDirection.SouthEast => Direction.SouthEast,
            MapDirection.South => Direction.South,
            MapDirection.SouthWest => Direction.SouthWest,
            MapDirection.West => Direction.West,
            MapDirection.NorthWest => Direction.NorthWest,
            _ => Direction.None
        };
    }

    public static MapDirection Reverse(this MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => MapDirection.South,
            MapDirection.NorthEast => MapDirection.SouthWest,
            MapDirection.East => MapDirection.West,
            MapDirection.SouthEast => MapDirection.NorthWest,
            MapDirection.South => MapDirection.North,
            MapDirection.SouthWest => MapDirection.NorthEast,
            MapDirection.West => MapDirection.East,
            MapDirection.NorthWest => MapDirection.SouthEast,
            _ => MapDirection.None
        };
    }
}

public enum Side
{
    NorthSouth,
    EastWest
}

public record WallRenderingResult(
    ImmutableArray<double> ZIndexes,
    (int x, int y) WallInFrontOfPlayer,
    bool IsDoorInFrontOfPlayer,
    double DistanceToWallInFrontOfPlayer,
    // Going to see what its like using an option type in C#. null makes me nauseaus.
    Option<int> SpriteInFrontOfPlayerIndexOption
);

public enum SoundEffect
{
    UttGuards,
    Dog,
    DoorClose,
    DoorOpen,
    PlayerMachineGun,
    PlayerPistol,
    PlayerChainGun,
    Hoofafo,
    GutenTag,
    Mutti,
    GuardChainGun,
    GuardMachineGun,
    Aarggh,
    Aieeee,
    Ooof,
    SecretDoor,
    MeinLeben,
    GuardPistol,
    BubblesQuestion,
    VictoryYeah,
    Tick
}

public enum DoorDirection
{
    NorthSouth,
    EastWest
}

public enum DoorStatus
{
    Opening,
    Closing,
    Open,
    Closed
}

public record DoorState(
    int TextureIndex,
    DoorDirection DoorDirection,
    DoorStatus Status,
    double Offset,
    double TimeRemainingInAnimation,
    (int, int) MapPosition,
    int AreaOne,
    int AreaTwo
);


// Back in F# land this was a discriminated union, not having DUs is hurting my brain
public abstract record Cell((int x, int y) MapPosition);

public record Wall((int x, int y) MapPosition, int NorthSouthTextureIndex, int EastWestTextureIndex) : Cell(
    MapPosition)
{
    public bool IsExit =>
        NorthSouthTextureIndex == 40 || EastWestTextureIndex == 40 ||
        NorthSouthTextureIndex == 41 || EastWestTextureIndex == 41;

    public int TextureIndex(Side side) => side == Side.NorthSouth ? NorthSouthTextureIndex : EastWestTextureIndex;
}

public record Door((int x, int y) MapPosition, int DoorIndex) : Cell(MapPosition);

public record TurningPoint((int x, int y) MapPosition, MapDirection TurnsToDirection) : Cell(MapPosition);

public record Empty((int x, int y) MapPosition) : Cell(MapPosition);

public enum EnemyType
{
    Guard,
    Officer,
    SS,
    Dog,
    Zombie,
    FakeAdolf,
    Adolf,
    Fettgesicht,
    Schabbs,
    Gretel,
    Hans,
    Otto,
    Ghost
}

public record PathState(int TargetX, int TargetY, bool ChaseOnTargetReached)
{
    public static readonly PathState Empty = new PathState(-1, -1, false);
}

public class EnemyState : OneOfBase<
    EnemyState.Type.Standing,
    EnemyState.Type.Ambushing,
    EnemyState.Type.Attack,
    EnemyState.Type.Path,
    EnemyState.Type.Shoot,
    EnemyState.Type.Chase,
    EnemyState.Type.Die,
    EnemyState.Type.Dead,
    EnemyState.Type.Pain>
{
    private EnemyState(
        OneOf<Type.Standing, Type.Ambushing, Type.Attack, Type.Path,
            Type.Shoot, Type.Chase, Type.Die, Type.Dead,
            Type.Pain> input) : base(input)
    {
    }

    public static class Type
    {
        public record Standing;
        public record Ambushing;
        public record Attack;
        public record Path(PathState PathState);
        public record Shoot;
        public record Chase((int x, int y) TargetMapPosition);
        public record Die;
        public record Dead;
        public record Pain(EnemyState PreviousStateType);
    }

    public static readonly EnemyState Standing = new(new Type.Standing());
    public static readonly EnemyState Ambushing = new(new Type.Ambushing());
    public static readonly EnemyState Attack = new(new Type.Attack());
    public static EnemyState Path(PathState pathState) => new(new Type.Path(pathState));
    public static readonly EnemyState Shoot = new(new Type.Shoot());
    public static EnemyState Chase((int x, int y) targetMapPosition) =>
        new EnemyState(new Type.Chase(targetMapPosition));
    public static readonly EnemyState Die = new(new Type.Die());
    public static readonly EnemyState Dead = new(new Type.Dead());
    public static EnemyState Pain(EnemyState previousStateType) => new(new Type.Pain(previousStateType));
}

public record BasicGameObjectProperties(
    Vector2D Position,
    Vector2D PlayerRelativePosition,
    double UnsquaredDistanceFromPlayer,
    int SpriteIndex,
    bool CollidesWithBullets,
    bool Pickupable,
    int HitPointsRestored,
    int AmmoRestored,
    int LivesRestored,
    int Score,
    bool Blocking
)
{
    public (int, int) MapPosition => ((int) Position.X, (int) Position.Y);
}

public record EnemyProperties(
    EnemyType EnemyType,
    MapDirection Direction,
    int[] DeathSpriteIndexes,
    int[] AttackSpriteIndexes,
    int SpriteBlocks,
    int FramesPerBlock,
    int CurrentAnimationFrame,
    double TimeUntilNextAnimationFrame,
    EnemyState State,
    bool IsFirstAttack,
    bool FireAtPlayerRequired,
    bool MoveToChaseRequired,
    int HitPoints,
    int HurtSpriteIndex,
    double PatrolSpeed,
    double ChaseSpeed
);

public abstract record AbstractGameObject(BasicGameObjectProperties CommonProperties);

public record StaticGameObject(BasicGameObjectProperties CommonProperties) : AbstractGameObject(CommonProperties);

public record EnemyGameObject(BasicGameObjectProperties CommonProperties, EnemyProperties EnemyProperties)
    : AbstractGameObject(CommonProperties)
{
    public Vector2D DirectionVector => EnemyProperties.Direction.ToVector();
    public int StationarySpriteBlockIndex => CommonProperties.SpriteIndex;
    public int NumberOfMovementAnimationFrames => EnemyProperties.SpriteBlocks - 1;

    public int MovementSpriteBlockIndex(int frame)
    {
        return (CommonProperties.SpriteIndex + frame) * EnemyProperties.FramesPerBlock;
    }

    public bool IsAlive => EnemyProperties.State.Value is EnemyState.Type.Dead or EnemyState.Type.Die;

    public int BaseSpriteIndexForState =>
        EnemyProperties.State.Value switch
        {
            EnemyState.Type.Standing => 0,
            EnemyState.Type.Chase or EnemyState.Type.Path => MovementSpriteBlockIndex(EnemyProperties
                .CurrentAnimationFrame),
            EnemyState.Type.Attack => EnemyProperties.AttackSpriteIndexes[EnemyProperties.CurrentAnimationFrame],
            EnemyState.Type.Pain => EnemyProperties.HurtSpriteIndex,
            _ => CommonProperties.SpriteIndex
        };

    public double AnimationTimeForState =>
        EnemyProperties.State.Value switch
        {
            EnemyState.Type.Attack => 200.0,
            EnemyState.Type.Chase => 100.0,
            EnemyState.Type.Path => 300.0,
            EnemyState.Type.Pain => 100.0,
            EnemyState.Type.Dead or EnemyState.Type.Dead => 100.0,
            _ => 0.0
        };

    public int SpriteIndexForAnimationFrame =>
        EnemyProperties.State.Value switch
        {
            EnemyState.Type.Attack => EnemyProperties.AttackSpriteIndexes[EnemyProperties.CurrentAnimationFrame],
            EnemyState.Type.Dead or EnemyState.Type.Dead => EnemyProperties.DeathSpriteIndexes[EnemyProperties.CurrentAnimationFrame],
            _ => StationarySpriteBlockIndex
        };
    
    public int AnimationFrames =>
        EnemyProperties.State.Value switch
        {
            EnemyState.Type.Attack => EnemyProperties.AttackSpriteIndexes.Length,
            EnemyState.Type.Dead or EnemyState.Type.Dead => EnemyProperties.DeathSpriteIndexes.Length,
            _ => 1
        };

    public bool IsBoss =>
        EnemyProperties.EnemyType switch
        {
            EnemyType.Guard or EnemyType.Officer or EnemyType.Dog or EnemyType.SS or EnemyType.Zombie => false,
            _ => true
        };

    public bool IsVisible => true;
}

public enum WeaponType { Knife, Pistol, MachineGun, ChainGun }

public record PlayerWeapon(
    Texture[] Sprites,
    int CurrentFrame,
    int Damage,
    bool AutoRepeat,
    bool RequiresAmmunition,
    int StatusBarImageIndex,
    WeaponType WeaponType
)
{
    public int AnimationFrames => Sprites.Length;
    public Texture CurrentSprite => Sprites[CurrentFrame];
}

[Flags]
public enum ControlState
{
    None          = 0b0000000000000,
    Forward       = 0b0000000000001,
    TurningLeft   = 0b0000000000010,
    TurningRight  = 0b0000000000100,
    StrafingLeft  = 0b0000000001000,
    StrafingRight = 0b0000000010000,
    Backward      = 0b0000000100000,
    Fire          = 0b0000001000000,
    Action        = 0b0000010000000,
    Weapon0       = 0b0000100000000,
    Weapon1       = 0b0001000000000,
    Weapon2       = 0b0010000000000,
    Weapon3       = 0b0100000000000
}

public record Player(
    int Score,
    int Lives,
    int Health,
    double Radius,
    int CurrentWeaponIndex,
    int Ammunition,
    List<PlayerWeapon> Weapons,
    int CurrentFaceIndex,
    double TimeToFaceChangeMs
)
{
    public PlayerWeapon CurrentWeapon => Weapons[CurrentWeaponIndex];

    public static Player NewPlayer(AssetPack assetPack)
    {
        return new Player(
            Score: 0,
            Lives: 3,
            Health: 100,
            Radius: 0.5,
            CurrentWeaponIndex: 1,
            Ammunition: 9,
            Weapons: new List<PlayerWeapon>() { assetPack.Weapons[WeaponType.Knife], assetPack.Weapons[WeaponType.Pistol] },
            CurrentFaceIndex:0,
            TimeToFaceChangeMs:1500.0
        );
    }
}

public record Camera(
    Vector2D Position,
    Vector2D Direction,
    Vector2D Plane,
    double FieldOfView
);

public record SpriteLayout(
    uint Offset,
    uint FirstColumn,
    uint LastColumn,
    uint PixelPoolOffset
);

public record CompositeArea(int Area, HashSet<int> ConnectedTo);

public enum PixelDissolverState { Forwards, Backwards, Transitioning, Stopped }

public record PixelDissolver(
    IReadOnlyCollection<(int, int)> RemainingPixels,
    IReadOnlyCollection<(int, int)> DrawnPixels,
    double PixelSize,
    double PauseTimeRemaining,
    PixelDissolverState DissolverState
)
{
    public int TotalPixels => RemainingPixels.Count + DrawnPixels.Count;
    public bool IsComplete => DissolverState == PixelDissolverState.Backwards && DrawnPixels.Count == 0;
}

public record StatusBarGraphics(
    Texture Background,
    IReadOnlyCollection<IReadOnlyCollection<Texture>> HealthFaces,
    Texture Dead,
    Texture GrinFace,
    Texture GreyFace,
    IReadOnlyCollection<Texture> Font,
    IReadOnlyCollection<Texture> Weapons
);

// TODO: These almost certainly belong somewhere else
public static class Constants
{
    public const double TextureWidth = 64.0;
    public const double TextureHeight = 64.0;
    public const int FiringTolerance = 40;
    public const double AttackAnimationFrameTime = 200.0;
}