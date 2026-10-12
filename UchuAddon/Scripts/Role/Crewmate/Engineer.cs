using Hori.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Nebula.Utilities;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class EngineerU : DefinedRoleTemplate, DefinedRole, IAssignableDocument, HasCitation
{
    private EngineerU() : base("engineerU", new(168, 226, 255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [VentConfiguration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    //108, 158, 245 168, 226, 255

    Citation? HasCitation.Citation => Nebula.Roles.Citations.AmongUs;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.VentConfiguration("role.engineer.vent", false, null, -1, (0f, 60f, 2.5f), 15f, (5f, 70f, 2.5f), 10f);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Engineer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private Image VentImage = new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<EngineerRole>()!.Ability.Image);
    static private Image ImpostorVentImage = NebulaAPI.AddonAsset.GetResource("ImpostorVentButton.png")!.AsImage(100f)!;

    static public EngineerU MyRole = new EngineerU();

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                SetEngineerVisual();
            }
        }

        void IGameOperator.OnReleased()
        {
            if (AmOwner)
            {
                ReleaseEngineerVisual();
            }
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            ReleaseEngineerVisual();
        }

        private void SetEngineerVisual()
        {
            var ventButton = HudManager.Instance.ImpostorVentButton;

            ventButton.graphic.sprite = VentImage.GetSprite();
            ventButton.buttonLabelText.SetSharedMaterial(AmongUsLLImpl.HudManagerBridge.UseButtonVanillaSettings[ImageNames.UseButton].FontMaterial);
        }

        private void ReleaseEngineerVisual()
        {
            var ventButton = HudManager.Instance.ImpostorVentButton;

            ventButton.graphic.sprite = ImpostorVentImage.GetSprite();
            ventButton.buttonLabelText.SetSharedMaterial(RoleManager.Instance.GetRole(RoleTypes.Shapeshifter).Ability.FontMaterial);
        }
    }
}