using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Configuration.GeneralConfigurations;
using Nebula.Behavior;
using Nebula.Map;
using Nebula.Modules.GUIWidget;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using System.Linq;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Modules.HelpScreen;
using Nebula.Patches;

namespace Hori.Scripts.Core;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
public static class CustomPolusConfiguration
{
    static private T MapCustomization<T>(byte mapId, MapOptionType mapOptionType, Virial.Compat.Vector2 pos, T config, int adminIndex = -1) where T : ISharableEntry
    {
        MapCustomizations[mapId].Add(new(config, pos, mapOptionType, adminIndex));
        return config;
    }

    static internal ISharableVariable<bool> PolusVitalOption = MapCustomization(2, MapOptionType.Blueprint, new(30.5f, -6.5f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.relocationVital", false));
    static internal ISharableVariable<bool> PolusTaskRelocationOption = MapCustomization(2, MapOptionType.Blueprint, new(15.16f, -2.20f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.relocationTask", false));
    static internal ISharableVariable<bool> PolusCustomVentOption = MapCustomization(2, MapOptionType.Vent, new(16.63f, -10.57f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.customVent", false));
    static internal ISharableVariable<bool> PolusSpecimensDoorLowerOption = MapCustomization(2, MapOptionType.Door, new(25.68f, -24.35f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.specimensDoorLower", false));
    static internal ISharableVariable<bool> PolusSpecimensDoorUpperOption = MapCustomization(2, MapOptionType.Door, new(37.8f, -9.6f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.specimensDoorUpper", false));

    static internal ISharableVariable<bool> FungleMushroomChaosOption = MapCustomization(5, MapOptionType.Blueprint, new(-2.95f, -1.24f), NebulaAPI.Configurations.SharableVariable("options.map.customization.fungle.mushroomChaos", false));
}

public class PolusExtension
{
    public static void PatchModificationUchu(byte mapId)
    {
        switch (mapId)
        {
            case 2:
                ModifyPolusUchu();
                break;

            case 5:
                ModifyFungleUchu();
                    break;
        }
    }

    private static void ModifyPolusUchu()
    {
        if (CustomPolusConfiguration.PolusVitalOption.CurrentValue)
        {
            var vitalObj = ShipStatus.Instance.FastRooms[SystemTypes.Office].gameObject;
            var vitalPanel = vitalObj.transform.FindChild("panel_vitals");
            vitalPanel.localPosition = new UnityEngine.Vector3(30.277f, -6.654f, 0.9f);
            vitalPanel.position = new UnityEngine.Vector3(30.277f, -6.654f, 0.9f);
            vitalPanel.transform.localScale = new UnityEngine.Vector3(1.1f, 1.1f, 0.9f);
        }

        if (CustomPolusConfiguration.PolusTaskRelocationOption.CurrentValue)
        {
            var navObj = ShipStatus.Instance.FastRooms[SystemTypes.Dropship].gameObject;
            var navPanel = navObj.transform.FindChild("panel_nav");
            navPanel.localPosition = new UnityEngine.Vector3(11.066f, -15.38f, -0.0148f);
            navPanel.position = new UnityEngine.Vector3(11.066f, -15.38f, -0.0148f);

            var wifiObj = ShipStatus.Instance.FastRooms[SystemTypes.Comms].gameObject;
            var walls = wifiObj.transform.FindChild("Walls");
            var wifiPanel = walls.transform.FindChild("panel_wifi");
            wifiPanel.SetParent(ShipStatus.Instance.transform, true);
            wifiPanel.localPosition = new UnityEngine.Vector3(15.98f, 0.077f, 0.9f);
            wifiPanel.position = new UnityEngine.Vector3(15.98f, 0.077f, 0.9f);
        }

        if (CustomPolusConfiguration.PolusCustomVentOption.CurrentValue)
        {
            var scienceBuildingVent = GetVent("ScienceBuildingVent");
            var electricBuildingVent = GetVent("ElectricBuildingVent");
            var electricVent = GetVent("ElectricalVent");
            var storageVent = GetVent("StorageVent");

            if(scienceBuildingVent != null && electricBuildingVent != null && electricVent != null && storageVent != null)
            {
                scienceBuildingVent.Left = null;
                electricBuildingVent.Left = null;

                scienceBuildingVent.Right = storageVent;
                storageVent.Center = scienceBuildingVent;

                electricBuildingVent.Right = electricVent;
                electricVent.Center = electricBuildingVent;
            }
        }

        if (CustomPolusConfiguration.PolusSpecimensDoorLowerOption.CurrentValue)
        {
            var door = GameObject.Find("PolusShip(Clone)")?.transform?.FindChild("LowerDecon")?.gameObject;
            if (door != null)
            {
                var inner = door.transform.FindChild("DeconDoorInner")?.gameObject;
                var outer = door.transform.FindChild("DeconDoorOuter")?.gameObject;

                if (inner == null || outer == null) return;
                inner.SetActive(false);
                outer.SetActive(false);
            }
        }

        if (CustomPolusConfiguration.PolusSpecimensDoorUpperOption.CurrentValue)
        {
            var door = GameObject.Find("PolusShip(Clone)")?.transform?.FindChild("UpperDecon")?.gameObject;
            if (door != null)
            {
                var inner = door.transform.FindChild("DeconDoorInner")?.gameObject;
                var outer = door.transform.FindChild("DeconDoorOuter")?.gameObject;

                if (inner == null || outer == null) return;
                inner.SetActive(false);
                outer.SetActive(false);
            }
        }
    }

    private static void ModifyFungleUchu()
    {
        if (CustomPolusConfiguration.FungleMushroomChaosOption.CurrentValue)
        {
            ShipStatus.Instance.MapPrefab.infectedOverlay.transform.GetChild(6).GetChild(0).gameObject.SetActive(true);
        }
    }

    static private Vent? GetVent(string name)
    {
        return ShipStatus.Instance.AllVents.FirstOrDefault(v => v.name == name);
    }

    private static Vent CreateVent(SystemTypes room, string ventName, UnityEngine.Vector2 position)
    {
        var referenceVent = ShipStatus.Instance.AllVents[0];
        Vent vent = UnityEngine.Object.Instantiate<Vent>(referenceVent, ShipStatus.Instance.FastRooms[room].transform);
        vent.transform.localPosition = new UnityEngine.Vector3(position.x, position.y, -1);
        vent.Left = null;
        vent.Right = null;
        vent.Center = null;
        vent.Id = ShipStatus.Instance.AllVents.Select(x => x.Id).Max() + 1; // Make sure we have a unique id

        var allVentsList = ShipStatus.Instance.AllVents.ToList();
        allVentsList.Add(vent);
        ShipStatus.Instance.AllVents = allVentsList.ToArray();

        vent.gameObject.SetActive(true);
        vent.name = ventName;
        vent.gameObject.name = ventName;

        var console = vent.GetComponent<VentCleaningConsole>();
        if (console)
        {
            console.Room = room;
            console.ConsoleId = ShipStatus.Instance.AllVents.Length;

            var allConsolesList = ShipStatus.Instance.AllConsoles.ToList();
            allConsolesList.Add(console);
            ShipStatus.Instance.AllConsoles = allConsolesList.ToArray();
        }

        return vent;
    }
}

[NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
public class MapCustomSystem : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static MapCustomSystem() => DIManager.Instance.RegisterModule(() => new MapCustomSystem());
    protected override void OnInjected(Virial.Game.Game container) => this.Register(container);
    static public MapCustomSystem Instance { get; private set; }

    private MapCustomSystem()
    {
        Instance = this;
    }

    void OnGameStart(GameStartEvent ev)
    {
        PolusExtension.PatchModificationUchu(AmongUsUtil.CurrentMapId);
    }
}