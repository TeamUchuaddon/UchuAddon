using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Virial.Accessibility;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Media;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class BettorU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.bettorU", new(247, 157, 72), TeamRevealType.OnlyMe);

    private BettorU() : base("bettorU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [AnalyzeCooldownOption, NumOfAnalyzeOption, AllowIdentifyonDeadPlayersOption, GetIdentifyPercentageOption, DisableGuessTurn])
    {
		base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Bettor.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagChaotic);
    }


    static private readonly FloatConfiguration AnalyzeCooldownOption = NebulaAPI.Configurations.Configuration("options.role.bettorU.analyzeCooldownOption", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private readonly IntegerConfiguration NumOfAnalyzeOption = NebulaAPI.Configurations.Configuration("options.role.bettorU.numOfAnalyzeOption", (0, 10, 1), 3);
    static private readonly BoolConfiguration AllowIdentifyonDeadPlayersOption = NebulaAPI.Configurations.Configuration("options.role.bettorU.allowIdentifyonDeadPlayersOption", true);
    static private readonly FloatConfiguration GetIdentifyPercentageOption = NebulaAPI.Configurations.Configuration("options.role.bettorU.getIdentifyPercentageOption", (0f, 100f, 10f), 50f, FloatConfigurationDecorator.Percentage);
    static private readonly IntegerConfiguration DisableGuessTurn = NebulaAPI.Configurations.Configuration("options.role.bettorU.disableGuessTurn", (0, 5, 1), 1);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Bettor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(guessButtonSprite, "role.bettorU.ability.guess");
        yield return new(analyzeButtonSprite, "role.bettorU.ability.analyze");
        yield return new(IdentifyButtonSprite, "role.bettorU.ability.identify");
    }

    static Image guessButtonSprite = NebulaAPI.AddonAsset.GetResource("BettorGuessButton.png")!.AsImage(115f)!;
    static Image analyzeButtonSprite = NebulaAPI.AddonAsset.GetResource("BettorAnalyzeButton.png")!.AsImage(115f)!;

    static Image guessScreenBG = NebulaAPI.AddonAsset.GetResource("BettorBG.png")!.AsImage(150f)!;
    static MultiImage closeButtonSprite = NebulaAPI.AddonAsset.GetResource("BettorCloseButton.png")!.AsMultiImage(2, 1, 100f)!;
    static MultiImage choiceButtonSprite = NebulaAPI.AddonAsset.GetResource("ChoiceButton.png")!.AsMultiImage(2, 1, 100f)!;
    static Image BetButtonSprite = NebulaAPI.AddonAsset.GetResource("BetButton.png")!.AsImage(100f)!;

    static MultiImage InformationIcon = NebulaAPI.AddonAsset.GetResource("NoticeIcon.png")!.AsMultiImage(4, 1, 250f)!;
    static Image IdentifyButtonSprite = NebulaAPI.AddonAsset.GetResource("IdentifyButton.png")!.AsImage(115f)!;


    static public BettorU MyRole = new BettorU();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        private enum State
        {
            MISS,
            BLOW,
            HIT
        }

        bool guessed = false;

        bool canUseIdentify = false;

        bool gottenIdentify = false;

        int disableGuessTurn = DisableGuessTurn;

        Dictionary<GamePlayer, DefinedRole?> guessingRoles = new();
        Dictionary<GamePlayer, State> state = new();

        Dictionary<GamePlayer, string?> analyzedPlayers = new();

        List<DefinedRole> blowRoles = new();
        List<DefinedRole> missRoles = new();

        List<GamePlayer> identified = new();

        static private Image identifyIcon = MeetingPlayerButtonManager.Icons.AsLoader(2);


        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                Helpers.TextHudContent("BettorText", this, tmPro => tmPro.text = $"{NebulaAPI.Language.Translate(!gottenIdentify ? "role.bettorU.identifyHelp" : "role.bettorU.normalHelp")}: {(int)(((float)state.Values.Count(v => v == State.HIT) / (float)state.Count) * 100)}%{(gottenIdentify ? "" : $" / {(float)GetIdentifyPercentageOption}%")}", true);
            }
        }



        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {

                int leftAnalyze = NumOfAnalyzeOption;

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer, p => state[p] != State.HIT);
                playerTracker.SetColor(MyRole.RoleColor);

                ModAbilityButton? analyzeButton = null;

                var guessButton = new ModAbilityButtonImpl(alwaysShow: true).Register(this);
                guessButton.SetSprite(guessButtonSprite.GetSprite());
                guessButton.Availability = (button) => (MyPlayer.CanMove || MeetingHud.Instance) && disableGuessTurn <= 0;
                guessButton.Visibility = (button) => !ExileController.Instance && !MyPlayer.IsDead;
                guessButton.OnClick = (button) =>
                {
                    MyPlayer.VanillaPlayer.protectedByGuardianId = MyPlayer.PlayerId;
                    OpenGuessScreen();
                };
                guessButton.SetLabel("guess");
                guessButton.KeyBind(VirtualKeyInput.Ability);

                analyzeButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, playerTracker, VirtualKeyInput.SecondaryAbility, null, AnalyzeCooldownOption, "analyze", analyzeButtonSprite, (player, button) =>
                {

                    if (player.IsImpostor) analyzedPlayers[player] = NebulaAPI.Language.Translate("help.rolePreview.category.impostor").Color(Palette.ImpostorRed);
                    else if (player.IsCrewmate) analyzedPlayers[player] = NebulaAPI.Language.Translate("help.rolePreview.category.crewmate").Color(Palette.CrewmateBlue);
                    else analyzedPlayers[player] = NebulaAPI.Language.Translate("help.rolePreview.category.neutral").Color(new Color(244f / 255f, 211f / 255f, 53f / 255f));

                    NebulaAPI.CurrentGame?.GetModule<BettorOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, GetAnalyzeResult()), InformationIcon.AsLoader(1), MyRole.RoleColor, false);

                    leftAnalyze --;
                    analyzeButton?.UpdateUsesIcon(leftAnalyze.ToString());
                    button.StartCoolDown();
                }, _ => leftAnalyze != 0);

                analyzeButton.ShowUsesIcon(2, leftAnalyze.ToString());


                foreach (var pl in NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p != MyPlayer))
                {
                    guessingRoles[pl] = null;
                    state[pl] = State.MISS;
                    analyzedPlayers[pl] = null;
                }

            }
        }

        IEnumerable<GUIWidget> GetAnalyzeResult()
        {
            yield return NebulaAPI.GUI.LocalizedText(GUIAlignment.Left, AttributeAsset.OverlayTitle, "role.bettorU.analyze.title");
            yield return NebulaAPI.GUI.VerticalMargin(0.1f);
            foreach (var player in analyzedPlayers.Keys)
            {
                if (analyzedPlayers[player] != null) yield return NebulaAPI.GUI.RawText(GUIAlignment.Left, AttributeAsset.OverlayContent, $"{player.ColoredName} - {analyzedPlayers[player]}");
            }
        }

        [Local]
        void OnPlayerDisconnected(PlayerDisconnectEvent ev)
        {
            guessingRoles.Remove(ev.Player);
            state.Remove(ev.Player);
        }


        [Local]
        void OnGameStart(GameStartEvent ev)
        {
            foreach (var pl in NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p != MyPlayer))
            {
                guessingRoles[pl] = null;
                state[pl] = State.MISS;
                analyzedPlayers[pl] = null;
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
                var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
                buttonManager?.RegisterMeetingAction(new(IdentifyButtonSprite,
                   p =>
                   {
                       canUseIdentify = false;
                       identified.Add(p.MyPlayer);
                       RPCIdentify.Invoke((p.MyPlayer.PlayerId, MyPlayer));

                   },
                   p => !MyPlayer.IsDead && canUseIdentify && !p.MyPlayer.AmOwner && (AllowIdentifyonDeadPlayersOption || !p.MyPlayer.IsDead) && !identified.Contains(p.MyPlayer))
                   );
        }

        [Local]
        void SeePlayerRole(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            if (identified.Contains(ev.Player)) ev.CanSeeRole = true;
        }

        RemoteProcess<(byte targetPlayerId, GamePlayer owner)> RPCIdentify = new("Identify", (message, _) =>
        {
            if (MeetingHud.Instance.TryGetPlayer(message.targetPlayerId, out var pva))
            {
                pva.NameText.StartCoroutine(AnimationEffects.CoPlayRoleNameEffect(pva.NameText.transform.parent, new(0.3384f, -0.13f, -0.1f), BettorU.MyRole.UnityColor, LayerExpansion.GetUILayer(), 2f).WrapToIl2Cpp());

                if (!message.owner.AmOwner) NebulaManager.Instance.StartCoroutine(CoAnimateRoleText(pva.NameText.transform.Find("RoleText").GetComponent<TextMeshPro>()).WrapToIl2Cpp());                

            }

        });

        static IEnumerator CoAnimateRoleText(TextMeshPro roleText)
        {
            if (roleText.text == "")
            {
                var text = GameObject.Instantiate(roleText, roleText.transform.parent);
                text.gameObject.name = "BettorText";

                while (MeetingHud.Instance)
                {
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&=^@:/\\+-";
                    text.text = new string(Enumerable.Range(0, 10).Select(_ => chars[UnityEngine.Random.Range(0, chars.Length)]).ToArray()).Color(Color.gray);

                    yield return new WaitForSeconds(0.05f);
                }

            }
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (guessingRoles.Keys.All(p => state[p] == State.HIT))
            {
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(UchuGameEnd.BettorWin, 1 << MyPlayer.PlayerId);
            }
            if (guessed)
            {
                guessed = false;
            }

            if (disableGuessTurn > 0) disableGuessTurn--;
        }

		[Local]
        void GetPlayerRole(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            if (state[ev.Player] == State.HIT) ev.CanSeeRole = true;
        }

        MetaScreen roleSelectWindow = null!;

        public MetaScreen OpenGuessScreen()
        {
            MyPlayer.VanillaPlayer.moveable = false;
            MyPlayer.VanillaPlayer.MyPhysics.body.velocity = Vector2.zero;

            roleSelectWindow = null!;

            var window = MetaScreen.GenerateWindow(new Vector2(4.5f, 3.8f), HudManager.Instance.transform, new Vector3(0f, 0f, -200f), false, false, true, BackgroundSetting.Off, false);

            

            IEnumerable<GUIWidget?> GetWidgets()
            {
                foreach (var player in guessingRoles.Keys)
                {
                    yield return NebulaAPI.GUI.HorizontalHolder(GUIAlignment.Center,
                        NebulaAPI.GUI.HorizontalMargin(0.3f),
                        new NoSGameObjectGUIWrapper(GUIAlignment.Center, () =>
                        {
                            var poolablePlayer = AmongUsUtil.GetPlayerIcon(player.DefaultOutfit.outfit, null, Vector3.zero, new Vector3(0.35f, 0.35f, 1f), false, false);
                            poolablePlayer.SetAsGUIComponent(true);
                            return (poolablePlayer.gameObject, new(1f, 1f));
                        }),
                        NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.OptionsValue, player.PlayerName),
                        NebulaAPI.GUI.HorizontalMargin(0.15f),
                        new NoSGUIText(GUIAlignment.Center, AttributeAsset.OptionsValueShorter, new RawTextComponent(state[player] != State.HIT ? (guessingRoles[player]?.DisplayColoredName ?? "未選択") : player.Role.Role.DisplayColoredName)),
                        NebulaAPI.GUI.HorizontalMargin(0.15f),
                        MeetingHud.Instance && !guessed && state[player] != State.HIT ? NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.OptionsButton, "-") : state[player] != State.HIT && !guessed ? NebulaAPI.GUI.Image(GUIAlignment.Center, choiceButtonSprite.AsLoader(0), new(0.5f, 0.5f), _ =>
                        {
                            roleSelectWindow = MeetingRoleSelectWindow.OpenRoleSelectWindow(Roles.AllRoles.Where(r => r.IsSpawnable), null, true, "", role =>
                            {
                                guessingRoles[player] = role;
                                roleSelectWindow.CloseScreen();
                                window.CloseScreen();
                                OpenGuessScreen();
                            });
                        }) : ( state[player] == State.HIT ? NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.OptionsButton, "HIT".Color(UnityEngine.Color.yellow)) : (state[player] == State.BLOW ? NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.OptionsButton, "BLOW".Color(UnityEngine.Color.green)) : NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.OptionsButton, "MISS"))));
                }
                if (!guessed)

                {
                    yield return NebulaAPI.GUI.HorizontalHolder(GUIAlignment.Center, NebulaAPI.GUI.HorizontalMargin(0.3f), NebulaAPI.GUI.Image(GUIAlignment.Center, BetButtonSprite, new(1f, 1f), _ =>

                    {

                        if (!guessingRoles.Values.Contains(null))
                        {
                            guessed = true;

                            foreach (var p in guessingRoles.Keys)
                            {
                                if (state[p] == State.HIT && guessingRoles[p] != p.Role.Role) guessingRoles[p] = p.Role.Role;

                                if (guessingRoles[p] == p.Role.Role) { state[p] = State.HIT; blowRoles.Add(guessingRoles[p]!); missRoles.Remove(guessingRoles[p]!); }
                                else if (guessingRoles.Keys.Select(r => r.Role.Role).Contains(guessingRoles[p])) { state[p] = State.BLOW; blowRoles.Add(guessingRoles[p]!); missRoles.Remove(guessingRoles[p]!); }
                                else { missRoles.Add(guessingRoles[p]!); blowRoles.Remove(guessingRoles[p]!); Debug.Log("MISS " + guessingRoles[p]!.DisplayColoredName); }
                            }

                            blowRoles = blowRoles.Distinct().ToList();
                            missRoles = missRoles.Distinct().ToList();


                            NebulaAPI.CurrentGame?.GetModule<BettorOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, NebulaAPI.GUI.LocalizedText(GUIAlignment.Left, AttributeAsset.OverlayTitle, "role.bettorU.information.title"), NebulaAPI.GUI.VerticalMargin(0.1f), NebulaAPI.GUI.LocalizedText(GUIAlignment.Left, AttributeAsset.OverlayBold, "role.bettorU.information.blow"), NebulaAPI.GUI.RawText(GUIAlignment.Left, AttributeAsset.OverlayContent, blowRoles.Count == 0 ? NebulaAPI.Language.Translate("role.bettorU.information.none") : string.Join("", blowRoles.Select((role, index) =>role.DisplayColoredName + (index < blowRoles.Count - 1 ? ", " : "") + ((index + 1) % 3 == 0 && index < blowRoles.Count - 1 ? "\n" : "")))), NebulaAPI.GUI.VerticalMargin(0.075f), NebulaAPI.GUI.LocalizedText(GUIAlignment.Left, AttributeAsset.OverlayBold, "role.bettorU.information.miss"), NebulaAPI.GUI.RawText(GUIAlignment.Left, AttributeAsset.OverlayContent, missRoles.Count == 0 ? NebulaAPI.Language.Translate("role.bettorU.information.none") : string.Join("", missRoles.Select((role, index) => role.DisplayColoredName + (index < missRoles.Count - 1 ? ", " : "") + ((index + 1) % 3 == 0 && index < missRoles.Count - 1 ? "\n" : ""))))), InformationIcon.AsLoader(0), MyRole.RoleColor, true);

                            window.CloseScreen();
                            OpenGuessScreen();


                            if ((float)state.Values.Count(v => v == State.HIT) / (float)state.Count >= (GetIdentifyPercentageOption / 100) && !gottenIdentify)
                            {
                                canUseIdentify = true;
                                gottenIdentify = true;
                            }
                        }
                    }));
                }
            }

            var scrollWidget = NebulaAPI.GUI.ScrollView(
                GUIAlignment.Center,
                new(4.5f, 3.8f),
                "bettorScrollView",
                NebulaAPI.GUI.VerticalHolder(GUIAlignment.Center, GetWidgets()),
                out _
            );

            window.SetWidget(scrollWidget, new Vector2(0.5f, 0.7f), out _);
            var collider = UnityHelper.CreateObject<BoxCollider2D>("CloseButton", window.transform.parent.parent, new Vector3(-2f, 2.5f, -1f));
            collider.transform.localScale = new(0.17f, 0.17f, 1f);
            collider.isTrigger = true;
            collider.size = new(3.5f, 3.5f);
            SpriteRenderer? renderer = null;
            renderer = collider.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = closeButtonSprite.GetSprite(0);
            var button = collider.gameObject.SetUpButton(true);
            button.OnClick.AddListener(() =>
            {
                window.CloseScreen();
                MyPlayer.VanillaPlayer.moveable = true;
            });
            button.OnMouseOver.AddListener(() =>
            {
                renderer.sprite = closeButtonSprite.GetSprite(1);
            });
            button.OnMouseOut.AddListener(() =>
            {
                renderer.sprite = closeButtonSprite.GetSprite(0);
            });
            NebulaManager.Instance.RegisterUI(window.transform.parent.parent.gameObject, button);


            window.transform.GetComponentsInChildren<SpriteRenderer>(true).Where(s => s.gameObject.name == "UI_ScrollbarTrack(Clone)" || s.gameObject.name == "UI_Scrollbar(Clone)").Do(s => GameObject.Destroy(s.gameObject));

                var background = UnityHelper.CreateObject<SpriteRenderer>("Background", window.transform.parent.parent, new Vector3(0f, 0f, 0.1f));
            background.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            background.sprite = guessScreenBG.GetSprite();

            return window;
        }

    }


}

