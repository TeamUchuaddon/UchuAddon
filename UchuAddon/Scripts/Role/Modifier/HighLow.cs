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
    private HighLowU() : base("HighLowU", "HIG", new(194, 198, 110), [Random, SuccessVote, FailureVote])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/HighLow.png")!.AsImage(115f);
    }

    static private FloatConfiguration Random = NebulaAPI.Configurations.Configuration("options.role.HighLowU.random", (0f, 100f, 10f), 50f, FloatConfigurationDecorator.Percentage);
    static private IntegerConfiguration FailureVote = NebulaAPI.Configurations.Configuration("options.role.HighLowU.FailureVote", (0, 6), 0);
    static private IntegerConfiguration SuccessVote = NebulaAPI.Configurations.Configuration("options.role.HighLowU.SuccessVote", (1, 7), 2);
    static public HighLowU MyRole = new HighLowU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/HighLow.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        int MeetingVote = FailureVote;
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
                float roll = UnityEngine.Random.Range(0f, 100f);
                if (roll <= Random)
                {
                    MeetingVote = SuccessVote;
                }
                else
                {
                    MeetingVote = FailureVote;
                }
                ev.Vote = MeetingVote;
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}