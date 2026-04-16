using Nebula.Roles.Ghost.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Ghost.Crewmate;

public class RestoreU : DefinedGhostRoleTemplate, DefinedGhostRole
{
    public RestoreU() : base("restoreU", new(29, 14, 158), RoleCategory.CrewmateRole, [NumOfRewind])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    string ICodeName.CodeName => "RST";

    static public readonly RestoreU MyRole = new();

    public static IntegerConfiguration NumOfRewind = NebulaAPI.Configurations.Configuration("options.role.restoreU.numOfRewind", (1, 9), 3);

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Restore.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                bool isUsed = false;
                var fixButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    10f, "fix", buttonSprite, null, _ => !isUsed, true);
                fixButton.OnClick = (button) =>
                {
                    SabotageFixAbility.SabotageFix();
                    MyPlayer.Tasks.RewindTasks(NumOfRewind);

                    isUsed = true;
                };
                fixButton.ShowUsesIcon(3, "1");
            }
        }
    }
}