using Hori.Core;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate
{
    public class SlimeU : DefinedSingleAbilityRoleTemplate<SlimeU.Ability>, DefinedRole,IAssignableDocument
    {
        public SlimeU() : base("slimeU",new(36, 255, 12),RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam,new[]
        {
            new GroupConfiguration("options.group.slimejumbo", new[] { JumboCoolDown, JumboDuration, JumboSize, JumboVision, JumboScreen }, new UnityEngine.Color(0.3176f, 0.8902f, 0.1921f)),
            new GroupConfiguration("options.group.slimemini", new[] { MiniCoolDown, MiniDuration, MiniSize, MiniScreen, MiniSpeed }, new UnityEngine.Color(0.1921f, 0.8902f, 0.4824f))
        })
        {
            ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
            base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Slime.png")!.AsImage(115f);
        }
        static private FloatConfiguration JumboCoolDown = NebulaAPI.Configurations.Configuration("options.role.slimeU.JumboCoolDown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
        static private FloatConfiguration JumboDuration = NebulaAPI.Configurations.Configuration("options.role.slimeU.JumboDuration", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
        static private FloatConfiguration JumboSize = NebulaAPI.Configurations.Configuration("options.role.slimeU.JumboSize", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Ratio);
        static private FloatConfiguration JumboVision = NebulaAPI.Configurations.Configuration("options.role.slimeU.JumboVision", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Ratio);
        static private FloatConfiguration JumboScreen = NebulaAPI.Configurations.Configuration("options.role.slimeU.JumboScreen", (0f, 60f, 2.5f), 2f, FloatConfigurationDecorator.Ratio);
        static private FloatConfiguration MiniCoolDown = NebulaAPI.Configurations.Configuration("options.role.slimeU.MiniCoolDown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
        static private FloatConfiguration MiniDuration = NebulaAPI.Configurations.Configuration("options.role.slimeU.MiniDuration", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
        static private FloatConfiguration MiniSize = NebulaAPI.Configurations.Configuration("options.role.slimeU.MiniSize", (0f, 1f, 0.1f), 0.1f, FloatConfigurationDecorator.Ratio);
        static private FloatConfiguration MiniScreen = NebulaAPI.Configurations.Configuration("options.role.slimeU.MiniScreen", (0.125f, 1f, 0.125f), 0.5f, FloatConfigurationDecorator.Ratio);
        static private FloatConfiguration MiniSpeed = NebulaAPI.Configurations.Configuration("options.role.slimeU.MiniSpeed", (0f, 60f, 0.5f), 1.5f, FloatConfigurationDecorator.Ratio);
        AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

        public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

        static private Virial.Media.Image JumboImage = NebulaAPI.AddonAsset.GetResource("SlimeJumboButton.png")!.AsImage(115f)!;
        static private Virial.Media.Image MiniImage = NebulaAPI.AddonAsset.GetResource("SlimeMiniButton.png")!.AsImage(115f)!;
        static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Slime.png")!.AsImage(100f)!;
        Image? DefinedAssignable.IconImage => IconImage;
        static public SlimeU MyRole = new();
        static private readonly GameStatsEntry StatsSlime = NebulaAPI.CreateStatsEntry("stats.slimeU.SlimeSkill", GameStatsCategory.Roles, MyRole);

        bool IAssignableDocument.HasTips => true;
        bool IAssignableDocument.HasAbility => true;
        IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
        {
            yield return new(JumboImage, "role.slimeU.ability.jumbo");
            yield return new(MiniImage, "role.slimeU.ability.mini");
        }
        public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
        {
            int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

            public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
            {
                if (AmOwner)
                {
                    var JumboButton = NebulaAPI.Modules.AbilityButton(this,MyPlayer,Virial.Compat.VirtualKeyInput.Ability,"slimeU.jumbo",JumboCoolDown,"Jumbo",JumboImage);
                    var MiniButton = NebulaAPI.Modules.AbilityButton(this,MyPlayer,Virial.Compat.VirtualKeyInput.SecondaryAbility,"slimeU.mini",MiniCoolDown,"Mini",MiniImage);
                    JumboButton!.OnClick = (button) =>
                    {
                        button.StartEffect();
                    };
                    JumboButton.OnEffectStart = (button) =>
                    {
                        MiniButton!.StartCoolDown();
                        MyPlayer.GainAttribute(PlayerAttributes.Eyesight, JumboDuration, JumboVision, false, 0, "jumbo::vision");
                        (MiniButton.CoolDownTimer as GameTimer)?.Pause();
                        MyPlayer.GainAttribute(PlayerAttributes.ScreenSize, JumboDuration, JumboScreen, false, 1, "jumbo::screen");
                        MyPlayer.GainSizeAttribute(new Vector2(JumboSize, JumboSize),JumboDuration,true,1,"slime:jumbo");
                        StatsSlime.Progress();
                    };
                    JumboButton.OnEffectEnd = (button) =>
                    {
                        MiniButton!.StartCoolDown();
                        (MiniButton.CoolDownTimer as GameTimer)?.Resume();
                        button.StartCoolDown();
                    };
                    JumboButton.StartCoolDown();
                    JumboButton.EffectTimer = NebulaAPI.Modules.Timer(this, JumboDuration);
                    JumboButton.SetLabel("jumbo");
                    MiniButton.OnClick = (button) =>
                    {
                        button.StartEffect();
                    };
                    MiniButton!.OnClick = (button) =>
                    {
                        button.StartEffect();
                    };
                    MiniButton.OnEffectStart = (button) =>
                    {
                        JumboButton.StartCoolDown();
                        (JumboButton.CoolDownTimer as GameTimer)?.Pause();
                        MyPlayer.GainSpeedAttribute(MiniSpeed, MiniDuration, false, 1, "mini::speed");
                        MyPlayer.GainAttribute(PlayerAttributes.ScreenSize, MiniDuration, MiniScreen, false, 1, "mini::screen");
                        MyPlayer.GainSizeAttribute(new Vector2(MiniSize, MiniSize),MiniDuration,true,1,"slime:mini");
                        StatsSlime.Progress();
                    };
                    MiniButton.OnEffectEnd = (button) =>
                    {
                        JumboButton.StartCoolDown();
                        (JumboButton.CoolDownTimer as GameTimer)?.Resume();
                        button.StartCoolDown();
                    };
                    MiniButton.StartCoolDown();
                    MiniButton.EffectTimer = NebulaAPI.Modules.Timer(this, MiniDuration);
                    MiniButton.SetLabel("mini");
                }
            }
        }
    }
}