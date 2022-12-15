
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class SkyboxSwapper : UdonSharpBehaviour
    {
        public TVManagerV2 tv;
        public Material skybox;
        public Material fallback;
        public Slider brightness;

        [Header("State Indicators")]
        public Color active = new Color(0.4f, 0.4f, 0.4f);
        public Color inactive = new Color(0.25f, 0.25f, 0.25f);
        public Button flipVertical;
        public Button swapEyes;
        public Button panoramicMode;
        public Button cubeMapMode;
        public Button deg180Mode;
        public Button not3DLayout;
        public Button sideBySideLayout;
        public Button overUnderLayout;

        private bool hasBrightness;
        private bool hasFlipVertical;
        private bool hasSwapEyes;
        private bool hasPanoramicMode;
        private bool hasCubeMapMode;
        private bool hasDeg180Mode;
        private bool hasNot3DLayout;
        private bool hasSideBySideLayout;
        private bool hasOverUnderLayout;
        private bool init = false;

        public void _Initialize() {
            if (init) return;

            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (fallback == null) fallback = RenderSettings.skybox;

            hasBrightness = brightness != null;
            hasFlipVertical = flipVertical != null;
            hasSwapEyes = swapEyes != null;
            hasPanoramicMode = panoramicMode != null;
            hasCubeMapMode = cubeMapMode != null;
            hasDeg180Mode = deg180Mode != null;
            hasNot3DLayout = not3DLayout != null;
            hasSideBySideLayout = sideBySideLayout != null;
            hasOverUnderLayout = overUnderLayout != null;

            _Panoramic();
            _Not3D();
            if (hasBrightness) brightness.value = 1f;

            tv._RegisterUdonSharpEventReceiver(this);
            init = true;
        }

        void Start()
        {
            _Initialize();
        }

        void OnDisable() {
            revert();
        }

        // ========== Shader Toggles =========

        public void _Brightness()
        {
            if (hasBrightness) skybox.SetFloat("_Exposure", brightness.value);
        }

        public void _Not3D()
        {
            skybox.SetFloat("_Layout", 0);
            if (hasNot3DLayout) not3DLayout.targetGraphic.color = active;
            if (hasSideBySideLayout) sideBySideLayout.targetGraphic.color = inactive;
            if (hasOverUnderLayout) overUnderLayout.targetGraphic.color = inactive;
        }

        public void _SideBySide()
        {
            skybox.SetFloat("_Layout", 1);
            if (hasNot3DLayout) not3DLayout.targetGraphic.color = inactive;
            if (hasSideBySideLayout) sideBySideLayout.targetGraphic.color = active;
            if (hasOverUnderLayout) overUnderLayout.targetGraphic.color = inactive;
        }

        public void _OverUnder()
        {
            skybox.SetFloat("_Layout", 2);
            if (hasNot3DLayout) not3DLayout.targetGraphic.color = inactive;
            if (hasSideBySideLayout) sideBySideLayout.targetGraphic.color = inactive;
            if (hasOverUnderLayout) overUnderLayout.targetGraphic.color = active;
        }

        public void _Flip()
        {
            float flipTo = (skybox.GetFloat("_Flip") + 1) % 2;
            skybox.SetFloat("_Flip", flipTo);
            if (hasFlipVertical) flipVertical.targetGraphic.color = flipTo == 1 ? inactive : active;
        }

        public void _SwapEyes()
        {
            float swapTo = (skybox.GetFloat("_SwapEyes") + 1) % 2;
            skybox.SetFloat("_SwapEyes", swapTo);
            if (hasSwapEyes) swapEyes.targetGraphic.color = swapTo == 0 ? inactive : active;
        }

        public void _Deg180()
        {
            skybox.SetFloat("_ImageType", 1);
            skybox.SetFloat("_Mapping", 1); // 180 implies panoramic
            if (hasDeg180Mode) deg180Mode.targetGraphic.color = active;
            if (hasCubeMapMode) cubeMapMode.targetGraphic.color = inactive;
            if (hasPanoramicMode) panoramicMode.targetGraphic.color = inactive;
        }

        public void _Panoramic()
        {
            skybox.SetFloat("_Mapping", 1);
            skybox.SetFloat("_ImageType", 0); // Panoramic implies 360
            if (hasDeg180Mode) deg180Mode.targetGraphic.color = inactive;
            if (hasCubeMapMode) cubeMapMode.targetGraphic.color = inactive;
            if (hasPanoramicMode) panoramicMode.targetGraphic.color = active;
        }

        public void _CubeMap()
        {
            skybox.SetFloat("_Mapping", 0);
            skybox.SetFloat("_ImageType", 0); // CubeMap implies 360
            if (hasDeg180Mode) deg180Mode.targetGraphic.color = inactive;
            if (hasCubeMapMode) cubeMapMode.targetGraphic.color = active;
            if (hasPanoramicMode) panoramicMode.targetGraphic.color = inactive;
        }

        // =========== TV Events ==============

        public void _TvPlay() => activate();
        public void _TvMediaStart() => activate();
        public void _TvMediaEnd() => revert();
        public void _TvStop() => revert();
        public void _TvVideoPlayerError() => revert();

        private void activate()
        {
            RenderSettings.skybox = skybox;
            foreach (string option in tv.urlHashParams)
            {
                switch (option.ToLower())
                {
                    case "180": _Deg180(); break;
                    case "panoramic": _Panoramic(); break;
                    case "cubemap": _CubeMap(); break;
                    case "sidebyside": _SideBySide(); break;
                    case "overunder": _OverUnder(); break;
                    case "nolayout": // same as not3d
                    case "standard": // same as not3d
                    case "not3d": _Not3D(); break;
                }
            }
        }

        private void revert()
        {
            RenderSettings.skybox = fallback;
        }
    }
}