using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.ParticleSystem.PlaybackState;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class BruteSabotageU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.bruteSabotageU.cooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("bruteSabotageU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
    new PerkDefinition("bruteSabotageU", 4, 40, Virial.Color.ImpostorColor).CooldownText("%CD%", () => Cooldown), (def, instance) => new BruteSabotageU(def, instance), [CooldownOption]);

    public BruteSabotageU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
        doorTracker = new OpenDoorTracker(MyPlayer, 1f);
    }

    private GameTimer cooldownTimer;
    private OpenDoorTracker doorTracker = null!;

    public override bool HasAction => true;
    public override void OnClick()
    {
        var door = doorTracker.CurrentDoor;
        if (door == null) return;

        RpcCloseDoor.Invoke(door.Id);

        cooldownTimer.Start(Cooldown);
    }

    static private RemoteProcess<int> RpcCloseDoor = new("RpcCloseDoorBruteSabotage_Uchu",
    (message, _) =>
    {
        if (ShipStatus.Instance == null) return;

        var door = ShipStatus.Instance.AllDoors.FirstOrDefault(d => d.Id == message);

        if (door == null) return;

        door.SetDoorway(false);
    });

    void OnUpdate(GameHudUpdateEvent ev)
    {
        doorTracker?.Update();
        bool canUse = doorTracker?.CurrentDoor != null && !cooldownTimer.IsProgressing;

        PerkInstance.SetDisplayColor(canUse ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}

internal sealed class OpenDoorTracker
{
    public OpenableDoor? CurrentDoor { get; private set; }

    private readonly GamePlayer player;
    private readonly float range;

    public OpenDoorTracker(GamePlayer player, float range = 2.4f)
    {
        this.player = player;
        this.range = range;
    }

    public void Update()
    {
        CurrentDoor = null;

        if (ShipStatus.Instance == null) return;

        float min = range;
        Vector3 pos = player.VanillaPlayer.transform.position;
        var mapData = MapData.GetCurrentMapData();

        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            if (!door.IsOpen) continue;
            if (!mapData.IsSealableDoor(door)) continue;

            float d = pos.Distance(door.transform.position);
            if (d < min)
            {
                min = d;
                CurrentDoor = door;
            }
        }
    }
}