using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace SCUnshackled
{
    public class PluginConfig
    {
        static public ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "SCUnshackled.cfg"), true);

        static public ConfigEntry<bool> canEditConditions;
        static public ConfigEntry<bool> moreVariablePatch;
        static public ConfigEntry<bool> fixTechLowercase;

        static public ConfigEntry<bool> enableBasicVariables;
        static public ConfigEntry<bool> enableAdvVariables;
        static public ConfigEntry<bool> enableSupAdvVariables;

        static public void Awake()
        {
            moreVariablePatch = config.Bind(
                "Variables",
                "AddMoreVariables",
                false,
                "If true, more variables that are normally hidden will become available. May lead to unpredictable outcomes if mishandled.");

            canEditConditions = config.Bind(
                "Variables",
                "AllowSettingConditions",
                false,
                "If true, condition-only variables can be altered using outcomes. May lead to unpredictable outcomes, enable at your risk.");

            fixTechLowercase = config.Bind(
                "QOL",
                "FixTechLowercaseBug",
                true,
                "If true, the notorious tech name becoming lowercase bug will be patched out. Could lead to other bugs regarding translation? but haven't had that happen myself");

            enableBasicVariables = config.Bind(
                "QOL",
                "EnableBasicVariablesAtStart",
                true,
                "Whether basic variables should be enabled at the start"
            );
            enableAdvVariables = config.Bind(
                "QOL",
                "EnableAdvancedVariablesAtStart",
                true,
                "Whether advanced variables should be enabled at the start"
            );
            enableSupAdvVariables = config.Bind(
                "QOL",
                "EnableSuperAdvancedVariablesAtStart",
                true,
                "Whether super advanced variables should be enabled at the start"
            );

        }
    }
}