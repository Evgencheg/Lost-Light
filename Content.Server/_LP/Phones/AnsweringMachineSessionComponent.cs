using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._LP.Phones;

/// <summary>
/// Висит на трубке звонящего, пока автоответчик слушает его сообщение.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AnsweringMachineSessionComponent : Component
{
    /// <summary>Диктофон-автоответчик, на который идёт запись.</summary>
    public EntityUid Machine;

    /// <summary>Трубка вызываемого телефона, которая звонила.</summary>
    public EntityUid Callee;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Started;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastSpeech;

    /// <summary>Сколько реплик записано.</summary>
    public int Recorded;
}
