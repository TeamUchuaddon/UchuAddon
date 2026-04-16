using Hori.Scripts.Role.Crewmate;
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

public class MatchU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private MatchU() : base("matchU", "MCH", new(219, 203, 55), [LightRatio])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private FloatConfiguration LightRatio = NebulaAPI.Configurations.Configuration("options.role.matchU.lightRatio", (1f, 5f, 1f), 1.5f, FloatConfigurationDecorator.Ratio);

    static public MatchU MyRole = new MatchU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Lighter.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

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
                using (RPCRouter.CreateSection("LighterULight"))
                {
                    MyPlayer.GainAttribute(PlayerAttributes.Eyesight, float.MaxValue, LightRatio, true, 100, "matchU.light");
                }
            }
        }

        void IGameOperator.OnReleased()
        {
            MyPlayer.RemoveAttributeByTag("matchU.light");
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}