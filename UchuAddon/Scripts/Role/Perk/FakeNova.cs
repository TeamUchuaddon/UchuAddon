using Hori.Scripts.Role.Impostor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.Impostor.Marionette;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class FakeNovaU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front5.png")!.AsImage(100f)!;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.fakeNovaU.cooldown", (10f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("fakeNovaU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
    new PerkDefinition("fakeNovaU", PerkDefinition.GetPerkBackIcon(3), IconImage, Virial.Color.ImpostorColor.ToUnityColor(), null).CooldownText("%CD%", () => Cooldown), (def, instance) => new FakeNovaU(def, instance), [CooldownOption]);

    public FakeNovaU(PerkDefinition def, PerkInstance instance) : base(def, instance)
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

        var obj = new GameObject("FakeNovaSE");

        var controller = obj.AddComponent<VFXController>();

        NebulaManager.Instance.StartCoroutine(controller.SoundEffectBeamFiring().WrapToIl2Cpp());

        cooldownTimer.Start(Cooldown);
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(!cooldownTimer.IsProgressing ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}
