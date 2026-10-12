using AsmResolver.PE;
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
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class TunaU : DefinedRoleTemplate, HasCitation, DefinedRole,IAssignableDocument
{
    public static Team MyTeam = new Team("teams.tunaU", new Virial.Color(171, 245, 255), TeamRevealType.OnlyMe);
    private TunaU() : base("tunaU", new Virial.Color(171, 245, 255), RoleCategory.NeutralRole, MyTeam, [TunaAction,UseallyStopTime,TotalStopTime,MeetingTotalTimeReset,MeetingStopTime,VentConfiguration,
         new GroupConfiguration("options.role.tuna.group.gauge",[RequiredGaugeToWin, StopGaugeRatio], GroupConfigurationColor.ToDarkenColor(MyTeam.UnityColor))])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Tuna.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation =>Nebula.Roles.Citations.SuperNewRoles;

    static private ValueConfiguration<int> TunaAction = NebulaAPI.Configurations.Configuration("options.role.tunaU.tunaAction", ["options.role.tunaU.tunaAction.usually", "options.role.tunaU.tunaAction.total"], 0);
    static private FloatConfiguration UseallyStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.UseallystopTime", (1f, 10f, 1f), 3f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 0);
    static private FloatConfiguration TotalStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.TotalstopTime", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 1);
    static private BoolConfiguration MeetingTotalTimeReset = NebulaAPI.Configurations.Configuration("options.role.tunaU.meetingTotalTimeReset", false, static () => TunaAction.GetValue() == 1);
    static private FloatConfiguration MeetingStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.MeetingstopTime", (1f, 10f, 1f), 10f, FloatConfigurationDecorator.Second);
    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.amnesiaU.vent", true);
    static private FloatConfiguration RequiredGaugeToWin = NebulaAPI.Configurations.Configuration("options.role.tunaU.requiredGaugeToWin", (50f, 640f, 5f), 130f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration StopGaugeRatio = NebulaAPI.Configurations.Configuration("options.role.tunaU.stopGaugeRatio", (1f, 10f, 0.25f), 1.25f, FloatConfigurationDecorator.Ratio);

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Tuna.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public TunaU MyRole = new TunaU();
    static private readonly Virial.Media.Image GaugeImage = NebulaAPI.AddonAsset.GetResource("TunaGaugeDocuments.png")!.AsImage(100f)!;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(GaugeImage, "role.tunaU.ability.gauge");
    }
    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;
        private Vector2 TunaPosition;
        float TunaTimerUse = UseallyStopTime;
        float TunaTimerTotal = TotalStopTime;
        bool TunaCount = false;
        float StartTimer = 10f;
        bool TimerBreak = true;
        TunaGaugeAbility TunaGauge = null!; //ゲージ自体
        bool IsTunaStopped = false;
        private float StopBonus = (StopGaugeRatio - 1.0000000000f) / 60.0000000000f;
        private float TunaGaugeValue = 0f; //ゲージの値
        static private float TunaGaugeMax => RequiredGaugeToWin;
        private bool TunaGaugeActive { get; set; } = false;
        public static MultiImage GaugeIcons = NebulaAPI.AddonAsset.GetResource("TunaGaugeIcon.png")!.AsMultiImage(2, 1, 160f)!;

        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                TimerBreak = true;
                TunaCount = false;
                StartTimer = 10f;

                TunaGauge = new TunaGaugeAbility(TunaGaugeMax, 340f, GaugeIcons.AsLoader(0), GaugeIcons.AsLoader(1), () => TunaGaugeValue, () => TunaGaugeActive).Register(this);
                TunaGauge.SetActive(true);
            }
        }

        [Local]
        void GameStart(GameStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            StartTimer = 10f;
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            StartTimer = 10f;
            if (MeetingTotalTimeReset)
            {
                TunaTimerTotal = TotalStopTime;
            }
        }

        [Local]
        void MeetingEnd(MeetingEndEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            StartTimer = MeetingStopTime;
            if (MeetingTotalTimeReset)
            {
                TunaTimerTotal = TotalStopTime;
            }
        }

        [OnlyMyPlayer, Local]
        void PlayerRevive(PlayerReviveEvent ev)
        {
            TunaTimerUse = UseallyStopTime;
            TunaTimerTotal = TotalStopTime;
        }

        private bool HasTunaPosition = false;

        void GameUpdate(GameUpdateEvent ev)//自殺タイマーの管理
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
            if (MyPlayer == null || MyPlayer.IsDead || !MyPlayer.CanMove || MeetingHud.Instance || ExileController.Instance) return;
            if (TunaAction.GetValue() == 1)
            {
                if (!HasTunaPosition)
                {
                    TunaPosition = MyPlayer.TruePosition;
                    HasTunaPosition = true;
                }

                if ((TunaPosition - MyPlayer.TruePosition).Magnitude < 0.001f)
                {
                    IsTunaStopped = true;
                    TunaTimerTotal -= Time.deltaTime;

                    if (TunaTimerTotal <= 0f)
                    {
                        MyPlayer.Suicide(UchuTranslatable.tunaDead, PlayerState.Suicide, KillParameter.NormalKill, null);
                        return;
                    }
                }
                else
                {
                    IsTunaStopped = false;
                    TunaPosition = MyPlayer.TruePosition;
                }

                return;
            }

            if (TunaAction.GetValue() != 0) return;

            if (!HasTunaPosition)
            {
                TunaPosition = MyPlayer.TruePosition;
                HasTunaPosition = true;
            }

            if ((TunaPosition - MyPlayer.TruePosition).Magnitude < 0.001f)
            {
                IsTunaStopped = true;
                TunaTimerUse -= Time.deltaTime;
                if (TunaTimerUse <= 0f)
                {
                    MyPlayer.Suicide(UchuTranslatable.tunaDead, PlayerState.Suicide, KillParameter.NormalKill, null);
                    return;
                }
            }
            else
            {
                IsTunaStopped = false;
                TunaTimerUse = UseallyStopTime;
                TunaPosition = MyPlayer.TruePosition;
            }
        }

        float shareInterval = 0.5f;
        float progressGrace = 0f;

        [Local]
        void GaugeUpdate(GameUpdateEvent ev)
        {
            if (TunaGaugeValue >= TunaGaugeMax)
            {
                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.TunaTeamWin, 1 << MyPlayer.PlayerId);
                return;
            }
            if (MyPlayer.IsDead)
            {
                TunaGauge.SetActive(false);
                return;
            }
            TunaGauge.SetActive(true);
            TunaGaugeActive = false;

            if (MeetingHud.Instance || ExileController.Instance)
            {
                if (!(progressGrace > 0f))
                {
                    RpcShareTunaGauge.Invoke((MyPlayer, TunaGaugeValue));
                    progressGrace = 5f;
                    shareInterval = 0.5f;
                }
                return;
            }

            if (progressGrace > 0f)
            {
                progressGrace -= Time.deltaTime;
                return;
            }

            if (!MyPlayer.CanMove)
            {
                TunaGaugeActive = false;
                return;
            }

            if (TimerBreak)
            {
                TunaGaugeActive = false;
                return;
            }

            if (Player.AllPlayers.Any(p => p != MyPlayer && !p.IsDead && p.Position.Distance(MyPlayer.Position) < 1.4f))
            {
                if (!IsTunaStopped)
                {
                    TunaGaugeValue = Math.Clamp(TunaGaugeValue + Time.deltaTime, 0f, TunaGaugeMax);
                }
                else
                {
                    TunaGaugeValue = Math.Clamp(TunaGaugeValue + Time.deltaTime + StopBonus,0f, TunaGaugeMax);
                }
                TunaGaugeActive = true;
            }
            else
            {
                TunaGaugeActive = false;
            }

        }
        static private RemoteProcess<(GamePlayer tuna, float gauge)> RpcShareTunaGauge = new("ShareTunaGauge", (message, _) =>
        {
            if (message.tuna.Role is Instance tunaInstance) tunaInstance.TunaGaugeValue = message.gauge;
        }, false);

        [Local,OnlyMyPlayer]
        void Name(PlayerDecorateNameEvent ev)
        {
            if (MyPlayer.IsDead)return;
            string label = NebulaAPI.Language.Translate("role.tunaU.stopTimeText");
            string special = NebulaAPI.Language.Translate("role.tunaU.stopTimeTextSpecial");
            if (!TunaCount)
            {
                ev.Name += $" ({special})".Color(Virial.Color.Yellow);
                return;
            }
            if (!TunaCount) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaTimerTotal > 1)
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Virial.Color.Yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Virial.Color.Red);
                }
            }
            else
            {
                if (TunaTimerUse > 1)
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Virial.Color.Yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Virial.Color.Red);
                }
            }

        }
    }
}

