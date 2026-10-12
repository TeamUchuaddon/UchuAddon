using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
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
using Nebula.Roles.Complex;
using Nebula.Roles.Impostor;
using Nebula.Utilities;
using PowerTools;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using compat = Hori.Core.UchuTranslatable;

namespace Hori.Scripts.Role.Impostor;

public class RocketU : DefinedSingleAbilityRoleTemplate<RocketU.Ability>, DefinedRole, HasCitation,IAssignableDocument
{
    public RocketU() : base("rocketU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [HoldCooldownOption, NextHoldCooldownOption, LaunchCooldownOption, CanUseNormalKillOption, CanCounterOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Rocket.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagFunny);
    }
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.SuperNewRoles; } }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    static bool holded = false;


    static private FloatConfiguration HoldCooldownOption = NebulaAPI.Configurations.Configuration("options.role.rocketU.holdCooldown", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration NextHoldCooldownOption = NebulaAPI.Configurations.Configuration("options.role.rocketU.nextHoldCooldown", (2.5f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration LaunchCooldownOption = NebulaAPI.Configurations.Configuration("options.role.rocketU.launchCooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration CanUseNormalKillOption = NebulaAPI.Configurations.Configuration("options.role.rocketU.canUseNormalKill", false);
    static private BoolConfiguration CanCounterOption = NebulaAPI.Configurations.Configuration("options.role.rocketU.canCounter", true);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Rocket.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public RocketU MyRole = new();

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static private readonly Image HoldButtonSprite = NebulaAPI.AddonAsset.GetResource("HoldButton.png")!.AsImage(115f)!;
    static private readonly Image LaunchButtonSprite = NebulaAPI.AddonAsset.GetResource("RocketLaunchButton.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(HoldButtonSprite, "role.rocketU.ability.hold");
        yield return new(LaunchButtonSprite, "role.rocketU.ability.launch");
    }
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => new int[] { IsUsurped.AsInt() };

        static private byte[] holdingPlayers = Array.Empty<byte>();
        bool usedMeetingLaunch = false;

        bool IPlayerAbility.HideKillButton => holdingPlayers != Array.Empty<byte>() || !CanUseNormalKillOption;


        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, p => ObjectTrackers.PlayerlikeStandardPredicate(p) && !holdingPlayers.Any(s => s == p.RealPlayer.PlayerId) && p.RealPlayer.Role.Role.Team != MyPlayer.Role.Role.Team);
                playerTracker.SetColor(MyRole.RoleColor);

                ModAbilityButton launchButton = null;

                var holdButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, playerTracker, new PlayerInteractParameter(true, false, true), Virial.Compat.VirtualKeyInput.Ability, null,
                HoldCooldownOption, "Hold", HoldButtonSprite, (p, button) =>
                {
                    if (!holdingPlayers.Contains(playerTracker.CurrentTarget.RealPlayer.PlayerId))
                    {
                        holdingPlayers = holdingPlayers.Append(playerTracker.CurrentTarget.RealPlayer.PlayerId).ToArray();
                    }
                    button.CoolDownTimer.Start(NextHoldCooldownOption);
                    launchButton?.StartCoolDown();
                }).SetAsUsurpableButton(this);
                holdButton.SetLabelType(ModAbilityButton.LabelType.Impostor);

                launchButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility, null, LaunchCooldownOption, "Launch", LaunchButtonSprite, _ => true, _ => holdingPlayers != Array.Empty<byte>());
                launchButton.OnClick = (button) =>
                {
                    foreach (var id in holdingPlayers)
                    {
                        var player = GamePlayer.GetPlayer(id);
                        MyPlayer.MurderPlayer(player, compat.RocketLaunch, null, KillParameter.WithAssigningGhostRole | KillParameter.WithoutSelfSE);

                        RpcRocketKill.Invoke(new(MyPlayer, player, MyPlayer.TruePosition, Array.IndexOf(holdingPlayers, id)));
                    }

                    holdButton.StartCoolDown();
                    holdingPlayers = Array.Empty<byte>();
                };
                launchButton.SetLabelType(ModAbilityButton.LabelType.Impostor);



            }
        }

        void OnGameUpdate(GameUpdateEvent ev)
        {
            if (AmOwner)
            {
                if (!MyPlayer.IsDead && !MeetingHud.Instance && !ExileController.Instance && !usedMeetingLaunch)
                {
                    foreach (var id in holdingPlayers)
                    {
                        var player = GamePlayer.GetPlayer(id);
                        MyPlayer.MurderPlayer(player, compat.RocketLaunch, null, KillParameter.WithAssigningGhostRole | KillParameter.WithoutSelfSE, KillCondition.NoCondition);
                    }
                    holdingPlayers = Array.Empty<byte>();
                    usedMeetingLaunch = true;
                }

                if (holdingPlayers != Array.Empty<byte>())
                {
                    RpcHoldPlayer.Invoke(new(holdingPlayers, MyPlayer.PlayerId));
                    foreach (var id in holdingPlayers)
                    {
                        var player = GamePlayer.GetPlayer(id);
                        if (player == null || player.IsDead)
                        {
                            holdingPlayers = holdingPlayers.Where(i => i != id).ToArray();
                        }
                    }
                }
            }
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (AmOwner)
            {
                usedMeetingLaunch = false;
            }
        }


        void OnGameEnd(GameEndEvent ev)
        {
            if (AmOwner)
            {
                holdingPlayers = Array.Empty<byte>();
            }
        }

        [OnlyMyPlayer]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (ev.Player == MyPlayer)
            {
                holdingPlayers = Array.Empty<byte>();
            }
        }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (CanCounterOption) return;
            if (!holdingPlayers.Contains(ev.Killer.PlayerId)) return;
            if (ev.IsMeetingKill || ev.EventDetail != EventDetails.Kill) return;
            if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;

            ev.Result = KillResult.ObviousGuard;
        }

    }

    readonly static RemoteProcess<(byte[] holdingPlayers, byte rocketPlayer)> RpcHoldPlayer
        = new("HoldPlayer", (message, _) =>
        {
            foreach (var id in message.holdingPlayers)
            {
                var player = GamePlayer.GetPlayer(id);
                if (player == null)
                {
                    continue;
                }
                if (player.AmOwner) holded = true;

                var rocketPlayer = GamePlayer.GetPlayer(message.rocketPlayer);
                if (rocketPlayer == null || rocketPlayer.IsDead) continue;

                player.VanillaPlayer.NetTransform.SnapTo(new Vector2(rocketPlayer.TruePosition.x, rocketPlayer.TruePosition.y + 0.35f));
            }
        }
        );

    static readonly RemoteProcess<(GamePlayer rocket, GamePlayer dead, Vector2 pos, int number)> RpcRocketKill = new(
           "RocketKill",
           (message, _) =>
           {
               SpawnRocket(message.rocket, message.dead, message.pos, message.number);
           }
           );

    static List<GameObject> allRockets = [];

    static private readonly Image rocketRopeSprite = NebulaAPI.AddonAsset.GetResource("RocketRope.png")!.AsImage(130f)!;
    static private readonly Image rocketSprite = NebulaAPI.AddonAsset.GetResource("Rocket.png")!.AsImage(50f)!;
    static private readonly MultiImage rocketMovingSprite = NebulaAPI.AddonAsset.GetResource("RocketMoving.png")!.AsMultiImage(2, 1, 50f)!;
    static private readonly IDividedSpriteLoader explosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 70f, 4, 2);


    static void SpawnRocket(GamePlayer killer, GamePlayer player, Vector2 position, int number)
    {
        var rocketHolder = UnityHelper.CreateObject("Rocket", null, position.AsVector3(-1f), LayerExpansion.GetDefaultLayer());
        var myBehaviour = rocketHolder.gameObject.AddComponent<EmptyBehaviour>();
        allRockets.Add(rocketHolder);

        rocketHolder.transform.localPosition += GetCustomCirclePos(number);
        rocketHolder.transform.localScale = new Vector3(1f, 1f, 0.1f);
        var rocket = UnityHelper.CreateObject("Sin", rocketHolder.transform, Vector3.Zero);

        var rocketRenderer = UnityHelper.CreateObject<SpriteRenderer>("Sprite", rocket.transform, new Vector3(0f, 0f, 10f));

        rocketRenderer.transform.localScale = new(0.43f, 0.43f, 1f);
        rocketRenderer.sprite = rocketSprite.GetSprite();

        var rocketRopeRenderer = UnityHelper.CreateObject<SpriteRenderer>("Rope", rocket.transform, new Vector3(0f, 0f, 0f));
        rocketRopeRenderer.sprite = rocketRopeSprite.GetSprite();


        var launchPlayer = UnityHelper.CreateObject("LaunchPlayer", rocket.transform, Vector3.Zero);

        var deadBody = GameObject.Instantiate(HudManager.Instance.KillOverlay.KillAnims[0].victimParts, launchPlayer.transform);
        if (player.AmOwner) AmongUsUtil.SetCamTarget(myBehaviour);

        deadBody.transform.localPosition = new Vector3(-0.28f, 0.42f, 5f);

        deadBody.transform.localScale = new(-0.35f, 0.35f, 0.5f);
        deadBody.gameObject.ForEachAllChildren(obj => obj.layer = LayerExpansion.GetDefaultLayer());
        deadBody.gameObject.layer = LayerExpansion.GetUILayer();


        deadBody.UpdateFromPlayerOutfit(player.CurrentOutfit.outfit, PlayerMaterial.MaskType.None, false, false, (System.Action)(() =>
        {   
            deadBody.transform.Find("Cosmetics/Skin").transform.localPosition = new Vector3(-0.8f, -0.5f, 0f);
        }));
        deadBody.ToggleName(false);
        NebulaManager.Instance.StartCoroutine(CoLaunchLocket().WrapToIl2Cpp());

        IEnumerator CoLaunchLocket()
        {
            var anims = deadBody.GetComponentsInChildren<SpriteAnim>();
            deadBody.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 0f);
            foreach (var anim in anims)
            {
                anim.Speed = 0f;
                anim.Time = 0f;
            }
                
            yield return Effects.Wait((float)(number + 1) * 0.5f);

            NebulaAsset.PlaySE(APICompat.GetSound("launch"), player.VanillaPlayer.transform.position, 3f, 4f, 1f);

            for (int i = 0; i < 75; i++)
            {
                rocketHolder.transform.position += new UnityEngine.Vector3(0, (float)7.5 * Time.deltaTime, 0);
                rocketRenderer.sprite = rocketMovingSprite.GetSprite(i % 2);
                yield return Effects.Wait(0.01f);
            }

            UnityEngine.Object.Destroy(launchPlayer);
            UnityEngine.Object.Destroy(rocketRopeRenderer);

            var sfxSource = SoundManager.instance.gameObject.GetComponent<AudioSource>();

            sfxSource.PlayOneShot(APICompat.GetSound("rocket_explosion"), 1f);

            for (int i = 0; i < 8; i++)
            {

                rocketRenderer.sprite = explosionSprite.GetSprite(i);

                yield return Effects.Wait(0.12f);
            }

            if (player.AmOwner) AmongUsUtil.SetCamTarget(null);

            UnityEngine.Object.Destroy(rocketHolder);
        }

        UnityEngine.Vector3 GetCustomCirclePos(int number)
        {
            int elementsPerRing = 8;
            float radius = 1.5f;

            int idx = (number) % elementsPerRing;

            float[] angles = {
            90f,
            45f,
            135f,
            0f,
            180f,
            -45f,
            -135f,
            -90f
            };
            float angleDeg = angles[idx];
            float rad = angleDeg * Mathf.Deg2Rad;

            return new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
        }

    }
}
