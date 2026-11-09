using AmongUs.Data.Legacy;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Ghost.Complex;
using Hori.Scripts.Role.Impostor;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Neutral;


public class SeedU : DefinedRoleTemplate, DefinedRole
{
    public static Team MyTeam = new Team("teams.seedU", new Virial.Color(106, 178, 189), TeamRevealType.OnlyMe);
    public static ExtraWin SeedExtra = NebulaAPI.Preprocessor!.CreateExtraWin("seedU", SeedU.MyTeam.Color);
    private SeedU() : base("seedU", new(106, 178, 189), RoleCategory.NeutralRole, MyTeam, [SeedCooldown,NumOfSeed,CanSeeRoleOption,DieBlockWins,PlayerDieBlockWins,
     new GroupConfiguration("options.role.seed.group.gauge",[SeedWinGauge, RequiredGaugeToWin, SurplusGauge, GaugeReductionSpeed], GroupConfigurationColor.ToDarkenColor(MyTeam.UnityColor))])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Seed.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [EraserU.MyRole.ConfigurationHolder!]);
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private FloatConfiguration SeedCooldown = NebulaAPI.Configurations.Configuration("options.role.seedU.seedCooldown", (2.5f, 30f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfSeed = NebulaAPI.Configurations.Configuration("options.role.seedU.numOfSeed", (1, 10), 2);
    static private BoolConfiguration DieBlockWins = NebulaAPI.Configurations.Configuration("options.role.seedU.dieBlockWins", true);
    static private BoolConfiguration PlayerDieBlockWins = NebulaAPI.Configurations.Configuration("options.role.seedU.playerDieBlockWins", true);
    static private BoolConfiguration SeedWinGauge = NebulaAPI.Configurations.Configuration("options.role.seedU.seedGauge", true);
    static private FloatConfiguration RequiredGaugeToWin = NebulaAPI.Configurations.Configuration("options.role.seedU.requiredGaugeToWin", (10f, 150f, 5f), 20f, FloatConfigurationDecorator.Second, () => SeedWinGauge);
    static private FloatConfiguration SurplusGauge = NebulaAPI.Configurations.Configuration("options.role.seedU.surplusGauge", (5f, 80f, 5f), 20f, FloatConfigurationDecorator.Second, () => SeedWinGauge);
    static private FloatConfiguration GaugeReductionSpeed = NebulaAPI.Configurations.Configuration("options.role.seedU.gaugeReductionRatio", (0f, 2f, 0.125f), 0.25f, FloatConfigurationDecorator.Ratio, () => SeedWinGauge);
    static internal BoolConfiguration CanSeeRoleOption = NebulaAPI.Configurations.Configuration("options.role.seedU.canSeeRole", true);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Seed.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public SeedU MyRole = new SeedU();

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;

        static private Image SeedImage = NebulaAPI.AddonAsset.GetResource("SeedButton.png")!.AsImage(115f)!;

        int left = NumOfSeed;
        private int SeedAllCount = 0;
        private List<GamePlayer>? SeedTargets = new List<GamePlayer>();
        private SeedGaugeAbility SeedGauge = null!;
        private float SeedGaugeValue = 0f;
        private float SeedGaugeMax => RequiredGaugeToWin + SurplusGauge;
        private float SeedGaugeThreshold => RequiredGaugeToWin;
        private bool SeedGaugeWin => SeedGaugeValue >= SeedGaugeThreshold;

        public static MultiImage GaugeIcons = NebulaAPI.AddonAsset.GetResource("SeedGaugeIcon.png")!.AsMultiImage(2, 1, 160f)!;

        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);
                var seedButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, SeedCooldown, "seedSkill", SeedImage, _ => playerTracker.CurrentTarget != null && left != 0 && !SeedTargets!.Contains(playerTracker.CurrentTarget));
                seedButton.Visibility = (button) => !MyPlayer.IsDead && left > 0;
                seedButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        left--;
                        SeedAllCount++;
                        if (SeedAllCount >= 1)
                        {
                            //new StaticAchievementToken("seedU.common1");
                        }
                        seedButton.UpdateUsesIcon(left.ToString());
                        RpcShareSeedTarget.Invoke((MyPlayer, target));
                        seedButton.StartCoolDown();
                    }
                };
                seedButton.ShowUsesIcon(3, left.ToString());

                SeedGauge = new SeedGaugeAbility(SeedGaugeMax, SeedGaugeThreshold, 340f, GaugeIcons.AsLoader(0), GaugeIcons.AsLoader(1), () => SeedGaugeValue, () => SeedGaugeActive).Register(this);
                SeedGauge.SetActive(false);
            }
        }

        [Local]
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (SeedTargets == null) return;
            if (SeedTargets.Contains(ev.Player))
            {
                ev.Name += " ▼".Color(new UnityEngine.Color(106f / 255f, 178f / 255f, 189f / 255f));
            }
        }

        [Local]
        void RoleInfoVisibilityLocalEvent(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            if (CanSeeRoleOption && SeedTargets != null && SeedTargets.Contains(ev.Player)) ev.CanSeeAll = true;
        }

        static private readonly RemoteProcess<(GamePlayer myPlayer, GamePlayer target)> RpcShareSeedTarget = new("RpcShareSeedTarget_Uchu", (message, _) =>
        {
            var seed = message.myPlayer.Role as SeedU.Instance;
            if (seed != null && seed.SeedTargets != null)
            {
                seed.SeedTargets.Add(message.target);
            }
        });

        [OnlyMyPlayer]
        void CheckSeedExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (SeedWinGauge && !SeedGaugeWin) return; // ゲージが必要かつ、溜まっていない場合敗北
            if (SeedTargets == null || !SeedTargets.Any()) return; // リストに人がいない場合敗北

            bool anyoneWon = SeedTargets.Any(p => // 共生先が勝利しているか、オプションがオンの場合死亡していないかをチェック
            {
                if (!ev.WinnersMask.Test(p)) return false;
                if (PlayerDieBlockWins && p.IsDead) return false;

                return true;
            });

            if (!anyoneWon) return; // 相乗りできる共生先がいないなら敗北

            if (DieBlockWins && MyPlayer.IsDead) return; // 自身の生存が必要かつ死亡している場合敗北

            // 勝利
            ev.SetWin(true);
            ev.ExtraWinMask.Add(SeedExtra);
            ev.IsExtraWin = true;
        }
        
        /*
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            //勝利
            if (ev.EndState.Winners.Test(MyPlayer))
            {
                new StaticAchievementToken("seedU.hard1");

                if (SeedTargets != null && SeedTargets.Count == 1)
                {
                    new StaticAchievementToken("seedU.challenge1");
                }
            }
            //敗北
            else
            {
                if (left < NumOfSeed)
                {
                    new StaticAchievementToken("seedU.another1");
                }
            }
        }
        */
        
        float shareInterval = 0.5f;
        float progressGrace = 0f;
        private bool SeedGaugeActive { get; set; } = false;

        [Local]
        void OnUpdate(GameUpdateEvent ev) 
        {
            if (!SeedWinGauge) return;
            if (SeedTargets == null || MyPlayer.IsDead) 
            {
                SeedGauge.SetActive(false);
                return;
            }
            SeedGauge.SetActive(true); 

            SeedGaugeActive = false;

            if (MeetingHud.Instance || ExileController.Instance)
            {
                if (!(progressGrace > 0f))
                {
                    RpcShareSeedGauge.Invoke((MyPlayer, SeedGaugeValue));
                    progressGrace = 5f;
                    shareInterval = 0.5f;
                }
                return;
            }

            if (progressGrace > 0f)
            {
                progressGrace -= Time.deltaTime;
                return;
            }

            if (SeedTargets.Any(target => target.Position.Distance(MyPlayer.Position) < 2f && !target.IsInvisible))
            {
                SeedGaugeValue = Math.Clamp(SeedGaugeValue + Time.deltaTime, 0f, SeedGaugeMax);
                SeedGaugeActive = true;
            }
            else
            {
                SeedGaugeValue = Math.Clamp(SeedGaugeValue - Time.deltaTime * GaugeReductionSpeed, 0f, SeedGaugeMax);
                SeedGaugeActive = false;
            }

        }
        static private RemoteProcess<(GamePlayer seed, float gauge)> RpcShareSeedGauge = new("ShareSeedGauge", (message, _) =>
        {
            if (message.seed.Role is Instance seedInstance) seedInstance.SeedGaugeValue = message.gauge;
        }, false);
    }
}