using Hori.Scripts.Role.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Achievements;
using Virial.Runtime;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class PierrotU : DefinedSingleAbilityRoleTemplate<PierrotU.Ability>, DefinedRole, IAssignableDocument
{
    public PierrotU() : base("pierrotU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam)
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Pierrot.png")!.AsImage(115f);
    }

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public PierrotU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Pierrot.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ReportImage, "role.pierrotU.ability.report");
    }

    static private readonly Virial.Media.Image ReportImage = NebulaAPI.AddonAsset.GetResource("ReportButton.png")!.AsImage(115f)!;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "pierrotU.common1") //サーカス3回
        {
            RelatedRole = PierrotU.MyRole,
            Goal = 5,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.pierrotU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "pierrotU.common1")
        {
            Goal = 5
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "pierrotU.hard1") //幻惑ミスディレクション
        {
            RelatedRole = PierrotU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "pierrotU.another1") //サーカス50回
        {
            RelatedRole = PierrotU.MyRole,
            Goal = 50,
            Rank = TrophyLevel.Gold,
            IsSecret = true,
            RelatedRecord = "uchuaddon.pierrotU.another1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "pierrotU.another1")
        {
            Goal = 50
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "pierrotU.another2")
        {
            RelatedRole = PierrotU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "pierrotU.challenge1") //サーカスを3回開催した上で最後まで生存して勝利する
        {
            RelatedRole = PierrotU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        ModAbilityButton? pierrotButton = null;
        List<GamePlayer>? myDead = new List<GamePlayer>();

        int ValueChallenge1 = 0;
        bool ValueHard1 = false;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                pierrotButton = NebulaAPI.Modules.PlayerlikeKillButton(this, MyPlayer, new Virial.Events.Player.PlayerInteractParameter(RealPlayerOnly: true, IsKillInteraction: true), true, Virial.Compat.VirtualKeyInput.Kill, "pierrotU.kill",
                NebulaTeams.ImpostorTeam.KillCooldown, "kill", Virial.Components.ModAbilityButton.LabelType.Impostor,
                null, (target, _) =>
                {
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(target, UchuTranslatable.PierrotDead, EventDetail.Kill, Virial.Game.KillParameter.RemoteKill);
                    }

                    myDead.Add(target.RealPlayer);

                    int nearbyCount = 0;

                    foreach (var player in GamePlayer.AllPlayers)
                    {
                        if (player == target.RealPlayer) continue;
                        if (player.IsDead) continue;

                        if (VVector2.Distance(player.Position, target.RealPlayer.Position) <= 3f)
                        {
                            nearbyCount++;
                        }
                    }

                    if (nearbyCount >= 3)
                    {
                        ValueHard1 = true;
                    }
                    
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                }).SetAsUsurpableButton(this);
                pierrotButton.OnBroken = _ => Snatcher.RewindKillCooldown();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(pierrotButton.GetKillButtonLike());
            }
        }
        bool IPlayerAbility.HideKillButton => !(pierrotButton?.IsBroken ?? false);

        [Local]
        void PierrotReport(ReportDeadBodyEvent ev)
        {
            if (myDead == null) return;

            foreach (var p in myDead)
            {
                if (ev.Reported == p)
                {
                    ModSingleton<PierrotSystem>.Instance.PierrotActive();

                    ValueChallenge1++;

                    TitleUnLock("uchuaddon.pierrotU.common1");
                    TitleUnLock("uchuaddon.pierrotU.another1");

                    if (MyPlayer.IsDead)
                    {
                        TitleUnLock("uchuaddon.pierrotU.another2");
                    }
                }
            }

            myDead.Clear();
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            myDead?.Clear();
        }

        [Local]
        void OnMeetingEnd(TaskPhaseRestartEvent ev)
        {
            if (!MyPlayer.IsDead && ValueHard1)
            {
                TitleUnLock("uchuaddon.pierrotU.hard1");
            }

            ValueHard1 = false;
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.Winners.Test(MyPlayer) && !MyPlayer.IsDead && ValueChallenge1 >= 3)
            {
                TitleUnLock("uchuaddon.pierrotU.challenge1");
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}

