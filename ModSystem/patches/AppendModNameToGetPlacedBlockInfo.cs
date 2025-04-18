using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace sortablestorage.ModSystem.patches
{
    [HarmonyPatch(typeof(Block), nameof(Block.GetPlacedBlockInfo))]
    public static class AppendModNameToGetPlacedBlockInfo
    {
        [HarmonyPostfix]
        public static void SSBlockGetPlacedBlockInfoPostfix(Block __instance, ref string __result)
        {
            if (__instance.Code.Domain == "sortablestorage")
            {
                __result += "\n\n<font color=\"#E8DAEF\"><i>Sortable Storage</i></font>\n\n";
            }
        }
    }
}
