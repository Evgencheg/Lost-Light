// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Pulling.Events;

namespace Content.Shared._Trauma.PullBlocker;

public sealed partial class BlockPullSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BlockPullComponent, BeingPulledAttemptEvent>(OnAttemptPull);
    }

    private void OnAttemptPull(Entity<BlockPullComponent> ent, ref BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }
}
