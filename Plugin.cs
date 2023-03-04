using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SCUnshackled
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Base : BaseUnityPlugin
    {
        public const string GUID = "com.9thcore.piscunshackled";
        public const string NAME = "SC Unshackled";
        public const string VERSION = "1.0.0";

        public static readonly Harmony harmony = new(GUID);
        public static ManualLogSource logger;

        public void Awake()
        {
            PluginConfig.Awake();

            logger = Logger;

            harmony.PatchAll();
            VariablePatchHelper.Awake();

            Utils.Print($"Plugin successfully loaded: v{VERSION}");
        }
    }
}
