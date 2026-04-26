using Cpp2IL.Core.Extensions;
using Hori.Core;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;
using UnityColor = UnityEngine.Color;
using Image = Virial.Media.Image;
using UnityEngine;
using static Nebula.Modules.MetaWidgetOld;

namespace Hori.Scripts.Role.Impostor;

public class SerialKillerU : DefinedSingleAbilityRoleTemplate<SerialKillerU.Ability>, DefinedRole, HasCitation
{
    public SerialKillerU() : base("SerialKillerU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SuicideCooldownOption, SerikillCooldownOption, MeetingEndsTimerActiveOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SuicideCooldownOption = NebulaAPI.Configurations.Configuration("options.role.serialkillerU.SuicideCooldown", (10f, 120f, 5f), 60f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SerikillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.serialkillerU.SerikillCooldown", (0f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration MeetingEndsTimerActiveOption = NebulaAPI.Configurations.Configuration("options.role.serialkillerU.MeetingEndsTimerActive", false);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/SerialKiller.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public SerialKillerU MyRole = new();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private ModAbilityButtonImpl suicideDisplay = null!;
        private TimerImpl suicideTimerImpl = null!;

        private bool isTimerActive = false;
        private bool isKilled = false;
        private float SuicideTime = SuicideCooldownOption;

        static private Virial.Media.Image SuicideCooldownButton = NebulaAPI.AddonAsset.GetResource("SuicideButton.png")!.AsImage(115f)!;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool IPlayerAbility.HideKillButton => true;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, VirtualKeyInput.Kill, SerikillCooldownOption, "kill", ModAbilityButton.LabelType.Impostor, null, (player, button) =>
                {
                    MyPlayer.MurderPlayer(player, PlayerStates.Dead, EventDetails.Kill, KillParameter.NormalKill);
                    button.StartCoolDown();
                    isTimerActive = true;
                    isKilled = true;
                    SuicideTime = SuicideCooldownOption;
                    if (suicideDisplay?.EffectTimer != null)
                    {
                        if (suicideDisplay.EffectTimer is TimerImpl impl)
                        {
                            impl.Reset();
                        }
                        suicideDisplay.ActivateEffect();
                    }
                });

                suicideDisplay = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl().Register(this);
                suicideDisplay.SetSprite(SuicideCooldownButton.GetSprite());
                suicideDisplay.Availability = (button) => true;
                suicideDisplay.Visibility = (button) => !MyPlayer.IsDead && isTimerActive;
                suicideTimerImpl = new TimerImpl(SuicideCooldownOption).Register(this);
                suicideDisplay.EffectTimer = suicideTimerImpl;
                suicideDisplay.SetLabel("serialkill");
                /*suicideDisplay.SetLabel(Mathn.CeilToInt(SuicideTime).ToString());*/
            }
        }
        void OnMeetingStart(MeetingStartEvent ev)
        {
            isTimerActive = false;
            SuicideTime = SuicideCooldownOption;
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (MeetingEndsTimerActiveOption)
            {
                isTimerActive = true;
                SuicideTime = SuicideCooldownOption;
            }
            else if (isKilled)
            {
                isTimerActive = true;
                SuicideTime = SuicideCooldownOption;
            }
            if (suicideDisplay?.EffectTimer != null)
            {
                if (suicideDisplay.EffectTimer is TimerImpl impl)
                {
                    impl.Reset();
                }
                suicideDisplay.ActivateEffect();
            }
        }
        void OnGameUpdate(GameUpdateEvent ev)
        {
            if (isTimerActive)
            {
                SuicideTime -= Time.deltaTime;
                if (SuicideTime <= 0f)
                {
                    if (!MyPlayer.IsDead) MyPlayer.Suicide(PlayerState.Suicide, EventDetail.Kill, KillParameter.NormalKill);
                    isTimerActive = false;
                    SuicideTime = SuicideCooldownOption;
                }
            }
        }
    }
}
