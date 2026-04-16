using Hori.Core;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Image = Virial.Media.Image;
using Hori.Scripts.Abilities;
using Nebula.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Game.Statistics;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Behavior;
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
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using static Nebula.Roles.Modifier.Bloody;

namespace Hori.Scripts.Role.Modifier;

public class ExpressU : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private ExpressU() : base("expressU", "EXP", new(121, 126, 208), [ExpressSpeed])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Express.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    static private readonly FloatConfiguration ExpressSpeed = NebulaAPI.Configurations.Configuration("options.ExpressUSpeed", (0.5f, 3f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Express.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public ExpressU MyRole = new ExpressU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() 
        {
            MyPlayer.GainSpeedAttribute(ExpressSpeed, 9999999999999f, true, 0, "ExpressU:express");
        }

        void IGameOperator.OnReleased()
        {
            MyPlayer.RemoveAttributeByTag("ExpressU:express");
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        /*void Update(GameUpdateEvent ev)
        {
            MyPlayer.GainSpeedAttribute(ExpressSpeed, 0.1f, false, 1);
        }*/

    }
}