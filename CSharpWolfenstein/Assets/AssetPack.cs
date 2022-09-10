using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CSharpWolfenstein.Assets;

public record AssetPack(
    StatusBarTextures StatusBar,
    Texture[] Sprites,
    Texture[] Walls,
    IReadOnlyDictionary<WeaponType, PlayerWeapon> Weapons,
    byte[] MapHeader,
    byte[] GameMaps)
{
    public static async Task<AssetPack> Load(HttpClient httpClient, double scaleFactor, (double width, double height) viewportSize)
    {
        const bool isSharewareVersion = true;
        var extension = isSharewareVersion ? "WL1" : "WL6";
        
        async Task<Texture> LoadPngAsTexture(string path)
        {
            var png = await httpClient.GetByteArrayAsync(new Uri(path, UriKind.Relative));
            var image = Image.Load<Rgba32>(png);
            var pixels = new uint[image.Height, image.Width];
            Enumerable.Range(0, image.Height).Iter(row =>
                Enumerable.Range(0, image.Width).Iter(col =>
                    pixels[row, col] = image[col, row].PackedValue
                )
            );
            
            return new Texture(Pixels: pixels, Width: image.Width, Height: image.Height);
        }

        async Task<Texture[]> LoadPngsAsTextures(Func<int, string> pathFactory, IEnumerable<int> range)
        {
            return await range
                .Select(textureIndex => LoadPngAsTexture(pathFactory(textureIndex)))
                .ToArrayAsync();
        }

        async Task<Texture[]> LoadPngsAsTexturesWithEmptyPadding(Func<int, string> pathFactory, Func<int,bool> includeTexture, IEnumerable<int> range)
        {
            return await range
                .Select(textureIndex =>
                {
                    if (!includeTexture(textureIndex))
                        return Task.FromResult(new Texture(Pixels: new uint[0, 0], Width: 0, Height: 0));
                    return LoadPngAsTexture(pathFactory(textureIndex));
                })
                .ToArrayAsync();
        }
        
        var fontTextures = await LoadPngsAsTextures(i => i == 10 ? $"assets/statusBar/font/_.png" :$"assets/statusBar/font/{i}.png", Enumerable.Range(0,11));
        var statusBarTextures = await LoadPngsAsTextures(i => $"assets/statusBar/PIC{(i+109):D5}.png", Enumerable.Range(0,24));
        // Sprites and walls are not scaled - they are scaled during the render loop based on the distance from the player
        bool IsSpriteIncluded(int index) => !isSharewareVersion || index <= 186 || (index >= 296 && index <= 306) || (index >= 408 && index <= 435);
        bool IsWallIncludes(int index) => !isSharewareVersion || index <= 55 || index >= 98;
        var sprites = await LoadPngsAsTexturesWithEmptyPadding(i => $"assets/sprites/SPR{i:D5}.png", IsSpriteIncluded, Enumerable.Range(0,436));
        var wallTextures = await LoadPngsAsTexturesWithEmptyPadding(i => $"assets/walls/WAL{i:D5}.png", IsWallIncludes, Enumerable.Range(0,106));
        var knifeSprites = sprites[416..420];
        var pistolSprites = new List<Texture>(sprites[421..425]);
        pistolSprites.AddRange(sprites[422..424].Reverse());
        var machineGunSprites = sprites[426..430];
        var chainGunSprites = sprites[431..435];
        var weaponScaleFactor = viewportSize.height / sprites[416].Height;
        var knife = new PlayerWeapon(
            Sprites: knifeSprites.Scale(weaponScaleFactor),
            CurrentFrame:0,
            Damage:25,
            AutoRepeat:false,
            RequiresAmmunition:false,
            StatusBarImageIndex:0,
            WeaponType.Knife
        );
        var pistol = new PlayerWeapon(
            Sprites: pistolSprites.ToArray().Scale(weaponScaleFactor),
            CurrentFrame:0,
            Damage:25,
            AutoRepeat:false,
            RequiresAmmunition:false,
            StatusBarImageIndex:0,
            WeaponType.Knife
        );
        var machineGun = new PlayerWeapon(
            Sprites: machineGunSprites.ToArray().Scale(weaponScaleFactor),
            CurrentFrame:0,
            Damage:25,
            AutoRepeat:true,
            RequiresAmmunition:false,
            StatusBarImageIndex:2,
            WeaponType.Knife
        );
        var chainGun = new PlayerWeapon(
            Sprites: chainGunSprites.ToArray().Scale(weaponScaleFactor),
            CurrentFrame:0,
            Damage:25,
            AutoRepeat:true,
            RequiresAmmunition:false,
            StatusBarImageIndex:3,
            WeaponType.Knife
        );
        return new AssetPack(
            MapHeader: await httpClient.GetByteArrayAsync(new Uri($"assets/MAPHEAD.{extension}", UriKind.Relative)),
            GameMaps: await httpClient.GetByteArrayAsync(new Uri($"assets/GAMEMAPS.{extension}", UriKind.Relative)),
            Sprites: sprites,
            Walls: wallTextures,
            Weapons: new Dictionary<WeaponType, PlayerWeapon>()
            {
                {WeaponType.Knife, knife},
                {WeaponType.Pistol, pistol},
                {WeaponType.ChainGun,chainGun},
                {WeaponType.MachineGun,machineGun}
            },
            StatusBar: new StatusBarTextures(
                Background: (await LoadPngAsTexture("assets/statusBar/background.png")).Scale(scaleFactor),
                HealthFaces: new []
                {
                    statusBarTextures[..2].Scale(scaleFactor),
                    statusBarTextures[3..5].Scale(scaleFactor),
                    statusBarTextures[6..8].Scale(scaleFactor),
                    statusBarTextures[9..11].Scale(scaleFactor),
                    statusBarTextures[12..14].Scale(scaleFactor),
                    statusBarTextures[15..17].Scale(scaleFactor),
                    statusBarTextures[18..20].Scale(scaleFactor)
                },
                Dead: statusBarTextures[21].Scale(scaleFactor),
                Grin: statusBarTextures[22].Scale(scaleFactor),
                Grey: statusBarTextures[23].Scale(scaleFactor),
                Font: fontTextures.Scale(scaleFactor),
                Weapons: new[]
                {
                    await LoadPngAsTexture("assets/statusBar/weapons/knife.png"),
                    await LoadPngAsTexture("assets/statusBar/weapons/pistol.png"),
                    await LoadPngAsTexture("assets/statusBar/weapons/machineGun.png"),
                    await LoadPngAsTexture("assets/statusBar/weapons/chainGun.png")
                }.Scale(scaleFactor)
            )
        );
    }
};