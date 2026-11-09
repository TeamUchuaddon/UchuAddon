using Hori.Scripts.Abilities;
using Hori.Scripts.Role.Crewmate;
using Nebula.Modules;
using Nebula.Player;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

public class DeployedU : DefinedModifierTemplate, DefinedModifier
{
    static public DeployedU MyRole = new DeployedU();
    private DeployedU() : base("deployedU", MarshallU.MyRole.Color, withConfigurationHolder: false) 
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Deployed.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        static private readonly Image KillButtonImage = NebulaAPI.AddonAsset.GetResource("VanillaKillButton.png")!.AsImage(100f)!;
        int leftkill = 1;

        public Instance(GamePlayer player) : base(player)
        {

        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MarshallU.MyRole.Color, 0f, 0.25f, 0.45f);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                var KillButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.None,
                    MarshallU.MarshallkillCooldown, "Kill", KillButtonImage, _ => playerTracker.CurrentTarget != null && leftkill > 0);
                KillButton.Visibility = (button) => !MyPlayer.IsDead && leftkill > 0;
                KillButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target == null) return;

                    leftkill--;
                    KillButton.StartCoolDown();
                    MyPlayer.MurderPlayer(target!, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    KillButton.HideUsesIcon();

                    AmongUsUtil.PlayCustomFlash(MarshallU.MyRole.Color, 0f, 0.25f, 0.45f);
                };

                KillButton.ShowUsesIcon(3, "1");
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}