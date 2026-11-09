using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Hori.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules.ScriptComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using AmongUs.Matchmaking;

namespace Hori.Scripts.Role.Impostor;

public class PhantomU : DefinedSingleAbilityRoleTemplate<PhantomU.Ability>, DefinedRole, HasCitation
{
    public PhantomU() : base("phantomU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [PhantomCooldown, PhantomDuration, CanKillInVanish, VanishImpostor])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    Citation? HasCitation.Citation => Nebula.Roles.Citations.AmongUs;

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;

    static private FloatConfiguration PhantomCooldown = NebulaAPI.Configurations.Configuration("options.role.phantomU.phantomCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration PhantomDuration = NebulaAPI.Configurations.Configuration("options.role.phantomU.phantomDuration", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration CanKillInVanish = NebulaAPI.Configurations.Configuration("options.role.phantomU.canKillInVanish", true);
    static private BoolConfiguration VanishImpostor = NebulaAPI.Configurations.Configuration("options.role.phantomU.vanishImpostor", false);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Phantom.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;

    static public PhantomU MyRole = new();

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        bool IsVanish = false;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var PhantomButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, PhantomCooldown, PhantomDuration, "phantomU.vanish", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<PhantomRole>()!.Ability.Image), (ModAbilityButton _) => MyPlayer.CanMove,(ModAbilityButton _) => !MyPlayer.IsDead, false, true).SetLabelType(ModAbilityButton.LabelType.Impostor);
                PhantomButton.OnEffectStart = (button) =>
                {
                    var phantom = AmongUsUtil.GetRolePrefab<PhantomRole>();
                    button.SetImage(new WrapSpriteLoader(() => phantom!.Ability.SecondImage));
                    button.SetLabel("phantomU.away");

                    PhantomAnimVanishRpc.Invoke((MyPlayer, true));

                    IsVanish = true;

                    NebulaManager.Instance.StartDelayAction(0.25f, () =>
                    {
                        if (VanishImpostor && MyPlayer.IsImpostor)
                        {
                            MyPlayer.GainAttribute(PlayerAttributes.InvisibleElseImpostor, 99999f, false, 0, "phantomU:vanish");
                        }
                        else
                        {
                            MyPlayer.GainAttribute(PlayerAttributes.Invisible, 99999f, false, 0, "phantomU:vanish");
                        }
                    });
                };
                PhantomButton.OnEffectEnd = (button) =>
                {
                    var phantom = AmongUsUtil.GetRolePrefab<PhantomRole>();
                    button.SetImage(new WrapSpriteLoader(() => phantom!.Ability.Image));
                    button.SetLabel("phantomU.vanish");
                    PhantomAnimVanishRpc.Invoke((MyPlayer, false));

                    IsVanish = false;

                    button.StartCoolDown();

                    NebulaManager.Instance.StartDelayAction(1.1f, () =>
                    {
                        MyPlayer.RemoveAttributeByTag("phantomU:vanish");
                    });
                };
            }
        }

        [OnlyMyPlayer, Local]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (!CanKillInVanish && IsVanish)
            {
                ev.SetAsCannotKillBasically();
            }
        }

        public static RemoteProcess<(GamePlayer, bool)> PhantomAnimVanishRpc = new("PhantomVanishRPC_Uchu", (message, _) =>
        {
            if (MeetingHud.Instance)
            {
                return;
            }
            var phantomPlayer = message.Item1.VanillaPlayer;
            var phantomRole = AmongUsUtil.GetRolePrefab<PhantomRole>();
            if (message.Item2)
            {
                RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_ChargeAnim, phantomPlayer.gameObject.transform);

                roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
                PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
                roleEffectAnimation.Play(phantomPlayer, new System.Action(() =>
                {
                    if (message.Item1.AmOwner)
                    {
                        if (Constants.ShouldPlaySfx())
                        {
                            SoundManager.Instance.PlayDynamicSound("PhantomVanish", phantomRole!.UseSound, false, new System.Action<AudioSource, float>(phantomRole.SoundDynamics), SoundManager.Instance.SfxChannel);
                        }
                    }

                    /*
                    if (SEOption)
                    {
                        var localPlayer = GamePlayer.LocalPlayer;
                        if (localPlayer == null) return;
                        if (localPlayer == message.Item1) return;

                        float distance = localPlayer.Position.Distance(message.Item1.Position);
                        if (distance > SERange) return;

                        if (Constants.ShouldPlaySfx())
                        {
                            SoundManager.Instance.PlayDynamicSound("PhantomVanish", phantomRole!.UseSound, false, new System.Action<AudioSource, float>(phantomRole.SoundDynamics), SoundManager.Instance.SfxChannel);
                        }
                    }
                    */

                    RoleEffectAnimation roleEffectAnimation2 = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_PoofAnim, phantomPlayer.gameObject.transform);
                    PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation2.Renderer);
                    roleEffectAnimation2.Play(phantomPlayer, null, PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, false, 0f);
                }), PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0.5f, true, -0.05f);
            }
            else
            {
                if (message.Item1.AmOwner)
                {
                    if (Constants.ShouldPlaySfx() && !MeetingHud.Instance)
                    {
                        SoundManager.Instance.PlayDynamicSound("PhantomAppear", phantomRole!.appearSound, false, new System.Action<AudioSource, float>(phantomRole.SoundDynamics), SoundManager.Instance.SfxChannel);
                    }
                    RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_ChargeAnim, phantomPlayer.gameObject.transform);
                    roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
                    PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
                    roleEffectAnimation.Play(phantomPlayer, new System.Action(() =>
                    {
                        RoleEffectAnimation roleEffectAnimation2 = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.appear_PoofAnim, phantomPlayer.gameObject.transform);
                        PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation2.Renderer);
                        roleEffectAnimation2.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
                        roleEffectAnimation2.Play(phantomPlayer, null, PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
                    }), PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 1f, true, -0.05f);
                }
                else
                {
                    NebulaManager.Instance.StartDelayAction(1f, () =>
                    {
                        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.appear_PoofAnim, phantomPlayer.gameObject.transform);
                        PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
                        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(PlayerControl.LocalPlayer.AmOwner);
                        roleEffectAnimation.Play(phantomPlayer, null, PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
                    });
                }
            }
        });
    }
}