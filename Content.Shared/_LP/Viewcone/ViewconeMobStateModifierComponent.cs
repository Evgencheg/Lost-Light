using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._LP.Viewcone;

/// <summary>
/// Меняет угол конуса зрения (<see cref="Content.Shared._ES.Viewcone.Components.ESViewconeComponent"/>)
/// в зависимости от состояния моба. Значения складываются с остальными модификаторами так же, как у одежды.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewconeMobStateModifierComponent : Component
{
    /// <summary>
    /// Добавка к углу в градусах для каждого состояния. Состояния без записи угол не меняют.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<MobState, float> Modifiers = new();
}
