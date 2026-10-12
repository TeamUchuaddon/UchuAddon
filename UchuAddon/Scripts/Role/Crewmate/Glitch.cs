using AsmResolver.PE.DotNet.ReadyToRun;
using Hori.Core;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Utilities;
using RewiredConsts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using compat = Hori.Core.UchuTranslatable;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class GlitchU : DefinedSingleAbilityRoleTemplate<GlitchU.Ability>, DefinedRole, IAssignableDocument
{
    public GlitchU() : base("GlitchU", new(100, 0, 100), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, 
        [
        InstantReviveOption, BlockMeetGlitch, 
        new GroupConfiguration("options.role.GlitchU.group.ReplaceTasks", [NumOfExtraCommonTasksOption, NumOfExtraShortTasksOption, NumOfExtraLongTasksOption], UnityEngine.Color.magenta),
        new GroupConfiguration("options.role.GlitchU.group.GlitchArrows", [GlitchArrowCrewmate, GlitchArrowImpostor, GlitchArrowNeutral], UnityEngine.Color.magenta)
        ])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Glitch.png")!.AsImage(115f);
    }
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static private readonly BoolConfiguration InstantReviveOption = NebulaAPI.Configurations.Configuration("options.role.GlitchU.InstantReviveOption", false);
    static private readonly BoolConfiguration BlockMeetGlitch = NebulaAPI.Configurations.Configuration("options.role.GlitchU.BlockMeetGlitch", true);
    static private readonly BoolConfiguration GlitchArrowImpostor = NebulaAPI.Configurations.Configuration("options.role.GlitchU.GlitchArrowImpostor", true);
    static private readonly BoolConfiguration GlitchArrowCrewmate = NebulaAPI.Configurations.Configuration("options.role.GlitchU.GlitchArrowCrewmate", false);
    static private readonly BoolConfiguration GlitchArrowNeutral = NebulaAPI.Configurations.Configuration("options.role.GlitchU.GlitchArrowNeutral", false);
    static private readonly IntegerConfiguration NumOfExtraCommonTasksOption = NebulaAPI.Configurations.Configuration("options.role.GlitchU.NumOfExtraCommonTasksOption", (0, 8), 6);
    static private readonly IntegerConfiguration NumOfExtraShortTasksOption = NebulaAPI.Configurations.Configuration("options.role.GlitchU.NumOfExtraShortTasksOption", (0, 30), 5);
    static private readonly IntegerConfiguration NumOfExtraLongTasksOption = NebulaAPI.Configurations.Configuration("options.role.GlitchU.NumOfExtraLongTasksOption", (0, 46), 6); 
    static public GlitchU MyRole = new();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Glitch.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasTips => true;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        List<UnityEngine.Vector2> spawnPoints = NebulaPreSpawnLocation.Locations[NebulaAPI.AmongUs.MapId].Where(l => l.Position.HasValue).Select(l => l.Position!.Value).ToList();
        private bool Revive = false;
        private bool firstMeeting = false;
        private bool NoTasks = false;
        private bool ZeroTask = false;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                MyPlayer.Tasks.RewindTasks(0);
                MyPlayer.Tasks.ReplaceTasks(0,0,0);
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!firstMeeting)
            {
                firstMeeting = true;
                MyPlayer.MurderPlayer(MyPlayer, compat.Glitch, null, KillParameter.MeetingKill, KillCondition.NoCondition);
                MyPlayer.Tasks.ReplaceTasks(NumOfExtraCommonTasksOption, NumOfExtraShortTasksOption, NumOfExtraLongTasksOption);
            }
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (InstantReviveOption) return;

            if (!Revive && NoTasks && MyPlayer.IsDead)
            {
                GlitchRevive();
            }
        }

        [Local]
        void OnTaskUpdated(PlayerTaskUpdateEvent ev)
        {
            NoTasks = MyPlayer.Tasks.TotalCompleted >= MyPlayer.Tasks.Quota;

            if (!Revive && NoTasks && MyPlayer.IsDead && InstantReviveOption)
            {
                GlitchRevive();
            }
        }

        void GlitchRevive() //復活処理
        {
            Vector2 spawnPos = spawnPoints[System.Random.Shared.Next(spawnPoints.Count)];
            MyPlayer.Revive(MyPlayer, spawnPos, true, true);
            RpcGlitchArrow.Invoke(MyPlayer);
            Revive = true;
        }

        [Local]
        void OnGameUpdate(UpdateSpectatorEvent ev)
        {
            if (AmOwner)
            {
                ev.CanMonitorAlives = false;
                ev.CanZoom = false;
                ev.CanSeeRoles = false;
            }
        }

        [Local]
        void BlockCallEmergencyMeeting(CheckCanPushEmergencyButtonEvent ev)
        {
            if (AmOwner && BlockMeetGlitch)
                ev.DenyButton("role.GlitchU.denyReason");
        }
    }

    private static void RegistersArrow(GamePlayer player)
    {
        List<TrackingArrowAbility> arrows = new();

        void RegisterArrow(GamePlayer player) => arrows.Add(new TrackingArrowAbility(player.Unbox(), 0f, UnityEngine.Color.magenta, false).RegisterSelf());
        if (player.AmOwner) return; 
        if (GlitchArrowImpostor && player.IsImpostor || GlitchArrowImpostor && !player.IsTrueCrewmate && player.IsCrewmate || GlitchArrowCrewmate && player.IsTrueCrewmate || GlitchArrowNeutral && !player.IsCrewmate && !player.IsImpostor)
        {
            RegisterArrow(player);
        }
    }


    static public RemoteProcess<GamePlayer> RpcGlitchArrow = new("RpcGlitchArrow_Uchu",
    (message, _) =>
    {
        if (GamePlayer.LocalPlayer == null) return;
        if (GamePlayer.LocalPlayer.IsTrueCrewmate) return;

        AmongUsUtil.PlayCustomFlash(UnityEngine.Color.magenta, 0f, 0.25f, 0.45f);
        RegistersArrow(message);
    });
}