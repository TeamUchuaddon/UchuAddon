using Cpp2IL.Core.Extensions;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppInterop.Runtime.Injection;
using System.IO;
using UnityEngine.UIElements;
using Virial.Compat;
using Virial.Events.Game.Minimap;
using Virial.Helpers;
using Hori.Core;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;
using Nebula.Modules.GUIWidget;
using Nebula.Map;
using Virial.Events.VoiceChat;

namespace Hori.Scripts.Role.Impostor;

// ペナルティが作動しない
public class GluttonyU : DefinedSingleAbilityRoleTemplate<GluttonyU.Ability>, DefinedRole, IAssignableDocument
{
    public GluttonyU() : base("gluttonyU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SwallowCooldownOption, MeetingStartAction,
        new GroupConfiguration("options.role.gluttony.group.swallowPenalty", [SwallowPenalty, PenaltyActiveTime], GroupConfigurationColor.ImpostorRed)])
    {
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers; //ジャッカル・タイラント化
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private IRelativeCooldownConfiguration SwallowCooldownOption = NebulaAPI.Configurations.KillConfiguration("options.role.gluttonyU.killCooldown", CoolDownType.Immediate, (0f, 60f, 2.5f), 35f, (-30f, 30f, 2.5f), 10f, (0.5f, 5f, 0.125f), 1.5f);
    static private ValueConfiguration<int> MeetingStartAction = NebulaAPI.Configurations.Configuration("options.role.gluttonyU.meetingStartAction", ["options.role.gluttonyU.meetingStartAction.dead", "options.role.gluttonyU.meetingStartAction.release"], 0);

    static private BoolConfiguration SwallowPenalty = NebulaAPI.Configurations.Configuration("options.role.gluttonyU.swallowPenalty", true);
    static private FloatConfiguration PenaltyActiveTime = NebulaAPI.Configurations.Configuration("options.role.gluttonyU.penaltyActiveTime", (5f, 90f, 5f), 20f, FloatConfigurationDecorator.Second, ()=> SwallowPenalty);
    
    static public GluttonyU MyRole = new();

    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;

    static private Image killImage = ModAbilityButtonImpl.VanillaKillImage;
    static private Image buttonSprite = NebulaAPI.AddonAsset.GetResource("GluttonyButton.png")!.AsImage(115f)!;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Gluttony.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;
    bool IAssignableDocument.HasTips => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(killImage, "role.gluttonyU.ability.kill");
        yield return new(buttonSprite, "role.gluttonyU.ability.swallow");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%ACTION%", Language.Translate(MeetingStartAction.GetValue() == 1 ? "role.gluttonyU.meetingStart.release" : "role.gluttonyU.meetingStart.dead"));
        yield return new("%PENALTY%", SwallowPenalty ? Language.Translate("role.gluttonyU.swallowPenalty.on") : "");
        yield return new("%SEC%", PenaltyActiveTime.GetValue().ToString());
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        bool OnSwallow => Swallowed != null;
        internal GamePlayer? Swallowed;
        internal PoolablePlayer? swallowIcon = null;

        ModAbilityButton killButton = null!, swallowButton = null!;
        bool IPlayerAbility.HideKillButton => OnSwallow;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(VColor.ImpostorColor);

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, Virial.Compat.VirtualKeyInput.Kill, "gluttonyU.kill",
                    0f, "kill", null, null, _ => OnSwallow).SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor).SetAsUsurpableButton(this);
                killButton.OnClick = (button) =>
                {
                    RpcSwallowKillSetPos.Invoke((MyPlayer, Swallowed!));
                    
                    IEnumerator CoDeley() // 待たずにキルすると位置がおかしくなる
                    {
                        yield return Effects.Wait(0.1f);

                        MyPlayer.MurderPlayer(Swallowed!, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);

                        OnSwallowReset(MyPlayer, Swallowed!);
                    }

                    NebulaManager.Instance.StartCoroutine(CoDeley().WrapToIl2Cpp());
                };
                
                swallowButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SwallowCooldownOption.GetCooldown(MyPlayer.TeamKillCooldown), "swallow", buttonSprite, _ => playerTracker.CurrentTarget != null && !OnSwallow).SetAsUsurpableButton(this);
                swallowButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        MyPlayer.VanillaPlayer.NetTransform.RpcSnapTo(target.Position);

                        Swallowed = target;
                        RpcShareSwallowPlayer.Invoke((MyPlayer, target, true));

                        swallowStartTime = NebulaGameManager.Instance!.CurrentTime;
                        penaltyExecuted = false;

                        swallowIcon = (killButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(target);
                        NebulaAsset.PlaySE(NebulaAudioClip.HadarGush);
                        MyPlayer.GainAttribute(PlayerAttributes.Roughening, 0.21f, 8f, false, 0, "gluttonyU::roughening");
                        target.GainAttribute(PlayerAttributes.InternalInvisible, float.MaxValue, 1f, true, 0, "gluttonyU::invisible");
                        NameVisibleModule.RpcHideName.Invoke(target);

                        RpcSwallowOnTargetOwner.Invoke((MyPlayer, target));
                        //target.VanillaPlayer.NetTransform.RpcSnapTo(new Vector2(-300f, -300f)); // 仕様変更
                    }
                };
                swallowButton.SetLabelType(ModAbilityButton.LabelType.Impostor);

                GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner) swallowButton.StartCoolDown();
                }, this);
            }
        }

        void IGameOperator.OnReleased()
        {
            if (Swallowed != null)
            {
                RpcReleaseEvent.Invoke((Swallowed, MyPlayer));
                OnSwallowReset(MyPlayer, Swallowed);
            }
        }

        float lastShareTime = -10f;

        float swallowStartTime = -1f;
        bool penaltyExecuted = false;

        [Local]
        void OnGameUpdateOnSwallow(GameUpdateEvent ev)
        {
            if (Swallowed == null) return;

            if (SwallowPenalty && swallowStartTime >= 0f && !penaltyExecuted && NebulaGameManager.Instance!.CurrentTime >= swallowStartTime + PenaltyActiveTime)
            {
                penaltyExecuted = true;

                MyPlayer.GainAttribute(PlayerAttributes.CurseOfBloody, float.MaxValue, false, 1, "gluttonyU::bloody");
            }

            if (NebulaGameManager.Instance!.CurrentTime > lastShareTime + 0.2f)
            {
                Swallowed.VanillaPlayer.NetTransform.RpcSnapTo(MyPlayer.Position);
                NameVisibleModule.RpcHideName.Invoke(Swallowed);

                lastShareTime = NebulaGameManager.Instance!.CurrentTime;
            }
        }

        void OnMeetingStart(MeetingRequestAcceptedHostEvent ev)
        {
            if (Swallowed == null) return;

            if (MeetingStartAction.GetValue() == 0) // 殺
            {
                MyPlayer.MurderPlayer(Swallowed, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                OnSwallowReset(MyPlayer, Swallowed);
            }
            else if (MeetingStartAction.GetValue() == 1) // 解放
            {
                RpcReleaseEvent.Invoke((Swallowed, MyPlayer));
                OnSwallowReset(MyPlayer, Swallowed);
            }
        }

        // 呑み込んだ時に呑み込まれた側で発火する
        static internal readonly RemoteProcess<(GamePlayer player, GamePlayer target)> RpcSwallowOnTargetOwner = new(
        "RpcSwallowOnTargetOwner_Uchu",
        (message, _) =>
        {
            if (message.target.AmOwner)
            {
                if (Minigame.Instance) Minigame.Instance.Close();
                if (MapBehaviour.Instance) MapBehaviour.Instance.Close();
                if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySoundImmediate(VentDiscoveredAudio, false); // SE
                AmongUsUtil.ToggleCamTarget(message.player.VanillaPlayer, null);
                new VisionModule.GrayVision().Register(NebulaAPI.CurrentGame!);
            }
        });

        static internal readonly RemoteProcess<(GamePlayer player, GamePlayer target)> RpcSwallowKillOnTargetOwner = new(
        "RpcSwallowKillOnTargetOwner_Uchu",
        (message, _) =>
        {
            if (message.target.AmOwner)
            {
                AmongUsUtil.SetCamTarget(null);
            }
        });

        static private RemoteProcess<(GamePlayer player, GamePlayer target)> RpcSwallowKillSetPos = new("RpcSwallowKillSetPos_Uchu",
        (message, _) =>
        {
            var player = Helpers.GetPlayer(message.player.PlayerId);
            var swallowed = Helpers.GetPlayer(message.target.PlayerId);
            if (player == null || swallowed == null) return;
            swallowed.transform.position = player.transform.position;
        });

        void OnDiedSwallow(PlayerDieOrDisconnectEvent ev)
        {
            if (ev.Player == Swallowed && ev.Player.MyKiller != MyPlayer) // 呑み込んでるプレイヤーが他の要因で死んだ
            {
                AmongUsUtil.PlayCustomFlash(VColor.Black, 0f, 0.25f, 0.45f);
                //ev.Player.VanillaPlayer.NetTransform.RpcSnapTo(MyPlayer.Position);
                OnSwallowReset(MyPlayer, Swallowed);
            }

            if (ev.Player == MyPlayer && OnSwallow) // 呑み込んでる最中に自分が死んだ
            {
                RpcReleaseEvent.Invoke((Swallowed!, MyPlayer!));
                Swallowed.VanillaPlayer.NetTransform.RpcSnapTo(MyPlayer.Position);
                Swallowed.VanillaPlayer.ModDive(false);

                OnSwallowReset(MyPlayer, Swallowed!);
            }
        }

        void OnSwallowReset(GamePlayer my, GamePlayer swallow) // リセット系の処理を詰め込む
        {
            if (swallowIcon) GameObject.Destroy(swallowIcon!.gameObject);
            swallowIcon = null;
            Swallowed = null;
            RpcShareSwallowPlayer.Invoke((MyPlayer, swallow, false));

            swallowStartTime = -1f;
            penaltyExecuted = false;

            swallow.RemoveAttributeByTag("gluttonyU::invisible");
            NameVisibleModule.RpcShowName.Invoke(swallow);
            RpcSwallowKillOnTargetOwner.Invoke((my, swallow));

            MyPlayer.RemoveAttributeByTag("gluttonyU::bloody");

            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
            swallowButton.StartCoolDown();
        }

        static internal RemoteProcess<(GamePlayer player, GamePlayer swallowed, bool IsPlayer)> RpcShareSwallowPlayer = new("RpcShareSwallowPlayer_Uchu",
        (message, _) =>
        {
            if (message.IsPlayer)
            {
                if (message.player.TryGetAbility<GluttonyU.Ability>(out var gluttony))
                {
                    gluttony.Swallowed = message.swallowed;
                }
            }
            else
            {
                if (message.player.TryGetAbility<GluttonyU.Ability>(out var gluttony))
                {
                    gluttony.Swallowed = null;
                }
            }
        });

        void OnRoleInfo(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            if (Swallowed == null) return;

            if (Swallowed.AmOwner)
            {
                if (ev.Player != MyPlayer) return;
                ev.CanSeeRole = true;
            }
        }

        void VcPos(FixSpeakerPositionEvent ev)
        {
            if (MyPlayer.AmOwner) return;

            if (ev.Player == Swallowed)
            {
                ev.Position = new VVector2(1000f, 1000f);
            }
        }
    }

    /// <summary>
    /// グラトニーの呑み込み中にグラトニーが死亡し<br>
    /// 解放された時に発火します。</br>
    /// </summary>
    public class PlayerSwallowReleaseEvent : AbstractPlayerEvent
    {
        public Virial.Game.Player Swallowed { get; private set; }

        internal PlayerSwallowReleaseEvent(GamePlayer player, GamePlayer swalloewd) : base(player) 
        {
            this.Swallowed = swalloewd;
        }
    }

    static internal RemoteProcess<(GamePlayer player, GamePlayer swallowed)> RpcReleaseEvent = new("RpcPlayerSwallowReleaseEvent_Uchu",
    (message, _) =>
    {
        GameOperatorManager.Instance?.Run<PlayerSwallowReleaseEvent>(new PlayerSwallowReleaseEvent(message.player, message.swallowed));
    });

    static private AudioClip VentDiscoveredAudio
    {
        get
        {
            if (!field)
            {
                var console = UnityEngine.Object.FindObjectOfType<VentCleaningConsole>();

                if (console != null) field = console.ImpostorDiscoveredSound;
            }

            return field;
        }
    }
}