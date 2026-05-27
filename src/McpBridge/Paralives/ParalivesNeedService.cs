#if MONO
using System.Globalization;

namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesNeedService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_set_need_value"] = SetNeedValue
        };

        private static object SetNeedValue(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            ulong characterGuid = McpParameters.RequiredUInt64(parameters, "characterGuid");
            ulong needGuid = McpParameters.RequiredUInt64(parameters, "needGuid");
            float value = Convert.ToSingle(McpParameters.RequiredString(parameters, "value"), CultureInfo.InvariantCulture);
            bool force = McpParameters.OptionalBool(parameters, "force", true);
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);

            Type characterManagerType = ReflectionUtility.GetTypeByName("CharacterManager");
            object characterManager = UnityReflectionUtility.GetSingletonInstance(characterManagerType);
            if (characterManager == null)
                throw new McpBridgeException("not_available", "CharacterManager.Instance is not available.");

            MethodInfo getCharacter = characterManagerType.GetMethod("GetCharacterByGUID", ReflectionUtility.FLAGS);
            if (getCharacter == null)
                throw new McpBridgeException("method_not_found", "CharacterManager.GetCharacterByGUID was not found.");

            object character = getCharacter.Invoke(characterManager, new object[] { characterGuid });
            if (character == null)
                throw new McpBridgeException("validation_failed", $"Character {characterGuid} was not found.");

            Type needManagerType = ReflectionUtility.GetTypeByName("NeedManager");
            object needManager = UnityReflectionUtility.GetSingletonInstance(needManagerType);
            if (needManager == null)
                throw new McpBridgeException("not_available", "NeedManager singleton is not available.");

            MethodInfo getNeedValue = needManagerType.GetMethod("GetNeedValue", ReflectionUtility.FLAGS);
            MethodInfo setNeedToValue = needManagerType.GetMethod("SetNeedToValue", ReflectionUtility.FLAGS);
            if (setNeedToValue == null)
                throw new McpBridgeException("method_not_found", "NeedManager.SetNeedToValue was not found.");

            object oldValue = getNeedValue != null ? getNeedValue.Invoke(needManager, new object[] { needGuid, character }) : null;
            Dictionary<string, object> result = new()
            {
                ["operation"] = "set_need_value",
                ["characterGuid"] = characterGuid.ToString(CultureInfo.InvariantCulture),
                ["needGuid"] = needGuid.ToString(CultureInfo.InvariantCulture),
                ["oldValue"] = oldValue?.ToString(),
                ["newValue"] = value.ToString(CultureInfo.InvariantCulture),
                ["force"] = force,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
                return result;
            }

            setNeedToValue.Invoke(needManager, new object[] { needGuid, character, value, force });
            result["applied"] = true;
            return result;
        }
    }
}
#endif
