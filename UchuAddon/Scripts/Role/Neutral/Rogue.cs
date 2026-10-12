using Hori.Scripts.Role.Crewmate;
using Il2CppSystem.Text.Json;
using Nebula.Map;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Ghost.Impostor;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class RogueU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    public static Team MyTeam = new Team("teams.rogueU", new Virial.Color(0, 74, 97), TeamRevealType.OnlyMe);
    private RogueU() : base("rogueU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption, CanKillHidingPlayerOption,
     new GroupConfiguration("options.role.rogueU.group.failure", [FailureAction, AbilityPreview, ClockCooldown, ClockDuration, ClockRatio, EchoCooldown, EchoRange], GroupConfigurationColor.ToDarkenColor(Color.Blue))])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    private static MultiImage button = NebulaAPI.AddonAsset.GetResource("RogueButton.png")!.AsMultiImage(2, 1, 115f)!;

    static private IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.rogueU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static public BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.rogueU.canKillHidingPlayer", false);

    static private ValueConfiguration<int> FailureAction = NebulaAPI.Configurations.Configuration("options.role.rogueU.failureAction", ["options.role.rogueU.failureAction.clock", "options.role.rogueU.failureAction.search"], 0);
    static private readonly IConfiguration AbilityPreview = NebulaAPI.Configurations.Configuration(() => null, () =>
    {
        int index = FailureAction.GetValue();
        var img = button.AsLoader(index);

        GUIWidget widget(int dex)
        {
            if (dex == 0)
            {
                return
                NebulaAPI.GUI.HorizontalHolder(Virial.Media.GUIAlignment.Left,
                NebulaAPI.GUI.Image(GUIAlignment.Left, img, new FuzzySize(1f, 1f)),
                NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Center, AttributeAsset.LeftBoldFixed, "options.role.rogueU.abilityPreview.clock"));
            }
            else if (dex == 1)
            {
                return
                NebulaAPI.GUI.HorizontalHolder(Virial.Media.GUIAlignment.Left,
                NebulaAPI.GUI.Image(GUIAlignment.Left, img, new FuzzySize(1f, 1f)),
                NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Center, AttributeAsset.LeftBoldFixed, "options.role.rogueU.abilityPreview.echo"));
            }
            else
            {
                return NebulaAPI.GUI.EmptyWidget;
            }
        }

        return
        NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left,
        NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Left, AttributeAsset.DocumentBold, "options.role.rogueU.abilityPreview"),
        NebulaAPI.GUI.VerticalMargin(0.1f),
        widget(index));
    });

    static private readonly FloatConfiguration ClockCooldown = NebulaAPI.Configurations.Configuration("options.role.rogueU.clockCooldown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second, () => FailureAction.GetValue() == 0);
    static private readonly FloatConfiguration ClockRatio = NebulaAPI.Configurations.Configuration("options.role.rogueU.clockRatio", (1f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio, () => FailureAction.GetValue() == 0);
    static private readonly FloatConfiguration ClockDuration = NebulaAPI.Configurations.Configuration("options.role.rogueU.clockDuration", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => FailureAction.GetValue() == 0);

    static private readonly FloatConfiguration EchoCooldown = NebulaAPI.Configurations.Configuration("options.role.rogueU.echoCooldown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second, () => FailureAction.GetValue() == 1);
    static private readonly FloatConfiguration EchoRange = NebulaAPI.Configurations.Configuration("options.role.rogueU.echoRange", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Ratio, () => FailureAction.GetValue() == 1);

    static public RogueU MyRole = new RogueU();
    bool DefinedRole.IsKiller => true;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Rogue.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public Image ClockImage => button.AsLoader(0);static public Image EchoImage => button.AsLoader(1);
    static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonRogue.png")!.AsImage(100f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    bool IAssignableDocument.HasWinCondition => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        var failureClock = FailureAction.GetValue() == 0;
        yield return new(KillImage, "role.rogueU.ability.kill");
        if(failureClock) yield return new(ClockImage, "role.rogueU.ability.clock");
        else yield return new(EchoImage, "role.rogueU.ability.echo");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%VENT%", CanKillHidingPlayerOption ? Language.Translate("role.rogueU.ability.kill.vent") : "");
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;


        public DefinedRole? GeneRole { get; private set; }
        public IPlayerAbility? GeneAbility { get; private set; }
        private FlexibleLifespan? GeneLifespan = null;

        private FlexibleLifespan? MadmateLifespan = null; 
        private MadmateAbility? MadmateAbility = null;

        private FlexibleLifespan? VultureLifespan = null;
        private VultureAbility? VultureAbility = null;

        bool isClock = false;
        bool isEcho;
        bool OptionsClock = FailureAction.GetValue() == 0;
        bool flash = false;

        public bool HasStolenAbility { get; private set; } //勝敗判定フラグ

        ModAbilityButton clockButton = null!, echoButton = null!;

        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp
        {
            get
            {
                if (GeneRole != null)
                {
                    return [MyRole, GeneRole];
                }
                else
                {
                    return [MyRole];
                }
            }
        }

        IEnumerable<IPlayerAbility> GetAbilities()
        {
            if (GeneAbility != null)
            {
                yield return GeneAbility;

                foreach (var sub in GeneAbility.SubAbilities)
                    yield return sub;
            }
        }

        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => GetAbilities();

        public bool GiveRoleAbility(DefinedRole role)
        {
            int[] arguments = role.DefaultAssignableArguments ?? [];

            GeneLifespan?.Release();
            MadmateLifespan?.Release();
            VultureLifespan?.Release();

            GeneRole = null;
            GeneAbility = null;

            if (role == Madmate.MyRole) //一部はこっちで作ったやつ付与する
            {
                MadmateLifespan = new FlexibleLifespan(this);
                MadmateAbility = new MadmateAbility(MyPlayer);
                MadmateAbility.Register(MadmateLifespan);

                GeneRole = role;
                RpcRogueShare.Invoke((MyPlayer, true));

                return true; 
            }
            else if (role == Vulture.MyRole)
            {
                VultureLifespan = new FlexibleLifespan(this);
                VultureAbility = new VultureAbility(MyPlayer);
                VultureAbility.Register(VultureLifespan);

                GeneRole = role;
                RpcRogueShare.Invoke((MyPlayer, true));

                return true;
            }
            else if(role ==GuardianU.MyRole)
            {
                return false;
            }
            else
            {
                GeneRole = role;

                if (role == Busker.MyRole)
                {
                    arguments[1] = 1;
                }

                var ability = role.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.CanLoadToKillNeutral, arguments);

                if (ability == null) return false;

                GeneLifespan = new FlexibleLifespan(this);
                (ability as FlexibleLifespan)?.Bind(GeneLifespan);

                GeneAbility = ability.Register(GeneLifespan);
                RpcRogueShare.Invoke((MyPlayer, true));

                return true;
            }
        }



        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                isClock = false;
                isEcho = false;
                RpcRogueShare.Invoke((MyPlayer, false));

                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, CanKillHidingPlayerOption);
                myTracker.SetColor(MyRole.RoleColor);

                var killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                KillCoolDownOption.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                (target, _) =>
                {
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));

                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    }
                    
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();          
                }, null,
                _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                _ => MyPlayer.AllowToShowKillButtonByAbilities);

                killButton.SetImage(KillImage);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());

                clockButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "rogueU.clock", ClockCooldown, ClockDuration, "clock", ClockImage);
                clockButton.Visibility = (button) => !MyPlayer.IsDead && isClock;
                clockButton.OnEffectStart = button =>
                {
                    MyPlayer.GainAttribute(PlayerAttributes.CooldownSpeed, ClockDuration, ClockRatio, false, 100);
                };
                clockButton.OnEffectEnd = button => button.StartCoolDown();
                clockButton.PlayFlashWhile = button => flash;

                echoButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, EchoCooldown, "rogueU.echo", EchoImage);
                echoButton.Visibility = (button) => !MyPlayer.IsDead && isEcho;
                echoButton.OnClick = (button) =>
                {
                    NebulaManager.Instance.StartCoroutine(CoEcho(MyPlayer.Position).WrapToIl2Cpp());
                    button.StartCoolDown();
                };
                echoButton.PlayFlashWhile = button => flash;
            }
        }

        void RogueSystem(PlayerMurderedEvent ev)
        {
            if (ev.Murderer != MyPlayer 
                || ev.Dead.PlayerState == PlayerState.Suicide 
                || ev.Dead.PlayerState == UchuTranslatable.ChickenDoorDead
                || ev.Dead.PlayerState == UchuTranslatable.ChickenDropDead
                || ev.Dead.PlayerState == UchuTranslatable.ChickenFallDead
                || ev.Dead.PlayerState == UchuTranslatable.ChickenMessDead
                || ev.Dead.PlayerState == UchuTranslatable.ChickenVentDead) return;

            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.45f);

                isClock = false;
                isEcho = false;

                var role = ev.Dead.Role.Role as DefinedRole;

                bool canStealAbility = GiveRoleAbility(role);

                if (!canStealAbility)
                {
                    if (OptionsClock)
                    {
                        isClock = true;
                        clockButton.StartCoolDown();

                        flash = true;
                        NebulaManager.Instance.StartDelayAction(0.08f, () =>
                        {
                            flash = false;
                        });
                    }
                    else
                    {
                        isEcho = true;
                        echoButton.StartCoolDown();

                        flash = true;
                        NebulaManager.Instance.StartDelayAction(0.08f, () =>
                        {
                            flash = false;
                        });
                    }
                }
                else
                {
                    RpcRogueShare.Invoke((MyPlayer, true));
                }
            }
        }

        IEnumerator CoEcho(Vector2 position)
        {
            EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
            float radious = 0f;
            var circle = EffectCircle.SpawnEffectCircle(null, MyPlayer.Position.ToUnityVector(), MyRole.RoleColor, 0f, null, true);
            this.BindGameObject(circle.gameObject);
            circle.OuterRadius = () => radious;

            MyRole.RoleColor.ToHSV(out var hue, out _, out _);
            bool isFirst = true;
            while (radious < EchoRange)
            {
                if (MeetingHud.Instance) break;

                radious += Time.deltaTime * 20f;
                foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                {
                    if (!p.AmOwner && !p.IsDead && !pMask.Test(p) && p.Position.Distance(position) < radious && p.TryGetAbility<IPlayerAbility>(out _))
                    {
                        pMask.Add(p);
                        AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                        isFirst = false;
                    }
                }
                yield return null;
            }

            circle.Disappear();
        }

        void ClaimRogueRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam == RogueU.MyTeam) return;

            if (MyPlayer.IsAlive) callback.MarkRemaining();
        }

        void IGameOperator.OnReleased()
        {
            GeneLifespan?.Release();

            GeneRole = null;
            GeneAbility = null;
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if ((AmOwner || canSeeAllInfo) && GeneRole != null && !MyPlayer.IsDead) name += GeneRole.GetRoleIconTagSmall();
        }

        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;

        RemoteProcess<(GamePlayer rogue, bool has)> RpcRogueShare = new("RpcRogueShare", (message, _) =>
        {
            var rogue = message.rogue.Role as RogueU.Instance;
            if (rogue != null)
            {
                rogue.HasStolenAbility = message.has;
            }
        });
    }
}

