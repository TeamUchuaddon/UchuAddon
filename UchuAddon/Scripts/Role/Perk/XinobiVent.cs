using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class XinobiVentU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front4.png")!.AsImage(100f)!;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.xinobiVentU.cooldown", (5f, 30f, 5f), 15f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("xinobiVentU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
    new PerkDefinition("xinobiVentU", PerkDefinition.GetPerkBackIcon(1), IconImage, Virial.Color.ImpostorColor.ToUnityColor(), null).CooldownText("%CD%", () => Cooldown), (def, instance) => new XinobiVentU(def, instance), [CooldownOption]);

    public XinobiVentU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    private GameTimer cooldownTimer;

    public override bool HasAction => true;

    float min = 0f;
    Vent? ventLocal = null;
    Vector3 previousPosition = Vector3.zero;
    bool canUse => !cooldownTimer.IsProgressing && !MyPlayer.IsDead && MyPlayer.CanMove && MyPlayer.IsImpostor;

    public override void OnClick()
    {
        if (!canUse) return;

        MyPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.39f, false, 1);
        foreach (var v in ShipStatus.Instance.AllVents)
        {
            float d = PlayerControl.LocalPlayer.transform.position
                .Distance(v.gameObject.transform.position);
            if (ventLocal == null || d < min)
            {
                min = d;
                ventLocal = v;
            }
        }
        if (ventLocal != null)
        {
            var player = PlayerControl.LocalPlayer;
            var physics = player.MyPhysics;

            Vector2 ventPos = ventLocal.transform.position;
            Vector2 warpPos = ventPos + new Vector2(0f, 0.35f);

            physics.body.velocity = Vector2.zero;

            player.NetTransform.RpcSnapTo(warpPos);

            physics.RpcEnterVent(ventLocal.Id);

            ventLocal.SetButtons(true);
        }
        Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
        myPos.z -= 0.0000001f;
        ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(), null, myPos, 1f).StartOnScene();
        ventLocal = null;
        min = 0f;

        cooldownTimer.Start(Cooldown);
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(canUse ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}
