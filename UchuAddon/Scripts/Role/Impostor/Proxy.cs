using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Complex;
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
using static Il2CppSystem.Net.FtpWebRequest;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

file static class ProxyAsset
{
    static public Virial.Media.Image ButtonImage = NebulaAPI.AddonAsset.GetResource("ProxyRequestButton.png")!.AsImage(115f)!;
    static public Virial.Media.Image RequestImage = NebulaAPI.AddonAsset.GetResource("ProxyButton.png")!.AsImage(115f)!;

}

public class ProxyU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    private ProxyU() : base("proxyU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, othersAssignments: () =>{
        return [new((_, playerId) =>{int groupId = playerId; return (MonitorU.MyRole, new[] { groupId });}, RoleCategory.CrewmateRole)];})
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [MonitorU.MyRole.ConfigurationHolder!]);
    }

    static public FloatConfiguration RequestCooldown = NebulaAPI.Configurations.Configuration("options.role.proxyU.requestCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);

    DefinedRole[] DefinedRole.AdditionalRoles => [MonitorU.MyRole];
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)=> new Instance(player, arguments.Get(0, player.PlayerId));

    static public ProxyU MyRole = new ProxyU();
    public static RemoteProcess<GamePlayer> RpcRequest = new("RpcProxyRequest", (player, _) =>
    {
        if (player.Role is ProxyU.Instance proxy)
        {
            proxy.SetRequest(true);
        }
    });

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ProxyAsset.RequestImage, "role.proxyU.ability.request");
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Proxy&Monitor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public int GroupId { get; }
        static public bool Request { get; private set; }
        public void SetRequest(bool value)
        {
            Request = value;
        }

        int[] RuntimeAssignable.RoleArguments => [GroupId];

        public Instance(GamePlayer player, int groupId) : base(player)
        {
            GroupId = groupId;
            UchuDebug.Log($"[ProxyU] Name={player.PlayerName} PlayerId={player.PlayerId} GroupId={GroupId}");
        }

        public bool IsMyMonitor(GamePlayer? player)
        {
            if (player == null) return false;
            if (player.Role is MonitorU.Instance monitor && monitor.GroupId == GroupId) return true;
            return false;
        }
        public bool IsSameTeam(GamePlayer? player)
        {
            if (IsMyMonitor(player)) return true;
            if (player?.Role is Instance proxy && proxy.GroupId == GroupId) return true;
            return false;
        }
        public bool IsMyProxy(GamePlayer? player)
        {
            if (player?.Role is Instance proxy && proxy.GroupId == GroupId) return true;
            return false;
        }


        void RuntimeAssignable.OnActivated()
        {
            GamePlayer? my = GamePlayer.LocalPlayer;

            if (AmOwner)
            {
                Request = false;
                AmongUsUtil.PlayCustomFlash(Color.red, 0f, 0.25f, 0.4f);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var RequestButton = NebulaAPI.Modules.AbilityButton(this,MyPlayer,Virial.Compat.VirtualKeyInput.Ability, RequestCooldown,"Request", ProxyAsset.RequestImage,
                    _ =>{var target = playerTracker.CurrentTarget;　return target != null && IsMyMonitor(target);});
                RequestButton.Visibility = (button) => !MyPlayer.IsDead;
                RequestButton.OnClick = _ =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer && IsMyMonitor(target))
                    {
                        RpcRequestSwap.Invoke((MyPlayer, target));
                    }
                };

                var ApprovalButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, false, true, Virial.Compat.VirtualKeyInput.SidekickAction, null,
                   2.5f, "approval", ProxyAsset.ButtonImage, null);
                ApprovalButton.Visibility = (button) => Request && !MyPlayer.IsDead;
                ApprovalButton.OnClick = (button) =>
                {
                    var monitor = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => IsMyMonitor(p));

                    RpcRequestSwap.Invoke((MyPlayer, monitor.RealPlayer));
                };
            }
        }

        static private RemoteProcess<(GamePlayer proxy, GamePlayer monitor)> RpcRequestSwap= new("ProxyUSwap", (message, _) =>
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Request = false;

            var proxy = message.proxy;
            var monitor = message.monitor;

            if (proxy == null || monitor == null) return;
            if (proxy.IsDead || monitor.IsDead) return;

            if (proxy.Role is not ProxyU.Instance proxyRole) return;
            if (monitor.Role is not MonitorU.Instance monitorRole) return;

            int groupId = proxyRole.GroupId;

            proxy.SetRole(MonitorU.MyRole, new int[] { groupId });
            monitor.SetRole(ProxyU.MyRole, new int[] { groupId });

            PlayerExtension.SendRoleSwapping(proxy, monitor,ProxyU.MyRole,PlayerRoleSwapEvent.SwapType.Swap);
            PlayerExtension.SendRoleSwapping(monitor, proxy,MonitorU.MyRole,PlayerRoleSwapEvent.SwapType.Swap);
        });

        [Local]
        void DecorateMonitorColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(ev.Player) && !ev.Player.AmOwner) ev.Color = ProxyU.MyRole.RoleColor;
        }

        [OnlyMyPlayer]
        void DecorateProxyColor(PlayerDecorateNameEvent ev)
        {
            if (IsMyMonitor(GamePlayer.LocalPlayer) && !ev.Player.AmOwner) ev.Color = ProxyU.MyRole.RoleColor;
        }

    }
}

public class MonitorU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    private MonitorU() : base("monitorU",NebulaTeams.ImpostorTeam.Color,RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam,[],false,optionHolderPredicate: () => (ProxyU.MyRole as ISpawnable).IsSpawnable)
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [ProxyU.MyRole.ConfigurationHolder!]);
    }


    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)=> new Instance(player, arguments.Get(0, -1));

    static public MonitorU MyRole = new MonitorU();
    bool ISpawnable.IsSpawnable => false;
    bool DefinedRole.IsMadmate => true;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ProxyAsset.RequestImage, "role.monitorU.ability.request");
        yield return new(ProxyAsset.ButtonImage, "role.monitorU.ability.approval");
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Proxy&Monitor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public int GroupId { get; private set; }
        int[] RuntimeAssignable.RoleArguments => [GroupId];

        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;

        public Instance(GamePlayer player, int groupId) : base(player)
        {
            GroupId = groupId;

            UchuDebug.Log($"[MonitorU] Name={player.PlayerName} PlayerId={player.PlayerId} GroupId={GroupId}");
        }

        public bool IsMyProxy(GamePlayer? player)
        {
            if (player == null) return false;
            if (player.Role is ProxyU.Instance proxy && proxy.GroupId == GroupId) return true;
            return false;
        }

        bool trunRequest = false;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                trunRequest = false;
                AmongUsUtil.PlayCustomFlash(Color.red, 0f, 0.25f, 0.4f);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var RequestButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, ProxyU.RequestCooldown, "Request", ProxyAsset.RequestImage,
                    _ => { var target = playerTracker.CurrentTarget; return target != null && IsMyProxy(target); });
                RequestButton.Visibility = (button) => !MyPlayer.IsDead && !trunRequest;
                RequestButton.OnClick = _ =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer && IsMyProxy(target))
                    {
                        ProxyU.RpcRequest.Invoke(target);
                        trunRequest = true;
                        RequestButton.StartCoolDown();
                    }
                };
            }
        }

        void MeetingEnd(MeetingEndEvent ev)
        {
            trunRequest = false;
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
    }
}
