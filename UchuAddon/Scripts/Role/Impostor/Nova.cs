using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using Hori.Core;
using Il2CppInterop.Runtime.Injection;
using Nebula;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles.Abilities;
using Nebula.Utilities;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Helpers;
using Virial.Media;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

public class NovaU : DefinedSingleAbilityRoleTemplate<NovaU.Ability>, DefinedRole
{
    public NovaU() : base("novaU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [firingMode, EquipCooldownOption, BeamSizeOption, ChargeDurationOption])
    {
    }

    static readonly ValueConfiguration<int> firingMode = NebulaAPI.Configurations.Configuration("options.role.novaU.firingMode", ["options.role.novaU.firingMode.free", "options.role.novaU.firingMode.twoDirections", "options.role.novaU.firingMode.fourDirections"], 0);
    internal static readonly FloatConfiguration EquipCooldownOption = NebulaAPI.Configurations.Configuration("options.role.novaU.equipCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    internal static readonly FloatConfiguration BeamSizeOption = NebulaAPI.Configurations.Configuration("options.role.novaU.beamSize", (0.25f, 5f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static readonly FloatConfiguration ChargeDurationOption = NebulaAPI.Configurations.Configuration("options.role.novaU.chargeDuration", (0.5f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);
    static readonly FloatConfiguration BeamDurationOption = NebulaAPI.Configurations.Configuration("options.role.novaU.beamDuration", (0.5f, 10f, 0.25f), 5f, FloatConfigurationDecorator.Second);
    static readonly BoolConfiguration ResetKillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.novaU.resetKillCooldown", true);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    public static AssetBundle assetBundle = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("novaasset.bundle")!.AsStream()!.ReadBytes());

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Nova.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    static public NovaU MyRole = new();


    public class NovaBarrel : DirectionalEquipableAbility
    {
        static private Image barrelSprite = NebulaAPI.AddonAsset.GetResource("NovaBarrel.png")!.AsImage(100f)!;
        static private MultiImage barrelAnimationSprite = NebulaAPI.AddonAsset.GetResource("NovaBarrelAnim.png")!.AsMultiImage(3, 4, 100f)!;
        protected override float Size => 0.4f;
        protected override float Distance => 2f;

        public NovaBarrel(GamePlayer owner) : base(owner, false, "Nova", firingMode.GetValue() == 0 ? AimMode.Free : firingMode.GetValue() == 1 ? AimMode.Horizontal : AimMode.Cardinal)
        {
            Renderer.sprite = barrelSprite.GetSprite();
        }

        float? fireAngle = null;

        IEnumerator CoDisappear()
        {
            var wait = new WaitForSeconds(0.075f);
            for (int i = 0; i < 12; i++)
            {
                Renderer.sprite = barrelAnimationSprite.GetSprite(i);
                yield return wait;
            }
        }

        public void OnFire(float angle)
        {
            if (fireAngle.HasValue) return;

            fireAngle = angle;
            NebulaManager.Instance.StartCoroutine(CoDisappear().WrapToIl2Cpp());
        }
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public NovaBarrel? MyBarrel { get; private set; } = null;
        public SpriteRenderer? launchingSprite = null;

        VFXController? controller = null;
        bool launching = false;

        static readonly Image EquipButtonImage = NebulaAPI.AddonAsset.GetResource("NovaEquipButton.png")!.AsImage(115f)!;
        static readonly Image FireButtonImage = NebulaAPI.AddonAsset.GetResource("NovaFireButton.png")!.AsImage(115f)!;

        ModAbilityButton? equipButton = null, killButton = null;


        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool IPlayerAbility.HideKillButton => true;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.FixedAbility, EquipCooldownOption, "equip", EquipButtonImage).SetAsUsurpableButton(this);
                equipButton.Availability = button => !MyBarrel?.IsLocked ?? true && MyPlayer.CanMove;
                equipButton.OnClick = button =>
                {
                    RpcEquip.Invoke((MyPlayer, MyBarrel == null));
                };

                var fireButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, VirtualKeyInput.Kill, null, 0, "fire", FireButtonImage).SetAsMouseClickButton().SetAsUsurpableButton(this);
                fireButton.Availability = button => MyBarrel != null && !MyBarrel.IsLocked && MyPlayer.CanMove;
                fireButton.Visibility = button => MyBarrel != null;
                fireButton.OnClick = button =>
                {

                    launching = true;

                    MyPlayer.RemoveAttributeByTag("NovaU:equip");
                    MyPlayer.GainSpeedAttribute(0f, ChargeDurationOption * 1.16666666f + BeamDurationOption + 1, false, 100, "NovaU:launch");

                    RpcLaunch.Invoke((MyPlayer, MyPlayer.Unbox().MouseAngle));


                };

                killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, VirtualKeyInput.Kill, AmongUsLLImpl.Instance.VanillaKillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null, (player, button) =>
                {
                    MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    button.StartCoolDown();
                });
                killButton.Visibility = _ => MyBarrel == null && !launching && !MyPlayer.IsDead;

            }

        }

        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            CancelFire();
        }


        [OnlyMyPlayer]
        void OnDieOrDisconnect(PlayerDieOrDisconnectEvent ev)
        {
            CancelFire();
        }

        void CancelFire()
        {
            UnEquip();
            equipButton!.StartCoolDown();
            if (ResetKillCooldownOption) killButton!.StartCoolDown();
            MyPlayer.RemoveAttributeByTag("NovaU:launch");
            foreach (var controller in GameObject.FindObjectsOfType<VFXController>())
            {
                GameObject.Destroy(controller.gameObject);
            }

        }

        void EnterVent(PlayerVentEnterEvent ev) => CancelFire();

        IEnumerator CoFire()
        {
            if (controller == null || MyBarrel == null) yield break;
            yield return new WaitForSeconds(1f);
            controller.playVFX();

            yield return new WaitForSeconds(controller.chargingDuration * 1.16666666f);

            float elapsed = 0f;

            Vector2 beamOrigin = controller.gameObject.transform.position;
            float beamAngle = controller.rotation;

            while (elapsed < controller.beamDuration)
            {
                elapsed += Time.deltaTime;

                if (AmOwner)
                {
                    foreach (var target in NebulaGameManager.Instance!.AllPlayerInfo)
                    {
                        if (target.IsDead) continue;

                        Vector2 diff = target.TruePosition.ToUnityVector() - beamOrigin;

                        var vec = diff.Rotate(-beamAngle);

                        float beamWidth = 1.85f * BeamSizeOption;
                        if (vec.x > 0f && Mathf.Abs(vec.y) < beamWidth)
                        {
                            MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.RemoteKill);
                        }
                    }
                }

                yield return null;
            }

            UnEquip();
            equipButton!.StartCoolDown();
            if (ResetKillCooldownOption) killButton!.StartCoolDown();
            launching = false;
            GameObject.Destroy(controller.gameObject);
        }
        static RemoteProcess<(GamePlayer myPlayer, bool equip)> RpcEquip = new("NovaEquip", (message, _) =>
        {
            var ability = message.myPlayer.Role.GetAbility<Ability>()!;

            if (message.equip)
            {
                ability.Equip();

            }
            else
            {
                ability.UnEquip();
            }
        });

        void Equip()
        {
            equipButton?.SetLabel("unequip");
            MyBarrel = new NovaBarrel(MyPlayer).Register(this);
            if (MyPlayer.AmOwner) MyPlayer.GainSpeedAttribute(0f, 1000000f, false, 100, "NovaU:equip");
        }

        void UnEquip()
        {
            launching = false;
            equipButton?.SetLabel("equip");
            MyBarrel?.Release();
            MyBarrel = null;
            if (MyPlayer.AmOwner) MyPlayer.RemoveAttributeByTag("NovaU:equip");
        }


        static RemoteProcess<(GamePlayer myPlayer, float mouseAngle)> RpcLaunch = new("NovaLaunch", (message, _) =>
        {
            var ability = message.myPlayer.Role.GetAbility<Ability>()!;

            NebulaAsset.PlaySE(APICompat.GetSound("Nova_Equip"), message.myPlayer.Position, 1f, 2f);

            var beamObject = GameObject.Instantiate(assetBundle.LoadAsset("Beam VFX.prefab").Cast<GameObject>(), message.myPlayer.Position.ToUnityVector().AsVector3(0), new Quaternion(0, 0, 0, 0));
            ability.controller = beamObject.AddComponent<VFXController>();

            var allParticleSystems = beamObject.GetComponentsInChildren<ParticleSystem>(true);
            var allParticleSystemRenderers = beamObject.GetComponentsInChildren<ParticleSystemRenderer>(true);

            ability.controller.particleSystems = allParticleSystems;
            ability.controller.particleSystemRenderersNormalColor = allParticleSystemRenderers.Where(p => p.name == "Emission" || p.name == "Beam" || p.name == "Beam Entry").ToArray();
            ability.controller.particleSystemRenderersInvertColor = allParticleSystemRenderers.Where(p => p.name == "LittleSphere" || p.name == "Charging 1" || p.name == "Charging 2" || p.name == "Exploding beam" || p.name == "Exploding").ToArray();
            ability.controller.particleSystemRenderersParticleColor1 = allParticleSystemRenderers.Where(p => p.name == "Particle Trail" || p.name == "Particle Regroup").ToArray();
            ability.controller.particleSystemRenderersParticleColor2 = allParticleSystemRenderers.Where(p => p.name == "Particle Trail 2" || p.name == "Particle Explosion Emission").ToArray();
            ability.controller.particleSystemsDurationOnly = allParticleSystems.Where(p => p.name == "Particle Regroup" || p.name == "Particle Trail" || p.name == "Particle Trail 2").ToArray();
            ability.controller.particleSystemsLifeTimeIsDuration = allParticleSystems.Where(p => p.name == "Emission" || p.name == "LittleSphere").ToArray();
            ability.controller.particleSystemsLifeTimeIsHalfDuration = allParticleSystems.Where(p => p.name == "Charging 1" || p.name == "Charging 2").ToArray();
            ability.controller.particleSystemsLifeTimeIsLessHalfDuration = allParticleSystems.Where(p => p.name == "Exploding").ToArray();
            ability.controller.SetBeamDurationParticleSystems = allParticleSystems.Where(p => p.name == "Beam" || p.name == "Beam Entry").ToArray();
            ability.controller.particleSystemsChargingDelays = allParticleSystems.Where(p => p.name == "Charging 1" || p.name == "Charging 2").ToArray();
            ability.controller.particleSystemsexplodingDelays = allParticleSystems.Where(p => p.name == "Exploding").ToArray();
            ability.controller.particleSystemsSetBeamDelays = allParticleSystems.Where(p => p.name == "Beam" || p.name == "Beam Entry" || p.name == "Exploding beam" || p.name == "Particle Explosion Emission").ToArray();
            ability.controller.beamParticleSystem = allParticleSystems.First(p => p.name == "Beam");
            ability.controller.beamEntryParticleSystem = allParticleSystems.First(p => p.name == "Beam Entry");

            ability.controller.Color1 = Color.white;
            ability.controller.Color2 = Color.black;

            ability.controller.chargingDuration = ChargeDurationOption;
            ability.controller.beamDuration = BeamDurationOption;


            float rotation = 0;

            switch (firingMode.GetValue())
            {
                case 0:
                    rotation = message.mouseAngle;
                    break;

                case 1:
                    rotation = Mathn.Cos(message.mouseAngle) >= 0f ? 0f : MathF.PI;
                    break;

                case 2:
                    float tau = MathF.PI * 2f;
                    float normalized = ((message.mouseAngle % tau) + tau) % tau;
                    float snapped = MathF.Round(normalized / (MathF.PI / 2f))
                                    * (MathF.PI / 2f);
                    if (snapped >= tau) snapped -= tau;
                    rotation = snapped;
                    break;
            }
            ability.controller.rotation = rotation * 180f / MathF.PI;
            beamObject.transform.localPosition += (new Vector2(Mathn.Cos(rotation), Mathn.Sin(rotation)) * 1.85f).AsVector3(-4.9f);
            beamObject.transform.Find("----Beam----/Beam Entry").localPosition += (new Vector2(Mathn.Cos(rotation), Mathn.Sin(rotation)) * ((1f / 8f) * NovaU.BeamSizeOption - (1f / 8f))).AsVector3(0);

            ability.MyBarrel!.Lock(message.mouseAngle);
            ability.MyBarrel!.OnFire(message.mouseAngle);

            ability.controller.StartCoroutine(ability.CoFire().WrapToIl2Cpp());

        });
    }
}

