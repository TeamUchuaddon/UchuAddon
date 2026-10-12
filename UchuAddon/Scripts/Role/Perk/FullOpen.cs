using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sentry.MeasurementUnit;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class FullOpenU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.fullOpenU.cooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("fullOpenU", PerkFunctionalDefinition.Category.Standard, new PerkDefinition(
        "fullOpenU", 4, 17, new(189, 92, 2)).CooldownText("%CD%", () => Cooldown), (def, instance) => new FullOpenU(def, instance), [CooldownOption]);


    public FullOpenU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    private GameTimer cooldownTimer;

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (cooldownTimer.IsProgressing) return;

        RpcToiletOpenUchu.Invoke();

        cooldownTimer.Start();
    }

    static RemoteProcess RpcToiletOpenUchu = new RemoteProcess("RpcToiletOpen_Uchu", (_) => //Thx SNR
    {
        ShipStatus.Instance.UpdateSystem(SystemTypes.Doors, PlayerControl.LocalPlayer, 79);
        ShipStatus.Instance.UpdateSystem(SystemTypes.Doors, PlayerControl.LocalPlayer, 80);
        ShipStatus.Instance.UpdateSystem(SystemTypes.Doors, PlayerControl.LocalPlayer, 81);
        ShipStatus.Instance.UpdateSystem(SystemTypes.Doors, PlayerControl.LocalPlayer, 82);
    });

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(!cooldownTimer.IsProgressing ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}
