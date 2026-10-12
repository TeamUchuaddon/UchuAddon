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
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

file static class ProxyAsset
{
    static public Virial.Media.Image ButtonImage = NebulaAPI.AddonAsset.GetResource("ProxyRequestButton.png")!.AsImage(115f)!;
    static public Virial.Media.Image RequestImage = NebulaAPI.AddonAsset.GetResource("ProxyButton.png")!.AsImage(115f)!;

}

public class ProxyU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    private ProxyU() : base("proxyU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam,[RequestCooldown, EmbroilAction], othersAssignments: () =>{
        return [new((_, playerId) =>{int groupId = playerId; return (MonitorU.MyRole, new[] { groupId });}, RoleCategory.CrewmateRole)];})
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Proxy.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [MonitorU.MyRole.ConfigurationHolder!]);
    }

    static public FloatConfiguration RequestCooldown = NebulaAPI.Configurations.Configuration("options.role.proxyU.requestCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    //static public BoolConfiguration ShowPairArrow = NebulaAPI.Configurations.Configuration("options.role.proxyU.showPairArrow", true);
    static public ValueConfiguration<int> EmbroilAction = NebulaAPI.Configurations.Configuration("options.role.proxyU.embroilAction", ["options.role.proxyU.embroilAction.not", "options.role.proxyU.embroilAction.proxy", "options.role.proxyU.embroilAction.monitor", "options.role.proxyU.embroilAction.all"], 0);

    DefinedRole[] DefinedRole.AdditionalRoles => [MonitorU.MyRole];
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)=> new Instance(player, arguments.Get(0, player.PlayerId));

    static public ProxyU MyRole = new ProxyU();


    bool IAssignableDocument.HasTips => tips;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ProxyAsset.RequestImage, "role.proxyU.ability.request");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        string text = EmbroilAction.GetValue() switch
        {
            0 => Language.Translate("role.proxyU.embroilAction.not"),
            1 => Language.Translate("role.proxyU.embroilAction.proxy"),
            2 => Language.Translate("role.proxyU.embroilAction.monitor"),
            3 => Language.Translate("role.proxyU.embroilAction.all"),
            _ => ""
        };

        yield return new("%EMBROIL%", text);
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Proxy&Monitor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool tips = EmbroilAction.GetValue() != 0;

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public int GroupId { get; }

        TrackingArrowAbility? pairArrow = null;

        void SetPairArrow()
        {
            //if (!ShowPairArrow) return;

            if (pairArrow != null)
            {
                pairArrow.Release();
                pairArrow = null;
            }

            var monitor = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => IsMyMonitor(p) && !p.IsDead);
            if (monitor == null) return;

            if (pairArrow != null) pairArrow.Release();
            pairArrow = new TrackingArrowAbility(monitor, 0f, Color.Red, false).Register(this);
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
                //SetPairArrow();
                AmongUsUtil.PlayCustomFlash(Color.Red, 0f, 0.25f, 0.4f);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);

                var RequestButton = NebulaAPI.Modules.AbilityButton(this,MyPlayer,Virial.Compat.VirtualKeyInput.Ability, RequestCooldown,"Request", ProxyAsset.RequestImage,
                    _ =>{var target = playerTracker.CurrentTarget;　return target != null && IsMyMonitor(target);});
                RequestButton.Visibility = (button) => !MyPlayer.IsDead;
                RequestButton.OnClick = _ =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer && IsMyMonitor(target))
                    {
                        RpcRequestSwap.Invoke((MyPlayer, target));
                        RequestButton.StartCoolDown();
                    }
                };
            }
        }

        /*
        void IGameOperator.OnReleased()
        {
            if (pairArrow != null)
            {
                pairArrow.Release();
                pairArrow = null;
            }
        }

        [Local]
        void OnSetRole(PlayerRoleSetEvent ev)
        {
            SetPairArrow();
        }
        */

        static public RemoteProcess<(GamePlayer proxy, GamePlayer monitor)> RpcRequestSwap= new("ProxyUSwap", (message, _) =>
        {
            if (!AmongUsClient.Instance.AmHost) return;

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

        void OnProxyDead(PlayerDieOrDisconnectEvent ev)
        {
            //SetPairArrow();

            if (ev.Player == MyPlayer && (ProxyU.EmbroilAction.GetValue() == 1 || ProxyU.EmbroilAction.GetValue() == 3))
            {
                var monitor = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => IsMyMonitor(p) && !p.IsDead);

                if (monitor == null) return;

                if (ev is PlayerExiledEvent)
                {
                    monitor.VanillaPlayer.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide);
                }
                else
                {
                    monitor.Suicide(PlayerState.Suicide, PlayerState.Suicide, KillParameter.RemoteKill);
                }
            }
        }
    }
}

