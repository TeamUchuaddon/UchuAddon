using Nebula.Modules.Cosmetics;
using Nebula.Roles.Crewmate;
using Rewired.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Virial.Color;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Modifier;

public class ResonanceU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private ResonanceU() : base("resonanceU", "RSO", NebulaTeams.ImpostorTeam.Color, [IsColorFlash], allocateToCrewmate: false, allocateToNeutral: false)
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Resonance.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private BoolConfiguration IsColorFlash = NebulaAPI.Configurations.Configuration("options.role.resonanceU.isColorFlash", false);

    static public ResonanceU MyRole = new ResonanceU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Resonance.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() { }

        void Flash(PlayerCheckKilledEvent ev)
        {
            if (!ev.Killer.IsImpostor || ev.Killer == MyPlayer) return;

            if (AmOwner)
            {
                if (IsColorFlash)
                {
                    AmongUsUtil.PlayFlash(DynamicPalette.PlayerColors[ev.Player.PlayerId]);
                }
                else
                {
                    AmongUsUtil.PlayFlash(Color.Red);
                }
            }
        }


        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}