public enum AimMode
{
    Free,
    Horizontal,
    Cardinal,
}

public class DirectionalEquipableAbility : EquipableAbility
{
    public AimMode Mode { get; set; }
    public bool IsLocked { get; private set; } = false;

    private float lockedAngle = 0f;

    public DirectionalEquipableAbility(GamePlayer owner, bool canSeeInShadow, string name, AimMode mode = AimMode.Free)
        : base(owner, canSeeInShadow, name)
    {
        Mode = mode;
    }

    public void Lock(float angle)
    {
        lockedAngle = FixAngle(angle);
        IsLocked = true;
    }

    protected override float FixAngle(float angle)
    {
        if (IsLocked) return lockedAngle;
        return Mode switch
        {
            AimMode.Horizontal => SnapToHorizontal(angle),
            AimMode.Cardinal => SnapToCardinal(angle),
            _ => angle, // Free: そのまま
        };
    }

    // 左右のみ：0度（右）か180度（左）に吸着
    private static float SnapToHorizontal(float angle)
    {
        // cosが正なら右(0)、負なら左(π)
        return Mathn.Cos(angle) >= 0f ? 0f : MathF.PI;
    }

    // 上下左右のみ：90度刻みで最も近い方向に吸着
    private static float SnapToCardinal(float angle)
    {
        float tau = MathF.PI * 2f;
        float normalized = ((angle % tau) + tau) % tau;
        float snapped = MathF.Round(normalized / (MathF.PI / 2f))
                        * (MathF.PI / 2f);
        if (snapped >= tau) snapped -= tau;
        return snapped;
    }

}

