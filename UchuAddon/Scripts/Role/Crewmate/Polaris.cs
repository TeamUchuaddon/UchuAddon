using Cpp2IL.Core.Extensions;
using Hori.Core;
using Hori.Scripts.Role.Neutral;
using Nebula;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Il2CppSystem.DateTimeParse;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using Virial.DI;
using Hori.Scripts.Core;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class PolarisU : DefinedSingleAbilityRoleTemplate<PolarisU.Ability>, DefinedRole,IAssignableDocument
{
    private PolarisU(): base("polarisU", new(130, 178, 255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfKILLOption,KillCooldownOption,CanKillHidingPlayerOption, SealAbilityUntilReportingDeadBodiesOption,MediumCooldown, MediumDuration,NumOfMedium,RewindTask,MediumRewindTask]) 
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Polaris.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public static IntegerConfiguration NumOfKILLOption = NebulaAPI.Configurations.Configuration("options.role.polarisU.numofKillOption", (1, 15), 3);
    public static FloatConfiguration KillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.polarisU.killCoolDownOption", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.canKillHidingPlayer", false);
    static internal readonly BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.sealAbilityUntilReportingDeadBodies", false);
    public static FloatConfiguration MediumCooldown = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MediumDuration = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumDuration", (0.25f, 10f, 0.25f), 3f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfMedium = NebulaAPI.Configurations.Configuration("options.role.polarisU.numOfMedium", (1, 15), 3);
    public static BoolConfiguration RewindTask = NebulaAPI.Configurations.Configuration("options.role.polarisU.rewindTask", true);
    public static IntegerConfiguration MediumRewindTask = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumRewindTask", (1, 15), 3, ()=>RewindTask);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(1));
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Polaris.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public PolarisU MyRole = new PolarisU();
    static private readonly GameStatsEntry StatsKill = NebulaAPI.CreateStatsEntry("stats.polarisU.statsKill", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsMedium = NebulaAPI.CreateStatsEntry("stats.polarisU.statsMedium", GameStatsCategory.Roles, MyRole);
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(KillImage, "role.polarisU.ability.kill");
        yield return new(MediumImage, "role.polarisU.ability.medium");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%REWIND%", Language.Translate(RewindTask ? "role.polarisU.ability.main.rewind" : "role.polarisU.ability.main.norewind"));
    }

    static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonCyan.png")!.AsImage(100f)!;
    static private readonly Virial.Media.Image MediumImage = NebulaAPI.AddonAsset.GetResource("PolarisMediumButton.png")!.AsImage(115f)!;
    private static MultiImage Icon = NebulaAPI.AddonAsset.GetResource("NoticeIcon.png")!.AsMultiImage(4, 1, 250f)!;
    static private Image NoticeIconImage => Icon.AsLoader(2);

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.common1")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.common2")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.another1")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.another2")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.hard1")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "polarisU.challenge1")
        {
            RelatedRole = PolarisU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int left = NumOfKILLOption;
        int leftMedium = NumOfMedium;
        ModAbilityButton? killButton;
        SpriteRenderer? lockSprite = null;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, CanKillHidingPlayerOption, false);
                killTracker.SetColor(MyRole.RoleColor);
                var deadBodyTracker = ObjectTrackers.ForDeadBody(this, null, MyPlayer, (d) => GamePlayer.AllPlayers.All(p => p.HoldingDeadBody != d));
                deadBodyTracker.SetColor(MyRole.RoleColor);

                var mediumButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,MediumCooldown, "polarisU.Medium", MediumImage ,_ => deadBodyTracker.CurrentTarget != null && (!RewindTask || MyPlayer.Tasks.CurrentCompleted >= MediumRewindTask)).SetAsUsurpableButton(this);
                mediumButton.ShowUsesIcon(3, leftMedium.ToString());
                mediumButton.Visibility = (button) => !MyPlayer.IsDead && leftMedium > 0;
                mediumButton.EffectTimer = NebulaAPI.Modules.Timer(this, MediumDuration);
                mediumButton.OnClick = (button) => button.StartEffect();
                mediumButton.OnEffectStart = (button) =>
                {
                    MyPlayer.GainSpeedAttribute(0f, MediumDuration, false, 100);

                    var body = deadBodyTracker.CurrentTarget;
                    if (body == null) return;

                    IsMedium = body.Player;
                };

                mediumButton.OnEffectEnd = (button) =>
                {
                    var body = deadBodyTracker.CurrentTarget;
                    if (body == null) return;

                    Player deadPlayer = body.Player;

                    NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.polarisU.message.header")),
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(deadPlayer.ColoredName + "<br>" + deadPlayer.Role.DisplayColoredName))),
                        NoticeIconImage, MyRole.RoleColor);

                    if (RewindTask)
                    {
                        MyPlayer.Tasks.RewindTasks(MediumRewindTask);
                    }
                    NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess);
                    leftMedium--;
                    mediumButton.UpdateUsesIcon(leftMedium.ToString());
                    mediumButton.StartCoolDown();
                    StatsMedium.Progress();
                    IsMedium = null;

                    TitleUnLock("uchuaddon.polarisU.common2");
                };


                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(base.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.Ability, null).SetAsUsurpableButton(this);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && left > 0 && !MyPlayer.IsImpostor && MyPlayer.Role.Role != RogueU.MyRole;
                killButton.SetImage(KillImage);
                killButton.ShowUsesIcon(3, left.ToString());
                if (SealAbilityUntilReportingDeadBodiesOption)
                {
                    if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                    {
                        lockSprite = (killButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                    }
                }
                killButton.OnClick = (button) =>
                {
                    left--;
                    killButton.UpdateUsesIcon(left.ToString());
                    GamePlayer? target = killTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

                        IsTitleCheakOnKill(target);
                    }
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.StartCoolDown();
                killButton.SetLabel("kill");
                (killButton as ModAbilityButtonImpl)!.OnMeeting = (button) =>
                {
                    if (lockSprite != null)
                    {
                        if (NebulaGameManager.Instance!.AllPlayerInfo.Any((Player p) => p.IsDead))
                        {
                            UnityEngine.Object.Destroy(lockSprite.gameObject);
                            lockSprite = null;
                        }
                    }
                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
          
                GameOperatorManager.Instance?.Subscribe<ReportDeadBodyEvent>(ev =>
                {
                    if (IsMedium == null) return;
                    
                    if (ev.Reported == IsMedium)
                    {
                        TitleUnLock("uchuaddon.polarisU.another2"); 
                    }           
                }, this);
            }
        }

        int ImpostorKill = 0;
        bool CrewKill = false;
        GamePlayer? IsMedium;

        void IsTitleCheakOnKill(GamePlayer dead)
        {
            if (dead.IsTrueCrewmate)
            {
                CrewKill = true; //challengeの判定用
                TitleUnLock("uchuaddon.polarisU.another1");
            }
            else
            {
                TitleUnLock("uchuaddon.polarisU.common1");
            }

            if (dead.IsImpostor)
            {
                ImpostorKill++;
            }

            if (MyPlayer.HasAttribute(PlayerAttributes.Accel))
            {
                TitleUnLock("uchuaddon.polarisU.hard1");
            }
        }

        [Local, OnlyMyPlayer]
        void GameEnd(GameEndEvent ev)
        {
            var impostors = GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.ImpostorRole);
            int num = impostors.Count();

            if (ev.EndState.Winners.Test(MyPlayer) && num == ImpostorKill && !CrewKill)
            {
                TitleUnLock("uchuaddon.polarisU.challenge1");
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }     
    }
}