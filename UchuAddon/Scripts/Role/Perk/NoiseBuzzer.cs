using Nebula.Roles.Crewmate;
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

internal class NoiseBuzzerU : PerkFunctionalInstance
{
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front6.png")!.AsImage(100f)!;


    static PerkFunctionalDefinition Def = new("noiseBuzzerU", PerkFunctionalDefinition.Category.Standard,
        new PerkDefinition("noiseBuzzerU", PerkDefinition.GetPerkBackIcon(4), IconImage, (Scarlet.MyRole as DefinedAssignable).Color.ToUnityColor(), null),(def, instance) => new NoiseBuzzerU(def, instance));

    public NoiseBuzzerU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {

    }

    [OnlyMyPlayer]
    void OnMurdered(PlayerMurderedEvent ev)
    {
        if (!GamePlayer.LocalPlayer!.Role.IgnoreNoisemakerNotification)
        {
            AmongUsUtil.InstantiateNoisemakerArrow(ev.Player.VanillaPlayer.transform.position, true).arrow.SetDuration(Noisemaker.NoiseDurationOption);
        }
    }
}