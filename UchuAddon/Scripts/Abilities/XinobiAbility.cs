using Hori.Core;
using Rewired.Utils.Platforms.Windows;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

public class XinobiAbility : FlexibleLifespan, IGameOperator, IBindPlayer
{
    static private readonly Virial.Media.Image PossessImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image ReleaseImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image VentImage = NebulaAPI.AddonAsset.GetResource("XinobiVentButton.png")!.AsImage(115f)!;

    private GamePlayer myPlayer;
    GamePlayer IBindPlayer.MyPlayer => myPlayer;
    bool Possess = false;
    float min = 0f;
    Vent? ventLocal = null;
    Vector3 previousPosition = Vector3.zero;
    public XinobiAbility(GamePlayer player)
    {
        this.myPlayer = player;

        if (player.AmOwner)
        {
            var abilityVent = new VentArrowAbility().Register(this);
            var ability = new AllVentConnectAbility().Register(this);
            ability.AllVentConnects(true);

            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, player);
            playerTracker.SetColor(Virial.Color.Red);
            ModAbilityButton killButton = null!;
            ModAbilityButton ventNinjaButton = null!;
            ModAbilityButton ventPossessButton = null!;
            ModAbilityButton possessButton = null!;
            ModAbilityButton breakButton = null!;

            killButton = NebulaAPI.Modules.KillButton(this, player, true, Virial.Compat.VirtualKeyInput.Kill,
            Role.Impostor.XinobiU.KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!, (target, _) =>
            {
                if (Possess)
                {
                    player.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.RemoteKill);
                }
                else if (!Possess)
                {
                    player.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                }
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                killButton.StartCoolDown();
            },
            null,
            _ => playerTracker.CurrentTarget != null && !player.IsDived,
            _ => player.AllowToShowKillButtonByAbilities
            );
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            killButton.Visibility = (button) => !player.IsDead;
            killButton.Availability = (button) => playerTracker.CurrentTarget != null && ((!player.IsDead && player.CanMove) || Possess);

            //通常ベン遁ボタン
            ventNinjaButton = NebulaAPI.Modules.AbilityButton(this, player, Virial.Compat.VirtualKeyInput.SecondaryAbility, Role.Impostor.XinobiU.XinobiVentCooldown, "xinobiU.vent", VentImage);
            ventNinjaButton.Visibility = (button) => !player.IsDead;
            ventNinjaButton.Availability = (button) => (!player.IsDead && player.CanMove) || Possess;
            var ventNinjaTimer = ventNinjaButton.CoolDownTimer as TimerImpl;
            var ventNinjaLastPredicate = ventNinjaTimer!.Predicate;      
            ventNinjaTimer.SetPredicate(() => ventNinjaLastPredicate!.Invoke() || HudManager.Instance.PlayerCam.Target != null);
            ventNinjaButton.OnClick = (button) =>
            {
                NebulaGameManager.Instance?.RpcDoGameAction(myPlayer, myPlayer.Position, UchuGameAction.XinobiVentAction);
                Possess = false;
                AmongUsUtil.ToggleCamTarget(null);
                XinobiVent();
                ventNinjaButton.StartCoolDown();
                ventPossessButton.StartCoolDown();
            };


            Virial.Components.ObjectTracker<Console> consoleTracker = new ObjectTrackerUnityImpl<Console, Console>(player.VanillaPlayer, AmongUsLLImpl.Instance.VanillaKillDistance/*AmongUsUtil.VanillaKillDistance*/,
                () => ShipStatus.Instance.AllConsoles, c => true, c => true, c => c,
                c => [c.transform.position], c => c.Image, Color.red, true, false).Register(this);

            possessButton = NebulaAPI.Modules.AbilityButton(this, player, Virial.Compat.VirtualKeyInput.Ability, 0.5f, "xinobiU.possess", PossessImage, _ => consoleTracker.CurrentTarget != null);
            possessButton.Visibility = (button) => !player.IsDead && !Possess;
            possessButton.OnClick = (button) =>
            {
                RpcHideName.Invoke(player);

                possessButton.StartCoolDown();
                breakButton.StartCoolDown();
                Possess = true;
                var target = consoleTracker.CurrentTarget;
                if (target != null)
                {
                    AmongUsUtil.ToggleCamTarget(target);
                    Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
                    ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(), null, myPos, 1f).StartOnScene();
                }
            };

            breakButton = NebulaAPI.Modules.AbilityButton(this, player, Virial.Compat.VirtualKeyInput.Ability, 0.5f, "xinobiU.release", ReleaseImage);
            breakButton.Visibility = (button) => !player.IsDead && Possess;
            breakButton.Availability = (button) => Possess;
            var breakPosTimer = breakButton.CoolDownTimer as TimerImpl;
            var breakPosPredicate = breakPosTimer!.Predicate;
            breakPosTimer.SetPredicate(() => breakPosPredicate!.Invoke() || HudManager.Instance.PlayerCam.Target != null);
            breakButton.OnClick = (button) =>
            {
                RpcPublicName.Invoke(player);

                possessButton.StartCoolDown();
                breakButton.StartCoolDown();
                AmongUsUtil.ToggleCamTarget(null);
                Possess = false;
                var target = consoleTracker.CurrentTarget;
                if (target != null)
                {
                    Vector3 effectPos = target.transform.position;
                    effectPos.z -= 0.0000001f;
                    ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(), null, effectPos, 1.1f).StartOnScene();
                }
            };
        }
    }

    public void XinobiVent()
    {
        Possess = false;
        myPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.39f, false, 1);
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
    }

    void Update(GameUpdateEvent ev)
    {
        if (!Possess) return;
        myPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.1f, false, 1);
        myPlayer.GainSpeedAttribute(0f, 0.1f, false, 1);
    }

    static void NameSeclet(GamePlayer target)
    {
        var name = target.VanillaPlayer.transform.Find("Names");
        name.Find("NameText_TMP").gameObject.SetActive(false);
        name.Find("ColorblindName_TMP").gameObject.SetActive(false);
    }

    static void NamePublic(GamePlayer target)
    {
        var name = target.VanillaPlayer.transform.Find("Names");
        name.Find("NameText_TMP").gameObject.SetActive(true);
        name.Find("ColorblindName_TMP").gameObject.SetActive(true);
    }

    public static readonly RemoteProcess<GamePlayer> RpcHideName = new("RpcHidePlayerName_Uchu", (player, _) =>
    {
        NameSeclet(player);
    });
    public static readonly RemoteProcess<GamePlayer> RpcPublicName = new("RpcPublicPlayerName_Uchu", (player, _) =>
    {
        NamePublic(player);
    });

    [OnlyMyPlayer]
    void CameraDead(PlayerDieEvent ev)
    {
        AmongUsUtil.ToggleCamTarget(null);
        RpcPublicName.Invoke(myPlayer);
        Possess = false;
    }

    void CameraMeeting(MeetingPreStartEvent ev)
    {
        AmongUsUtil.ToggleCamTarget(null);
        RpcPublicName.Invoke(myPlayer);
        Possess = false;
    }

    void a(PlayerTryToChangeRoleEvent ev)
    {
        AmongUsUtil.ToggleCamTarget(null);
        Possess = false;
        RpcPublicName.Invoke(myPlayer);
    }
}