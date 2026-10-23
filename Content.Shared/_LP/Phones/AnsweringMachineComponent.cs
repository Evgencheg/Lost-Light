using Robust.Shared.Serialization;

namespace Content.Shared._LP.Phones;

/// <summary>
/// Диктофон, который работает автоответчиком, если вставить его в подставку дискового телефона.
/// Когда на вызов не ответили, звонящему играет приветствие, а всё, что он говорит, пишется на кассету.
/// Логика только серверная.
/// </summary>
[RegisterComponent]
public sealed partial class AnsweringMachineComponent : Component
{
    /// <summary>
    /// Приветствие, которое записал владелец. Если не задано, играет <see cref="DefaultGreeting"/>.
    /// </summary>
    [DataField]
    public string? Greeting;

    [DataField]
    public LocId DefaultGreeting = "answering-machine-default-greeting";

    [DataField]
    public int MaxGreetingLength = 200;

    /// <summary>
    /// Дольше одного сообщения автоответчик не слушает.
    /// </summary>
    [DataField]
    public TimeSpan MaxMessageLength = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Сколько тишины после последней реплики звонящего завершает сообщение.
    /// </summary>
    [DataField]
    public TimeSpan SilenceTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// На кассете есть сообщения, которые ещё не слушали. Сбрасывается, когда диктофон вынимают из подставки.
    /// </summary>
    [ViewVariables]
    public bool NewMessages;
}

/// <summary>
/// Подставка телефона, в которую можно вставить автоответчик. Показывает индикатор новых сообщений.
/// </summary>
[RegisterComponent]
public sealed partial class AnsweringMachineStandComponent : Component;

[Serializable, NetSerializable]
public enum AnsweringMachineVisuals : byte
{
    NewMessages,
}
