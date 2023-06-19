using HarmonyLib;
using NeosModLoader;
using System;
using FrooxEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ExposeEditorlessMembers
{
	public class ExposeEditorlessMembers : NeosMod
	{
		public override string Name => "ExposeEditorlessMembers";
		public override string Author => "eia485";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/EIA485/NeosExposeEditorlessMembers/";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.eia485.ExposeEditorlessMembers");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(SyncMemberEditorBuilder), nameof(SyncMemberEditorBuilder.Build))]
		class ExposeEditorlessMembersPatch
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
			{
				List<CodeInstruction> instructions = new List<CodeInstruction>(codes);
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
				if(index != -1)
				{
					instructions.RemoveAt(index);
					instructions.RemoveAt(index);
					instructions.RemoveAt(index);
				}
				if (label != null)
				{
                    for (int i = 0; i < instructions.Count; i++)
                    {
						if (instructions[i].opcode == OpCodes.Ldloc_S && instructions[i].operand == label)
							instructions[i] = new(OpCodes.Ldarg_0);
                    }
                }
				return instructions;
			}
		}
	}
}