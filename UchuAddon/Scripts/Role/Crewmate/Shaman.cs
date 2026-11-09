using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Role.Neutral;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
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
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class ShamanU : DefinedSingleAbilityRoleTemplate<ShamanU.Ability>, DefinedRole, IAssignableDocument, HasCitation
{
    public ShamanU() : base("shamanU", new(202, 80, 242), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NecromancyCooldown, NecromancyDuration, NumOfNecromancy, ResultViewAlivePlayers, TaskCompletesRoomReroll, SkillBoost,NumOfBoostTask])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Shaman.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    public static FloatConfiguration NecromancyCooldown = NebulaAPI.Configurations.Configuration("options.role.shamanU.necromancyCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration NecromancyDuration = NebulaAPI.Configurations.Configuration("options.role.shamanU.necromancyDuration", (0.25f, 10f, 0.25f), 3f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfNecromancy = NebulaAPI.Configurations.Configuration("options.role.shamanU.numOfnecromancy", (1, 15), 1);
    public static BoolConfiguration ResultViewAlivePlayers = NebulaAPI.Configurations.Configuration("options.role.shamanU.resultViewAlivePlayers", false);
    public static BoolConfiguration TaskCompletesRoomReroll = NebulaAPI.Configurations.Configuration("options.role.shamanU.taskCompletesRoomReroll", false);
    public static BoolConfiguration SkillBoost = NebulaAPI.Configurations.Configuration("options.role.shamanU.skillBoost", true);
    public static IntegerConfiguration NumOfBoostTask = NebulaAPI.Configurations.Configuration("options.role.shamanU.numOfBoostTask", (1, 12), 8, () => SkillBoost);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    Citation? HasCitation.Citation => Hori.Core.Citations.ExtremeRoles;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Shaman.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public ShamanU MyRole = new();
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.sampleU.sampleSkill", GameStatsCategory.Roles, MyRole);

    static private Image NecromancyImage = NebulaAPI.AddonAsset.GetResource("ShamanNecromancyButton.png")!.AsImage(115f)!;
    static private Image NecromancyBoostImage = NebulaAPI.AddonAsset.GetResource("ShamanNecromancyBoostButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(NecromancyImage, "role.shamanU.ability.necromancy");
        if (SkillBoost) yield return new(NecromancyBoostImage, "role.shamanU.ability.necromancyBoost");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        if(SkillBoost) yield return new("%NUMBOOST%", NumOfBoostTask.GetValue().ToString());
    }

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "shamanU.another1")
        {
            RelatedRole = ShamanU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {      
        private readonly List<SystemTypes> targetRoom = new();
        private SystemTypes randomRoom;
        private Arrow? roomArrow;
        private UnityEngine.Vector2 PlayerPosition;
        bool Stopped = false;
        int leftNecromancy = NumOfNecromancy;
        bool BoostCheack = false;
        ModAbilityButton NecromancyButton = null!;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                randomRoom = GetRandomRoom();

                bool CanUseHere()
                {
                    return
                        MyPlayer.VanillaPlayer.moveable && ShipStatus.Instance.FastRooms[randomRoom].roomArea.OverlapPoint(MyPlayer.TruePosition);
                }

                roomArrow = new Arrow().Register(this);
                roomArrow.IsActive = false;
                roomArrow.SetColor(MyRole.RoleColor);

                GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>((ev) =>
                {
                    if (!CanUseHere())
                    {
                        roomArrow.IsActive = true;
                        roomArrow.TargetPos = ShipStatus.Instance.FastRooms[randomRoom].roomArea.ClosestPoint(MyPlayer.VanillaPlayer.transform.position);
                    }
                }, this);

                NecromancyButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    NecromancyCooldown, NecromancyDuration, "shamanU.necromancy", NecromancyImage, _ => CanUseHere() && leftNecromancy > 0 && Stopped).SetAsUsurpableButton(this);
                NecromancyButton.OnClick = (button) => button.StartEffect();
                NecromancyButton.OnEffectEnd = (button) =>
                {
                    if (!Stopped) return;
                    AmongUsUtil.PlayCustomFlash(ShamanU.MyRole.RoleColor, 0f, 0.25f, 0.45f);
                    NecromancyResult();
                    randomRoom = GetRandomRoom();
                    leftNecromancy--;
                    NecromancyButton.UpdateUsesIcon(leftNecromancy.ToString());
                    NecromancyButton.StartCoolDown();
                };

                NecromancyButton.OnUpdate = (button) =>
                {
                    if (!Stopped　&& NecromancyButton.IsInEffect)
                        NecromancyButton.InterruptEffect();
                };
                NecromancyButton.ShowUsesIcon(3, leftNecromancy.ToString());
            }
        }

        SystemTypes GetRandomRoom()
        {
            targetRoom.Clear();

            foreach (var entry in ShipStatus.Instance.FastRooms)
            {
                if (entry.Key == SystemTypes.Ventilation) continue;
                targetRoom.Add(entry.Key);
            }

            if (targetRoom.Count == 0)
                return SystemTypes.Cafeteria;

            return targetRoom[System.Random.Shared.Next(targetRoom.Count)];
        }


        string? bodyText;

        void NecromancyResult()
        {
            var aliveImpostors = GamePlayer.AllPlayers.Where(p => !p.IsDead && !p.IsTrueCrewmate).ToArray();
            var alives = GamePlayer.AllPlayers.Where(p => !p.IsDead).ToArray();
            int aliveCount = aliveImpostors.Length;
            int players = alives.Length;

            if (GamePlayer.AllPlayers.Where(p => !p.IsDead && p != MyPlayer).All(p => !p.IsTrueCrewmate))
            {
                TitleUnLock("uchuaddon.shamanU.another1"); //自分以外が全員人外なら解放
            }

            string roles = SkillBoost && BoostCheack ? string.Join("<br>", aliveImpostors.Select(p => p.Role.Role.DisplayColoredName).Distinct()) : "";

            bodyText = Language.Translate(!ResultViewAlivePlayers ? "role.shamanU.result" : "role.shamanU.resultAndAlive").Replace("%COUNT%", aliveCount.ToString()).Replace("%ROLES%", roles).Replace("%ALIVES%", players.ToString());

            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay( NebulaAPI.GUI.VerticalHolder( Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left,NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle),new TranslateTextComponent("options.role.shamanU.message.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left,NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(bodyText))),
                MeetingOverlayHolder.IconsSprite[3],MyRole.RoleColor);

            bodyText = null;
        }


        void GameUpdate(GameUpdateEvent ev)
        {
            if (PlayerPosition == default)
                PlayerPosition = MyPlayer.TruePosition;

            if (PlayerPosition == MyPlayer.TruePosition)
            {
                Stopped = true;
            }
            else
            {
                Stopped = false;
                PlayerPosition = MyPlayer.TruePosition;
            }
        }

        [Local]
        void OnMeetingEnd(MeetingPreEndEvent ev)
        {
            randomRoom = GetRandomRoom();
        }

        [OnlyMyPlayer]
        void TaskCompleted(PlayerTaskCompleteLocalEvent ev)
        {
            if (TaskCompletesRoomReroll)
                randomRoom = GetRandomRoom();

            if (!SkillBoost) return;

            if (MyPlayer.Tasks.CurrentCompleted >= NumOfBoostTask)
            {
                BoostCheack = true;
                NecromancyButton.SetImage(NecromancyBoostImage);
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}