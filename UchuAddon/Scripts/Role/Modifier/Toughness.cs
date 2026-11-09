using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Sample;

public class ToughnessU : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private ToughnessU() : base("toughnessU", "TUH", new(222, 223, 255)) 
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Guardian.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public ToughnessU MyRole = new ToughnessU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private bool hasGuarded = false;

        public Instance(GamePlayer player) : base(player)
        {
        }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (hasGuarded) return;
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
            if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;
            ev.Result = KillResult.ObviousGuard; 
            hasGuarded = true;
        }

        void RuntimeAssignable.OnActivated() { }

        /*void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if ((AmOwner || canSeeAllInfo) && !hasGuarded)
                name += " ♦".Color(new Color(222, 223, 255));
        }*/
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}