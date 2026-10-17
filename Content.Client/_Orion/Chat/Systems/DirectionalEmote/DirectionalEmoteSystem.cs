using Content.Shared._Orion.Chat.Components;
using Content.Shared.CCVar;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Orion.Chat.Systems.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DirectionalEmoteTargetComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public void ShowMessage(NetEntity source, NetEntity target, string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > _cfg.GetCVar(CCVars.ChatMaxEmoteLength))
            return;

        RaiseNetworkEvent(new SendDirectionalEmoteEvent(source, target, text));
    }

    private void OnGetVerbs(EntityUid uid, DirectionalEmoteTargetComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Target == args.User ||
            !HasComp<DirectionalEmoteTargetComponent>(args.User))
            return;

        var source = GetNetEntity(args.User);
        var target = GetNetEntity(args.Target);

        args.Verbs.Add(new Verb
        {
            Act = () => _userInterfaceManager.GetUIController<DirectionalEmoteUIController>().ToggleWindow(source, target),
            Priority = 15,
            Text = Loc.GetString("directional-emote-verb-name"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/emotes.svg.192dpi.png")),
            ClientExclusive = true,
        });
    }
}
