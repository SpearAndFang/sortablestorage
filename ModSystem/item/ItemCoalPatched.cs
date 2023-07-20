namespace SortableStorage.ModSystem
{
    //using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    //using Vintagestory.API.Common.Entities;
    //using Vintagestory.API.MathTools;
    //using Vintagestory.API.Util;
    //using Vintagestory.GameContent;

    public class ItemCoalPatched : ItemPileablePatched
    {
        protected override AssetLocation PileBlockCode => new AssetLocation("coalpile");
    }
}
