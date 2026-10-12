using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Role.Modifier;
using Hori.Scripts.Role.Neutral;
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
using Nebula.Roles.Crewmate;
using Nebula.Roles.Ghost.Crewmate;
using Nebula.Roles.Ghost.Neutral;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Hori.Scripts.Role.Ghost.Complex.BaphometU;
using static Nebula.Roles.Ghost.Neutral.Grudge;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Marionette;
using Epic.OnlineServices.Presence;
using Nebula.Roles.Complex;
using Nebula.VoiceChat;
using System.ComponentModel;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Role;
using Virial.Game;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Ghost.Complex;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
file static class BaphometAsset
{
    static public readonly Virial.Media.Image BackImage = NebulaAPI.AddonAsset.GetResource("BaphometAnimImage/Back.png")!.AsImage(50f)!;
    static public readonly Virial.Media.Image DemonImage = NebulaAPI.AddonAsset.GetResource("BaphometAnimImage/Demon.png")!.AsImage(80f)!;
    static public readonly Virial.Media.Image LightImage = NebulaAPI.AddonAsset.GetResource("BaphometAnimImage/DemonOpacity.png")!.AsImage(80f)!;

    static public readonly Sprite BackSprite = BackImage.GetSprite();
    static public readonly Sprite DemonSprite = DemonImage.GetSprite();
    static public readonly Sprite LightSprite = LightImage.GetSprite();
}

