using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Patches;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Rewired.Utils.Classes.Utility;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Virial;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Compat;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Hadar;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

file static class BoxerAsset
{
    static public Image MinigameBackground = NebulaAPI.AddonAsset.GetResource("Boxer/Background.png")!.AsImage(100f)!;
    public static MultiImage Sandbag = NebulaAPI.AddonAsset.GetResource("Boxer/Sandbag.png")!.AsMultiImage(4, 1, 100f)!;
    public static MultiImage Globe = NebulaAPI.AddonAsset.GetResource("Boxer/Globe.png")!.AsMultiImage(4, 1, 100f)!;
}

[NebulaPreprocess(PreprocessPhase.Roles)]
public class BoxerU : DefinedSingleAbilityRoleTemplate<BoxerU.Ability>, DefinedRole, IAssignableDocument
{
    public BoxerU() : base("boxerU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [BoxingCooldown, NumOfSandbag,
    new GroupConfiguration("options.role.boxerU.group.kill", [NumOfKillButton, KillButtonLost, ShowBlink, RangeKill, KillRange, FriendlyFire], GroupConfigurationColor.ImpostorRed)])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Boxer.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public FloatConfiguration BoxingCooldown = NebulaAPI.Configurations.Configuration("options.role.boxerU.boxingCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static public IntegerConfiguration NumOfSandbag = NebulaAPI.Configurations.Configuration("options.role.boxerU.numOfSandbag", (1, 7), 2);

    static public IntegerConfiguration NumOfKillButton = NebulaAPI.Configurations.Configuration("options.role.boxerU.numOfKillButton", (1, 8), 2);
    static public BoolConfiguration KillButtonLost = NebulaAPI.Configurations.Configuration("options.role.boxerU.killButtonLost", true);
    static public BoolConfiguration ShowBlink = NebulaAPI.Configurations.Configuration("options.role.boxerU.showBlink", false);
    static public BoolConfiguration RangeKill = NebulaAPI.Configurations.Configuration("options.role.boxerU.rangeKill", false);
    static public FloatConfiguration KillRange = NebulaAPI.Configurations.Configuration("options.role.boxerU.killRange", (0.25f, 2f, 0.25f), 1f, FloatConfigurationDecorator.Ratio, ()=> RangeKill);
    static public BoolConfiguration FriendlyFire = NebulaAPI.Configurations.Configuration("options.role.boxerU.friendlyFire", false, () => RangeKill);

    static public BoxerU MyRole = new();

    static private readonly GameStatsEntry StatsTask = NebulaAPI.CreateStatsEntry("stats.boxerU.task", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsKill = NebulaAPI.CreateStatsEntry("stats.boxerU.kill", GameStatsCategory.Roles, MyRole);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Boxer.png")!.AsImage(100f)!;

    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(BoxingImage, "role.boxerU.ability.boxing");
        yield return new(ModAbilityButtonImpl.VanillaKillImage, "role.boxerU.ability.kill");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%KILL%", Language.Translate(RangeKill ? "role.boxerU.ability.main.rangeKill" : "role.boxerU.ability.main.noRangeKill"));
        yield return new("%BLINK%", Language.Translate(ShowBlink ? "role.boxerU.ability.main.showBlink" : "role.boxerU.ability.main.noShowBlink"));
    }

    static private readonly Virial.Media.Image BoxingImage = NebulaAPI.AddonAsset.GetResource("Boxer/BoxerButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image BreakSandbag = NebulaAPI.AddonAsset.GetResource("Boxer/BreakSandbag.png")!.AsImage(610f)!;

    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class BoxerSandbag : NebulaSyncStandardObject, IGameOperator
    {
        public static string MyTag = "BoxerSandbag";
        private int stage = 0;

        public BoxerSandbag(Vector2 pos) : base(pos, ZOption.Just, true, BreakSandbag.GetSprite())
        {
        }

        static BoxerSandbag()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new BoxerSandbag(new Vector2(args[0], args[1])));
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            stage++;
            if (stage >= NumOfSandbag) NebulaSyncObject.RpcDestroy(this.ObjectId);
        }
    }

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "boxerU.common1")
        {
            RelatedRole = BoxerU.MyRole,
            Goal = 5,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.boxerU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "boxerU.common1")
        {
            Goal = 5
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "boxerU.another1")
        {
            RelatedRole = BoxerU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "boxerU.hard1")
        {
            RelatedRole = BoxerU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "boxerU.challenge1")
        {
            RelatedRole = BoxerU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool IPlayerAbility.HideKillButton => true;
        ModAbilityButton boxingButton = null!;

        Queue<(ModAbilityButton button, FlexibleLifespan lifespan)> killButtons = [];

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var action = CustomConsoleProperty.MinigameAction<BoxerMinigame>(null!, (minigame, console) =>{  });

                boxingButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    BoxingCooldown, "boxing", BoxingImage, _ => killButtons.Count <= 0).SetAsUsurpableButton(this);
                boxingButton.OnClick = (button) =>
                {
                    action(null!);
                };
                boxingButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
            }
        }

        [OnlyMyPlayer, Local]
        void OnChangeChampion(BoxingEvent ev)
        {
            for (int i = 0; i < NumOfKillButton; i++)
            {
                GenerateKillButton();
            }

            var localPos = MyPlayer.Position;
            NebulaSyncObject.RpcInstantiate(BoxerSandbag.MyTag, [localPos.x,localPos.y + 0.35f]);
            StatsTask.Progress();
            AchievementUnLock.TitleUnLock("uchuaddon.boxerU.common1");
        }

        ModAbilityButton GenerateKillButton()
        {
            FlexibleLifespan lifespan = new(this);

            var killButton = NebulaAPI.Modules.PlayerlikeKillButton(lifespan, MyPlayer, false,  Virial.Compat.VirtualKeyInput.None, null,
            0f, "kill", Virial.Components.ModAbilityButton.LabelType.Impostor, null,
            (p, button) =>
            {
                MyPlayer.MurderPlayer(p, PlayerState.Dead, null, ShowBlink ? KillParameter.NormalKill : KillParameter.RemoteKill);

                if (RangeKill && MyPlayer.IsImpostor)
                {
                    var targets = GamePlayer.AllPlayers.Where(player =>
                    !player.IsDead && player != MyPlayer && player.Position.Distance(MyPlayer.Position) <= KillRange && (!player.IsImpostor || FriendlyFire)).ToArray();

                    if (targets.Length >= 6) AchievementUnLock.TitleUnLock("uchuaddon.boxerU.challenge1");

                    hard++;
                    if(hard >= 3) AchievementUnLock.TitleUnLock("uchuaddon.boxerU.hard1");
                    
                    foreach (var target in targets)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, null, KillParameter.RemoteKill);
                    }
                }

                lifespan.Release();
                UpdateKillButtons();

                if (killButtons.Count <= 0) boxingButton.StartCoolDown();
                StatsKill.Progress();
            }, null).SetAsUsurpableButton(this);

            killButtons.Enqueue((killButton, lifespan));
            UpdateKillButtons();

            return killButton;
        }

        void UpdateKillButtons()
        {
            while (killButtons.Count > 0)
            {
                var entry = killButtons.Peek();

                if (entry.lifespan.IsDeadObject)
                {
                    killButtons.Dequeue();
                    continue;
                }

                if (!entry.button.IsKillButtonContent)
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        entry.button.IsKillButtonContent = true;
                        entry.button.ShouldBeInLastLine = false;
                    });

                    entry.button.BindKey(Virial.Compat.VirtualKeyInput.Kill);
                }

                break;
            }
        }

        void ReleaseAllKillButtons()
        {
            while (killButtons.Count > 0)
            {
                var entry = killButtons.Dequeue();

                if (!entry.lifespan.IsDeadObject) entry.lifespan.Release();
            }

            UpdateKillButtons();
        }

        int hard = 0;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (KillButtonLost)
            {
                ReleaseAllKillButtons();
                hard = 0;
            }  
        }

        [Local]
        void OnTaskPhaseStart(TaskPhaseRestartEvent ev)
        {
            foreach (var entry in killButtons)
            {
                if (!entry.lifespan.IsDeadObject)
                {
                    entry.button.CoolDownTimer?.Start(10f);
                }
            }
        }

        void IGameOperator.OnReleased()
        {
            AchievementUnLock.TitleUnLock("uchuaddon.boxerU.another1");
        }
    }


    static internal RemoteProcess<GamePlayer> RpcBoxingEvent = new("RpcBoxingEvent_Uchu",
    (message, _) =>
    {
        if (message != null) GameOperatorManager.Instance?.Run<BoxingEvent>(new BoxingEvent(message));
    });

    public class BoxingEvent : AbstractPlayerEvent
    {
        internal BoxingEvent(GamePlayer player) : base(player) { }
    }

    public class BoxerMinigame : Minigame
    {
        static BoxerMinigame() => ClassInjector.RegisterTypeInIl2Cpp<BoxerMinigame>();
        public BoxerMinigame(System.IntPtr ptr) : base(ptr) { }
        public BoxerMinigame() : base(ClassInjector.DerivedConstructorPointer<BoxerMinigame>())
        { ClassInjector.DerivedConstructorBody(this); }

        private SpriteRenderer? Background;
        private SpriteRenderer? Sandbag;
        private SpriteRenderer? GlobeRight;
        private SpriteRenderer? GlobeLeft;

        private int hitCount = 0;
        private const int MaxHits = 28;
        private int lastSpriteIndex = -1;
        private bool isRightPunch = true;
        private bool CompleteToggle = false;

        public override void Begin(PlayerTask task)
        {
            this.BeginInternal(task);

            MetaScreen.InstantiateCloseButton(transform, new Vector3(-3.4f, 2f, -0.5f)).OnClick.AddListener(Close);

            hitCount = 0;
            CompleteToggle = false;

            void BeginBackground()
            {
                Background = UnityHelper.CreateObject<SpriteRenderer>("Background", transform, new Vector3(0f, 0f, 2f));

                Background.sprite = BoxerAsset.MinigameBackground.GetSprite();
                Background.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }

            void BeginSandbag()
            {
                Sandbag = UnityHelper.CreateObject<SpriteRenderer>("Sandbag", transform, new Vector3(0f, 0f, -0.1f));

                Sandbag.sprite = BoxerAsset.Sandbag.GetSprite(0);
                Sandbag.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                var collider = Sandbag.gameObject.AddComponent<PolygonCollider2D>();
                collider.isTrigger = true;

                var button = Sandbag.gameObject.SetUpButton(false);

                button.OnClick.AddListener(() =>                {

                    if (amClosing == CloseState.Closing) return;

                    if (GlobeRight == null || GlobeLeft == null) return;
                    if (CompleteToggle) return;

                    hitCount++;

                    if (isRightPunch) //右パンチ
                    {
                        GlobeRight.sprite = BoxerAsset.Globe.GetSprite(3);
                        GlobeRight.transform.localPosition = new Vector3(1.2f, -1.1f, -0.2f);
                        GlobeRight.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

                        GlobeLeft.sprite = BoxerAsset.Globe.GetSprite(0);
                        GlobeLeft.transform.localPosition = new Vector3(-1.75f, -1.7f, -0.2f);
                        GlobeLeft.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                    }
                    else // 左パンチ
                    {
                        GlobeRight.sprite = BoxerAsset.Globe.GetSprite(2);
                        GlobeRight.transform.localPosition = new Vector3(1.75f, -1.7f, -0.2f);
                        GlobeRight.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                        GlobeLeft.sprite = BoxerAsset.Globe.GetSprite(1);
                        GlobeLeft.transform.localPosition = new Vector3(-1.2f, -1.1f, -0.2f);
                        GlobeLeft.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                    }

                    isRightPunch = !isRightPunch;

                    int newIndex;
                    do
                    {
                        newIndex = System.Random.Shared.Next(1, 4); 
                    }
                    while (newIndex == lastSpriteIndex);

                    lastSpriteIndex = newIndex;
                    Sandbag.sprite = BoxerAsset.Sandbag.GetSprite(newIndex);

                    StartCoroutine(ManagedEffects.Lerp(0.1f, p =>
                    {
                        float offset = (1f - p) * 0.1f;
                        Sandbag.transform.localPosition = new Vector3(offset, 0f, -0.1f);
                    }).WrapToIl2Cpp());

                    if (hitCount >= MaxHits)
                    {
                        CompleteToggle = true;

                        RpcBoxingEvent.Invoke(GamePlayer.LocalPlayer!);

                        SoundManager.instance.PlaySound(APICompat.GetSound("TrainingTaskComplete"), false, 3f);
                        StartCoroutine(ManagedEffects.Sequence(ManagedEffects.Wait(0.3f),ManagedEffects.Action(() => Close())).WrapToIl2Cpp());
                    }

                    if (CompleteToggle) return;
                    SoundManager.instance.PlaySound(APICompat.GetSound("FungleDoorPunch"), false, 1f);
                });
            }

            void BeginGlobeRight()
            {
                GlobeRight = UnityHelper.CreateObject<SpriteRenderer>("GlobeRight", transform, new Vector3(1.75f, -1.7f, -0.2f));

                GlobeRight.sprite = BoxerAsset.Globe.GetSprite(2);
                GlobeRight.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }

            void BeginGlobeLeft()
            {
                GlobeLeft = UnityHelper.CreateObject<SpriteRenderer>("GlobeLeft", transform, new Vector3(-1.75f, -1.7f, -0.2f));

                GlobeLeft.sprite = BoxerAsset.Globe.GetSprite(0);
                GlobeLeft.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }

            BeginBackground();
            BeginSandbag();
            BeginGlobeRight();
            BeginGlobeLeft();
        }

        public override void Close()
        {
            this.CloseInternal();
        }
    }
}