using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Crewmate;

static public class ModifierSelectWindow
{
    static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.05f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);

    static public MetaScreen OpenModifierSelectWindow(IEnumerable<DefinedModifier>? modifiers, Predicate<DefinedModifier>? predicate, string underText, Action<DefinedModifier> onSelected)
    {
        var window = MetaScreen.GenerateWindow(new(7.6f, 4.2f), HudManager.Instance.transform, new Vector3(0, 0, -50f), true, false);

        MetaWidgetOld widget = new();

        MetaWidgetOld inner = new();

        if (modifiers == null)
        {
            HashSet<DefinedModifier> modifierSet = [];

            foreach (var m in Roles.AllModifiers) modifierSet.Add(m);

            modifiers = modifierSet;
        }

        var ary = modifiers.ToArray();

        inner.Append(ary,m => new CombinedWidgetOld(new MetaWidgetOld.HorizonalMargin(0.1f),
        new MetaWidgetOld.Button(()=>
        {
            onSelected.Invoke(m); 
            window.CloseScreen(); 
        }, ButtonAttribute)
        {
            RawText = m.DisplayColoredName
        }), 4, -1, 0, 0.59f);

        MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
        widget.Append(scroller);

        widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
        window.SetWidget(widget);
        return window;
    }
}

public class InflictU : DefinedSingleAbilityRoleTemplate<InflictU.Ability>, DefinedRole,IAssignableDocument
{
    public InflictU() : base("inflictU", new(200, 255, 0), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam)
    {
    }
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    bool IAssignableDocument.HasTips => true;

    static public InflictU MyRole = new();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private static SpriteLoader?[] buttonImages = [SpriteLoader.FromResource("Nebula.Resources.Buttons.AccelTrapButton.png",115f),SpriteLoader.FromResource("Nebula.Resources.Buttons.DecelTrapButton.png",115f)];
        private static SpriteLoader?[] buttonImages2 = [SpriteLoader.FromResource("Nebula.Resources.Buttons.CommTrapButton.png",115f),SpriteLoader.FromResource("Nebula.Resources.Buttons.KillTrapButton.png",115f)];
        int buttonIndex1 = 0;
        int buttonIndex2 = 0;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                var InflictButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                  0f, "InflictSkill", buttonImages[buttonIndex1], _ => playerTracker.CurrentTarget != null);
                InflictButton.Visibility = (button) => !MyPlayer.IsDead;
                InflictButton.BindSubKey(Virial.Compat.VirtualKeyInput.AidAction, "Inflict.Switch", true);
                InflictButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        switch (buttonIndex1)
                        {
                            case 0: 
                                ModifierSelectWindow.OpenModifierSelectWindow(null, null, "Select a modifier", modifier =>
                                {
                                    if (target.IsDead) return;
                                    target.AddModifier(modifier);
                                });
                                break;

                            case 1: 
                                ModifierSelectWindow.OpenModifierSelectWindow(null, null, "Select a modifier to remove", modifier =>
                                {
                                    if (target.IsDead) return;
                                    target.RemoveModifier(modifier);
                                });
                                break;
                        }
                    }
                };

                InflictButton.OnSubAction = (button) =>
                {
                    buttonIndex1 = (buttonIndex1 + 1) % buttonImages.Length;
                    InflictButton.SetImage(buttonImages[buttonIndex1]!);
                };

                var MyInflictButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility,
                    0f, "InflictSkill2", buttonImages2[buttonIndex2]);
                MyInflictButton.Visibility = (button) => !MyPlayer.IsDead;
                MyInflictButton.BindSubKey(Virial.Compat.VirtualKeyInput.AidAction, "Inflict.Switch2", true);
                MyInflictButton.OnClick = (button) =>
                {
                    switch (buttonIndex2)
                    {
                        case 0:
                            ModifierSelectWindow.OpenModifierSelectWindow(null, null, "Select a modifier", modifier =>
                            {
                                if (MyPlayer.IsDead) return;
                                MyPlayer.AddModifier(modifier);
                            });
                            break;

                        case 1:
                            ModifierSelectWindow.OpenModifierSelectWindow(null, null, "Select a modifier to remove", modifier =>
                            {
                                if (MyPlayer.IsDead) return;
                                MyPlayer.RemoveModifier(modifier);
                            });
                            break;
                    }
                };

                MyInflictButton.OnSubAction = (button) =>
                {
                    buttonIndex2 = (buttonIndex2 + 1) % buttonImages2.Length;
                    MyInflictButton.SetImage(buttonImages2[buttonIndex2]!);
                };
            }
        }
    }
}

/*
# インフリクト
"role.inflictU.name" : "インフリクト"
"role.inflictU.short" : "イ"
"role.inflictU.blurb" : "開発版役職。リリース時必ず削除すること"
"options.role.inflictU.detail" : "モディファイアを選択し付与する。開発専用"
"button.label.InflictSkill" : "他者"
"button.label.InflictSkill2" : "自身"
*/
