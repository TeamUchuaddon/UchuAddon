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
using Virial.Attributes;
using Virial.Events.Player;
using Hori.Core;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules.ScriptComponents;
using System.Collections;
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

public class HorseU : DefinedAllocatableModifierTemplate, DefinedModifier, DefinedAllocatableModifier
{
    private HorseU() : base("horseU", "HRS", new(22, 195, 45), [])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Horse.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public HorseU MyRole = new HorseU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.horseU.blurb");
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
            }
        }
        void IGameOperator.OnReleased()
        {
            if (AmOwner)
            {
                MyPlayer.ChangeBodyType(PlayerBodyTypes.Normal);
                MyPlayer.VanillaPlayer.cosmetics.SetBodyCosmeticsVisible(true);
                MyPlayer.VanillaPlayer.cosmetics.SetHatVisorVisible(true);
            }
        }

        void OnUpdate(GameUpdateEvent ev)
        {
            if (MyPlayer.VanillaPlayer.MyPhysics.bodyType == PlayerBodyTypes.Normal)
            {
                MyPlayer.VanillaPlayer.NetTransform.Halt();
                MyPlayer.ChangeBodyType(PlayerBodyTypes.Horse);
            }
            if (MyPlayer.VanillaPlayer.MyPhysics.bodyType == PlayerBodyTypes.Horse && !MyPlayer.IsDead && !MyPlayer.Logic.InVent && !MyPlayer.IsDived)
            {
                MyPlayer.VanillaPlayer.cosmetics.SetBodyCosmeticsVisible(false);
                MyPlayer.VanillaPlayer.cosmetics.SetHatVisorVisible(true);
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}