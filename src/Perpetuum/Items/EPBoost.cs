using Perpetuum.Accounting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Items
{
	public class EPBoost : Item
	{
		public int Boost
		{
			get
			{
				return ED.Options.GetOption<int>("addBoost");
			}
		}

		public int TimePeriodHours
		{
			get
			{
				return ED.Options.GetOption<int>("timePeriodHours");
			}
		}

		public string Tier
		{
			get
			{
				return ED.Options.GetOption<string>("tier");
			}
		}
		
		public void Activate(IAccountManager accountManager, Account account)
		{
				accountManager.ExtensionSubscriptionStart(account, DateTime.UtcNow, DateTime.UtcNow.AddHours(TimePeriodHours), Boost);
		}
	}
}
