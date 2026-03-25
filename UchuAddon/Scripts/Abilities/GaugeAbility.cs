using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

internal class SeedGaugeAbility : FlexibleLifespan, IGameOperator
{
    static internal readonly ResourceExpandableSpriteLoader GuageBackgroundSprite = new("Nebula.Resources.SpectreGuageBackground.png", 100f, 10, 10);
    static private readonly MultiImage GaugeSprites = DividedExpandableSpriteLoader.FromResource("Nebula.Resources.VerticalGauge.png", 100f, 18, 5, new(0.5f, 0f), 3, 1);
    static private readonly Image GaugeScaleSprite = SpriteLoader.FromResource("Nebula.Resources.VerticalGaugeScale.png", 100f);
    private readonly Func<float> value;
    private readonly Func<bool> isInProgress;
    private const float GaugeMargin = 0.08f;

    private float lastValue = -1f;
    public float Value => value.Invoke();
    public float Max { get; private set; }

    private const float GaugeWidth = 0.4f;
    private const float GaugeHeight = 1.0f;
    private const float GaugeValueHeight = GaugeHeight - GaugeMargin;
    private const float GaugeValueActualHeight = GaugeHeight - GaugeMargin * 2f;
    public float Threshold { get; private set; }
    private Image LowIconSprite { get; }
    private Image HighIconSprite { get; }

    private static Vector2 RendererLocalPos = new(0.36f, -0.47f);
    private SpriteRenderer IconRenderer, GaugeRenderer, GaugeBaseRenderer;
    private GameObject Scaler;
    private Transform Adjuster;
    private bool isActive = true;
    public void SetActive(bool active)
    {
        this.isActive = active;
    }

    GameObject GaugeObject;
    public SeedGaugeAbility(float max, float threshold, float hue, Image lowIconSprite, Image highIconSprite, Func<float> value, Func<bool> isInProgress)
    {
        this.Threshold = threshold;
        this.LowIconSprite = lowIconSprite;
        this.HighIconSprite = highIconSprite;
        this.value = value;
        this.isInProgress = isInProgress;
        this.Max = max;

        var gauge = HudContent.InstantiateContent("Gauge", true, false, false, true);
        Adjuster = gauge.gameObject.AddAdjuster();
        GaugeObject = gauge.gameObject;
        this.BindGameObject(GaugeObject);

        Scaler = UnityHelper.CreateObject("Scaler", Adjuster, RendererLocalPos);
        Scaler.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var background = UnityHelper.CreateObject<SpriteRenderer>("Background", Adjuster, new(0f, -0.37f, 0.01f));
        background.SetAsExpandableRenderer();
        background.sprite = GuageBackgroundSprite.GetSprite();
        background.size = new(0.8f, 0.2f);

        GaugeBaseRenderer = UnityHelper.CreateObject<SpriteRenderer>("BaseSprite", Scaler.transform, new(0f, 0f, 0f));
        GaugeBaseRenderer.SetAsExpandableRenderer();
        GaugeBaseRenderer.sprite = GaugeSprites.GetSprite(0);
        GaugeBaseRenderer.size = new(GaugeWidth, GaugeHeight);

        var gaugeBackRenderer = UnityHelper.CreateObject<SpriteRenderer>("BackSprite", Scaler.transform, new(0f, 0f, -0.01f));
        gaugeBackRenderer.SetAsExpandableRenderer();
        gaugeBackRenderer.sprite = GaugeSprites.GetSprite(1);
        gaugeBackRenderer.size = new(GaugeWidth, GaugeHeight);
        gaugeBackRenderer.material = new(NebulaAsset.HSVShader);
        gaugeBackRenderer.material.SetFloat("_Hue", 360f - 126f);

        GaugeRenderer = UnityHelper.CreateObject<SpriteRenderer>("FrontSprite", Scaler.transform, new(0f, 0f, -0.02f));
        GaugeRenderer.SetAsExpandableRenderer();
        GaugeRenderer.sprite = GaugeSprites.GetSprite(2);
        GaugeRenderer.size = new(GaugeWidth, GaugeMargin + 0f);
        GaugeRenderer.material = new(NebulaAsset.HSVShader);
        GaugeRenderer.material.SetFloat("_Hue", 360f - 126f);

        IconRenderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", Adjuster, new(-0.2f, -0.05f, -0.03f));
        IconRenderer.sprite = lowIconSprite.GetSprite();

        var scaleRenderer = UnityHelper.CreateObject<SpriteRenderer>("Scale", Scaler.transform, new(0f, GaugeMargin + (threshold / max) * GaugeValueActualHeight, -0.025f));
        scaleRenderer.sprite = GaugeScaleSprite.GetSprite();
    }

