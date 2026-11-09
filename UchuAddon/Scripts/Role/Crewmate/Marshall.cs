using Hori.Core;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using Hori.Scripts.Role.Neutral;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial.Assignable;
using Virial.Helpers;
using static Il2CppSystem.DateTimeParse;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Crewmate;

public class MarshallU : DefinedSingleAbilityRoleTemplate<MarshallU.Ability>, DefinedRole, IAssignableDocument
{
    public MarshallU() : base("marshallU", new(105, 183, 191), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [provisionCooldown,NumOfProvision, MarshallkillCooldown])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Marshall.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private FloatConfiguration provisionCooldown = NebulaAPI.Configurations.Configuration("options.role.marshalllU.ProvisionCooldown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfProvision = NebulaAPI.Configurations.Configuration("options.role.marshallU.numOfProvision", (1, 15), 1);
    static public FloatConfiguration MarshallkillCooldown = NebulaAPI.Configurations.Configuration("options.role.marshalllU.KillCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public MarshallU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Marshall.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(MarshallImage, "role.marshallU.ability.provision");
    }

    static private readonly Virial.Media.Image MarshallImage = NebulaAPI.AddonAsset.GetResource("MarshallButton.png")!.AsImage(115f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        int leftprovision = NumOfProvision;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);
                var ProvisionButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    provisionCooldown, "Provision", MarshallImage, _ => playerTracker.CurrentTarget != null && leftprovision > 0);
                ProvisionButton.Visibility = (button) => !MyPlayer.IsDead && leftprovision > 0;
                ProvisionButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        target.RemoveModifier(Modifier.DeployedU.MyRole);
                        target?.AddModifier(Modifier.DeployedU.MyRole, assignType: RoleAssignType.WithoutRecording);

                        leftprovision--;
                        ProvisionButton.StartCoolDown();
                        ProvisionButton.UpdateUsesIcon(leftprovision.ToString());
                    }
                };

                ProvisionButton.ShowUsesIcon(3, leftprovision.ToString());
            }
        }
    }
}