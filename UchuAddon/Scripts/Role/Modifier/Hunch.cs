using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
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
using static UnityEngine.RectTransform;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Sample;

public class FlashAbility : IGameOperator
{
    public Color FlashColor { get; set; } = Color.white;

    void OnPlayerMurdered(PlayerMurderedEvent ev)
    {
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (ev.Player.AmOwner) return;
        if (!ev.Dead.HasAttribute(PlayerAttributes.BuskerEffect))
        {
            AmongUsUtil.PlayFlash(FlashColor);
        }
    }
}

    public class DetectedFlashAbility : IGameOperator
{
    public Color FlashColor { get; set; } = Color.white;

    void OnPlayerMurdered(PlayerMurderedEvent ev)
    {
        var myPos = GamePlayer.LocalPlayer.VanillaPlayer.GetTruePosition();
        float maxDis = HunchU.DetectedRangeOption;
        byte currentHolding = GamePlayer. LocalPlayer.HoldingDeadBody?.Player.PlayerId ?? byte.MaxValue;
        foreach (var deadbody in Helpers.AllDeadBodies())
        {
            if (MeetingHud.Instance || ExileController.Instance) return;
            if (currentHolding == deadbody.ParentId) continue;
            if ((deadbody.TruePosition - myPos).magnitude > maxDis) continue;
            if (ev.Player.AmOwner) return;
            if (!ev.Dead.HasAttribute(PlayerAttributes.BuskerEffect))
            {
                AmongUsUtil.PlayFlash(FlashColor);
            }
        }
    }
}

public class HunchU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, HasCitation
{
    private HunchU() : base("hunchU", "HNC", new(73, 166, 104), [DetectedOption, DetectedRangeOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    static private readonly BoolConfiguration DetectedOption = NebulaAPI.Configurations.Configuration("options.role.hunchU.detected", false);
    static public readonly FloatConfiguration DetectedRangeOption = NebulaAPI.Configurations.Configuration("options.role.hunchU.detectedRange", (2.5f, 30f, 2.5f), 7.5f, FloatConfigurationDecorator.Ratio, () => DetectedOption);

    static public HunchU MyRole = new HunchU();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Hunch.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {

        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private SpriteRenderer? fullScreen;
        public Instance(GamePlayer player) : base(player)
        {
        }
        bool isNearBodyLastFrame = false;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner && DetectedOption)
            {
              　new DetectedFlashAbility() { FlashColor = MyRole.UnityColor }.Register(new FunctionalLifespan(() => !this.IsDeadObject));

            }
            else
            {
                new FlashAbility() { FlashColor = MyRole.UnityColor }.Register(new FunctionalLifespan(() => !this.IsDeadObject));
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}