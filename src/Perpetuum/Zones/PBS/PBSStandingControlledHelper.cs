using System.Collections.Generic;
using Perpetuum.Units;

namespace Perpetuum.Zones.PBS
{

    public interface IHaveStandingLimit
    {
        double StandingLimit { get; set; }
    }

    public interface IStandingController : IHaveStandingLimit
    {
        bool StandingEnabled { get; set; }
    }




    /// <summary>
    /// Stores and controls standing limit which defines a set to operate on/or not
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PBSStandingController<T>  where T:Unit,IStandingController
    {
        private readonly T _unit;

       

        public PBSStandingController(T unit)
        {
            _unit = unit;
        }


        /// <summary>
        /// If true the standing limit will always have a value
        /// </summary>
        public bool AlwaysEnabled { private get; set; }

        public bool Enabled
        {
            get
            {
                if (AlwaysEnabled)
                    return true;
                
                return _unit.DynamicProperties.Contains(k.standing);
            }
            set
            {
                if ( AlwaysEnabled )
                    return;
                
                if (!value)
                {
                    //set to false 
                    _unit.DynamicProperties.Remove(k.standing);
                }
                else
                {
                    //set to true
                    StandingLimit = 0.0;
                }
            }
        }

        public double StandingLimit
        {
            get { return _unit.DynamicProperties.GetOrDefault<double>(k.standing); }
            set { _unit.DynamicProperties.Set(k.standing,value); }
        }

        public override string ToString()
        {
            return $"AlwaysEnabled: {AlwaysEnabled}, Enabled: {Enabled}, StandingLimit: {StandingLimit}";
        }
    }

    public static class PBSStandingControllerExtension
    {
        public static void AddStandingInfoToDictonary(this IStandingController standingController,IDictionary<string,object> dictionary)
        {
            dictionary.Add(k.standing, standingController.StandingLimit);
            dictionary.Add(k.standingSet, standingController.StandingEnabled.ToInt());
        }
    }


}