    void OnUpdate(GameUpdateEvent ev)
    {
        GaugeObject.SetActive(isActive && !AmongUsUtil.MapIsOpen && !ExileController.Instance);
        if (MeetingHud.Instance)
        {
            Adjuster.localScale = new(0.8f, 0.8f, 1f);
            Adjuster.localPosition = new(-0.3f, -0.2f);
        }
        else
        {
            Adjuster.localScale = Vector3.one;
            Adjuster.localPosition = Vector3.zero;
        }

        float currentVal = Math.Clamp(Value, 0f, Max);
        bool lastLow = lastValue < Threshold;
        bool low = currentVal < Threshold;

        if (Math.Abs(lastValue - currentVal) > 0f)
        {
            if (lastLow != low) IconRenderer.sprite = (low ? LowIconSprite : HighIconSprite).GetSprite();
            GaugeRenderer.size = new(GaugeWidth, GaugeMargin + currentVal / Max * GaugeValueHeight);
            lastValue = currentVal;
        }

        if (isInProgress.Invoke())
        {
            GaugeBaseRenderer.color = Color.white;
            var sin = (float)Helpers.ScaledSin(9f) * 0.015f + 0.015f + 1f;
            Scaler.transform.localScale = new(sin, sin, 1f);
        }
        else
        {
            GaugeBaseRenderer.color = new(0.5f, 0.5f, 0.5f, 1f);
            Scaler.transform.localScale = Vector3.one;
        }
    }
}

internal class TunaGaugeAbility : FlexibleLifespan, IGameOperator
{
    static internal readonly ResourceExpandableSpriteLoader GuageBackgroundSprite = new("Nebula.Resources.SpectreGuageBackground.png", 100f, 10, 10);
    static private readonly MultiImage GaugeSprites = DividedExpandableSpriteLoader.FromResource("Nebula.Resources.VerticalGauge.png", 100f, 18, 5, new(0.5f, 0f), 3, 1);
    private readonly Func<float> value;
    private readonly Func<bool> isInProgress;
    private const float GaugeMargin = 0.08f;

    private float lastValue = -1f;
    public float Value => value.Invoke();
    public float Max { get; private set; }

    private const float GaugeWidth = 0.4f;
    private const float GaugeHeight = 1.0f;
    private const float GaugeValueHeight = GaugeHeight - GaugeMargin;
    private Image LowIconSprite { get; }
    private Image HighIconSprite { get; }

    private static Vector2 RendererLocalPos = new(0.36f, -0.47f);
    private SpriteRenderer IconRenderer, GaugeRenderer, GaugeBaseRenderer;
    private GameObject Scaler;
    private Transform Adjuster;
    private bool isActive = true;
    public void SetActive(bool active)
    {
        this.isActive = active;
    }