public class VFXController : MonoBehaviour
{
    internal ParticleSystem[] particleSystems = null!;
    internal ParticleSystemRenderer[] particleSystemRenderersNormalColor = null!;
    internal ParticleSystemRenderer[] particleSystemRenderersInvertColor = null!;
    internal ParticleSystemRenderer[] particleSystemRenderersParticleColor1 = null!;
    internal ParticleSystemRenderer[] particleSystemRenderersParticleColor2 = null!;
    internal ParticleSystem[] particleSystemsDurationOnly = null!;
    internal ParticleSystem[] particleSystemsLifeTimeIsDuration = null!;
    internal ParticleSystem[] particleSystemsLifeTimeIsHalfDuration = null!;
    internal ParticleSystem[] particleSystemsLifeTimeIsLessHalfDuration = null!;

    internal ParticleSystem[] SetBeamDurationParticleSystems = null!;
    internal ParticleSystem[] particleSystemsChargingDelays = null!;
    internal ParticleSystem[] particleSystemsexplodingDelays = null!;
    internal ParticleSystem[] particleSystemsSetBeamDelays = null!;
    internal ParticleSystem beamParticleSystem = null!;
    internal ParticleSystem beamEntryParticleSystem = null!;

    public Color Color1 = Color.white;
    public Color Color2 = Color.black;
    public float chargingDuration = 3f;
    public float beamDuration = 5f;
    private string colorProperty1 = "_Color1";
    private string colorProperty2 = "_Color2";
    float delay;

