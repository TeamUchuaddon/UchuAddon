using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hori.Scripts.Abilities;

internal class SabotageFixAbility
{
    static public void SabotageFix()
    {
        switch (AmongUsUtil.CurrentMapId)
        {
            case 0:
            case 3:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(GamePlayer.LocalPlayer!);
                    return;
                }
                break;
            case 1:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                {
                    FixMiraComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(GamePlayer.LocalPlayer!);
                    return;
                }
                break;
            case 2:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Laboratory);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(GamePlayer.LocalPlayer!);
                    return;
                }
                break;
            case 4:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>().IsActive)
                {
                    FixAirshipReactor();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(GamePlayer.LocalPlayer!);
                    return;
                }
                break;
            case 5:
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                {
                    FixMiraComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(GamePlayer.LocalPlayer!);
                    return;
                }
                break;
            default:
                return;
        }
    }

    private static void FixComms()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 0);
    }

    private static void FixMiraComms()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 17);
    }

    private static void FixAirshipReactor()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 17);
    }

    private static void FixReactor(SystemTypes system)
    {
        ShipStatus.Instance.RpcUpdateSystem(system, 16);
    }

    private static void FixOxygen()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 16);
    }
    [NebulaRPC]
    public static void RpcFix(GamePlayer engineer)
    {
        SwitchSystem switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
        switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
    }
}