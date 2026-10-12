using Epic.OnlineServices.Mods;
using MS.Internal.Xml.XPath;
using Nebula.Behavior;
using Nebula.Map;
using Nebula.Modules.GUIWidget;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using static Nebula.Configuration.GeneralConfigurations;
using static Nebula.Modules.HelpScreen;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

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
    //static internal ISharableVariable<bool> PolusLightSpecimenOption = MapCustomization(2, MapOptionType.BlackOut, new(35.59f, -18.66f), NebulaAPI.Configurations.SharableVariable("options.map.customization.polus.lightSpecsimen", false));

    //static internal ISharableVariable<bool> FungleMushroomChaosOption = MapCustomization(5, MapOptionType.Blueprint, new(-2.95f, -1.24f), NebulaAPI.Configurations.SharableVariable("options.map.customization.fungle.mushroomChaos", false));
}

public class PolusExtension
{
    public static void PatchModificationUchu(byte mapId)
    {
        switch (mapId)
        {
            case 1:
                //ModifyMira();
                break;
            case 2:
                ModifyPolusUchu();
                break;
                /*
            case 5:
                ModifyFungleUchu();
                    break;
                */
        }
    }

    /*
    public static void PatchEarlierModificationUchu(byte mapId)
    {
        switch (mapId)
        {
            case 2:
                ModifyEarlierPolusUchu();
                break;
        }
    }
    */

