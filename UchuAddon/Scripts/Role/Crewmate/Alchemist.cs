using Hori.Core;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Virial.Attributes;
using Virial.Events.Player;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

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
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.alchemistU.alchemy", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int leftAlchemy = 0;


        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var AlchemyTracker = ObjectTrackers.ForDeadBody(this, null, MyPlayer, (d) => true);

                var AlchemyButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    AlchemyUCooldown, "Alchemy", AlchemySprite,
                    _ => AlchemyTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                AlchemyButton.OnClick = (button) => {
                    NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.CleanCorpseAction);
                    AmongUsUtil.RpcCleanDeadBody(AlchemyTracker.CurrentTarget!, MyPlayer.PlayerId, EventDetail.Clean);
                    leftAlchemy += alchemycount;
                    AlchemyButton.UpdateUsesIcon(leftAlchemy.ToString());
                    AlchemyButton.StartCoolDown();
                };
                AlchemyButton.ShowUsesIcon(3, leftAlchemy.ToString());
                if (base.AmOwner)
                {
                    string prefix = Language.Translate("alchemistU.leftalchemy");
                    Helpers.TextHudContent("alchemistUleftText", this, delegate (TextMeshPro tmPro)
                    {
                        tmPro.text = prefix + ": " + leftAlchemy.ToString();
                    }, true);
                }

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
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p.MyPlayer, PlayerState.Dead, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);
                    leftAlchemy--;
                    leftShotsPerMeeting--;
                },
                p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftAlchemy > 0 && leftShotsPerMeeting > 0 && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));
        }

    }
}