internal class BettorOverlayHolder : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static IDividedSpriteLoader IconSprite = DividedSpriteLoader.FromResource("Nebula.Resources.MeetingNotification.png", 100f, 42, 42, true);
    static Image NotificationSprite = SpriteLoader.FromResource("Nebula.Resources.MeetingNotificationDot.png", 135f);

    Transform? shower;

    public bool IsDeadObject => false;

    public BettorOverlayHolder()
    {
        this.RegisterPermanently();
        ModSingleton<BettorOverlayHolder>.Instance = this;
    }

    static BettorOverlayHolder()
    {
        DIManager.Instance.RegisterModule(() => new BettorOverlayHolder());
    }


    SpriteRenderer informationRenderer = null!;
    SpriteRenderer resultRenderer = null!;

    public void RegisterOverlay(GUIWidgetSupplier overlay, Image icon, Virial.Color color/*, bool isNew*/, bool isInformation)
    {
        if (!shower) return;

        var renderer = isInformation ? informationRenderer : resultRenderer;
        bool isNew = false;

        if (renderer == null) isNew = true;

        if (renderer != null) GameObject.Destroy(renderer.gameObject);


        renderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", shower, new((isInformation ? -0.2f : 0.28f), 0f, 0f));
        renderer.sprite = IconSprite.GetSprite(0);
        renderer.color = Color.Lerp(color.ToUnityColor(), Color.white, 0.3f);

        var iconInner = UnityHelper.CreateObject<SpriteRenderer>("Inner", renderer.transform, new(0f, 0f, -1f));
        iconInner.sprite = icon.GetSprite();


        var notification = UnityHelper.CreateObject<SpriteRenderer>("Notification", renderer.transform, new(0.19f, 0.19f, -1.5f));
        notification.sprite = NotificationSprite.GetSprite();
        notification.gameObject.SetActive(true);

        IEnumerator CoAppear()
        {
            float p = 0f;
            while (true)
            {
                p += Time.deltaTime * 2.4f;
                if (p > 1f) break;

                if (p > 0f)
                    renderer.transform.localScale = Vector3.one * (p + Mathn.Pow(Mathn.Cos(0.5f * p * Mathn.PI), 1.5f) * Mathn.Pow(p, 0.3f) * 2.5f);
                else
                    renderer.transform.localScale = Vector3.zero;
                yield return null;
            }
            renderer.transform.localScale = Vector3.one;
        }

        if (isNew) NebulaManager.Instance.StartCoroutine(CoAppear().WrapToIl2Cpp());

        var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new(0.4f, 0.4f);

        var button = renderer.gameObject.SetUpButton(false, renderer, color.ToUnityColor());
        button.OnMouseOver.AddListener(() => { VanillaAsset.PlayHoverSE(); NebulaManager.Instance.SetHelpWidget(button, overlay.Invoke()); notification.gameObject.SetActive(false);});
        button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));

        if (isInformation) informationRenderer = renderer;
        else resultRenderer = renderer;

    }

    [EventPriority(+1)]
    void OnGameStart(GameStartEvent ev)
    {
        shower = UnityHelper.CreateObject("OverlayHolder", HudManager.Instance.transform, new(0f, 2.7f, -20f)).transform;
    }
}