    private static void ModifyPolusUchu()
    {
        if (CustomPolusConfiguration.PolusVitalOption.CurrentValue)
        {
            var vitalObj = ShipStatus.Instance.FastRooms[SystemTypes.Office].gameObject;
            var vitalPanel = vitalObj.transform.FindChild("panel_vitals");
            vitalPanel.localPosition = new UnityEngine.Vector3(30.277f, -6.654f, 0.9f);
            vitalPanel.position = new UnityEngine.Vector3(30.277f, -6.654f, 0.9f);
            vitalPanel.transform.localScale = new UnityEngine.Vector3(1.1f, 1.1f, 0.9f);

            var dvdObj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject;
            var dvdPanel = dvdObj.transform.FindChild("dvdscreen");
            var clonedPanel = UnityEngine.Object.Instantiate(dvdPanel.gameObject, dvdPanel.parent ).transform;
            clonedPanel.name = "dvdscreen_clone";
            clonedPanel.position = new UnityEngine.Vector3(26.6979f, -15.9446f, 0.9999f);
            clonedPanel.transform.localScale = new UnityEngine.Vector3(0.75f, 0.75f, 0.9f);
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

    static private readonly Image entryImage = NebulaAPI.AddonAsset.GetResource("MiraHQCustomEntryWalls.png")!.AsImage(100f)!;
    static private readonly Image cafeteriaImage = NebulaAPI.AddonAsset.GetResource("MiraHQCustomCafeteria.png")!.AsImage(100f)!;

    private static void ModifyMira()
    {
        var ship = ShipStatus.Instance;

        // 画像を変える
        ship.transform.FindChild("Walls").FindChild("EntryWalls").GetComponent<SpriteRenderer>().sprite = entryImage.GetSprite();
        ship.transform.FindChild("Walls").FindChild("CafeteriaWalls").GetComponent<SpriteRenderer>().sprite = cafeteriaImage.GetSprite();

        // O2のパネルを右側に移動させる
        var Locker = ShipStatus.Instance.FastRooms[SystemTypes.LockerRoom].gameObject;

        var O2Panel = Locker.transform.FindChild("NoOxyConsole");
        O2Panel.position = new UnityEngine.Vector3(6.3136f, -0.3f, 1f);

        var LockerVent = Locker.transform.FindChild("LockerVent");
        LockerVent.position = new UnityEngine.Vector3(4.355f, 1.83f, 1f);

        ModifyEnrtyWalls(); // O2端末がある部分の壁
        ModifyMedbay(); // 医務室の壁
        ModifyCafeteria(); // カフェテリアの壁
    }

    private static void ModifyEnrtyWalls()
    {
        var ship = ShipStatus.Instance;

        var room = ship.transform.FindChild("LeftBottomRoom");
        var shadowObject = room.FindChild("Shadows");

        var colliders = room.GetComponents<EdgeCollider2D>();
        var shadows = shadowObject.GetComponents<EdgeCollider2D>();

        var collider = colliders[2];
        var shadow = shadows[3];
        
        var shadowPoints = shadow.points;
        var points = collider.points;

        // 元の影と判定を消す
        GameObject.Destroy(collider);
        GameObject.Destroy(shadow);

        // Colliderを作り直す
        // 右側の壁部分
        var collider1 = room.gameObject.AddComponent<EdgeCollider2D>();
        collider1.points = new[]
        {
            new Vector2(-5.7972f, -4.3827f),
            new Vector2(-5.7972f, -2.5122f),
            points[1],
            points[2],
            points[3],
            points[4],
            points[5],
            new Vector2(-6.072f, -1.6465f),
            new Vector2(-6.072f, -4.3827f)
        };

        // 左壁の部分 空いた分下に伸ばす
        var collider2 = room.gameObject.AddComponent<EdgeCollider2D>();
        collider2.points = new[]
        {
            new Vector2(-7.9548f, -2.5122f),
            points[6],
            points[7],
            points[8]
        };

        // 影も作り直す
        var shadow1 = shadowObject.gameObject.AddComponent<EdgeCollider2D>();
        shadow1.points = new[]
        {
            new Vector2(-8.0569f, -2.4530f),
            shadowPoints[3],
            shadowPoints[4]
        };

        var shadow2 = shadowObject.gameObject.AddComponent<EdgeCollider2D>();
        shadow2.points = new[]
        {
            shadowPoints[0],
            shadowPoints[1],
            new Vector2(-5.9769f, -1.6974f),
            new Vector2(-5.9769f, -4.3280f)
        };
    }

    private static void ModifyMedbay()
    {
        var ship = ShipStatus.Instance;

        var room = ship.transform.FindChild("LeftBottomRoom");
        var shadowObject = room.FindChild("Shadows");

        var colliders = room.GetComponents<EdgeCollider2D>();
        var shadows = shadowObject.GetComponents<EdgeCollider2D>();

        var medbay = colliders[1];
        var shadow = shadows[0];

        var points = medbay.points;
        var shadowPoints = shadow.points;

        GameObject.Destroy(medbay);
        GameObject.Destroy(shadow);

        // 下側 途中で切って右に伸ばす
        var collider1 = room.gameObject.AddComponent<EdgeCollider2D>();
        collider1.points = new[]
        {
            points[0],
            points[1],
            points[2],
            points[3],
            points[4],
            points[5],
            new Vector2(5.3264f, -2.34995f),
            new Vector2(6.2252f, -2.34995f),
        };

        // 上側 
        var collider2 = room.gameObject.AddComponent<EdgeCollider2D>();
        collider2.points = new[]
        {
            new Vector2(6.2252f, -0.98725f),
            new Vector2(5.3264f, -0.98725f),
            points[6],
            points[7],
            points[8],
            points[9],
            points[10],
            points[11],
            points[12],
            points[13],
            points[14],
            points[15],
            points[16],
            points[17],
            points[18],
            points[19],
            points[20],
            points[21],
            points[22],
            points[23],
            points[24],
            points[25],
        };

        var shadow1 = shadowObject.gameObject.AddComponent<EdgeCollider2D>();
        shadow1.points = new[]
        {
            shadowPoints[0],
            shadowPoints[1],
            shadowPoints[2],
            new Vector2(5.3983f, -2.39995f),
            new Vector2(6.2252f, -2.39995f),
        };

        var shadow2 = shadowObject.gameObject.AddComponent<EdgeCollider2D>();
        shadow2.points = new[]
        {
            new Vector2(6.2252f, -0.2881f),
            new Vector2(5.3983f, -0.2881f),
            shadowPoints[3],
            shadowPoints[4],
            shadowPoints[5],
        };
    }

    private static void ModifyCafeteria()
    {
        var ship = ShipStatus.Instance;

        // SystemTypesを使うとpointsが上手く取得できないから直接検索
        var cafe = ship.transform.FindChild("Cafe");
        var shadowObject = cafe.FindChild("Shadows");

        var colliders = cafe.GetComponents<EdgeCollider2D>();
        var shadows = shadowObject.GetComponents<EdgeCollider2D>();

        var collider = colliders[0];
        var shadow = shadows[1];

        var points = collider.points;
        var shadowPoints = shadow.points;

        GameObject.Destroy(collider);
        GameObject.Destroy(shadow);

        var collider1 = cafe.gameObject.AddComponent<EdgeCollider2D>();
        collider1.points = new[]
        {
            points[0],
            points[1],
            points[2],
            points[3],
            points[4],
            points[5],
            points[6],
            points[7],
            points[8],
            points[9],
            points[10],
            points[11],
            points[12],
            points[13],
            points[14],
            points[15]
        };

        var collider2 = cafe.gameObject.AddComponent<EdgeCollider2D>();
        collider2.points = new[]
        {
            points[16],
            points[17],
            points[18],
            points[19],
            points[20],
            points[21]
        };

        var shadow1 = shadowObject.gameObject.AddComponent<EdgeCollider2D>();
        shadow1.points = new[]
        {
            shadowPoints[1],
            shadowPoints[2]
        };
    }

    /*
    private static void ModifyEarlierPolusUchu()
    {
        if (CustomPolusConfiguration.PolusLightSpecimenOption.CurrentValue)
        {
            int lightConsoleId = 0;
            Console SetUpAsLightConsole(Console console)
            {
                console.AllowImpostor = true;
                console.GhostsIgnored = true;
                console.TaskTypes = new TaskTypes[] { TaskTypes.FixLights };
                console.ConsoleId = lightConsoleId++;
                return console;
            }

            var miningConsole = SetUpAsLightConsole(ShipExtension.CreateConsole(SystemTypes.Electrical, "lightSpecimens", fungleLight3Sprite.GetSprite(), new(35.59f, -18.66f)));
            miningConsole.transform.localScale = new UnityEngine.Vector3(-1f, 1f, 1f);
            UnityHelper.CreateObject<BoxCollider2D>("Collider", miningConsole.transform, new(0f, 0f, 0f)).size = new(0.6f, 0.3f);
        }
    }


    private static SpriteLoader fungleLight3Sprite = SpriteLoader.FromResource("Nebula.Resources.FungleLightConsole3.png", 100f);
    */

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

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
public class ShipStatusPatch
{
    static private byte cachedMapId = 0;
    static public void Prefix(ShipStatus __instance)
    {
        byte mapId = 0;
        if (__instance.TryCast<SkeldShipStatus>())
            mapId = 0;
        else if (__instance.TryCast<MiraShipStatus>())
            mapId = 1;
        else if (__instance.TryCast<PolusShipStatus>())
            mapId = 2;
        else if (__instance.TryCast<AirshipStatus>())
            mapId = 4;
        else if (__instance.TryCast<FungleShipStatus>())
            mapId = 5;
        cachedMapId = mapId;
    }

    static public void Postfix(ShipStatus __instance)
    {
        if (__instance.Type == (ShipStatus.MapType)6) return;

        __instance.StartCoroutine(Effects.Sequence(Effects.Wait(0.1f), ManagedEffects.Action(Modify).WrapToIl2Cpp()));
    }


    static private void Modify()
    {
        PolusExtension.PatchModificationUchu(cachedMapId);
    }
}