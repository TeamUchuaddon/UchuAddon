using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
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
using UnityEngine.UI;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using Virial.Text;
using static Il2CppSystem.DateTimeParse;
using static Nebula.Roles.Impostor.Cannon;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Impostor;

public class HarvesterU : DefinedSingleAbilityRoleTemplate<HarvesterU.Ability>, DefinedRole,IAssignableDocument
{
    public HarvesterU() : base("harvesterU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [NumOfHarvest, NumOfPerMeeting,HarvestAddKill,NumOfAddHarvest])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Harvester.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public readonly IntegerConfiguration NumOfHarvest = NebulaAPI.Configurations.Configuration("options.role.harvesterU.numOfHarvest", (1, 15), 5);
    static public readonly IntegerConfiguration NumOfPerMeeting = NebulaAPI.Configurations.Configuration("options.role.harvesterU.numOfHarvestPerMeeting", (1, 15), 2);
    static public readonly BoolConfiguration HarvestAddKill = NebulaAPI.Configurations.Configuration("options.role.harvesterU.harvestAddKill", true);
    static public readonly IntegerConfiguration NumOfAddHarvest = NebulaAPI.Configurations.Configuration("options.role.harvesterU.numOfAddHarvest", (1, 15), 2, () => HarvestAddKill );
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Harvester.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public HarvesterU MyRole = new();
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;

    static readonly DividedSpriteLoader MyIcons = DividedSpriteLoader.FromResource("Nebula.Resources.MeetingButtons.png", 100f, 100, 110, true);
    static readonly Image MediumImage = MyIcons.AsLoader(5);

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(MediumImage, "role.harvesterU.ability.medium");

    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        int leftHarvest = NumOfHarvest;
        int leftHarvestPer = NumOfPerMeeting;
        static List<GamePlayer>? MediumTargets = new List<GamePlayer>();

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                string harvesterText = Language.Translate("harvesterU.Text");
                Helpers.TextHudContent("harvesterLeftText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = harvesterText + ": " + leftHarvest.ToString();
                }, true);
            }
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            leftHarvestPer = NumOfPerMeeting;
            var mediumButton = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            mediumButton?.RegisterMeetingAction(new(MediumImage,
                p =>
                {
                    GamePlayer target = p.MyPlayer;
                    if (target != null)
                    {
                        NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                            new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.harvesterU.message.header")),
                            new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(target.ColoredName + "<br>" + target.Role.DisplayColoredName))),
                            MeetingOverlayHolder.IconsSprite[5], MyRole.RoleColor);
                        MediumTargets?.Add(target);
                        leftHarvest--;
                        leftHarvestPer--;
                    }
                }, p => p.MyPlayer.IsDead  && !p.MyPlayer.AmOwner && !MediumTargets!.Contains(p.MyPlayer) && leftHarvest > 0 && leftHarvestPer > 0 && !PlayerControl.LocalPlayer.Data.IsDead
            ));
        }

        [OnlyMyPlayer, Local]
        void Kill(PlayerKillPlayerEvent ev)
        {
            if (HarvestAddKill)
            {
                leftHarvest += NumOfAddHarvest;
            }
        }
    }
}