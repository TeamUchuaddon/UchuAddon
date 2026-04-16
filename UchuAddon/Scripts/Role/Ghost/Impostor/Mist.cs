using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Ghost.Neutral;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
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
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Ghost.Neutral.Grudge;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;
using Virial;
using Sentry.Internal;
using Nebula.Game.Statistics;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using static UnityEngine.GridBrushBase;
using Virial.Events.Game;

namespace Hori.Scripts.Role.Ghost.Impostor;

[NebulaRPCHolder]
public class MistU : DefinedGhostRoleTemplate, DefinedGhostRole
{
    public MistU() : base("mistU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, [NightCooldown, NightDuration, NumOfNight, NightSize, inNightLightSize]) { }

    string ICodeName.CodeName => "MST";

    static private FloatConfiguration NightCooldown = NebulaAPI.Configurations.Configuration("options.role.mistU.nightCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfNight = NebulaAPI.Configurations.Configuration("options.role.mistU.numOfNight", (1, 99), 1);
    static private readonly FloatConfiguration NightSize = NebulaAPI.Configurations.Configuration("options.role.mistU.nightSize", (1f, 10f, 0.25f), 2f, FloatConfigurationDecorator.Ratio);
    static private readonly FloatConfiguration NightDuration = NebulaAPI.Configurations.Configuration("options.role.mistU.nightDuration", (5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration inNightLightSize = NebulaAPI.Configurations.Configuration("options.role.mistU.inNightLightSize", (0.25f, 5f, 0.25f), 0.5f, FloatConfigurationDecorator.Ratio);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Mist.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public readonly MistU MyRole = new();
    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static internal readonly GameStatsEntry StatsNight = NebulaAPI.CreateStatsEntry("stats.mistU.night", GameStatsCategory.Roles, MyRole);

    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        static private readonly Virial.Media.Image MistImage = NebulaAPI.AddonAsset.GetResource("MistButton.png")!.AsImage(115f)!;
        int leftNight = NumOfNight;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var nightButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    NightCooldown, NightDuration, "mistU.night", MistImage, null, _ => leftNight > 0);
                nightButton.Visibility = (button) => MyPlayer.IsDead && leftNight > 0;
                nightButton.OnClick = (button) => button.StartEffect();
                nightButton.OnEffectStart = (button) =>
                {
                    var pos = MyPlayer.Position;
                    RpcNight.Invoke((MyPlayer, new Vector2[] { pos }));
                    StatsNight.Progress();
                };
                nightButton.OnEffectEnd = (button) =>
                {
                    leftNight--;
                    nightButton.UpdateUsesIcon(leftNight.ToString());
                    nightButton.StartCoolDown();
                };
                nightButton.ShowUsesIcon(0, leftNight.ToString());
            }
        }

        static private Image[] shadowSprites = Helpers.Sequential(5).Select(i => SpriteLoader.FromResource($"Nebula.Resources.Shadow.Smoke{i}.png", 100f)).ToArray();
        static private Image lightSprite = SpriteLoader.FromResource($"Nebula.Resources.LightSharpMask.png", 100f);
        static private void GenerateShadow(Vector2 pos, Func<bool> asSurviver)
        {
            var shadow = AmongUsUtil.GenerateCustomShadow(pos, shadowSprites[0].GetSprite());
            shadow.Predicate = asSurviver;
            shadow.MulCoeffForGhosts = 0.9f;
            shadow.MulCoeffForSurvivers = 0.45f;
            shadow.SetAlpha(1f);
            shadow.SetBlend(0f);

            float baseScale = 1.5f * NightSize;
            shadow.gameObject.transform.localScale = new Vector3(baseScale, baseScale, 1f);

            bool isDisappearing = false;

            IEnumerator CoChangeSpriteAndScaling()
            {
                for (int i = 0; i < 4; i++)
                {
                    yield return Effects.Wait(0.125f);
                    shadow.SetSprite(shadowSprites[i + 1].GetSprite());
                }
                float t = 0f;
                float additional = 0f;
                while (shadow)
                {
                    float p = baseScale + (float)Math.Sin(t * 7.7f) * 0.03f;
                    if (isDisappearing) additional += Time.deltaTime / 0.8f;
                    shadow.transform.localScale = new(p + additional, p + additional, 1f);
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            IEnumerator CoUpdate()
            {
                float t = 0f;
                float tGoal = 0.125f * 2.4f;
                while (t < tGoal)
                {
                    t += Time.deltaTime;
                    shadow.SetBlend(t / tGoal * 0.4f);
                    yield return null;
                }

                float p = 0.4f;
                while (p < 1f)
                {
                    shadow.SetBlend(p);
                    p += Time.deltaTime / 0.28f;
                    yield return null;
                }
                shadow.SetBlend(1f);

                while (!isDisappearing) yield return null;

                p = 1f;
                while (p > 0f)
                {
                    shadow.SetAlpha(p);
                    p -= Time.deltaTime / 0.9f;
                    yield return null;
                }
                GameObject.Destroy(shadow.gameObject);
            }
            NebulaManager.Instance.StartCoroutine(CoChangeSpriteAndScaling().WrapToIl2Cpp());
            NebulaManager.Instance.StartCoroutine(CoUpdate().WrapToIl2Cpp());
            NebulaManager.Instance.StartDelayAction(NightDuration, () => isDisappearing = true);
        }

        private class InShadowLight : IGameOperator, ILifespan
        {
            public bool IsDisappearing = false;
            private float Alpha = 0f;
            private float ZeroKeep = 1.2f;
            public bool IsDeadObject => IsDisappearing && !(Alpha > 0f);
            private Vector2[] Positions;
            private SpriteRenderer Light;
            public InShadowLight(Vector2[] positions)
            {
                this.Positions = positions;
                this.Light = AmongUsUtil.GenerateCustomLight(GamePlayer.LocalPlayer!.Position, lightSprite.GetSprite());
                this.Light.transform.SetParent(GamePlayer.LocalPlayer!.VanillaPlayer.transform);
                this.Light.transform.SetWorldZ(-11f);
                Light.material.color = new UnityEngine.Color(1f, 1f, 1f, 0f);
            }

            void OnUpdate(GameHudUpdateEvent ev)
            {
                if (ZeroKeep > 0f)
                {
                    ZeroKeep -= Time.deltaTime;
                    return;
                }

                var playerPos = GamePlayer.LocalPlayer!.Position.ToUnityVector();
                var isInNight = Positions.Any(p => p.Distance(playerPos) < NightSize * 1.54f);

                if (IsDisappearing) Alpha -= Time.deltaTime * 1.6f;
                else if (!isInNight) Alpha -= Time.deltaTime * 3.4f;
                else Alpha += Time.deltaTime * 0.8f;
                Alpha = Math.Clamp(Alpha, 0f, 1f);

                Light.material.color = new UnityEngine.Color(1f, 1f, 1f, Alpha * inNightLightSize);
                float scale = (0.4f + Alpha * 0.6f) * 1.96f * 0.5f;
                Light.transform.localScale = new(scale, scale, 1f);
            }

            void IGameOperator.OnReleased()
            {
                if (Light) GameObject.Destroy(Light.gameObject);
            }
        }

        static private RemoteProcess<(GamePlayer nightmare, Vector2[] pos)> RpcNight = new("MistNightUchu", (message, calledByMe) =>
        {
            message.pos.Do(p =>
            {
                GenerateShadow(p, () => message.nightmare.CanKill(GamePlayer.LocalPlayer!));
            });

            var pos = message.pos;

            var inShdowLight = new InShadowLight(pos).RegisterSelf();
            NebulaManager.Instance.StartDelayAction(NightDuration, () => inShdowLight.IsDisappearing = true);
        });
    }
}