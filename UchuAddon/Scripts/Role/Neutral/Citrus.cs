using Hori.Core;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Media;
using static Nebula.Modules.Cosmetics.DynamicPalette;
using Color = UnityEngine.Color;


namespace Hori.Scripts.Role.Neutral;

public class CitrusU : DefinedRoleTemplate, DefinedRole
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.citrusU", new(255, 255, 0), TeamRevealType.OnlyMe);

    private CitrusU() : base("citrusU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [InfectionCooldownOption, CanUseVentOption, CanUseStampOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagChaotic);
    }
    static private readonly FloatConfiguration InfectionCooldownOption = NebulaAPI.Configurations.Configuration("options.role.citrusU.infectionCooldown", (5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private readonly BoolConfiguration CanUseVentOption = NebulaAPI.Configurations.Configuration("options.role.citrusU.canUseVent", true);
    static public readonly BoolConfiguration CanUseStampOption = NebulaAPI.Configurations.Configuration("options.role.citrusU.canUseStamp", true);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Citrus.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;


    static public CitrusU MyRole = new CitrusU();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        static private Image InfectionButtonImage = NebulaAPI.AddonAsset.GetResource("CitrusInfectionButton.png")!.AsImage(115f)!;
        static private Image SelfInfectionButonImage = NebulaAPI.AddonAsset.GetResource("CitrusSelfInfectionButton.png")!.AsImage(115f)!;

        DefinedRole RuntimeRole.Role => MyRole;

        bool RuntimeRole.CanUseVent => CanUseVentOption;

        internal List<GamePlayer> infected = new();

        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer, (player) => !infected.Contains(player));
                ModAbilityButton infectionButton = null!, selfInfectionButton = null!;
                infectionButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, playerTracker, VirtualKeyInput.Ability, null, InfectionCooldownOption, "infection", InfectionButtonImage, (player, button) =>
                {
                    RpcInfect.Invoke((MyPlayer, player));
                    player.AddModifier(CitrusStateU.MyRole, [MyPlayer.PlayerId]);
                    button.StartCoolDown();
                    selfInfectionButton.StartCoolDown();
                });
                selfInfectionButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, InfectionCooldownOption, "selfInfection", SelfInfectionButonImage, null, _ => !infected.Contains(MyPlayer));
                selfInfectionButton.OnClick = (button) =>
                {
                    RpcInfect.Invoke((MyPlayer, MyPlayer));
                    MyPlayer.AddModifier(CitrusStateU.MyRole, [MyPlayer.PlayerId]);
                    button.StartCoolDown();
                    infectionButton.StartCoolDown();
                };
            }
        }

        static RemoteProcess<(GamePlayer citrus, GamePlayer target)> RpcInfect = new("CitrusInfect", (message, _) =>
        {
            try
            {
                var instance = (Instance)message.citrus.Role;
                instance.infected.Add(message.target);
            }
            catch (Exception ex)
            {
                Debug.LogError("Infection Error" + ex.Message);
            }
        });

        [OnlyMyPlayer]
        void OnPlayerDieOrDisconnect(PlayerDieOrDisconnectEvent ev)
        {
            foreach (var player in infected)
            {
                if (player.TryGetModifier<CitrusStateU.Instance>(out _))
                {
                    player.RemoveModifierLocal(player.GetModifiers<CitrusStateU.Instance>().First(modifier => modifier.Owner == MyPlayer));
                }
            }

            infected.Clear();
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.All(player => infected.Contains(player) || player.IsDead || player == MyPlayer)) NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.CitrusTeamWin, 1 << MyPlayer.PlayerId);
        }
    }

}

public class CitrusStateU : DefinedModifierTemplate, DefinedModifier
{
    private CitrusStateU() : base("citrusStateU", CitrusU.MyTeam.Color, [], true, () => false)
    {
    }