// 基本的に処理をManagerに任せる。
[NebulaPreprocess(PreprocessPhase.PostRoles)]
[NebulaRPCHolder]
public class PierrotSystem : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static PierrotSystem() => DIManager.Instance.RegisterModule(() => new PierrotSystem());

    private PierrotSystem()
    {
        ModSingleton<PierrotSystem>.Instance = this;
        this.RegisterPermanently();
    }

    public void PierrotActive() //役職側でここを呼び出す。これで確実に同期
    {
        RpcPierrot.Invoke(true);
    }

    public bool IsPierrotMeeting = false;


    private readonly Dictionary<byte, Vector2> plateVelocity = [];

    private Vector2 skipVelocity;

    private bool initializedMeetingMove = false;
    private bool initializedSkipMove = false;

    private readonly Vector2 wallMin = new(-3.55f, -2.4f);　//壁
    private readonly Vector2 wallMax = new(3.55f, 2.4f);

    void PierrotUpdate(GameUpdateEvent ev) //移動の挙動めんどいからAIにも手伝ってもらう
    {
        if (!IsPierrotMeeting) return;
        if (!MeetingHud.Instance) return;

        // プレイヤー初期化
        if (!initializedMeetingMove)
        {
            initializedMeetingMove = true;

            foreach (var state in MeetingHud.Instance.playerStates)
            {
                if (!state || !state.gameObject.activeSelf) continue;

                float angle = System.Random.Shared.NextSingle() * 360f;
                float speed = 3.2f + System.Random.Shared.NextSingle() * 1.5f;

                foreach (var collider in state.GetComponentsInChildren<Collider2D>(true))
                {
                    //collider.enabled = false;
                    collider.isTrigger = true;
                }

                plateVelocity[state.PlayerId] = Vector2.right.Rotate(angle) * speed;
            }
        }

        // スキップ初期化
        if (!initializedSkipMove)
        {
            initializedSkipMove = true;

            float angle = System.Random.Shared.NextSingle() * 360f;
            float speed = 3.2f + System.Random.Shared.NextSingle();

            skipVelocity = Vector2.right.Rotate(angle) * speed;
        }

        // ネームプレート移動
        foreach (var state in MeetingHud.Instance.playerStates)
        {
            if (!state || !state.gameObject.activeSelf) continue;

            if (!plateVelocity.TryGetValue(state.PlayerId, out var velocity)) continue;

            var pos = state.transform.localPosition;

            Vector2 next = (Vector2)pos + velocity * Time.deltaTime;

            bool reflected = false;

            // 左
            if (next.x < wallMin.x)
            {
                next.x = wallMin.x;
                velocity.x = Mathf.Abs(velocity.x);
                reflected = true;
            }

            // 右
            if (next.x > wallMax.x)
            {
                next.x = wallMax.x;
                velocity.x = -Mathf.Abs(velocity.x);
                reflected = true;
            }

            // 下
            if (next.y < wallMin.y)
            {
                next.y = wallMin.y;
                velocity.y = Mathf.Abs(velocity.y);
                reflected = true;
            }

            // 上
            if (next.y > wallMax.y)
            {
                next.y = wallMax.y;
                velocity.y = -Mathf.Abs(velocity.y);
                reflected = true;
            }

            // 反射時に多少ブレさせる
            if (reflected)
            {
                float randomAngle = (System.Random.Shared.NextSingle() - 0.5f) * 35f;

                velocity = velocity.Rotate(randomAngle);
            }

            plateVelocity[state.PlayerId] = velocity;

            state.transform.localPosition = new Vector3(next.x, next.y, pos.z);
        }

        // スキップボタン移動
        var skip = MeetingHud.Instance.SkipVoteButton;

        if (skip && skip.gameObject.activeSelf)
        {
            var pos = skip.transform.localPosition;

            Vector2 next = (Vector2)pos + skipVelocity * Time.deltaTime;

            bool reflected = false;

            if (next.x < wallMin.x)
            {
                next.x = wallMin.x;
                skipVelocity.x = Mathf.Abs(skipVelocity.x);
                reflected = true;
            }

            if (next.x > wallMax.x)
            {
                next.x = wallMax.x;
                skipVelocity.x = -Mathf.Abs(skipVelocity.x);
                reflected = true;
            }

            if (next.y < wallMin.y)
            {
                next.y = wallMin.y;
                skipVelocity.y = Mathf.Abs(skipVelocity.y);
                reflected = true;
            }

            if (next.y > wallMax.y)
            {
                next.y = wallMax.y;
                skipVelocity.y = -Mathf.Abs(skipVelocity.y);
                reflected = true;
            }

            if (reflected)
            {
                skipVelocity = skipVelocity.Rotate((System.Random.Shared.NextSingle() - 0.5f) * 35f);
            }

            skip.transform.localPosition = new Vector3(next.x, next.y, pos.z);
        }
    }

    void PierrotMeetingStart(MeetingStartEvent ev)
    {
        if (!IsPierrotMeeting) return;

        NebulaManager.Instance.StartDelayAction(0.45f, () =>
        {
            NebulaAsset.PlaySE(NebulaAudioClip.FakeSabo);
        });
    }

    [EventPriority(EventPriority.VeryLow)]
    void PierrotCanUseAbility(MeetingStartEvent ev)
    {
        if (!IsPierrotMeeting) return;

        RpcCanUseAbility.Invoke();
    }

    void PierrotMeetingEnd(MeetingEndEvent ev)
    {
        RpcPierrot.Invoke(false);
    }

    RemoteProcess<bool> RpcPierrot = new("RpcPierrot_Uchu", (message, _) =>
    {
        var piero = ModSingleton<PierrotSystem>.Instance;

        if (message)
        {
            piero.IsPierrotMeeting = true;

            piero.initializedMeetingMove = false;
            piero.initializedSkipMove = false;

            piero.plateVelocity.Clear();
            piero.skipVelocity = Vector2.zero;
        }
        if (!message)
        {
            piero.IsPierrotMeeting = false;

            piero.initializedMeetingMove = false;
            piero.initializedSkipMove = false;

            piero.plateVelocity.Clear();
            piero.skipVelocity = Vector2.zero;
        }
    });

    RemoteProcess RpcCanUseAbility = new("RpcCanUseAbilityPierrot_Uchu", (_) =>
    {
        foreach (var player in GamePlayer.AllPlayers)
        {
            MeetingHudExtension.RemoveUsingAbilityMask(1 << player.PlayerId);
        }
    });
}