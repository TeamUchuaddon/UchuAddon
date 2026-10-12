using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Core;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Rewired.Utils.Classes.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VFX;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Impostor;

public class SentinelU : DefinedSingleAbilityRoleTemplate<SentinelU.Ability>, DefinedRole, IAssignableDocument
{
    public SentinelU() : base("sentinelU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [ImpostorNoDevice, ShorteningCooldownInKilling])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Sentinel.png")!.AsImage(115f);
    }
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private BoolConfiguration ImpostorNoDevice = NebulaAPI.Configurations.Configuration("options.role.sentinelU.impostorNoDevice", true);
    static private FloatConfiguration ShorteningCooldownInKilling = NebulaAPI.Configurations.Configuration("options.role.sentinelU.shorteningCooldownInKilling", (0f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);

    static public SentinelU MyRole = new();
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueMapAbility;

    static public bool IsPlayerNearAdmin { get; private set; }
    static public bool IsPlayerNearVital { get; private set; }
    static public bool IsPlayerNearCamera { get; private set; }
    static public bool IsPlayerNearTelescope { get; private set; }
    static public bool IsPlayerNearDoorLog { get; private set; }

    public static MultiImage DeviceIcon = NebulaAPI.AddonAsset.GetResource("SentinelDevice.png")!.AsMultiImage(2, 5, 135f)!;
    static private readonly Image BackImage = NebulaAPI.AddonAsset.GetResource("SentinelBack.png")!.AsImage(100f)!;

    [NebulaRPCHolder]
    public class SentinelViewer : DependentLifespan, IGameOperator
    {
        private GamePlayer player;

        private static Vector3 GetDeviceIconPos(int index, int total)
        {
            float spacing = 1f;
            float startX = -(total - 1) * spacing * 0.5f; 
            return new Vector3(startX + spacing * index, -0.02f, -0.1f);
        }

        private static Vector3 GetShakeOffset(bool active)
        {
            if (!active) return Vector3.Zero;

            float x = Mathf.Sin(Time.time * 40f) * 0.01f;
            float y = Mathf.Cos(Time.time * 40f) * 0.01f;
            return new Vector3(x, y, 0f);
        }

        public SentinelViewer(GamePlayer myPlayer)
        {
            player = myPlayer;

            var gauge = HudContent.InstantiateContent("SentinelUI", true, true, false, true);
            this.BindGameObject(gauge.gameObject);
            var adjuster = gauge.gameObject.AddAdjuster();

            float gaugeScale = 0.6f;
            var center = UnityHelper.CreateObject("Adjuster", adjuster.transform, new(-1f + 5f * gaugeScale * 0.5f, -0.2f, -0.1f));
            center.transform.localScale = new(gaugeScale, gaugeScale, 1f);

            var gaugeRenderer = UnityHelper.CreateObject<SpriteRenderer>("GaugeSpriteBack", center.transform, new(0f, 0f, 0f));
            gaugeRenderer.sprite = BackImage.GetSprite();
            gaugeRenderer.drawMode = SpriteDrawMode.Sliced;
            gaugeRenderer.tileMode = SpriteTileMode.Continuous;
            gaugeRenderer.size = new(7f, 0.75f);

            SpriteRenderer? AdminRenderer = null;
            SpriteRenderer? VitalRenderer = null;
            SpriteRenderer? CameraRenderer = null;
            SpriteRenderer? TelescopeRenderer = null;
            SpriteRenderer? DoorLogRenderer = null;

            switch (NebulaAPI.AmongUs.MapId)
            {
                case 0:
                    if (GeneralConfigurations.SkeldAdminOption.Value)
                    {
                        AdminRenderer = UnityHelper.CreateObject<SpriteRenderer>("AdminIcon", center.transform, Vector3.Zero);
                        AdminRenderer.sprite = DeviceIcon.GetSprite(0);
                    }

                    CameraRenderer = UnityHelper.CreateObject<SpriteRenderer>("CameraIcon", center.transform, Vector3.Zero);
                    CameraRenderer.sprite = DeviceIcon.GetSprite(4);

                    break;

                case 1:
                    if (GeneralConfigurations.MiraAdminOption.Value)
                    {
                        AdminRenderer = UnityHelper.CreateObject<SpriteRenderer>("AdminIcon", center.transform, Vector3.Zero);
                        AdminRenderer.sprite = DeviceIcon.GetSprite(0);
                    }

                    DoorLogRenderer = UnityHelper.CreateObject<SpriteRenderer>("DoorLogIcon", center.transform, Vector3.Zero);
                    DoorLogRenderer.sprite = DeviceIcon.GetSprite(8);

                    break;

                case 2:
                    if (GeneralConfigurations.PolusAdminOption.Value)
                    {
                        AdminRenderer = UnityHelper.CreateObject<SpriteRenderer>("AdminIcon", center.transform, Vector3.Zero);
                        AdminRenderer.sprite = DeviceIcon.GetSprite(0);
                    }

                    VitalRenderer = UnityHelper.CreateObject<SpriteRenderer>("VitalIcon", center.transform, Vector3.Zero);
                    VitalRenderer.sprite = DeviceIcon.GetSprite(2);

                    CameraRenderer = UnityHelper.CreateObject<SpriteRenderer>("CameraIcon", center.transform, Vector3.Zero);
                    CameraRenderer.sprite = DeviceIcon.GetSprite(4);

                    break;

                case 3:
                    //Dleksはなし
                    break;

                case 4:
                    if (GeneralConfigurations.AirshipCockpitAdminOption.Value || GeneralConfigurations.AirshipRecordAdminOption.Value)
                    {
                        AdminRenderer = UnityHelper.CreateObject<SpriteRenderer>("AdminIcon", center.transform, Vector3.Zero);
                        AdminRenderer.sprite = DeviceIcon.GetSprite(0);
                    }

                    VitalRenderer = UnityHelper.CreateObject<SpriteRenderer>("VitalIcon", center.transform, Vector3.Zero);
                    VitalRenderer.sprite = DeviceIcon.GetSprite(2);

                    CameraRenderer = UnityHelper.CreateObject<SpriteRenderer>("CameraIcon", center.transform, Vector3.Zero);
                    CameraRenderer.sprite = DeviceIcon.GetSprite(4);

                    break;

                case 5:
                    VitalRenderer = UnityHelper.CreateObject<SpriteRenderer>("VitalIcon", center.transform, Vector3.Zero);
                    VitalRenderer.sprite = DeviceIcon.GetSprite(2);

                    TelescopeRenderer = UnityHelper.CreateObject<SpriteRenderer>("TelescoreIcon", center.transform, Vector3.Zero);
                    TelescopeRenderer.sprite = DeviceIcon.GetSprite(6);

                    break;
            }

            GameOperatorManager.Instance?.Subscribe<GameHudUpdateEvent>((ev) =>
            {
                if (MeetingHud.Instance)
                {
                    adjuster.localScale = new(0.8f, 0.8f, 1f);
                    adjuster.localPosition = new(-0.35f, -0.25f);
                }
                else
                {
                    adjuster.localScale = Vector3.One;
                    adjuster.localPosition = Vector3.Zero;
                }
                if (AdminRenderer != null)AdminRenderer.sprite = DeviceIcon.GetSprite(IsPlayerNearAdmin ? 1 : 0);
                if (VitalRenderer != null)VitalRenderer.sprite = DeviceIcon.GetSprite(IsPlayerNearVital ? 3 : 2);
                if (CameraRenderer != null)CameraRenderer.sprite = DeviceIcon.GetSprite(IsPlayerNearCamera ? 5 : 4);
                if (TelescopeRenderer != null) TelescopeRenderer.sprite = DeviceIcon.GetSprite(IsPlayerNearTelescope ? 7 : 6);
                if (DoorLogRenderer != null) DoorLogRenderer.sprite = DeviceIcon.GetSprite(IsPlayerNearDoorLog ? 9 : 8);

                List<SpriteRenderer> active = new List<SpriteRenderer>();

                if (AdminRenderer != null) active.Add(AdminRenderer);
                if (VitalRenderer != null) active.Add(VitalRenderer);
                if (CameraRenderer != null) active.Add(CameraRenderer);
                if (TelescopeRenderer != null) active.Add(TelescopeRenderer);
                if (DoorLogRenderer != null) active.Add(DoorLogRenderer);

                for (int i = 0; i < active.Count; i++)
                {
                    Vector3 basePos = GetDeviceIconPos(i, active.Count);

                    bool isNear =
                        (active[i] == AdminRenderer && IsPlayerNearAdmin) ||
                        (active[i] == VitalRenderer && IsPlayerNearVital) ||
                        (active[i] == CameraRenderer && IsPlayerNearCamera) ||
                        (active[i] == TelescopeRenderer && IsPlayerNearTelescope) ||
                        (active[i] == DoorLogRenderer && IsPlayerNearDoorLog);

                    active[i].transform.localPosition = basePos + GetShakeOffset(isNear);
                }
            }, this);
        }
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Sentinel.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DeviceIcon.AsLoader(0), "role.sentinelU.ability.device");
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private List<Vector2> AdminDevice = new();
        static private List<Vector2> VitalDevice = new();
        static private List<Vector2> CameraDevice = new();
        static private List<Vector2> TelescopeDevice = new();
        static private List<Vector2> DoorLogDevice = new();

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                DeviceArea();
                var guage = new SentinelViewer(MyPlayer).Register(this);
            }
        }

        void Check(GameUpdateEvent ev)
        {
            var allPlayers = GamePlayer.AllPlayers;

            foreach (var player in allPlayers)
            {
                if (player.IsDead || (ImpostorNoDevice && player.IsImpostor))
                {
                    DetectedPlayers.Remove(player);
                    continue;
                }

                Vector2 pos = player.VanillaPlayer.GetTruePosition();

                if (IsNearDevice(pos, AdminDevice) ||
                    IsNearDevice(pos, VitalDevice) ||
                    IsNearDevice(pos, CameraDevice) ||
                    IsNearDevice(pos, TelescopeDevice) ||
                    IsNearDevice(pos, DoorLogDevice))
                {
                    DetectedPlayers.Add(player);
                }
                else
                {
                    DetectedPlayers.Remove(player);
                }
            }

            IsPlayerNearAdmin = IsAnyPlayerNear(allPlayers, AdminDevice);
            IsPlayerNearVital = IsAnyPlayerNear(allPlayers, VitalDevice);
            IsPlayerNearCamera = IsAnyPlayerNear(allPlayers, CameraDevice);
            IsPlayerNearTelescope = IsAnyPlayerNear(allPlayers, TelescopeDevice);
            IsPlayerNearDoorLog = IsAnyPlayerNear(allPlayers, DoorLogDevice);
        }
        
        private bool IsNearDevice(Vector2 pos, List<Vector2> devices)
        {
            foreach (var devicePos in devices)
            {
                if (Vector2.Distance(pos, devicePos) <= 1.6f)
                    return true;
            }

            return false;
        }

        private bool IsAnyPlayerNear(IEnumerable<GamePlayer> players, List<Vector2> devices)
        {
            foreach (var player in players)
            {
                if (player.IsDead) continue;
                Vector2 pos = player.TruePosition;

                if (ImpostorNoDevice && player.IsImpostor) continue;

                foreach (var devicePos in devices)
                {
                    if (Vector2.Distance(pos, devicePos) <= 1.6f) return true;
                }
            }
            return false;
        }

        public static void DeviceArea()
        {
            AdminDevice.Clear();
            VitalDevice.Clear();
            CameraDevice.Clear();
            TelescopeDevice.Clear();
            DoorLogDevice.Clear();

            switch (NebulaAPI.AmongUs.MapId)
            {
                case 0: // Skeld
                    if (GeneralConfigurations.SkeldAdminOption.Value)
                        AdminDevice.Add(new Vector2(3.5f, -8.4f)); // アドミン
                    CameraDevice.Add(new Vector2(-13.0f, -2.7f)); // カメラ

                    break;

                case 1: // Mira
                    if (GeneralConfigurations.MiraAdminOption.Value)
                        AdminDevice.Add(new Vector2(21.0f, 19.2f)); // アドミン
                    DoorLogDevice.Add(new Vector2(16.1f, 5.6f)); // ドアログ

                    break;

                case 2: // Polus
                    if (GeneralConfigurations.PolusAdminOption.Value)
                    {
                        AdminDevice.Add(new Vector2(23.2f, -21.2f)); // 左アドミン
                        AdminDevice.Add(new Vector2(24.6f, -21.2f)); // 右アドミン
                    }

                    if (CustomPolusConfiguration.PolusVitalOption.Value)
                    {
                        VitalDevice.Add(new Vector2(30.27f, -6.65f)); //研究室バイタル
                    }
                    else
                    {
                        VitalDevice.Add(new Vector2(26.6f, -17.0f)); // オフィスバイタル
                    }
                    CameraDevice.Add(new Vector2(2.9f, -12.6f));  // カメラ

                    break;

                case 4: // Airship
                    if (GeneralConfigurations.AirshipCockpitAdminOption.Value)
                        AdminDevice.Add(new Vector2(-22.3f, 1.1f)); // コクピアドミン

                    if (GeneralConfigurations.AirshipRecordAdminOption.Value)
                        AdminDevice.Add(new Vector2(20.0f, 12.5f)); // アーカイブアドミン

                    VitalDevice.Add(new Vector2(25.3f, -7.9f)); // バイタル
                    CameraDevice.Add(new Vector2(8.1f, -10.1f)); // カメラ
                    break;

                case 5: // Fungle
                    VitalDevice.Add(new Vector2(-2.8f, -9.8f)); // バイタル
                    TelescopeDevice.Add(new Vector2(6.6f, 0.8f));   // 望遠鏡
                    break;
            }
        }

        private List<GamePlayer> DetectedPlayers = new();

        GamePlayer? LastKilled;
        void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (ev.Murderer == MyPlayer)
            {
                LastKilled = ev.Dead;
            }
        }

        [OnlyMyPlayer, Local]
        void OnKill(ResetKillCooldownLocalEvent ev)
        {
            if (LastKilled != null && DetectedPlayers.Contains(LastKilled))
            {
                ev.SetFixedCooldown(ShorteningCooldownInKilling);
            }
        }
    }
}