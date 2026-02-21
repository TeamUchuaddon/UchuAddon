using Hori.Core;
using Hori.Scripts.Role.Neutral;
using Hori.Scripts.Role.Sample;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Utilities;
using System;
using System.Linq;
using Virial.Attributes;
using Virial.Events.Player;
using Image = Virial.Media.Image;
public class HexU : DefinedAllocatableModifierTemplate, DefinedModifier, HasCitation
{
    private HexU() : base("hexU", "HEX", new(236, 0, 140), [])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static public HexU MyRole = new HexU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Hex.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {

            }
        }

        [Local, OnlyMyPlayer]
        void E(PlayerExiledEvent ev)
        {
            ExtraExileRoleSystem.MarkExtraVictim(MyPlayer);
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}