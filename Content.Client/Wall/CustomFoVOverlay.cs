// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted for CHUDSTATION2 from SS220 PR #537 (Aesthetics rework).
using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Wall;

/// <summary>
/// Draws the wall-edge FOV mask so the reverse side of an opaque wall is not visible.
/// The same mask is also rendered into bloom's source and occlusion targets.
/// </summary>
public sealed partial class CustomFoVOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    private readonly EntityQuery<OccluderComponent> _occluderQuery;
    private readonly EntityQuery<WallMountComponent> _wallMountQuery;
    private readonly EntityQuery<DoorComponent> _doorQuery;
    private readonly EntityQuery<TransformComponent> _transformQuery;
    private readonly EntityQuery<MapGridComponent> _gridQuery;

    // This marker is an explicit whitelist: wallmounts are intentionally not included.
    private readonly HashSet<Entity<FoVOverlayComponent>> _walls = new();
    private readonly Dictionary<EntityUid, Dictionary<Vector2i, Entity<TransformComponent>>> _wallMap = new();

    private readonly SpriteSpecifier _fovCorner;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public CustomFoVOverlay()
    {
        IoCManager.InjectDependencies(this);

        _lookup = _entMan.System<EntityLookupSystem>();
        _map = _entMan.System<SharedMapSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
        _sprite = _entMan.System<SpriteSystem>();

        _occluderQuery = _entMan.GetEntityQuery<OccluderComponent>();
        _wallMountQuery = _entMan.GetEntityQuery<WallMountComponent>();
        _doorQuery = _entMan.GetEntityQuery<DoorComponent>();
        _transformQuery = _entMan.GetEntityQuery<TransformComponent>();
        _gridQuery = _entMan.GetEntityQuery<MapGridComponent>();

        _fovCorner = new SpriteSpecifier.Texture(new("SS220/Misc/fov_corner.png"));
        _shader = _prototype.Index(UnshadedShader).InstanceUnique();

        ZIndex = (int) Shared.DrawDepth.DrawDepth.WallFovOverlay;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _wallMap.Clear();
        _walls.Clear();

        if (args.Viewport.Eye is not { } eye || !eye.DrawFov)
            return;

        PrepareBloomOcclusion(args.MapId, args.WorldBounds);
        DrawBloomOcclusion(args.WorldHandle, eye, Matrix3x2.Identity);
    }

    /// <summary>
    /// Builds the exact same wall/closed-airlock list used by the visible FOV overlay.
    /// Entities must explicitly have FoVOverlayComponent and an enabled OccluderComponent.
    /// </summary>
    internal void PrepareBloomOcclusion(MapId mapId, Box2Rotated worldBounds)
    {
        PrepareBloomOcclusion(mapId, worldBounds.CalcBoundingBox());
    }

    internal void PrepareBloomOcclusion(MapId mapId, Box2 worldBounds)
    {
        _wallMap.Clear();
        _walls.Clear();

        // Include a one-tile margin so wall corners at the viewport or bloom-cell edge are still drawn.
        _lookup.GetEntitiesIntersecting(mapId, worldBounds.Enlarged(1f), _walls);

        foreach (var wall in _walls)
        {
            var uid = wall.Owner;

            // Wall-mounted signs, controls and fixtures never contribute to the FOV wall mask.
            // Firelocks also have WallMountComponent, but are doors and must still block the FOV.
            if (_wallMountQuery.HasComp(uid) && !_doorQuery.HasComp(uid))
                continue;

            if (!_occluderQuery.TryGetComponent(uid, out var occluder) || !occluder.Enabled)
                continue;

            if (!_transformQuery.TryGetComponent(uid, out var xform) ||
                !xform.Anchored ||
                xform.GridUid is not { } gridUid)
            {
                continue;
            }

            if (!_gridQuery.TryGetComponent(gridUid, out var grid))
                continue;

            var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

            if (!_wallMap.TryGetValue(gridUid, out var gridWalls))
            {
                gridWalls = new Dictionary<Vector2i, Entity<TransformComponent>>();
                _wallMap.Add(gridUid, gridWalls);
            }

            // A tile can only contribute one wall mask.
            if (!gridWalls.ContainsKey(tile))
                gridWalls.Add(tile, new Entity<TransformComponent>(uid, xform));
        }
    }

    /// <summary>
    /// Draws the prepared FOV mask. worldToTarget maps world coordinates into the active render target.
    /// </summary>
    internal void DrawBloomOcclusion(DrawingHandleWorld handle, IEye eye, Matrix3x2 worldToTarget, Box2? worldBounds = null)
    {
        if (!eye.DrawFov)
            return;

        var texture = _sprite.Frame0(_fovCorner);
        if (texture == null)
            return;

        var oldTransform = handle.GetTransform();
        var oldShader = handle.GetShader();

        try
        {
            handle.UseShader(_shader);

            void DrawFoVCorner(Vector2 worldPosition, Angle worldRotation)
            {
                var xAxis = worldRotation.RotateVec(Vector2.UnitX);
                var yAxis = worldRotation.RotateVec(Vector2.UnitY);
                var transform = new Matrix3x2(
                    xAxis.X, xAxis.Y,
                    yAxis.X, yAxis.Y,
                    worldPosition.X, worldPosition.Y);

                handle.SetTransform(transform * worldToTarget);
                handle.DrawTexture(texture, new Vector2(-0.5f, -0.5f));
            }

            foreach (var (gridUid, gridWalls) in _wallMap)
            {
                foreach (var (tile, wall) in gridWalls)
                {
                    var (worldPosition, _) = _transform.GetWorldPositionRotation(wall.Comp);
                    if (worldBounds is { } bounds &&
                        !bounds.Enlarged(1f).Intersects(Box2.CenteredAround(worldPosition, Vector2.One)))
                    {
                        continue;
                    }

                    var gridRotation = _transform.GetWorldRotation(gridUid);
                    var toEye = eye.Position.Position - worldPosition;
                    var gridXAxis = gridRotation.RotateVec(Vector2.UnitX);
                    var gridYAxis = gridRotation.RotateVec(Vector2.UnitY);

                    Vector2i GetDirRelativeToEdge(Vector2i edge)
                    {
                        var relativePos = new Vector2(
                            Vector2.Dot(toEye, gridXAxis),
                            Vector2.Dot(toEye, gridYAxis)) + edge * 0.5f;

                        return new Vector2i(
                            (int) MathF.Sign(relativePos.X),
                            (int) MathF.Sign(relativePos.Y));
                    }

                    var southNeighbour = gridWalls.ContainsKey(tile + Vector2i.Down);
                    var southShadowed = GetDirRelativeToEdge(Vector2i.Up).Y > 0;
                    var southObscured = southShadowed || southNeighbour;

                    var northNeighbour = gridWalls.ContainsKey(tile + Vector2i.Up);
                    var northShadowed = GetDirRelativeToEdge(Vector2i.Down).Y < 0;
                    var northObscured = northShadowed || northNeighbour;

                    var eastNeighbour = gridWalls.ContainsKey(tile + Vector2i.Right);
                    var eastShadowed = GetDirRelativeToEdge(Vector2i.Left).X < 0;
                    var eastObscured = eastShadowed || eastNeighbour;

                    var westNeighbour = gridWalls.ContainsKey(tile + Vector2i.Left);
                    var westShadowed = GetDirRelativeToEdge(Vector2i.Right).X > 0;
                    var westObscured = westShadowed || westNeighbour;

                    // Southwest corner.
                    if (southObscured && westObscured && !(!southShadowed && !westShadowed))
                        DrawFoVCorner(worldPosition, gridRotation);

                    // Southeast corner.
                    if (southObscured && eastObscured && !(!southShadowed && !eastShadowed))
                        DrawFoVCorner(worldPosition, gridRotation + Angle.FromDegrees(90));

                    // Northeast corner.
                    if (northObscured && eastObscured && !(!northShadowed && !eastShadowed))
                        DrawFoVCorner(worldPosition, gridRotation + Angle.FromDegrees(180));

                    // Northwest corner.
                    if (northObscured && westObscured && !(!northShadowed && !westShadowed))
                        DrawFoVCorner(worldPosition, gridRotation + Angle.FromDegrees(270));
                }
            }
        }
        finally
        {
            handle.UseShader(oldShader);
            handle.SetTransform(oldTransform);
        }
    }
}
