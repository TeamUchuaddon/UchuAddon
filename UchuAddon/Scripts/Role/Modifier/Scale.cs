using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
namespace Hori.Scripts.Role.Modifier;

public class ScaleU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private ScaleU() : base("scaleU", "SCL", new(115, 107, 107), [ScaleAction,
     new GroupConfiguration("options.role.scaleU.group.size",[SizeFixed,SizeMax,SizeMin,SizeMeetingRandom,NumOfSelect,SizeSelect1,SizeSelect2,SizeSelect3,SizeSelect4,SizeSelect5,SizeMeetingSelect],GroupConfigurationColor.ToDarkenColor(Color.gray)),])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Scale.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private ValueConfiguration<int> ScaleAction = NebulaAPI.Configurations.Configuration("options.role.scaleU.scaleAction", ["options.role.scaleU.scaleAction.fixed", "options.role.scaleU.scaleAction.random", "options.role.scaleU.scaleAction.select"], 0);
    static private FloatConfiguration SizeFixed = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeFixed", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 0);
    static private FloatConfiguration SizeMax = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeMax", (1f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 1);
    static private FloatConfiguration SizeMin = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeMin", (0.125f, 1f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 1);
    static private BoolConfiguration SizeMeetingRandom = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeMeetingRandom", false, static () => ScaleAction.GetValue() == 1);
    static private IntegerConfiguration NumOfSelect = NebulaAPI.Configurations.Configuration("options.role.scaleU.numOfSelect", (1, 5), 5, static () => ScaleAction.GetValue() == 2);
    static private FloatConfiguration SizeSelect1 = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeSelect", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 2 && 1 <= NumOfSelect);
    static private FloatConfiguration SizeSelect2 = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeSelect", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 2 && 2 <= NumOfSelect);
    static private FloatConfiguration SizeSelect3 = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeSelect", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 2 && 3 <= NumOfSelect);
    static private FloatConfiguration SizeSelect4 = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeSelect", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 2 && 4 <= NumOfSelect);
    static private FloatConfiguration SizeSelect5 = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeSelect", (0.125f, 10f, 0.125f), 1f, FloatConfigurationDecorator.Ratio, static () => ScaleAction.GetValue() == 2 && 5 <= NumOfSelect);
    static private BoolConfiguration SizeMeetingSelect = NebulaAPI.Configurations.Configuration("options.role.scaleU.sizeMeetingSelect", false, static () => ScaleAction.GetValue() == 2);

    static public ScaleU MyRole = new ScaleU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Scale.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        float RandomSize = 1f;
        float SelectSize = 1f;
        static readonly FloatConfiguration[] SizeSelects ={SizeSelect1,SizeSelect2,SizeSelect3,SizeSelect4,SizeSelect5};

        public Instance(GamePlayer player) : base(player)
        {

        }

        void RuntimeAssignable.OnActivated()
        {
            if (ScaleAction.GetValue() == 0)
            {
                MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(SizeFixed, SizeFixed), float.MaxValue, true, 0, "scaleU:size");
            }
            else if (ScaleAction.GetValue() == 1)
            {
                RandomSize = UnityEngine.Random.Range(SizeMin, SizeMax); 
                MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(RandomSize, RandomSize), float.MaxValue, true, 0, "scaleU:size");
            }
            else if (ScaleAction.GetValue() == 2)
            {
                SelectSize = SelectRandomSize();
                MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(SelectSize, SelectSize), float.MaxValue, true, 0, "scaleU:size");
            }
        }

        void IGameOperator.OnReleased()
        {
            MyPlayer.RemoveAttributeByTag("scaleU:size");
        }

        void RandomMeeting(MeetingPreEndEvent ev)
        {
            if (ScaleAction.GetValue() == 1 && SizeMeetingRandom)
            {
                MyPlayer.RemoveAttributeByTag("scaleU:size");
                RandomSize = UnityEngine.Random.Range(SizeMin, SizeMax);
                MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(RandomSize, RandomSize), float.MaxValue, true, 0, "scaleU:size");
            }
            if (ScaleAction.GetValue() == 2 && SizeMeetingSelect)
            {
                MyPlayer.RemoveAttributeByTag("scaleU:size");
                SelectSize = SelectRandomSize();
                MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(SelectSize, SelectSize), float.MaxValue, true, 0, "scaleU:size");
            }
        }

        float SelectRandomSize()
        {
            int count = Mathf.Clamp(NumOfSelect, 1, SizeSelects.Length);
            int index = UnityEngine.Random.Range(0, count);
            return SizeSelects[index];
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}