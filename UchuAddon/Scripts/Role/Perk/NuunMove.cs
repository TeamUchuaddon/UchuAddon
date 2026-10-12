using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class NuunMoveU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.nuunMoveU.cooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("nuunMoveU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
    new PerkDefinition("nuunMoveU", 4, 13, Virial.Color.ImpostorColor).CooldownText("%CD%", () => Cooldown), (def, instance) => new NuunMoveU(def, instance), [CooldownOption]);

    public NuunMoveU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    private GameTimer cooldownTimer;

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (cooldownTimer.IsProgressing) return;
        if (GeneralConfigurations.AirshipOneWayMeetingRoomOption.Value) return;
        RpcNuunUchu.Invoke();

        cooldownTimer.Start(Cooldown);
    }

    static RemoteProcess RpcNuunUchu = new RemoteProcess("RpcNuun_Uchu", (_) => //Thx SNR
    {
        AirshipStatus airshipStatus = GameObject.FindObjectOfType<AirshipStatus>();
        if (airshipStatus)
        {
            var platform = airshipStatus.GapPlatform;

            platform.StopAllCoroutines();
            platform.StartCoroutine(MovePlatformSimple(platform).WrapToIl2Cpp());
        }
    });

    private static IEnumerator MovePlatformSimple(MovingPlatformBehaviour platform)
    {
        bool isTargetOn = platform.Target != null;

        if (platform.Target == null)
        {
            platform.Target = PlayerControl.LocalPlayer;
        }

        Vector3 usePos = platform.IsLeft ? platform.LeftUsePosition : platform.RightUsePosition;
        Vector3 useTargetPos = (!platform.IsLeft) ? platform.LeftUsePosition : platform.RightUsePosition;
        Vector3 sourcePos = platform.IsLeft ? platform.LeftPosition : platform.RightPosition;
        Vector3 targetPos = (!platform.IsLeft) ? platform.LeftPosition : platform.RightPosition;

        Vector3 worldUseTargetPos = platform.transform.parent.TransformPoint(useTargetPos);
        Vector3 worldTargetPos = platform.transform.parent.TransformPoint(targetPos);

        if (Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.StopNamedSound("PlatformMoving");
            SoundManager.Instance.PlayDynamicSound("PlatformMoving", platform.MovingSound, true, (DynamicSound.GetDynamicsFunction)platform.SoundDynamics, SoundManager.instance.sfxMixer);
        }

        platform.IsLeft = !platform.IsLeft;

        if (isTargetOn)
        {
            yield return
                Effects.All(Effects.Slide2D(platform.transform, platform.transform.localPosition, targetPos, platform.Target.MyPhysics.Speed),
                Effects.Slide2DWorld(platform.Target.transform, platform.transform.position + new Vector3(0, 0.3f), worldTargetPos + new Vector3(0, 0.3f), platform.Target.MyPhysics.Speed));
        }
        else
        {
            yield return
                Effects.All(Effects.Slide2D(platform.transform, platform.transform.localPosition, targetPos, PlayerControl.LocalPlayer.MyPhysics.Speed));
        }

        if (Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.StopNamedSound("PlatformMoving");
        }


        if (isTargetOn)
        {
            platform.Target.MyPhysics.enabled = true;
            yield return platform.Target.MyPhysics.WalkPlayerTo(worldUseTargetPos);
            platform.Target.SetPetPosition(platform.Target.transform.position);
            yield return Effects.Wait(0.1f);

            platform.Target.inMovingPlat = false;
            platform.Target.Collider.enabled = true;
            platform.Target.moveable = true;
            platform.Target.NetTransform.enabled = true;
        }

        platform.Target = null;
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        bool canUse = !cooldownTimer.IsProgressing && !GeneralConfigurations.AirshipOneWayMeetingRoomOption.Value;

        PerkInstance.SetDisplayColor(canUse ? Color.white : Color.gray);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}