    GameObject GaugeObject;
    public TunaGaugeAbility(float max, float hue, Image lowIconSprite, Image highIconSprite, Func<float> value, Func<bool> isInProgress)
    {
        this.LowIconSprite = lowIconSprite;
        this.HighIconSprite = highIconSprite;
        this.value = value;
        this.isInProgress = isInProgress;
        this.Max = max;

        var gauge = HudContent.InstantiateContent("Gauge", true, false, false, true);
        Adjuster = gauge.gameObject.AddAdjuster();
        GaugeObject = gauge.gameObject;
        this.BindGameObject(GaugeObject);

        Scaler = UnityHelper.CreateObject("Scaler", Adjuster, RendererLocalPos);
        Scaler.transform.localScale = new Vector3(0.85f, 0.85f, 1f);


        var background = UnityHelper.CreateObject<SpriteRenderer>("Background", Adjuster, new(0f, -0.37f, 0.01f));
        background.SetAsExpandableRenderer();
        background.sprite = GuageBackgroundSprite.GetSprite();
        background.size = new(0.8f, 0.2f);

        GaugeBaseRenderer = UnityHelper.CreateObject<SpriteRenderer>("BaseSprite", Scaler.transform, new(0f, 0f, 0f));
        GaugeBaseRenderer.SetAsExpandableRenderer();
        GaugeBaseRenderer.sprite = GaugeSprites.GetSprite(0);
        GaugeBaseRenderer.size = new(GaugeWidth, GaugeHeight);

        var gaugeBackRenderer = UnityHelper.CreateObject<SpriteRenderer>("BackSprite", Scaler.transform, new(0f, 0f, -0.01f));
        gaugeBackRenderer.SetAsExpandableRenderer();
        gaugeBackRenderer.sprite = GaugeSprites.GetSprite(1);
        gaugeBackRenderer.size = new(GaugeWidth, GaugeHeight);
        gaugeBackRenderer.material = new(NebulaAsset.HSVShader);
        gaugeBackRenderer.material.SetFloat("_Hue", 360f - 193f);

        GaugeRenderer = UnityHelper.CreateObject<SpriteRenderer>("FrontSprite", Scaler.transform, new(0f, 0f, -0.02f));
        GaugeRenderer.SetAsExpandableRenderer();
        GaugeRenderer.sprite = GaugeSprites.GetSprite(2);
        GaugeRenderer.size = new(GaugeWidth, GaugeMargin + 0f);
        GaugeRenderer.material = new(NebulaAsset.HSVShader);
        GaugeRenderer.material.SetFloat("_Hue", 360f - 193f);

        IconRenderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", Adjuster, new(-0.02f, -0.21f, -0.03f));
        IconRenderer.sprite = lowIconSprite.GetSprite();
    }
    void OnUpdate(GameUpdateEvent ev)
    {
        GaugeObject.SetActive(isActive && !AmongUsUtil.MapIsOpen && !ExileController.Instance);
        if (MeetingHud.Instance)
        {
            Adjuster.localScale = new(0.8f, 0.8f, 1f);
            Adjuster.localPosition = new(-0.3f, -0.2f);
        }
        else
        {
            Adjuster.localScale = Vector3.one;
            Adjuster.localPosition = Vector3.zero;
        }

        float currentVal = Math.Clamp(Value, 0f, Max);

        if (Math.Abs(lastValue - currentVal) > 0f)
        {
            GaugeRenderer.size = new(GaugeWidth,GaugeMargin + currentVal / Max * GaugeValueHeight);
            lastValue = currentVal;
        }
        
        bool progressing = isInProgress.Invoke();
        IconRenderer.sprite = progressing? LowIconSprite.GetSprite(): HighIconSprite.GetSprite();
        if (isInProgress.Invoke())
        {
            GaugeBaseRenderer.color = Color.white;
            var sin = (float)Helpers.ScaledSin(9f) * 0.015f + 0.015f + 1f;
            Scaler.transform.localScale = new(sin, sin, 1f);
        }
        else
        {
            GaugeBaseRenderer.color = new(0.5f, 0.5f, 0.5f, 1f);
            Scaler.transform.localScale = Vector3.one;
        }
    }
}

internal class BaphometGaugeAbility : FlexibleLifespan, IGameOperator
{
    static internal readonly ResourceExpandableSpriteLoader GuageBackgroundSprite = new("Nebula.Resources.SpectreGuageBackground.png", 100f, 10, 10);
    static private readonly MultiImage GaugeSprites = DividedExpandableSpriteLoader.FromResource("Nebula.Resources.VerticalGauge.png", 100f, 18, 5, new(0.5f, 0f), 3, 1);
    private readonly Func<float> value;
    private readonly Func<bool> isInProgress;
    private const float GaugeMargin = 0.08f;

    private float lastValue = -1f;
    public float Value => value.Invoke();
    public float Max { get; private set; }

    private const float GaugeWidth = 0.4f;
    private const float GaugeHeight = 1.0f;
    private const float GaugeValueHeight = GaugeHeight - GaugeMargin;
    private Image LowIconSprite { get; }
    private Image HighIconSprite { get; }

    private static Vector2 RendererLocalPos = new(0.36f, -0.47f);
    private SpriteRenderer IconRenderer, GaugeRenderer, GaugeBaseRenderer;
    private GameObject Scaler;
    private Transform Adjuster;
    private bool isActive = true;
    public void SetActive(bool active)
    {
        this.isActive = active;
    }

