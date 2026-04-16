using BepInEx.Unity.IL2CPP.Utils;
using Hori.Core;
using Hori.Scripts.Role.Impostor;
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
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Image = Virial.Media.Image;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;

namespace Hori.Scripts.Role.Crewmate;

[NebulaRPCHolder]
public class NiceObserverU : DefinedSingleAbilityRoleTemplate<NiceObserverU.Ability>, DefinedRole
{
    private NiceObserverU() : base("niceObserverU", new(0, 103, 144), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [MaxCameraCountOption, PlaceCoolDownOption, CanSeeCameraOtherPlayerOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagFunny);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;


    static private IntegerConfiguration MaxCameraCountOption = NebulaAPI.Configurations.Configuration("options.role.observerU.maxCameraCount", (1, 10), 3);
    static private FloatConfiguration PlaceCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.observerU.placeCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration CanSeeCameraOtherPlayerOption = NebulaAPI.Configurations.Configuration("options.role.observerU.canSeeCameraOtherPlayer", true);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Observer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public NiceObserverU MyRole = new NiceObserverU();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        static private Image placeButtonSprite = NebulaAPI.AddonAsset.GetResource("ObserverPlaceButtonN.png")!.AsImage(115f)!;
        static private Image monitorButtonSprite = NebulaAPI.AddonAsset.GetResource("ObserverMonitorButtonN.png")!.AsImage(115f)!;


        static private Image nextButtonSprite = NebulaAPI.AddonAsset.GetResource("CameraNextButton.png")!.AsImage(115f)!;
        static private Image selfButtonSprite = NebulaAPI.AddonAsset.GetResource("CameraSelfButton.png")!.AsImage(115f)!;

        public EvilObserverU.Camera[] MyGlobalCamera = new EvilObserverU.Camera[MaxCameraCountOption];
        public EvilObserverU.Camera[] MyLocalCamera = new EvilObserverU.Camera[MaxCameraCountOption];
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
                        MyLocalCamera[nowLocalCameraCount] = (NebulaSyncObject.LocalInstantiate(EvilObserverU.Camera.MyLocalTag, [
                        PlayerControl.LocalPlayer.transform.localPosition.x,
                        PlayerControl.LocalPlayer.transform.localPosition.y,
                        PlayerControl.LocalPlayer.cosmetics.FlipX ? 1f : -1f
                        ]).SyncObject as EvilObserverU.Camera);
                        nowLocalCameraCount++;
                    });
                    placeButton.StartCoolDown();
                };

                placeButton.ShowUsesIcon(3, left.ToString());

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

                    if (CanSeeCameraOtherPlayerOption) MyGlobalCamera[i] = (NebulaSyncObject.RpcInstantiate(EvilObserverU.Camera.MyGlobalTag, [
                        MyLocalCamera[i].Position.x,
                        MyLocalCamera[i].Position.y,
                        MyLocalCamera[i].Flipped ? -1f : 1f
                        ]).SyncObject as EvilObserverU.Camera);

                    else MyGlobalCamera[i] = (NebulaSyncObject.LocalInstantiate(EvilObserverU.Camera.MyGlobalTag, [
                        MyLocalCamera[i].Position.x,
                        MyLocalCamera[i].Position.y,
                        MyLocalCamera[i].Flipped ? 1f : -1f
                        ]).SyncObject as EvilObserverU.Camera);
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