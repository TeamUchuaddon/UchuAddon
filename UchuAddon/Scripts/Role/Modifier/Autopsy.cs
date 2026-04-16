using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
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
using static Nebula.Roles.Modifier.Bloody;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class AutopsyU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier/*, HasCitation*/
{
    private AutopsyU() : base("autopsyU", "ATP", new(128, 255, 221), [])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private Image buttonSprite = new WrapSpriteLoader(() => HudManager.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton].Image);
    /*Citation? HasCitation.Citation => Hori.Core.Citations.SuperNewRoles;*/
    /*
    SuperNewRoles
    TownOfHostY
    TownOfHostK
    TheOtherRoles
    ExtremeRoles
    TownOfUs
    */
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Autopsy.png")!.AsImage(80f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public AutopsyU MyRole = new AutopsyU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var vitalButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, false, true, Virial.Compat.VirtualKeyInput.None, null, 0f, "vital", buttonSprite, null)
                    .SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = Nebula.Roles.Crewmate.Doctor.OpenSpecialVitalsMinigame();

                    IEnumerator CoUpdateState(VitalsPanel panel, GamePlayer player)
                    {
                        SpriteRenderer renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", panel.transform, new(-0.3f, -0.2278f, -0.5f));
                        PassiveButton button = renderer.gameObject.SetUpButton(true);
                        while (true)
                        {
                            renderer.gameObject.SetActive(panel.IsDead);
                            yield return null;
                        }
                    }
                    vitalsMinigame.vitals.Do(panel =>
                    {
                        panel.StartCoroutine(CoUpdateState(panel, NebulaGameManager.Instance!.GetPlayer(panel.PlayerInfo.PlayerId)!).WrapToIl2Cpp());
                    });
                };
            }
        }
        
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}