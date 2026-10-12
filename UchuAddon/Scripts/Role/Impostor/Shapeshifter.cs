using AsmResolver.PE;
using HarmonyLib;
using Nebula;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Linq;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Helpers;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Impostor;

public class ShapeshifterU : DefinedSingleAbilityRoleTemplate<ShapeshifterU.Ability>, DefinedRole, IAssignableDocument
{
    public ShapeshifterU() : base("shapeshifterU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam,
        [ShapeshiftCoolDownOption, ShapeshiftDurationOption])
    {
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration ShapeshiftCoolDownOption = NebulaAPI.Configurations.Configuration(
        "options.role.shapeshifterU.shapeshiftCoolDown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration ShapeshiftDurationOption = NebulaAPI.Configurations.Configuration(
        "options.role.shapeshifterU.shapeshiftDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Shapeshifter.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image), "role.shapeshifterU.ability.shapeshift");
    }

    static public ShapeshifterU MyRole = new();

    static ShapeshifterU()
    {
        var shapeshiftMethod = typeof(ShapeshifterMinigame).GetMethod("Shapeshift");
        if (shapeshiftMethod != null)
        {
            var harmony = new Harmony("shapeshifterU");
            harmony.Patch(shapeshiftMethod,
                prefix: new HarmonyMethod(typeof(Ability).GetMethod(nameof(Ability.OnShapeshiftPanelSelected))));
        }
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        private ModAbilityButton? shapeshiftButton = null;
        private const string OutfitTag = "SampleShapeshift";
        static internal Ability? LocalInstance = null;
        private bool revertWithoutAnimation = false;

        static private RemoteProcess<(byte shifterId, byte targetId, bool doAnimation, bool isTransforming)> ShapeshiftAnimationRpc = new(
            "sampleImpostorShapeshiftAnimRPC", (m, _) =>
            {
                var player = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.PlayerId == m.shifterId);
                if (player == null) return;

                Action? midAnimExtra = null;
                if (player.AmOwner)
                {
                    midAnimExtra = () =>
                    {
                        if (m.isTransforming)
                        {
                            var targetPlayer = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.PlayerId == m.targetId);
                            if (targetPlayer != null)
                            {
                                var outfit = targetPlayer.GetOutfit(OutfitPriority.TransformedThrethold);
                                PlayerModInfo.RpcAddOutfit.Invoke(new(AmongUsLLImpl.LocalPlayer.PlayerId, new(outfit, OutfitTag, OutfitPriority.Morph, true)));
                            }
                        }
                        else
                        {
                            PlayerModInfo.RpcRemoveOutfit.Invoke(new(AmongUsLLImpl.LocalPlayer.PlayerId, OutfitTag));
                        }
                    };
                }

                PlayShapeshiftAnimation(player, m.doAnimation, midAnimExtra, () =>
                {
                    if (player.AmOwner && LocalInstance != null)
                    {
                        LocalInstance.shapeshiftButton?.SetLabel(m.isTransforming ? "shapeshifterU.UnShift" : "shapeshifterU.shapeshift");
                    }

                    if (m.isTransforming && player.AmOwner && LocalInstance != null)
                    {
                        LocalInstance.shapeshiftButton?.ToggleEffect();
                    }
                });
            });

