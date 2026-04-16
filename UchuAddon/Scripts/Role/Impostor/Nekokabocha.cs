using Hori.Core;
using Nebula;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Impostor;

public class NekokabochaU : DefinedSingleAbilityRoleTemplate<NekokabochaU.Ability>, HasCitation, DefinedRole, IAssignableDocument
{
    public NekokabochaU() : base("nekokabochaU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [ExileEmbroil])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Nekokabocha.png")!.AsImage(115f);
    }
    Citation? HasCitation.Citation => Nebula.Roles.Citations.TheOtherRolesGM;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Nekokabocha.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static private readonly BoolConfiguration ExileEmbroil = NebulaAPI.Configurations.Configuration("options.role.nekokabochaU.exileembroil", false);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public NekokabochaU MyRole = new();
    bool IAssignableDocument.HasTips => true;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {

            }
        }
        [OnlyHost, OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.PlayerId == MyPlayer.PlayerId) return;

            MyPlayer.MurderPlayer(ev.Murderer, PlayerState.Embroiled, EventDetail.Embroil, KillParameter.RemoteKill, KillCondition.TargetAlive);
        }
        [Local, OnlyMyPlayer]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && p.Role.Role.Category == RoleCategory.ImpostorRole))

            if (!ExileEmbroil) return;

            GamePlayer[] voters = MeetingHudExtension.LastVotedForMap
                .Where(entry => entry.Value == MyPlayer.PlayerId && entry.Key != MyPlayer.PlayerId)
                .Select(entry => NebulaGameManager.Instance!.GetPlayer(entry.Key)).ToArray()!;

            void Embroil()
            {
                ExtraExileRoleSystem.MarkExtraVictim(MyPlayer.Unbox(), false, true, false ? null : []);
            }
                Embroil();
        }
    }
}