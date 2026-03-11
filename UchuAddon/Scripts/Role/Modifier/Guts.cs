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

public class GutsU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, HasCitation
{
    private GutsU() : base("gutsU", "GUT", new(255, 165, 0), [])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static public GutsU MyRole = new GutsU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Guts.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private bool hasRefused = false;
        private bool shouldRevive = false;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
            }
        }

        void OnExiled(PlayerExiledEvent ev)
        {
            if (ev.Player != MyPlayer) return;
            if (hasRefused) return;

            // 追放を拒否
            hasRefused = true;
            shouldRevive = true;
        }

        [OnlyMyPlayer]
        void MyRevive(PlayerExiledEvent ev)
        {
            if (ev.Player != MyPlayer) return;
            if (!shouldRevive) return;

            // プレイヤーを復活
            MyPlayer.Revive(MyPlayer, MyPlayer.TruePosition, true, true);
            shouldRevive = false;
        } 
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}
