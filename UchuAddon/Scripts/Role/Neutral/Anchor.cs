using AsmResolver.PE;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Il2CppInterop.Runtime.Injection;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Media;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class AnchorU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.anchorU", new(8, 0, 237), TeamRevealType.OnlyMe);
    private AnchorU() : base("anchorU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [AnchorPlaceCooldown, NumOfAnchor, NumOfAnchorPerTurn, FakeNoticeCooldown, NumOfFakeNotice, AnchorRange, AnchorVentConfiguration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Anchor.png")!.AsImage(115f);
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Anchor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private FloatConfiguration AnchorPlaceCooldown = NebulaAPI.Configurations.Configuration("options.role.anchorU.PlaceCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfAnchor = NebulaAPI.Configurations.Configuration("options.role.anchorU.NumOfAnchor", (1, 15), 3);
    static public readonly IntegerConfiguration NumOfAnchorPerTurn = NebulaAPI.Configurations.Configuration("options.role.anchorU.NumOfAnchorPerTurn", (1, 15), 1);
    static private FloatConfiguration FakeNoticeCooldown = NebulaAPI.Configurations.Configuration("options.role.anchorU.FakeNoticeCooldown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfFakeNotice = NebulaAPI.Configurations.Configuration("options.role.anchorU.NumOfFakeNotice", (1, 15), 3);
    static private FloatConfiguration AnchorRange = NebulaAPI.Configurations.Configuration("options.role.anchorU.AnchorRangeOption", (2.5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Ratio);
    static private IVentConfiguration AnchorVentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.anchorU.vent", true);
    static public AnchorU MyRole = new AnchorU();


    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(FocusImage, "role.anchorU.ability.place");
        yield return new(AnchorMapButton, "role.anchorU.ability.fakenotice");
    }
    static private Virial.Media.Image FocusImage = NebulaAPI.AddonAsset.GetResource("AnchorButton.png")!.AsImage(115f)!;
    static private Virial.Media.Image AnchorMapButton = NebulaAPI.AddonAsset.GetResource("AnchorFakeButton.png")!.AsImage(115f)!;

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class FocusUPlace : NebulaSyncStandardObject, IGameOperator
    {
        public const string MyTag = "FocusUPlace"; //重要
        private static MultiImage AnchorSprite = NebulaAPI.AddonAsset.GetResource("Anchor.png")!.AsMultiImage(5, 1, 200f)!;
        float indexT = 0f;
        int index = 0;
        public FocusUPlace(Vector2 pos) : base(pos, ZOption.Just, true, AnchorSprite.GetSprite(0))
        {
        }

        static FocusUPlace()
        {
            RegisterInstantiater(MyTag, (args) => new FocusUPlace(new Vector2(args[0], args[1])));
        }
        void OnUpdate(GameUpdateEvent ev)//アニメーション
        {
            indexT -= Time.deltaTime;
            if (indexT < 0f)
            {
                indexT = 0.21f;
                Sprite = AnchorSprite.GetSprite(index % 4);
                index++;
            }
        }
    }

    //マップレイヤー
    private static readonly SpriteLoader mapButtonSprite = SpriteLoader.FromResource("Nebula.Resources.CannonButton.png", 100f);
    static private readonly Image mapButtonInnerSprite = NebulaAPI.AddonAsset.GetResource("AnchorInner.png")!.AsImage(375f)!;
    public class AnchorMapLayer : MonoBehaviour
    {
        static AnchorMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<AnchorMapLayer>();

        public void AddMark(NebulaSyncStandardObject obj, Action onClick)
        {
            var center = VanillaAsset.GetMapCenter(NebulaAPI.AmongUs.MapId);
            var scale = VanillaAsset.GetMapScale(NebulaAPI.AmongUs.MapId);
            var localPos = VanillaAsset.ConvertToMinimapPos(obj.Position, center, scale);

            var renderer = UnityHelper.CreateObject<SpriteRenderer>("AnchorMapButton", transform, localPos.AsVector3(-0.5f));

            renderer.sprite = mapButtonSprite.GetSprite();

            var inner = UnityHelper.CreateObject<SpriteRenderer>("Inner", renderer.transform, new Vector3(0f, 0f, -0.1f));
            inner.sprite = mapButtonInnerSprite.GetSprite();
            inner.gameObject.AddComponent<MinimapScaler>();

            var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;

            var button = renderer.gameObject.SetUpButton(true, renderer);
            button.OnClick.AddListener(() =>
            {
                onClick.Invoke();
                MapBehaviour.Instance.Close();
            });
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public override DefinedRole Role => throw new NotImplementedException();

        private List<NebulaSyncStandardObject> Marks = [];
        private AnchorMapLayer mapLayer = null!;
        int currentIndex = 0;
        Arrow? currentArrow;
        int PlaceCount = NumOfAnchor;
        int NoticeCount = NumOfFakeNotice;
        int TurnPlace = NumOfAnchorPerTurn;
        private ModAbilityButton? mapButton;
        private bool isAnchorMapOpen = false;

        public Instance(GamePlayer player) : base(player, AnchorVentConfiguration)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                PlaceCount = NumOfAnchor;
                NoticeCount = NumOfFakeNotice;
                TurnPlace = NumOfAnchorPerTurn;
                var AnchorPlaceButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, AnchorPlaceCooldown, "AnchorPlace", FocusImage, _ => PlaceCount > 0 && TurnPlace > 0
                );

                AnchorPlaceButton.OnClick = (button) =>
                {
                    var mark = NebulaSyncObject.RpcInstantiate(FocusUPlace.MyTag, [
                        PlayerControl.LocalPlayer.transform.localPosition.x,
                        PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f
                        ]).SyncObject! as NebulaSyncStandardObject; Marks.Add(mark!);
                    if (mapLayer != null)
                    {
                        mapLayer.AddMark(mark!, () =>
                        {
                            ShowMapClickArrow(mark!.Position);
                        });
                    }
                    PlaceCount--;
                    TurnPlace--;
                    RpcAnchorIcon.Invoke(MyPlayer.Position);
                    AmongUsUtil.PlayFlash(Color.Cyan);
                    AnchorPlaceButton.StartCoolDown();
                    AnchorPlaceButton.UpdateUsesIcon(PlaceCount.ToString());
                };
                AnchorPlaceButton.ShowUsesIcon(3, PlaceCount.ToString());
                //マップ開く
                mapButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility, FakeNoticeCooldown, "FakeNotice", AnchorMapButton, _ => NoticeCount > 0);
                mapButton.OnClick = (button) =>
                {
                    isAnchorMapOpen = true;
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        HudManager.Instance.InitMap();
                        MapBehaviour.Instance.ShowNormalMap();
                        MapBehaviour.Instance.taskOverlay.gameObject.SetActive(false);
                    });
                };
                mapButton.ShowUsesIcon(0, NoticeCount.ToString());
            }
        }
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            TurnPlace = NumOfAnchorPerTurn;
            if (!MyPlayer.IsDead && PlaceCount <= 0)
            {
                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.AnchorUTeamWin, 1 << MyPlayer.PlayerId);
            }
        }
        static private Virial.Media.Image FocusAnchorIcon = NebulaAPI.AddonAsset.GetResource("AnchorIcon.png")!.AsImage(200f)!;
        public static RemoteProcess<Vector2> RpcAnchorIcon = new(
            "ShowAnchorNotice",
            (message, _) =>
            {
                if ((message - (Vector2)PlayerControl.LocalPlayer.transform.position).Magnitude < AnchorRange)
                {
                    var arrow = new Arrow(FocusAnchorIcon.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                    arrow.Register(arrow);
                    arrow.TargetPos = message;
                    NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
                }
            }
            );
        //マップレイヤー系の処理、押した処理の処理もここ
        [Local]
        void OnOpenMap(AbstractMapOpenEvent ev)
        {
            if (!MeetingHud.Instance && ev is MapOpenNormalEvent && isAnchorMapOpen)
            {
                if (!mapLayer)
                {
                    mapLayer = UnityHelper.CreateObject<AnchorMapLayer>("MapLayer", MapBehaviour.Instance.transform, new Vector3(0, 0, -1f));

                    Marks.ForEach(m => mapLayer.AddMark(m, () =>
                    {
                        ShowMapClickArrow(m.Position);
                    }));

                    this.BindGameObject(mapLayer.gameObject);
                }

                mapLayer.gameObject.SetActive(true);
            }
            else
            {
                if (mapLayer) mapLayer.gameObject.SetActive(false);
            }
        }
        [Local]
        void OnCloseMap(MapCloseEvent ev)
        {
            isAnchorMapOpen = false;
            if (mapLayer) mapLayer.gameObject.SetActive(false);
        }

        private void ShowMapClickArrow(Vector2 pos)
        {
            RpcAnchorIcon.Invoke(pos);
            NoticeCount--;
            if (mapButton != null)
            {
                mapButton.UpdateUsesIcon(NoticeCount.ToString());
                mapButton.StartCoolDown();
            }
        }

        public override void OnActivated()
        {

        }

        void Set(PlayerRoleSetEvent ev)
        {
            PlaceCount = NumOfAnchor;
            NoticeCount = NumOfFakeNotice;
            TurnPlace = NumOfAnchorPerTurn;
        }
    }
}
