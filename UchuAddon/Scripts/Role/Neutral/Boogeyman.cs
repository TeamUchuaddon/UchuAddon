using Hori.Core;
using Hori.Scripts.Role.Ghost.Complex;
using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

// 自身の死体レポート以外の会議開始では復活する
// 39, 89, 43

public class BoogeymanU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    public static Team MyTeam = new Team("teams.boogeymanU", new Virial.Color(80, 97, 82), TeamRevealType.OnlyMe);
    private BoogeymanU() : base("boogeymanU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [NumOfMischiefToWin, NumOfMischiefPerTaskPhase, VentConfiguration, 
    new GroupConfiguration("options.role.boogeymanU.group.mischief", [MischiefCooldown, MischiefDuration, MischiefAttributesDuration], GroupConfigurationColor.Gray),
    new GroupConfiguration("options.role.boogeymanU.group.pseudocide", [PseudocideCooldownOption, HidePseudocideFromVitalsOption], GroupConfigurationColor.Gray),])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Boogeyman.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.neutralVentU.vent", true);

    static private IntegerConfiguration NumOfMischiefToWin = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.numOfMischiefToWin", (1, 12), 5);
    static private IntegerConfiguration NumOfMischiefPerTaskPhase = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.numOfMischiefPerTaskPhase", (1, 7), 2);
    static private FloatConfiguration MischiefCooldown = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.mischiefCooldown", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration MischiefDuration = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.mischiefDuration", (1f, 10f, 0.5f), 1.5f, FloatConfigurationDecorator.Second);
    static public FloatConfiguration MischiefAttributesDuration = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.mischiefAttributesDuration", (5f, 20f, 2.5f), 10f, FloatConfigurationDecorator.Second);

    static private readonly FloatConfiguration PseudocideCooldownOption = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.pseudocideCooldown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly BoolConfiguration HidePseudocideFromVitalsOption = NebulaAPI.Configurations.Configuration("options.role.boogeymanU.hidePseudocideFromVitals", false);

    static public BoogeymanU MyRole = new BoogeymanU();


    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Boogeyman.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private Image pseudocideButtonSprite = NebulaAPI.AddonAsset.GetResource("BoogeymanPseudocideButton.png")!.AsImage(115f)!;
    static private Image reviveButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BuskReviveButton.png", 115f);

    static private Image buttonSprite = NebulaAPI.AddonAsset.GetResource("BoogeymanButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasAbility => true;
    bool IAssignableDocument.HasWinCondition => true;
    bool IAssignableDocument.HasTips => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(pseudocideButtonSprite, "role.boogeymanU.ability.pseudocide");
        yield return new(buttonSprite, "role.boogeymanU.ability.mischief");
    }

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        void IGameOperator.OnReleased()
        {
            if (AmOwner) MyPlayer.RemoveAttribute(PlayerAttributes.BuskerEffect);
        }

        int left = NumOfMischiefToWin;
        int leftPerTaskPhase = NumOfMischiefPerTaskPhase;

        bool IsPseudocide = false;
        ModAbilityButton mischiefButton = null!;

        public override void OnActivated()
        {
            if (AmOwner)
            {
                Nebula.Utilities.Helpers.TextHudContent("BoogeymanText", this, (tmPro) => tmPro.text = left > 0 ? NebulaAPI.Language.Translate("role.boogeymanU.hudText") + ": " + left : NebulaAPI.Language.Translate("role.boogeymanU.hudText.complete"), true);

                var pseudocideButton = NebulaAPI.Modules.AbilityButton(this, isArrangedAsKillButton: true)
                  .BindKey(Virial.Compat.VirtualKeyInput.Kill)
                  .SetImage(pseudocideButtonSprite)
                  .SetLabel("pseudocide");

                var reviveButton = NebulaAPI.Modules.AbilityButton(this, isArrangedAsKillButton: true)
                  .BindKey(Virial.Compat.VirtualKeyInput.Kill)
                  .SetImage(reviveButtonSprite)
                  .SetLabel("revive");

                pseudocideButton.Availability = (button) => MyPlayer.CanMove;
                pseudocideButton.Visibility = (button) => !MyPlayer.IsDead;
                pseudocideButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, PseudocideCooldownOption).SetAsAbilityTimer().Start();

                reviveButton.Availability = button => MyPlayer.CanMove && MapData.GetCurrentMapData().CheckMapArea(AmongUsLLImpl.LocalPlayer.GetTruePosition());
                reviveButton.Visibility = button => Helpers.AllDeadBodies().Any(deadBody => deadBody.ParentId == MyPlayer.PlayerId) && IsPseudocide;
                reviveButton.PlayFlashWhile = button => true;

                pseudocideButton.OnClick = (button) =>
                {
                    IsPseudocide = true;
                    RpcShareBool.Invoke((MyPlayer, IsPseudocide));

                    // MyPlayer.GainAttribute(BoogeymanAttributes.BoogeymanMischief, 5f, false, 1);
                    if (mischiefButton.CoolDownTimer!.CurrentTime < 5f) mischiefButton.CoolDownTimer?.Start(5f);

                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        using (RPCRouter.CreateSection("PseudocideBoogeymanU_Uchu"))
                        {
                            if (HidePseudocideFromVitalsOption) MyPlayer.GainAttribute(PlayerAttributes.BuskerEffect, float.MaxValue, false, 0);
                            MyPlayer.MurderPlayer(MyPlayer, PlayerState.Pseudocide, null, KillParameter.WithDeadBody, KillCondition.NormalKill);
                        }
                    });
                };

                reviveButton.OnClick = (button) =>
                {
                    ReviveBoogeyman();
                };

                var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p));

                mischiefButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, MischiefCooldown, MischiefDuration, "mischief", 
                    buttonSprite, _ => playerTracker.CurrentTarget != null && !ModSingleton<BoogeymanManager>.Instance.IsMischief(playerTracker.CurrentTarget.RealPlayer) && 
                    left > 0 && leftPerTaskPhase > 0 && IsPseudocide);

                mischiefButton.Visibility = (button) => MyPlayer.IsDead && IsPseudocide;
                mischiefButton.OnEffectStart = _ => playerTracker.KeepAsLongAsPossible = true;
                mischiefButton.OnEffectEnd = (button) =>
                {
                    playerTracker.KeepAsLongAsPossible = false;

                    if (playerTracker.CurrentTarget == null) return;
                    if (MeetingHud.Instance) return;

                    if (!button.EffectTimer!.IsProgressing)
                    {
                        left--;
                        leftPerTaskPhase--;
                        mischiefButton.UpdateUsesIcon(leftPerTaskPhase.ToString());
                        button.StartCoolDown();

                        ModSingleton<BoogeymanManager>.Instance.OnMischief(playerTracker.CurrentTarget.RealPlayer);
                        ModSingleton<BoogeymanManager>.Instance.RegisterAsMischief(playerTracker.CurrentTarget.RealPlayer);
                    }
                };

                mischiefButton.OnUpdate = (button) =>
                {
                    if (!button.IsInEffect) return;
                    if (playerTracker.CurrentTarget == null) button.InterruptEffect();
                };
                mischiefButton.ShowUsesIcon(3, leftPerTaskPhase.ToString());

                GameOperatorManager.Instance?.Subscribe<PlayerReviveEvent>(ev =>
                {
                    if (ev.Player.AmOwner)
                    {
                        IsPseudocide = false;
                        RpcShareBool.Invoke((MyPlayer, IsPseudocide));

                        pseudocideButton.StartCoolDown();
                    }
                }, this);
            }
        }

        void ReviveBoogeyman()
        {
            using (RPCRouter.CreateSection("ReviveBoogeymanU_Uchu"))
            {
                MyPlayer.RemoveAttribute(PlayerAttributes.BuskerEffect);
                MyPlayer.Revive(null, MyPlayer.Position, true, false);
                MyPlayer.VanillaPlayer.ModDive(false);
            }
        }

        // 自身の死体がレポートされる以外なら復活する
        void OnMeetingAccepted(MeetingRequestAcceptedHostEvent ev)
        {
            bool onRevive = IsPseudocide;

            if (ev.Reported != null && ev.Reported == MyPlayer) onRevive = false;

            if (onRevive)
            {
                MyPlayer.RemoveAttribute(PlayerAttributes.BuskerEffect);
                MyPlayer.Revive(null, MyPlayer.Position, true, false);
            }     
        }

        [Local]
        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            IsPseudocide = false;
            RpcShareBool.Invoke((MyPlayer, IsPseudocide));
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            leftPerTaskPhase = NumOfMischiefPerTaskPhase;
            mischiefButton.UpdateUsesIcon(leftPerTaskPhase.ToString());

            if (MyPlayer.IsDead) return;
            if (left <= 0) NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.BoogeymanWin, 1 << MyPlayer.PlayerId);
        }

        static private RemoteProcess<(GamePlayer player, bool isPseudocide)> RpcShareBool = new("RpcShareBool_Uchu", (message, _) =>
        {
            if (message.player.Role is BoogeymanU.Instance boogeyman) boogeyman.IsPseudocide = message.isPseudocide;
        }, false);
    }
}

