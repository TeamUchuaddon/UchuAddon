using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
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

internal class RepairU : PerkFunctionalInstance
{
    const float Cooldown = 5f;

    static PerkFunctionalDefinition Def = new("repairU", PerkFunctionalDefinition.Category.Standard, new PerkDefinition("repairU", 3, 52, new Virial.Color(209, 209, 209)).CooldownText("%CD%", () => Cooldown), (def, instance) => new RepairU(def, instance));

    bool used = false;
    public RepairU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }
    private GameTimer cooldownTimer;

    public override bool HasAction => true;

    public override void OnClick()
    {
        if (used) return;
        if (cooldownTimer.IsProgressing) return;
        if (!AmongUsUtil.InAnySab) return;

        used = true;
        SabotageFixAbility.SabotageFix();
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(used ? Color.gray : Color.white);
    }
}