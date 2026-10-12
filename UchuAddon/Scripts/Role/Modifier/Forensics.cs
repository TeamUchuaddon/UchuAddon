using Hori.Core;
using Hori.Core;
using Hori.Scripts.Role.Neutral;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Utilities;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Events.Game;
using System.Linq;
using System;
using UnityEngine;
using Virial.Game;
using Virial.Assignable;
using Virial;
using Virial.Text;
using Image = Virial.Media.Image;

public class ForensicsU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private ForensicsU() : base("forensicsU", "FRS", new(12, 90, 147), [])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Forensics.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    static public ForensicsU MyRole = new ForensicsU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Forensics.png")!.AsImage(100f)!;
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
            }
        }

        void ShowMessage(FixExileTextEvent ev)
        {
            if (ev.Exiled.Count == 0 || MyPlayer.IsDead) return;

            foreach (var player in ev.Exiled)
            {
                if (player.IsImpostor)
                {
                    ev.AddText(NebulaAPI.Language.Translate("role.forensicsU.textImpostor").Replace("%PLAYER%", player.ColoredName));
                }
                else
                {
                    ev.AddText(NebulaAPI.Language.Translate("role.forensicsU.textNotImpostor").Replace("%PLAYER%", player.ColoredName));
                }
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}