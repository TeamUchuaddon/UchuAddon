using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Neutral;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Virial.Components;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Impostor;

public class EvilDecoratorU : DefinedSingleAbilityRoleTemplate<EvilDecoratorU.Ability>, DefinedRole,IAssignableDocument
{
    public EvilDecoratorU() : base("evilDecoratorU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [NumOfDecorationOption, DecorationCoolDownOption, ImpostorFilterOption, CrewmateFilterOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/EvilDecorator.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private IntegerConfiguration NumOfDecorationOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.maxdecorationCount", (1, 99), 5);
    static private FloatConfiguration DecorationCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.decorationCoolDown", (5f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static internal SimpleRoleFilterConfiguration ImpostorFilterOption = new("options.role.decoratorU.ImpostorgiveFilter") { RolePredicate = r => r.Team == NebulaTeams.ImpostorTeam && r.Category == RoleCategory.ImpostorRole && r.ShowOnHelpScreen && r != SpectrumU.MyRole, ScrollerTag = "ImpostorFilter", InvertOption = false, PreviewOnlySpawnableRoles = false };
    static internal SimpleRoleFilterConfiguration CrewmateFilterOption = new("options.role.decoratorU.CrewmategiveFilter") { RolePredicate = r => r.Team == NebulaTeams.CrewmateTeam && r.Category == RoleCategory.CrewmateRole && r.ShowOnHelpScreen && r != GlitchU.MyRole, ScrollerTag = "CrewmateFilter", InvertOption = false, PreviewOnlySpawnableRoles = false };

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Decorator.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public EvilDecoratorU MyRole = new EvilDecoratorU();

    static private GameStatsEntry StatsDecorated = NebulaAPI.CreateStatsEntry("stats.evildecorator.Decorated", GameStatsCategory.Roles, MyRole);

    static private readonly Virial.Media.Image DecorationImage = NebulaAPI.AddonAsset.GetResource("DecorationButton.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DecorationImage, "role.decoratorU.ability.decoration");
    }

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

                var DecorationButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DecorationCoolDownOption, "decoration", DecorationImage, _ => playerTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                DecorationButton.Visibility = (button) => !MyPlayer.IsDead && leftDecoration > 0;
                DecorationButton.ShowUsesIcon(0, leftDecoration.ToString());
                DecorationButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    var IallOptions = Roles.AllRoles.Where(r => ImpostorFilterOption.Contains(r)).ToList();
                    var CallOptions = Roles.AllRoles.Where(r => CrewmateFilterOption.Contains(r)).ToList();
                    if (target != null)
                    {
                        if (target.IsTrueCrewmate) //クルーメイト
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
                        else if (target.IsImpostor) //インポスター
                        {
                            byte targetId = target.RealPlayer.PlayerId;
                            var availableOptions = IallOptions;

                            if (lastDecoratedRole.ContainsKey(targetId))
                            {
                                var currentMod = lastDecoratedRole[targetId];
                                availableOptions = IallOptions.Where(m => m.Id != currentMod.Id).ToList();
                            }


                            if (availableOptions.Count == 0) availableOptions = IallOptions;
                            var selectedRole = availableOptions[UnityEngine.Random.Range(0, availableOptions.Count)];
                            target.RealPlayer.SetRole(selectedRole);
                            lastDecoratedRole[targetId] = selectedRole;
                        }
                        else //それ以外(マッドメイト、第三陣営の場合)
                        {

                        }

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