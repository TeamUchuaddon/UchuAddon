using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Utilities;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;
using UnityColor = UnityEngine.Color;
using Image = Virial.Media.Image;
using UnityEngine;
using Hori.Scripts.Abilities;

namespace Hori.Scripts.Role.Impostor;

public class KunoichiU : DefinedSingleAbilityRoleTemplate<KunoichiU.Ability>, HasCitation, DefinedRole, IAssignableDocument
{
    public KunoichiU() : base("kunoichiU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam,
        [
        new GroupConfiguration("options.role.kunoichi.group.kunai", [KunaiCooldown, KunaiSize, KunaiSpeed, NumOfHit, ResetHitCountOnMeeting, KunaiDisappearOnWall, CanKillImpostorOption], GroupConfigurationColor.ImpostorRed),
        new GroupConfiguration("options.role.kunoichi.group.invisibily", [CanInvisibily, UseInvisibleButton, InvTime, CanKunaiInInvisibily], GroupConfigurationColor.ImpostorRed)
        ])
    {
    }
    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;

    static private FloatConfiguration KunaiCooldown = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.KunaiCooldown", (0f, 60f, 0.25f), 0.5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration KunaiSize = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.KunaiSize", (0.25f, 4f, 0.25f), 1.25f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration KunaiSpeed = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.KunaiSpeed", (0.5f, 5f, 0.25f), 3f, FloatConfigurationDecorator.Ratio);
    static public readonly IntegerConfiguration NumOfHit = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.numofhit", (1, 30), 10);
    static private BoolConfiguration CanKillImpostorOption = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.canKillImpostor", false);
    static private BoolConfiguration KunaiDisappearOnWall = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.KunaiDisappearOnWall", true);
    static private BoolConfiguration ResetHitCountOnMeeting = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.ResetHitCountOnMeeting", false);
    static private BoolConfiguration CanInvisibily = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.CanIninvisibily", true);
    static private BoolConfiguration UseInvisibleButton = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.UseInvisibleButton", true, static () => CanInvisibily);
    static private FloatConfiguration InvTime = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.InvisibleTime", (0f, 10f, 1f), 3f, FloatConfigurationDecorator.Second, static () => CanInvisibily);
    static private BoolConfiguration CanKunaiInInvisibily = NebulaAPI.Configurations.Configuration("options.role.kunoichiU.KunaiIninvisibily", true, static () => CanInvisibily);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.Length > 0 && arguments[0] != 0);
    static private readonly Virial.Media.Image? buttonSprite = NebulaAPI.AddonAsset.GetResource("KunoichiKunaiButton.png")?.AsImage(115f);
    static private readonly Virial.Media.Image? InvisibleButtonSprite = NebulaAPI.AddonAsset.GetResource("KunoichiInvisibilyButton.png")?.AsImage(115f);
    static private readonly Virial.Media.Image? KillButtonSprite = NebulaAPI.AddonAsset.GetResource("VanillaKillButton.png")?.AsImage(115f);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Kunoichi.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public KunoichiU MyRole = new();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(buttonSprite, "role.kunoichiU.ability.Kunai");
        yield return new(KillButtonSprite, "role.kunoichiU.ability.kill");
        yield return new(InvisibleButtonSprite, "role.kunoichiU.ability.invisibily");
    }
    //参考:レイダー

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class KunoichiKunai : NebulaSyncStandardObject, IGameOperator
    {
        public static readonly string MyTag = "KunoichiKunai";
        static private Virial.Media.Image NormalKunai = NebulaAPI.AddonAsset.GetResource("KunoichiKunai.png")!.AsImage(300f)!;
        static private Virial.Media.Image ThrownKunai = NebulaAPI.AddonAsset.GetResource("KunoichiKunaiThrow.png")!.AsImage(300f)!;

        private float thrownAngle = 0f;
        private int state = 0;
        private float speed = KunaiSpeed;
        private float thrownDistance = 0f;
        HashSet<IPlayerlike> tryKillSet = [];

        public static void ResetHitCount() => hitCount.Clear();
        public static Dictionary<byte, int> hitCount = new();

        public KunoichiKunai(PlayerControl owner) : base(owner.GetTruePosition(), ZOption.Front, false, NormalKunai.GetSprite())
        {
            CanSeeInShadow = true;
        }

        void HudUpdate(GameHudUpdateEvent ev)
        {
            if (!AmOwner)
            {
                MyRenderer.gameObject.SetActive(false);
                return;
            }

            if (state == 0)
            {
                if (AmOwner) Owner.Unbox().RequireUpdateMouseAngle();
                var mouseAngle = Owner.Unbox().MouseAngle;
                MyRenderer.transform.localEulerAngles = new UnityEngine.Vector3(0, 0, (mouseAngle * 180f / Mathn.PI) + 90f);

                var pos = Owner.VanillaPlayer.transform.position + new UnityEngine.Vector3(Mathn.Cos(mouseAngle), Mathn.Sin(mouseAngle), -1f) * 0.67f;
                var diff = (pos - MyRenderer.transform.position) * Time.deltaTime * 7.5f;
                Position += (UnityEngine.Vector2)diff;

                MyRenderer.flipX = true;
                MyRenderer.flipY = false;

                if (AmOwner)
                {
                    var vec = MyRenderer.transform.position - PlayerControl.LocalPlayer.transform.position;
                    if (KunaiDisappearOnWall && PhysicsHelpers.AnyNonTriggersBetween(PlayerControl.LocalPlayer.GetTruePosition(), (UnityEngine.Vector2)vec.normalized, ((UnityEngine.Vector2)vec).magnitude, Constants.ShipAndAllObjectsMask) && !Physics2D.Raycast(PlayerControl.LocalPlayer.GetTruePosition(), vec, vec.magnitude, 1 << LayerExpansion.GetRaiderColliderLayer()))
                        MyRenderer.color = UnityEngine.Color.red;
                    else
                        MyRenderer.color = UnityEngine.Color.white;
                }
            }
            else if (state == 1)
            {
                var vec = new UnityEngine.Vector2(Mathn.Cos(thrownAngle), Mathn.Sin(thrownAngle));
                var pos = Position;
                var size = KunaiSize;

                if (!MeetingHud.Instance)
                {
                    foreach (var p in GamePlayer.AllPlayerlikes)
                    {
                        if (p.AmOwner || p.RealPlayer.PlayerId == Owner.PlayerId) continue;
                        if (p.IsDead || !p.IsActive) continue;
                        if (!CanKillImpostorOption && !Owner.CanKill(p.RealPlayer)) continue;
                        if (p.IsDived || p.Logic.InVent || p.IsBlown || p.WillDie) continue;
                        if (tryKillSet.Contains(p)) continue;

                        if (!Helpers.AnyNonTriggersBetween(p.TruePosition, pos, out var diff, Constants.ShipAndAllObjectsMask) && diff.magnitude < size * 0.4f)
                        {
                            if (p.IsInvisible) continue;
                            if (GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(Owner, p, new(IsKillInteraction: true))).IsCanceled ?? false) continue;

                            tryKillSet.Add(p);
                            state = 2;

                            byte playerId = p.RealPlayer.PlayerId;
                            if (!hitCount.ContainsKey(playerId)) hitCount[playerId] = 0;
                            hitCount[playerId]++;

                            MyRenderer.gameObject.SetActive(false);
                            if (hitCount[playerId] >= NumOfHit)
                            {
                                Owner.MurderPlayer(p, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.RemoteKill);
                            }
                            break;
                        }
                    }
                }

                if (state == 1)
                {
                    float d = speed * 4f * Time.deltaTime;
                    float originalD = d;

                    bool isHittingWall = KunaiDisappearOnWall &&
                                         !Ability.OverlapKunaiIgnoreArea(MyRenderer.transform.position) &&
                                         NebulaPhysicsHelpers.AnyNonTriggersBetween(MyRenderer.transform.position, vec, d, Constants.ShipAndAllObjectsMask | (1 << LayerExpansion.GetHookshotWallLayer()), out d);

                    if (thrownDistance > 30f || isHittingWall)
                    {
                        state = 2;
                        Position += vec * d;
                        MyRenderer.gameObject.SetActive(false);
                        NebulaManager.Instance.StartCoroutine(ManagedEffects.CoDisappearEffect(MyRenderer.gameObject.layer, null, MyRenderer.transform.position, 0.8f).WrapToIl2Cpp());
                    }
                    else
                    {
                        Position += vec * originalD;
                        thrownDistance += originalD;
                    }
                    MyRenderer.transform.position = new UnityEngine.Vector3(Position.x, Position.y, -1f);
                }
            }
        }

        public bool CanThrow => MyRenderer.color.b > 0.5f;

        public void Throw(UnityEngine.Vector2 pos, float angle)
        {
            thrownAngle = angle;
            state = 1;
            Position = pos;
            ZOrder = ZOption.Just;
            MyRenderer.transform.position = new UnityEngine.Vector3(pos.x, pos.y, -1f);
            CanSeeInShadow = true;
            MyRenderer.sprite = ThrownKunai.GetSprite();
            MyRenderer.color = UnityEngine.Color.white;
            MyRenderer.gameObject.SetActive(true);

            MyRenderer.transform.eulerAngles = new UnityEngine.Vector3(0f, 0f, (angle * 180f / Mathf.PI) + 90f);
            MyRenderer.flipX = true;
            MyRenderer.flipY = false;
        }

        static KunoichiKunai()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new KunoichiKunai(Helpers.GetPlayer((byte)args[0])!));
        }
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private UchuPlayersIconHolder? iconHolder;
        private bool isIconInitialized = false;

        private ModAbilityButton? equipButton, invisibleButton;
        public KunoichiKunai? MyKunai = null;
        private UnityEngine.Vector2 lastPosition;
        private float MoveTime = 0f;
        private bool isInvisible = false;

        bool IPlayerAbility.HideKillButton => !(equipButton?.IsBroken ?? false);
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                KunoichiKunai.ResetHitCount();
                new GuideLineAbility(MyPlayer, () => MyKunai != null).Register(this);

                lastPosition = MyPlayer.TruePosition;

                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "kunoichiU.equip",
                    0f, "equip", buttonSprite, _ => (CanKunaiInInvisibily || !isInvisible)).SetAsUsurpableButton(this);

                equipButton.OnClick = (button) =>
                {
                    if (MyKunai == null) { EquipKunai(); equipButton.SetLabel("unequip"); }
                    else { UnequipKunai(); equipButton.SetLabel("equip"); }
                };
                equipButton.OnBroken = (button) =>
                {
                    if (MyKunai != null) { UnequipKunai(); equipButton.SetLabel("equip"); }
                    Snatcher.RewindKillCooldown();
                };
                equipButton.SetLabel("equip");

                var killButton = NebulaAPI.Modules.AbilityButton(this, isArrangedAsKillButton: true)
                    .BindKey(Virial.Compat.VirtualKeyInput.Kill, "kunoichiU.kill")
                    .SetLabel("KunaiThrow").SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor)
                    .SetAsMouseClickButton().SetAsUsurpableButton(this);

                killButton.Availability = (button) => MyKunai != null && MyPlayer.CanMove && MyKunai.CanThrow;
                killButton.Visibility = (button) => !MyPlayer.IsDead && !equipButton.IsBroken;
                killButton.OnClick = (button) =>
                {
                    if (MyKunai != null)
                    {
                        MyKunai.Throw(MyKunai.Position, MyPlayer.Unbox().MouseAngle);
                        EquipKunai();
                    }
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KunaiCooldown).SetAsKillCoolTimer().Start();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());

                invisibleButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                    InvTime, "KunoichiInvisibily", InvisibleButtonSprite, _ => MoveTime >= 3f);
                invisibleButton.Visibility = (button) => !MyPlayer.IsDead && CanInvisibily && UseInvisibleButton;
                invisibleButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                invisibleButton.OnClick = (button) =>
                {
                    if (!isInvisible)
                    {
                        invisibleButton.SetLabel("Kunoichirelease");
                        isInvisible = true;
                        MyPlayer.GainAttribute(PlayerAttributes.Invisible, 9999f, false, 100);
                        if (!CanKunaiInInvisibily && MyKunai != null) { UnequipKunai(); equipButton.SetLabel("equip"); }
                    }
                    else
                    {
                        invisibleButton.SetLabel("KunoichiInvisibily");
                        isInvisible = false;
                        MyPlayer.RealPlayer.RemoveAttribute(PlayerAttributes.Invisible);
                    }
                };
            }
        }

        private void InitializeIcons()
        {
            if (isIconInitialized || MeetingHud.Instance || !AmOwner) return;
            isIconInitialized = true;

            iconHolder = new UchuPlayersIconHolder(true).Register(this);
            iconHolder.XInterval = 0.35f;

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.PlayerId == MyPlayer.PlayerId || p.Data.IsDead) continue;
                var gp = GamePlayer.GetPlayer(p.PlayerId);
                if (gp == null) continue;
                if (!CanKillImpostorOption && !MyPlayer.CanKill(gp)) continue;

                iconHolder.AddPlayer(gp);
            }
        }

        void GameUpdate(GameUpdateEvent ev)
        {
            if (!AmOwner) return;

            if (!isIconInitialized && !MeetingHud.Instance) InitializeIcons();

            if (iconHolder != null)
            {
                foreach (var icon in iconHolder.AllIcons.ToArray())
                {
                    int count = KunoichiKunai.hitCount.GetValueOrDefault(icon.Player.PlayerId);
                    int remaining = NumOfHit - count;

                    if (remaining <= 0)
                    {
                        iconHolder.Remove(icon);
                    }
                    else
                    {
                        icon.SetText(remaining.ToString());
                    }
                }
            }

            if (!MyPlayer.CanMove || (MeetingHud.Instance && CanInvisibily))
            {
                MoveTime = 0f;
                lastPosition = MyPlayer.TruePosition;
                return;
            }

            UnityEngine.Vector2 currentPosition = MyPlayer.TruePosition;

            if ((currentPosition - lastPosition).magnitude > 0.01f)
            {
                MoveTime = 0f;
                if (isInvisible && CanInvisibily)
                {
                    isInvisible = false;
                    MyPlayer.RealPlayer.RemoveAttribute(PlayerAttributes.Invisible);
                    if (UseInvisibleButton && invisibleButton != null) invisibleButton.SetLabel("KunoichiInvisibily");
                }
                if (UseInvisibleButton && invisibleButton != null) invisibleButton.StartCoolDown();
            }
            else
            {
                MoveTime += Time.deltaTime;
                if (!UseInvisibleButton && MoveTime >= InvTime && !isInvisible && CanInvisibily)
                {
                    isInvisible = true;
                    MyPlayer.GainAttribute(PlayerAttributes.Invisible, 9999f, false, 100);
                    if (!CanKunaiInInvisibily && MyKunai != null) { UnequipKunai(); equipButton.SetLabel("equip"); }
                }
            }
            lastPosition = currentPosition;
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            UnequipKunai();
            equipButton?.SetLabel("equip");
            MoveTime = 0f;
            isInvisible = false;
            MyPlayer.RealPlayer.RemoveAttribute(PlayerAttributes.Invisible);

            if (ResetHitCountOnMeeting)
            {
                KunoichiKunai.ResetHitCount();
            }

            foreach (var icon in iconHolder.AllIcons.ToArray())
            {
                if (icon.Player.IsDead) iconHolder.Remove(icon);
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyKunai != null) UnequipKunai();
        }

        [OnlyMyPlayer]
        [Local]
        void EquipKunai() => MyKunai = (NebulaSyncObject.RpcInstantiate(KunoichiKunai.MyTag, [(float)PlayerControl.LocalPlayer.PlayerId]).SyncObject as KunoichiKunai);

        [OnlyMyPlayer]
        [Local]
        void UnequipKunai()
        {
            if (MyKunai != null) NebulaSyncObject.RpcDestroy(MyKunai.ObjectId);
            MyKunai = null;
        }

        private static int KunaiIgnoreLayerMask = 1 << LayerExpansion.GetRaiderColliderLayer();
        public static bool OverlapKunaiIgnoreArea(UnityEngine.Vector2 pos) => Physics2D.OverlapPoint(pos, KunaiIgnoreLayerMask);
    }
}