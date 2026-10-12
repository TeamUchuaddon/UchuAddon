using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.Impostor.Marionette;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Modifier;

public class JizoU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private JizoU() : base("jizoU", "JIZ", new(133, 133, 133), [])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Jizo.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static public JizoU MyRole = new JizoU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Jizo.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private VVector2 LastPosition;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {

            }
        }

        [Local]
        public void JizoPosition(MeetingEvent ev)
        {
            LastPosition = MyPlayer.TruePosition;
        }

        [Local]
        public void JizoSystem(TaskPhaseRestartEvent ev)
        {
            JizoSwap.Invoke((MyPlayer.PlayerId, LastPosition));
        }

        private RemoteProcess<(byte playerId, VVector2 pos)> JizoSwap = new("DecoySwap",
        (message, _) =>
        {
            var jizo = Helpers.GetPlayer(message.playerId);
            if (jizo == null) return;

            jizo.transform.localPosition = new (message.pos.x, message.pos.y);
        });

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}