[NebulaRPCHolder]
public class BaphometU : DefinedGhostRoleTemplate, DefinedGhostRole, IAssignableDocument
{
    public BaphometU() : base("baphometU", new(31, 26, 122), RoleCategory.CrewmateRole | RoleCategory.ImpostorRole | RoleCategory.NeutralRole, [TotalStandingTimeToWin, RewindGaugeToMeetingEnd]) 
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Baphomet.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    string ICodeName.CodeName => "BAH";

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, (byte)arguments.Get(0, player.PlayerId));

    static private readonly FloatConfiguration TotalStandingTimeToWin = NebulaAPI.Configurations.Configuration("options.role.baphometU.totalStandingTimeToWin", (15f, 120f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration RewindGaugeToMeetingEnd = NebulaAPI.Configurations.Configuration("options.role.baphometU.rewindGaugeToMeetingEnd", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);

    static public readonly BaphometU MyRole = new();


    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Baphomet.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    [NebulaRPCHolder]
    public class BaphometGhost : FlexibleLifespan, IGameOperator
    {
        PoolablePlayer player;
        SpriteRenderer[] allRenderers;

        public bool IsActive { get; set; } = true;
        private float Alpha { get; set; } = 0f;

        public BaphometGhost(GamePlayer player, Vector3 position, bool flipX)
        {
            this.player = AmongUsUtil.GetPlayerIcon(player.DefaultOutfit.outfit, null, position, new(0.35f, 0.35f, 0.001f), flipX, false);
            UnityHelper.DoForAllChildren(this.player.gameObject, obj => obj.layer = LayerExpansion.GetPlayersLayer());
            allRenderers = this.player.gameObject.GetComponentsInChildren<SpriteRenderer>();
            SetAlpha(0f);
        }

        private void SetAlpha(float a)
        {
            Color color = new(1f, 1f, 1f, a);

            foreach (var renderer in allRenderers) renderer.color = color;
        }

        void Update(GameUpdateEvent ev)
        {
            if (IsDeadObject) return;

            if (IsActive)
            {
                Alpha += Time.deltaTime * 0.6f;
                if (Alpha > 1f) Alpha = 1f;
            }
            else
            {
                Alpha -= Time.deltaTime * 2.5f;
                if (Alpha < 0f)
                {
                    Alpha = 0f;
                    Release();
                }
            }
            //SetAlpha(Alpha);

            //死者目線では幻影は見えない
            SetAlpha((GamePlayer.LocalPlayer?.IsDead ?? true) ? 0f : Alpha);
        }

        void IGameOperator.OnReleased()
        {
            if (player) GameObject.Destroy(player.gameObject);
        }
    }

    static private readonly Virial.Media.Image FamilyImage = NebulaAPI.AddonAsset.GetResource("BaphometFamilyButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image GaugeImage = NebulaAPI.AddonAsset.GetResource("BaphometDocument.png")!.AsImage(90f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    bool IAssignableDocument.HasWinCondition => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(FamilyImage, "role.baphometU.ability.family");
        yield return new(GaugeImage, "role.baphometU.ability.gauge");
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;
        public int BaphometId { get; private set; }
        public BaphometU.Instance? MyBaphomet => this;
        static float t = 0f;
        static BaphometGaugeAbility BaphometGauge = null!; //ゲージ自体
        public static MultiImage GaugeIcons = NebulaAPI.AddonAsset.GetResource("TunaGaugeIcon.png")!.AsMultiImage(2, 1, 160f)!;
        public bool BaphometGaugeActive;
        List<TrackingArrowAbility> famulusArrows = [];

        public Instance(GamePlayer player, byte baphometId) : base(player)
        {
            BaphometId = baphometId;
        }
        int[] RuntimeAssignable.RoleArguments => [BaphometId];

        public bool IsMyFamulus(GamePlayer? player)
        {
            if (player == null) return false;
            if (player.Modifiers.Any(m => m is FamulusU.Instance famulus && famulus.BaphometId == BaphometId)) return true;
            return false;
        }

        public bool IsMyBaphomet(GamePlayer? player)
        {
            if (player?.GhostRole is Instance bap && bap.BaphometId == BaphometId) return true;
            return false;
        }

        public bool IsSameTeam(GamePlayer? player)
        {
            if (IsMyFamulus(player)) return true;
            if (player?.GhostRole is Instance baphomet && baphomet.BaphometId == BaphometId) return true;
            return false;
        }

        [Local]
        void DecorateFamulusColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(ev.Player) && !ev.Player.AmOwner) ev.Color = BaphometU.MyRole.RoleColor;
        }

        [OnlyMyPlayer]
        void DecorateBaphometColor(PlayerDecorateNameEvent ev)
        {
            if (IsMyFamulus(GamePlayer.LocalPlayer) && !ev.Player.AmOwner) ev.Color = BaphometU.MyRole.RoleColor;
        }

        private void BaphometArrow(GamePlayer player)
        {
            void RegisterArrow(GamePlayer player, Virial.Color color) => famulusArrows.Add(new TrackingArrowAbility(player.Unbox(), 0f, color, false).Register(this));
            famulusArrows.RemoveAll(a => { if (a.MyPlayer == player) { a.Release(); return true; } else return false; });


            if (player.AmOwner) return;

            RegisterArrow(player, BaphometU.MyRole.Color);
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                BaphometId = MyPlayer.PlayerId;

                NebulaAPI.CurrentGame?.GetModule<TitleShower>()?.SetText(Language.Translate("role.baphometU.hudText"), MyRole.RoleColor.ToUnityColor(), 5.5f, true);
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.8f, 0.7f);
                SoundManager.instance.PlaySound(APICompat.GetSound("ClockCount"), false, 1f);

                bool isUsed = false;
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);

                var familyButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                     5f, "baphometU.family", FamilyImage, null, _ => playerTracker.CurrentTarget != null && !isUsed && !playerTracker.CurrentTarget.TryGetModifier<FamulusU.Instance>(out var famulus), true);
                familyButton.OnClick = (button) =>
                {
                    isUsed = true;

                    var target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        RpcBaphometCutinUchu.Invoke();

                        target.AddModifier(FamulusU.MyRole, [1, BaphometId]);

                        BaphometArrow(target);
                    }
                };
                familyButton.ShowUsesIcon(0, "1");
                familyButton.Visibility = (button) => MyPlayer.IsDead && !isUsed;
                familyButton.Availability = (button) => playerTracker.CurrentTarget != null && !isUsed && !playerTracker.CurrentTarget.TryGetModifier<FamulusU.Instance>(out var famulus);
                BaphometGauge = new BaphometGaugeAbility(TotalStandingTimeToWin, 340f, () => progress, () => BaphometGaugeActive).Register(this);
                BaphometGauge.SetActive(true);
            }
        }

        static RemoteProcess RpcBaphometCutinUchu = new RemoteProcess("RpcBaphometCutin_Uchu", (_) =>
        {
            NebulaManager.Instance.StartCoroutine(CoCutin().WrapToIl2Cpp());
        });

        static IEnumerator CoCutin()
        {
            SoundManager.instance.PlaySound(APICompat.GetSound("ClockPendulum"), false, 1f);

            var CutinHolder = UnityHelper.CreateObject("BaphometCutinHolder", HudManager.Instance.transform, Vector3.Zero);

            var Back = UnityHelper.CreateObject<SpriteRenderer>("BackGround", CutinHolder.transform, new Vector3(0f, 0f, -69f));
            Back.sprite = BaphometAsset.BackSprite;
            Back.color = new UnityEngine.Color(1f, 1f, 1f, 0.4f);

            var Demon = UnityHelper.CreateObject<SpriteRenderer>("Demon", CutinHolder.transform, new Vector3(0f, 0f, -70f));
            Demon.sprite = BaphometAsset.DemonSprite;

            var DemonOpacity = UnityHelper.CreateObject<SpriteRenderer>("DemonOpacity", CutinHolder.transform, new Vector3(0f, 0f, -71f));
            DemonOpacity.sprite = BaphometAsset.LightSprite;
            DemonOpacity.color = new UnityEngine.Color(1f, 1f, 1f, 0.8f);

            yield return new WaitForSeconds(2.7f);

            Color backColor = Back.color;
            Color demonColor = Demon.color;
            Color lightColor = DemonOpacity.color;

            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float alpha = 1f - (t / 0.6f);

                Back.color = new Color(backColor.r, backColor.g, backColor.b, backColor.a * alpha);
                Demon.color = new Color(demonColor.r, demonColor.g, demonColor.b, alpha);
                DemonOpacity.color = new Color(lightColor.r, lightColor.g, lightColor.b, lightColor.a * alpha);

                yield return null;
            }

            t = 0f;
            GameObject.Destroy(CutinHolder);
        }

        BaphometGhost? currentIllusion = null;
        float standingTime = 0f;
        float standingCoolDown = 10f;
        float progress = 0f;
        bool canWin = false;
        float bored = 0f;
        bool takeoverWin = false;

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            standingCoolDown = 5f;
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (canWin) return;

            progress -= RewindGaugeToMeetingEnd;
            if (progress <= 0f)
            {
                progress = 0f;
            }
        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (!MyPlayer.IsDead)
            {
                BaphometGauge.SetActive(false);
                return;
            }

            if (TotalStandingTimeToWin >= progress)
                BaphometGauge.SetActive(true);
            BaphometGaugeActive = true;
            var myPos = MyPlayer.Position.ToUnityVector();
            bool hasNearbyFamulus = NebulaGameManager.Instance!.AllPlayerInfo.Any(p =>!p.IsDead && IsMyFamulus(p) && p.VanillaPlayer.transform.position.Distance(myPos) < 1.5f);

            if (standingCoolDown > 0f)
            {
                if (!MeetingHud.Instance && !ExileController.Instance)
                {
                    standingCoolDown -= Time.deltaTime;
                }
                standingTime = 0f;
            }
            else
            {
                if (MyPlayer.IsDead && !MeetingHud.Instance && !ExileController.Instance && MyPlayer.VanillaPlayer.MyPhysics.Velocity.magnitude < 0.001f && hasNearbyFamulus)
                {
                    standingTime += Time.deltaTime;
                }
                else
                {
                    standingTime = 0f;                
                }
            }

            if (standingTime > 0.6f && currentIllusion == null)
            {
                RpcShowGhostUchu.Invoke((MyPlayer, MyPlayer.VanillaPlayer.transform.position, MyPlayer.VanillaPlayer.cosmetics.FlipX));
            }
            if (!(standingTime > 0f) && currentIllusion != null)
            {
                RpcDisappearGhostUchu.Invoke(MyPlayer);
            }

            if (!canWin && standingTime > 0.8f)
            {
                if (NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && IsMyFamulus(p) && p.VanillaPlayer.transform.position.Distance(myPos) < 1.5f))
                {
                    progress += Time.deltaTime;
                    BaphometGaugeActive = true;
                    if (TotalStandingTimeToWin < progress)
                    {
                        canWin = true;
                        AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.8f, 0.7f);
                    }
                }
                else
                {
                    bored += Time.deltaTime;
                    BaphometGaugeActive = false;
                }
            }
            else
            {
                bored = 0f;
                BaphometGaugeActive = false;
            }
            if (TotalStandingTimeToWin < progress)
            {
                BaphometGauge.SetActive(false);
            }
        }

        [Local]
        void UpdateTaskText(PlayerTaskTextLocalEvent ev)
        {
            string text = Language.Translate("role.baphometU.taskText");
            if (canWin)
            {
                text = text.Color(UnityEngine.Color.green);
            }
            else
            {
                text += string.Format(" {0:0.#}", progress) + "s/" + TotalStandingTimeToWin.GetValue() + "s";
                Color color = progress > 0f ? UnityEngine.Color.yellow : UnityEngine.Color.white;
                if (standingTime < 0.8f) color = color.RGBMultiplied(0.5f);
                text = text.Color(color);
            }
            ev.AppendText(text);
        }

        void RuntimeAssignable.OnInactivated()
        {
            if (currentIllusion != null) currentIllusion.IsActive = false;
        }

        static private readonly RemoteProcess<(GamePlayer player, Vector3 pos, bool flipX)> RpcShowGhostUchu = new("RpcShowGhost_Uchu", (message, _) =>
        {
            var baphomet = message.player.GhostRole as BaphometU.Instance;
            if (baphomet != null)
            {
                if (baphomet.currentIllusion != null) baphomet.currentIllusion.IsActive = false;
                baphomet.currentIllusion = new BaphometGhost(message.player, message.pos, message.flipX);
                baphomet.currentIllusion.Register(baphomet);
            }
        });

        static private readonly RemoteProcess<GamePlayer> RpcDisappearGhostUchu = new("RpcDisappearGhost_Uchu", (message, _) =>
        {
            var baphomet = message.GhostRole as BaphometU.Instance;
            if (baphomet != null && baphomet.currentIllusion != null)
            {
                baphomet.currentIllusion.IsActive = false;
                baphomet.currentIllusion = null;
            }
        });

        static private readonly RemoteProcess<(GamePlayer, bool win)> RpcBaphometWinUchu = new("RpcBaphometWin_Uchu", (message, _) =>
        {
            var baphomet = message.Item1.GhostRole as BaphometU.Instance;
            if (baphomet != null)
            {
                baphomet.CanWinWithFamulus = message.win;
            }
        });

        static private RemoteProcess<(GamePlayer player, float time)> RpcShareFloat = new("RpcShareFloat_Uchu", (message, _) =>
        {
            if (message.player.GhostRole is BaphometU.Instance baphomet) baphomet.progress = message.time;
        }, false);

        public bool CanWinWithFamulus { get; private set; }

        [OnlyHost]
        void WinUpdate(GameUpdateEvent ev)
        {
            CanWinWithFamulus = progress >= TotalStandingTimeToWin && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && IsMyFamulus(p));
        }

        float lastShareTime = -10f;
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (NebulaGameManager.Instance!.CurrentTime > lastShareTime + 0.2f)
            {
                RpcBaphometWinUchu.Invoke((MyPlayer, CanWinWithFamulus));
                RpcShareFloat.Invoke((MyPlayer, progress));
                lastShareTime = NebulaGameManager.Instance!.CurrentTime;
            }
        }

        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (CanWinWithFamulus && ev.OriginalEndReason != GameEndReason.Sabotage)
            {
                ev.TryOverwriteEnd(UchuGameEnd.BaphometWin, GameEndReason.Special);
            }
        }

        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == UchuGameEnd.BaphometWin && CanWinWithFamulus && IsSameTeam(ev.Player));
        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd != UchuGameEnd.BaphometWin;

        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;
    }
}

