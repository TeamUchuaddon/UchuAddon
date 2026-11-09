using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Neutral;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Impostor;
using Nebula.Roles.Perks;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Hadar;
using static Sentry.MeasurementUnit;
using static UnityEngine.ProBuilder.UvUnwrapping;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Perk;

internal class MasterKeyU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front2.png")!.AsImage(100f)!;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.masterKeyU.cooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("masterKeyU", PerkFunctionalDefinition.Category.Standard,
        new PerkDefinition("masterKeyU", PerkDefinition.GetPerkBackIcon(4), IconImage, (Ubiquitous.MyRole as DefinedAssignable).Color.ToUnityColor(), null).CooldownText("%CD%", () => Cooldown),(def, instance) => new MasterKeyU(def, instance),[CooldownOption]);

    public MasterKeyU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
        doorTracker = new CloseDoorTracker(MyPlayer, 1f);
    }

    private GameTimer cooldownTimer;
    private CloseDoorTracker doorTracker = null!;

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (cooldownTimer.IsProgressing) return;
        if (doorTracker?.CurrentDoor == null) return;

        var door = doorTracker.CurrentDoor;
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors,(byte)(door.Id | 64));

        cooldownTimer.Start();
    }
    void OnUpdate(GameHudUpdateEvent ev)
    {
        doorTracker?.Update();
        bool canUse = doorTracker?.CurrentDoor != null &&!cooldownTimer.IsProgressing;

        PerkInstance.SetDisplayColor(canUse ? Color.white : Color.gray);
    }
    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}

internal sealed class CloseDoorTracker
{
    public OpenableDoor? CurrentDoor { get; private set; }

    private readonly GamePlayer player;
    private readonly float range;
    private readonly bool onlyOpenDoor;

    public CloseDoorTracker(GamePlayer player, float range = 2.4f, bool onlyOpenDoor = true)
    {
        this.player = player;
        this.range = range;
        this.onlyOpenDoor = onlyOpenDoor;
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
            if (onlyOpenDoor && door.IsOpen) continue;
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