    internal float rotation;

    private MaterialPropertyBlock propertyBlock = null!;

    static VFXController() => ClassInjector.RegisterTypeInIl2Cpp<VFXController>();

    void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        delay = 1.16666666f * chargingDuration;
        SetLifeTimeAndDuration();
        SetColor(Color1, Color2);
        setRotation();
        UpdateBeamSize();
    }


    public void UpdateBeamSize()
    {
        foreach (var ps in particleSystems)
        {
            ps.transform.localScale *= 1.5f;
        }
        beamParticleSystem.transform.localScale *= 6 * NovaU.BeamSizeOption;
        beamEntryParticleSystem.transform.localScale *= 6 * NovaU.BeamSizeOption;
    }

    public void SetLifeTimeAndDuration()
    {
        foreach (var ps in particleSystemsLifeTimeIsDuration)
        {
            var main = ps.main;
            main.startLifetimeMultiplier = chargingDuration;
        }
        foreach (var ps in particleSystemsLifeTimeIsHalfDuration)
        {
            var main = ps.main;
            main.startLifetimeMultiplier = chargingDuration * 0.433333f;
        }
        foreach (var ps in particleSystemsLifeTimeIsLessHalfDuration)
        {
            var main = ps.main;
            main.startLifetimeMultiplier = chargingDuration * 0.266666f;
        }
        foreach (var ps in particleSystemsChargingDelays)
        {
            var main = ps.main;
            main.startDelayMultiplier = chargingDuration * 0.733333f;
        }
        foreach (var ps in particleSystemsexplodingDelays)
        {
            var main = ps.main;
            main.startDelayMultiplier = chargingDuration * 0.78333333333f;
        }
        foreach (var ps in particleSystemsSetBeamDelays)
        {
            var main = ps.main;
            main.startDelayMultiplier = delay;
        }
        foreach (var ps in SetBeamDurationParticleSystems)
        {
            var main = ps.main;
            main.startLifetimeMultiplier = beamDuration;
        }


    }

    public void SetColor(Color color1, Color color2)
    {

        foreach (var r in particleSystemRenderersNormalColor)
        {
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty1, color1);
            propertyBlock.SetColor(colorProperty2, color2);
            r.SetPropertyBlock(propertyBlock);
        }
        foreach (var r in particleSystemRenderersInvertColor)
        {
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty1, color2);
            propertyBlock.SetColor(colorProperty2, color1);
            r.SetPropertyBlock(propertyBlock);
        }

        foreach (var r in particleSystemRenderersParticleColor1)
        {
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty1, color1);
            propertyBlock.SetColor(colorProperty2, color2);
            r.SetPropertyBlock(propertyBlock);
        }

        foreach (var r in particleSystemRenderersParticleColor2)
        {
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty1, color2);
            propertyBlock.SetColor(colorProperty2, color1);
            r.SetPropertyBlock(propertyBlock);
        }


    }


    IEnumerator SoundEffectBeamFiring()
    {
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.35f;
        audioSource.clip = APICompat.GetSound("Nova_Charge");
        audioSource.Play();

        yield return new WaitForSeconds(delay);

        audioSource.Stop();
        audioSource.loop = false;

        audioSource.clip = APICompat.GetSound("Nova_Beam");
        audioSource.Play();
    }



    public void playVFX()
    {
        StartCoroutine(SoundEffectBeamFiring().WrapToIl2Cpp());
        if (particleSystems != null)
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
    }

    public void setRotation()
    {
        foreach (var p in particleSystems)
        {
            var transform = p.transform;
            transform.eulerAngles = new Vector3(0, 0, rotation);
        }
    }


}
