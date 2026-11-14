using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light.Bloom;

/// <summary>
/// Stores bloom-compatible lights for spatial lookup.
/// </summary>
[RegisterComponent]
public sealed partial class BloomOverlayTreeComponent : Component, IComponentTreeComponent<BloomOverlayVisualsComponent>
{
    public DynamicTree<ComponentTreeEntry<BloomOverlayVisualsComponent>> Tree { get; set; } = default!;
}
