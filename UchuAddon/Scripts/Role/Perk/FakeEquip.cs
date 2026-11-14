using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Injection;
using Nebula.Modules.Cosmetics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Events.Game.Minimap;
using static Hori.Scripts.Role.Crewmate.AdmiralU;
using static Hori.Scripts.Role.Crewmate.SonarU;
using static Nebula.Roles.Impostor.Hadar;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Perk;

[NebulaRPCHolder]
internal class FakeEquipU : PerkFunctionalInstance
{
    public class FakeEquipWeapon : EquipableAbility, IGameOperator
    {
        private static SpriteLoader rifleSprite = SpriteLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        private static SpriteLoader axeSprite = SpriteLoader.FromResource("Nebula.Resources.RaiderAxe.png", 150f);
        static private IDividedSpriteLoader bubblegunSprite = DividedSpriteLoader.FromResource("Nebula.Resources.Bubblegun.png", 115f, 4, 2).SetPivot(new(0.8f, 0.38f));

        protected override float Distance => distance;
        float distance = 1.25f;

        // type･･･ 0:ライフル 1:斧 2:バブル
        public FakeEquipWeapon(GamePlayer owner) : base(owner, false, "FakeWeapon")
        {
            // 出現する可能性がある役職を優先する。
            List<int> priorityTypes = new();

            if (Sniper.MyRole.IsSpawnableInSomeForm()) priorityTypes.Add(0);
            if (Raider.MyRole.IsSpawnableInSomeForm()) priorityTypes.Add(1);
            if (Bubblegun.MyRole.IsSpawnableInSomeForm()) priorityTypes.Add(2);

            int type = priorityTypes.Count > 0 ? priorityTypes[UnityEngine.Random.Range(0, priorityTypes.Count)] : UnityEngine.Random.Range(0, 3);

            switch (type)
            {
                case 0:
                    Renderer.sprite = rifleSprite.GetSprite();
                    break;
                case 1:
                    Renderer.sprite = axeSprite.GetSprite(); 
                    Renderer.transform.localScale = new Vector3(1.51f, 1.51f, 1f);
                    distance = 1f;
                    break;
                case 2:
                    Renderer.sprite = bubblegunSprite.GetSprite(0);
                    Renderer.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
                    distance = 0.7f;
                    break;
            }
        }
    }

    static private float Cooldown => CooldownOption;
    static private float Duration => DurationOption;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front10.png")!.AsImage(85f)!;

    static private FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.fakeEquipU.cooldown", (0f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration DurationOption = NebulaAPI.Configurations.Configuration("perk.fakeEquipU.duration", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("fakeEquipU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
        new PerkDefinition("fakeEquipU", PerkDefinition.GetPerkBackIcon(1), IconImage, Palette.ImpostorRed, null).CooldownText("%CD%", () => Cooldown).DurationText("%D%", () => Duration), (def, instance) => new FakeEquipU(def, instance), [CooldownOption, DurationOption]);

    private GameTimer cooldownTimer;
    int left = 3;
    private ObjectTracker<IPlayerlike> tracker;

    public FakeEquipU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        tracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, p => ObjectTrackers.PlayerlikeStandardPredicate(p));

        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (MyPlayer.IsDead) return;

        if (left <= 0) return;
        if (cooldownTimer.IsProgressing) return;
        if (tracker.CurrentTarget == null) return;

        RpcFakeEquip.Invoke(tracker.CurrentTarget.RealPlayer);
        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);

        left--;
        cooldownTimer.Start(Cooldown);
    }

    static IEnumerator CoFakeEquip(GamePlayer target)
    {
        FakeEquipWeapon MyWeapon = null!;
        MyWeapon = new FakeEquipWeapon(target).RegisterPermanently();

        yield return new WaitForSeconds(Duration);

        MyWeapon.Release(); // 解除
    }

    // 装備
    static RemoteProcess<GamePlayer> RpcFakeEquip = new(
    "FakeEquipWeapon",
    (message, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoFakeEquip(message).WrapToIl2Cpp());
    });

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor((left > 0 && tracker.CurrentTarget != null) ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }   
}