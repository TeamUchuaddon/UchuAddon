using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Il2CppSystem.Uri;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Modifier;

public class StaticU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private StaticU() : base("staticU", "TIC", new(228, 255, 25), [CanSeeAllOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    public static BoolConfiguration CanSeeAllOption = NebulaAPI.Configurations.Configuration("options.role.staticU.canSeeAll", true);

    static public StaticU MyRole = new StaticU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Static.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => CanSeeAllOption && (NebulaGameManager.Instance?.CanSeeAllInfo ?? false);
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                FakeInformation.RpcFakeAdmin.Invoke((FakeInformation.Instance!.CurrentAdmin.Players, float.MaxValue));
                FakeInformation.RpcFakeVitals.Invoke((FakeInformation.Instance!.CurrentVitals.Players, float.MaxValue));
            }
        }
        void IGameOperator.OnReleased()
        {
            if (AmOwner)
            {
                FakeInformation.RpcFakeAdmin.Invoke(([], 0f));
                FakeInformation.RpcFakeVitals.Invoke(([], 0f));
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}