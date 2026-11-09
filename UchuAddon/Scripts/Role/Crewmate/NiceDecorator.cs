using Hori.Core;
using Hori.Scripts.Role.Impostor;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Crewmate;

public class NiceDecorator : DefinedSingleAbilityRoleTemplate<NiceDecorator.Ability>, DefinedRole, IAssignableDocument
{
    public NiceDecorator() : base("niceDecoratorU", new(253f / 255f, 187f / 255f, 22f / 255f), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfDecorationOption, DecorationCoolDownOption, ImpostorFilterOption, CrewmateFilterOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/NiceDecorator.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private IntegerConfiguration NumOfDecorationOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.maxdecorationCount", (1, 10, 1), 5);
    static private FloatConfiguration DecorationCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.decorationCoolDown", (5f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static internal SimpleRoleFilterConfiguration ImpostorFilterOption = new("options.role.decoratorU.ImpostorgiveFilter") { RolePredicate = r => r.Team == NebulaTeams.ImpostorTeam && r.Category == RoleCategory.ImpostorRole && r.ShowOnHelpScreen && r != SpectrumU.MyRole, ScrollerTag = "ImpostorFilter", InvertOption = false, PreviewOnlySpawnableRoles = false };
    static internal SimpleRoleFilterConfiguration CrewmateFilterOption = new("options.role.decoratorU.CrewmategiveFilter") { RolePredicate = r => r.Team == NebulaTeams.CrewmateTeam && r.Category == RoleCategory.CrewmateRole && r.ShowOnHelpScreen && r != GlitchU.MyRole, ScrollerTag = "CrewmateFilter", InvertOption = false, PreviewOnlySpawnableRoles = false };

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Decorator.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public NiceDecorator MyRole = new();

    static private GameStatsEntry StatsDecorated = NebulaAPI.CreateStatsEntry("stats.nicedecorator.Decorated", GameStatsCategory.Roles, MyRole);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DecorationImage, "role.decoratorU.ability.decoration");
    }
    static private readonly Virial.Media.Image DecorationImage = NebulaAPI.AddonAsset.GetResource("DecorationButtonNice.png")!.AsImage(115f)!;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        private Dictionary<byte, DefinedRole> lastDecoratedRole = new Dictionary<byte, DefinedRole>();
        int leftDecoration = NumOfDecorationOption;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var DecorationButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DecorationCoolDownOption, "decoration", DecorationImage, _ => playerTracker.CurrentTarget != null);
                DecorationButton.Visibility = (button) => !MyPlayer.IsDead && leftDecoration > 0;
                DecorationButton.ShowUsesIcon(3, leftDecoration.ToString());
                DecorationButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    var IallOptions = Roles.AllRoles.Where(r => ImpostorFilterOption.Contains(r)).ToList();
                    var CallOptions = Roles.AllRoles.Where(r => CrewmateFilterOption.Contains(r)).ToList();
                    if (target != null)
                    {
                        if (target.IsTrueCrewmate && CallOptions.Count > 0)
                        {
                            byte targetId = target.RealPlayer.PlayerId;
                            var availableOptions = CallOptions;

                            if (lastDecoratedRole.ContainsKey(targetId))
                            {
                                var currentRole = lastDecoratedRole[targetId];
                                availableOptions = CallOptions.Where(r => r.Id != currentRole.Id).ToList();
                            }

                            if (availableOptions.Count == 0) availableOptions = CallOptions;
                            var selectedRole = availableOptions[UnityEngine.Random.Range(0, availableOptions.Count)];
                            target.RealPlayer.SetRole(selectedRole);
                            lastDecoratedRole[targetId] = selectedRole;
                        }
                        else if (target.IsImpostor && IallOptions.Count > 0)
                        {
                            byte targetId = target.RealPlayer.PlayerId;
                            var availableOptions = IallOptions;

                            if (lastDecoratedRole.ContainsKey(targetId))
                            {
                                var currentRole = lastDecoratedRole[targetId];
                                availableOptions = IallOptions.Where(r => r.Id != currentRole.Id).ToList();
                            }

                            if (availableOptions.Count == 0) availableOptions = IallOptions;
                            var selectedRole = availableOptions[UnityEngine.Random.Range(0, availableOptions.Count)];
                            target.RealPlayer.SetRole(selectedRole);
                            lastDecoratedRole[targetId] = selectedRole;
                        }

                        // クルー・インポスター共通の後処理
                        button.StartCoolDown();
                        StatsDecorated.Progress();
                        leftDecoration--;
                        DecorationButton.UpdateUsesIcon(leftDecoration.ToString());
                    }
                };
            }
        }
    }
}
//I=Impostor,C=Crewmate
