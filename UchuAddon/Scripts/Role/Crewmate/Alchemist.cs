using Hori.Scripts.Core;
using Nebula.Roles.Crewmate;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using TMPro;
using Virial.Achievements;
using Virial.Runtime;
using compat = Hori.Core.UchuTranslatable;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class AlchemistU : DefinedSingleAbilityRoleTemplate<AlchemistU.Ability>, DefinedRole, IAssignableDocument
{
    public AlchemistU() : base("alchemistU", new(42, 44, 80), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [AlchemyUCooldown, alchemycount, NumOfPerMeeting])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Alchemist.png")!.AsImage(115f);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    static private FloatConfiguration AlchemyUCooldown = NebulaAPI.Configurations.Configuration("options.role.AlchemistU.AlchemyCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfPerMeeting = NebulaAPI.Configurations.Configuration("options.role.AlchemistU.NumOfShotPerMeeting", (1, 15), 5);
    static public readonly IntegerConfiguration alchemycount = NebulaAPI.Configurations.Configuration("options.role.AlchemistU.alchemycount", (1, 15), 3);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Alchemist.png")!.AsImage(100f)!;
    static private readonly Virial.Media.Image AlchemySprite = NebulaAPI.AddonAsset.GetResource("Alchemy.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image BulletSprite = NebulaAPI.AddonAsset.GetResource("AlchemyBullet.png")!.AsImage(115f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(AlchemySprite, "role.alchemistU.ability.alchemy");
        yield return new(BulletSprite, "role.alchemistU.ability.alchemybullet");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%SEC%", alchemycount.GetValue().ToString());
    }

    static public AlchemistU MyRole = new();
    static private readonly GameStatsEntry StatsAlchemy = NebulaAPI.CreateStatsEntry("stats.alchemistU.alchemy", GameStatsCategory.Roles, MyRole);
    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.common1")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.common2")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.another1")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.hard1")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.hard2")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.hard3")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.hard4")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 20,
            Rank = TrophyLevel.Gold,
            RelatedRecord = "uchuaddon.alchemistU.hard4"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "alchemistU.hard4")
        {
            Goal = 20,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "alchemistU.challenge1")
        {
            RelatedRole = AlchemistU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow,
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int leftAlchemy = 0;
        bool MissShot = false;
        int CorrectShot = 0;
        bool CrewmateSuicide = false;
        bool Hard1 = false;
        bool Hard2 = false;
        private List<GamePlayer>? AlchemyKillers = new List<GamePlayer>();

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var AlchemyTracker = ObjectTrackers.ForDeadBody(this, null, MyPlayer, (d) => true);

                var AlchemyButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    AlchemyUCooldown, "Alchemy", AlchemySprite,
                    _ => AlchemyTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                AlchemyButton.OnClick = (button) =>
                {
                    var body = AlchemyTracker.CurrentTarget;
                    if (body == null) return;
                    var deadPlayer = body.Player;
                    var killer = deadPlayer.MyKiller;

                    NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.CleanCorpseAction);
                    AmongUsUtil.RpcCleanDeadBody(body, MyPlayer.PlayerId, EventDetail.Clean);
                    leftAlchemy += alchemycount;
                    AlchemyButton.UpdateUsesIcon(leftAlchemy.ToString());
                    StatsAlchemy.Progress();

                    if (killer != null) AlchemyKillers!.Add(killer);
                    var state = deadPlayer.PlayerState;
                    if (deadPlayer.IsTrueCrewmate && (state == PlayerState.Suicide ||state == PlayerState.Pseudocide ||state == PlayerState.Punished ||state == PlayerState.Misfired ||state == PlayerState.Layoff ||state == PlayerState.SelfSacrifice ||state == compat.ChickenMessDead ||state == compat.ChickenDoorDead ||state == compat.ChickenDropDead ||state == compat.ChickenFallDead ||state == compat.ChickenVentDead))
                    {
                        CrewmateSuicide = true;
                    }
                    AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.hard4");
                    AlchemyButton.StartCoolDown();
                };
                AlchemyButton.ShowUsesIcon(3, leftAlchemy.ToString());

                string prefix = Language.Translate("alchemistU.leftalchemy");
                Helpers.TextHudContent("alchemistUleftText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = prefix + ": " + leftAlchemy.ToString();
                }, true);
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            int leftShotsPerMeeting = NumOfPerMeeting;
            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(BulletSprite,
                p =>
                {
                    bool targetIsTrueCrewmate = p.MyPlayer.IsTrueCrewmate;
                    bool targetIsTrueUnCrewmate = !p.MyPlayer.IsTrueCrewmate;
                    bool targetIsKiller = p.MyPlayer.IsKiller;
                    bool DeadBodyKiller = AlchemyKillers!.Any(k => k.PlayerId == p.MyPlayer.PlayerId);
                    int deadBefore = NebulaAPI.CurrentGame!.GetAllPlayers().Count(x => x.IsDead);

                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p.MyPlayer, PlayerState.Dead, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);
                    leftAlchemy--;
                    leftShotsPerMeeting--;
                    AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.common1");

                    if (targetIsTrueCrewmate)
                    {
                        AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.another1");
                        MissShot = true;
                    }
                    if (targetIsTrueUnCrewmate && CrewmateSuicide)
                    {
                        Hard2 = true;
                    }
                    if (targetIsKiller)
                    {
                        AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.common2");
                        CorrectShot++;
                        if (deadBefore == 1)
                        {
                            Hard1 = true;
                        }
                    }
                    if (DeadBodyKiller)
                    {
                        AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.hard3");
                    }
                },
                p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftAlchemy > 0 && leftShotsPerMeeting > 0 && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            Hard1 = false;
            Hard2 = false;
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            bool iWon = ev.EndState.Winners.Test(MyPlayer);

            if (Hard1 && iWon)
            {
                AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.hard1");
            }
            if (Hard2 && iWon)
            {
                AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.hard2");
            }
            if (CorrectShot >= 3 && !MissShot && iWon)
            {
                AchievementUnLock.TitleUnLock("uchuaddon.alchemistU.challenge1");
            }
        }
    }
}