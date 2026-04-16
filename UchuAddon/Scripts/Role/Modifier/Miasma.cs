using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Modifier;

public class MiasmaU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private MiasmaU() : base("miasmaU", "MSA", new(135, 135, 135), [KillerLight])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private FloatConfiguration KillerLight = NebulaAPI.Configurations.Configuration("options.role.miasmaU.killerLight", (0.125f, 1f, 0.125f), 0.25f, FloatConfigurationDecorator.Ratio);

    static public MiasmaU MyRole = new MiasmaU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Miasma.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {

        }

        [OnlyMyPlayer]
        void MiasmaSkill(PlayerCheckKilledEvent ev)
        {
            RpcMiasmaLightUchu.Invoke(ev.Killer);
        }


        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        static public RemoteProcess<GamePlayer> RpcMiasmaLightUchu = new("RpcMiasmaLight_Uchu", (player, _) =>
        {
            if (player == null) return;
            if (GamePlayer.LocalPlayer !=player) return;

            if (ShipStatus.Instance)
                ShipStatus.Instance.MaxLightRadius = KillerLight;
        });
    }
}