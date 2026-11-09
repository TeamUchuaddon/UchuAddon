using Hori.Core;
using Nebula;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

public class HypersawU : DefinedSingleAbilityRoleTemplate<HypersawU.Ability>, DefinedRole, IAssignableDocument
{
    public HypersawU() : base("hypersawU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SawSizeOption, SawCooldown, SawDuration, OnSawSpeed, SyncKillCooldownOption, SeStrengthOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Hypersaw.png")!.AsImage(115f);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SawSizeOption = NebulaAPI.Configurations.Configuration("options.role.hypersawU.sawSize", (0.25f, 3f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration SawCooldown = NebulaAPI.Configurations.Configuration("options.role.hypersawU.sawCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SawDuration = NebulaAPI.Configurations.Configuration("options.role.hypersawU.sawDuration", (2.5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration OnSawSpeed = NebulaAPI.Configurations.Configuration("options.role.hypersawU.insaweffectspeed", (0.25f, 1f, 0.25f), 0.75f, FloatConfigurationDecorator.Ratio);
    static readonly BoolConfiguration SyncKillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.hypersawU.syncKillCooldown", true);
    static private FloatConfiguration SeStrengthOption = NebulaAPI.Configurations.Configuration("options.role.hypersawU.seStrength", (1f, 5f, 0.5f), 2f, FloatConfigurationDecorator.Ratio);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Hypersaw.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public HypersawU MyRole = new();

    static private Virial.Media.Image EquipImage = NebulaAPI.AddonAsset.GetResource("HypersawButton.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(EquipImage, "role.hypersawU.ability.chainsaw");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%SPEED%", Language.Translate(OnSawSpeed < 1f ? "role.hypersawU.ability.speed.decel" : "role.hypersawU.ability.speed.nodecel"));
    }

    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class HypersawObject : NebulaSyncStandardObject, IGameOperator
    {
        public static readonly string MyTag = "HypersawObject";

        static private Virial.Media.MultiImage sawAnimSprite = NebulaAPI.AddonAsset.GetResource("ChainsawAnim.png")!.AsMultiImage(3, 1, 400f)!;

        private float animationTimer = 0f;
        private int currentFrameIndex = 0;
        private const float AnimationSpeed = 12f;
        private const float ShakeAmount = 0.04f;
        private Vector2? basePosition = null;
        private AudioSource? sawSE = null;
        private AudioSource? sawFricSE = null;

        public HypersawObject(PlayerControl owner) : base(owner.GetTruePosition(), ZOption.Front, false, sawAnimSprite.GetSprite(0))
        {
            if (Constants.ShouldPlaySfx() && MyRenderer != null)
            {
                sawSE = NebulaAsset.PlaySE(NebulaAudioClip.Drill, Vector2.Zero, 0.95f, SeStrengthOption, 1f, true);
                if (sawSE != null)
                {
                    sawSE.pitch = 1.23f;
                    sawSE.volume = 0.7f;
                    sawSE.transform.SetParent(MyRenderer.transform);
                    sawSE.transform.localPosition = Vector3.Zero;
                }

                sawFricSE = NebulaAsset.PlaySE(NebulaAudioClip.DrillFric, Vector2.Zero, 0.95f, SeStrengthOption, 1f, true);
                if (sawFricSE != null)
                {
                    sawFricSE.pitch = 1.23f;
                    sawFricSE.volume = 0.7f;
                    sawFricSE.transform.SetParent(MyRenderer.transform);
                    sawFricSE.transform.localPosition = Vector3.Zero;
                }
            }
        }

        void HudUpdate(GameHudUpdateEvent ev)
        {
            if (Owner == null) return;

            if (AmOwner) Owner.Unbox().RequireUpdateMouseAngle();

            var angle = Owner.Unbox().MouseAngle;
            MyRenderer.transform.localEulerAngles = new Vector3(0, 0, angle * 180f / Mathn.PI);
            MyRenderer.flipY = Mathn.Cos(angle) < 0f;
            if (basePosition == null) basePosition = (Vector2)MyRenderer.transform.position;

            var pos = Owner.VanillaPlayer.transform.position + new UnityEngine.Vector3(Mathn.Cos(angle), Mathn.Sin(angle), -1f) * 0.67f;
            var diff = ((Vector2)pos - basePosition.Value) * Time.deltaTime * 7.5f;
            basePosition += diff;

            var shake = new Vector2(
                UnityEngine.Random.Range(-ShakeAmount, ShakeAmount),
                UnityEngine.Random.Range(-ShakeAmount, ShakeAmount));
            Position = basePosition.Value + shake;
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            animationTimer += Time.deltaTime;
            float frameTime = 1f / AnimationSpeed;

            if (animationTimer >= frameTime)
            {
                animationTimer -= frameTime;
                currentFrameIndex = (currentFrameIndex + 1) % 3;

                var sprite = sawAnimSprite.GetSprite(currentFrameIndex);
                if (sprite != null && MyRenderer != null)
                {
                    MyRenderer.sprite = sprite;
                }
            }
        }

        static HypersawObject()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new HypersawObject(Helpers.GetPlayer((byte)args[0])!));
        }
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        private ModAbilityButton? equipButton = null, killButton = null;
        private bool isEquipped = false;
        private HypersawObject? mySaw = null;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SawCooldown, "equip", EquipImage).SetAsUsurpableButton(this);

