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
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
using static Nebula.Roles.Modifier.Bloody;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Modifier;

public class HighLowU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private HighLowU() : base("HighLowU", "HIG", new(194, 198, 110), [Vote, YesVote, Random])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/HighLow.png")!.AsImage(115f);
    }

    static private IntegerConfiguration Vote = NebulaAPI.Configurations.Configuration("options.role.HighLowU.Vote", (0, 5), 0);
    static private IntegerConfiguration Random = NebulaAPI.Configurations.Configuration("options.role.HighLowU.random", (0, 90), 50);
    static private IntegerConfiguration YesVote = NebulaAPI.Configurations.Configuration("options.role.HighLowU.YesVote", (1, 5), 1);
    static public HighLowU MyRole = new HighLowU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/HighLow.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        int HoodooVote = Vote;
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
        }
        [Local]
        void OnCastVoteLocal(PlayerVoteCastLocalEvent ev)
        {
            if (AmOwner)
            {
                int roll = UnityEngine.Random.Range(0, 101);
                if (roll <= Random)
                {
                    HoodooVote = YesVote;
                }
                else
                {
                    HoodooVote = Vote;
                }
                ev.Vote = HoodooVote;
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}