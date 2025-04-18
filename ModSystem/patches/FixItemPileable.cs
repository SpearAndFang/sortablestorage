using HarmonyLib;
using SortableStorage.ModSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.GameContent;

namespace sortablestorage.ModSystem.patches
{
    [HarmonyPatch(typeof(ItemPileable), nameof(ItemPileable.OnHeldInteractStart))]
    public static class FixItemPileable
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label = generator.DefineLabel();
            Type toFindType = typeof(BlockEntityLabeledChest);
            Label? toFindLabel = null;

            for(var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (toFindLabel != null)
                {
                    if (code.labels != null && code.labels.Contains(toFindLabel.Value))
                    {
                        code.labels.Add(label);
                        break;
                    }
                }
                else if(code.opcode == OpCodes.Isinst && code.operand is Type foundType && foundType == toFindType && codes[i + 1].operand is Label foundLabel)
                {
                    toFindLabel = foundLabel;
                    codes.InsertRange(i + 2, new CodeInstruction[]
                    {
                        new(OpCodes.Ldloc_3),
                        new(OpCodes.Isinst, typeof(BESortableLabeledChest)),
                        new(OpCodes.Brtrue_S, label)
                    });
                }
            }

            return codes;
        }
    }
}
