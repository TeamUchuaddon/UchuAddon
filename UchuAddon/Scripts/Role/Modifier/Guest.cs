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

public class GuestU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private GuestU() : base("guestU", "GST", new(66, 207, 144))
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    static public GuestU MyRole = new GuestU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Guest.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        MeetingButtonBlockAbility? meetingButtonBlockAbility;
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => [meetingButtonBlockAbility];

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner)
                meetingButtonBlockAbility = new MeetingButtonBlockAbility(MyPlayer);
        }

        void RuntimeAssignable.OnActivated()
        {

        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        void GameUpdate(GameUpdateEvent ev)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");

                var dir = new Vector2(h, v);

                if (dir.sqrMagnitude > 0.01f)
                {
                    player.MyPhysics.body.velocity = dir.normalized * player.MyPhysics.Speed;
                }
                else
                {
                    player.MyPhysics.body.velocity = Vector2.zero;
                }
            }
        }
    }
}