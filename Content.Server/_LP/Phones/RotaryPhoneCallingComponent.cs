using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._LP.Phones;

/// <summary>
/// Висит на трубке, пока исходящий вызов звонит и никто не ответил.
/// Нужна, чтобы тикали только звонящие телефоны, а не все.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RotaryPhoneCallingComponent : Component
{
    /// <summary>
    /// Когда вызов сбрасывается, если трубку так и не взяли.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;
}
