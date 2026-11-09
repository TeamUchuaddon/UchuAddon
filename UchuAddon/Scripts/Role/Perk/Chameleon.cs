using Hori.Scripts.Role.Neutral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Perk;

internal class ChameleonU : PerkFunctionalInstance
{
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front3.png")!.AsImage(100f)!;

    static PerkFunctionalDefinition Def = new("chameleonU", PerkFunctionalDefinition.Category.Standard,
        new PerkDefinition("chameleonU", PerkDefinition.GetPerkBackIcon(3), IconImage, Color.green, null), (def, instance) => new ChameleonU(def, instance));

    public ChameleonU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
    }

    float stand = 3f;

    void OnUpdate(GameUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(UnityEngine.Color.white);

        if (!MyPlayer.IsDead && !MeetingHud.Instance && !ExileController.Instance && MyPlayer.VanillaPlayer.MyPhysics.Velocity.magnitude < 0.001f)
        {
            stand -= Time.deltaTime;

            if (stand <= 0)
            {
                MyPlayer.GainAttribute(PlayerAttributes.Invisible, 9999f, false, 100, "chameleonU.perk.invisible");
            }
        }
        else
        {
            stand = 3f;

            MyPlayer.RealPlayer.RemoveAttributeByTag("chameleonU.perk.invisible");
        }

    }
}