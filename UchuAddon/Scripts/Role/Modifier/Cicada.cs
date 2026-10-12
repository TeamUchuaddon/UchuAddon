using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Modifier;

public class CicadaU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, DefinedModifier, IAssignableDocument
{
    private CicadaU() : base("cicadaU", "CIC", new(196, 103, 67))
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Cicada.png")!.AsImage(130f);
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    static public CicadaU MyRole = new CicadaU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Cicada.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ButtonImage, "role.cicadaU.ability.cicada");
    }

    static private readonly Virial.Media.Image ButtonImage = NebulaAPI.AddonAsset.GetResource("CicadaButton.png")!.AsImage(115f)!;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool IsStop = false;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var cicadaButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, false, true, Virial.Compat.VirtualKeyInput.None, null, 0f, "cicadaU.Button", ButtonImage);
                cicadaButton.Availability = (button) => MyPlayer.Logic.OnLadder;
                cicadaButton.OnClick = (button) =>
                {
                    if (IsStop)
                    {
                        ResetEffect();
                    }
                    else
                    {
                        MyPlayer.GainSpeedAttribute(0f, float.MaxValue, false, 99999, "cicadaU.effect");
                        IsStop = true;
                    }
                };
            }
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            ResetEffect();
        }

        [OnlyMyPlayer]
        void Dead(PlayerDieOrDisconnectEvent ev)
        {
            ResetEffect();
        }

        void IGameOperator.OnReleased()
        {
            ResetEffect();
        }

        void ResetEffect()
        {
            MyPlayer.RemoveAttributeByTag("cicadaU.effect");
            IsStop = false;
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}