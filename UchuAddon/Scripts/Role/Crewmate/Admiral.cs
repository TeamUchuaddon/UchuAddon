using Epic.OnlineServices.Sessions;
using Hori.Core;
using Hori.Scripts.Role.Complex;
using Il2CppInterop.Runtime.Injection;
using Nebula.Map;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Virial.Events.Game.Minimap;
using Virial.Media;
using static Il2CppSystem.Globalization.CultureInfo;
using static Nebula.Roles.Impostor.Thurifer;
using static UnityEngine.EventSystems.PointerEventData;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class AdmiralU : DefinedSingleAbilityRoleTemplate<AdmiralU.Ability>, DefinedRole, IAssignableDocument
{
    public AdmiralU() : base("admiralU", new(61, 88, 179), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfCall, VoteWatching])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Admiral.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate; //マッド化
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private IntegerConfiguration NumOfCall = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfCall", (1, 15), 2);
    static public BoolConfiguration VoteWatching = NebulaAPI.Configurations.Configuration("options.role.admiralU.voteWatching", true);
    // static public BoolConfiguration AdmiralSystemStop = NebulaAPI.Configurations.Configuration("options.role.admiralU.admiralSystemStop", false);

    static private readonly Image CallImage = NebulaAPI.AddonAsset.GetResource("AdmiralCallButton.png")!.AsImage(115f)!;
    static private readonly Image CloseImage = NebulaAPI.AddonAsset.GetResource("AdmiralCloseButton.png")!.AsImage(115f)!;
    static private readonly Image ResponseImage = NebulaAPI.AddonAsset.GetResource("AdmiralResponseButton.png")!.AsImage(115f)!;
    static private readonly Image MapImage = NebulaAPI.AddonAsset.GetResource("AdmiralMapButton.png")!.AsImage(115f)!;

    static private readonly MultiImage PlayerPinImage = DividedSpriteLoader.FromResource("Nebula.Resources.MapPin.png", 130f, 2, 1);

    static public AdmiralU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Admiral.png")!.AsImage(115f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(CallImage, "role.admiralU.ability.call");
        yield return new(ResponseImage, "role.admiralU.ability.response");
        yield return new(CloseImage, "role.admiralU.ability.close");
    }

    public class AdmiralMapLayer : MonoBehaviour
    {
        static AdmiralMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<AdmiralMapLayer>();

        public void SetResponses(List<ResponseData> responses)
        {
            DestroyPins();

            foreach (var response in responses)
            {
                var pin = UnityHelper.CreateObject<SpriteRenderer>($"Pin_{response.Player.PlayerId}", transform, Vector3.zero);

                var pinPlayer = Helpers.GetPlayer(response.Player.PlayerId);

                if (pinPlayer == null) return; // nullチェック

                pin.sprite = PlayerPinImage.GetSprite(1);
                pin.material = pinPlayer.cosmetics.currentBodySprite.BodySprite.material;
                pin.transform.localPosition = new Vector3(response.MinimapPos.x, response.MinimapPos.y, -25f);

                var menu = pin.gameObject.SetUpButton(true, pin);
                menu.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(menu, Language.Translate("role.admiralU.title") + "<br>" + "<b>" + response.Player.ColoredName + "</b>"));
                menu.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(menu));
                menu.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;

                pin.gameObject.SetActive(true);
            }
        }

        public void DestroyPins()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
    }


    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        bool MyCall = false; // 自分自身が、点呼してるか。ボタンの表示条件で使う
        int left = NumOfCall; // 回数
        bool UsedMeeting = false; // 会議で1回以上使ったか。

        bool OnMap = false; 

        private AdmiralMapLayer mapLayer = null!;
        private List<ResponseData> ResponsesResult = new(); // 応答結果

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                Nebula.Utilities.Helpers.TextHudContent("AdmiralUIText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.admiralU.hudText") + ": " + left, true);

                // 点呼
                var callButton = NebulaAPI.Modules.AbilityButton(this, alwaysShow: true).BindKey(VirtualKeyInput.Ability).SetLabel("admiralU.call").SetImage(CallImage).SetAsUsurpableButton(this);
                callButton.Visibility = _ => !MyCall && left > 0 && !UsedMeeting && !MyPlayer.IsDead && !AmongUsUtil.MapIsOpen && !ExileController.Instance && MeetingHud.Instance;
                callButton.Availability = _ => !ModSingleton<AdmiralGameManager>.Instance.IsCall;
                callButton.OnClick = (button) =>
                {
                    MyCall = true;
                    left--;

                    ModSingleton<AdmiralGameManager>.Instance.OnCall(MyPlayer);
                    ModSingleton<AdmiralGameManager>.Instance.InjectAbility(this);
                };

                // 点呼の応答を締め切る
                var closeButton = NebulaAPI.Modules.AbilityButton(this, alwaysShow: true).BindKey(VirtualKeyInput.Ability).SetLabel("admiralU.close").SetImage(CloseImage).SetAsUsurpableButton(this);
                closeButton.Visibility = _ => MyCall && ModSingleton<AdmiralGameManager>.Instance.IsCallOwner == MyPlayer && !MyPlayer.IsDead && !AmongUsUtil.MapIsOpen && !ExileController.Instance && MeetingHud.Instance;
                closeButton.Availability = _ => ModSingleton<AdmiralGameManager>.Instance.IsCall;
                closeButton.OnClick = (button) =>
                {
                    MyCall = false;
                    UsedMeeting = true; // 1会議1回
                    ModSingleton<AdmiralGameManager>.Instance.OnSendResult();
                };

                var mapButton = NebulaAPI.Modules.AbilityButton(this, alwaysShow: true).BindKey(VirtualKeyInput.SecondaryAbility).SetLabel("admiralU.map").SetImage(MapImage).SetAsUsurpableButton(this);
                mapButton.Visibility = _ => UsedMeeting && !MyPlayer.IsDead && !AmongUsUtil.MapIsOpen && !ExileController.Instance && MeetingHud.Instance;
                mapButton.Availability = _ => UsedMeeting;
                mapButton.OnClick = (button) =>
                {
                    OnMap = true;
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        HudManager.Instance.InitMap();
                        MapBehaviour.Instance.ShowNormalMap();
                        MapBehaviour.Instance.taskOverlay.gameObject.SetActive(false);
                    });
                };
            }
        }

        [Local]
        void OnOpenMap(AbstractMapOpenEvent ev)
        {
            if (mapLayer == null)
            {
                mapLayer = UnityHelper.CreateObject<AdmiralMapLayer>("AdmiralLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                this.BindGameObject(mapLayer.gameObject);
            }

            mapLayer.SetResponses(ResponsesResult);

            if (OnMap && ev is MapOpenNormalEvent && !IsUsurped && !MyPlayer.IsDead)
            {
                mapLayer.gameObject.SetActive(true);
            }
            else
            {
                mapLayer.gameObject.SetActive(false);
            }
        }

        [Local]
        void OnMapClose(MapCloseEvent ev)
        {
            OnMap = false;
        }

        [Local]
        void OnPhaseRestart(TaskPhaseStartEvent ev)
        {
            if (mapLayer != null) mapLayer.DestroyPins();
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            MyCall = false;
            UsedMeeting = false;
            OnMap = false;
            ResponsesResult.Clear();

            if (mapLayer != null) mapLayer.DestroyPins();       
        }

        public void OnCallResult(List<ResponseData> responses)
        {
            ResponsesResult = new List<ResponseData>(responses);
            UchuDebug.Log("[Admiral] Response");
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    public class AdmiralGameManager : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static AdmiralGameManager() => DIManager.Instance.RegisterModule(() => new AdmiralGameManager());

        private AdmiralGameManager()
        {
            ModSingleton<AdmiralGameManager>.Instance = this;
            this.RegisterPermanently();
        }


        private Ability? myAbility;
        public void InjectAbility(Ability ability) => myAbility = ability;

        public bool IsCall { get; private set; } = false; // 現在点呼中か
        private List<GamePlayer> NoResponse = new(); // 点呼に応じてないプレイヤーをまとめる。
        public GamePlayer? IsCallOwner; // 点呼実行中のプレイヤー

        private List<ResponseData> ResponsesResult = new(); // 応答結果

        [EventPriority(Priority.Low)]
        void OnGameStarted(GameStartEvent _)
        {
            //if (AdmiralSystemStop) return;

            var localPlayer = GamePlayer.LocalPlayer;

            ModAbilityButton responseButton = null!;
            ResponseMapLayer mapLayer = null!;
            bool OnMapLayer = false;

            responseButton = NebulaAPI.Modules.AbilityButton(NebulaAPI.CurrentGame!, alwaysShow: true).BindKey(VirtualKeyInput.None).SetLabel("admiralU.response").SetImage(ResponseImage);
            responseButton.Visibility = _ => IsCallOwner != null && !IsCallOwner.AmOwner && IsCall && NoResponse.Contains(localPlayer!) && !AmongUsUtil.MapIsOpen && !ExileController.Instance && MeetingHud.Instance;
            responseButton.OnClick = (button) =>
            {
                OnMapLayer = true;

                HudManager.Instance.InitMap();
                MapBehaviour.Instance.ShowNormalMap();
                MapBehaviour.Instance.taskOverlay.gameObject.SetActive(false);

                mapLayer.OnClickedMinimapPos = (minimapPos) =>
                {
                    MapBehaviour.Instance.Close();
                    RpcOnResponse.Invoke((localPlayer!, minimapPos));
                };
            };

            GameOperatorManager.Instance?.Subscribe<AbstractMapOpenEvent>(ev =>
            {
                if (mapLayer == null)
                {
                    mapLayer = UnityHelper.CreateObject<ResponseMapLayer>("ResponseLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                    NebulaAPI.CurrentGame!.BindGameObject(mapLayer.gameObject);
                    mapLayer.gameObject.SetActive(false);
                }

                if (OnMapLayer)
                {
                    mapLayer.gameObject.SetActive(true);
                }
            }, NebulaAPI.CurrentGame!);
            
            GameOperatorManager.Instance?.Subscribe<MapCloseEvent>(ev =>
            {
                OnMapLayer = false;
                mapLayer.gameObject.SetActive(false);
            }, NebulaAPI.CurrentGame!);
        }

        internal void OnCall(GamePlayer owner)
        {
            RpcOnCall.Invoke(owner);
        }

        RemoteProcess<GamePlayer> RpcOnCall = new("RpcOnCall_Uchu", (message, _) =>
        {
            ModSingleton<AdmiralGameManager>.Instance.IsCall = true;
            ModSingleton<AdmiralGameManager>.Instance.IsCallOwner = message;
            ModSingleton<AdmiralGameManager>.Instance.NoResponse = GamePlayer.AllPlayers.Where(p => !p.IsDead).ToList();
            ModSingleton<AdmiralGameManager>.Instance.ResponsesResult.Clear();
            AmongUsUtil.PlayCustomFlash(MyRole.Color, 0f, 0.25f, 0.45f);
        });

        // レスポンス状況を更新
        RemoteProcess<(GamePlayer player, Vector2 miniMapPos)> RpcOnResponse = new("RpcOnResponse_Uchu", (message, _) =>
        {
            ModSingleton<AdmiralGameManager>.Instance.NoResponse.Remove(message.player);
            ModSingleton<AdmiralGameManager>.Instance.ResponsesResult.Add(new ResponseData(message.player, message.miniMapPos));
        });

        internal void OnSendResult()
        {
            RpcSendResult.Invoke();
        }

        // 送信とリセット
        RemoteProcess RpcSendResult = new("RpcSendResult_Uchu", (_) =>
        {       
            if (ModSingleton<AdmiralGameManager>.Instance.IsCallOwner == GamePlayer.LocalPlayer)
            {
                ModSingleton<AdmiralGameManager>.Instance.myAbility?.OnCallResult(new List<ResponseData>(ModSingleton<AdmiralGameManager>.Instance.ResponsesResult));
            }      

            //ModSingleton<AdmiralGameManager>.Instance.myAbility?.OnCallResult(ModSingleton<AdmiralGameManager>.Instance.ResponsesResult);
            ModSingleton<AdmiralGameManager>.Instance.IsCall = false;
            ModSingleton<AdmiralGameManager>.Instance.IsCallOwner = null;
            ModSingleton<AdmiralGameManager>.Instance.NoResponse.Clear();
            ModSingleton<AdmiralGameManager>.Instance.ResponsesResult.Clear();
            ModSingleton<AdmiralGameManager>.Instance.myAbility = null;
        });

        RemoteProcess RpcReset = new("RpcResetAdmiral_Uchu", (_) =>
        {
            ModSingleton<AdmiralGameManager>.Instance.IsCall = false;
            ModSingleton<AdmiralGameManager>.Instance.IsCallOwner = null;
            ModSingleton<AdmiralGameManager>.Instance.NoResponse.Clear();
            ModSingleton<AdmiralGameManager>.Instance.ResponsesResult.Clear();
            ModSingleton<AdmiralGameManager>.Instance.myAbility = null;
        });

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            RpcReset.Invoke();
        }

        void OnGameStart(GameStartEvent ev)
        {
            RpcReset.Invoke();
        }

        void OnGameEnd(GameEndEvent ev)
        {
            RpcReset.Invoke();
        }
    }

    internal class ResponseMapLayer : UchuFakePlayerMapLayer
    {
        static ResponseMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<ResponseMapLayer>();
        public Action<Vector2>? OnClickedMinimapPos;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnClick(Vector2 worldPos, Vector2 minimapPos)
        {
            OnClickedMinimapPos?.Invoke(minimapPos);
            MapBehaviour.Instance.Close();
        }
    }

    public readonly struct ResponseData
    {
        public readonly GamePlayer Player;
        public readonly Vector2 MinimapPos;

        public ResponseData(GamePlayer player, Vector2 minimapPos)
        {
            Player = player;
            MinimapPos = minimapPos;
        }
    }
}

