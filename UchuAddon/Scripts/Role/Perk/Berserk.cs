using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Media;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

[NebulaRPCHolder]
internal class BerserkU : PerkFunctionalInstance
{
    const float CoolDown = 15f;
    static PerkFunctionalDefinition Def = new("berserkU", PerkFunctionalDefinition.Category.NoncrewmateOnly, new PerkDefinition("berserkU", 3, 3, Virial.Color.ImpostorColor, Virial.Color.ImpostorColor).CooldownText("%CD%", () => CoolDown), (def, instance) => new BerserkU(def, instance));

    public BerserkU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, CoolDown);
        cooldownTimer.Start();
        PerkInstance.BindTimer(cooldownTimer);
    }

    static private RemoteProcess<GamePlayer> RpcBerserkSound = new("RpcBeserkSoundEffect_Uchu", (player, _) =>
    {
        NebulaAsset.PlaySE(VanillaAsset.HnSTransformClip.Clip, player.Position, 0.8f, Berserker.berserkSEStrengthOption, 0.8f);
    });

    private GameTimer cooldownTimer;
    int left = 3;

    public override bool HasAction => true;

    public override void OnClick()
    {
        if (left <= 0) return;
        if (cooldownTimer.IsProgressing) return;
        if (MyPlayer.IsDead) return;

        left--;
        cooldownTimer.Start(CoolDown);

        RpcBerserkSound.Invoke(MyPlayer);
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor((left > 0) ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(CoolDown);
    }
}