using Content.IntegrationTests.Fixtures;
using Content.Server._LP.Phones;
using Content.Server.Power.EntitySystems;
using Content.Shared._LP.Phones;
using Content.Shared._Trauma.Phones.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._LP;

/// <summary>
/// Дисковые телефоны звонят только через рабочую АТС: нужны питание и целые провода.
/// </summary>
public sealed class TelephoneExchangeTest : GameTest
{
    private const string StandProto = "RotaryPhoneStandNoRot";
    private const string HiddenStandProto = "RotaryPhoneStandHidden";
    private const string Slot = "phone";

    private EntityUid _caller;
    private EntityUid _callee;
    private EntityUid _user;
    private EntityUid _exchange;

    private async Task Setup(string standProto = StandProto)
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            _user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var callerStand = SEntMan.SpawnEntity(standProto, map.GridCoords);
            var calleeStand = SEntMan.SpawnEntity(standProto, map.GridCoords);

            var slots = SEntMan.System<ItemSlotsSystem>();
            _caller = slots.GetItemOrNull(callerStand, Slot)!.Value;
            _callee = slots.GetItemOrNull(calleeStand, Slot)!.Value;

            Assert.That(slots.TryGetSlot(callerStand, Slot, out var slot));
            Assert.That(slots.TryEjectToHands(callerStand, slot!, _user));

            _exchange = SEntMan.SpawnEntity("LPTelephoneExchange", map.GridCoords);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(_exchange, false);
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

    private bool CalleeRinging() => SEntMan.GetComponent<RotaryPhoneComponent>(_callee).ConnectedPhone == _caller;

    [Test]
    public async Task CallsWorkOnlyThroughWorkingExchange()
    {
        await Setup();
        var exchange = SEntMan.System<TelephoneExchangeSystem>();

        // исправная АТС: звонок идёт
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging()));

        // саботаж обрывает идущий звонок
        await Server.WaitPost(() =>
            exchange.SetSabotaged((_exchange, SEntMan.GetComponent<TelephoneExchangeComponent>(_exchange)), true));
        await Server.WaitAssertion(() =>
        {
            Assert.That(CalleeRinging(), Is.False);
            Assert.That(SEntMan.GetComponent<RotaryPhoneComponent>(_caller).ConnectedPhone, Is.Null);
        });

        // и не пускает новый
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging(), Is.False));

        // починка возвращает связь
        await Server.WaitPost(() =>
            exchange.SetSabotaged((_exchange, SEntMan.GetComponent<TelephoneExchangeComponent>(_exchange)), false));
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging()));
    }

    [Test]
    public async Task NoPowerBlocksCalls()
    {
        await Setup();

        await Server.WaitPost(() =>
        {
            var power = SEntMan.System<PowerReceiverSystem>();
            power.SetNeedsPower(_exchange, true);
            power.SetPowerDisabled(_exchange, true);
        });
        await Pair.RunTicksSync(5);

        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging(), Is.False));
    }

    [Test]
    public async Task NoExchangeBlocksCalls()
    {
        await Setup();

        await Server.WaitPost(() => SEntMan.DeleteEntity(_exchange));
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging(), Is.False));
    }

    [Test]
    public async Task HiddenPhonesIgnoreExchange()
    {
        await Setup(HiddenStandProto);

        await Server.WaitPost(() => SEntMan.DeleteEntity(_exchange));
        await Server.WaitPost(Dial);
        await Server.WaitAssertion(() => Assert.That(CalleeRinging()));
    }
}
