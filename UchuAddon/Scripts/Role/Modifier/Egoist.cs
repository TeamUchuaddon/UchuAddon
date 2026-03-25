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

public class EgoistU : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    private EgoistU() : base("egoistU", "EGO", new(170, 32, 201), allocateToImpostor: false, allocateToNeutral: false)
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public EgoistU MyRole = new EgoistU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Egoist.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfUsMira;

    bool IAssignableDocument.HasWinCondition => true;
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => false;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        static bool IsTask;

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            bool crewmatesLost = ev.GameEnd != NebulaGameEnd.CrewmateWin && ev.GameEnd != UchuGameEnd.CrewmateChickenWin && ev.GameEnd != NebulaGameEnd.NoGame;
            ev.IsWin |= crewmatesLost;
        }

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin || ev.GameEnd == UchuGameEnd.CrewmateChickenWin;

        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => IsTask;

        static private RemoteProcess<bool> RpcShareEgoistTaskUchu = new("RpcShareEgoist_Uchu", (message, _) =>
        {
            IsTask = message;
        });

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            RpcShareEgoistTaskUchu.Invoke(true);
        }

        void IGameOperator.OnReleased()
        {
            RpcShareEgoistTaskUchu.Invoke(false);
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}