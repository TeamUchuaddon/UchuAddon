using AsmResolver.PE.DotNet.ReadyToRun;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine;
using UnityEngine.Analytics;
using Virial;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Configuration;
using Virial.DI;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using Virial.Text;
using static Epic.OnlineServices.Helper;
using static Il2CppSystem.DateTimeParse;
using static Nebula.Roles.Impostor.Cannon;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Tilemaps.Tilemap;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class TriggerU : DefinedSingleAbilityRoleTemplate<TriggerU.Ability>, DefinedRole,IAssignableDocument
{
    public TriggerU() : base("triggerU", new(227, 227, 227), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [AnalysisCooldown,NumOfAnalysis,PlayerColor,RelocationCooldown,RelocationDuration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Trigger.png")!.AsImage(115f);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    public static FloatConfiguration AnalysisCooldown = NebulaAPI.Configurations.Configuration("options.role.triggerU.analysisCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfAnalysis = NebulaAPI.Configurations.Configuration("options.role.triggerU.numOfAnalysis", (1, 15), 4);
    public static FloatConfiguration RelocationCooldown = NebulaAPI.Configurations.Configuration("options.role.triggerU.relocationCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration RelocationDuration = NebulaAPI.Configurations.Configuration("options.role.triggerU.relocationDuration", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration PlayerColor = NebulaAPI.Configurations.Configuration("options.role.triggerU.playerColor", true);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public TriggerU MyRole = new();


    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    private class VentHolderManager : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static VentHolderManager() => DIManager.Instance.RegisterModule(() => new VentHolderManager());
        private VentHolderManager()
        {
            ModSingleton<VentHolderManager>.Instance = this;
        }
        protected override void OnInjected(Virial.Game.Game container)
        {
            this.Register(container);
        }

        public Dictionary<int, GamePlayer> VentHolders = [];
        private Vent? currentLocalHolding = null;
        float syncTimer = 0f;
        public void RequestHoldVent(Vent vent)
        {
            RpcCheckHoldVent.Invoke((GamePlayer.LocalPlayer, vent.Id));
        }
        public void RequestReleaseVent(Vent vent)
        {
            RpcReleaseVent.Invoke((GamePlayer.LocalPlayer, vent.Id, vent.transform.position));
        }
        private void CheckHoldVent(GamePlayer player, Vent vent)
        {
            if (VentHolders.ContainsKey(vent.Id)) return;
            if (VentHolders.Any(entry => entry.Value.PlayerId == player.PlayerId)) return;//同時に1つのベントしか掴めない

            RpcHoldVent.Invoke((player, vent.Id));
        }

        private void HoldVent(GamePlayer player, Vent vent)
        {
            VentHolders.Add(vent.Id, player);
            if (player.AmOwner) currentLocalHolding = vent;
        }

        private void CheckAndReleaseVent(GamePlayer player, Vent vent, Vector3 pos)
        {
            if (VentHolders.TryGetValue(vent.Id, out var holder) && player.PlayerId == holder.PlayerId)
            {
                VentHolders.Remove(vent.Id);
                vent.transform.position = pos;

                if (player.AmOwner && currentLocalHolding?.Id == vent.Id) currentLocalHolding = null;
            }
        }

        public bool AnyoneHolds(Vent vent) => VentHolders.ContainsKey(vent.Id);
        static private bool TryGetVent(int id, [MaybeNullWhen(false)] out Vent vent) => ShipStatus.Instance.AllVents.Find(v => v.Id == id, out vent);
        static private RemoteProcess<(GamePlayer player, int ventId)> RpcCheckHoldVent = new("PlumberCheckHoldVent", (message, _) =>
        {
            if (Helpers.AmHost(PlayerControl.LocalPlayer) && TryGetVent(message.ventId, out var vent)) ModSingleton<VentHolderManager>.Instance.CheckHoldVent(message.player, vent);
        });

        static private RemoteProcess<(GamePlayer player, int ventId)> RpcHoldVent = new("TriggerHoldVent_Uchu", (message, _) =>
        {
            if (TryGetVent(message.ventId, out var vent)) ModSingleton<VentHolderManager>.Instance.HoldVent(message.player, vent);
        });

        static private RemoteProcess<(GamePlayer player, int ventId, Vector3 pos)> RpcReleaseVent = new("TriggerReleaseVent_Uchu", (message, _) =>
        {
            if (TryGetVent(message.ventId, out var vent)) ModSingleton<VentHolderManager>.Instance.CheckAndReleaseVent(message.player, vent, message.pos);
        });

        static private RemoteProcess<(int ventId, Vector3 pos)> RpcSyncVentPos = new("TriggerSyncPos_Uchu", (message, _) =>
        {
            if (TryGetVent(message.ventId, out var vent))
            {
                vent.transform.position = message.pos;
            }
        });

        private bool CheckVentPosition(UnityEngine.Vector3 position)
        {
            var data = MapData.GetCurrentMapData();
            return data.CheckMapArea(position, 0.3f);
        }
        void OnUpdate(GameHudUpdateEvent ev)
        {
            if (!MeetingHud.Instance)
            {
                if (currentLocalHolding != null)
                {
                    var currentPos = currentLocalHolding.transform.position;
                    var targetPos = GamePlayer.LocalPlayer.VanillaPlayer.GetTruePosition();

                    UnityEngine.Vector3 nextPos;
                    if (currentPos.Distance(targetPos) < 0.7f)
                        nextPos = currentPos + (UnityEngine.Vector3)((UnityEngine.Vector2)(targetPos - (UnityEngine.Vector2)currentPos).Delta(5.5f, 0.02f));
                    else
                        nextPos = targetPos;

                    nextPos.z = nextPos.y / 1000f + 0.01f;

                    if (CheckVentPosition(nextPos)) currentLocalHolding.transform.position = nextPos;

                    syncTimer -= Time.deltaTime; 
                    if (currentLocalHolding != null && syncTimer <= 0f)
                    {
                        syncTimer = 0.02f;

                        RpcSyncVentPos.Invoke((currentLocalHolding.Id, currentLocalHolding.transform.position));
                    }
                }
            }
        }

        void OnDead(PlayerDieEvent ev)
        {
            if (ev.Player.AmOwner && currentLocalHolding != null) RequestReleaseVent(currentLocalHolding);
        }

        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            if (currentLocalHolding != null) RequestReleaseVent(currentLocalHolding);
        }
    }


    static private Image AnalysisImage = NebulaAPI.AddonAsset.GetResource("TriggerAnalysisButton.png")!.AsImage(115f)!;
    static private Image DragImage = NebulaAPI.AddonAsset.GetResource("TriggerDragButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(AnalysisImage, "role.triggerU.ability.analysis");
        yield return new(DragImage, "role.trigeerU.ability.relocation");
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Trigger.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    private static MultiImage Icon = NebulaAPI.AddonAsset.GetResource("NoticeIcon.png")!.AsMultiImage(4, 1, 250f)!;
    static private Image NoticeIconImage => Icon.AsLoader(3);

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private Vent? holdingVent = null;
        private ObjectTracker<Vent>? ventTracker;
        private bool isDragging = false;
        List<int> AnalysisVent = [];
        int left = NumOfAnalysis;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                ventTracker = ObjectTrackers.ForVents(this, 0.8f, MyPlayer, vent => true /* !ModSingleton<VentHolderManager>.Instance.AnyoneHolds(vent) */, UnityEngine.Color.white);

                var AnalysisButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    AnalysisCooldown, "analysis", AnalysisImage, _ => ventTracker.CurrentTarget != null && !AnalysisVent.Contains(ventTracker.CurrentTarget.Id) && left > 0).SetAsUsurpableButton(this);
                AnalysisButton.ShowUsesIcon(3, left.ToString());
                AnalysisButton.Visibility = (button) => !MyPlayer.IsDead && left > 0;
                AnalysisButton.OnClick = (button) =>
                {
                    var targetVent = ventTracker?.CurrentTarget;
                    if (targetVent != null)
                    {
                        left--;
                        AnalysisVent.Add(targetVent.Id);
                        AnalysisButton.UpdateUsesIcon(left.ToString());
                        AnalysisButton.StartCoolDown();
                    }
                };


                var RelocationButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                    RelocationCooldown, RelocationDuration,"relocation", DragImage, _ => ventTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                RelocationButton.OnClick = (button) =>
                {
                    RelocationButton.ToggleEffect();
                    if (RelocationButton.IsInEffect)
                    {
                        if (!isDragging)
                        {
                            var targetVent = ventTracker?.CurrentTarget;
                            if (targetVent != null)
                            {
                                holdingVent = targetVent;
                                isDragging = true;

                                ModSingleton<VentHolderManager>.Instance.RequestHoldVent(targetVent);
                            }
                        }
                    }
                };
                RelocationButton.OnEffectEnd = (button) =>
                {
                    if (holdingVent == null) return;

                    ModSingleton<VentHolderManager>.Instance.RequestReleaseVent(holdingVent);

                    holdingVent = null;
                    isDragging = false;

                    RelocationButton.StartCoolDown();
                };
                RelocationButton.SetAsUsurpableButton(this);
            }
        }

        void VentCheck(PlayerVentEnterEvent ev)
        {
            var player = ev.Player;
            var roomName = NebulaAPI.CurrentGame?.CurrentMap?.GetRoomName(player.TruePosition, true, false, false) ?? "ERROR";

            Vector2 playerPos = player.VanillaPlayer.transform.position;

            Vent? nearestVent = null;
            float minDistance = float.MaxValue;

            foreach (var v in ShipStatus.Instance.AllVents)
            {
                float d = playerPos.Distance((Virial.Compat.Vector2)v.transform.position);
                if (d < minDistance)
                {
                    minDistance = d;
                    nearestVent = v;
                }
            }
            if (nearestVent == null) return;

            if (AnalysisVent.Contains(nearestVent.Id))
            {
                List<(string tag, string message)> cand = new();

                AmongUsUtil.PlayFlash(Color.Cyan);

                var lifespan = new FlexibleLifespan(this);
                var timer = NebulaAPI.Modules.Timer(lifespan, 5f);
                new TrackingArrowAbility(ev.Player.RealPlayer, 0f, Color.Cyan, false).Register(lifespan);
                timer.Start();
                GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev =>
                {
                    if (!timer.IsProgressing)
                    {
                        lifespan.Release();
                    }
                }, lifespan);

                if (PlayerColor)
                {
                    cand.Add(("PlayerColor", Language.Translate("options.role.triggerU.message.ventColor").Replace("%COLOR%", Language.Translate(ModSingleton<BalancedColorManager>.Instance.IsLightColor(DynamicPalette.PlayerColors[ev.Player.PlayerId]) ? "options.role.triggerU.message.inner.lightColor" : "options.role.triggerU.message.inner.darkColor"))));

                    (string tag, string rawText) = cand.Random();
                    NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.triggerU.message.header")),
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(Language.Translate("options.role.triggerU.message") + "<br>" +rawText))),
                       NoticeIconImage, MyRole.RoleColor);
                }
                else
                {
                    NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.triggerU.message.header")),
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(Language.Translate("options.role.triggerU.message")))),
                        NoticeIconImage, MyRole.RoleColor);
                }
            }
        }
    }
}