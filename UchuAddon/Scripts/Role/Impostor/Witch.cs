using AsmResolver.PE.DotNet.ReadyToRun;
using Hori.Core;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Crewmate;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Achievements;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Runtime;
using Color = Virial.Color;
using compat = Hori.Core.UchuTranslatable;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class WitchU : DefinedSingleAbilityRoleTemplate<WitchU.Ability>, DefinedRole, IAssignableDocument, HasCitation
{
    public WitchU() : base("witchU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SpellCooldown, SpellDuration, NumOfOneTurnSpell, WitchNormalKill])
    {
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers; //ジャッカル・タイラント化
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SpellCooldown = NebulaAPI.Configurations.Configuration("options.role.witchU.spellCooldown", (5f, 60f, 5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SpellDuration = NebulaAPI.Configurations.Configuration("options.role.witchU.spellDuration", (1f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfOneTurnSpell = NebulaAPI.Configurations.Configuration("options.role.witchU.numOfOneTurnSpell", (1, 15), 1);
    static private BoolConfiguration WitchNormalKill = NebulaAPI.Configurations.Configuration("option.role.witchU.witchNormalKill", true);

    static public WitchU MyRole = new();

    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility; //アマルガム、種類は合わせて変える

    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Witch.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.common1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.another1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.another2")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.hard1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.challenge1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow,
        });
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    public class WitchManager : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static WitchManager() => DIManager.Instance.RegisterModule(() => new WitchManager());

        private readonly List<(GamePlayer Killer, GamePlayer Target)> WitchGroups = new();

        private WitchManager()
        {
            ModSingleton<WitchManager>.Instance = this;
            this.RegisterPermanently();
        }

        /// <summary>
        /// キラーとターゲットをペアとして登録する
        /// </summary>
        [NebulaRPC]
        public static void SetWitch(GamePlayer killer, GamePlayer target)
        {
            // WitchGroups.RemoveAll(x => x.Target == target);
            ModSingleton<WitchManager>.Instance.WitchGroups.Add((killer, target));
        }

        /// <summary>
        /// 余計な魔術を削除します。
        /// </summary>
        [NebulaRPC]
        public static void RemoveListFresh()
        {
            ModSingleton<WitchManager>.Instance.WitchGroups.RemoveAll(x => x.Target.IsDead || x.Killer.IsDead);
        }

        /// <summary>
        /// 魔術をすべて削除します。
        /// </summary>
        [NebulaRPC]
        public static void ClearWitch()
        {
            ModSingleton<WitchManager>.Instance.WitchGroups.Clear();
        }

        /// <summary>
        /// 対象が魔術を受けてるか判定
        /// </summary>
        internal bool IsTarget(GamePlayer player) => WitchGroups.Any(x => x.Target == player);

        /// <summary>
        /// どれでもいいから自分が魔術をかけてるか
        /// </summary>
        internal bool IsKiller(GamePlayer player) => WitchGroups.Any(x => x.Killer == player);

        /// <summary>
        /// 自分がKillerになっている魔術の数
        /// </summary>
        internal int GetKillerCount(GamePlayer player) => WitchGroups.Count(x => x.Killer == player);

        void OnVoteArea(SetUpVotingAreaEvent ev)
        {
            if (ev.Player.IsDead) return; // 死んでるプレイヤーにはマークをつけない（仕様変更）

            if (WitchGroups.Any(x => x.Target == ev.Player && !x.Killer.IsDead))
            {
                UnityHelper.SimpleAnimator(ev.VoteArea.transform, new Vector3(0f, 0f, -0.5f), 0.15f, _ => WitchMark.GetSprite());
            }
        }

        void OnMeetingStart(MeetingEvent ev)
        {
            RemoveListFresh();
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            foreach (var group in WitchGroups)
            {
                var killer = group.Killer;
                var target = group.Target;

                if (killer.IsDead || target.IsDead) continue;

                OnAchievementCheck(killer);

                target.Suicide(UchuTranslatable.WitchSpell, PlayerState.Suicide, KillParameter.RemoteKill | KillParameter.WithKillSEWidely);

                // killer.MurderPlayer(target, UchuTranslatable.WitchSpell, EventDetail.Kill, KillParameter.MeetingKill | KillParameter.WithAssigningGhostRole | KillParameter.WithKillSEWidely);
                // target.VanillaPlayer.ModMarkAsExtraVictim(ev.Player.VanillaPlayer, UchuTranslatable.WitchSpell, UchuTranslatable.WitchSpell);
            }

            ClearWitch();
        }

        [NebulaRPC]
        void OnAchievementCheck(GamePlayer killer)
        {
            if (killer.AmOwner)
            {
                if (IsKiller(killer))
                {
                    AchievementUnLock.TitleUnLock("uchuaddon.witchU.common1");
                }

                if(GetKillerCount(killer) >= 3)
                {
                    AchievementUnLock.TitleUnLock("uchuaddon.witchU.challenge1");
                }
            }
        }
    }

    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(SpellImage, "role.witchU.ability.spell");
    }

    static private readonly Virial.Media.Image SpellImage = NebulaAPI.AddonAsset.GetResource("WitchSpellButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image WitchMark = NebulaAPI.AddonAsset.GetResource("MarkWitch.png")!.AsImage(100f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        ModAbilityButton? SpellButton = null;
        bool WitchCheck = false; // 称号判定用
        int leftSpell = NumOfOneTurnSpell;

        bool IPlayerAbility.HideKillButton => !WitchNormalKill;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                SpellButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SpellCooldown, SpellDuration, "Spell", SpellImage, _ => playerTracker.CurrentTarget != null && !ModSingleton<WitchManager>.Instance.IsTarget(playerTracker.CurrentTarget)
                    && (!MyPlayer.IsImpostor || !playerTracker.CurrentTarget.IsImpostorlike)).SetAsUsurpableButton(this);

                SpellButton.Visibility = (button) => !MyPlayer.IsDead && leftSpell > 0;
                SpellButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                SpellButton.OnEffectStart = _ => playerTracker.KeepAsLongAsPossible = true;
                SpellButton.OnEffectEnd = (button) =>
                {
                    if (MeetingHud.Instance) return;

                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        AmongUsUtil.PlayCustomFlash(Color.Red, 0f, 0.25f, 0.4f);
                        WitchManager.SetWitch(MyPlayer, target);

                        WitchCheck = true;
                        leftSpell--;
                        SpellButton.UpdateUsesIcon(leftSpell.ToString());
                        SpellButton.StartCoolDown();
                    }
                };

                SpellButton.OnUpdate = (button) =>
                {
                    if (!button.IsInEffect) return;
                    if (playerTracker.CurrentTarget == null) button.InterruptEffect();
                };

                SpellButton.ShowUsesIcon(0, leftSpell.ToString());

                GameOperatorManager.Instance?.Subscribe<MeetingEndEvent>(ev =>
                {
                    leftSpell = NumOfOneTurnSpell;
                    SpellButton.UpdateUsesIcon(leftSpell.ToString());
                }, this);
            }
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (!ModSingleton<WitchManager>.Instance.IsKiller(MyPlayer)) return;
            if (ev.Player == MyPlayer) return;

            AchievementUnLock.TitleUnLock("uchuaddon.witchU.hard1");
        }

        [Local]
        void OnDeadTitle(PlayerDieEvent ev)
        {
            if (!ModSingleton<WitchManager>.Instance.IsKiller(MyPlayer)) return; //誰にも魔術を書けていないなら終わり

            if (MyPlayer.PlayerState == PlayerState.Guessed || MyPlayer.PlayerState == PlayerState.Exiled)
            {
                AchievementUnLock.TitleUnLock("uchuaddon.witchU.another2");
            }
        }

        [Local]
        void GameEndCheck(GameEndEvent ev)
        {
            if (WitchCheck) return;

            AchievementUnLock.TitleUnLock("uchuaddon.witchU.another1");
        }
    }
}

