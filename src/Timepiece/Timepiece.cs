using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.GameContent;


namespace Timepiece {
    class Timepiece : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("BlockHourglass", typeof(BlockHourglass));
            api.RegisterBlockEntityClass("Hourglass", typeof(BlockEntityHourglass));

            api.RegisterBlockBehaviorClass("Sundial", typeof(Sundial));
        }
    }


    class Sundial : BlockBehavior
    {
        public Sundial(Block block) : base(block)
        {

        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)world.Api;

                TimeSpan timespan = TimeSpan.FromHours(world.Calendar.HourOfDay); // should be noted this is using client time
                timespan = TimeSpan.FromMinutes(30 * Math.Ceiling(timespan.TotalMinutes / 30)); // rounds up to the nearest 30 minutes
                string time = timespan.ToString("hh\\:mm");

                if (world.Calendar.FullHourOfDay >= 6 && world.Calendar.FullHourOfDay <= 20)
                    capi.ShowChatMessage("It looks to be about " + time + ".");
                else
                    capi.ShowChatMessage("It's too dark to see the sundial clearly.");
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }
    }
}
