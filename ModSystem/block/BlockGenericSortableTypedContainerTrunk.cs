namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;

    // 1.18 was IMultiBlockMonolithicSmall
    public class BlockGenericSortableTypedContainerTrunk : BlockGenericSortableTypedContainer, IMultiBlockColSelBoxes
    {
        private Cuboidf[] mirroredColBox;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.mirroredColBox = new Cuboidf[] { this.CollisionBoxes[0].RotatedCopy(0, 180, 0, new Vec3d(0.5, 0.5, 0.5)) };
        }


        public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        { return this.mirroredColBox; }


        public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return this.mirroredColBox;
        }
    }
}