    static public CitrusStateU MyRole = new CitrusStateU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.Get(0, -1));



    Image? DefinedAssignable.IconImage => CitrusU.IconImage;

    bool DefinedAssignable.ShowOnFreeplayScreen => false;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        bool changed = false;

        bool started = false;

        int random = 0;
        public static readonly (string citrusName, Color mainColor, Color shadowColor, int citrus)[] citrusList = { ("ミカン", new(1f, 0.55f, 0.3f), new(0.75f, 0.4f, 0.2f), 0), ("オレンジ", new(1f, 0.6f, 0.25f), new(0.8f, 0.45f, 0.2f), 0), ("レモン", new(1f, 0.95f, 0.35f), new(0.8f, 0.75f, 0.25f), 1), ("ライム", new(0.65f, 0.9f, 0.3f), new(0.45f, 0.7f, 0.2f), 1), ("グレープフルーツ", new(1f, 0.75f, 0.55f), new(0.85f, 0.6f, 0.45f), 0), ("ユズ", new(1f, 0.85f, 0.35f), new(0.8f, 0.65f, 0.25f), 0), ("ブンタン", new(0.95f, 0.9f, 0.55f), new(0.8f, 0.75f, 0.45f), 0), ("ポンカン", new(1f, 0.6f, 0.3f), new(0.8f, 0.45f, 0.2f), 0), ("イヨカン", new(1f, 0.55f, 0.25f), new(0.75f, 0.4f, 0.2f), 0), ("ハッサク", new(1f, 0.8f, 0.45f), new(0.8f, 0.65f, 0.35f), 0), ("デコポン", new(1f, 0.6f, 0.25f), new(0.8f, 0.45f, 0.2f), 0), ("クレメンティン", new(1f, 0.55f, 0.3f), new(0.8f, 0.4f, 0.2f), 0), ("ブラッド", new(0.75f, 0.2f, 0.25f), new(0.55f, 0.1f, 0.15f), 0), ("ベルガモット", new(0.9f, 0.9f, 0.4f), new(0.7f, 0.7f, 0.3f), 1), ("マイヤー", new(1f, 0.85f, 0.45f), new(0.8f, 0.65f, 0.3f), 1), ("スダチ", new(0.6f, 0.85f, 0.35f), new(0.4f, 0.65f, 0.25f), 1), ("カボス", new(0.65f, 0.8f, 0.35f), new(0.45f, 0.6f, 0.25f), 1), ("シークヮーサー", new(0.55f, 0.85f, 0.3f), new(0.35f, 0.65f, 0.2f), 1), ("ヒュウガナツ", new(1f, 0.9f, 0.4f), new(0.8f, 0.7f, 0.3f), 0), ("ジャバラ", new(1f, 0.75f, 0.35f), new(0.8f, 0.6f, 0.25f), 0), ("タンジェリン", new(1f, 0.55f, 0.3f), new(0.8f, 0.4f, 0.2f), 0), ("マンダリン", new(1f, 0.6f, 0.3f), new(0.8f, 0.45f, 0.2f), 0), ("キーライム", new(0.7f, 0.9f, 0.4f), new(0.5f, 0.7f, 0.3f), 1), ("フィンガーライム", new(0.5f, 0.75f, 0.35f), new(0.35f, 0.55f, 0.25f), 1), ("カラマンシー", new(0.9f, 0.7f, 0.3f), new(0.7f, 0.55f, 0.2f), 1), ("ナツ", new(1f, 0.8f, 0.45f), new(0.8f, 0.65f, 0.35f), 0), ("カンペイ", new(1f, 0.55f, 0.25f), new(0.8f, 0.4f, 0.2f), 0), ("セトカ", new(1f, 0.6f, 0.3f), new(0.8f, 0.45f, 0.2f), 0), ("バンペイユ", new(0.95f, 0.9f, 0.6f), new(0.8f, 0.75f, 0.5f), 0), ("シトロン", new(1f, 0.95f, 0.4f), new(0.8f, 0.75f, 0.3f), 1) };

        public GamePlayer Owner { get; }

        string MyHatId { get; }
        string MyVisorId { get; }
        string MySkinId { get; }
        string MyPetId { get; }
        string MyNameplateId { get; }
        Color MyMainColor { get; }
        Color MyShadowColor { get; }
        Color MyVisorColor { get; }
        string? MyColorName { get; }
        string MyPlayerName { get; }


        static private Sprite OrangeSprite = NebulaAPI.AddonAsset.GetResource("CitrusOrange.png")!.AsImage(120f)!.GetSprite();
        static private Sprite LemonSprite = NebulaAPI.AddonAsset.GetResource("CitrusLemon.png")!.AsImage(120f)!.GetSprite();

        bool RuntimeAssignable.CanBeAwareAssignment => changed;

        public Instance(GamePlayer player, int ownerId) : base(player)
        {
            Owner = GamePlayer.GetPlayer((byte)ownerId)!;

            var outfit = MyPlayer.DefaultOutfit.outfit;

            MyHatId = outfit.HatId;
            MyVisorId = outfit.VisorId;
            MySkinId = outfit.SkinId;
            MyPetId = outfit.PetId;
            MyNameplateId = outfit.NamePlateId;
            MyMainColor = PlayerColors[MyPlayer.PlayerId];
            MyShadowColor = ShadowColors[MyPlayer.PlayerId];
            MyColorName = ColorNameDic[MyPlayer.PlayerId].name;
            MyVisorColor = VisorColors[MyPlayer.PlayerId];
            MyPlayerName = MyPlayer.PlayerName;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner) RpcRandom.Invoke((MyPlayer, Owner, UnityEngine.Random.Range(0, citrusList.Length)));
        }

        RemoteProcess<(GamePlayer myPlayer, GamePlayer owner, int random)> RpcRandom = new("CitrusRandom", (message, _) =>
        {
            message.myPlayer.GetModifiers<Instance>().First(modifier => modifier.Owner == message.owner).random = message.random;
        });

        void OnMeetingAwake(MeetingEvent ev)
        {
            started = false;
            ChangeOutfit();
        }

        void ChangeOutfit()
        {
            changed = true;

            var playerId = MyPlayer.PlayerId;
            if (citrusList[random].citrus == 0)
            {
                MyPlayer.VanillaPlayer.SetHat("noshat_catudon_Citrus_Orange", playerId);
            }
            else
            {
                MyPlayer.VanillaPlayer.SetHat("noshat_catudon_Citrus_Lemon", playerId);
            }
            MyPlayer.VanillaPlayer.SetVisor(CosmeticsLayer.EMPTY_VISOR_ID, playerId);
            MyPlayer.VanillaPlayer.SetSkin(CosmeticsLayer.EMPTY_SKIN_ID, playerId);
            MyPlayer.VanillaPlayer.SetPet(CosmeticsLayer.EMPTY_PET_ID, playerId);
            MyPlayer.VanillaPlayer.SetNamePlate("nosplate_catudon_Citrus_NamePlate");

            MyPlayer.VanillaPlayer.SetName(citrusList[random].citrusName);

            PlayerColors[playerId] = citrusList[random].mainColor;
            ShadowColors[playerId] = citrusList[random].shadowColor;
            VisorColors[playerId] = Palette.VisorColor;
            ColorNameDic[playerId] = (byte.MaxValue, 0, "");

            MyPlayer.VanillaPlayer.SetColor(MyPlayer.VanillaPlayer.PlayerId);

        }

        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (AmOwner) RpcRandom.Invoke((MyPlayer, Owner, UnityEngine.Random.Range(0, citrusList.Length)));

            started = true;
            var player = MeetingHud.Instance.GetPlayer(MyPlayer.PlayerId);
            if (player == null) return;
            player.LevelNumberText.text = "1";
        }

        [OnlyMyPlayer]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            MyPlayer.RemoveModifierLocal(MyPlayer.GetModifiers<Instance>().First(modifier => modifier.Owner == Owner));
        }


        void ChangeReporterSprite(GameUpdateEvent ev)
        {
            if (started) return;
            if (!MeetingHud.Instance) return;
            if (NebulaAPI.CurrentGame!.CurrentMeeting!.InvokedBy != MyPlayer) return;
            if (NebulaAPI.CurrentGame.CurrentMeeting.ReportedDeadBody != null) return;

            var playerParts = HudManager.Instance.KillOverlay.GetComponentInChildren<MeetingCalledAnimation>().playerParts;

            if (playerParts == null) return;

            if (MyPlayer.DefaultOutfit.outfit.HatId == "noshat_catudon_Citrus_Orange") playerParts.GetComponent<SpriteRenderer>().sprite = OrangeSprite;
            else if (MyPlayer.DefaultOutfit.outfit.HatId == "noshat_catudon_Citrus_Lemon") playerParts.GetComponent<SpriteRenderer>().sprite = LemonSprite;
        }

        void IGameOperator.OnReleased()
        {
            ResetPlayerOutfit();
        }

        void OnGameEnd(GameEndEvent ev)
        {
            ResetPlayerOutfit();
        }

        void ResetPlayerOutfit()
        {
            var playerId = MyPlayer.PlayerId;
            var player = MyPlayer.VanillaPlayer;
            player.SetHat(MyHatId, playerId);
            player.SetVisor(MyVisorId, playerId);
            player.SetSkin(MySkinId, playerId);
            player.SetPet(MyPetId, playerId);
            player.SetNamePlate(MyNameplateId);
            player.SetName(MyPlayerName);

            PlayerColors[playerId] = MyMainColor;
            ShadowColors[playerId] = MyShadowColor;
            VisorColors[playerId] = MyVisorColor;
            ColorNameDic[playerId] = (byte.MaxValue, 0, MyColorName);

            player.SetColor(playerId);

        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if ((AmOwner || canSeeAllInfo) && changed) name += MyRole.GetRoleIconTagSmall();
        }

    }
}