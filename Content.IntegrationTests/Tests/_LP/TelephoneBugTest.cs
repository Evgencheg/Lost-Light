using Content.IntegrationTests.Fixtures;
using Content.Server._LP.Phones;
using Content.Shared._LP.Phones;
using Content.Shared._Starlight.TapeRecorder.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._LP;

/// <summary>
/// Жучок прячется внутри телефона, записывает реплики на свою кассету и возвращается при разборе.
/// </summary>
public sealed class TelephoneBugTest : GameTest
{
    private const string StandProto = "RotaryPhoneStandNoRot";
    private const string PhoneSlot = "phone";

    [Test]
    public async Task BugRecordsAndCanBeRemoved()
    {
        var map = await Pair.CreateTestMap();

        EntityUid user = default;
        EntityUid stand = default;
        EntityUid handset = default;
        EntityUid bug = default;

        await Server.WaitPost(() =>
        {
            user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            stand = SEntMan.SpawnEntity(StandProto, map.GridCoords);
            handset = SEntMan.System<ItemSlotsSystem>().GetItemOrNull(stand, PhoneSlot)!.Value;
            bug = SEntMan.SpawnEntity("LPTelephoneBug", map.GridCoords);

            var system = SEntMan.System<TelephoneBugSystem>();
            Assert.That(system.TryInstall((bug, SEntMan.GetComponent<TelephoneBugComponent>(bug)), stand, user));

            // второй жучок на тот же телефон не ставится
            var second = SEntMan.SpawnEntity("LPTelephoneBug", map.GridCoords);
            Assert.That(system.TryInstall((second, SEntMan.GetComponent<TelephoneBugComponent>(second)), stand, user), Is.False);

            system.RecordRotary(handset, user, "Секрет");
        });

        await Pair.RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var slots = SEntMan.System<ItemSlotsSystem>();
            var cassette = slots.GetItemOrNull(bug, "cassette_tape")!.Value;
            var tape = SEntMan.GetComponent<TapeCassetteComponent>(cassette);
            Assert.That(tape.RecordedData, Has.Count.EqualTo(1));
            Assert.That(tape.RecordedData[0].Message, Is.EqualTo("Секрет"));
            Assert.That(SEntMan.GetComponent<TapeRecorderComponent>(bug).Mode, Is.EqualTo(TapeRecorderMode.Recording));
        });

        // разговор затих: кассета перестаёт идти
        await Pair.RunSeconds(7);

        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.GetComponent<TapeRecorderComponent>(bug).Mode, Is.EqualTo(TapeRecorderMode.Stopped)));

        // разбор телефона возвращает жучок
        await Server.WaitPost(() =>
        {
            var system = SEntMan.System<TelephoneBugSystem>();
            Assert.That(system.TryRemove(stand, user));
            Assert.That(system.TryRemove(stand, user), Is.False);
            Assert.That(SEntMan.HasComponent<BuggedPhoneComponent>(stand), Is.False);
        });
    }
}
