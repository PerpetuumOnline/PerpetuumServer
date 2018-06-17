using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items
{
    public class Paint : Item
    {
        private readonly RobotHelper _robotHelper;
        public Paint(RobotHelper helper)
        {
            this._robotHelper = helper;
        }

        public void Activate(RobotInventory targetContainer, Character character)
        {
            var robot = _robotHelper.LoadRobotForCharacter(targetContainer.GetOrLoadParentEntity().Eid, character, true);
            robot.ThrowIfNull(ErrorCodes.NoRobotFound);
            robot.DynamicProperties.Update(k.tint, this.ED.Config.Tint); //Apply color
            robot.DynamicProperties.Update(k.decay, 255); //Reset Decay
            robot.Save();
        }

    }
}