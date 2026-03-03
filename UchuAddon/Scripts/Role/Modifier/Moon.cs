using Hori.Scripts.Abilities;
using Nebula.Modules;
using Nebula.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Game.Statistics;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Hori.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules.ScriptComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using static Nebula.Roles.Modifier.Bloody;

namespace Hori.Scripts.Role.Modifier;

public class MoonU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier,HasCitation
{
    private MoonU() : base("moonU", "STR", new(255, 255, 20))
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Moon.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostK;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Moon.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public MoonU MyRole = new MoonU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.moonU.blurb");
        IgnoreBlackoutVisionAbility? ignoreBlackoutAbility;
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => [ignoreBlackoutAbility];

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner)
                ignoreBlackoutAbility = new IgnoreBlackoutVisionAbility(MyPlayer);
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            { 
                
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}