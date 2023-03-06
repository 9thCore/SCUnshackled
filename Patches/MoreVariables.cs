using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SCUnshackled
{
    public class RuntimeVariableInfo
    {

        EventVariable modifiedVar;
        EventVariable normalVar;
        EventVariable specialVar;
        string diseaseTypeForSpecial;

        public RuntimeVariableInfo(EventVariable normalVariable, EventVariable specialVariable, string diseaseType)
        {
            modifiedVar = normalVariable;
            normalVar = CopyVariable(normalVariable);
            specialVar = specialVariable;
            diseaseTypeForSpecial = diseaseType;
        }

        // fucked up copy but it works well enough
        private EventVariable CopyVariable(EventVariable var)
        {
            EventVariable copy = new EventVariable();

            Type type = copy.GetType();
            FieldInfo[] infos = type.GetFields();

            foreach(FieldInfo info in infos)
                info.SetValue(copy, type.GetField(info.Name).GetValue(var));

            return copy;
        }

        private void SwitchToNormal()
        {
            modifiedVar.variableString = normalVar.variableString;
            modifiedVar.tooltip = normalVar.tooltip;

            // Utils.Print($"Switched {modifiedVar.id} to normal: {modifiedVar.variableString}, {modifiedVar.tooltip}, {modifiedVar.category}");
        }
        private void SwitchToSpecial()
        {
            modifiedVar.variableString = specialVar.variableString;
            modifiedVar.tooltip = specialVar.tooltip;

            // Utils.Print($"Switched {modifiedVar.id} to special: {modifiedVar.variableString}, {modifiedVar.tooltip}, {modifiedVar.category}");
        }

        public void DetermineSwitch(string diseaseType)
        {
            // Utils.Print("Checking " + diseaseTypeForSpecial.ToLower() + " against " + diseaseType.ToLower() );

            if (diseaseTypeForSpecial.ToLower() != diseaseType.ToLower())
            {
                SwitchToNormal();
            }
            else
            {
                SwitchToSpecial();
            }
        }

    }

    public static class VariablePatchHelper
    {

        readonly private static Dictionary<string, string> variableToProperty = new()
        {
            { "ape_lab_state", "ChangeApeLabState" },
            { "ape_colony_state", "ChangeApeColonyState" }
        };

        public static Dictionary<string, RuntimeVariableInfo> runtimeModifiedVariables = new() { };

        public static void Awake()
        {
            if(!PluginConfig.moreVariablePatch.Value) { return; }

            ScenarioCreatorAPI instance = ScenarioCreatorAPI.Instance;
            int canEditConditions = PluginConfig.canEditConditions.Value ? 1 : 0;

            // reset everything done by LoadDefaultData
            instance.sortedEventVariables.Clear();
            instance.variableDataSorted.Clear();
            instance.variableDataCondGlobal.Clear();
            instance.variableDataCondLocal.Clear();
            instance.variableDataExpression.Clear();
            instance.variableDataOutcome.Clear();

            // call it again, after the transpiler has transpiled
            instance.LoadDefaultData(false);

            List<string> keysToReplace = new();
            List<Tuple<EventVariable, string>> specialVariables = new();

            // Vampire plague type is somewhat annoying to deal with, its variables overwrite a few others if their diseaseType is set to ""
            // therefore, they will be removed and their title and tooltip set at runtime! i am a smart

            // as we're going to be removing diseaseType, we need to remove keys with the diseaseType in their name
            foreach (KeyValuePair<string, EventVariable> pair in instance.variableDataSorted)
            {
                string key = pair.Key;
                string diseaseType = pair.Value.diseaseType;

                if (diseaseType.Length > 0 && key.Substring(key.Length - diseaseType.Length).ToLower() == diseaseType.ToLower())
                {
                    keysToReplace.Add(key);
                }
            }

            // remove duplicate keys
            foreach (string key in keysToReplace)
            {
                instance.variableDataSorted.Remove(key);
                instance.variableDataCondGlobal.Remove(key);
                instance.variableDataCondLocal.Remove(key);
                instance.variableDataExpression.Remove(key);
                instance.variableDataOutcome.Remove(key);
            }

            foreach (EventVariable var in instance.sortedEventVariables)
            {
                var.outcome |= (var.condition & canEditConditions); // cant be bothered to do this in the transpiler

                // is this a duplicate variable? remember it, if so
                string checkAgainstID = $"{var.variable}_{var.diseaseType.ToLower()}_{var.file.ToLower()}";
                if(var.id == checkAgainstID)
                {
                    // Utils.Print("Found duplicate variable: " + var.id);
                    specialVariables.Add( new(var, var.diseaseType) );
                }

                var.diseaseType = "";
                
            }

            // have special handling for duplicates
            foreach(Tuple<EventVariable, string> tuple in specialVariables)
            {
                EventVariable normalVar = default(EventVariable);
                EventVariable specialVar = tuple.Item1;
                string diseaseType = tuple.Item2;

                bool found = false;

                // find variable this one is overshadowing
                foreach(EventVariable var2 in instance.sortedEventVariables)
                {
                    if(var2.variable == specialVar.variable && var2 != specialVar)
                    {
                        normalVar = var2;
                        found = true;
                        break;
                    }
                }

                if(!found)
                {
                    Utils.Print("Couldn't find overshadowed variable for " + specialVar.id + ", skipping", ConsoleLevel.Warning);
                    continue;
                }

                instance.sortedEventVariables.Remove(specialVar);

                string key = FixVariableIfNeeded(normalVar);
                if(key == null) { continue; }

                runtimeModifiedVariables[key] = new RuntimeVariableInfo(normalVar, specialVar, diseaseType);

            }

            foreach(EventVariable var in instance.sortedEventVariables)
            {
                if (variableToProperty.ContainsKey(var.variable))
                {
                    FixVariableIfNeeded(var);
                }
            }

            SetVariableLevel();
        }

        private static string FixVariableIfNeeded(EventVariable normalVar)
        {
            ScenarioCreatorAPI instance = ScenarioCreatorAPI.Instance;

            string key = normalVar.CamelCaseID + "/" + normalVar.ReflectionTarget;
            string forceKey = "";

            if (variableToProperty.ContainsKey(normalVar.variable))
            {
                normalVar.variable = variableToProperty[normalVar.variable];
                forceKey = normalVar.CamelCaseID + "/" + normalVar.ReflectionTarget;
            }

            Type type = normalVar.Type;
            if (type == null)
            {
                Utils.Print(key + " is neither a field nor a property, skipping", ConsoleLevel.Warning);
                return null;
            }

            string returnedKey = key;
            if (forceKey.Length > 0)
            {
                instance.variableDataSorted.Remove(key);
                instance.variableDataCondGlobal.Remove(key);
                instance.variableDataCondLocal.Remove(key);
                instance.variableDataExpression.Remove(key);
                instance.variableDataOutcome.Remove(key);

                instance.variableDataSorted[forceKey] = normalVar;
                instance.variableDataCondGlobal[forceKey] = normalVar;
                instance.variableDataCondLocal[forceKey] = normalVar;
                instance.variableDataExpression[forceKey] = normalVar;
                instance.variableDataOutcome[forceKey] = normalVar;

                returnedKey = forceKey;
            }
            else
            {
                instance.variableDataSorted[key] = normalVar;
                ReplaceIfExists(instance.variableDataCondGlobal, key, normalVar);
                ReplaceIfExists(instance.variableDataCondLocal, key, normalVar);
                ReplaceIfExists(instance.variableDataExpression, key, normalVar);
                ReplaceIfExists(instance.variableDataOutcome, key, normalVar);
            }

            return returnedKey;
        }

        private static void ReplaceIfExists(Dictionary<string, EventVariable> dict, string key, EventVariable value)
        {
            if (dict.ContainsKey(key)) { dict[key] = value; }
        }

        private static async void SetVariableLevel()
        {
            VariableSelectOverlay overlay = CUIManager.instance.GetOverlay<VariableSelectOverlay>();
            while(overlay == null)
            {
                overlay = CUIManager.instance.GetOverlay<VariableSelectOverlay>();
                await Task.Delay(100); // lol
            }

            FieldInfo basic = Utils.GetField(overlay, "toggleBasic");
            FieldInfo adv = Utils.GetField(overlay, "toggleAdvanced");
            FieldInfo supAdv = Utils.GetField(overlay, "toggleSuperAdvanced");

            UIToggle basicToggle = basic.GetValue(overlay) as UIToggle;
            UIToggle advToggle = adv.GetValue(overlay) as UIToggle;
            UIToggle supAdvToggle = supAdv.GetValue(overlay) as UIToggle;

            basicToggle.value = PluginConfig.enableBasicVariables.Value;
            advToggle.value = PluginConfig.enableAdvVariables.Value;
            supAdvToggle.value = PluginConfig.enableSupAdvVariables.Value;
        }

    }

    [HarmonyPatch]
    public static class VariablePatches
    {

        // methods used in instructions
        readonly private static MethodInfo stringUpper = typeof(string).GetMethod("ToUpper", new Type[] { });
        readonly private static MethodInfo stringLength = typeof(string).GetMethod("get_Length");
        readonly private static MethodInfo stringRemove = typeof(string).GetMethod("Remove", new Type[] { typeof(int), typeof(int) });
        readonly private static MethodInfo stringConcat = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });
        readonly private static MethodInfo stringSub = typeof(string).GetMethod("Substring", new Type[] { typeof(int), typeof(int) });
        readonly private static MethodInfo getValue = typeof(KeyValuePair<string, EventVariable>).GetMethod("get_Value");

        readonly private static int Pair = 11; // address of the KeyValuePair<string, EventVariable> local variable inside of ScenarioCreatorAPI.LoadDefaultData

        // makes any fields CamelCase instead of lowercase
        private static void FieldCamelCaser(List<CodeInstruction> list, string varName, ILGenerator generator)
        {
            FieldInfo var = typeof(EventVariable).GetField(varName);

            Label label = generator.DefineLabel();
            CodeInstruction labelledNop = new CodeInstruction(OpCodes.Nop);
            labelledNop.labels.Add(label);

            List<CodeInstruction> newCodes = new() {
                new CodeInstruction(OpCodes.Ldloca_S, Pair),            // push KeyValue pair on the stack
                new CodeInstruction(OpCodes.Call, getValue),            // get the value
                new CodeInstruction(OpCodes.Ldfld, var),                // get field
                new CodeInstruction(OpCodes.Callvirt, stringLength),    // get string length
                new CodeInstruction(OpCodes.Ldc_I4_0),                  // push 0 on the stack
                new CodeInstruction(OpCodes.Cgt_Un),                    // compare the two
                new CodeInstruction(OpCodes.Brfalse, label),            // jump to [X] if the field's length is 0
                new CodeInstruction(OpCodes.Ldloca_S, Pair),            // push KeyValue pair on the stack
                new CodeInstruction(OpCodes.Call, getValue),            // get the value
                new CodeInstruction(OpCodes.Ldloca_S, Pair),            // push KeyValue pair on the stack
                new CodeInstruction(OpCodes.Call, getValue),            // get the value
                new CodeInstruction(OpCodes.Ldfld, var),                // get field
                new CodeInstruction(OpCodes.Callvirt, stringUpper),     // make string uppercase
                new CodeInstruction(OpCodes.Ldc_I4_0),                  // push 0 on the stack
                new CodeInstruction(OpCodes.Ldc_I4_1),                  // push 1 on the stack
                new CodeInstruction(OpCodes.Callvirt, stringSub),       // get the first character of the string
                new CodeInstruction(OpCodes.Ldloca_S, Pair),            // push KeyValue pair on the stack
                new CodeInstruction(OpCodes.Call, getValue),            // get the value
                new CodeInstruction(OpCodes.Ldfld, var),                // get field
                new CodeInstruction(OpCodes.Ldc_I4_0),                  // push 0 on the stack
                new CodeInstruction(OpCodes.Ldc_I4_1),                  // push 1 on the stack
                new CodeInstruction(OpCodes.Callvirt, stringRemove),    // remove first character from string
                new CodeInstruction(OpCodes.Call, stringConcat),        // concatenate the strings
                new CodeInstruction(OpCodes.Stfld, var),                // set the field to the result
                labelledNop                                             // [X]
            };

            list.AddRange(newCodes);
        }

        public static void InsertCodes(List<CodeInstruction> codes, int i, ILGenerator generator)
        {
            List<CodeInstruction> newCodes = new() {};

            FieldCamelCaser(newCodes, "file", generator);
            FieldCamelCaser(newCodes, "complexity", generator);

            codes.InsertRange(i, newCodes);

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CDiseaseScreen), nameof(CDiseaseScreen.SetDisease))]
        public static void SetDisease_Patch(Disease diseaseData)
        {
            string newType = diseaseData.diseaseType.ToString();
            foreach(KeyValuePair<string, RuntimeVariableInfo> pair in VariablePatchHelper.runtimeModifiedVariables)
            {
                pair.Value.DetermineSwitch(newType);
            }
            
            // Utils.Print("Switched disease to " + newType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScenarioScreen), nameof(LoadScenarioScreen.OnLoadScenarioClicked))]
        public static void OnLoadScenarioClicked_Patch()
        {
            string diseaseType = ScenarioCreatorAPI.Instance.CurrentDisease.diseaseType.ToString();

            foreach (KeyValuePair<string, RuntimeVariableInfo> pair in VariablePatchHelper.runtimeModifiedVariables)
            {
                pair.Value.DetermineSwitch(diseaseType);
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ScenarioCreatorAPI), nameof(ScenarioCreatorAPI.LoadDefaultData))]
        public static IEnumerable<CodeInstruction> LoadDefaultData_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // simple strategy: look for the if(variable.include == 1) instruction, and add our instructions after

            var codes = new List<CodeInstruction>(instructions);
            if (!PluginConfig.moreVariablePatch.Value) { return codes.AsEnumerable(); }

            for (int i = 0; i < codes.Count; i ++)
            {
                CodeInstruction code = codes[i];

                if (code.opcode == OpCodes.Ldfld)
                {
                    string operand = code.operand.ToString();
                    if (operand.Contains("include")) // found the include check
                    {
                        for(int j = i+1; j < codes.Count; j++)
                        {
                            CodeInstruction code2 = codes[j];
                            if (code2.opcode == OpCodes.Bne_Un) // found a jump instruction, time to add our stuff
                            {

                                InsertCodes(codes, j+1, generator);
                                return codes.AsEnumerable();

                            }
                        }
                    }
                }

            }

            return codes.AsEnumerable();
        }
    }

}