        public static bool OnShapeshiftPanelSelected(ShapeshifterMinigame __instance, PlayerControl target)
        {
            if (LocalInstance == null)
            {
                return true;
            }

            if (PlayerControl.LocalPlayer.inVent)
            {
                __instance.Close();
                return false;
            }

            if (target != null)
            {
                var targetPlayer = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.PlayerId == target.PlayerId);
                if (targetPlayer != null)
                {
                    ShapeshiftAnimationRpc.Invoke((LocalInstance.MyPlayer.PlayerId, target.PlayerId, true, true));
                }
            }
            __instance.Close();
            return false;
        }
        static private void PlayShapeshiftAnimation(GamePlayer player, bool doAnimation, Action? midAnimExtra = null, Action? onComplete = null)
        {
            var au = player.VanillaPlayer;
            au.shapeshifting = true;
            au.MyPhysics.SetNormalizedVelocity(UnityEngine.Vector2.zero);

            if (player.AmOwner && !Minigame.Instance)
            {
                PlayerControl.HideCursorTemporarily();
            }

            RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate(
                DestroyableSingleton<RoleManager>.Instance.shapeshiftAnim, au.gameObject.transform);
            roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(player.AmOwner);
            PlayerMaterial.SetColors(player.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
            if (au.cosmetics.FlipX)
            {
                roleEffectAnimation.transform.position -= new UnityEngine.Vector3(0.14f, 0f, 0f);
            }

            float shapeshiftScale = au.MyPhysics.Animations.ShapeshiftScale;
            if (AprilFoolsMode.ShouldLongAround())
            {
                au.cosmetics.ShowLongModeParts(false);
                au.cosmetics.SetHatVisorVisible(false);
            }
            au.StartCoroutine(au.ScalePlayer(shapeshiftScale, 0.25f));
            Action midAnimCb = () =>
            {
                au.cosmetics.SetScale(au.MyPhysics.Animations.DefaultPlayerScale, au.defaultCosmeticsScale);
                midAnimExtra?.Invoke();
            };
            roleEffectAnimation.MidAnimCB = midAnimCb;

            Action onClipEnd = () =>
            {
                au.shapeshifting = false;
                if (AprilFoolsMode.ShouldLongAround())
                {
                    au.cosmetics.ShowLongModeParts(true);
                    au.cosmetics.SetHatVisorVisible(true);
                }
                onComplete?.Invoke();
            };
            roleEffectAnimation.Play(au, onClipEnd, PlayerControl.LocalPlayer.cosmetics.FlipX,
                RoleEffectAnimation.SoundType.Local, 0f, doAnimation, 0f);
        }

        private void OpenShapeshiftPanel()
        {
            var shapeshiftRolePrefab = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
            ShapeshifterMinigame minigame = UnityEngine.Object.Instantiate(shapeshiftRolePrefab!.ShapeshifterMenu);
            minigame.transform.SetParent(UnityEngine.Camera.main.transform, false);
            minigame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
            minigame.SetIsCustom(true);
            minigame.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material = PlayerControl.LocalPlayer.cosmetics.currentBodySprite.BodySprite.material;
            minigame.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material = PlayerControl.LocalPlayer.cosmetics.currentBodySprite.BodySprite.material;
            minigame.Begin(null);
        }

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                LocalInstance = this;

                shapeshiftButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, VirtualKeyInput.Ability, null,
                    ShapeshiftCoolDownOption, ShapeshiftDurationOption, "shapeshifterU.shapeshift",
                    new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image),
                    (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false, false)
                    .SetAsUsurpableButton(this)
                    .SetLabelType(ModAbilityButton.LabelType.Impostor);

                shapeshiftButton.OnClick = (button) =>
                {
                    if (!shapeshiftButton.IsInEffect)
                    {
                        OpenShapeshiftPanel();
                    }
                    else
                    {
                        shapeshiftButton.InterruptEffect();
                    }
                };

                shapeshiftButton.OnEffectEnd = (button) =>
                {
                    shapeshiftButton.StartCoolDown();
                    if (revertWithoutAnimation)
                    {
                        revertWithoutAnimation = false;
                        PlayerModInfo.RpcRemoveOutfit.Invoke(new(AmongUsLLImpl.LocalPlayer.PlayerId, OutfitTag));
                    }
                    else
                    {
                        ShapeshiftAnimationRpc.Invoke((MyPlayer.PlayerId, MyPlayer.PlayerId, true, false));
                    }
                };
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (AmOwner && shapeshiftButton != null && shapeshiftButton.IsInEffect)
            {
                revertWithoutAnimation = true;
                shapeshiftButton.InterruptEffect();
            }
        }
        [Local, OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (AmOwner && shapeshiftButton != null && shapeshiftButton.IsInEffect)
            {
                revertWithoutAnimation = true;
                shapeshiftButton.InterruptEffect();
            }
        }

        void IGameOperator.OnReleased()
        {
            if (AmOwner)
            {
                MyPlayer.Unbox().RemoveOutfit(OutfitTag);
                if (LocalInstance == this) LocalInstance = null;
            }
        }
    }
}