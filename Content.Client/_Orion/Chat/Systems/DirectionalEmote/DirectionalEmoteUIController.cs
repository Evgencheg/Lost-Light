using Content.Client.Chat.TypingIndicator;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Orion.Chat.Systems.DirectionalEmote;

[UsedImplicitly]
public sealed class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private DirectionalEmoteSystem _directionalEmoteSystem = default!;
    [UISystemDependency] private TypingIndicatorSystem _typingIndicator = default!;

    private DirectionalEmoteWindow? _emoteWindow;

    public void ToggleWindow(NetEntity source, NetEntity target)
    {
        if (_emoteWindow is { Disposed: false })
        {
            _emoteWindow.Close();
            return;
        }

        _emoteWindow = CreateWindow(source, target);
        _emoteWindow.OpenCentered();
        _emoteWindow.MoveToFront();
        _typingIndicator.ClientChangedWindowStatus(true);
    }

    private DirectionalEmoteWindow CreateWindow(NetEntity source, NetEntity target)
    {
        var window = UIManager.CreateWindow<DirectionalEmoteWindow>();
        window.Source = source;
        window.Target = target;

        window.MessageChanged += () =>
        {
            _typingIndicator.ClientChangedChatText();
            _typingIndicator.ClientChangedWindowStatus(true);
        };

        window.AcceptPressed += () =>
        {
            _typingIndicator.ClientSubmittedChatText();
            _directionalEmoteSystem.ShowMessage(window.Source, window.Target, window.Text);
            window.Close();
        };

        window.OnClose += () => _typingIndicator.ClientChangedWindowStatus(false);

        return window;
    }
}
