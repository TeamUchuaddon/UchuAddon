using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Modifier;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.Internal;
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
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Modifier;

public class TunaModiU : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private TunaModiU() : base("tunaModiU", "TNA", new(171, 245, 255), [TunaAction,UseallyStopTime,TotalStopTime,MeetingTotalTimeReset,MeetingStopTime])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Tuna.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Tuna.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;

    static private ValueConfiguration<int> TunaAction = NebulaAPI.Configurations.Configuration("options.role.tunaU.tunaAction", ["options.role.tunaU.tunaAction.usually", "options.role.tunaU.tunaAction.total"], 0);
    static private FloatConfiguration UseallyStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.UseallystopTime", (1f, 10f, 1f), 3f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 0);
    static private FloatConfiguration TotalStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.TotalstopTime", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 1);
    static private BoolConfiguration MeetingTotalTimeReset = NebulaAPI.Configurations.Configuration("options.role.tunaU.meetingTotalTimeReset", false, static () => TunaAction.GetValue() == 1);
    static private FloatConfiguration MeetingStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.MeetingstopTime", (1f, 10f, 1f), 10f, FloatConfigurationDecorator.Second);

    static public TunaModiU MyRole = new TunaModiU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private UnityEngine.Vector2 TunaPosition;
        float TunaTimerUse = UseallyStopTime;
        float TunaTimerTotal = TotalStopTime;
        bool TunaCount = false;
        float StartTimer = 10f;
        bool TimerBreak = true;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner)
            {

            }
        }

        void GameStart(GameStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
        }

        void MeetingStart(MeetingStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            if (MeetingTotalTimeReset)
            {
                float TunaTimerTotal = TotalStopTime;
            }
        }

        void MeetingEnd(MeetingEndEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            StartTimer = MeetingStopTime;
            if (MeetingTotalTimeReset)
            {
                float TunaTimerTotal = TotalStopTime;
            }
        }

        [Local]
        void PlayerRevive(PlayerReviveEvent ev)
        {
            TunaTimerUse = UseallyStopTime;
            TunaTimerTotal = TotalStopTime;
        }
        [Local]
        void GameUpdate(GameUpdateEvent ev)
        {
            if (TimerBreak)
            {
                StartTimer -= Time.deltaTime;
                if (StartTimer <= 0f)
                {
                    TunaCount = true;
                    TimerBreak = false;
                }
            }

            if (!TunaCount) return;
            if (MyPlayer == null || MyPlayer.IsDead || !MyPlayer.CanMove || MeetingHud.Instance) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaPosition == default)
                    TunaPosition = MyPlayer.TruePosition;

                if (TunaPosition == MyPlayer.TruePosition)
                {
                    TunaTimerTotal -= Time.deltaTime;
                    if (TunaTimerTotal <= 0f)
                    {
                        MyPlayer.Suicide(PlayerState.Suicide, null, KillParameter.NormalKill, null);
                        return;
                    }
                }
                else
                {
                    TunaPosition = MyPlayer.TruePosition;
                }
                return;
            }

            if (TunaAction.GetValue() != 0) return;

            if (TunaPosition == default)
                TunaPosition = MyPlayer.TruePosition;

            if (TunaPosition == MyPlayer.TruePosition)
            {
                TunaTimerUse -= Time.deltaTime;
                if (TunaTimerUse <= 0f)
                {
                    MyPlayer.Suicide(PlayerState.Suicide, null, KillParameter.NormalKill, null);
                    return;
                }
            }
            else
            {
                TunaTimerUse = UseallyStopTime;
                TunaPosition = MyPlayer.TruePosition;
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        [Local,OnlyMyPlayer]
        void Name(PlayerDecorateNameEvent ev)
        {
            string label = NebulaAPI.Language.Translate("role.tunaU.stopTimeText");
            string special = NebulaAPI.Language.Translate("role.tunaU.stopTimeTextSpecial");
            if (!TunaCount)
            {
                ev.Name += $" ({special})".Color(Color.yellow);
                return;
            }
            if (!TunaCount) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaTimerTotal > 1)
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Color.yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Color.red);
                }
            }
            else
            {
                if (TunaTimerUse > 1)
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Color.yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Color.red);
                }
            }
        }
    }
}