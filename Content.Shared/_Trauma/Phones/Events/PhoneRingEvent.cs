// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Trauma.Phones.Components;

namespace Content.Shared._Trauma.Phones.Events;

[ByRefEvent]
public record struct PhoneRingEvent(Entity<RotaryPhoneComponent> Phone);
