using Nebula.Game.Statistics;
using Nebula.Modules;
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

namespace Hori.Scripts.Role.Modifier;

public class WatcherU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private WatcherU() : base("watcherU", "WTC", new(87, 38, 166))
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Watcher.png")!.AsImage(115f);
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Watching.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public WatcherU MyRole = new WatcherU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
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
                var harmony = new HarmonyLib.Harmony("com.hori.watchingU.anonymousvote");
                AnonymousVoteBypass.Patch(harmony);
            }
        }

        /*void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += " ⊙".Color(MyRole.RoleColor.ToUnityColor());
        }*/
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}