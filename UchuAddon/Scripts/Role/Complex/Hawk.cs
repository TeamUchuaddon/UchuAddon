using Hori.Core;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Complex;

public class HawkU : DefinedSingleAbilityRoleTemplate<HawkU.Ability>, DefinedRole, HasCitation
{
    public HawkU() : base("evilHawkU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam,[HawkeyeCoolDownOption, HawkeyeDurationOption, HawkeyeRatioOption, HawkeyeStop])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Hawk.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration HawkeyeCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeCoolDown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration HawkeyeDurationOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeDuration", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration HawkeyeRatioOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeRatio", (0.25f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private BoolConfiguration HawkeyeStop = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeStop", false);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.SuperNewRoles; } }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Hawk.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public HawkU MyRole = new();
    static private readonly GameStatsEntry StatsHawkeye = NebulaAPI.CreateStatsEntry("stats.hawkU.hawkeyeSKill", GameStatsCategory.Roles, MyRole);
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueMapAbility;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image HawkImage = NebulaAPI.AddonAsset.GetResource("HawkButton.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public bool EyesightIgnoreWalls { get; private set; } = false;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var hawkeyeButton = NebulaAPI.Modules.AbilityButton(this).BindKey(Virial.Compat.VirtualKeyInput.Ability).SetImage(HawkImage).SetLabel("hawkeye").SetAsUsurpableButton(this);
                hawkeyeButton.Availability = (button) => MyPlayer.CanMove;
                hawkeyeButton.Visibility = (button) => !MyPlayer.IsDead;
                hawkeyeButton.OnClick = (button) => button.StartEffect();
                hawkeyeButton.OnEffectStart = (button) =>
                {
                    using (RPCRouter.CreateSection("HawkUHawkeye"))
                    {
                        StatsHawkeye.Progress();
                        EyesightIgnoreWalls = true;
                        MyPlayer.GainAttribute(PlayerAttributes.ScreenSize, HawkeyeDurationOption, HawkeyeRatioOption, false, 100);
                        MyPlayer.GainAttribute(PlayerAttributes.Eyesight, HawkeyeDurationOption, 100, false, 100);
                        if (HawkeyeStop)
                        {
                            MyPlayer.GainSpeedAttribute(0f, HawkeyeDurationOption, false, 100);
                        }
                    }
                };
                hawkeyeButton.OnEffectEnd = (button) =>
                {
                    EyesightIgnoreWalls = false;
                    hawkeyeButton.StartCoolDown();
                };
                hawkeyeButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, HawkeyeCoolDownOption).SetAsAbilityTimer().Start();
                hawkeyeButton.EffectTimer = NebulaAPI.Modules.Timer(this, HawkeyeDurationOption);
            }
        }
    }
}

public class NiceHawkU : DefinedSingleAbilityRoleTemplate<NiceHawkU.Ability>, DefinedRole, HasCitation
{
    public NiceHawkU() : base("niceHawkU", new(255, 255, 112), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [HawkeyeCoolDownOption, HawkeyeDurationOption, HawkeyeRatioOption, HawkeyeStop])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/HawkNice.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration HawkeyeCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeCoolDown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration HawkeyeDurationOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeDuration", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration HawkeyeRatioOption = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeRatio", (0.25f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private BoolConfiguration HawkeyeStop = NebulaAPI.Configurations.Configuration("options.role.hawkU.hawkeyeStop", false);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.SuperNewRoles; } }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Hawk.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public NiceHawkU MyRole = new();
    static private readonly GameStatsEntry StatsHawkeyeNice = NebulaAPI.CreateStatsEntry("stats.NicehawkU.hawkeyeSKill", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image HawkImage = NebulaAPI.AddonAsset.GetResource("HawkButtonNice.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public bool EyesightIgnoreWalls { get; private set; } = false;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var hawkeyeButton = NebulaAPI.Modules.AbilityButton(this).BindKey(Virial.Compat.VirtualKeyInput.Ability).SetImage(HawkImage).SetLabel("hawkeye").SetAsUsurpableButton(this);
                hawkeyeButton.Availability = (button) => MyPlayer.CanMove;
                hawkeyeButton.Visibility = (button) => !MyPlayer.IsDead;
                hawkeyeButton.OnClick = (button) => button.StartEffect();
                hawkeyeButton.OnEffectStart = (button) =>
                {
                    using (RPCRouter.CreateSection("NiceHawkUHawkeye"))
                    {
                        StatsHawkeyeNice.Progress();
                        EyesightIgnoreWalls = true;
                        MyPlayer.GainAttribute(PlayerAttributes.ScreenSize, HawkeyeDurationOption, HawkeyeRatioOption, false, 100);
                        MyPlayer.GainAttribute(PlayerAttributes.Eyesight, HawkeyeDurationOption, 100, false, 100);
                        if (HawkeyeStop)
                        {
                            MyPlayer.GainSpeedAttribute(0f, HawkeyeDurationOption, false, 100);
                        }
                    }
                };
                hawkeyeButton.OnEffectEnd = (button) =>
                {
                    EyesightIgnoreWalls = false;
                    hawkeyeButton.StartCoolDown();
                };
                hawkeyeButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, HawkeyeCoolDownOption).SetAsAbilityTimer().Start();
                hawkeyeButton.EffectTimer = NebulaAPI.Modules.Timer(this, HawkeyeDurationOption);
            }
        }
    }
}