[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
internal class RogueCriteria : AbstractModule<IGameModeStandard>, IGameOperator
{
    static RogueCriteria() => DIManager.Instance.RegisterModule(() => new RogueCriteria().Register(NebulaAPI.CurrentGame!));

    [OnlyHost]
    void OnUpdate(GameUpdateEvent ev)
    {
        var roguePlayers = GamePlayer.AllPlayers.Where(p => !p.IsDead && p.Role.Role is RogueU).ToArray();

        int totalAlive = GamePlayer.AllPlayers.Count(p => !p.IsDead);

        bool leftOtherKillers = GameOperatorManager.Instance?.Run(new KillerTeamCallback(RogueU.MyTeam)).RemainingOtherTeam ?? false;

        foreach (var roguePlayer in roguePlayers)
        {
            if (totalAlive == 1)
            {
                //NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.RogueWin, GameEndReason.Situation, BitMasks.AsPlayer(1u << roguePlayer.PlayerId));
                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.RogueWin, 1 << roguePlayer.PlayerId);
                return;
            }

            if (2 >= totalAlive && !leftOtherKillers)
            {
                if (roguePlayer.Role is RogueU.Instance rogue && rogue.HasStolenAbility)
                {
                    //NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.RogueWin, GameEndReason.Situation, BitMasks.AsPlayer(1u << roguePlayer.PlayerId));
                    NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.RogueWin, 1 << roguePlayer.PlayerId);
                    return;
                }
            }
        }
    }
}