    GameObject GaugeObject;
    public BaphometGaugeAbility(float max, float hue, Image lowIconSprite, Image highIconSprite, Func<float> value, Func<bool> isInProgress)
    {
        this.LowIconSprite = lowIconSprite;
        this.HighIconSprite = highIconSprite;
        this.value = value;
        this.isInProgress = isInProgress;
        this.Max = max;

        var gauge = HudContent.InstantiateContent("Gauge", true, false, false, true);
        Adjuster = gauge.gameObject.AddAdjuster();
        GaugeObject = gauge.gameObject;
        this.BindGameObject(GaugeObject);

        Scaler = UnityHelper.CreateObject("Scaler", Adjuster, RendererLocalPos);
        Scaler.transform.localScale = new Vector3(0.85f, 0.85f, 1f);


        var background = UnityHelper.CreateObject<SpriteRenderer>("Background", Adjuster, new(0f, -0.37f, 0.01f));
        background.SetAsExpandableRenderer();
        background.sprite = GuageBackgroundSprite.GetSprite();
        background.size = new(0.8f, 0.2f);

        GaugeBaseRenderer = UnityHelper.CreateObject<SpriteRenderer>("BaseSprite", Scaler.transform, new(0f, 0f, 0f));
        GaugeBaseRenderer.SetAsExpandableRenderer();
        GaugeBaseRenderer.sprite = GaugeSprites.GetSprite(0);
        GaugeBaseRenderer.size = new(GaugeWidth, GaugeHeight);

        var gaugeBackRenderer = UnityHelper.CreateObject<SpriteRenderer>("BackSprite", Scaler.transform, new(0f, 0f, -0.01f));
        gaugeBackRenderer.SetAsExpandableRenderer();
        gaugeBackRenderer.sprite = GaugeSprites.GetSprite(1);
        gaugeBackRenderer.size = new(GaugeWidth, GaugeHeight);
        gaugeBackRenderer.material = new(NebulaAsset.HSVShader);
        gaugeBackRenderer.material.SetFloat("_Hue", 360f - 270f);

        GaugeRenderer = UnityHelper.CreateObject<SpriteRenderer>("FrontSprite", Scaler.transform, new(0f, 0f, -0.02f));
        GaugeRenderer.SetAsExpandableRenderer();
        GaugeRenderer.sprite = GaugeSprites.GetSprite(2);
        GaugeRenderer.size = new(GaugeWidth, GaugeMargin + 0f);
        GaugeRenderer.material = new(NebulaAsset.HSVShader);
        GaugeRenderer.material.SetFloat("_Hue", 360f - 270f);

        IconRenderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", Adjuster, new(-0.02f, -0.21f, -0.03f));
        IconRenderer.sprite = lowIconSprite.GetSprite();
    }
    void OnUpdate(GameUpdateEvent ev)
    {
        GaugeObject.SetActive(isActive && !AmongUsUtil.MapIsOpen && !ExileController.Instance);
        if (MeetingHud.Instance)
        {
            Adjuster.localScale = new(0.8f, 0.8f, 1f);
            Adjuster.localPosition = new(-0.3f, -0.2f);
        }
        else
        {
            Adjuster.localScale = Vector3.one;
            Adjuster.localPosition = Vector3.zero;
        }

        float currentVal = Math.Clamp(Value, 0f, Max);

        if (Math.Abs(lastValue - currentVal) > 0f)
        {
            GaugeRenderer.size = new(GaugeWidth, GaugeMargin + currentVal / Max * GaugeValueHeight);
            lastValue = currentVal;
        }

        bool progressing = isInProgress.Invoke();
        IconRenderer.sprite = progressing ? LowIconSprite.GetSprite() : HighIconSprite.GetSprite();
        if (isInProgress.Invoke())
        {
            GaugeBaseRenderer.color = Color.white;
            var sin = (float)Helpers.ScaledSin(9f) * 0.015f + 0.015f + 1f;
            Scaler.transform.localScale = new(sin, sin, 1f);
        }
        else
        {
            GaugeBaseRenderer.color = new(0.5f, 0.5f, 0.5f, 1f);
            Scaler.transform.localScale = Vector3.one;
        }
    }
}