/*
[NebulaPreprocess(PreprocessPhase.Roles)]
public class WitchU : DefinedSingleAbilityRoleTemplate<WitchU.Ability>, DefinedRole, HasCitation
{
    public WitchU() : base("witchU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SpellCooldown, SpellDuration,NumOfOneTurnSpell,WitchNormalKill])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Witch.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SpellCooldown = NebulaAPI.Configurations.Configuration("options.role.witchU.spellCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SpellDuration = NebulaAPI.Configurations.Configuration("options.role.witchU.spellDuration", (1f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfOneTurnSpell = NebulaAPI.Configurations.Configuration("options.role.witchU.numOfOneTurnSpell", (1, 15), 1);
    static private BoolConfiguration WitchNormalKill = NebulaAPI.Configurations.Configuration("option.role.witchU.witchNormalKill", false);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Witch.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public WitchU MyRole = new();
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.common1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.another1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.another2")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.hard1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold,
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "witchU.challenge1")
        {
            RelatedRole = WitchU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow,
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image SpellImage = NebulaAPI.AddonAsset.GetResource("WitchSpellButton.png")!.AsImage(115f)!;
        static private readonly Virial.Media.Image WitchMark = NebulaAPI.AddonAsset.GetResource("MarkWitch.png")!.AsImage(100f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        static List<GamePlayer>? WitchTargets = new List<GamePlayer>();
        bool meetingStarted = false;
        bool WitchCheck = false;
        int leftSpell = NumOfOneTurnSpell;
        bool IPlayerAbility.HideKillButton => !WitchNormalKill;
        static Dictionary<byte, GameObject> WitchMarks = new();

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var SpellButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SpellCooldown, SpellDuration,"Spell", SpellImage, _ => playerTracker.CurrentTarget != null && playerTracker.CurrentTarget.RealPlayer.Role.Role.Team != NebulaTeams.ImpostorTeam&& !WitchTargets!.Contains(playerTracker.CurrentTarget)).SetAsUsurpableButton(this);
                SpellButton.Visibility = (button) => !MyPlayer.IsDead && leftSpell > 0;
                SpellButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                SpellButton.OnEffectStart = _ => playerTracker.KeepAsLongAsPossible = true;
                SpellButton.OnEffectEnd = (button) =>
                {
                    if (MeetingHud.Instance) return;

                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        WitchCheck = true;
                        leftSpell--;
                        SpellButton.UpdateUsesIcon(leftSpell.ToString());
                        RpcWitchTargets.Invoke(playerTracker.CurrentTarget!);
                        SpellButton.StartCoolDown();
                    }
                };

                SpellButton.OnUpdate = (button) =>
                {
                   SpellButton.UpdateUsesIcon(leftSpell.ToString());
                    if (!button.IsInEffect) return;
                    if (playerTracker.CurrentTarget == null) button.InterruptEffect();
                };

                SpellButton.ShowUsesIcon(0, leftSpell.ToString());
            }

        }
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingStarted = true;
            if (MyPlayer.IsDead) return;
            WitchTargets?.Remove(MyPlayer);

            if (WitchTargets == null) return;
            foreach (var p in WitchTargets)
            {
                UpdateWitchMark(p.PlayerId);
            }
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {         
            meetingStarted = false;
            leftSpell = NumOfOneTurnSpell;
           
            if (MyPlayer.IsDead) return;

            if (WitchTargets == null || WitchTargets.Count == 0) return;
            foreach (var target in WitchTargets.ToList())
            {
                if (target != null && !target.IsDead && target != MyPlayer)
                {
                    target.Suicide(compat.WitchSpell, PlayerState.Suicide, KillParameter.RemoteKill);
                    //MyPlayer.MurderPlayer(target, compat.WitchSpell, compat.WitchSpell, KillParameter.RemoteKill, KillCondition.)

                    TitleUnLock("uchuaddon.witchU.common1");   
                }
            }

            if (WitchTargets.Count >= 3)
            {
                TitleUnLock("uchuaddon.witchU.challenge1");
            }

            RpcWitchTargetsClear.Invoke();
        }

        [Local]
        void OnDeadTitle(PlayerDieEvent ev)
        {
            if (WitchTargets == null || WitchTargets.Count == 0) return; //誰にも魔術を書けていないなら終わり

            if (MyPlayer.PlayerState == PlayerState.Guessed || MyPlayer.PlayerState == PlayerState.Exiled)
            {
                TitleUnLock("uchuaddon.witchU.another2");
            }
        }

        /*
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (!meetingStarted) return;
            if (WitchTargets == null) return;

            if (WitchTargets.Contains(ev.Player) && ev.Player != MyPlayer)
            {
                ev.Name += " ∆".Color("#FF0000");
            }
        }

        RemoteProcess<GamePlayer> RpcWitchTargets = new("WitchTheSpell", (message, _) =>
        {
            WitchTargets?.Add(message);

            if (MeetingHud.Instance != null)
            {
                UpdateWitchMark(message.PlayerId);
            }
        });

        RemoteProcess RpcWitchTargetsClear = new("WitchTheSpellClear", (_) =>
        {
            WitchTargets?.Clear();

            ClearAllWitchMarks();
        });

        [Local]
        void GameStart(GameStartEvent ev)
        {
            RpcWitchTargetsClear.Invoke();
        }

        [Local]
        void GameEnd(GameEndEvent ev)
        {
            RpcWitchTargetsClear.Invoke();
        }

        
        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (WitchTargets?.Count == 0) return;
            if (ev.Player == MyPlayer) return;

            TitleUnLock("uchuaddon.witchU.hard1");
        }

        [Local]
        void GameEndCheck(GameEndEvent ev)
        {
            if (WitchCheck) return;

            TitleUnLock("uchuaddon.witchU.another1");
        }
        

        static void UpdateWitchMark(byte playerId)
        {
            if (WitchMarks.TryGetValue(playerId, out var oldObj) && oldObj)
            {
                UnityEngine.Object.Destroy(oldObj);
                WitchMarks.Remove(playerId);
            }

            var pva = MeetingHud.Instance?.GetPlayer(playerId);
            if (!pva) return;
            if (pva == null) return;

            var obj = UnityHelper.SimpleAnimator(pva.transform, new Vector3(0f, 0f, -0.5f), 0.15f, _ => WitchMark.GetSprite()).gameObject;

            obj.name = "WitchMark";
            obj.transform.localScale = Vector3.One;
            obj.transform.localPosition = new Vector3(0f, 0f, -0.0001f);

            var renderer = obj.GetComponent<SpriteRenderer>();


            WitchMarks[playerId] = obj;
        }

        static void ClearAllWitchMarks()
        {
            foreach (var obj in WitchMarks.Values)
            {
                if (obj) UnityEngine.Object.Destroy(obj);
            }
            WitchMarks.Clear();
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}
*/