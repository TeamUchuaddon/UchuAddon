using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
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
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Text;
using static Il2CppSystem.Net.MonoChunkParser;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Modifier.Bloody;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
namespace Hori.Scripts.Role.Modifier;

public class DrunkU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private DrunkU() : base("drunkU", "DRK", new(247, 195, 148), [MeetingDrunkShuffle])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    public static BoolConfiguration MeetingDrunkShuffle = NebulaAPI.Configurations.Configuration("options.role.drunkU.meetingDrunkShuffle", true);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Drunk.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public DrunkU MyRole = new DrunkU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.drunkU.blurb");
        private SpeedModulator? speedMod;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            ApplyDrunkEffect();
        }

        void IGameOperator.OnReleased()
        {
            MyPlayer.RemoveAttribute(PlayerAttributes.Drunk);
        }

        [OnlyMyPlayer]
        void Shuffle(MeetingEndEvent ev)
        {
            if (!MeetingDrunkShuffle) return;
            ApplyDrunkEffect();
        }

        private static Vector4 RandomVec()
        {
            int step = UnityEngine.Random.Range(0, 4);
            float angle = step * 90f;

            return step switch
            {
                0 => new Vector4(0f, 1f, -1f, 0f),
                1 => new Vector4(-1f, 0f, 0f, -1f),
                _ => new Vector4(0f, -1f, 1f, 0f),
            };
        }

        private void ApplyDrunkEffect()
        {
            MyPlayer.RemoveAttribute(PlayerAttributes.Drunk);

            speedMod = new SpeedModulator(1f, RandomVec(), true, float.MaxValue, true, 0, "drunkU.effect");

            PlayerModInfo.RpcAttrModulator.Invoke((MyPlayer.PlayerId, speedMod, true));
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}