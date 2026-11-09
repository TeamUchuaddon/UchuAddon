using Sentry.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Ghost.Impostor;

public class BuzzerU : DefinedGhostRoleTemplate, DefinedGhostRole, IAssignableDocument
{
    public BuzzerU() : base("buzzerU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, [BuzzerCooldown,NumOfBuzzer])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    string ICodeName.CodeName => "BUZ";

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private readonly FloatConfiguration BuzzerCooldown = NebulaAPI.Configurations.Configuration("options.role.buzzerU.buzzerCooldown", (5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private readonly IntegerConfiguration NumOfBuzzer = NebulaAPI.Configurations.Configuration("options.role.buzzer.numOfBuzzer", (1, 12), 3);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Buzzer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public readonly BuzzerU MyRole = new();

    static private readonly Virial.Media.Image BuzzerImage = NebulaAPI.AddonAsset.GetResource("BuzzerButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(BuzzerImage, "role.buzzerU.ability.buzzer");
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;
        int left = NumOfBuzzer;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var buzzerButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    BuzzerCooldown, "buzzerU.buzzer", BuzzerImage, null, _ => left > 0, true);
                buzzerButton.ShowUsesIcon(0, left.ToString());
                buzzerButton.OnClick = (button) =>
                {
                    RpcBuzzerUchu.Invoke(MyPlayer.TruePosition);

                    left--;
                    buzzerButton.UpdateUsesIcon(left.ToString());
                    buzzerButton.StartCoolDown();
                };
            }
        }

        static public readonly RemoteProcess<Vector2> RpcBuzzerUchu = new("RpcBuzzer_Uchu", (pos, _) =>
        {
            if (GamePlayer.LocalPlayer == null) return;
            if (!GamePlayer.LocalPlayer.IsImpostor) return;
            AmongUsUtil.Ping([pos], false, true);
        });
      
    }
}