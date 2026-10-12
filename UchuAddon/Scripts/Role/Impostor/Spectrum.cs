using AsmResolver.PE;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Neutral;
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
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Crewmate.Madmate;
using static Nebula.Roles.Impostor.Cannon;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Impostor;


[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
internal static class CustomAssignmentSetUp
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        //preprocessor.RegisterAssignmentType(() => CustomImpostorU.MyRole, (lastArgs, role) => CustomImpostorU.GenerateArgument(role), "custom", NebulaTeams.ImpostorTeam.Color, (status, role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => (CustomImpostorU.MyRole as ISpawnable).IsSpawnable);
        preprocessor.RegisterAssignmentType(() => SpectrumU.MyRole, (lastArgs, role) => SpectrumU.GenerateArgument(role), "custom", new(255,0,0), (status, role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => (SpectrumU.MyRole as ISpawnable).IsSpawnable);
    }
}


public class SpectrumU : DefinedRoleTemplate,  DefinedRole
{
    private SpectrumU() : base("spectrumU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [KillCoolDown])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Spectrum.png")!.AsImage(115f);
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, Roles.GetRole(arguments.Get(0, -1)), arguments.Skip(1).ToArray());

    static private IRelativeCooldownConfiguration KillCoolDown = NebulaAPI.Configurations.KillConfiguration("options.role.spectrumU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static public float KillCooldown => KillCoolDown.Cooldown;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Spectrum.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public SpectrumU MyRole = new ();
    static public int[] GenerateArgument(DefinedRole? customs) => [customs?.Id ?? -1, .. (customs?.DefaultAssignableArguments ?? [])];
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles() => ((this as DefinedRole).IsSpawnable) ? [this] : [];

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        bool RuntimeRole.HasVanillaKillButton => false;
        int[] evacuatedArguments = [];
        public Instance(GamePlayer player, DefinedRole? customsRole, int[] customsRoleArguments) : base(player) 
        {
            this.evacuatedArguments = customsRoleArguments; this.MyCustoms = customsRole; 
        }
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyCustoms != null ? [MyRole, MyCustoms] : [MyRole];
        public DefinedRole? MyCustoms { get; private set; }
        public IPlayerAbility? CustomsAbility { get; private set; } = null;
        int[]? RuntimeAssignable.RoleArguments => ((int[])[MyCustoms?.Id ?? -1]).Concat(CustomsAbility?.AbilityArguments ?? []).ToArray();
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => CustomsAbility != null ? [CustomsAbility, .. CustomsAbility.SubAbilities] : [];

        string RuntimeAssignable.DisplayName
        {
            get
            {
                return CustomsAbility != null ? Language.Translate("role.spectrumU.prefix") + MyCustoms!.GetDisplayName(CustomsAbility) : (MyRole as DefinedAssignable).DisplayName;
            }
        }
        string RuntimeRole.DisplayShort => CustomsAbility != null ? Language.Translate("role.spectrumU.prefix") + MyCustoms!.GetDisplayShort(CustomsAbility) : (MyRole as DefinedRole).DisplayShort;
        string RuntimeRole.DisplayIntroBlurb => MyCustoms?.DisplayIntroBlurb ?? (MyRole as DefinedRole).DisplayIntroBlurb;
        string RuntimeRole.DisplayIntroRoleName => (this as RuntimeAssignable).DisplayName;
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole) => abilityRole == MyCustoms || abilityRole == MyRole;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);
                var killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!, (target, _) =>
                {
                    MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                },
                null,
                _ => playerTracker.CurrentTarget != null && !MyPlayer.IsDived,
                _ => MyPlayer.AllowToShowKillButtonByAbilities
                );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());

            }
            if (MyCustoms != null) CustomsAbility = MyCustoms.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.CanLoadToMadmate, evacuatedArguments)?.Register(this);
        }


    }
}