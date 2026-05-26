using HarmonyLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UnityExplorer.Localization;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UnityExplorer.UI.Widgets
{
    public class TimeScaleWidget
    {
        public TimeScaleWidget(GameObject parent)
        {
            Instance = this;

            ConstructUI(parent);

            InitPatch();
        }

        static TimeScaleWidget Instance;

        Toggle overrideTimeScaleToggle;
        InputFieldRef timeInput;
        float desiredTime;
        bool settingTimeScale;
        bool pause;
        Slider slider;

        bool pressedPauseHotkey = false;
        float previousDesiredTime;
        bool previousOverride;

        public void Update()
        {
            if (overrideTimeScaleToggle.isOn)
                SetTimeScale(desiredTime);
        }

        public void PauseToggle()
        {
            if (desiredTime == 0 && overrideTimeScaleToggle.isOn && !pause) pause = true;

            pause = !pause;
            desiredTime = pause ? 0f : previousDesiredTime;

            pressedPauseHotkey = true;
            overrideTimeScaleToggle.isOn = pause ? true : previousOverride;
            slider.value = desiredTime;
            pressedPauseHotkey = false;
        }

        public bool IsPaused()
        {
            return pause;
        }

        public void SetTimeScale(float time)
        {
            settingTimeScale = true;
            Time.timeScale = time;
            settingTimeScale = false;
        }

        public void IncreaseTimeScale()
        {
            float newValue = Mathf.Min(desiredTime + 0.1f, slider.maxValue);
            slider.value = newValue;
            if (overrideTimeScaleToggle.isOn)
                SetTimeScale(newValue);
        }

        public void DecreaseTimeScale()
        {
            float newValue = Mathf.Max(desiredTime - 0.1f, slider.minValue);
            slider.value = newValue;
            if (overrideTimeScaleToggle.isOn)
                SetTimeScale(newValue);
        }

        public void ToggleOverride()
        {
            overrideTimeScaleToggle.isOn = !overrideTimeScaleToggle.isOn;
        }

        void OnTimeInputEndEdit(string val)
        {
            if (float.TryParse(val, out float f))
            {
                if (f < slider.minValue)
                {
                    ExplorerCore.LogWarning("Error, new time scale value outside of margins.");
                    timeInput.Text = desiredTime.ToString("0.00");
                    return;
                }

                if (f >= slider.maxValue)
                {
                    slider.value = slider.maxValue;

                    desiredTime = f;
                    pause = false;
                    previousDesiredTime = desiredTime;
                }
                else
                {
                    slider.value = f;
                }

                timeInput.Text = f.ToString("0.00");
            }
        }

        void OnOverrideValueChanged(bool value)
        {
            if (!pressedPauseHotkey)
            {
                previousOverride = overrideTimeScaleToggle.isOn;
                if (pause) pause = false;
            }

            if (value)
            {
                SetTimeScale(desiredTime);
            }
            else
            {
                SetTimeScale(1f);
            }
        }

        void ConstructUI(GameObject parent)
        {
            Text timeLabel = UIFactory.CreateLabel(parent, "TimeLabel", Localizer.Get("LBL_TIME_SCALE", "Time:"), TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 35);

            timeInput = UIFactory.CreateInputField(parent, "TimeInput", "timeScale");
            UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            GameObject sliderObj = UIFactory.CreateSlider(parent, "Slider_time_scale", out slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 75, flexibleWidth: 0);
            slider.value = 1;
            desiredTime = 1;
            previousDesiredTime = 1;

            slider.onValueChanged.AddListener((newTimeScale) =>
            {
                desiredTime = newTimeScale;
                timeInput.Text = desiredTime.ToString("0.00");

                if (!pressedPauseHotkey)
                {
                    pause = false;
                    if (desiredTime != 0) previousDesiredTime = desiredTime;
                }
            });
            slider.m_FillImage.color = Color.clear;
            slider.minValue = 0f;
            slider.maxValue = 2f;

            GameObject overrideTimeScaleObj = UIFactory.CreateToggle(parent, "Override TimeScale", out overrideTimeScaleToggle, out Text overrideTimeScaleText);
            UIFactory.SetLayoutElement(overrideTimeScaleObj, minHeight: 25, flexibleWidth: 0);
            overrideTimeScaleToggle.isOn = false;
            overrideTimeScaleToggle.onValueChanged.AddListener(OnOverrideValueChanged);
            overrideTimeScaleText.text = Localizer.Get("LBL_OVERRIDE_TIMESCALE", "Override");
        }

        static void InitPatch()
        {
            try
            {
                MethodInfo target = typeof(Time).GetProperty("timeScale").GetSetMethod();
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(target,
                    prefix: new(AccessTools.Method(typeof(TimeScaleWidget), nameof(Prefix_Time_set_timeScale))));
            }
            catch { }
        }

        static bool Prefix_Time_set_timeScale()
        {
            return !Instance.overrideTimeScaleToggle.isOn || Instance.settingTimeScale;
        }
    }
}
