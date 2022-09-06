namespace CSharpWolfenstein.Assets;

public record StatusBarTextures(
    Texture Background,
    IReadOnlyCollection<IReadOnlyCollection<Texture>> HealthFaces,
    Texture Dead,
    Texture Grin,
    Texture Grey,
    IReadOnlyCollection<Texture> Font,
    IReadOnlyCollection<Texture> Weapons
    );