using BepInEx.Unity.IL2CPP.Utils;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Documents;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles.Complex;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Image = Virial.Media.Image;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class MoiraU : DefinedRoleTemplate, DefinedRole, IAssignableDocument, HasCitation
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.moiraU", new(194, 125, 207), TeamRevealType.OnlyMe);

    private MoiraU() : base("moiraU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [NeedRevelationOption, RevelationCooldownOption, NumOfSwappingWinOption, WinConditionOption, NeedAliveOption, CanSeeVoteOption, CanSeeRoleOption, CanPushButtonOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Moira.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private BoolConfiguration NeedRevelationOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.needRevelation", true);
    static private FloatConfiguration RevelationCooldownOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.revelationCooldown", (2.5f, 45f, 2.5f), 15f, FloatConfigurationDecorator.Second, () => NeedRevelationOption);
    static private IntegerConfiguration NumOfSwappingWinOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.numOfSwappingWin", (1, 10, 1), 5);
    static public ValueConfiguration<int> WinConditionOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.winCondition", ["options.role.moiraU.winCondition.solo", "options.role.moiraU.winCondition.extra"], 0);
    static private BoolConfiguration NeedAliveOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.needAlive", true, () => WinConditionOption.GetValue() == 1);
    static internal BoolConfiguration CanSeeVoteOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.canSeeVote", false);
    static internal BoolConfiguration CanSeeRoleOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.canSeeRole", true);
    static private BoolConfiguration CanPushButtonOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.canPushButton", true);
    static private BoolConfiguration ChangeVoteOption = NebulaAPI.Configurations.Configuration("options.role.moiraU.changeVoteButton", true);


    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.SuperNewRoles; } }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Moira.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ExchangeButtonImage, "role.moiraU.ability.exchange");
        if (NeedRevelationOption) yield return new(RevelationButtonImage, "role.moiraU.ability.revelation");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%WIN%", Language.Translate(WinConditionOption.GetValue() == 0 ? "role.moiraU.ability.main.normalWin" : "role.moiraU.ability.main.extraWin"));
    }


    static private Virial.Media.Image RevelationButtonImage = NebulaAPI.AddonAsset.GetResource("MoiraRevelationButton.png")!.AsImage(115f)!;
    static private Virial.Media.Image ExchangeButtonImage = NebulaAPI.AddonAsset.GetResource("MoiraExchangeButton.png")!.AsImage(115f)!;


    static public MoiraU MyRole = new MoiraU();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        bool usedAbility = false;

        bool canWin = false;

        GamePlayer changeTarget1 = null!;
        GamePlayer changeTarget2 = null!;

        int left = NumOfSwappingWinOption;

        List<GamePlayer> revelationed = new List<GamePlayer>();

        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner && !canWin)
            {
                string prefix = Language.Translate("role.moiraU.leftWin");
                Helpers.TextHudContent("MoiraText", this, (tmPro) => tmPro.text = prefix + ": " + left, true);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner && NeedRevelationOption)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer, p => !revelationed.Contains(p));

                var revelationButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, playerTracker, VirtualKeyInput.Ability, "", RevelationCooldownOption, "revelation", RevelationButtonImage, (player, button) =>
                {
                    revelationed.Add(player);
                    button.StartCoolDown();
                });


            }
        }


        //[Local]
        //void PlayerDecorateName(PlayerDecorateNameEvent ev)
        //{
        //    ev.Color = ev.Player.Role.Role.Color;
        //}

        [Local]
        void RoleInfoVisibilityLocalEvent(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            if (CanSeeRoleOption) ev.CanSeeAll = true;
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!usedAbility)
            {
                var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
                buttonManager?.RegisterMeetingAction(new(ExchangeButtonImage,
                   p =>
                   {
                       {
                           if (changeTarget1 == null)
                           {
                               changeTarget1 = p.MyPlayer;
                               p.SetSelect(true);
                           }
                           if (changeTarget1 != null && changeTarget2 == null && p.MyPlayer != changeTarget1)
                           {
                               changeTarget2 = p.MyPlayer;
                               p.SetSelect(true);
                               usedAbility = true;

                               RpcSetTarget.Invoke((changeTarget1, changeTarget2, MyPlayer));

                           }
                       }
                   },
                   p => (!usedAbility || changeTarget1 == p.MyPlayer || changeTarget2 == p.MyPlayer) && !p.MyPlayer.IsDead && !MeetingHudExtension.ExileEvenIfTie && !MyPlayer.IsDead && !p.MyPlayer.AmOwner && !canWin && (NeedRevelationOption ? revelationed.Contains(p.MyPlayer) : true))
                   );
            }
        }

        static RemoteProcess<(GamePlayer target1, GamePlayer target2, GamePlayer myPlayer)> RpcSetTarget = new("moiraSetTarget", (message, _) =>
        {
            (message.myPlayer.Role as Instance)!.changeTarget1 = message.target1;
            (message.myPlayer.Role as Instance)!.changeTarget2 = message.target2;
        });


        [Local]
        void PlayerVoted(PlayerVotedLocalEvent ev)
        {
            if (changeTarget1 == null || changeTarget2 == null) return;
            revelationed.Remove(changeTarget1);
            revelationed.Remove(changeTarget2);
            if (usedAbility && !changeTarget1.IsDead && !changeTarget2.IsDead && usedAbility) RpcShowChangeAnim.Invoke(new(changeTarget1, changeTarget2));
            left--;
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            changeTarget1 = null!;
            changeTarget2 = null!;

            usedAbility = false;

            if (canWin && !MyPlayer.IsDead && WinConditionOption.GetValue() == 0)
            {
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(UchuGameEnd.MoiraWin, 1 << MyPlayer.PlayerId);
            }

        }

        
        void ChangeVote(PlayerFixVoteHostEvent ev)
        {
            if (!ChangeVoteOption) return;
            if (ev.VoteTo == changeTarget1)
            {
                ev.VoteTo = changeTarget2;
                return;
            }
            else if (ev.VoteTo == changeTarget2)
            {
                ev.VoteTo = changeTarget1;
                return;
            }
        }

        void BlockCallEmergencyMeeting(CheckCanPushEmergencyButtonEvent ev)
        {
            if (!CanPushButtonOption) ev.DenyButton("role.moiraU.denyReason");
        }


        [Local]
        void OnMeetingPreEnd(MeetingPreEndEvent ev)
        {
            ev.PushCoroutine(SwapPlayerCompletely(changeTarget1, changeTarget2));
        }

        [OnlyMyPlayer]
        void OnCheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if ((NeedAliveOption ? !MyPlayer.IsDead : true) && canWin && WinConditionOption.GetValue() == 1)
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(UchuGameEnd.MoiraExtra);

            }
        }




        static void ShowChangeAnim(GamePlayer changeTarget1, GamePlayer changeTarget2)
        {
            PlayerVoteArea playerVoteArea1 = null!, playerVoteArea2 = null!;
            foreach (PlayerVoteArea temp in MeetingHud.Instance.playerStates)
            {
                if (temp.TargetPlayerId == changeTarget1!.PlayerId) playerVoteArea1 = temp;
                else if (temp.TargetPlayerId == changeTarget2!.PlayerId) playerVoteArea2 = temp;
            }

            if (playerVoteArea1 == null || playerVoteArea2 == null) return;

            UnityEngine.Vector3 startPos1 = playerVoteArea1.transform.localPosition;
            UnityEngine.Vector3 startPos2 = playerVoteArea2.transform.localPosition;
            MeetingHud.Instance.StartCoroutine(PlaySwapAnimation(playerVoteArea1.transform, startPos1, startPos2, 1.5f, true));
            MeetingHud.Instance.StartCoroutine(PlaySwapAnimation(playerVoteArea2.transform, startPos2, startPos1, 1.5f, false));
            playerVoteArea1 = null!;
            playerVoteArea2 = null!;
        }

        static readonly RemoteProcess<(GamePlayer changeTarget1, GamePlayer changeTarget2)> RpcShowChangeAnim = new("moiraShowChangeAnim", (message, _) =>
        {
            ShowChangeAnim(message.changeTarget1, message.changeTarget2);
        });

        private static IEnumerator PlaySwapAnimation(Transform movingTransform, UnityEngine.Vector3 startPos, UnityEngine.Vector3 endPos, float duration, bool isUpper)
        {
            float elapsedTime = 0f;

            movingTransform.localPosition = new Vector3(startPos.x, startPos.y, isUpper ? -20 : -10);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                movingTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
                movingTransform.localPosition = new Vector3(movingTransform.localPosition.x, movingTransform.localPosition.y, isUpper ? -10 : movingTransform.localPosition.z);

                yield return null;
            }

            movingTransform.localPosition = endPos;
        }


        IEnumerator SwapPlayerCompletely(GamePlayer p1, GamePlayer p2)
        {
            if (changeTarget1 == null || changeTarget2 == null) yield break;
            if (!changeTarget1.VanillaPlayer || !changeTarget2.VanillaPlayer) yield break;

            var role1 = p1.Role.Role;
            var role2 = p2.Role.Role;

            int[] args1 = [];
            int[] args2 = [];

            yield return p1.Unbox().CoGetRoleArgument(a => args1 = a);
            yield return p2.Unbox().CoGetRoleArgument(a => args2 = a);

            int guess1 = p1.TryGetModifier<GuesserModifier.Instance>(out var g1)
                ? g1.LeftGuess : -1;
            int guess2 = p2.TryGetModifier<GuesserModifier.Instance>(out var g2)
                ? g2.LeftGuess : -1;

            bool jailer1 = p1.TryGetModifier<Nebula.Roles.Impostor.JailerModifier.Instance>(out _);
            bool jailer2 = p2.TryGetModifier<Nebula.Roles.Impostor.JailerModifier.Instance>(out _);

            bool nighty1 = p1.TryGetModifier<Nebula.Roles.Modifier.Nighty.Instance>(out _);
            bool nighty2 = p2.TryGetModifier<Nebula.Roles.Modifier.Nighty.Instance>(out _);

            int p1LeftTask = 0;
            int p1LeftQuota = 0;
            int p2LeftTask = 0;
            int p2LeftQuota = 0;

            if (p1.Tasks.IsCrewmateTask && p1.Tasks.HasExecutableTasks)
            {
                p1LeftTask = Mathf.Max(0, p1.Tasks.CurrentTasks - p1.Tasks.CurrentCompleted);
                p1LeftQuota = Mathf.Max(0, p1.Tasks.Quota - p1.Tasks.TotalCompleted);
            }

            if (p2.Tasks.IsCrewmateTask && p2.Tasks.HasExecutableTasks)
            {
                p2LeftTask = Mathf.Max(0, p2.Tasks.CurrentTasks - p2.Tasks.CurrentCompleted);
                p2LeftQuota = Mathf.Max(0, p2.Tasks.Quota - p2.Tasks.TotalCompleted);
            }

            using (RPCRouter.CreateSection("CompleteSwap"))
            {
                // ---------- タスク入れ替え ----------
                void ApplyTask(GamePlayer target, int leftTask, int leftQuota)
                {
                    if (leftTask > 0)
                    {
                        int common = GameOptionsManager.Instance.CurrentGameOptions
                            .GetInt(AmongUs.GameOptions.Int32OptionNames.NumCommonTasks);
                        int shortT = GameOptionsManager.Instance.CurrentGameOptions
                            .GetInt(AmongUs.GameOptions.Int32OptionNames.NumShortTasks);
                        int longT = GameOptionsManager.Instance.CurrentGameOptions
                            .GetInt(AmongUs.GameOptions.Int32OptionNames.NumLongTasks);

                        float total = common + shortT + longT;
                        int actualLong = (int)(System.Random.Shared.NextDouble() * (longT / total) * leftTask);
                        int actualCommon = (int)(System.Random.Shared.NextDouble() * (common / total) * leftTask);

                        target.Tasks.Unbox().ReplaceTasksAndRecompute(
                            leftTask - actualLong - actualCommon,
                            actualLong,
                            actualCommon
                        );

                        target.Tasks.Unbox().BecomeToCrewmate();
                        target.Tasks.Unbox().ReplaceTasks(leftTask, leftQuota - leftTask);
                    }
                    else
                    {
                        target.Tasks.Unbox().ReleaseAllTaskState();
                    }
                }

                ApplyTask(p1, p2LeftTask, p2LeftQuota);
                ApplyTask(p2, p1LeftTask, p1LeftQuota);

                // ---------- 役職入れ替え ----------
                p1.SetRole(role2, args2);
                p2.SetRole(role1, args1);

                PlayerExtension.SendRoleSwapping(p1, p2, role1, PlayerRoleSwapEvent.SwapType.Swap);
                PlayerExtension.SendRoleSwapping(p2, p1, role2, PlayerRoleSwapEvent.SwapType.Swap);

                // ---------- Modifier リセット ----------
                p1.RemoveModifier(GuesserModifier.MyRole);
                p2.RemoveModifier(GuesserModifier.MyRole);

                if (guess1 != -1) p2.AddModifier(GuesserModifier.MyRole, new[] { guess1 });
                if (guess2 != -1) p1.AddModifier(GuesserModifier.MyRole, new[] { guess2 });

                if (jailer1 != jailer2)
                {
                    if (jailer1)
                    {
                        p1.RemoveModifier(Nebula.Roles.Impostor.JailerModifier.MyRole);
                        p2.AddModifier(Nebula.Roles.Impostor.JailerModifier.MyRole, []);
                    }
                    else
                    {
                        p2.RemoveModifier(Nebula.Roles.Impostor.JailerModifier.MyRole);
                        p1.AddModifier(Nebula.Roles.Impostor.JailerModifier.MyRole, []);
                    }
                }

                if (nighty1)
                {
                    p1.RemoveModifier(Nebula.Roles.Modifier.Nighty.MyRole);
                    p2.AddModifier(Nebula.Roles.Modifier.Nighty.MyRole, []);
                }

                if (nighty2)
                {
                    p2.RemoveModifier(Nebula.Roles.Modifier.Nighty.MyRole);
                    p1.AddModifier(Nebula.Roles.Modifier.Nighty.MyRole, []);
                }

                new NebulaRPCInvoker(() =>
                {
                    p1.Unbox().UpdateTaskState();
                    p2.Unbox().UpdateTaskState();
                }).InvokeSingle();


                if (left == 0) canWin = true;
            }
        }
    }
}