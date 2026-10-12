using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class MushroomPandemicU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front7.png")!.AsImage(100f)!;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.mushroomPandemicU.cooldown", (0f, 30f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("mushroomPandemicU", PerkFunctionalDefinition.Category.NoncrewmateOnly,
        new PerkDefinition("mushroomPandemicU", PerkDefinition.GetPerkBackIcon(4), IconImage, Palette.ImpostorRed, null).CooldownText("%CD%", () => Cooldown), (def, instance) => new MushroomPandemicU(def, instance), [CooldownOption]);

    public MushroomPandemicU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    bool used = false;
    private GameTimer cooldownTimer;

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (used) return;
        if (cooldownTimer.IsProgressing) return;

        used = true;
        RpcMushroomPlace.Invoke((MyPlayer.Position, MyPlayer));
        cooldownTimer.Start(Cooldown);
    }

    RemoteProcess<(Vector2 pos, GamePlayer owner)> RpcMushroomPlace = new("RpcMushroomPlace_Uchu", (message, _) =>
    {
        MushroomsSystem.AddMushroom(message.pos, mushroom =>
        {
            if (mushroom == null) return;

            mushroom.enabled = false; //設置後即は踏めない

            if (GamePlayer.LocalPlayer == message.owner) //自分自身目線は半透明に
            {
                mushroom.gameObject.SetActive(true);
                mushroom.GetComponent<SpriteRenderer>().color = new (1f, 1f, 1f, 0.5f);
            }
            else //他人は見えない
            {
                mushroom.gameObject.SetActive(false);
            }

            NebulaManager.Instance.StartCoroutine(CoMushroomDelay(mushroom).WrapToIl2Cpp());
        });
    });

    private static IEnumerator CoMushroomDelay(Mushroom mushroom)
    {
        yield return new WaitForSeconds(5f);

        if (mushroom == null) yield break;

        mushroom.gameObject.SetActive(true);
        mushroom.enabled = true;

        mushroom.GetComponent<SpriteRenderer>().color = UnityEngine.Color.white;
    }
}

public static class MushroomsSystem
{
    public static Dictionary<int, Mushroom> mushRooms { get; private set; } = new();

    public static void ClearAndReloads()
    {
        mushRooms = new();
    }

    public static void AddMushroom(Vector2 position, Action<Mushroom> callback, int id = -1)
    {
        FungleShipStatus fungleShipStatus = ShipStatus.Instance.TryCast<FungleShipStatus>();
        Mushroom mushroom = null;
        if (fungleShipStatus != null)
        {
            mushroom = AddMushroomAsync(fungleShipStatus, position, id);
            callback(mushroom);
        }
        else
            FungleLoader.Load(fungle =>
            {
                mushroom = AddMushroomAsync(fungle, position, id);
                callback(mushroom);
            });
    }

    public static Mushroom AddMushroomAsync(FungleShipStatus ship, Vector2 position, int id = -1)
    {
        Dictionary<int, Mushroom> mushrooms = mushRooms;
        Vector3 position3 = new Vector3(position.x, position.y, 1);
        position3.z = 1;
        Il2CppSystem.Collections.Generic.Dictionary<int, Mushroom> mushroomsShip = null;

        if (NebulaAPI.AmongUs.MapId == 5)
        {
            mushroomsShip = ship.sporeMushrooms;
            mushrooms = null;
        }
        else
        {
            mushroomsShip = null;
        }

        if (id == -1)
        {
            if (mushrooms != null)
                id = mushrooms.Count <= 0 ? 0 : mushrooms.Max(x => x.Key) + 1;
            else
            {
                int maxId = 0;
                foreach (var kvp in mushroomsShip)
                {
                    if (kvp.Key > maxId) maxId = kvp.Key;
                }
                id = maxId + 1;
            }
        }

        // FungleマップのMushroomプレハブを取得
        Mushroom templateMushroom = ship.GetComponentInChildren<Mushroom>();

        if (templateMushroom == null)
        {
            return null;
        }

        Mushroom newmushRoom = UnityEngine.Object.Instantiate(templateMushroom, ShipStatus.Instance.transform);
        newmushRoom.transform.position = position3;
        newmushRoom.id = id;
        newmushRoom.origPosition = position3;

        if (mushrooms != null)
            mushrooms.Add(id, newmushRoom);
        else
            mushroomsShip.Add(id, newmushRoom);

        return newmushRoom;
    }

    public static Mushroom GetMushroomFromId(int id)
    {
        FungleShipStatus fungleShipStatus = ShipStatus.Instance.TryCast<FungleShipStatus>();
        if (fungleShipStatus != null)
            return fungleShipStatus.GetMushroomFromId(id);

        if (mushRooms.TryGetValue(id, out var value))
            return value;

        return null;
    }

    public static void TriggerSporesFromMushroom(int id)
    {
        FungleShipStatus fungleShipStatus = ShipStatus.Instance.TryCast<FungleShipStatus>();
        if (fungleShipStatus != null)
        {
            var mushroom = fungleShipStatus.GetMushroomFromId(id);
            if (mushroom != null)
            {
                mushroom.TriggerSpores();
                return;
            }
        }
        if (mushRooms.TryGetValue(id, out var value))
        {
            value.TriggerSpores();
        }
    }
}

public static class FungleLoader
{
    private static FungleShipStatus fungle;

    public static void Load(Action<FungleShipStatus> onLoaded)
    {
        if (fungle != null)
        {
            onLoaded(fungle);
            return;
        }

        AmongUsClient.Instance.StartCoroutine(LoadCoroutine(onLoaded).WrapToIl2Cpp());
    }

    private static IEnumerator LoadCoroutine(Action<FungleShipStatus> onLoaded)
    {
        while (AmongUsClient.Instance == null)
            yield return null;

        AssetReference ship = AmongUsClient.Instance.ShipPrefabs[(int)MapNames.Fungle];

        if (ship.Asset == null)
        {
            AsyncOperationHandle<GameObject> op = ship.LoadAssetAsync<GameObject>();
            yield return op;

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                yield break;
            }
        }

        GameObject prefab = ship.Asset.Cast<GameObject>();
        fungle = prefab.GetComponent<FungleShipStatus>();

        onLoaded(fungle);
    }

    public static void Unload()
    {
        if (fungle == null) return;

        AmongUsClient.Instance.ShipPrefabs[(int)MapNames.Fungle].ReleaseAsset();
        fungle = null;

        Resources.UnloadUnusedAssets();
    }
}