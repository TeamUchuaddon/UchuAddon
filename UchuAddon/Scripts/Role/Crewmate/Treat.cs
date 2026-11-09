using Epic.OnlineServices;
using Hori.Core;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Neutral;
using Il2CppInterop.Runtime.Injection;
using Il2CppMono.Security.X509;
using Nebula.Modules.Cosmetics;
using Rewired.Utils.Classes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Virial.Events.Game.Minimap;
using Virial.Logging;
using Virial.Media;
using static Hori.Scripts.Role.Crewmate.AdmiralU;
using static Hori.Scripts.Role.Crewmate.SonarU;
using static Nebula.Roles.Impostor.Hadar;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class TreatU : DefinedSingleAbilityRoleTemplate<TreatU.Ability>, DefinedRole, IAssignableDocument
{
    public TreatU() : base("treatU", new(207, 135, 12), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [SweetCooldown, MixCooldown, SweetGetCooldown, SweetGetPerTask, NumOfSweet,
    new GroupConfiguration("options.role.treatU.group.sweet",[BeanDuration, BeanRatio, EnhancedBeanRatio, ConfigurationMargin, CandyDuration, EnhancedCandyDuration, ConfigurationMargin, CookieDuration, EnhancedCookieDuration, ConfigurationMargin, JellyDuration, JellyRatio, EnhancedJellyRatio], GroupConfigurationColor.ToDarkenColor(new UnityEngine.Color(0.8118f, 0.5294f, 0.0471f)))])
    {
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate; //マッド化
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SweetCooldown = NebulaAPI.Configurations.Configuration("options.role.treatU.sweetCooldown", (5f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SweetGetCooldown = NebulaAPI.Configurations.Configuration("options.role.treatU.sweetGetCooldown", (10f, 120f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration SweetGetPerTask = NebulaAPI.Configurations.Configuration("options.role.treatU.sweetGetPerTask", false);
    static private IntegerConfiguration NumOfSweet = NebulaAPI.Configurations.Configuration("options.role.treatU.numOfSweet", (3, 14), 8);
    static private FloatConfiguration MixCooldown = NebulaAPI.Configurations.Configuration("options.role.treatU.mixCooldown", (5f, 60f, 5f), 10f, FloatConfigurationDecorator.Second);

    static private readonly FloatConfiguration BeanDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.beanDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration BeanRatio = NebulaAPI.Configurations.Configuration("options.role.treatU.beanRatio", (0.5f, 3f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private readonly FloatConfiguration EnhancedBeanRatio = NebulaAPI.Configurations.Configuration("options.role.treatU.enhancedBeanRatio", (0.125f, 5f, 0.125f), 1f, FloatConfigurationDecorator.Ratio);

    static private readonly FloatConfiguration CandyDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.candyDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration EnhancedCandyDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.enhancedCandyDuration", (2.5f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);

    static private readonly FloatConfiguration CookieDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.cookieDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration EnhancedCookieDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.enhancedCookieDuration", (2.5f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);

    static private readonly FloatConfiguration JellyDuration = NebulaAPI.Configurations.Configuration("options.role.treatU.jellyDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration JellyRatio = NebulaAPI.Configurations.Configuration("options.role.treatU.jellyRatio", (1.125f, 5f, 0.125f), 2f, FloatConfigurationDecorator.Ratio);
    static private readonly FloatConfiguration EnhancedJellyRatio = NebulaAPI.Configurations.Configuration("options.role.treatU.enhancedJellyRatio", (0.125f, 5f, 0.125f), 2f, FloatConfigurationDecorator.Ratio);

    static private readonly IConfiguration ConfigurationMargin = NebulaAPI.Configurations.Configuration(() => null, () =>
    {
        return NebulaAPI.GUI.VerticalMargin(0.25f);
    });

    static public TreatU MyRole = new();

    static private Virial.Media.Image buttonSprite = NebulaAPI.AddonAsset.GetResource("TreatButton.png")!.AsImage(115f)!;
    static private Virial.Media.Image MixImage = NebulaAPI.AddonAsset.GetResource("TreatMixButton.png")!.AsImage(115f)!;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Treat.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(buttonSprite, "role.treatU.ability.sweet");
        yield return new(MixImage, "role.treatU.ability.mix");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%GET%", SweetGetCooldown.GetValue().ToString());
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        private Transform holder = null!;

        public static MultiImage SweetsImage = NebulaAPI.AddonAsset.GetResource("TreatSweets.png")!.AsMultiImage(4, 2, 1500f)!;

        private readonly List<SweetType> sweets = new();
        private readonly List<SpriteRenderer> sweetRenderers = new();
        private int MaxHoldSweet = NumOfSweet;

        private SpriteRenderer? selectedMixSweet = null; // 一つ目のMix
        private SweetType selectedMixType;

        private enum SweetType
        {
            Bean,
            Candy,
            Cookie,
            Jelly,

            EnhancedBean,
            EnhancedCandy,
            EnhancedCookie,
            EnhancedJelly
        }

        private SweetType GetEnhancedType(SweetType type)
        {
            return type switch
            {
                SweetType.Bean or SweetType.EnhancedBean => SweetType.EnhancedBean,
                SweetType.Candy or SweetType.EnhancedCandy => SweetType.EnhancedCandy,
                SweetType.Cookie or SweetType.EnhancedCookie => SweetType.EnhancedCookie,
                SweetType.Jelly or SweetType.EnhancedJelly => SweetType.EnhancedJelly,
                _ => type
            };
        }

        ModAbilityButton sweetButton = null!, mixButton = null!;
        ObjectTracker<Player> playerTracker = null!;
        GameObject? widget;

        float lastShareTime = -10f;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                holder = HudContent.InstantiateStretchContent(this, "TreatSweetsHolder", true, true);

                playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                sweetButton = NebulaAPI.Modules.AbilityButton(this).BindKey(VirtualKeyInput.None);
                sweetButton.SetImage(buttonSprite);
                sweetButton.SetLabel("sweet");
                sweetButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, SweetCooldown).SetAsAbilityTimer().Start();
                (sweetButton as ModAbilityButtonImpl)?.SetCanUseByMouseClick(true, ButtonEffect.ActionIconType.NonclickAction, "sweetAction", false);
                sweetButton.OnClick = (button) =>
                {
                    DebugScreen.Push(Language.Translate("role.treatU.pushSweetButton"), 1f);
                };

                mixButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "treatU.mix", MixCooldown, "mix", MixImage);
                mixButton!.OnClick = (button) =>
                {
                    UpdateMixMode(IsMixing);
                };

                // 設定秒ごとにお菓子を入手
                GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev =>
                {
                    if (NebulaGameManager.Instance!.CurrentTime > lastShareTime + SweetGetCooldown)
                    {
                        lastShareTime = NebulaGameManager.Instance!.CurrentTime;

                        if (MeetingHud.Instance || ExileController.Instance) return;

                        var randomType = (SweetType)UnityEngine.Random.Range(0, 4);
                        AddSweets(randomType);
                    }
                }, this);

                GameOperatorManager.Instance?.Subscribe<PlayerTaskCompleteLocalEvent>(ev =>
                {
                    if (SweetGetPerTask)
                    {
                        var randomType = (SweetType)UnityEngine.Random.Range(0, 4);
                        AddSweets(randomType);
                    }
                }, this);

                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev =>
                {
                    UpdateMixMode(true);
                }, this);

                GameOperatorManager.Instance?.Subscribe<GameHudUpdateEvent>(ev =>
                {
                    holder.gameObject.SetActive(!Minigame.Instance);
                }, this);
            }
        }

        void GiveSweet(GamePlayer target, SweetType type)
        {
            if (type == SweetType.Bean || type == SweetType.EnhancedBean) // 赤いゼリービーンズ
            {
                target.GainSpeedAttribute(type == SweetType.Bean ? BeanRatio : BeanRatio + EnhancedBeanRatio, BeanDuration, false, 100);
            }
            else if (type == SweetType.Cookie || type == SweetType.EnhancedCookie)
            {
                target.GainAttribute(PlayerAttributes.Invisible, type == SweetType.Cookie ? CookieDuration : CookieDuration + EnhancedCookieDuration, false, 100);
            }
            else if (type == SweetType.Candy || type == SweetType.EnhancedCandy)
            {
                target.GainAttribute(UchuAttributes.Rainbow, type == SweetType.Candy ? CandyDuration : CandyDuration + EnhancedCandyDuration, false, 100);
                target.GainAttribute(UchuAttributes.ObviousGuard, type == SweetType.Candy ? CandyDuration : CandyDuration + EnhancedCandyDuration, false, 100);
            }
            else if (type == SweetType.Jelly || type == SweetType.EnhancedJelly)
            {
                target.GainAttribute(PlayerAttributes.ScreenSize, JellyDuration, type == SweetType.Jelly ? JellyRatio : JellyRatio + EnhancedJellyRatio, false, 100);
                target.GainAttribute(PlayerAttributes.Eyesight, JellyDuration, type == SweetType.Jelly ? JellyRatio : JellyRatio + EnhancedJellyRatio, false, 100);
                target.GainSizeAttribute(type == SweetType.Jelly ? new Vector2(JellyRatio, JellyRatio) : new Vector2(JellyRatio + EnhancedJellyRatio, JellyRatio + EnhancedJellyRatio), 10f, false, 100);
            }
        }

        private void MixSweet(SpriteRenderer sweet, SweetType type)
        {
            if (selectedMixSweet == null)
            {
                selectedMixSweet = sweet;
                selectedMixType = type;

                return;
            }

            if (selectedMixSweet == sweet)
            {
                return;
            }

            int firstIndex = sweetRenderers.IndexOf(selectedMixSweet);
            int secondIndex = sweetRenderers.IndexOf(sweet);

            if (firstIndex < 0 || secondIndex < 0)
            {
                selectedMixSweet = null;
                return;
            }

            SweetType chosenType = UnityEngine.Random.Range(0, 2) == 0 ? selectedMixType : type; // 強化するのを決定
            SweetType enhancedType = GetEnhancedType(chosenType);

            // 後始末
            selectedMixSweet = null;

            int maxIndex = Math.Max(firstIndex, secondIndex);
            int minIndex = Math.Min(firstIndex, secondIndex);

            var firstSweet = sweetRenderers[firstIndex];
            var secondSweet = sweetRenderers[secondIndex];

            sweets.RemoveAt(maxIndex);
            sweetRenderers.RemoveAt(maxIndex);

            sweets.RemoveAt(minIndex);
            sweetRenderers.RemoveAt(minIndex);

            UnityEngine.Object.Destroy(firstSweet.gameObject);
            UnityEngine.Object.Destroy(secondSweet.gameObject);


            AddSweets(enhancedType); // お菓子を追加
            UpdateSweets();
            UpdateMixMode(true);
            mixButton.StartCoolDown();
            if (sweetButton.CoolDownTimer!.CurrentTime < 5f) sweetButton.CoolDownTimer?.Start(5f);

            AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.45f);
        }

        private void AddSweets(SweetType type)
        {
            if (sweets.Count >= MaxHoldSweet) return;

            sweets.Add(type);

            var sweet = UnityHelper.CreateObject<SpriteRenderer>("TreatSweet", holder, Vector3.zero, LayerExpansion.GetUILayer());

            sweet.sprite = SweetsImage.GetSprite((int)type);
            sweetRenderers.Add(sweet);
            NebulaManager.Instance.StartCoroutine(CoPlaySweetGetAnimation(sweet.transform).WrapToIl2Cpp());

            var collider = sweet.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.2f, 0.4f);

            var button = sweet.gameObject.SetUpButton(true, sweet);
            button.OnClick.AddListener(() =>
            {
                if (MyPlayer.IsDead) return;

                if (IsMixing)
                {
                    MixSweet(sweet, type);
                    return;
                }

                var target = playerTracker.CurrentTarget;
                if (target != null && !sweetButton.IsInCooldown) // 渡す対象がいてクールダウンが明けている
                {
                    GiveSweet(target, type);
                    sweetButton.StartCoolDown();
                    AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.45f);
                    RpcFlashOnTarget.Invoke(target);

                    int index = sweetRenderers.IndexOf(sweet);
                    if (index >= 0)
                    {
                        sweets.RemoveAt(index);
                        sweetRenderers.RemoveAt(index);

                        UnityEngine.Object.Destroy(sweet.gameObject);

                        UpdateSweets(); // 更新
                    }
                }
                else if (sweetButton.IsInCooldown)
                {
                    DebugScreen.Push(Language.Translate("role.treatU.pushCooldown"), 1f);
                }
                else if (!sweetButton.IsInCooldown && target == null)
                {
                    DebugScreen.Push(Language.Translate("role.treatU.pushTarget"), 1f);
                }
            });
            button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(button, Language.Translate($"role.treatU.{type.ToString()}")));
            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));

            UpdateSweets();
        }

        private IEnumerator CoPlaySweetGetAnimation(Transform target)
        {
            float expandDuration = 0.2f;  // 拡大時間
            float holdDuration = 0.1f;    // 最大サイズで停止する時間
            float shrinkDuration = 0.2f;  // 縮小時間

            float maxScale = 1.35f;

            Vector3 originalScale = Vector3.one;
            Vector3 enlargedScale = originalScale * maxScale;

            float time = 0f;
            while (time < expandDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / expandDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                if (target != null) target.localScale = Vector3.Lerp(originalScale, enlargedScale, t);

                yield return null;
            }

            if (target == null) yield break;
            target.localScale = enlargedScale;

            yield return new WaitForSeconds(holdDuration);

            if (target == null) yield break;

            time = 0f;
            while (time < shrinkDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / shrinkDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                target.localScale = Vector3.Lerp(enlargedScale, originalScale, t);

                yield return null;
            }

            if (target != null) target.localScale = originalScale;
        }

        private void UpdateSweets()
        {
            for (int i = 0; i < sweetRenderers.Count; i++)
            {
                sweetRenderers[i].transform.localPosition = new Vector3(i * 0.57f, 0f, 0f);
            }
        }

        private bool IsMixing = false; // trueでミックスモード

        void UpdateMixMode(bool mode) 
        {
            if (!mode) // falseの場合modeにする
            {
                IsMixing = true;
                mixButton.SetLabel("unmix");
                MyPlayer.GainSpeedAttribute(0f, float.MaxValue, false, 0, "treatU.mix");

                var text = NebulaAPI.GUI.RawText(GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(AttributeAsset.CenteredBold), Language.Translate("role.treatU.mixText"));
                widget = text.Instantiate(new(4f, 0.6f), out _);
                if (widget == null) return;

                widget.transform.SetParent(HudManager.Instance.transform);
                widget.transform.localPosition = new Vector3(0f, -2f, 0f);
                widget.layer = LayerExpansion.GetPlayersLayer();
            }
            else
            {
                IsMixing = false;
                mixButton.SetLabel("mix");
                selectedMixSweet = null;
                MyPlayer.RemoveAttributeByTag("treatU.mix");

                if (widget != null)
                {
                    GameObject.Destroy(widget);
                    widget = null;
                }
            }
        }
    }

    static public RemoteProcess<GamePlayer> RpcFlashOnTarget = new("RpcFlashOnTreat_Uchu", (message, _) =>
    {
        if (message.AmOwner)
        {
            AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.45f);
        }
    });
}