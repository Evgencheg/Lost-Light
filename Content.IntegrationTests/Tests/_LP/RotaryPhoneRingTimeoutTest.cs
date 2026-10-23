using Content.IntegrationTests.Fixtures;
using Content.Server._LP.Phones;
using Content.Shared._Trauma.Phones.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._LP;

/// <summary>
/// Исходящий вызов дискового телефона сбрасывается, если никто не ответил за RingTimeout.
/// </summary>
public sealed class RotaryPhoneRingTimeoutTest : GameTest
{
    private const string StandProto = "RotaryPhoneStandNoRot";
    private const string Slot = "phone";

    private EntityUid _caller;
    private EntityUid _callerStand;
    private EntityUid _callee;
    private EntityUid _calleeStand;
    private EntityUid _user;

    private async Task Setup()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            _user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            _callerStand = SEntMan.SpawnEntity(StandProto, map.GridCoords);
            _calleeStand = SEntMan.SpawnEntity(StandProto, map.GridCoords);

            var slots = SEntMan.System<ItemSlotsSystem>();
            _caller = slots.GetItemOrNull(_callerStand, Slot)!.Value;
            _callee = slots.GetItemOrNull(_calleeStand, Slot)!.Value;

            // звонящий снимает свою трубку
            Assert.That(slots.TryGetSlot(_callerStand, Slot, out var slot));
            Assert.That(slots.TryEjectToHands(_callerStand, slot!, _user));
        });
    }

    private void Dial()
    {
        var callerComp = SEntMan.GetComponent<RotaryPhoneComponent>(_caller);
        callerComp.DialedNumber = SEntMan.GetComponent<RotaryPhoneComponent>(_callee).PhoneNumber;
        SEntMan.EventBus.RaiseLocalEvent(_caller, new PhoneDialedMessage());
    }

    [Test]
    public async Task UnansweredCallIsDropped()
    {
        await Setup();

        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_callee).ConnectedPhone, Is.EqualTo(_caller));
            Assert.That(SEntMan.HasComponent<RotaryPhoneCallingComponent>(_caller));
        });

        // ещё звонит
        await Pair.RunSeconds(20);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_callee).ConnectedPhone, Is.EqualTo(_caller)));

        // таймаут вышел: оба телефона свободны
        await Pair.RunSeconds(15);
        await Server.WaitAssertion(() =>
        {
            var caller = SEntMan.GetComponent<RotaryPhoneComponent>(_caller);
            var callee = SEntMan.GetComponent<RotaryPhoneComponent>(_callee);
            Assert.That(caller.ConnectedPhone, Is.Null);
            Assert.That(callee.ConnectedPhone, Is.Null);
            Assert.That(caller.Engaged, Is.True, "трубка всё ещё в руках");
            Assert.That(SEntMan.HasComponent<RotaryPhoneCallingComponent>(_caller), Is.False);
        });

        // повторный набор работает
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_callee).ConnectedPhone, Is.EqualTo(_caller)));
    }

    [Test]
    public async Task AnsweredCallIsNotDropped()
    {
        await Setup();

        await Server.WaitPost(Dial);
        await Server.WaitPost(() =>
        {
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryGetSlot(_calleeStand, Slot, out var slot));
            Assert.That(slots.TryEjectToHands(_calleeStand, slot!, _user));
        });

        await Pair.RunSeconds(40);
        await Server.WaitAssertion(() =>
        {
            var caller = SEntMan.GetComponent<RotaryPhoneComponent>(_caller);
            var callee = SEntMan.GetComponent<RotaryPhoneComponent>(_callee);
            Assert.That(caller.Connected, Is.True);
            Assert.That(callee.Connected, Is.True);
            Assert.That(SEntMan.HasComponent<RotaryPhoneCallingComponent>(_caller), Is.False);
        });
    }

    [Test]
    public async Task DeletedCalleeDoesNotLeaveCallerRinging()
    {
        await Setup();

        await Server.WaitPost(Dial);
        await Server.WaitPost(() => SEntMan.DeleteEntity(_calleeStand));
        await Pair.RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var caller = SEntMan.GetComponent<RotaryPhoneComponent>(_caller);
            Assert.That(caller.ConnectedPhone, Is.Null);
            Assert.That(SEntMan.HasComponent<RotaryPhoneCallingComponent>(_caller), Is.False);
        });
    }
}
