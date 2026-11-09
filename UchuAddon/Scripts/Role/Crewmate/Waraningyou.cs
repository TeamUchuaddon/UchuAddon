using Hori.Core;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles.Crewmate;
using Nebula.Utilities;
using System.Linq;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Image = Virial.Media.Image;
using Virial.Media;

namespace Nebula.Roles.Impostor;

public class WaraningyouU : DefinedSingleAbilityRoleTemplate<WaraningyouU.Ability>, DefinedRole
{
    private WaraningyouU() : base("waraningyouU", new(163, 149, 112), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [AllowSkipOption, DefaultVote])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Waraningyou.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;


    static private BoolConfiguration AllowSkipOption = NebulaAPI.Configurations.Configuration("options.role.waraningyouU.allowSkip", false);
    static private FloatConfiguration DefaultVote = NebulaAPI.Configurations.Configuration("options.role.waraningyouU.defaultVote", (0f, 5f, 1f), 0f);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new(player, arguments.GetAsBool(0));

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Waraningyou.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public readonly WaraningyouU MyRole = new();
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private Image curseButtonSprite = NebulaAPI.AddonAsset.GetResource("CurseButton.png")!.AsImage(115f)!;


        bool usingCurse = false;
        bool selfVote = false;
        int voterCount = 0;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {


            if (AmOwner)
            {

                if (!usingCurse) Nebula.Utilities.Helpers.TextHudContent("WaraningyouText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.waraningyouU.hudText") + ": " + voterCount, true);

                bool usedInTheMeeting = true;
                var meetingButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                meetingButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted && voterCount != 0;
                meetingButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.Discussion) && !usedInTheMeeting;
                meetingButton.SetLabel("curse");
                meetingButton.SetSprite(curseButtonSprite.GetSprite());
                meetingButton.OnClick = (button) =>
                {
                    usedInTheMeeting = true;
                    usingCurse = true;
                    if (!AllowSkipOption) MeetingHud.Instance.SkipVoteButton.gameObject.SetActive(false);
                };

                meetingButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };

            }
        }

        void GameStartEvent(GameStartEvent ev)
        {
            if (AmOwner)
            {
                voterCount = (int)DefaultVote;
            }
        }

        void PlayerVotedLocalEvent(PlayerVotedLocalEvent ev)
        {
            if (AmOwner)
            {
                if (!usingCurse && !selfVote)
                {
                    voterCount += ev.Voters.Count();
                }
                if (!usingCurse && selfVote)
                {
                    voterCount += ev.Voters.Count() - 1;
                    selfVote = false;
                }

            }
        }

        void MeetingEnd(MeetingEndEvent ev)
        {
            if (AmOwner && usingCurse)
            {
                usingCurse = false;
            }
        }

        void PlayerVoteCastLocalEvent(PlayerVoteCastLocalEvent ev)
        {
            if (AmOwner && !MyPlayer.IsDead)
            {
                if (ev.VoteFor == MyPlayer)
                {
                    selfVote = true;
                }

                if (usingCurse)
                {
                    ev.Vote = voterCount + 1;
                    voterCount = 0;
                }
            }
        }
    }
}