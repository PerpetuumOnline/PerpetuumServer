using System.Collections.Generic;

namespace Perpetuum.Services.Insurance
{
    public class InsurancePrice
    {
        public int definition;
        public double fee;
        public double payOut;


        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.definition, definition},
                    {k.fee, fee},
                    {k.payOut, payOut}
                };

        }

    }
}