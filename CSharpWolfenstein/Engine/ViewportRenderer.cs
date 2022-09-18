using System.Runtime.CompilerServices;
using CSharpWolfenstein.Assets;
using CSharpWolfenstein.Engine.RayCasting;
using CSharpWolfenstein.Extensions;
using CSharpWolfenstein.Game;

namespace CSharpWolfenstein.Engine;

public delegate WallRenderingResult WallRendererFunc(uint[] buffer, AssetPack assetPack, GameState game,
    (int width, int height) viewportSize, AbstractRayCaster rayCaster, int? stripToDraw=null);

public class ViewportRenderer
{
    private readonly WallRendererFunc _wallRenderer;
    private readonly AbstractRayCaster _rayCaster;

    public ViewportRenderer(WallRendererFunc wallRenderer, AbstractRayCaster rayCaster)
    {
        _wallRenderer = wallRenderer;
        _rayCaster = rayCaster;
    }

    public AbstractRayCaster RayCaster => _rayCaster;

    public uint[] UpdateFrameBuffer(AssetPack assetPack, GameState game, (int width, int height) viewportSize, int? stripToDraw=null)
    {
        uint[] buffer = new uint[viewportSize.height * viewportSize.width];
        _wallRenderer(buffer, assetPack, game, viewportSize, _rayCaster, stripToDraw);
        return buffer;
    }
}