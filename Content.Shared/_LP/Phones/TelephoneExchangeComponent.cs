namespace Content.Shared._LP.Phones;

/// <summary>
/// АТС. Телефоны с <c>RequiresExchange</c> звонят только через рабочую АТС на своей карте:
/// ей нужно питание, а саботаж (перерезанный провод) обрывает связь, пока её не починят.
/// Логика только серверная, компонент лежит в Shared, чтобы клиент знал о нём из прототипа.
/// </summary>
[RegisterComponent]
public sealed partial class TelephoneExchangeComponent : Component
{
    /// <summary>
    /// Связь оборвана саботажем: провод АТС перерезан.
    /// </summary>
    [DataField]
    public bool Sabotaged;
}
