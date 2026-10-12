using Hori.Core;
using Hori.Scripts.Role.Neutral;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Utilities;
using Virial.Attributes;
using Virial.Events.Player;
using System.Linq;
using System;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;

public class TestamentU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private TestamentU() : base("testamentU", "TMT", new(212, 85, 180), [])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Testament.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static public TestamentU MyRole = new TestamentU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Testament.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private bool hasTestament = false;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
            }
        }

        void OnExiled(PlayerExiledEvent ev)
        {
            if (ev.Player != MyPlayer) return;
            if (hasTestament) return;

            hasTestament = true;
        }

        void ShowMessage(FixExileTextEvent ev)
        {
            if (ev.Exiled.Count == 0 || MyPlayer.IsDead) return;
            if (!ev.Exiled.Contains(MyPlayer)) return; // 自分が追放されていなければスルー

            ev.AddText(NebulaAPI.Language.Translate("role.testamentU.logText")
                .Replace("%PLAYER%", MyPlayer.ColoredName)
                .Replace("%ROLE%", MyPlayer.Role.DisplayColoredName));
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}