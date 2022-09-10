using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine;

namespace CSharpWolfenstein.Game;

public record GameState(
    int Level,
    Cell[,] Map,
    int[,] Areas,
    IReadOnlyCollection<CompositeArea> CompositeAreas,
    IReadOnlyCollection<AbstractGameObject> GameObjects,
    Player Player,
    Camera Camera,
    ControlState ControlState,
    bool IsFiring,
    Option<double> TimeToNextWeaponFrame,
    DoorState[] Doors,
    Option<OverlayAnimation> ViewportFilter,
    Option<PixelDissolver> PixelDissolver,
    Func<GameState, Player, GameState> ResetLevel
)
{
    public (int x, int y) PlayerMapPosition => ((int) Camera.Position.X, (int) Camera.Position.Y);
    public bool IsPlayerRunning => (ControlState & ControlState.Forward) == ControlState.Forward;

    public static GameState NewGame(AssetPack assetPack, DifficultyLevel difficultyLevel)
    {
        Level level = CSharpWolfenstein.Assets.Level.Create(assetPack, difficultyLevel, 0);
        return new GameState(
            Level: 0,
            Map: level.Map,
            Areas: new int[0, 0],
            CompositeAreas: Array.Empty<CompositeArea>(),
            GameObjects: Array.Empty<AbstractGameObject>(),
            Player: Player.NewPlayer(assetPack),
            Camera: level.PlayerStartingPosition,
            ControlState: ControlState.None,
            IsFiring: false,
            TimeToNextWeaponFrame: Option<double>.None,
            Doors: level.Doors,
            ViewportFilter: Option<OverlayAnimation>.None,
            PixelDissolver: Option<PixelDissolver>.None,
            ResetLevel: (game, player) => game
        );
    }
}