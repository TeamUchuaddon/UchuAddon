using Cpp2IL.Core.Extensions;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppInterop.Runtime.Injection;
using System.IO;
using UnityEngine.UIElements;
using Virial.Compat;
using Virial.Events.Game.Minimap;
using Virial.Helpers;
using static Hori.Scripts.Role.Impostor.BoxerU;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Mathn = Virial.Utilities.Mathn;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;
using Nebula.Configuration;

namespace Hori.Scripts.Abilities;

public class VisorAdmin : IGameOperator, ILifespan
{
    bool canMoveWithMapWatching;
    public bool mapActive = false;

    public List<Func<bool>> parents = [];
    public bool IsDeadObject => parents.Count > 0 && parents.All(p => !p.Invoke());
    public void AddParent(Func<bool> lifespan) => parents.Add(lifespan);

    private static VisorAdmin AddAndRegisterAbility(Func<bool> lifespan)
    {
        var ability = new VisorAdmin(VisorU.CanMoveWithMapWatchingOption);
        ability.Register(ability);
        ability.AddParent(lifespan);
        return ability;
    }

    public static VisorAdmin TryAddAndBind(Func<bool> lifespan)
    {
        if (GameOperatorManager.Instance?.AllOperators.Find(op => op is VisorAdmin, out var ability) ?? false)
        {
            var jAbility = (ability as VisorAdmin)!;
            jAbility.AddParent(lifespan);
            return jAbility;
        }
        else
        {
            return AddAndRegisterAbility(lifespan);
        }
    }

    private VisorAdmin(bool canMoveWithMapWatching)
    {
        this.canMoveWithMapWatching = canMoveWithMapWatching;

        var map = MapBehaviour.Instance;
        if (map.AsBoolFast()) GameObject.Destroy(map.gameObject);
    }

    /*
    void OnOpenNormalMap(MapOpenNormalEvent ev)
    {
        if (MeetingHud.Instance.AsBoolFast())
        {
            if (canUseAdminOnMeeting)
            {
                var map = MapBehaviour.Instance;
                var countOverlay = map.countOverlay;
                countOverlay.gameObject.SetActive(true);
                countOverlay.SetModOption(false && (GamePlayer.LocalPlayer?.IsImpostor ?? false), false, false, false);
                countOverlay.SetOptions(true, true);
                ConsoleTimer.MarkAsNonConsoleMinigame();

                map.taskOverlay.Hide();
            }
        }
        else
        {
            OnOpenSabotageMap(null!);
        }
    }
    */

    // Visor用なのでNormalEventにまとめる
    void OnOpenSabotageMap(MapOpenNormalEvent ev)
    {
        if (mapActive)
        {
            var map = MapBehaviour.Instance;
            var countOverlay = map.countOverlay;
            countOverlay.gameObject.SetActive(true);
            countOverlay.SetModOption(false && (GamePlayer.LocalPlayer?.IsImpostor ?? false), false, false, false, true, new(54f / 255f, 83f / 255f, 156f / 255f));
            countOverlay.SetOptions(true, true);
            ConsoleTimer.MarkAsNonConsoleMinigame();

            map.taskOverlay.Hide();

            map.countOverlayAllowsMovement = canMoveWithMapWatching;
            if (!map.countOverlayAllowsMovement) AmongUsLLImpl.LocalPlayer.NetTransform.Halt();
        }
    }

    void IGameOperator.OnReleased()
    {
        var map = MapBehaviour.Instance;
        if (map.AsBoolFast()) GameObject.Destroy(map.gameObject);
    }

    void OnMapInstantiated(MapInstantiateEvent ev)
    {
        Transform roomNames;
        if (NebulaAPI.AmongUs.MapId == 0) roomNames = MapBehaviour.Instance.transform.FindChild("RoomNames (1)");
        else roomNames = MapBehaviour.Instance.transform.FindChild("RoomNames");

        OptimizeMap(roomNames, MapBehaviour.Instance.countOverlay, MapBehaviour.Instance.infectedOverlay);
    }

    public static void OptimizeMap(Transform roomNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        for (int i = 0; i < infectedOverlay.transform.childCount; i++) infectedOverlay.transform.GetChild(i).transform.localScale *= 0.8f;
        foreach (var c in countOverlay.CountAreas) c.YOffset *= -1f;

        switch (NebulaAPI.AmongUs.MapId)
        {
            case 0:
                OptimizeMapSkeld(roomNames, countOverlay, infectedOverlay);
                break;
            case 1:
                OptimizeMapMira(roomNames, countOverlay, infectedOverlay);
                break;
            case 2:
                OptimizeMapPolus(roomNames, countOverlay, infectedOverlay);
                break;
            case 4:
                OptimizeMapAirship(roomNames, countOverlay, infectedOverlay);
                break;
        }
    }

