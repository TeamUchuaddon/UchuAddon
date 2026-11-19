using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using Hori.Scripts.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Core;

/// <summary>
/// 全体アナウンスを専用Githubから取得し、ゲーム画面に反映します。
/// </summary>
internal class AnnouncementModule
{
    public AnnouncementModule()
    {
    }

    private const string AnnouncementUrl = "https://raw.githubusercontent.com/TeamUchuaddon/UchuAddon-GameAnnouncement/main/Announcements.json";
    // private const string AnnouncementUrl = "https://raw.githubusercontent.com/Hori-pizza/UchuAddon-Announcement/main/Announcements.json";
    private const float CheckInterval = 60f; // Githubからの受信をチェックするインターバル、1分ごと
    private readonly HashSet<string> receivedAnnouncements = []; // 受信済みのアナウンスを保存
    private float checkTimer = 0f; // チェック間隔を管理
    private bool isChecking = false; // チェック中か

    /// <summary>
    /// アナウンスに内包されてる情報です。
    /// </summary>
    public class ModAnnouncement
    {
        [JsonSerializableField]
        public string id = string.Empty; // アナウンスのId

        [JsonSerializableField]
        public string announceTime = string.Empty; // アナウンスの受信予定時間

        [JsonSerializableField]
        public int availableMinutes = 5; // アナウンスの受信可能時間、デフォ5分

        [JsonSerializableField]
        public string detail = string.Empty; // アナウンスの内容
    }

    public static event Action<string>? OnAnnouncementReceived; // アナウンス受信イベント NebulaのEventの方がいい？
    public static AnnouncementModule Instance { get; } = new(); // Instance

    public IEnumerator CoAnnouncementLoop()
    {
        while (true)
        {
            if (!isChecking && NebulaPlugin.AllowHttpCommunication) yield return CoCheckAnnouncements();

            // UchuDebug.Log("[Announcement] Cheakインターバル開始");

            yield return new WaitForSecondsRealtime(CheckInterval);
        }
    }

    private IEnumerator CoCheckAnnouncements()
    {
        isChecking = true;

        // UchuDebug.Log("[Announcement] GitHubチェック開始");

        try
        {
            string response = null!;

            yield return NebulaWebRequest.CoGet(Helpers.ConvertUrl(AnnouncementUrl), true, r => response = r);

            if (string.IsNullOrEmpty(response)) yield break;

            // UchuDebug.Log($"[Announcement] 受信JSON: {response}");

            string json = response.TrimStart('\uFEFF');

            var announcements = JsonStructure.Deserialize<List<ModAnnouncement>>(json) ?? new();

            // UchuDebug.Log($"[Announcement] JSON受信成功: {announcements.Count}件");

            var now = DateTimeOffset.Now;

            foreach (var announcement in announcements)
            {
                // UchuDebug.Log($"[Announcement] 読み込み: ID={announcement.id}, Time={announcement.announceTime}, Detail={announcement.detail}");

                if (string.IsNullOrWhiteSpace(announcement.id)) continue;
                if (receivedAnnouncements.Contains(announcement.id)) continue;
                if (!DateTimeOffset.TryParse(announcement.announceTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var announceTime)) continue;
                if (now < announceTime || now >= announceTime.AddMinutes(announcement.availableMinutes)) continue;

                receivedAnnouncements.Add(announcement.id);

                // UchuDebug.Log($"[Announcement] アナウンス受信: ID={announcement.id}, Detail={announcement.detail}");

                OnAnnouncementReceived?.Invoke(announcement.detail);
            }
        }
        finally
        {
            isChecking = false;
        }
    }

    private bool isLoopStarted = false;

    /// <summary>
    /// アナウンスの受信処理を開始します。
    /// </summary>
    public void Start(AmongUsClient client)
    {
        if (isLoopStarted) return;
        isLoopStarted = true;

        OnAnnouncementReceived += detail =>
        {
            client.StartCoroutine(CoShowAnnounement(detail).WrapToIl2Cpp());
        };

        client.StartCoroutine(CoAnnouncementLoop().WrapToIl2Cpp());
    }

    private static Transform announcementTransform = null!;

    public static void Initialize()
    {
        if (announcementTransform != null) return;

        var canvasObject = UnityHelper.CreateObject("UchuAnnouncementCanvas", null, Vector3.zero);

        UnityEngine.Object.DontDestroyOnLoad(canvasObject);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;
        canvasObject.AddComponent<CanvasScaler>();

        announcementTransform = canvasObject.transform;
    }

    static private readonly Image BackImage = NebulaAPI.AddonAsset.GetResource("GameAnnouncementBackground.png")!.AsImage(100f)!;
    private IEnumerator CoShowAnnounement(string detail)
    {
        if (announcementTransform == null)
        {
            // UchuDebug.Log("[Announcement] Canvasが存在しないため生成中止");
            yield break;
        }

        // UchuDebug.Log($"[Announcement] 画面生成開始: {detail}");

        var obj = UnityHelper.CreateObject("AnnouncementText", announcementTransform, Vector3.zero);

        var background = UnityHelper.CreateObject<UnityEngine.UI.Image>("AnnouncementBackground", announcementTransform, Vector3.zero);
        background.sprite = BackImage.GetSprite();
        background.preserveAspect = true;
        background.rectTransform.localScale = Vector3.one * 1f;

        // テキストと同じ位置・サイズにする
        var backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.1f, 0.88f);
        backgroundRect.anchorMax = new Vector2(0.9f, 0.98f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.transform.SetAsFirstSibling();

        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = detail;
        text.fontSize = 30f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;

        obj.transform.SetAsLastSibling();

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.88f);
        rect.anchorMax = new Vector2(0.9f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        yield return new WaitForSecondsRealtime(15f);

        UnityEngine.Object.Destroy(obj);
        UnityEngine.Object.Destroy(background.gameObject);
    }
}

// AmongUsClientのAwakeでアナウンスの見張り開始
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
public static class AmongUsClientAwakePatchOnAnnouncementModule
{
    public static bool IsFirstFlag = true;

    public static void Postfix(AmongUsClient __instance)
    {
        if (!IsFirstFlag) return;
        IsFirstFlag = false;

        AnnouncementModule.Initialize();
        AnnouncementModule.Instance.Start(__instance);
    }
}