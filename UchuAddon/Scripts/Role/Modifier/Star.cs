using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.Internal;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Modifier;

public class StarU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, HasCitation
{
    private StarU() : base("StarU", "STA", new(255, 255, 50), [RainbowStar, RainbowStarOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Star.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private BoolConfiguration RainbowStar = NebulaAPI.Configurations.Configuration("options.StarU.RainbowStar", false);
    static private IntegerConfiguration RainbowStarOption = NebulaAPI.Configurations.Configuration("options.role.StarU.RainbowStarOption", (5, 15), 8, () => RainbowStar);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Star.png")!.AsImage(100f)!;
    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;
    Image? DefinedAssignable.IconImage => IconImage;
    static public StarU MyRole = new StarU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        private bool IsGameEnding = false;
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var roleName = MyPlayer.Role.DisplayName;
            }
        }

        float colorTimer = 0f;

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
        [OnlyMyPlayer]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            colorTimer += Time.deltaTime;
            if (RainbowStar)
            {
                float hue = (Time.time * (RainbowStarOption / 10f)) % 1f;
                UnityEngine.Color rainbowColor = UnityEngine.Color.HSVToRGB(hue, 1f, 1f);
                ev.Color = new Virial.Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, rainbowColor.a);
            }
            else
            {
                ev.Color = new Virial.Color(1f, 1f, 0.1f, 1f);
            }
        }
    }
}