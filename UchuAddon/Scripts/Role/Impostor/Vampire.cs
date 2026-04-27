using AsmResolver.DotNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Injection;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Media;
using static Hori.Scripts.Role.Impostor.VampireU;

namespace Hori.Scripts.Role.Impostor;

public class VampireU : DefinedSingleAbilityRoleTemplate<VampireU.Ability>, DefinedRole
{
    public VampireU() : base("vampireU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [BiteCoolDownOption, KillDelayOption, NumOfGarlicOption, GarlicCoolDownOption, MineSizeOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Vampire.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.VampireU.BiteSkill", GameStatsCategory.Roles, MyRole);
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    static private readonly IRelativeCooldownConfiguration BiteCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.VampireU.BiteCooldown", CoolDownType.Relative, (10f, 45f, 2.5f), 30f, (-20f, 45f, 2.5f), 0f, (0.125f, 2f, 0.125f), 1f);
    static private FloatConfiguration KillDelayOption = NebulaAPI.Configurations.Configuration("options.role.VampireU.KillDelay", (5f, 15f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfGarlicOption = NebulaAPI.Configurations.Configuration("options.role.VampireU.numOfGarlic", (1, 7), 1);
    static public readonly FloatConfiguration GarlicCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.VampireU.GarlicCoolDown", (5f, 20f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration MineSizeOption = NebulaAPI.Configurations.Configuration("options.role.VampireU.mineSize", (1f, 5f, 0.5f), 2.5f, FloatConfigurationDecorator.Ratio);
    static public VampireU MyRole = new();
    static public float KillCooldown => BiteCoolDownOption.Cooldown;

    static private readonly Virial.Media.Image GarlicButtonImage = NebulaAPI.AddonAsset.GetResource("GarlicButton.png")!.AsImage(115f)!;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Vampire.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image BiteImage = NebulaAPI.AddonAsset.GetResource("BiteButton.png")!.AsImage(115f)!;

        ModAbilityButton? biteButton = null;
        private int[]? globalgarlics = null;
        List<NebulaSyncStandardObject> localGarlics = null!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool IPlayerAbility.HideKillButton => !(biteButton?.IsBroken ?? false);
        public bool IsInGarlicRange { get; set; }

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                myTracker.SetColor(MyRole.RoleColor);

                biteButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                    BiteCoolDownOption.GetCooldown(MyPlayer.TeamKillCooldown), "bite", ModAbilityButton.LabelType.Impostor, null!, (target, _) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            NebulaManager.Instance.StartCoroutine(CoDelayKill(target.RealPlayer).WrapToIl2Cpp());
                        }
                        biteButton.StartEffect();
                    },
                    null,
                    _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                    _ => MyPlayer.AllowToShowKillButtonByAbilities);
                biteButton.Visibility = (button) => !MyPlayer.IsDead;

                biteButton.OnEffectStart = (button) =>
                {
                    biteButton.StartCoolDown();
                };
                biteButton.Availability = (button) => myTracker.CurrentTarget != null && MyPlayer.CanMove && !IsInGarlicRange;
                biteButton.SetImage(BiteImage);
                biteButton.EffectTimer = NebulaAPI.Modules.Timer(this, KillDelayOption);
                biteButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldown).SetAsAbilityTimer().Start();
            }
        }

        IEnumerator CoDelayKill(GamePlayer victim)
        {
            if (!IsInGarlicRange)
            {
                yield return Effects.Wait(KillDelayOption);
            }
            MyPlayer.MurderPlayer(victim, PlayerState.Dead, EventDetail.Kill, KillParameter.RemoteKill);
        }

        void OnUpdate(GameUpdateEvent ev)
        {
            if (!AmOwner) return;
            IsInGarlicRange = false;
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class Garlic : NebulaSyncStandardObject
    {
        public const string MyTag = "Garlic";
        private static readonly Image garlicImage = NebulaAPI.AddonAsset.GetResource("Garlic.png")!.AsImage(350f)!;
        public Garlic(UnityEngine.Vector2 pos) : base(pos, ZOption.Just, true, garlicImage.GetSprite()) { }

        public override void OnInstantiated()
        {
            base.OnInstantiated();

            var smokeObj = UnityHelper.CreateObject<SpriteRenderer>("GarlicSmoke", null, new UnityEngine.Vector3(Position.x, Position.y, -6f));
            smokeObj.sprite = NebulaAPI.AddonAsset.GetResource("Smoke.png")!.AsImage(100f)!.GetSprite();
            float scale = ((float)MineSizeOption * 2f) / 3f;
            smokeObj.transform.localScale = new UnityEngine.Vector3(scale, scale, 1f);
            smokeObj.color = new UnityEngine.Color(0.4f, 0.9f, 0.2f, 0.35f);
            smokeObj.sortingOrder = 0;
        }

        void OnUpdate(GameUpdateEvent ev)
        {
            if (MeetingHud.Instance || ExileController.Instance) return;

            var local = GamePlayer.LocalPlayer;
            if (local == null || local.IsDead) return;

            if (!local.TryGetAbility<VampireU.Ability>(out var ability)) return;

            if (Position.Distance(local.TruePosition) < MineSizeOption)
            {
                ability.IsInGarlicRange = true;
            }
        }

        static Garlic()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new Garlic(new(args[0], args[1])));
        }
    }

    [NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
    public class GarlicSystem : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static GarlicSystem() => DIManager.Instance.RegisterModule(() => new GarlicSystem());
        protected override void OnInjected(Virial.Game.Game container) => this.Register(container);
        static public GarlicSystem Instance { get; private set; }
        private GarlicSystem()
        {
            Instance = this;
        }

        void OnGameStart(GameStartEvent ev)
        {
            //ヴァンパイアの出現確率が0じゃない、またはフリープレイならボタンを生成
            if (MyRole.IsSpawnableInSomeForm() || GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.FreePlay)
            {
                var local = GamePlayer.LocalPlayer;
                if (local == null) return;

                int leftGarlic = NumOfGarlicOption;

                var placeButton = NebulaAPI.Modules.AbilityButton(null, local, false, true, Virial.Compat.VirtualKeyInput.None, null, GarlicCoolDownOption, "vampireU.garlic", GarlicButtonImage, null);
                placeButton.Visibility = (button) => !local.IsDead && leftGarlic > 0;
                placeButton.ShowUsesIcon(3, leftGarlic.ToString());
                placeButton.OnClick = (button) =>
                {
                    if (leftGarlic <= 0) return;

                    var pos = PlayerControl.LocalPlayer.GetTruePosition();
                    NebulaSyncObject.RpcInstantiate(Garlic.MyTag, new float[] { pos.x, pos.y });

                    button.StartCoolDown();
                    leftGarlic--;
                    placeButton.UpdateUsesIcon(leftGarlic.ToString());
                };
            }
        }
    }
}