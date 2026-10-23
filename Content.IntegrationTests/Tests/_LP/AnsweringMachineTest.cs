using Content.IntegrationTests.Fixtures;
using Content.Server._LP.Phones;
using Content.Server.Power.EntitySystems;
using Content.Shared._LP.Phones;
using Content.Shared._Starlight.TapeRecorder.Components;
using Content.Shared._Trauma.Phones.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._LP;

/// <summary>
/// Автоответчик принимает вызов вместо сброса по таймауту и записывает сообщение звонящего на кассету.
/// </summary>
public sealed class AnsweringMachineTest : GameTest
{
    private const string StandProto = "RotaryPhoneStandNoRot";
    private const string PhoneSlot = "phone";

    private EntityUid _caller;
    private EntityUid _callee;
    private EntityUid _calleeStand;
    private EntityUid _machine;
    private EntityUid _user;

    private async Task Setup(bool withCassette = true)
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            _user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var callerStand = SEntMan.SpawnEntity(StandProto, map.GridCoords);
            _calleeStand = SEntMan.SpawnEntity(StandProto, map.GridCoords);

            var slots = SEntMan.System<ItemSlotsSystem>();
            _caller = slots.GetItemOrNull(callerStand, PhoneSlot)!.Value;
            _callee = slots.GetItemOrNull(_calleeStand, PhoneSlot)!.Value;

            Assert.That(slots.TryGetSlot(callerStand, PhoneSlot, out var slot));
            Assert.That(slots.TryEjectToHands(callerStand, slot!, _user));

            var exchange = SEntMan.SpawnEntity("LPTelephoneExchange", map.GridCoords);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(exchange, false);

            _machine = SEntMan.SpawnEntity("LPAnsweringMachine", map.GridCoords);

            if (!withCassette)
            {
                Assert.That(slots.TryGetSlot(_machine, "cassette_tape", out var cassetteSlot));
                slots.TryEject(_machine, cassetteSlot!, null, out _);
            }

            Assert.That(slots.TryGetSlot(_calleeStand, AnsweringMachineSystem.SlotId, out var machineSlot));
            Assert.That(slots.TryInsert(_calleeStand, machineSlot!, _machine, null));
        });

        // Powered обновляется в тике сети питания
        await Pair.RunTicksSync(5);
    }

    private void Dial()
    {
        var callerComp = SEntMan.GetComponent<RotaryPhoneComponent>(_caller);
        callerComp.DialedNumber = SEntMan.GetComponent<RotaryPhoneComponent>(_callee).PhoneNumber;
        SEntMan.EventBus.RaiseLocalEvent(_caller, new PhoneDialedMessage());
    }

    [Test]
    public async Task UnansweredCallIsRecorded()
    {
        await Setup();

        await Server.WaitPost(Dial);
        await Pair.RunSeconds(31);

        // вызов принят автоответчиком, а не сброшен
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AnsweringMachineSessionComponent>(_caller));
            Assert.That(SEntMan.GetComponent<TapeRecorderComponent>(_machine).Mode, Is.EqualTo(TapeRecorderMode.Recording));

            var answering = SEntMan.System<AnsweringMachineSystem>();
            Assert.That(answering.TryRecord((_caller, SEntMan.GetComponent<RotaryPhoneComponent>(_caller)), _user, "Тест", "Привет"));
        });

        await Pair.RunSeconds(1);

        // после тишины сообщение закрывается, вызов завершён, индикатор горит
        await Pair.RunSeconds(12);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AnsweringMachineSessionComponent>(_caller), Is.False);
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_caller).ConnectedPhone, Is.Null);
            Assert.That(SEntMan.GetComponent<AnsweringMachineComponent>(_machine).NewMessages, Is.True);

            var slots = SEntMan.System<ItemSlotsSystem>();
            var cassette = slots.GetItemOrNull(_machine, "cassette_tape")!.Value;
            var tape = SEntMan.GetComponent<TapeCassetteComponent>(cassette);
            Assert.That(tape.RecordedData, Has.Count.EqualTo(1));
            Assert.That(tape.RecordedData[0].Message, Is.EqualTo("Привет"));
        });
    }

    [Test]
    public async Task NoCassetteFallsBackToNormalTimeout()
    {
        await Setup(withCassette: false);

        await Server.WaitPost(Dial);
        await Pair.RunSeconds(31);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AnsweringMachineSessionComponent>(_caller), Is.False);
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_caller).ConnectedPhone, Is.Null);
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_callee).ConnectedPhone, Is.Null);
        });
    }
}
