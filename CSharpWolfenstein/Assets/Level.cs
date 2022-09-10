using System.Numerics;

namespace CSharpWolfenstein.Assets;
public class InvalidDoorException : Exception { }

public class StartingPositionException : Exception { }


public record Level(
    int Width,
    int Height,
    Cell[,] Map,
    int[,] Areas,
    int NumberOfAreas,
    IReadOnlyCollection<AbstractGameObject> AbstractGameObjects,
    Camera PlayerStartingPosition,
    DoorState[] Doors
)
{
    public static Level Create(AssetPack assetPack, DifficultyLevel difficulty, int levelIndex)
    {
        const int mapSize = 64;
        bool IsDoor(ushort value) => value >= 90 && value <= 101;
        int WallTextureIndex(ushort value) => 2 * value - 1;

        ushort GetPlaneValue(byte[] plane, (int colIndex, int rowIndex) mapPosition) =>
            plane.GetUint16(2 * (mapPosition.colIndex + mapSize * mapPosition.rowIndex));

        Wall CreateWall((int,int) mapPosition, ushort value) =>
            new Wall(
                MapPosition: mapPosition,
                NorthSouthTextureIndex: WallTextureIndex(value),
                EastWestTextureIndex: WallTextureIndex(value) - 1
            );

        Door CreateDoor((int, int) mapPosition, ushort value, List<DoorState> doors)
        {
            var (textureIndex, direction) = value switch
            {
                90 => (99,DoorDirection.NorthSouth),
                92 => (105,DoorDirection.NorthSouth),
                94 => (105,DoorDirection.NorthSouth),
                100 => (103,DoorDirection.NorthSouth),
                91 => (98,DoorDirection.EastWest),
                93 => (104,DoorDirection.EastWest),
                95 => (104,DoorDirection.EastWest),
                101 => (102,DoorDirection.EastWest),
                _ => throw new InvalidDoorException()
            };
            var doorState = new DoorState(
                TextureIndex:textureIndex,
                DoorDirection:direction,
                Status:DoorStatus.Closed,
                Offset: 0.0,
                TimeRemainingInAnimation: 0.0,
                MapPosition: mapPosition,
                AreaOne: -1,
                AreaTwo: -1
            );
            doors.Add(doorState);
            return new Door(MapPosition: mapPosition, DoorIndex: doors.Count - 1);
        }

        Cell CreateTurningPointOrEmpty((int, int) mapPosition, ushort value, ushort objectValue)
        {
            return objectValue switch
            {
                0x5A => new TurningPoint(mapPosition, MapDirection.East),
                0x5B => new TurningPoint(mapPosition, MapDirection.NorthEast),
                0x5C => new TurningPoint(mapPosition, MapDirection.North),
                0x5D => new TurningPoint(mapPosition, MapDirection.NorthWest),
                0x5E => new TurningPoint(mapPosition, MapDirection.West),
                0x5F => new TurningPoint(mapPosition, MapDirection.SouthWest),
                0x60 => new TurningPoint(mapPosition, MapDirection.South),
                0x61 => new TurningPoint(mapPosition, MapDirection.SouthEast),
                _ => new Empty(mapPosition)
            };
        }

        (Cell[,] cells, List<DoorState> doors) ConstructMapLayout(byte[] plane0, byte[] plane1)
        {
            var doorList = new List<DoorState>();
            var cellArray = new Cell[mapSize, mapSize];
            Enumerable.Range(0,mapSize).Iter(rowIndex =>
                Enumerable.Range(0,mapSize).Iter(colIndex =>
                {
                    var mapPosition = (colIndex, rowIndex);
                    var rawMapCell = GetPlaneValue(plane0, mapPosition);
                    var cell = rawMapCell switch
                    {
                        <= 63 => CreateWall(mapPosition, rawMapCell),
                        >= 90 and <= 101 => CreateDoor(mapPosition, rawMapCell, doorList),
                        _ => CreateTurningPointOrEmpty(mapPosition, rawMapCell, GetPlaneValue(plane1, mapPosition)) 
                    };
                    cellArray[rowIndex, colIndex] = cell;
                })    
            );
            return (cellArray, doorList);
        }
        
        void PatchWallsSurroundingDoors(Cell[,] cellsToPatch, List<DoorState> doorStates)
        {
            Enumerable.Range(0,mapSize).Iter(rowIndex =>
                Enumerable.Range(0,mapSize).Iter(colIndex =>
                {
                    var cell = cellsToPatch[rowIndex, colIndex];
                    if (cell is Wall wall)
                    {
                        var hasNorthSouthDoor =
                            doorStates.Exists(d => d.MapPosition == (colIndex + 1, rowIndex)) ||
                            doorStates.Exists(d => d.MapPosition == (colIndex - 1, rowIndex));
                        var hasEastWestDoor =
                            doorStates.Exists(d => d.MapPosition == (colIndex, rowIndex + 1)) ||
                            doorStates.Exists(d => d.MapPosition == (colIndex, rowIndex - 1));
                        cellsToPatch[rowIndex, colIndex] = wall with
                        {
                            EastWestTextureIndex = hasEastWestDoor ? 101 : wall.EastWestTextureIndex,
                            NorthSouthTextureIndex = hasNorthSouthDoor ? 100 : wall.NorthSouthTextureIndex
                        };
                    }
                })    
            );
        }

        Camera GetStartingPosition(byte[] plane)
        {
            for (int rowIndex = 0; rowIndex < mapSize; rowIndex++)
            {
                for (int colIndex = 0; colIndex < mapSize; colIndex++)
                {
                    var planeValue = GetPlaneValue(plane, (colIndex, rowIndex));
                    if (planeValue >= 19 && planeValue <= 22)
                    {
                        var direction = planeValue switch
                        {
                            19 => new Vector2(0.0f, -1.0f),
                            20 => new Vector2(-1.0f, 0.0f),
                            21 => new Vector2(0.0f, 1.0f),
                            22 => new Vector2(1.0f, 0.0f),
                            _ => throw new StartingPositionException()
                        };
                        var fieldOfView = 1.0f;
                        var startingPosition = new Vector2(mapSize - colIndex - 0.5f, rowIndex + 0.5f);
                        return new Camera(
                            Position: startingPosition,
                            Direction: direction,
                            Plane: Vector2.Multiply(direction.CrossProduct(), new Vector2(fieldOfView,fieldOfView)),
                            FieldOfView: fieldOfView
                        );
                    }
                }
            }

            throw new StartingPositionException();
        }
        
        var mapHeaderOffset = (int) assetPack.MapHeader.GetUint32(2 + 4 * levelIndex);
        var levelMapHeader = assetPack.GameMaps[mapHeaderOffset..(mapHeaderOffset + 42)];
        var plane0Start = (int) levelMapHeader.GetUint32(0);
        var plane0End = levelMapHeader.GetUint16(12) + plane0Start;
        var plane1Start = (int) levelMapHeader.GetUint32(4);
        var plane1End = levelMapHeader.GetUint16(14) + plane1Start;
        var plane0 = assetPack.GameMaps[plane0Start..plane0End].CarmackDecode().RlewDecode(assetPack.MapHeader);
        var plane1 = assetPack.GameMaps[plane1Start..plane1End].CarmackDecode().RlewDecode(assetPack.MapHeader);
        var (cells, doors) = ConstructMapLayout(plane0, plane1);
        PatchWallsSurroundingDoors(cells, doors);

        return new Level(
            Width: mapSize,
            Height: mapSize,
            Map: cells,
            Areas: new int [0, 0],
            NumberOfAreas: 0,
            AbstractGameObjects: Array.Empty<AbstractGameObject>(),
            PlayerStartingPosition: GetStartingPosition(plane1),
            Doors: doors.ToArray());
    }
}
