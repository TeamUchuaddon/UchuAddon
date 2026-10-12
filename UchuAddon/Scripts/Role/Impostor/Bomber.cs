using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Il2CppInterop.Runtime;
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
using System.Collections;
using System.Collections.Generic;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Media;
using static Nebula.Roles.Impostor.Thurifer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

[NebulaRPCHolder]
public class BomberU : DefinedSingleAbilityRoleTemplate<BomberU.Ability>, DefinedRole, IAssignableDocument
{
    public BomberU() : base("bomberU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [BombCooldownOption, BombDurationOption, SelfBombCooldownOption, SelfBombDurationOption, BombRangeOption, MeetingStartActionOption, CanNormalKillOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Bomber.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        MetaAbility.RegisterCircle(new("role.bomberU.bombRange", () => BombRangeOption, () => null, Color.Red));
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration BombCooldownOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.BombCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration BombDurationOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.BombDuration", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SelfBombCooldownOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.SelfBombCooldown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SelfBombDurationOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.SelfBombDuration", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration BombRangeOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.BombRangeOption", (0.5f, 15f, 0.5f), 4f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration ExplosionSizeOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.ExplosionSize", (0.5f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Ratio);
    static private ValueConfiguration<int> MeetingStartActionOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.meetingact", ["options.role.bomberU.meetingact.Kill", "options.role.bomberU.meetingact.progress"], 0);
    static private BoolConfiguration CanNormalKillOption = NebulaAPI.Configurations.Configuration("options.role.bomberU.cannormalkill", true);
    static public BomberU MyRole = new();


    static private Image BombImage = NebulaAPI.AddonAsset.GetResource("Bomber_Button.png")!.AsImage(115f)!;
    static private Image IgniteImage = NebulaAPI.AddonAsset.GetResource("BomberIgniteBuotton.png")!.AsImage(115f)!;
    static private Image SelfBombImage = NebulaAPI.AddonAsset.GetResource("BomberSelfFireBuotton.png")!.AsImage(115f)!;
    static private Image CountBombImage = NebulaAPI.AddonAsset.GetResource("CountFireButton.png")!.AsImage(115f)!;
    private static MultiImage bombSprite = NebulaAPI.AddonAsset.GetResource("Bomb.png")!.AsMultiImage(3, 4, 500f)!;
    static private readonly IDividedSpriteLoader ExplosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 100f, 4, 2);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Bomber.png")!.AsImage(100f)!;

    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(IgniteImage, "role.bomberU.ability.ignite");
        yield return new(BombImage, "role.bomberU.ability.pass");
        yield return new(SelfBombImage, "role.bomberU.ability.self");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%MEETING%", Language.Translate(MeetingStartActionOption.GetValue() == 0 ? "role.bomberU.ability.main.bomb" : "role.bomberU.ability.main.timer"));
    }

    [NebulaRPCHolder]
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool IPlayerAbility.HideKillButton => !CanNormalKillOption || (ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer) && ModSingleton<BombManager>.Instance.bombs[MyPlayer].Renderer == null);

        [NebulaRPCHolder]
        class BombSystem : FlexibleLifespan, IGameOperator
        {
            public GamePlayer Owner { get; }
            public GamePlayer CurrentHolder { get; set; }

            public SpriteRenderer? Renderer { get; private set; }


            SpriteRenderer? SubRenderer { get; set; }

            private float _timer = 0f;
            private float _timeToExplode = 0f;
            private int _counter = 0;

            public BombSystem(GamePlayer owner)
            {
                Owner = owner;
                CurrentHolder = owner;

                if (Owner.AmOwner)
                {
                    SubRenderer = UnityHelper.CreateObject<SpriteRenderer>("Bomb", owner.VanillaPlayer.transform, new(0.15f, 1.1f, 0f));
                    SubRenderer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                    SubRenderer.color = new (1f, 1f, 1f, 0.5f);
                }

                GameOperatorManager.Instance?.Subscribe(component: this, this);
            }

            void OnGameUpdate(GameUpdateEvent ev)
            {
                _timer += Time.deltaTime;
                if (Renderer != null) _timeToExplode += Time.deltaTime;

                if (_timer >= 0.08f)
                {
                    _timer -= 0.08f;
                    _counter = (_counter + 1) % 12;
                    if (Renderer != null) Renderer.sprite = bombSprite.GetSprite(_counter);
                    if (SubRenderer != null) SubRenderer.sprite = bombSprite.GetSprite(_counter);
                }

                if (_timeToExplode > BombDurationOption && Renderer != null) Explode();

            }

            void OnUpdatePlayerVisibility(PlayerUpdateVisibilityEvent ev)
            {
                if (ev.Player == CurrentHolder)
                {
                    if (Renderer != null)
                    {
                        if (ev.Visibility == PlayerUpdateVisibilityEvent.VisibilityLevel.Invisible)
                            Renderer.color = new (1f, 1f, 1f, 0f);
                        else if (ev.Visibility == PlayerUpdateVisibilityEvent.VisibilityLevel.SemiTransparent)
                            Renderer.color = new (1f, 1f, 1f, 0.5f);
                        else
                            Renderer.color = new (1f, 1f, 1f, 1f);
                    }

                    if (SubRenderer != null)
                    {
                        if (ev.Visibility == PlayerUpdateVisibilityEvent.VisibilityLevel.Invisible)
                            SubRenderer.color = new (1f, 1f, 1f, 0f);
                        else
                            SubRenderer.color = new (1f, 1f, 1f, 0.5f);
                    }
                }

            }

            void OnEnterVent(PlayerVentEnterEvent ev)
            {
                if (ev.Player == CurrentHolder)
                {
                    if (Renderer != null) Renderer.color = new (1f, 1f, 1f, 0f);
                    if (SubRenderer != null) SubRenderer.color = new (1f, 1f, 1f, 0f);
                }
            }


            public void Explode()
            {

                if (Owner.AmOwner)
                {
                    Owner.MurderPlayer(CurrentHolder, PlayerState.Dead, EventDetail.Kill, KillParameter.WithAssigningGhostRole | KillParameter.WithDeadBody, KillCondition.TargetAlive);
                }

                NebulaManager.Instance.StartCoroutine(CoExplodeAnim().WrapToIl2Cpp());
                this.Release();
            }

            [NebulaRPC]
            public static void SelfExplode(GamePlayer owner)
            {

                var bomb = ModSingleton<BombManager>.Instance.bombs.Values
                        .FirstOrDefault(b => b.Owner == owner);
                if (bomb == null) return;

                if (owner.AmOwner)
                {
                    foreach (var player in NebulaGameManager.Instance!.AllPlayerInfo
                        .Where(p => p.TruePosition.Distance(owner.TruePosition) <= ExplosionSizeOption))
                    {
                        owner.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill,
                            KillParameter.WithAssigningGhostRole | KillParameter.WithDeadBody,
                            KillCondition.TargetAlive);
                    }
                }

                NebulaManager.Instance.StartCoroutine(bomb.CoExplodeAnim().WrapToIl2Cpp());
                bomb.Release();
            }

            void IGameOperator.OnReleased()
            {
                ModSingleton<BombManager>.Instance.bombs.Remove(CurrentHolder);
                if (Renderer != null) GameObject.Destroy(Renderer.gameObject);
                if (SubRenderer != null) GameObject.Destroy(SubRenderer.gameObject);
                Renderer = null;
                SubRenderer = null;
            }

            void OnPlayerDie(PlayerDieOrDisconnectEvent ev)
            {
                if (ev.Player == CurrentHolder)
                {
                    Release();
                }
            }

            IEnumerator CoExplodeAnim()
            {
                NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear, CurrentHolder.TruePosition, 4f, 12f);
                if (CurrentHolder.AmOwner) NebulaManager.Instance.StartDelayAction(0.8f, () => Helpers.PlayKillStingerSE());

                var explosion = UnityHelper.CreateObject<SpriteRenderer>("Explosion", null, CurrentHolder.Position.AsVector3(-10f));
                explosion.transform.localScale *= 1.8f;
                explosion.transform.localEulerAngles = new(0f, 0f, System.Random.Shared.NextSingle() * 360f);

                for (int i = 0; i < 8; i++)
                {
                    explosion.sprite = ExplosionSprite.GetSprite(i);
                    yield return Effects.Wait(0.06f);
                }

                GameObject.Destroy(explosion.gameObject);

            }


            void OnMeetingStart(MeetingEvent ev)
            {
                if (MeetingStartActionOption.GetValue() == 0) Explode();
                if (Renderer != null) Renderer.color = new (1f, 1f, 1f, 1f);
                if (SubRenderer != null) SubRenderer.color = new (1f, 1f, 1f, 0.5f);
            }


            [NebulaRPC]
            public static void Pass(GamePlayer owner, GamePlayer target)
            {

                var bomb = ModSingleton<BombManager>.Instance.bombs.Values
                        .FirstOrDefault(b => b.CurrentHolder == owner);
                if (bomb == null) return;

                ModSingleton<BombManager>.Instance.bombs.Remove(bomb.CurrentHolder);
                ModSingleton<BombManager>.Instance.bombs[target] = bomb;
                bomb.CurrentHolder = target;

                if (bomb.SubRenderer != null)
                {
                    GameObject.Destroy(bomb.SubRenderer.gameObject);
                    bomb.SubRenderer = null;
                }
                if (bomb.Renderer == null)
                {
                    bomb.Renderer = UnityHelper.CreateObject<SpriteRenderer>("Bomb", target.VanillaPlayer.transform, new(0.15f, 1.1f, 0f));
                    bomb.Renderer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }


                if (bomb.Renderer != null) bomb.Renderer.transform.SetParent(target.VanillaPlayer.transform, false);

                if (target.AmOwner) AmongUsUtil.PlayFlash(Color.Red);
            }
        }

        [NebulaPreprocess(PreprocessPhase.PostRoles)]
        [NebulaRPCHolder]
        class BombManager : AbstractModule<Virial.Game.Game>, IGameOperator
        {
            static BombManager() => DIManager.Instance.RegisterModule(() => new BombManager());
            private BombManager()
            {
                ModSingleton<BombManager>.Instance = this;
                this.RegisterPermanently();
            }

            public Dictionary<GamePlayer, BombSystem> bombs = new();

            void OnGameStart(GameStartEvent ev)
            {
                var bombPlayerTracker = NebulaAPI.Modules.PlayerTracker(NebulaAPI.CurrentGame!, GamePlayer.LocalPlayer!, player => ModSingleton<BombManager>.Instance.bombs.ContainsKey(GamePlayer.LocalPlayer!) && !ModSingleton<BombManager>.Instance.bombs.ContainsKey(player));
                bombPlayerTracker.SetColor(Palette.ImpostorRed);

                var passButton = NebulaAPI.Modules.InteractButton(NebulaAPI.CurrentGame!, GamePlayer.LocalPlayer!, bombPlayerTracker, new PlayerInteractParameter(true, false, false), false, true, VirtualKeyInput.None, null, 0.5f, "bomberU.pass", BombImage, (player, button) =>
                {
                    BombSystem.Pass(GamePlayer.LocalPlayer!, player);
                });
                passButton.Visibility = button => ModSingleton<BombManager>.Instance.bombs.ContainsKey(GamePlayer.LocalPlayer!) && ModSingleton<BombManager>.Instance.bombs[GamePlayer.LocalPlayer!].Renderer != null;
            }
        }

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {

                ModAbilityButton? selfExplodeTimer = null;
                ModAbilityButton? selfExplodeButton = null;

                var igniteButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, BombCooldownOption, "Ignite", IgniteImage).SetAsUsurpableButton(this);
                igniteButton.Visibility = _ => !ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer) && !MyPlayer.IsDead;
                igniteButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                igniteButton.OnClick = button =>
                {
                    IgniteBomb(MyPlayer);
                    selfExplodeTimer!.StartEffect();
                    selfExplodeButton?.StartCoolDown();
                };

                var bombButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Kill, 0f, "Bomb", BombImage, button => GetBombTarget(out _), _ => ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer) && ModSingleton<BombManager>.Instance.bombs[MyPlayer].Renderer == null).SetAsUsurpableButton(this);
                bombButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                bombButton.OnClick = button =>
                {
                    GetBombTarget(out var target);
                    if (target != null)
                    {
                        BombSystem.Pass(MyPlayer, target);
                      
                        selfExplodeTimer!.InterruptEffect();
                        selfExplodeTimer.StartCoolDown();
                        igniteButton.StartCoolDown();
                        
                    }
                };

                selfExplodeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, SelfBombCooldownOption, "bomberU.selfbomb", SelfBombImage, null, _ => ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer) && ModSingleton<BombManager>.Instance.bombs[MyPlayer].Renderer == null).SetAsUsurpableButton(this);
                selfExplodeButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                selfExplodeButton.OnClick = button =>
                {
                    BombSystem.SelfExplode(MyPlayer);
                    selfExplodeTimer!.InterruptEffect();
                    selfExplodeTimer.StartCoolDown();
                };

                selfExplodeTimer = NebulaAPI.Modules.EffectButton(this, MyPlayer, VirtualKeyInput.None, 0, SelfBombDurationOption, "LeftSelfBomb", CountBombImage).SetAsUsurpableButton(this);
                selfExplodeTimer.Visibility = _ => ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer) && ModSingleton<BombManager>.Instance.bombs[MyPlayer].Renderer == null;
                selfExplodeTimer.OnEffectEnd = button =>
                {
                    if (ModSingleton<BombManager>.Instance.bombs.ContainsKey(MyPlayer))
                    {
                        BombSystem.SelfExplode(MyPlayer);
                    }
                };
            }
        }

        [NebulaRPC]
        public static void IgniteBomb(GamePlayer owner)
        {
            ModSingleton<BombManager>.Instance.bombs[owner] = new BombSystem(owner);
        }

        bool GetBombTarget(out GamePlayer? target)
        {
            target = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p != MyPlayer && !p.IsDead && !ModSingleton<BombManager>.Instance.bombs.ContainsKey(p) && !p.VanillaPlayer.inVent && !p.IsDived && p.TruePosition.Distance(MyPlayer.TruePosition) <= BombRangeOption).OrderBy(_ => System.Random.Shared.Next()).FirstOrDefault();
            return target != null;
        }
    }
}