public class MonitorU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    private MonitorU() : base("monitorU",NebulaTeams.ImpostorTeam.Color,RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam,[CanFixLightOption, CanFixCommsOption, HasImpostorVisionOption, CanUseVentsOption, CanMoveInVentsOption],false,optionHolderPredicate: () => (ProxyU.MyRole as ISpawnable).IsSpawnable)
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Monitor.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [ProxyU.MyRole.ConfigurationHolder!]);
    }


    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)=> new Instance(player, arguments.Get(0, -1));

    static private readonly BoolConfiguration CanFixLightOption = NebulaAPI.Configurations.Configuration("options.role.monitorU.canFixLight", false);
    static private readonly BoolConfiguration CanFixCommsOption = NebulaAPI.Configurations.Configuration("options.role.monitorU.canFixComms", false);
    static private readonly BoolConfiguration HasImpostorVisionOption = NebulaAPI.Configurations.Configuration("options.role.monitorU.hasImpostorVision", false);
    static private readonly BoolConfiguration CanUseVentsOption = NebulaAPI.Configurations.Configuration("options.role.monitorU.canUseVents", false);
    static private readonly BoolConfiguration CanMoveInVentsOption = NebulaAPI.Configurations.Configuration("options.role.monitorU.canMoveInVents", false);

    static public MonitorU MyRole = new MonitorU();
    bool ISpawnable.IsSpawnable => false;
    bool DefinedRole.IsMadmate => true;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ProxyAsset.ButtonImage, "role.monitorU.ability.request");
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Proxy&Monitor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public int GroupId { get; private set; }

        TrackingArrowAbility? pairArrow = null;

        int[] RuntimeAssignable.RoleArguments => [GroupId];

        void SetPairArrow()
        {
            //if (!ProxyU.ShowPairArrow) return;

            if (pairArrow != null)
            {
                pairArrow.Release();
                pairArrow = null;
            }

            var proxy = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => IsMyProxy(p) && !p.IsDead);
            if (proxy == null) return;

            if (pairArrow != null) pairArrow.Release();
            pairArrow = new TrackingArrowAbility(proxy, 0f, Color.Red, false).Register(this);
        }

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

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                //SetPairArrow();

                AmongUsUtil.PlayCustomFlash(Color.Red, 0f, 0.25f, 0.4f);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);

                var RequestButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, ProxyU.RequestCooldown, "monitorU.Request", ProxyAsset.ButtonImage,
                    _ => { var target = playerTracker.CurrentTarget; return target != null && IsMyProxy(target); });
                RequestButton.Visibility = (button) => !MyPlayer.IsDead;
                RequestButton.OnClick = _ =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer && IsMyProxy(target))
                    {
                        RpcRequestSwap_FromMonitor.Invoke((target, MyPlayer));
                        RequestButton.StartCoolDown();
                    }
                };
            }
        }

        /*
        void IGameOperator.OnReleased()
        {
            if (pairArrow != null)
            {
                pairArrow.Release();
                pairArrow = null;
            }
        }

        [Local]
        void OnSetRole(PlayerRoleSetEvent ev)
        {
            SetPairArrow();
        }
        */

        static private RemoteProcess<(GamePlayer proxy, GamePlayer monitor)> RpcRequestSwap_FromMonitor = new("ProxyUSwap_FromMonitor", (message, _) =>
        {
            if (!AmongUsClient.Instance.AmHost) return;

            var proxy = message.proxy;
            var monitor = message.monitor;

            if (proxy == null || monitor == null) return;
            if (proxy.IsDead || monitor.IsDead) return;

            if (proxy.Role is not ProxyU.Instance proxyRole) return;
            if (monitor.Role is not MonitorU.Instance monitorRole) return;

            int groupId = proxyRole.GroupId;

            proxy.SetRole(MonitorU.MyRole, new int[] { groupId });
            monitor.SetRole(ProxyU.MyRole, new int[] { groupId });

            PlayerExtension.SendRoleSwapping(proxy, monitor, ProxyU.MyRole, PlayerRoleSwapEvent.SwapType.Swap);
            PlayerExtension.SendRoleSwapping(monitor, proxy, MonitorU.MyRole, PlayerRoleSwapEvent.SwapType.Swap);
        });

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;

        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            //SetPairArrow();

            if (ev.Player == MyPlayer && (ProxyU.EmbroilAction.GetValue() == 2 || ProxyU.EmbroilAction.GetValue() == 3))
            {
                var proxy = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => IsMyProxy(p) && !p.IsDead);

                if (proxy == null) return;

                if (ev is PlayerExiledEvent)
                {
                    proxy.VanillaPlayer.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide);
                }
                else
                {
                    proxy.Suicide(PlayerState.Suicide, PlayerState.Suicide, KillParameter.RemoteKill);
                }
            }
        }

        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if (IsMyProxy(ev.Murderer))
            {
                AmongUsUtil.PlayQuickFlash(Virial.Color.Red);
            }
        }

        bool RuntimeAssignable.CanFixComm => CanFixCommsOption;
        bool RuntimeAssignable.CanFixLight => CanFixLightOption;
        bool RuntimeRole.CanMoveInVent => CanMoveInVentsOption;
        bool RuntimeRole.CanUseVent => CanUseVentsOption;
        bool RuntimeRole.HasImpostorVision => HasImpostorVisionOption;
        bool RuntimeRole.IgnoreBlackout => HasImpostorVisionOption;
    }
}
