using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using FrooxEngine;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace ExposeEditorlessMembers
{
    [ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
    [BepInDependency(BepInExResoniteShim.PluginMetadata.GUID)]
    public class ExposeEditorlessMembers : BasePlugin
	{
		static ManualLogSource log;
		static ConfigEntry<bool> ShowHidden;
		static bool HiddenPatched = false;
		const string HiddenCat = "HiddenToggle";
        public override void Load()
		{
			ShowHidden = Config.Bind(PluginMetadata.NAME, "Ignore HideInInspectorAttribute", true);
			log = Log;
			HarmonyInstance.PatchAllUncategorized();
            ShowHidden.SettingChanged += ShowHidden_SettingChanged;
			ShowHidden_SettingChanged();
        }

        private void ShowHidden_SettingChanged(object? sender = null, EventArgs e = null)
        {
			if (ShowHidden.Value == HiddenPatched) return;
			if (ShowHidden.Value) HarmonyInstance.PatchCategory(HiddenCat);
            else HarmonyInstance.UnpatchCategory(HiddenCat);
			HiddenPatched = ShowHidden.Value;
        }

        [HarmonyPatch(typeof(SyncMemberEditorBuilder), nameof(SyncMemberEditorBuilder.Build))]
		class SyncMemberEditorBuilderPatch
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
			{
				List<CodeInstruction> instructions = new(codes);
				int index = -1;
				object label = null;
				for (int i = 1; i < instructions.Count - 1; i++)
				{
					if (instructions[i].opcode == OpCodes.Isinst && instructions[i].operand == typeof(EmptySyncElement) &&
						instructions[i - 1].opcode == OpCodes.Ldarg_0 && instructions[i + 1].opcode == OpCodes.Stloc_S)
					{
						label = instructions[i + 1].operand;
						index = i - 1;
						break;
					}

				}
				if (label != null)
				{
					instructions.RemoveAt(index);
					instructions.RemoveAt(index);
					instructions.RemoveAt(index);
					for (int i = 0; i < instructions.Count; i++)
					{
						if (instructions[i].opcode == OpCodes.Ldloc_S && instructions[i].operand == label)
							instructions[i] = new(OpCodes.Ldarg_0);
					}
				}
				else log.Log(LogLevel.Error, "SyncMemberEditorBuilder.Build Transpiler Failed");

				return instructions;
			}
        }

		[HarmonyPatchCategory(HiddenCat)]
        [HarmonyPatch(typeof(WorkerInspector), nameof(WorkerInspector.BuildInspectorUI))]
        class WorkerInspectorPatch //HideInInspectorAttribute check
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes) 
			{
                List<CodeInstruction> instructions = new(codes);
                int index = -1;
                for (int i = 2; i < instructions.Count - 1; i++)
                {
                    if (instructions[i-2].opcode == OpCodes.Ldarg_0 && instructions[i].opcode == OpCodes.Callvirt && instructions[i + 1].opcode == OpCodes.Call && (instructions[i+1].operand as MethodInfo)?.Name == "GetCustomAttribute")
                    {
                        index = i - 2;
                        break;
                    }
                }
                if (index > -1)
                {
                    instructions.RemoveAt(index);
                    instructions.RemoveAt(index);
                    instructions.RemoveAt(index);
                    instructions.RemoveAt(index);
                    instructions.RemoveAt(index);
                }
                else log.Log(LogLevel.Error, "WorkerInspector.BuildInspectorUI Transpiler Failed");
                return instructions;
            }
        }
	}
}