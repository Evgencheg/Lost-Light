using Content.Shared.CCVar;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorSystem
{
    private bool _isClientEmoteWindowActive;

    public void ClientChangedWindowStatus(bool isOpen)
    {
        _isClientEmoteWindowActive = isOpen;
        if (_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            ClientUpdateTyping();
    }
}
