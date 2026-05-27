using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;
using UnityExplorer.Localization;

namespace UnityExplorer.CacheObject
{
    public class CacheConfigEntry : CacheObjectBase
    {
        public CacheConfigEntry(IConfigElement configElement)
        {
            RefConfigElement = configElement;
            FallbackType = configElement.ElementType;

            string category = Localizer.Get("CATEGORY_" + configElement.Category.ToUpperInvariant(), configElement.Category);
            string badges = $"<color=#9fb8d8>{category}</color>";
            if (configElement.RequiresRestart)
                badges += $"  <color=#d8b26a>{Localizer.Get("BADGE_RESTART", "Restart")}</color>";
            if (configElement.Advanced)
                badges += $"  <color=#b0b0b0>{Localizer.Get("BADGE_ADVANCED", "Advanced")}</color>";

            NameLabelText = $"<color=cyan>{Localizer.Get(configElement.Name, configElement.Name)}</color>  {badges}" +
                $"\r\n<color=grey><i>{Localizer.Get(configElement.Description, configElement.Description)}</i></color>";
            NameLabelTextRaw = string.Empty;

            configElement.OnValueChangedNotify += UpdateValueFromSource;
        }

        public IConfigElement RefConfigElement;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => true;

        public void UpdateValueFromSource()
        {
            SetValueFromSource(RefConfigElement.BoxedValue);

            if (CellView != null)
                SetDataToCell(CellView);
        }

        public override void TrySetUserValue(object value)
        {
            Value = value;
            RefConfigElement.BoxedValue = value;
        }

        protected override bool TryAutoEvaluateIfUnitialized(CacheObjectCell cell) => true;
    }
}
