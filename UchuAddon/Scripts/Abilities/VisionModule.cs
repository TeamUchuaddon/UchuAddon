using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Events.Game.Minimap;
using static Hori.Scripts.Role.Crewmate.AdmiralU;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Abilities;

public class VisionModule
{
    // グラトニー用 
    public class GrayVision : FlexibleLifespan, IGameOperator
    {
        // 遷移中かどうか
        bool releasing = false;
        float t = 0f;

        bool gainEffect = false;
        bool removeEffect = false;

        // グレーの視界
        static private VColor grayVisionColor = new(0.522f, 0.145f, 0.145f, 1f);

        public GrayVision()
        {
            ModSingleton<GrayVision>.Instance = this;
        }

        public void MarkReleased()
        {
            if (releasing) return;

            releasing = true;
            t = 0f;
        }

        public void OnReleased() => RemoveEffect();

        private void RemoveEffect()
        {
            PlayerModInfo.RpcRemoveAttrByTag.LocalInvoke(
                (GamePlayer.LocalPlayer!.PlayerId, "gluttony::vision")
            );
        }

        void OnEditCameraEffect(CameraUpdateEvent ev)
        {
            t += Time.deltaTime;

            // 視界効果を付与
            if (!gainEffect)
            {
                PlayerModInfo.RpcAttrModulator.LocalInvoke(
                    (GamePlayer.LocalPlayer!.PlayerId,
                    new FloatModulator(
                        PlayerAttributes.Roughening,
                        2,
                        999999f,
                        false,
                        0,
                        "gluttony::vision",
                        false
                    ),
                    true)
                );

                gainEffect = true;
            }

            if (!releasing)
            {
                // 通常視界 → グレーへフェード
                if (t < 1f)
                {
                    var num = t / 1f;

                    ev.Color = VColor.White * (1f - num) + grayVisionColor * num;
                    ev.UpdateBrightness(1f, true);
                }
                else
                {
                    // グレー視界を維持
                    ev.Color = grayVisionColor;
                }
            }
            else
            {
                // グレー → 通常視界へフェード
                if (t < 1f)
                {
                    var num = t / 1f;

                    ev.Color = grayVisionColor * (1f - num) + VColor.White * num;
                    ev.UpdateBrightness(1f, true);

                    // フェード途中で効果を解除
                    if (num >= 0.5f && !removeEffect)
                    {
                        RemoveEffect();
                        removeEffect = true;
                    }
                }
                else if (!IsDeadObject)
                {
                    // 演出終了
                    this.Release();
                }
            }
        }

        void OnEditEyesight(LightRangeUpdateEvent ev)
        {
            ev.LightRange = 0.4f;
        }

        void OnDied(PlayerDieOrDisconnectEvent ev)
        {
            if (ev.Player == GamePlayer.LocalPlayer)
            {
                MarkReleased();
            }
        }

        /*
        void OnSwalloeRelease(PlayerSwallowReleaseEvent ev)
        {
            if (ev.Player == GamePlayer.LocalPlayer)
            {
                MarkReleased();
            }
        }
        */
    }
}