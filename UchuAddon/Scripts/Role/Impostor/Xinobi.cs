using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Rewired.Utils.Classes.Utility;
using System;
using System;
using System;
using System;
using System.Collections;
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
using UnityEngine.EventSystems;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

[NebulaRPCHolder]
public class XinobiU : DefinedRoleTemplate, DefinedRole,IAssignableDocument
{
    private XinobiU() : base("xinobiU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [KillCoolDown,XinobiVentCooldown, ClairvoyanceOption,
     new GroupConfiguration("options.role.xinobiU.group.vent", [ConnectVentsOption, VentConfiguration], GroupConfigurationColor.ImpostorRed)])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Xinobi.png")!.AsImage(115f);
        UchuGameAction.XinobiVentAction = new("xinobiU.vent", this, isPhysicalAction: true);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    static public IRelativeCooldownConfiguration KillCoolDown = NebulaAPI.Configurations.KillConfiguration("options.role.xinobiU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static public FloatConfiguration XinobiVentCooldown = NebulaAPI.Configurations.Configuration("options.role.xinobiU.xinobiUVentCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static public BoolConfiguration ClairvoyanceOption = NebulaAPI.Configurations.Configuration("options.role.spectre.clairvoyance", true);
    static public BoolConfiguration ConnectVentsOption = NebulaAPI.Configurations.Configuration("options.role.reaper.connectAllVents", true);
    static public IVentConfiguration VentConfiguration = NebulaAPI.Configurations.VentConfiguration("role.reaper.vent", false, null, -1, (0f, 60f, 2.5f), 10f, (0f, 30f, 2.5f), 20f);

    static public float KillCooldown => KillCoolDown.Cooldown;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Xinobi.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public XinobiU MyRole = new XinobiU();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(PossessImage, "role.xinobiU.ability.possess");
        yield return new(VentImage, "role.xinobiU.ability.vent");
    }
    static private readonly Virial.Media.Image PossessImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image ReleaseImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image VentImage = NebulaAPI.AddonAsset.GetResource("XinobiVentButton.png")!.AsImage(115f)!;


    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            new Scripts.Abilities.XinobiAbility(MyPlayer).Register(new FunctionalLifespan(() => !IsDeadObject && !IsUsurped));
        }
    }

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        bool RuntimeRole.HasVanillaKillButton => false;
        private bool isUsurped = false;
        void Usurp() => isUsurped = true;

        public bool EyesightIgnoreWalls { get; private set; } = ClairvoyanceOption;

        private AllVentConnectAbility? ventAbility;

        void IGameOperator.OnReleased()
        {
            AmongUsUtil.SetCamTarget(null);
            if (ventAbility != null)
            {
                ventAbility.AllVentConnects(false);
            }
        }

        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        public override void OnActivated()
        {
            new Scripts.Abilities.XinobiAbility(MyPlayer).Register(new FunctionalLifespan(() => !IsDeadObject && !isUsurped));
            if (AmOwner)
            {
                if (ConnectVentsOption)
                {
                    ventAbility = new AllVentConnectAbility().Register(this);
                    ventAbility.AllVentConnects(true);
                }
            }
        }

    }
}





/*public class NinjaU : DefinedSingleAbilityRoleTemplate<NinjaU.Ability>, DefinedRole
{
    public NinjaU() : base("ninjaU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam)
    {
    }

    public static FloatConfiguration PossessCooldown = NebulaAPI.Configurations.Configuration("options.role.ninjU.possessCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public NinjaU MyRole = new();
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.sampleU.sampleSkill", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image PossessImage = NebulaAPI.AddonAsset.GetResource("EpsilonScanButton.png")!.AsImage(115f)!;
        static private readonly Virial.Media.Image ReleaseImage = NebulaAPI.AddonAsset.GetResource("EpsilonCovertButton.png")!.AsImage(115f)!;
        bool Possess = false;
        bool RuntimeRole.CanUseVent => false;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var sprite = HudManager.Instance.UseButton.fastUseSettings[ImageNames.VentButton].Image;
                var posVent = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Vent, 0f, "Vent", new WrapSpriteLoader(() => sprite));
                posVent.Visibility = (button) => !MyPlayer.IsDead && Possess;
                posVent.Availability = (button) => !MyPlayer.IsDead && MyPlayer.IsDead;

                Virial.Components.ObjectTracker<Console> consoleTracker = new ObjectTrackerUnityImpl<Console, Console>(MyPlayer.VanillaPlayer, AmongUsUtil.VanillaKillDistance, () => ShipStatus.Instance.AllConsoles, c => !ModSingleton<BalloonManager>.Instance.ConsoleHasTrap(c), c => !c.TryCast<VentCleaningConsole>() && !c.TryCast<AutoTaskConsole>() && !c.TryCast<StoreArmsTaskConsole>(), c => c, c => [c.transform.position], c => c.Image, Color.red,true, false).Register(this);
                var possessButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,10f,  "balloon", PossessImage, _ => consoleTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                possessButton.Visibility = (button) => !MyPlayer.IsDead && !Possess;
                possessButton.OnClick = (button) =>
                {
                    Possess = true;
                };
                var breakButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "Break", ReleaseImage);
                breakButton.Visibility = (button) => !MyPlayer.IsDead && Possess;
                breakButton.OnClick = (button) =>
                {
                    Possess = false;
                };
            }
        }


       void Update(GameUpdateEvent ev)
        {
            if (!Possess) return;
            MyPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.1f, false, 1);
            MyPlayer.GainSpeedAttribute(0f, 0.1f, false, 1);
        }

        [OnlyMyPlayer]
        void name(PlayerDecorateNameEvent ev)
        {
            if (!Possess) return;
            ev.Name = "";
        }
    }
}*/