                equipButton.Availability = (button) => MyPlayer.CanMove;
                equipButton.Visibility = (button) => !MyPlayer.IsDead;
                equipButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);

                equipButton.OnClick = (button) =>
                {
                    if (equipButton.IsInEffect)
                    {
                        equipButton.InterruptEffect();
                        MyPlayer.RealPlayer.RemoveAttribute(PlayerAttributes.Decel);
                    }
                    else
                    {
                        equipButton.StartEffect();
                    }
                };
                equipButton.OnEffectStart = (button) =>
                {
                    EquipSaw();
                    MyPlayer.GainSpeedAttribute(OnSawSpeed, SawDuration, false, 1, "hypersaw::decel");
                };
                equipButton.OnEffectEnd = (button) =>
                {
                    UnequipSaw();
                    equipButton.StartCoolDown();
                    if (SyncKillCooldownOption) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                equipButton.SetLabel("equip");
                equipButton.EffectTimer = NebulaAPI.Modules.Timer(this, SawDuration);

                equipButton.OnBroken = (button) =>
                {
                    UnequipSaw();
                    Snatcher.RewindKillCooldown();
                };
            }
        }


        void EquipSaw()
        {
            isEquipped = true;
            equipButton?.SetLabel("unequip");
            mySaw = NebulaSyncObject.RpcInstantiate(HypersawObject.MyTag, [(float)PlayerControl.LocalPlayer.PlayerId]).SyncObject as HypersawObject;
        }

        void UnequipSaw()
        {
            isEquipped = false;
            equipButton?.SetLabel("equip");

            if (mySaw != null)
            {
                NebulaSyncObject.RpcDestroy(mySaw.ObjectId);
                mySaw = null;
            }
        }

        [Local]
        void OnHudUpdate(GameHudUpdateEvent ev)
        {
            if (!AmOwner || !isEquipped || MeetingHud.Instance) return;

            var angle = MyPlayer.Unbox().MouseAngle;

            var offset = new Vector2(Mathn.Cos(angle), Mathn.Sin(angle)) * 0.67f;
            var sawTipPos = (Vector2)MyPlayer.TruePosition + offset;

            var size = SawSizeOption;

            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || !p.IsActive) continue;
                if (!MyPlayer.RealPlayer.CanKill(p.RealPlayer)) continue;
                if (p.IsDived || p.Logic.InVent || p.WillDie) continue;

                if (!Helpers.AnyNonTriggersBetween(p.TruePosition, sawTipPos, out var diff, Constants.ShipAndAllObjectsMask) && diff.Magnitude < size * 0.4f)
                {
                    if (p.IsInvisible) continue;

                    if (GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, p, new(IsKillInteraction: true))).IsCanceled ?? false) continue;

                    MyPlayer.MurderPlayer(p, PlayerState.Dead, EventDetail.Kill, KillParameter.RemoteKill);
                }
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            UnequipSaw();
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            UnequipSaw();
        }

        void IGameOperator.OnReleased()
        {
            UnequipSaw();
        }

        [Local]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.PlayerId == ev.Dead.PlayerId || ev.Murderer != MyPlayer) return;
            if (SyncKillCooldownOption)
            {
                equipButton?.StartCoolDown();
            }
        }
    }
}