public static class BoogeymanAttributes
{
    /// <summary>
    /// Boogeymanのいたずらを表します。
    /// </summary>
    static public IPlayerAttribute BoogeymanMischief { get; internal set; }

    static BoogeymanAttributes()
    {
        BoogeymanMischief = new PlayerAttributeImpl(5, "$boogeymanMischief", "boogeymanMischief");
    }
}

[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
file class BoogeymanManager : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static BoogeymanManager() => DIManager.Instance.RegisterModule(() => new BoogeymanManager().RegisterPermanently());

    private BoogeymanManager()
    {
        ModSingleton<BoogeymanManager>.Instance = this;
    }

    private HashSet<byte> mischiefs = []; // いたずらされてるプレイヤー

    public void RegisterAsMischief(GamePlayer player) => RpcRegisterStep.Invoke(player);
    private void ReleasedAsMischief(GamePlayer player) => mischiefs.Remove(player.PlayerId);

    static private RemoteProcess<GamePlayer> RpcRegisterStep = new("RpcRegisterStep_Uchu", (player, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoRegisterSteps(player).WrapToIl2Cpp());
    });

    public static IEnumerator CoRegisterSteps(GamePlayer player)
    {
        ModSingleton<BoogeymanManager>.Instance.mischiefs.Add(player.PlayerId);

        yield return new WaitForSeconds(BoogeymanU.MischiefAttributesDuration);

        ModSingleton<BoogeymanManager>.Instance.ReleasedAsMischief(player);
    }

    /// <summary>
    /// プレイヤーがいたずらされてるかどうか調べます。
    /// </summary>
    public bool IsMischief(GamePlayer player)
    {
        return mischiefs.Contains(player.PlayerId);
    }

    private readonly Action<GamePlayer>[] MischiefOptions =
    [
        SizeMischief,
        FlipXMischief,
        RougheningMischief,
        GhostMischief,
        SpawnMischief
    ];

    public void OnMischief(GamePlayer player)
    {
        var mischief = MischiefOptions[UnityEngine.Random.Range(0, MischiefOptions.Length)];

        mischief(player);
    }

    private static readonly UnityEngine.Vector2[] SizeOptions = [new(1f, 4f), new(4f, 1f), new(0.3f, 0.3f), new(2.5f, 2.5f)];

    static void SizeMischief(GamePlayer player)
    {
        player.GainAttribute(BoogeymanAttributes.BoogeymanMischief, BoogeymanU.MischiefAttributesDuration, false, 1);

        var selectSize = SizeOptions[UnityEngine.Random.Range(0, SizeOptions.Length)];
        player.GainSizeAttribute(selectSize, BoogeymanU.MischiefAttributesDuration, true, 0, "boogeymanU:size");
    }

    private static readonly IPlayerAttribute[] FlipOptions = [PlayerAttributes.FlipX, PlayerAttributes.FlipY, PlayerAttributes.FlipXY];

    static void FlipXMischief(GamePlayer player)
    {
        player.GainAttribute(BoogeymanAttributes.BoogeymanMischief, BoogeymanU.MischiefAttributesDuration, false, 1);

        var flip = FlipOptions[UnityEngine.Random.Range(0, FlipOptions.Length)];

        player.GainAttribute(flip, BoogeymanU.MischiefAttributesDuration, false, 1, "boogeymanU:flip");
    }

    static void RougheningMischief(GamePlayer player)
    {
        player.GainAttribute(BoogeymanAttributes.BoogeymanMischief, BoogeymanU.MischiefAttributesDuration, false, 1);

        player.GainAttribute(PlayerAttributes.Roughening, BoogeymanU.MischiefAttributesDuration, 8f, false, 1, "boogeymanU:eyesight");
    }

    static void GhostMischief(GamePlayer player)
    {
        player.GainAttribute(BoogeymanAttributes.BoogeymanMischief, BoogeymanU.MischiefAttributesDuration, false, 1);

        RpcGhostAnim.Invoke(player.VanillaPlayer);
    }

    static private RemoteProcess<PlayerControl> RpcGhostAnim = new("RpcCollapseAnimBoogeyman_Uchu", (player, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoPlayGhostAnim(player).WrapToIl2Cpp());
    });


    public static IEnumerator CoPlayGhostAnim(PlayerControl player, float animOffset = 1.82f)
    {
        player.moveable = false;
        var physics = player.MyPhysics;
        var anim = physics.Animations;
        anim.Animator.Play(anim.group.GhostIdleAnim, 0f);
        player.cosmetics.SetBodyCosmeticsVisible(false);
        player.cosmetics.SetHatVisorVisible(false);

        yield return new WaitForSeconds(BoogeymanU.MischiefAttributesDuration);

        player.cosmetics.SetBodyCosmeticsVisible(true);
        player.cosmetics.SetHatVisorVisible(true);
        player.MyPhysics.Animations.PlayIdleAnimation();
        player.cosmetics.AnimateSkinIdle();
        player.moveable = true;
        player.ModDive(false);
    }

    static void SpawnMischief(GamePlayer player)
    {
        player.GainAttribute(BoogeymanAttributes.BoogeymanMischief, BoogeymanU.MischiefAttributesDuration, false, 1);

        RpcSpawnAnim.Invoke(player.VanillaPlayer);
    }

    static private RemoteProcess<PlayerControl> RpcSpawnAnim = new("RpcSpawnAnimBoogeyman_Uchu", (player, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoPlaySpawnAnim(player).WrapToIl2Cpp());
    });


    public static IEnumerator CoPlaySpawnAnim(PlayerControl player, float animOffset = 1.82f)
    {
        var originalRotation = player.transform.rotation;

        player.moveable = false;
        var physics = player.MyPhysics;
        var anim = physics.Animations;
        anim.Animator.Play(anim.group.SpawnAnim, 0f);
        anim.Animator.SetTime(animOffset);
        player.cosmetics.SetBodyCosmeticsVisible(false);
        player.cosmetics.SetHatVisorVisible(false);

        float[] angles = { 40f, 200f, 310f };
        float angle = angles[UnityEngine.Random.Range(0, angles.Length)];

        player.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        yield return new WaitForSeconds(BoogeymanU.MischiefAttributesDuration);

        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        player.cosmetics.SetBodyCosmeticsVisible(true);
        player.cosmetics.SetHatVisorVisible(true);
        player.MyPhysics.Animations.PlayIdleAnimation();
        player.cosmetics.AnimateSkinIdle();
        player.moveable = true;
        player.ModDive(false);
    }
}

