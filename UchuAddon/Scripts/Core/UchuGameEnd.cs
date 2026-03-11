using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Neutral;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
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
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Image = Virial.Media.Image;
using UnityColor = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Core;

[NebulaRPCHolder]
[NebulaPreprocess(PreprocessPhase.PostRoles)]
public class UchuGameEnd
{
    public static GameEnd TunaTeamWin = NebulaAPI.Preprocessor!.CreateEnd("tunaU", new Virial.Color(171, 245, 255), 32);
    public static GameEnd AliceWin = NebulaAPI.Preprocessor!.CreateEnd("aliceU", new Virial.Color(255, 255, 15), 32);
    public static GameEnd ObolusWin = NebulaAPI.Preprocessor!.CreateEnd("obolusU", new Virial.Color(189, 135, 26), 35);
    public static GameEnd BaphometWin = NebulaAPI.Preprocessor!.CreateEnd("baphometU", new Virial.Color(72, 40, 153), 61);
    public static GameEnd LoversBreakerUTeamWin = NebulaAPI.Preprocessor!.CreateEnd("loversbreakerU", new Virial.Color(235, 0, 192), 65);
    public static GameEnd AnchorUTeamWin = NebulaAPI.Preprocessor!.CreateEnd("anchorU", new Virial.Color(8, 0, 237), 34);
    public static GameEnd CrewmateChickenWin = NebulaAPI.Preprocessor!.CreateEnd("crewmate", new Virial.Color(255, 255, 255), 127);
    public static GameEnd CaptainWin = NebulaAPI.Preprocessor!.CreateEnd("captainU", new Virial.Color(65, 73, 158), 32);
    public static GameEnd PandoraWin = NebulaAPI.Preprocessor!.CreateEnd("pandoraU", new Virial.Color(133, 9, 9), 32); 
    public static GameEnd EpidemicWin = NebulaAPI.Preprocessor!.CreateEnd("epidemicU", new Virial.Color(68, 255, 0), 33);

    public static ExtraWin AliceExtra = NebulaAPI.Preprocessor!.CreateExtraWin("aliceU", new Virial.Color(255, 255, 15));
    public static ExtraWin ObolusExtra = NebulaAPI.Preprocessor!.CreateExtraWin("obolusU", new Virial.Color(189, 135, 26));
    public static ExtraWin EnchanterExtra = NebulaAPI.Preprocessor!.CreateExtraWin("enchanterU", EnchanterU.MyTeam.Color);

    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        RegisterWinCondTip(NebulaGameEnd.CrewmateWin, () => (Scripts.Role.Crewmate.ChickenU.MyRole as ISpawnable).IsSpawnable, "chickenU");
        RegisterWinCondTip(TunaTeamWin, () => GeneralConfigurations.NeutralSpawnable && (Scripts.Role.Neutral.TunaU.MyRole as ISpawnable).IsSpawnable, "tunaU");
        RegisterWinCondTip(LoversBreakerUTeamWin, () => (Scripts.Role.Neutral.LoversBreakerU.MyRole as ISpawnable).IsSpawnable && Scripts.Role.Neutral.LoversBreakerU.TakeoverWin, "loversbreakerU.takeover");
        RegisterWinCondTip(LoversBreakerUTeamWin, () => (Scripts.Role.Neutral.LoversBreakerU.MyRole as ISpawnable).IsSpawnable && !Scripts.Role.Neutral.LoversBreakerU.TakeoverWin, "loversbreakerU.normal");
        RegisterWinCondTip(AliceWin, () => GeneralConfigurations.NeutralSpawnable && (Scripts.Role.Neutral.AliceU.MyRole as ISpawnable).IsSpawnable && !Scripts.Role.Neutral.AliceU.ExtraWin, "aliceU");
        RegisterWinCondTip(ObolusWin, () => GeneralConfigurations.NeutralSpawnable && (Scripts.Role.Neutral.ObolusU.MyRole as ISpawnable).IsSpawnable && !Scripts.Role.Neutral.ObolusU.ExtraWin, "obolusU");
        RegisterWinCondTip(AnchorUTeamWin, () => GeneralConfigurations.NeutralSpawnable && (Scripts.Role.Neutral.AnchorU.MyRole as ISpawnable).IsSpawnable, "anchorU");
        RegisterWinCondTip(EpidemicWin, () => GeneralConfigurations.NeutralSpawnable && (Scripts.Role.Neutral.EpidemicU.MyRole as ISpawnable).IsSpawnable, "epidemicU");
    }
    private static void RegisterWinCondTip(GameEnd gameEnd, Func<bool> predicate, string name, Func<string, string>? decorator = null)
    {
        NebulaAPI.RegisterTip(new WinConditionTip(gameEnd, predicate, () => Language.Translate("document.tip.winCond." + name + ".title"), () =>
        {
            string text = Language.Translate("document.tip.winCond." + name);
            return decorator?.Invoke(text) ?? text;
        }));
    }
}