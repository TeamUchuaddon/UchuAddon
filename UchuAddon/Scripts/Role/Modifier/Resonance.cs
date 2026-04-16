using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Modifier;

public class ResonanceU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private ResonanceU() : base("resonanceU", "RSO", NebulaTeams.ImpostorTeam.Color, [], allocateToCrewmate: false, allocateToNeutral: false)
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

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
            if (ev.Killer.IsTrueCrewmate || ev.Killer == MyPlayer) return;
            AmongUsUtil.PlayFlash(Color.red);
        }


        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}