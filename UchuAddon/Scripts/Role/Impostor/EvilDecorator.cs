using Hori.Core;
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
    static List<DefinedRole> IgiveableRoles = new List<DefinedRole>();
    static List<DefinedRole> CgiveableRoles = new List<DefinedRole>();
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private IntegerConfiguration NumOfDecorationOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.maxdecorationCount", (1, 10), 5);
    static private FloatConfiguration DecorationCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.decoratorU.decorationCoolDown", (20f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static internal IConfiguration ImpostorFilterOption = NebulaAPI.Configurations.Configuration(() => null, () => NebulaAPI.GUI.LocalizedButton(Virial.Media.GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OptionsTitleHalf), "options.role.decoratorU.ImpostorgiveFilter", _ => OpenImpostorFilterEditor()));
    static internal IConfiguration CrewmateFilterOption = NebulaAPI.Configurations.Configuration(() => null, () => NebulaAPI.GUI.LocalizedButton(Virial.Media.GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OptionsTitleHalf), "options.role.decoratorU.CrewmategiveFilter", _ => OpenCrewmateFilterEditor()));

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Decorator.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public EvilDecoratorU MyRole = new EvilDecoratorU();

    static private GameStatsEntry StatsDecorated = NebulaAPI.CreateStatsEntry("stats.evildecorator.Decorated", GameStatsCategory.Roles, MyRole);
    static void OpenImpostorFilterEditor()
    {
        var impostorRoles = Roles.AllRoles.Where(r => r.Team == NebulaTeams.ImpostorTeam && r.Category == RoleCategory.ImpostorRole);
        RoleOptionHelper.OpenFilterScreen(
            "giveableImpostorsFilter",
            impostorRoles,
            role => IgiveableRoles.Contains(role),
            (role, val) =>
            {
                if (val) { if (!IgiveableRoles.Contains(role)) IgiveableRoles.Add(role); }
                else { IgiveableRoles.Remove(role); }
            },
            role =>
            {
                if (IgiveableRoles.Contains(role)) IgiveableRoles.Remove(role);
                else IgiveableRoles.Add(role);
            });

    }
    static void OpenCrewmateFilterEditor()
    {
        var crewmateRoles = Roles.AllRoles.Where(r => r.Team == NebulaTeams.CrewmateTeam);
        RoleOptionHelper.OpenFilterScreen(
            "giveableCrewmatesFilter",
            crewmateRoles,
            role => CgiveableRoles.Contains(role),
            (role, val) =>
            {
                if (val) { if (!CgiveableRoles.Contains(role)) CgiveableRoles.Add(role); }
                else { CgiveableRoles.Remove(role); }
            },
            role =>
            {
                if (CgiveableRoles.Contains(role)) CgiveableRoles.Remove(role);
                else CgiveableRoles.Add(role);
            });
    }
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

                var DecorationButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DecorationCoolDownOption, "decoration", DecorationImage, _ => playerTracker.CurrentTarget != null);
                DecorationButton.Visibility = (button) => !MyPlayer.IsDead;
                DecorationButton.ShowUsesIcon(0, leftDecoration.ToString());
                DecorationButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    var IallOptions = IgiveableRoles;
                    var CallOptions = CgiveableRoles;
                    if (target != null && CgiveableRoles.Count > 0 && IgiveableRoles.Count > 0)
                    {
                        if (target.IsCrewmate) //クルーメイト
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
                            DecorationButton.Visibility = (button) => !MyPlayer.IsDead && leftDecoration > 0;
                            button.StartCoolDown();
                            StatsDecorated.Progress();
                            leftDecoration--;
                            DecorationButton.OnUpdate = (button) =>
                            {
                                DecorationButton.UpdateUsesIcon(leftDecoration.ToString());
                            };

                            DecorationButton.ShowUsesIcon(0, leftDecoration.ToString());
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
                            var index = UnityEngine.Random.Range(0, IgiveableRoles.Count);
                            lastDecoratedRole[targetId] = (DefinedRole)selectedRole;
                            target.RealPlayer.SetRole((DefinedRole)selectedRole);
                            DecorationButton.Visibility = (button) => !MyPlayer.IsDead && leftDecoration > 0;
                            button.StartCoolDown();
                            StatsDecorated.Progress();
                            leftDecoration--;
                            DecorationButton.UpdateUsesIcon(leftDecoration.ToString());
                            DecorationButton.OnUpdate = (button) =>
                            {
                                DecorationButton.UpdateUsesIcon(leftDecoration.ToString());
                            };

                            DecorationButton.ShowUsesIcon(0, leftDecoration.ToString());
                        }
                        else //それ以外(マッドメイト、第三陣営の場合)
                        {
                            DecorationButton.Visibility = (button) => !MyPlayer.IsDead && leftDecoration > 0;
                            button.StartCoolDown();
                            StatsDecorated.Progress();
                            leftDecoration--;
                            DecorationButton.OnUpdate = (button) =>
                            {
                                DecorationButton.UpdateUsesIcon(leftDecoration.ToString());
                            };

                            DecorationButton.ShowUsesIcon(0, leftDecoration.ToString());
                        }
                    }
                };
            }

        }
    }
}
//I=Impostor,C=Crewmate