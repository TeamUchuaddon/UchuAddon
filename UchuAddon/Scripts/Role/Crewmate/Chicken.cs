using AsmResolver.PE.DotNet.ReadyToRun;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Modifier;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Hori.Scripts.Role.Impostor.BoxerU;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using compat = Hori.Core.UchuTranslatable;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles), NebulaRPCHolder]
internal class ChickenU : DefinedSingleAbilityRoleTemplate<ChickenU.Ability>, DefinedRole,IAssignableDocument, HasCitation
{
    private ChickenU() : base("chickenU", new(255, 238, 0), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfWinTask,
    new GroupConfiguration("options.role.chickenU.group.dead", [DeadPercentVent,DeadPercentLadder,DeadPercentZipLine,DeadPercentPlatform,DeadPercentDoor,DeadElectricSecond], UnityEngine.Color.yellow)])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Chicken.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;

    static public readonly FloatConfiguration NumOfWinTask = NebulaAPI.Configurations.Configuration("options.role.chickenU.numOfWinTask", (0, 100, 10), 60, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentVent = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentVent", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentLadder = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentLadder", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentZipLine = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentZipLine", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentPlatform = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentPlatform", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentDoor = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentDoor", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadElectricSecond = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadElectricSecond", (10f, 180f, 5f), 40f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public ChickenU MyRole = new ChickenU();

    private static MultiImage DocumentImage = NebulaAPI.AddonAsset.GetResource("ChickenDocument.png")!.AsMultiImage(7, 1, 100f)!;
    static private Image DoorIcon => DocumentImage.AsLoader(0);static private Image VentIcon => DocumentImage.AsLoader(1);static private Image MessIcon => DocumentImage.AsLoader(2);static private Image DropIcon => DocumentImage.AsLoader(3);static private Image FallIcon => DocumentImage.AsLoader(4);static private Image WussIcon => DocumentImage.AsLoader(5);

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DoorIcon, "role.chicken.ability.Door");
        yield return new(VentIcon, "role.chicken.ability.Vent");
        yield return new(MessIcon, "role.chicken.ability.Mess");
        yield return new(DropIcon, "role.chicken.ability.Drop");
        yield return new(FallIcon, "role.chicken.ability.Fall");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%TASK%", NumOfWinTask.GetValue().ToString());
    }

        static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Chicken.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "chickenU.common1")
        {
            RelatedRole = ChickenU.MyRole,
            Goal = 15,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.chickenU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "chickenU.common1")
        {
            Goal = 15
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "chickenU.common2")
        {
            RelatedRole = ChickenU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "chickenU.hard1")
        {
            RelatedRole = ChickenU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "chickenU.horse1")
        {
            RelatedRole = ChickenU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "chickenU.challenge1")
        {
            RelatedRole = ChickenU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    static internal RemoteProcess<(GamePlayer chicken, bool isAvoid)> RpcChickenEvent = new("RpcChickenEvent_Uchu",
    (message, _) =>
    {
    if (message.chicken != null) GameOperatorManager.Instance?.Run<ChickenCrisisEvent>(new ChickenCrisisEvent(message.chicken, message.isAvoid));
    });

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        bool FrameVent = false;
        float VentDead = DeadPercentVent;
        float LadderDead = DeadPercentLadder;
        float ZipLineDead = DeadPercentZipLine;
        float PlatformDead = DeadPercentPlatform;
        float DoorDead = DeadPercentDoor;
        float Electric = 0;
        private ObjectTracker<Vent>? ventTracker;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {

        }

        float Ladder;
        float Vent;
        float ZipLine;
        float Platform;
        float Door;
        bool isTrackingNow;

        [Local]
        void Update(GameUpdateEvent ev)
        {
            ventTracker ??= ObjectTrackers.ForVents(this, 0.6f, MyPlayer, v => true, UnityEngine.Color.yellow, true);
            isTrackingNow = ventTracker.CurrentTarget != null;
            if (FrameVent && isTrackingNow)
            {
                FrameVent = isTrackingNow;
                return;
            }
            if (isTrackingNow)
            {
                if (ventTracker.CurrentTarget != null)
                {
                    Vent targetVent = ventTracker.CurrentTarget;
                    Vent = UnityEngine.Random.Range(1f, 100f);
                    if (Vent <= VentDead)
                    {
                        MyPlayer.Suicide(compat.ChickenVentDead, PlayerState.Suicide, KillParameter.RemoteKill);
                    }

                    RpcChickenEvent.Invoke((MyPlayer, Vent > VentDead));
                }
            }

            FrameVent = isTrackingNow;
        }

        [OnlyMyPlayer]
        void LadderCheck(PlayerClimbLadderEvent ev)
        {
            Ladder = UnityEngine.Random.Range(1f, 100f);
            if (Ladder <= LadderDead)
            {
                MyPlayer.Suicide(compat.ChickenFallDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            RpcChickenEvent.Invoke((MyPlayer, Ladder > LadderDead));
        }

        [OnlyMyPlayer]
        void ZipCheck(PlayerUseZiplineEvent ev)
        {
            ZipLine = UnityEngine.Random.Range(1f, 100f);
            if (ZipLine <= ZipLineDead)
            {
                MyPlayer.Suicide(compat.ChickenFallDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            RpcChickenEvent.Invoke((MyPlayer, ZipLine > ZipLineDead));
        }

        [OnlyMyPlayer]
        void PlatformCheack(PlayerUseMovingPlatformEvent ev)
        {
            Platform = UnityEngine.Random.Range(1f, 100f);
            if (Platform <= PlatformDead)
            {
                MyPlayer.Suicide(compat.ChickenDropDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void DoorCheack(PlayerBeginMinigameByDoorLocalEvent ev)
        {
            Door = UnityEngine.Random.Range(1f, 100f);
            if (Door <= DoorDead)
            {
                MyPlayer.Suicide(compat.ChickenDoorDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            RpcChickenEvent.Invoke((MyPlayer, Door > DoorDead));
        }

        [Local]
        void update(GameUpdateEvent ev)
        {
            bool ElectricSabo = PlayerTask.PlayerHasTaskOfType<ElectricTask>(PlayerControl.LocalPlayer);
            if (MyPlayer.IsDead || MeetingHud.Instance || ExileController.Instance) return;
            if (ElectricSabo)
            {
                Electric += Time.deltaTime;
                if (Electric >= DeadElectricSecond)
                {
                    MyPlayer.Suicide(compat.ChickenMessDead, PlayerState.Suicide, KillParameter.RemoteKill);
                }
            }
            else
            {
                Electric = 0f;
            }
        }

        bool wins = false;
        float lastShareTime = -10f;
        void WinUpdate(GameUpdateEvent ev)
        {
            if (NebulaGameManager.Instance!.CurrentTime > lastShareTime + 0.2f)
            {
                lastShareTime = NebulaGameManager.Instance!.CurrentTime;

                if (NumOfWinTask > 0)
                {
                    wins = (float)MyPlayer.Tasks.CurrentCompleted / (float)MyPlayer.Tasks.CurrentTasks >= NumOfWinTask / 100f;
                    ShareChicken((float)MyPlayer.Tasks.CurrentCompleted / (float)MyPlayer.Tasks.CurrentTasks >= NumOfWinTask / 100f);
                }
                else if (NumOfWinTask <= 0)
                {
                    wins = true;
                    ShareChicken(true);
                }
            }
        }

        [NebulaRPC]
        void ShareChicken(bool win)
        {
            this.wins = win;
        }

        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (!MyPlayer.IsTrueCrewmate) return;
            if (wins && ev.OriginalEndReason != GameEndReason.Sabotage) ev.TryOverwriteEnd(UchuGameEnd.CrewmateChickenWin, GameEndReason.Special);
        }

        int GameAvoid = 0;
        [Local, OnlyMyPlayer]
        void OnCirisis(ChickenCrisisEvent ev)
        {
            if (ev.IsAvoid)
            {
                GameAvoid++;
                AchievementUnLock.TitleUnLock("uchuaddon.chickenU.common1");

                if (GameAvoid >= 10)
                {
                    AchievementUnLock.TitleUnLock("uchuaddon.chickenU.common2");
                    if(MyPlayer.TryGetModifier<HorseU.Instance>(out _)) AchievementUnLock.TitleUnLock("uchuaddon.chickenU.horse1");
                }
            }
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == UchuGameEnd.CrewmateChickenWin && ev.EndState.Winners.Test(MyPlayer))
            {
                if (ev.EndState.OriginalEndReason.IsSpecial()) AchievementUnLock.TitleUnLock("uchuaddon.chickenU.hard1");

                var impostors = GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.ImpostorRole).ToList();
                bool allImpostorsAlive = impostors.All(p => !p.IsDead);

                if(ev.EndState.OriginalEndCondition == NebulaGameEnd.ImpostorWin && allImpostorsAlive)
                {
                    AchievementUnLock.TitleUnLock("uchuaddon.chickenU.challenge1");
                }
            }
        }

        [OnlyMyPlayer, Local, EventPriority(EventPriority.VeryLow)]
        void DecorateOtherPlayerName(PlayerSetFakeRoleNameEvent ev)
        {
            if(!wins) ev.Append(Language.Translate("role.chickenU.noWin").Color(UnityEngine.Color.gray));      
        }
    }
}

[NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
public class ChickenWinAssignRule : AbstractModule<IGameModeStandard>, IGameOperator
{
    static ChickenWinAssignRule() => DIManager.Instance.RegisterModule(() => new ChickenWinAssignRule());

    public ChickenWinAssignRule() => this.RegisterPermanently();

    void CheckWin(PlayerCheckWinEvent ev)
    {
        if (ev.GameEnd != UchuGameEnd.CrewmateChickenWin) return;

        if (ev.Player.Role.Role.Team == NebulaTeams.CrewmateTeam)
        {
            ev.SetWinIf(true);
        }
    }
}

public class ChickenModifierU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, HasCitation
{
    private ChickenModifierU() : base("chickenModifierU", "CHM", new(255, 238, 0), [
         new GroupConfiguration("options.role.chickenU.group.dead", [DeadPercentVent,DeadPercentLadder,DeadPercentZipLine,DeadPercentPlatform,DeadPercentDoor,DeadElectricSecond], UnityEngine.Color.yellow)])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Chicken.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [ChickenU.MyRole, MyRole];

    static public readonly FloatConfiguration DeadPercentVent = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentVent", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentLadder = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentLadder", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentZipLine = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentZipLine", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentPlatform = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentPlatform", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentDoor = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentDoor", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadElectricSecond = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadElectricSecond", (10f, 180f, 5f), 40f, FloatConfigurationDecorator.Second);

    static public ChickenModifierU MyRole = new ChickenModifierU();


    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Chicken.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        bool FrameVent = false;
        float VentDead = DeadPercentVent;
        float LadderDead = DeadPercentLadder;
        float ZipLineDead = DeadPercentZipLine;
        float PlatformDead = DeadPercentPlatform;
        float DoorDead = DeadPercentDoor;
        float Electric = 0;
        private ObjectTracker<Vent>? ventTracker;
        bool isTrackingNow;


        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {

        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            ventTracker ??= ObjectTrackers.ForVents(this, 0.6f, MyPlayer, v => true, UnityEngine.Color.yellow, true);
            isTrackingNow = ventTracker.CurrentTarget != null;
            if (FrameVent && isTrackingNow)
            {
                FrameVent = isTrackingNow;
                return;
            }
            if (isTrackingNow)
            {
                if (ventTracker.CurrentTarget != null)
                {
                    Vent targetVent = ventTracker.CurrentTarget;
                    float Vent = UnityEngine.Random.Range(1f, 100f);
                    if (Vent <= VentDead)
                    {
                        MyPlayer.Suicide(compat.ChickenVentDead, PlayerState.Suicide, KillParameter.RemoteKill);
                    }

                    ChickenU.RpcChickenEvent.Invoke((MyPlayer, Vent > VentDead));
                }
            }

            FrameVent = isTrackingNow;
        }

        [OnlyMyPlayer]
        void LadderCheck(PlayerClimbLadderEvent ev)
        {
            float Ladder = UnityEngine.Random.Range(1f, 100f);
            if (Ladder <= LadderDead)
            {
                MyPlayer.Suicide(compat.ChickenFallDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            ChickenU.RpcChickenEvent.Invoke((MyPlayer, Ladder > LadderDead));
        }

        [OnlyMyPlayer]
        void ZipCheck(PlayerUseZiplineEvent ev)
        {
            float ZipLine = UnityEngine.Random.Range(1f, 100f);
            if (ZipLine <= ZipLineDead)
            {
                MyPlayer.Suicide(compat.ChickenFallDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            ChickenU.RpcChickenEvent.Invoke((MyPlayer, ZipLine > ZipLineDead));
        }

        [OnlyMyPlayer]
        void Platform(PlayerUseMovingPlatformEvent ev)
        {
            float Platform = UnityEngine.Random.Range(1f, 100f);
            if (Platform <= PlatformDead)
            {
                MyPlayer.Suicide(compat.ChickenDropDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }

            ChickenU.RpcChickenEvent.Invoke((MyPlayer, Platform > PlatformDead));
        }

        [OnlyMyPlayer]
        void Door(PlayerBeginMinigameByDoorLocalEvent ev)
        {
            float Door = UnityEngine.Random.Range(1f, 100f);
            if (Door <= DoorDead)
            {
                MyPlayer.Suicide(compat.ChickenDoorDead, PlayerState.Suicide, KillParameter.RemoteKill);
            }
        }

        [Local]
        void update(GameUpdateEvent ev)
        {
            bool ElectricSabo = PlayerTask.PlayerHasTaskOfType<ElectricTask>(PlayerControl.LocalPlayer);
            if (MyPlayer.IsDead) return;
            if (ElectricSabo)
            {
                Electric += Time.deltaTime;
                if (Electric >= DeadElectricSecond)
                {
                    MyPlayer.Suicide(compat.ChickenMessDead, PlayerState.Suicide, KillParameter.RemoteKill);
                }
            }
            else
            {
                Electric = 0f;
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        int GameAvoid = 0;
        [Local, OnlyMyPlayer]
        void OnCirisis(ChickenCrisisEvent ev)
        {
            if (ev.IsAvoid)
            {
                GameAvoid++;
                AchievementUnLock.TitleUnLock("uchuaddon.chickenU.common1");

                if (GameAvoid >= 10)
                {
                    AchievementUnLock.TitleUnLock("uchuaddon.chickenU.common2");
                    if (MyPlayer.TryGetModifier<HorseU.Instance>(out _)) AchievementUnLock.TitleUnLock("uchuaddon.chickenU.horse1");
                }
            }
        }
    }
}

/// <summary>
/// Chickenが危機になると発火するイベントです。
/// </summary>
public class ChickenCrisisEvent : AbstractPlayerEvent
{
    /// <summary>
    /// 危機を回避した場合trueを返します。
    /// </summary>
    public bool IsAvoid;

    internal ChickenCrisisEvent(Virial.Game.Player player, bool isAvoid) : base(player)
    {
        this.IsAvoid = isAvoid;
    }
}