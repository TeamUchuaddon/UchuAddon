using BepInEx.Unity.IL2CPP.Utils;
using Hori.Core;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Utilities;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Image = Virial.Media.Image;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;

namespace Hori.Scripts.Role.Crewmate;

[NebulaRPCHolder]
public class EvilObserverU : DefinedSingleAbilityRoleTemplate<EvilObserverU.Ability>, DefinedRole
{
    private EvilObserverU() : base("evilObserverU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [MaxCameraCountOption, PlaceCoolDownOption, CanSeeCameraOtherPlayerOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Observer.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagFunny);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    static private IntegerConfiguration MaxCameraCountOption = NebulaAPI.Configurations.Configuration("options.role.observerU.maxCameraCount", (1, 10), 3);
    static private FloatConfiguration PlaceCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.observerU.placeCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration CanSeeCameraOtherPlayerOption = NebulaAPI.Configurations.Configuration("options.role.observerU.canSeeCameraOtherPlayer", true);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Observer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public EvilObserverU MyRole = new EvilObserverU();
    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class Camera : NebulaSyncStandardObject
    {
        public static MultiImage cameraSprite = NebulaAPI.AddonAsset.GetResource("ObserverCameraObject.png")!.AsMultiImage(4, 1, 150f)!;
        public static string MyGlobalTag = "CameraGloabl";
        public static string MyLocalTag = "CameraLocal";

        float indexT = 0f;
        int index = 0;
        public Camera(UnityEngine.Vector2 pos, bool reverse, bool isLocal) : base(pos, ZOption.Just, true, cameraSprite.GetSprite(0), isLocal)
        {
            MyRenderer.flipX = reverse;
            MyBehaviour = MyRenderer.gameObject.AddComponent<EmptyBehaviour>();
        }

        public bool Flipped { get => MyRenderer.flipX; set => MyRenderer.flipX = value; }
        public EmptyBehaviour MyBehaviour = null!;

        static Camera()
        {
            NebulaSyncObject.RegisterInstantiater(MyGlobalTag, (args) => new Camera(new UnityEngine.Vector2(args[0], args[1]), args[2] < 0f, false));
            NebulaSyncObject.RegisterInstantiater(MyLocalTag, (args) => new Camera(new UnityEngine.Vector2(args[0], args[1]), args[2] < 0f, true));
        }
        void OnUpdate(GameUpdateEvent ev)
        {
            indexT -= Time.deltaTime;
            if (indexT < 0f)
            {
                indexT = 0.13f;
                Sprite = Camera.cameraSprite.GetSprite(index % 4);
                index++;

            }
        }

    }
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueMapAbility;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        static private Image placeButtonSprite = NebulaAPI.AddonAsset.GetResource("ObserverPlaceButton.png")!.AsImage(115f)!;
        static private Image monitorButtonSprite = NebulaAPI.AddonAsset.GetResource("ObserverMonitorButton.png")!.AsImage(115f)!;


        static private Image nextButtonSprite = NebulaAPI.AddonAsset.GetResource("CameraNextButton.png")!.AsImage(115f)!;
        static private Image selfButtonSprite = NebulaAPI.AddonAsset.GetResource("CameraSelfButton.png")!.AsImage(115f)!;

        public Camera[] MyGlobalCamera = new Camera[MaxCameraCountOption];
        public Camera[] MyLocalCamera = new Camera[MaxCameraCountOption];
        int nowGlobalCameraCount = 0;
        int nowLocalCameraCount = 0;
        int watchingCameraNumber = -1;

        void IGameOperator.OnReleased()
        {
            watchingCameraNumber = -1;
            AmongUsUtil.SetCamTarget(null);
        }


        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {

            if (AmOwner)
            {

                int left = MaxCameraCountOption;

                ModAbilityButton monitorButton = null!, nextButton = null!, selfButton = null!;

                var placeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, PlaceCoolDownOption, "place", placeButtonSprite,
                    null, _ => true).SetAsUsurpableButton(this);
                placeButton.Visibility = (button) => left != 0 && watchingCameraNumber == -1 && !MyPlayer.IsDead;
                placeButton.OnClick = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        left--;
                        placeButton.UpdateUsesIcon(left.ToString());
                        MyLocalCamera[nowLocalCameraCount] = (NebulaSyncObject.LocalInstantiate(Camera.MyLocalTag, [
                        PlayerControl.LocalPlayer.transform.localPosition.x,
                        PlayerControl.LocalPlayer.transform.localPosition.y,
                        PlayerControl.LocalPlayer.cosmetics.FlipX ? 1f : -1f
                        ]).SyncObject as Camera);
                        nowLocalCameraCount++;
                    });
                    placeButton.StartCoolDown();
                };

                placeButton.ShowUsesIcon(0, left.ToString());

                monitorButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                    0f, "monitor", monitorButtonSprite).SetAsUsurpableButton(this);
                monitorButton.Visibility = (button) => watchingCameraNumber == -1 && nowGlobalCameraCount != 0 && !MyPlayer.IsDead;
                monitorButton.OnClick = (button) =>
                {
                    watchingCameraNumber = 0;
                    AmongUsUtil.SetCamTarget(MyGlobalCamera[watchingCameraNumber].MyBehaviour);
                };

                selfButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                0f, "self", selfButtonSprite).SetAsUsurpableButton(this);
                selfButton.Availability = (button) => MyGlobalCamera != null;
                selfButton.Visibility = (button) => watchingCameraNumber != -1;
                selfButton.OnClick = (button) =>
                {
                    watchingCameraNumber = -1;
                    AmongUsUtil.SetCamTarget(null);
                };
                selfButton.OnBroken = (button) => AmongUsUtil.SetCamTarget(null);

                nextButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                0f, "next", nextButtonSprite).SetAsUsurpableButton(this);
                nextButton.Availability = (button) => MyGlobalCamera != null;
                nextButton.Visibility = (button) => watchingCameraNumber != -1 && nowGlobalCameraCount != 1;
                nextButton.OnClick = (button) =>
                {
                    watchingCameraNumber++;
                    if (watchingCameraNumber == nowGlobalCameraCount) watchingCameraNumber = 0;

                    AmongUsUtil.SetCamTarget(MyGlobalCamera[watchingCameraNumber].MyBehaviour);
                };
                nextButton.OnBroken = (button) => AmongUsUtil.SetCamTarget(null);

            }
        }

        void OnMeetingStart(MeetingStartEvent ev)
        {
            AmongUsUtil.SetCamTarget(null);
            watchingCameraNumber = -1;
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            for (int i = 0; i < MaxCameraCountOption; i++)
            {
                if (MyLocalCamera[i] != null && MyGlobalCamera[i] == null)
                {

                    if (CanSeeCameraOtherPlayerOption) MyGlobalCamera[i] = (NebulaSyncObject.RpcInstantiate(Camera.MyGlobalTag, [
                        MyLocalCamera[i].Position.x,
                        MyLocalCamera[i].Position.y,
                        MyLocalCamera[i].Flipped ? -1f : 1f
                        ]).SyncObject as Camera);

                    else MyGlobalCamera[i] = (NebulaSyncObject.LocalInstantiate(Camera.MyGlobalTag, [
                        MyLocalCamera[i].Position.x,
                        MyLocalCamera[i].Position.y,
                        MyLocalCamera[i].Flipped ? 1f : -1f
                        ]).SyncObject as Camera);
                    NebulaSyncObject.LocalDestroy(MyLocalCamera[i].ObjectId);
                    nowGlobalCameraCount++;
                }
            }

            //RpcAnimateCamera.Invoke(MyGlobalCamera);
        }

        [OnlyMyPlayer]
        void PlayerDie(PlayerDieEvent ev)
        {
            watchingCameraNumber = -1;
            AmongUsUtil.SetCamTarget(null);
        }

        bool IPlayerAbility.EyesightIgnoreWalls => watchingCameraNumber != -1;
        bool IPlayerAbility.IgnoreBlackout => watchingCameraNumber != -1;
    }

}