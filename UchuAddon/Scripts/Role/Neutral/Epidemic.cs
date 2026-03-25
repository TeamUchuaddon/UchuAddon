using Hori.Core;
using Il2CppInterop.Common.Host;
using Il2CppInterop.Runtime.Injection;
using Nebula.Behavior;
using Nebula.Game.Statistics;
using Nebula.Modules;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Hadar;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

internal class EpidemicU : DefinedRoleTemplate, DefinedRole,IAssignableDocument
{
    public static Team MyTeam = new Team("teams.epidemicU", new Virial.Color(68, 255, 0), TeamRevealType.OnlyMe);
    private EpidemicU() : base("epidemicU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [InfectionCooldown,InfectionDuration,InfectionMaxTime,InfectionRange,
        new GroupConfiguration("options.role.epidemicU.group.break",[BreakCooldown,BreakDuration,NumOfBreak,NumOfBreakPhase],GroupConfigurationColor.ToDarkenColor(MyTeam.Color.ToUnityColor())),
        new GroupConfiguration("options.role.epidemicU.group.boxTask",[NumOfBox,NumOfMaxBoxUse,CleanUpDuration,NumOfResetInfection,NumOfPenalty],GroupConfigurationColor.ToDarkenColor(MyTeam.Color.ToUnityColor())),VentConfiguration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    public static FloatConfiguration InfectionCooldown = NebulaAPI.Configurations.Configuration("options.role.epidemicU.infectionCooldown", (5f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration InfectionDuration = NebulaAPI.Configurations.Configuration("options.role.epidemicU.infectionDuration", (1f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration InfectionMaxTime = NebulaAPI.Configurations.Configuration("options.role.epidemicU.infectionMaxTime", (30f, 120f, 5f), 60f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration InfectionRange = NebulaAPI.Configurations.Configuration("options.role.epidemicU.infectionRange", (1f, 10f, 0.25f), 3.5f, FloatConfigurationDecorator.Ratio);

    public static FloatConfiguration BreakCooldown = NebulaAPI.Configurations.Configuration("options.role.epidemicU.breakCooldown", (5f, 60f, 5f), 30f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration BreakDuration = NebulaAPI.Configurations.Configuration("options.role.epidemicU.breakDuration", (1f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfBreak = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfBreak", (1, 12), 3);
    public static IntegerConfiguration NumOfBreakPhase = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfBreakPhase", (1, 12), 4);

    static private IntegerConfiguration NumOfBox = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfBox", (1, 3), 2);
    static private IntegerConfiguration NumOfMaxBoxUse = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfMaxBoxUse", (1, 12), 3);
    static private FloatConfiguration CleanUpDuration = NebulaAPI.Configurations.Configuration("options.role.epidemicU.cleanUpDuration", (1f, 15f, 1f), 5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration NumOfResetInfection = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfResetInfection", (5f, 100f, 5f), 30f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration NumOfPenalty = NebulaAPI.Configurations.Configuration("options.role.epidemicU.numOfPenalty", (1f, 5f, 0.25f), 1.25f, FloatConfigurationDecorator.Ratio);

    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.epidemicU.vent", true);

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static public EpidemicU MyRole = new EpidemicU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Epidemic.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private readonly Virial.Media.Image InfectionImage = NebulaAPI.AddonAsset.GetResource("EpidemicInfectButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image BreakImage = NebulaAPI.AddonAsset.GetResource("EpidemicBreakButton.png")!.AsImage(115f)!;
    public static MultiImage sprites = NebulaAPI.AddonAsset.GetResource("EpidemicBox.png")!.AsMultiImage(3, 1, 400f)!;
    public static MultiImage Document= NebulaAPI.AddonAsset.GetResource("EpidemicBox.png")!.AsMultiImage(3, 1, 100f)!;
    static private Image BoxIcon => Document.AsLoader(0); static private Image BoxBreakIcon => Document.AsLoader(1);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    bool IAssignableDocument.HasWinCondition => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(InfectionImage, "role.epidemicU.ability.infection");
        yield return new(BreakImage, "role.epidemicU.ability.break");
        yield return new(BoxIcon, "role.epidemicU.ability.box");
        yield return new(BoxBreakIcon, "role.epidemicU.ability.boxBreak");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%NUMBOX%", NumOfBox.GetValue().ToString());
        yield return new("%PHASE%", NumOfBreakPhase.GetValue().ToString());
    }

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;
        static private readonly Dictionary<GamePlayer, float> infection = new();
        public static HashSet<GamePlayer>? InfectionCompletes = new HashSet<GamePlayer>(); // 感染済みプレイヤー
        private HashSet<GamePlayer> IsInfection = new(); //感染進行中のプレイヤー
        public static List<GamePlayer>? FirstInfection = new List<GamePlayer>();
        float ratePerSecond = 100f / InfectionMaxTime;
        int leftInfection = 1;
        int leftBreak = NumOfBreak;
        bool Toggle = false;
        static private Dictionary<GamePlayer, int> useCounts = new();
        ModAbilityButton infectionButton = null!;
        float distance;
        float current;

        void InfectionReset()//感染度の初期化
        {

            infection.Clear();
            InfectionCompletes?.Clear();
            IsInfection.Clear();

            foreach (var player in GamePlayer.AllPlayers)
            {
                infection[player] = 0f;
            }
        }

        public float GetInfection(GamePlayer player) // 感染率を取得
        {
            float value = infection.TryGetValue(player, out var v) ? v : 0f;
            return Mathf.Floor(value);
        }

        [Local] //感染完了の判定
        void InfectionCompleteCheack(GameUpdateEvent ev)
        {
            foreach (var player in GamePlayer.AllPlayers)
            {
                if (player == MyPlayer) continue;
                if (InfectionCompletes!.Contains(player)) continue;

                float value = GetInfection(player);

                if (value >= 100f)
                {
                    InfectionCompletes.Add(player);
                }
            }
        }

        [Local]
        void InfectionManager(GameUpdateEvent ev) //感染増加の処理
        {
            if (MeetingHud.Instance) return;
            foreach (var target in GamePlayer.AllPlayers)
            {
                if (target == MyPlayer) continue;

                if (target.IsDead)
                {
                    IsInfection.Remove(target);
                    continue;
                }

                bool nearInfected = false;

                if (InfectionCompletes == null) continue;
                foreach (var source in InfectionCompletes)
                {
                    if (source.IsDead) continue;

                    distance = Vector2.Distance(source.Position, target.Position);

                    if (distance <= InfectionRange)
                    {
                        nearInfected = true;
                        break;
                    }
                }

                if (nearInfected)
                {
                    if (!IsInfection.Contains(target))
                        IsInfection.Add(target);
                }
                else
                {
                    IsInfection.Remove(target);
                }
            }

            foreach (var player in IsInfection.ToArray())
            {
                if (player == MyPlayer) continue;

                current = infection.TryGetValue(player, out var v) ? v : 0f;

                current += ratePerSecond * ev.DeltaTime;
                current = Mathf.Clamp(current, 0f, 100f);

                infection[player] = current;

                if (current >= 100f)
                {
                    InfectionCompletes?.Add(player);
                    IsInfection.Remove(player);
                }
            }
        }

        static public void ReduceInfectionFor(GamePlayer player) //除染効果
        {
            if (player == null) return;
            if (!infection.ContainsKey(player)) return;

            float current = infection[player];
            if (current >= 100f) return;

            int count = useCounts.TryGetValue(player, out var c) ? c : 0;
            float baseReduce = NumOfResetInfection;

            float multiplier = Mathf.Pow(NumOfPenalty, count);
            float reduce = baseReduce / multiplier;

            current -= reduce;
            current = Mathf.Clamp(current, 0f, 100f);
            infection[player] = current;

            useCounts[player] = count + 1;
            UchuDebug.Log($"[EpidemicU] Rpc - CleanUpCheack");
        }

        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {

        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                InfectionReset();
                leftInfection = 1;
                Toggle = false;

                var infectionTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !InfectionCompletes!.Contains(p.RealPlayer));
                infectionTracker.SetColor(MyRole.RoleColor);

                infectionButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                InfectionCooldown, InfectionDuration, "epidemicU.infection", InfectionImage,
                _ => infectionTracker.CurrentTarget != null);
                infectionButton.Availability = (button) => infectionTracker.CurrentTarget != null && leftInfection > 0 && MyPlayer.CanMove;
                infectionButton.ShowUsesIcon(1, leftInfection.ToString());

                infectionButton.OnEffectStart = _ => infectionTracker.KeepAsLongAsPossible = true;
                infectionButton.OnEffectEnd = (button) =>
                {
                    infectionTracker.KeepAsLongAsPossible = false;
                    if (infectionTracker.CurrentTarget == null) return;
                    if (MeetingHud.Instance) return;

                    leftInfection--;
                    infectionButton.UpdateUsesIcon(leftInfection.ToString());
                    if (!button.EffectTimer!.IsProgressing)
                    {
                        if (!(GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, infectionTracker.CurrentTarget, new(RealPlayerOnly: true))).IsCanceled ?? false))
                        {
                            var target = infectionTracker.CurrentTarget.RealPlayer;

                            infection[target] = 100f;
                            FirstInfection?.Add(target);
                        }
                    }
                    infectionButton.StartCoolDown();
                };
                infectionButton.OnUpdate = (button) =>
                {
                    if (!button.IsInEffect) return;
                    if (infectionTracker.CurrentTarget == null) button.InterruptEffect();
                };

                ObjectTracker<CleanUpBox> boxTracker = new ObjectTrackerUnityImpl<CleanUpBox, CleanUpBox>(MyPlayer.VanillaPlayer, 0.9f, () => ModSingleton<CleanUpBoxManager>.Instance.allBoxes,
                    box => !MyPlayer!.IsDead && ModSingleton<CleanUpBoxManager>.Instance.CanUseBox(GamePlayer.LocalPlayer!, box), box => !NebulaPhysicsHelpers.AnyShadowBetween(MyPlayer.TruePosition, box.Position, out _),
                    b => b, box => [box.MyConsole.transform.position], box => box.MyConsole.Renderer, MyRole.UnityColor, true).Register(this);


                var breakButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                    BreakCooldown, BreakDuration, "epidemicU.break", InfectionImage,
                    _ => boxTracker.CurrentTarget != null && leftBreak > 0);
                breakButton.Visibility = (button) => !MyPlayer.IsDead && leftBreak > 0;
                breakButton.ShowUsesIcon(1, leftBreak.ToString());

                breakButton.OnEffectStart = _ => boxTracker.KeepAsLongAsPossible = true;
                breakButton.OnEffectEnd = (button) =>
                {
                    boxTracker.KeepAsLongAsPossible = false;
                    if (boxTracker.CurrentTarget == null) return;
                    if (MeetingHud.Instance) return;
                    leftBreak--;
                    breakButton.UpdateUsesIcon(leftBreak.ToString());

                    if (!button.EffectTimer!.IsProgressing)
                    {
                        var target = boxTracker.CurrentTarget;
                        if (target == null) return;

                        ModSingleton<CleanUpBoxManager>.Instance.Break(target);
                    }
                    breakButton.StartCoolDown();
                };
                breakButton.OnUpdate = (button) =>
                {
                    if (!button.IsInEffect) return;
                    if (boxTracker.CurrentTarget == null) button.InterruptEffect();
                };
            }
        }

        void IGameOperator.OnReleased()
        {
            infection.Clear();
            InfectionCompletes?.Clear();
            IsInfection.Clear();
        }

        void InfectionDead(PlayerDieOrDisconnectEvent ev) //感染プレイヤーが死んだときに1回だけチェック、生存数が0なら回数+1
        {
            if (Toggle) return;
            if (!FirstInfection.Contains(ev.Player)) return;

            Toggle = true;

            if (InfectionCompletes.Count > 1) return;

            leftInfection = 1;
            infectionButton.UpdateUsesIcon(leftInfection.ToString());
        }

        [Local]
        void AppendExtraTaskText(PlayerTaskTextLocalEvent ev)
        {
            var lines = new List<string>();

            foreach (var player in GamePlayer.AllPlayers)
            {
                if (player == MyPlayer) continue;

                int percent = Mathf.RoundToInt(GetInfection(player));


                string statusTag;

                if (player.IsDead)
                {
                    statusTag = ($"{Language.Translate("state.epidemicU.dead")}");
                }
                else if (GetInfection(player) >= 100f)
                {
                    statusTag = ($"{Language.Translate("state.epidemicU.complete")}");
                }
                else
                {
                    statusTag = ($"{percent}%");
                }

                lines.Add($"{player.ColoredName} : {statusTag}");
            }

            ev.ReplaceBody(string.Join("\n", lines));
        }

        BoxMapLayer? mapLayer = null;
        [Local]
        void OnOpenNormalMap(MapOpenNormalEvent ev)
        {
            if (mapLayer is null)
            {
                mapLayer = UnityHelper.CreateObject<BoxMapLayer>("BoxsLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                this.BindGameObject(mapLayer.gameObject);
            }
            mapLayer.gameObject.SetActive(true);
        }

        [Local]
        void OnOpenAdminMap(MapOpenAdminEvent ev)
        {
            if (mapLayer) mapLayer?.gameObject.SetActive(false);
        }

        [Local]
        void CheckWin(GameUpdateEvent ev) //勝利条件チェック
        {
            if (MeetingHud.Instance) return;

            foreach (var player in GamePlayer.AllPlayers)
            {
                if (player == MyPlayer) continue;
                if (player.IsDead) continue;

                if (!InfectionCompletes!.Contains(player))
                    return;
            }

            NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.EpidemicWin, GameEndReason.Special, BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class CleanUpBox : NebulaSyncStandardObject
    {
        public CustomConsole MyConsole;
        public int BoxId { get; internal set; } = -1;
        public int numPhase { get; set; } = 0;

        private SpriteRenderer shadowRenderer; //影の位置を固定するためにBoxと少し切り離す
        private Vector3 shadowBaseWorldPos;

        public CleanUpBox(Vector2 pos) : base(pos, ZOption.Just, true, sprites.GetSprite(0)) 
        {
            ModSingleton<CleanUpBoxManager>.Instance.RegisterBox(this);
            MyRenderer.material = VanillaAsset.GetHighlightMaterial();

            shadowRenderer = UnityHelper.CreateObject<SpriteRenderer>("ConsoleBack",null,Vector3.zero);

            shadowRenderer.sprite = sprites.GetSprite(2);
            shadowRenderer.transform.position = MyRenderer.transform.position + new Vector3(0f, -0.31f, 0.0001f);

            shadowBaseWorldPos = shadowRenderer.transform.position;

            basePos = MyRenderer.transform.localPosition;
            floatTimer = UnityEngine.Random.value * 10f;

            MyConsole = MyRenderer.gameObject.AddComponent<CustomConsole>();
            MyConsole.Renderer = MyRenderer;
            MyConsole.Property = new()
            {
                CanUse = (console) =>
                {
                    var myPlayer = GamePlayer.LocalPlayer;
                    if (myPlayer == null) return false;
                    if (myPlayer.IsDead) return false;
                    if (!myPlayer.CanMove) return false;

                    var mgr = ModSingleton<CleanUpBoxManager>.Instance;
                    if (mgr == null) return false;
                    return mgr.CanUseBox(myPlayer, this);
                },
                Use = console =>
                {
                    var prefab = VanillaAsset.MapAsset[4].ShortTasks[20].MinigamePrefab;
                    ShowerMinigame minigame = GameObject.Instantiate<ShowerMinigame>(prefab.CastFast<ShowerMinigame>());
                    var cleanUpGame = minigame.gameObject.AddComponent<CleanUpMinigame>();
                    cleanUpGame.SetUp(minigame);
                    GameObject.Destroy(minigame);
                    cleanUpGame.transform.SetParent(Camera.main.transform, false);
                    cleanUpGame.transform.localPosition = new Vector3(0f, 0f, -50f);
                    cleanUpGame.Console = null;
                    cleanUpGame.Begin(null!);
                },
                OutlineColor = Color.white
            };
        }
        public const string MyTag = "CleanUpBox";
        static CleanUpBox() => NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new CleanUpBox(new(args[0], args[1])));

        private Vector3 basePos;
        private float floatTimer;

        public void Update(GameUpdateEvent ev)
        {
            bool isBroken = numPhase > 0;

            int spriteIndex = isBroken ? 1 : 0;
            Sprite = sprites.GetSprite(spriteIndex);

            if (isBroken)
            {
                MyRenderer.transform.localPosition = basePos;

                shadowRenderer.transform.localScale = new Vector3(0f, 0f, 0f);

                return;
            }

            floatTimer += Time.deltaTime;

            float amplitude = 0.08f;
            float speed = 2.0f;

            float offsetY = Mathf.Sin(floatTimer * speed) * amplitude;

            var pos = basePos;
            pos.y += offsetY;
            MyRenderer.transform.localPosition = pos;

            float t = Mathf.InverseLerp(-amplitude, amplitude, offsetY);
            float scale = Mathf.Lerp(1.0f, 0.7f, t);

            shadowRenderer.transform.localScale =
                new Vector3(scale, scale * 0.7f, 1f);
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    public class CleanUpBoxManager : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static CleanUpBoxManager() => DIManager.Instance.RegisterModule(() => new CleanUpBoxManager());
        private CleanUpBox box = null!;
        public List<CleanUpBox> allBoxes = new();
        public CleanUpBox Box => box;

        private CleanUpBoxManager()
        {
            ModSingleton<CleanUpBoxManager>.Instance = this;
            this.RegisterPermanently();
        }

        public void RegisterBox(CleanUpBox box)
        {
            box.BoxId = allBoxes.Count;
            allBoxes.Add(box);
            this.box = box;
        }

        public bool IsAvailable { get; private set; } = false;
        public Dictionary<byte, int> UsedCounts = new(); //使用回数の記録
        public static int MaxUseCount => NumOfMaxBoxUse;

        [EventPriority(EventPriority.High)]
        void OnGameStarted(GameStartEvent _)
        {
            IsAvailable = GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.FreePlay || MyRole.IsSpawnableInSomeForm();
            if (!IsAvailable) return;

            if (AmongUsClient.Instance.AmHost)
            {
                BoxSpawn();
            }
        }

        static public RemoteProcess<GamePlayer> RpcAddUseCountUchu = new("CleanUpUseAdd_Uchu",(player, _) =>
        {
            var mgr = ModSingleton<CleanUpBoxManager>.Instance;
            if (player == null) return;

            byte id = player.PlayerId;

            if (!mgr.UsedCounts.ContainsKey(id))
                mgr.UsedCounts[id] = 0;
            
            mgr.UsedCounts[id]++;
            UchuDebug.Log($"[EpidemicU] Rpc - AddUseTask");
        });

        static public RemoteProcess<GamePlayer> RpcCleanUpInfection = new("CleanUpReduceInfection_Uchu", (player, _) =>
        {
            if (player == null) return;

            UchuDebug.Log($"[EpidemicU] Rpc - CleanUpInfection");
            Role.Neutral.EpidemicU.Instance.ReduceInfectionFor(player);
        });

        public bool CanUseBox(GamePlayer player, CleanUpBox targetBox)
        {
            if (player == null) return false;
            if (targetBox.numPhase > 0) return false;
            byte id = player.PlayerId;

            int used = UsedCounts.TryGetValue(id, out var c) ? c : 0;

            return used < MaxUseCount;
        }

        public bool TryGetBox(int id, [MaybeNullWhen(false)] out CleanUpBox box)
        {
            if (id < allBoxes.Count)
                box = allBoxes[id];
            else
                box = null!;
            return box != null;
        }

        public void Break(CleanUpBox targetBox)
        {
            int num = NumOfBreakPhase;
            targetBox.numPhase = num;
        }

        public void BreakCheck(MeetingStartEvent ev)
        {
            foreach (var box in allBoxes)
            {
                if (box.numPhase > 0)
                    box.numPhase--;
            }
        }
    }

    static TextureReplacerUchu cleanUpImages = new(new ResourceTextureLoader("Nebula.Resources.Depoison.png"));

    public class CleanUpMinigame : Minigame
    {
        static CleanUpMinigame() => ClassInjector.RegisterTypeInIl2Cpp<CleanUpMinigame>();
        public CleanUpMinigame(System.IntPtr ptr) : base(ptr) { }
        public CleanUpMinigame() : base(ClassInjector.DerivedConstructorPointer<CleanUpMinigame>())
        { ClassInjector.DerivedConstructorBody(this); }

        public void SetUp(ShowerMinigame minigame)
        {
            this.Gauge = minigame.Gauge;
            this.PercentText = minigame.PercentText;
            this.MaxTime = CleanUpDuration;
        }

        public override void Begin(PlayerTask task)
        {
            this.BeginInternal(task);
            this.timer = this.MaxTime * (float)Progress / 100f;
            this.PercentText.text = ((int)(100 - Progress)).ToString() + "%";
            base.SetupInput(true, false);

            var headerText = transform.GetChild(2).GetChild(1).GetComponent<TextMeshPro>();
            GameObject.Destroy(headerText.GetComponent<TextTranslatorTMP>());
            headerText.text = Language.Translate("role.epidemicU.minigame.cleanUp");
            cleanUpImages.ReplaceSprite(transform.GetChild(2).GetComponent<SpriteRenderer>());
            cleanUpImages.ReplaceSprite(transform.GetChild(3).GetComponent<SpriteRenderer>());
            cleanUpImages.ReplaceSprite(transform.GetChild(3).GetChild(1).GetComponent<SpriteRenderer>());
        }

        public void Update()
        {
            if (this.amClosing != Minigame.CloseState.None) return;

            this.timer += Time.deltaTime;
            Progress = this.timer / this.MaxTime * 100f;
            this.Gauge.value = 1f - this.timer / this.MaxTime;
            this.PercentText.text = ((int)(100 - Progress)).ToString() + "%";
            if (Progress >= 100)
            {
                if (Constants.ShouldPlaySfx())
                {
                    var doneSfx = VanillaAsset.MapAsset[5].CommonTasks[3].MinigamePrefab.CastFast<MultistageMinigame>().Stages[1].CastFast<RoastMarshmallowFireMinigame>().sfxMarshmallowDone;
                    SoundManager.Instance.PlaySoundImmediate(doneSfx, false, 1.5f, 1f, null);
                }
                base.StartCoroutine(base.CoStartClose(0.5f));
                var mgr = ModSingleton<CleanUpBoxManager>.Instance;
                if (mgr != null && GamePlayer.LocalPlayer != null)
                {
                    UchuDebug.Log($"[EpidemicU] Task Complete");
                    CleanUpBoxManager.RpcAddUseCountUchu.Invoke(GamePlayer.LocalPlayer!);
                    CleanUpBoxManager.RpcCleanUpInfection.Invoke(GamePlayer.LocalPlayer!);
                }
            }
        }

        public float Progress = 0f;
        public VerticalGauge? Gauge;
        public TextMeshPro? PercentText;
        private float timer;
        public float MaxTime = 12f;

        public override void Close()
        {
            this.CloseInternal();
        }
    }

    static public void BoxSpawn()
    {
        var mgr = ModSingleton<CleanUpBoxManager>.Instance;

        foreach (var box in mgr.allBoxes)
        {
            if (box != null)
                NebulaSyncStandardObject.RpcDestroy(box.ObjectId);
        }
        mgr.allBoxes.Clear();

        switch (AmongUsUtil.CurrentMapId)
        {
            case 0:
                List<Vector2> spawnPointsSkeld = new()
                {
                    new Vector2(-0.14f, 4.78f),
                    new Vector2(2.84f, -15.44f),
                    new Vector2(-13.94f, -6.63f)
                };
                int spawnCounts = Mathf.Clamp(NumOfBox, 0, spawnPointsSkeld.Count);

                var selectedPoints1 = spawnPointsSkeld.OrderBy(_ => UnityEngine.Random.value).Take(spawnCounts);

                foreach (var pos in selectedPoints1)
                {
                    NebulaSyncObject.RpcInstantiate(CleanUpBox.MyTag, [
                        pos.x,
                        pos.y]);
                }

                break;

            case 1:
                List<Vector2> spawnPointsMira = new()
                {
                    new Vector2(14.28f, 20.13f),
                    new Vector2(7.49f, 12.96f),
                    new Vector2(26.08f, -0.002f)
                };
                int spawnCountm = Mathf.Clamp(NumOfBox, 0, spawnPointsMira.Count);

                var selectedPoints2 = spawnPointsMira.OrderBy(_ => UnityEngine.Random.value).Take(spawnCountm);

                foreach (var pos in selectedPoints2)
                {
                    NebulaSyncObject.RpcInstantiate(CleanUpBox.MyTag, [
                        pos.x,
                        pos.y]);
                }

                break;

            case 2:
                List<Vector2> spawnPointsPolus = new()
                {
                    new Vector2(7.54f, -12.41f),
                    new Vector2(15.62f, -24.43f),
                    new Vector2(21.90f, -7.46f)
                };
                int spawnCountp = Mathf.Clamp(NumOfBox, 0, spawnPointsPolus.Count);

                var selectedPoints3 = spawnPointsPolus.OrderBy(_ => UnityEngine.Random.value).Take(spawnCountp);

                foreach (var pos in selectedPoints3)
                {
                    NebulaSyncObject.RpcInstantiate(CleanUpBox.MyTag, [
                        pos.x,
                        pos.y]);
                }
                break;

            case 4:
                List<Vector2> spawnPointsAirShip = new()
                {
                    new Vector2(29.41f, -6.21f),
                    new Vector2(11.83f, 9.19f),
                    new Vector2(-12.31f, -9.52f)
                };
                int spawnCounta = Mathf.Clamp(NumOfBox, 0, spawnPointsAirShip.Count);

                var selectedPoints4 = spawnPointsAirShip.OrderBy(_ => UnityEngine.Random.value).Take(spawnCounta);

                foreach (var pos in selectedPoints4)
                {
                    NebulaSyncObject.RpcInstantiate(CleanUpBox.MyTag, [
                        pos.x,
                        pos.y]);
                }
                break;

            case 5:
                List<Vector2> spawnPointsFungle = new()
                {
                    new Vector2(-15.54f, -8.24f),
                    new Vector2(12.18f, -12.14f),
                    new Vector2(12.14f, 9.55f)
                };
                int spawnCountf = Mathf.Clamp(NumOfBox, 0, spawnPointsFungle.Count);

                var selectedPoints5 = spawnPointsFungle.OrderBy(_ => UnityEngine.Random.value).Take(spawnCountf);

                foreach (var pos in selectedPoints5)
                {
                    NebulaSyncObject.RpcInstantiate(CleanUpBox.MyTag, [
                        pos.x,
                        pos.y]);
                }
                break;

        }
    }

    public class BoxMapLayer : MonoBehaviour
    {
        List<(int id, SpriteRenderer renderer)> allBoxs = null!;

        static BoxMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<BoxMapLayer>();

        public void Awake()
        {
            var center = VanillaAsset.GetMapCenter(AmongUsUtil.CurrentMapId);
            var scale = VanillaAsset.GetMapScale(AmongUsUtil.CurrentMapId);
            allBoxs = [];

            foreach (var box in ModSingleton<CleanUpBoxManager>.Instance.allBoxes)
            {
                var renderer = UnityHelper.CreateObject<SpriteRenderer>("Box", transform, VanillaAsset.ConvertToMinimapPos(box.Position, center, scale));
                renderer.transform.localScale = new(0.7f, 0.7f, 1f);
                renderer.gameObject.AddComponent<MinimapScaler>();
                allBoxs.Add((box.BoxId, renderer));
            }

            Update();
        }

        public void Update()
        {
            foreach (var box in allBoxs)
            {
                if (ModSingleton<CleanUpBoxManager>.Instance.TryGetBox(box.id, out var d))
                {
                    box.renderer.sprite = sprites.GetSprite(d.numPhase > 0 ? 1 : 0);
                }
            }
        }
    }
}