    private static void OptimizeMapSkeld(Transform roomNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        roomNames.GetChild(13).localPosition += new Vector3(0f, 0.1f, 0f);

        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(0.07f, 0.2f, 0f);

        countOverlay.transform.GetChild(0).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(1).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(2).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(4).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, -0.55f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(12).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(13).localPosition += new Vector3(0f, -0.42f, 0f);
    }

    private static void OptimizeMapMira(Transform roomNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        roomNames.GetChild(2).localPosition += new Vector3(0f, 0.15f, 0f);
        roomNames.GetChild(7).localPosition += new Vector3(0f, 0.15f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.24f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(0.45f, 0.5f, 0f);
        infectedOverlay.transform.GetChild(2).localPosition += new Vector3(-0.1f, 0.43f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0.6f, 0.25f, 0f);

        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.2f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0f, -0.6f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, -0.45f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0.05f, -0.3f, 0f);
    }

    private static void OptimizeMapPolus(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        romeNames.GetChild(1).localPosition += new Vector3(0f, 0.35f, 0f);
        romeNames.GetChild(3).localPosition += new Vector3(0f, 0.35f, 0f);
        romeNames.GetChild(7).localPosition += new Vector3(0f, 0.35f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.4f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(-1f, 0.4f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0.6f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(-0.5f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(5).localPosition += new Vector3(0f, 0.28f, 0f);
        infectedOverlay.transform.GetChild(6).localPosition += new Vector3(0f, 0.4f, 0f);

        countOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.1f, 0f);
        countOverlay.transform.GetChild(1).localPosition += new Vector3(0.55f, -0.9f, 0f);
        countOverlay.transform.GetChild(2).localPosition += new Vector3(0f, -0.1f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.2f, 0f);
        countOverlay.transform.GetChild(4).localPosition += new Vector3(0.0f, 0f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(6).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(7).localPosition += new Vector3(0.0f, 0f, 0f);
        countOverlay.transform.GetChild(8).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, 0.1f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0f, -0.1f, 0f);
    }

    private static void OptimizeMapAirship(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        romeNames.GetChild(0).localPosition += new Vector3(0f, 0.2f, 0f);
        romeNames.GetChild(2).localPosition += new Vector3(0f, 0.2f, 0f);
        romeNames.GetChild(3).localPosition += new Vector3(0f, 0.25f, 0f);
        romeNames.GetChild(8).localPosition += new Vector3(0f, 0.3f, 0f);
        romeNames.GetChild(11).localPosition += new Vector3(0f, 0.1f, 0f);
        romeNames.GetChild(15).localPosition += new Vector3(0f, 0.1f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, -0.15f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(-0.12f, 0.35f, 0f);
        infectedOverlay.transform.GetChild(2).localPosition += new Vector3(0f, 0.15f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0f, 0.15f, 0f);
        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(0.02f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(5).localPosition += new Vector3(0.06f, 0.12f, 0f);
        infectedOverlay.transform.GetChild(6).localPosition += new Vector3(0f, 0.35f, 0f);
        infectedOverlay.transform.GetChild(7).localPosition += new Vector3(0f, 0.25f, 0f);

        countOverlay.transform.GetChild(2).localPosition += new Vector3(-0.2f, -0.4f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0.05f, -0.2f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0.06f, -0.25f, 0f);
        countOverlay.transform.GetChild(6).localPosition += new Vector3(0f, -0.28f, 0f);
        if (GeneralConfigurations.AirshipLoungeDoorOption.Value || GeneralConfigurations.AirshipLoungeStorageDoorOption.Value) countOverlay.transform.GetChild(7).localPosition += new Vector3(0f, 0.55f, 0f);
        if (GeneralConfigurations.AirshipSecurityDeckDoorOption.Value || GeneralConfigurations.AirshipSecurityKitchenDoorOption.Value) countOverlay.transform.GetChild(15).localPosition += new Vector3(0f, -0.47f, 0f);
        countOverlay.transform.GetChild(16).localPosition += new Vector3(0.15f, -0.3f, 0f);
        countOverlay.transform.GetChild(17).localPosition += new Vector3(-0.1f, -0.5f, 0f);
        if (GeneralConfigurations.AirshipVentilationDoorOption.Value) countOverlay.transform.GetChild(18).localPosition += new Vector3(0f, -0.18f, 0f);
    }

    internal void SetMapActive(bool active)
    {
        this.mapActive = active;
    }
}