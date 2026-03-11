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

public class GloomU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private GloomU() : base("gloomU", "GLM", new(68, 148, 83), [], allocateToImpostor: false, allocateToNeutral: false)
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public GloomU MyRole = new GloomU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Gloom.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        IgnoreBlackoutVisionAbility? ignoreBlackoutAbility;
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => [ignoreBlackoutAbility];

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner)
                ignoreBlackoutAbility = new IgnoreBlackoutVisionAbility(MyPlayer);
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var ability = new GloomVisionAbility().Register(this);
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}