public class TunaModiU : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private TunaModiU() : base("tunaModiU", "TNA", new(171, 245, 255), [TunaAction, UseallyStopTime, TotalStopTime, MeetingTotalTimeReset, MeetingStopTime, TaskStopTime])
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
    static private readonly BoolConfiguration TaskStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.taskStopTime", true);

    static public TunaModiU MyRole = new TunaModiU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private Vector2 TunaPosition;
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

        bool CanMove()
        {
            return MyPlayer.CanMove || (TaskStopTime && Minigame.Instance);
        }

        private bool HasTunaPosition = false;

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
            if (MyPlayer == null || MyPlayer.IsDead || !CanMove() || MeetingHud.Instance || ExileController.Instance) return;
            if (TunaAction.GetValue() == 1)
            {
                if (!HasTunaPosition)
                {
                    TunaPosition = MyPlayer.TruePosition;
                    HasTunaPosition = true;
                }

                if ((TunaPosition - MyPlayer.TruePosition).Magnitude < 0.001f)
                {
                    TunaTimerTotal -= Time.deltaTime;

                    if (TunaTimerTotal <= 0f)
                    {
                        MyPlayer.Suicide(UchuTranslatable.tunaDead, PlayerState.Suicide, KillParameter.NormalKill, null);
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

            if (!HasTunaPosition)
            {
                TunaPosition = MyPlayer.TruePosition;
                HasTunaPosition = true;
            }

            if ((TunaPosition - MyPlayer.TruePosition).Magnitude < 0.001f)
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

        [Local, OnlyMyPlayer]
        void Name(PlayerDecorateNameEvent ev)
        {
            string label = NebulaAPI.Language.Translate("role.tunaU.stopTimeText");
            string special = NebulaAPI.Language.Translate("role.tunaU.stopTimeTextSpecial");
            if (!TunaCount)
            {
                ev.Name += $" ({special})".Color(Virial.Color.Yellow);
                return;
            }
            if (!TunaCount) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaTimerTotal > 1)
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Virial.Color.Yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Virial.Color.Red);
                }
            }
            else
            {
                if (TunaTimerUse > 1)
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Virial.Color.Yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Virial.Color.Red);
                }
            }
        }
    }
}