public class FamulusU : DefinedModifierTemplate, DefinedModifier
{
    private FamulusU() : base("famulusU", new(31, 26, 122), withConfigurationHolder: false) 
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static public FamulusU MyRole = new FamulusU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0, false), arguments.Get(1, 0));

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Famulus.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public int BaphometId { get; private set; }
        private bool ShouldResolveId = false;

        public Instance(GamePlayer player, bool givenTeamId, int baphometId) : base(player)
        {
            ShouldResolveId = !givenTeamId;
            BaphometId = baphometId;
        }
        int[] RuntimeAssignable.RoleArguments => [1,BaphometId];

        public bool IsMyBaphomet(GamePlayer? player)
        {
            if (player == null) return false;
            if (player.GhostRole is BaphometU.Instance baphomet && baphomet.BaphometId == BaphometId) return true;
            return false;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (ShouldResolveId)
            {
                BaphometId = (GamePlayer.GetPlayer((byte)BaphometId)?.Role as BaphometU.Instance)?.BaphometId ?? 0;
                ShouldResolveId = false;
            }
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(BaphometU.MyRole.Color, 0f, 0.25f, 0.4f);
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;

        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == UchuGameEnd.BaphometWin && IsMyBaphomet(ev.Player));
        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd != UchuGameEnd.BaphometWin;

    }
}