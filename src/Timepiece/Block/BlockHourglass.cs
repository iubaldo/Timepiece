using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace Timepiece
{
    class BlockHourglass : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityHourglass beHourglass = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityHourglass;

            if (beHourglass != null && beHourglass.CanFall() && (blockSel.SelectionBoxIndex == 1 || beHourglass.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
            {
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