/*
public class AdmiralU : DefinedSingleAbilityRoleTemplate<AdmiralU.Ability>, DefinedRole, IAssignableDocument
{
    public AdmiralU() : base("admiralU", new(61, 88, 179), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfAdmiral,PublickName, NumOfPublickTask, VoteWatching, NotGuess, PrivateNameGuess])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Admiral.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private IntegerConfiguration NumOfAdmiral = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfAdmiral", (1, 15), 2);
    static private BoolConfiguration PublickName = NebulaAPI.Configurations.Configuration("options.role.admiralU.publicName", true);
    static private IntegerConfiguration NumOfPublickTask = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfPublicTask", (1, 12), 8,() => PublickName);
    //static private FloatConfiguration NumOfPublickTask = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfPublicTask", (10f, 100f, 10f), 80f, FloatConfigurationDecorator.Percentage, () => PublickName);
    static public BoolConfiguration VoteWatching = NebulaAPI.Configurations.Configuration("options.role.admiralU.voteWatching", true);
    static private BoolConfiguration NotGuess = NebulaAPI.Configurations.Configuration("options.role.admiralU.notGuess", false);
    static private BoolConfiguration PrivateNameGuess = NebulaAPI.Configurations.Configuration("options.role.admiralU.privateNameGuess", false,() => NotGuess);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Admiral.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public AdmiralU MyRole = new();
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(admiralButtonSprite, "role.admiralU.ability.skill");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%PUBLIC%", Language.Translate(PublickName ? "role.admiralU.ability.main.public" : "role.admiralU.ability.main.private"));
    }
    static private Image admiralButtonSprite = NebulaAPI.AddonAsset.GetResource("AdmiralSkillButton.png")!.AsImage(115f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        private bool isColorActivated = false;
        static bool usingAdmiralSkill = false;
        bool myRevive = false;
        int leftAdmiral = NumOfAdmiral;
        static List<GamePlayer> AdmiralName = new List<GamePlayer>();
        static List<GamePlayer> ExiledPlayer = new List<GamePlayer>();
        float TaskPercent = NumOfPublickTask / 10f;

        
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                if (!usingAdmiralSkill) Nebula.Utilities.Helpers.TextHudContent("AdmiralUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.admiralU.hudText") + ": " + leftAdmiral, true);

                bool usedInTheMeeting = false;
                var admiralSkillButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                admiralSkillButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted && leftAdmiral > 0;
                admiralSkillButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.Discussion) && !usedInTheMeeting && leftAdmiral > 0;
                admiralSkillButton.SetLabel("admiral");
                admiralSkillButton.SetSprite(admiralButtonSprite.GetSprite());
                admiralSkillButton.OnClick = (button) =>
                {
                    usedInTheMeeting = true;
                    usingAdmiralSkill = true;
                    RpcAdmiralMessage.Invoke(true);
                    leftAdmiral--;
                };

                admiralSkillButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };

                if (VoteWatching)
                {
                    var harmony = new HarmonyLib.Harmony("com.hori.admiralU.anonymousvote");
                    AnonymousVoteBypass.Patch(harmony);
                }
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        RemoteProcess<bool> RpcAdmiralMessage = new("RpcAdmiralMessage_Uchu",
        (message, _) =>
        {
            usingAdmiralSkill = message;
        });

        void OnExiled(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsDead && ev.Player != MyPlayer) return; //ここで自分が死亡チェックをしてるから自身が復活できない？
            if (usingAdmiralSkill == false) return;

            //能力を使用してたら追放されたプレイヤーをリストに
            if (AmOwner && usingAdmiralSkill)
            {
                GamePlayer exiled = ev.Player;  

                if (exiled != null && exiled.IsDead)
                {
                    if (!ExiledPlayer.Contains(exiled))ExiledPlayer.Add(exiled);
                }
            }
        }


        void MeetingEnd(MeetingPreEndEvent ev)
        {
            usingAdmiralSkill = false;
            RpcAdmiralMessage.Invoke(false);

            foreach (var p in ExiledPlayer)
            {
                if (p != null && p.IsDead)
                {
                    p.Revive(p, p.TruePosition, true, true);
                }
            }
            ExiledPlayer.Clear();
        }

        void ShowMessage(FixExileTextEvent ev)
        {
            if (!usingAdmiralSkill) return;

            ev.AddText(NebulaAPI.Language.Translate("role.admiralU.message1"));
        }

        [Local]
        void TaskCompleted(PlayerTaskCompleteEvent ev)
        {
            if (!PublickName) return;
            if (MyPlayer.Tasks.CurrentCompleted >= NumOfPublickTask)
            {
                RpcAdmiralName.Invoke(MyPlayer);
            }
            /*if (MyPlayer.Tasks.CurrentCompleted / MyPlayer.Tasks.CurrentTasks >= TaskPercent)
            {
                RpcAdmiralName.Invoke(MyPlayer);
            }
        }

        [OnlyMyPlayer]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (!PublickName) return;
            if (!AdmiralName.Contains(ev.Player)) return;
            ev.Color = new Virial.Color(61f / 255f, 88f / 255f, 179f / 255f);
        }

        RemoteProcess<GamePlayer> RpcAdmiralName = new("PublicName", (message, _) =>
        {
            AdmiralName.Add(message);
        });

        [OnlyMyPlayer]
        void Guess(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (NotGuess)
            {
                if (PrivateNameGuess)
                {
                    if (AdmiralName.Count == 0) return;
                    ev.CanGuess = false;
                }
                else
                {
                    ev.CanGuess = false;
                }
            }
        }

        /*
        [OnlyMyPlayer]
        void Die(PlayerDieEvent ev)
        {
            if (MeetingHud.Instance)
            {
                new StaticAchievementToken("seedU.another1"); //会議中に死ぬ
            }
        }

        
    }
}
*/