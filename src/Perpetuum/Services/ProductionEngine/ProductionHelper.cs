using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;

namespace Perpetuum.Services.ProductionEngine
{
    public static class ProductionHelper
	{
		#region production logger

		public static void ProductionLogInsert(Character character, int definition, int amount, ProductionInProgressType productionInProgressType, int durationSecs, double price, bool useCorporationWallet)
		{
			var res = Db.Query().CommandText("insert productionlog (characterid,definition,amount, productiontype,durationsecs,price,usecorporationwallet) values (@characterID,@definition,@amount, @type,@duration,@price,@useCorpWallet)")
				.SetParameter("@characterID", character.Id)
				.SetParameter("@definition", definition)
				.SetParameter("@amount", amount)
				.SetParameter("@type", (int) productionInProgressType)
				.SetParameter("@duration", durationSecs)
				.SetParameter("@price", price)
				.SetParameter("@useCorpWallet", useCorporationWallet)
				.ExecuteNonQuery();

			if (res != 1)
			{
				Logger.Error("sql insert error in ProductionLogInsert. chid:" + character.Id + " def:" + definition + " amount:" + amount);
			}
		}

		public static Dictionary<string, object> ProductionLogList(Character character, int offsetInDays, PrivateCorporation corporation)
		{
			var result = new Dictionary<string, object>();
			var counter = 0;

			var later = DateTime.Now.AddDays(-1 * offsetInDays);
			var earlier = later.AddDays(-14);

			var queryString = "select definition,amount,productiontime,productiontype,durationsecs,price,usecorporationwallet,characterid from productionlog where productiontime > @earlier and productiontime < @later ";

			if (corporation != null)
			{
				var members = corporation.GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.ProductionManager).Select(m => m.character.Id);
				queryString += " and usecorporationwallet=1 and characterId in ( " + members.ArrayToString() + " )";
			}
			else
			{
				queryString += " and characterID=@characterID";
			}

			var records = Db.Query().CommandText(queryString)
				.SetParameter("@characterID", character.Id)
				.SetParameter("@earlier", earlier)
				.SetParameter("@later", later)
				.Execute();

			foreach (var record in records)
			{
				var stepper = record.GetStepper();
				var tempDict = new Dictionary<string, object>
				{
					{k.definition, stepper.GetNextValue<int>()},
					{k.amount, stepper.GetNextValue<int>()},
					{k.date, stepper.GetNextValue<DateTime>()},
					{k.productionType, stepper.GetNextValue<int>()},
					{k.duration, stepper.GetNextValue<int>()},
					{k.price, stepper.GetNextValue<double>()},
					{k.useCorporationWallet, stepper.GetNextValue<bool>()},
					{k.characterID, stepper.GetNextValue<int>()},
				};
				result.Add("c" + counter++, tempDict);
			}

			return result;
		}

        #endregion

        public static IEnumerable<long> LoadAllLiveFacilityEids()
        {
            //wont load the facilities from the trash
            const string queryStr = @"SELECT eid FROM dbo.getLiveDockingbaseChildren() WHERE definition IN (SELECT [definition] FROM dbo.getDefinitionByCF(@CF))";

            var facilityEids =
                Db.Query().CommandText(queryStr)
                    .SetParameter("@CF", (long)CategoryFlags.cf_production_facilities)
                    .Execute()
                    .Select(r => r.GetValue<long>(0))
                    .ToList();

            Logger.Info(facilityEids.Count + " live facilities will be loaded.");

            return facilityEids;
        }


        public static IEnumerable<long> LoadFacilityEidsFromActiveZones()
		{
			//wont load the facilities from the trash
			const string queryStr = @"SELECT eid FROM dbo.getDockingbaseChildrenFromActiveZones() WHERE definition IN (SELECT [definition] FROM dbo.getDefinitionByCF(@CF))";

			var facilityEids =
				Db.Query().CommandText(queryStr)
					.SetParameter("@CF", (long)CategoryFlags.cf_production_facilities)
					.Execute()
					.Select(r => r.GetValue<long>(0))
					.ToList();

			Logger.Info(facilityEids.Count + " facilities will be loaded from active zones.");

			return facilityEids;
		}


		public static long LoadStorageEid(long facilityEid)
		{
			var eid =
				Db.Query().CommandText("select top 1 eid from entities where parent=@facilityEid and definition=@containerDef")
				.SetParameter("@containerDef", EntityDefault.GetByName(DefinitionNames.SYSTEM_CONTAINER).Definition)
				.SetParameter("@facilityEid", facilityEid)
					.ExecuteScalar<long>();

			Logger.Info("storage loaded for " + facilityEid + " storageEid:" + eid);
			return eid;
		}

		public static ErrorCodes CheckReprocessCondition(Item targetItem, Character character)
		{
			if (targetItem is VolumeWrapperContainer)
			{
				return ErrorCodes.AccessDenied;
			}

			var parent = targetItem.GetOrLoadParentEntity();
			if (parent == null || parent is VolumeWrapperContainer)
			{
				return ErrorCodes.AccessDenied;
			}

			if (targetItem.ED.CategoryFlags.IsCategory(CategoryFlags.cf_basic_commodities))
			{
				return ErrorCodes.BasicCommoditiesAreNotReproccesable;
			}

			if (targetItem.ED.AttributeFlags.NonRecyclable)
			{
				return ErrorCodes.ItemNotRecyclable;
			}

            if (targetItem is Robot robot)
            {
                //is it an active robot?
                if (character.IsRobotSelectedForCharacter(robot))
                {
                    return ErrorCodes.RobotMustBeDeselected;
                }

                if (robot.HasModule)
                {
                    return ErrorCodes.RobotHasModulesEquipped;
                }

                if (robot.IsItemsInContainer)
                {
                    return ErrorCodes.RobotHasItemsInContainer;
                }
            }

			foreach (var component in ProductionComponentCollector.Collect(targetItem))
			{
				var currentQuantity = targetItem.Quantity;
				var defaultQuantity = targetItem.ED.Quantity;
				var batchCount = (int) Math.Floor(currentQuantity / (double) defaultQuantity);

				if ((double) component.Amount * batchCount > int.MaxValue)
				{
					return ErrorCodes.MaximumStackSizeExceeded;
				}
			}

			return ErrorCodes.NoError;
		}

		public static ErrorCodes FindResearchKitDefinitionByLevel(int targetLevel, out int definition)
		{
			definition = 0;

			foreach (var ed in EntityDefault.All.GetByCategoryFlags(CategoryFlags.cf_research_kits))
			{
				if (ed.Options.Level != targetLevel)
					continue;

				definition = ed.Definition;
				return ErrorCodes.NoError;
			}

			return ErrorCodes.ConsistencyError;
		}

		public static int GetComponentAmountFromSql(int sourceDefinition, int componentDefinition)
		{
			return Db.Query().CommandText("select componentamount from components where definition=@def and componentDefinition=@compDef")
                          .SetParameter("@def", sourceDefinition)
                          .SetParameter("@compDef", componentDefinition)
					      .ExecuteScalar<int>();
		}


		public static void DeleteComponentFromSql(int sourceDefinition, int componentDefinition)
		{
			Db.Query().CommandText("delete components where definition=@def and componentdefinition=@compDef")
                   .SetParameter("@def", sourceDefinition)
                   .SetParameter("@compDef", componentDefinition)
				   .ExecuteNonQuery();
		}

		public static void InsertComponentToSql(int sourceDefinition, int componentDefinition, int componentAmount)
		{
			Logger.Info($"   {EntityDefault.Get(sourceDefinition).Name} comp:{EntityDefault.Get(componentDefinition).Name} amount:{componentAmount}");

			Db.Query().CommandText("insert components (definition, componentdefinition, componentamount) values (@def, @compDef, @am)")
                   .SetParameter("@def", sourceDefinition)
                   .SetParameter("@compDef", componentDefinition)
                   .SetParameter("@am", componentAmount)
				   .ExecuteNonQuery();
		}

		public static void DeleteAllComponentsFromSql(int componentDefinition)
		{
			Db.Query().CommandText("delete components where definition=@cdef")
                   .SetParameter("@cdef", componentDefinition)
				   .ExecuteNonQuery();
		}

		public static void InsertProductionComponentsForRobotComponent(int robotComponent, IEnumerable<ProductionComponent> productionComponents, double multiplier)
		{
			foreach (var productionComponent in productionComponents)
			{
				if (productionComponent.Amount == 1 && multiplier != 1.0)
				{
					Logger.Info("skipping component: " + EntityDefault.Get(productionComponent.EntityDefault.Definition).Name + " " + productionComponent.EntityDefault.Definition);
					continue;
				}

				var compAmount = (int) Math.Ceiling((productionComponent.Amount * multiplier));
				var componentEd = EntityDefault.Get(productionComponent.EntityDefault.Definition);

				if (productionComponent.Amount == 1 && multiplier == 1.0)
				{
					compAmount = 1; //to avoid floating point idiocy
				}

				Logger.Info("production component: " + componentEd.Name + " amount in robot:" + productionComponent.Amount + " amount in robot component: " + compAmount);

				Db.Query().CommandText("insert components (definition, componentdefinition, componentamount) values (@def,@cdef,@cam)").SetParameter("@def", robotComponent).SetParameter("@cdef", productionComponent.EntityDefault.Definition).SetParameter("@cam", compAmount)
					.ExecuteNonQuery();
			}
		}


		public static Dictionary<CategoryFlags, ProductionFacilityType> PBSNodeCFTofacilityType = new Dictionary<CategoryFlags, ProductionFacilityType>
		{
			{CategoryFlags.cf_pbs_mill_nodes, ProductionFacilityType.MassProduce},
			{CategoryFlags.cf_pbs_prototyper_nodes, ProductionFacilityType.Prototype},
			{CategoryFlags.cf_pbs_refinery_nodes, ProductionFacilityType.Refine},
			{CategoryFlags.cf_pbs_reprocessor_nodes, ProductionFacilityType.Reprocess},
			{CategoryFlags.cf_pbs_repair_nodes, ProductionFacilityType.Repair},
			{CategoryFlags.cf_pbs_reseach_lab_nodes, ProductionFacilityType.Research},
			{CategoryFlags.cf_pbs_calibration_forge_nodes, ProductionFacilityType.CalibrationProgramForge},
			{CategoryFlags.cf_pbs_research_kit_forge_nodes, ProductionFacilityType.ResearchKitForge},
		};

	    public static IEnumerable<ProductionLiveComponent> SearchForAvailableComponents(Container container, List<ProductionComponent> components)
		{
			var componentDefinitions = components.Select(c => c.EntityDefault.Definition).ToList();

			var foundItems = container.GetItems().Where(i => componentDefinitions.Contains(i.Definition));

			var foundComponents = new List<ProductionLiveComponent>();

			foreach (var item in foundItems)
			{
				if (item.HealthRatio < 1.0)
					continue;

				//only items that are in container
				var co = item.GetOrLoadParentEntity() as Container;
				if (co == null)
					continue;

                if (item is Robot robot && !robot.IsRepackaged)
                    continue;

                var plc = new ProductionLiveComponent
				{
					eid = item.Eid,
					quantity = item.Quantity,
					definition = item.Definition,
					resultQuantity = item.Quantity,
				};

				foundComponents.Add(plc);
			}

			return foundComponents;
		}


		public static IEnumerable<ProductionLiveComponent> ProcessComponentRequirement(ProductionInProgressType productionType, List<ProductionLiveComponent> foundComponents, int targetAmount, double materialMultiplier, List<ProductionComponent> components)
		{
			var itemsNeeded = new List<ProductionLiveComponent>();
			var componentsMissing = new Dictionary<string, object>();
			var wasComponentMissing = false;
			var missingCounter = 0;

			foreach (var component in components)
			{
				var defName = component.EntityDefault.Name;

				if (component.IsSkipped(productionType))
					continue;

				var realNeededAmount = component.EffectiveAmount(targetAmount, materialMultiplier);
				var componentFound = false;
				var amountFound = 0;

		  
				foreach (var foundComponent in foundComponents)
				{
					if (foundComponent.definition != component.EntityDefault.Definition)
						continue;

					Logger.Info("checking eid:" + foundComponent.eid + " " + defName + " needed:" + realNeededAmount + " found:" + foundComponent.quantity);

					if (foundComponent.quantity <= realNeededAmount - amountFound)
					{
						Logger.Info("eaten completely: " + foundComponent.eid);

						//kill it
						foundComponent.resultQuantity = 0;

						//increase the total amount we found with the quantity was found in the current item
						amountFound += foundComponent.quantity;
					}
					else
					{
						Logger.Info("fraction used: " + foundComponent.eid + " component match successful!");

						//descrease item's quantity
						foundComponent.resultQuantity -= realNeededAmount - amountFound;

						//amount satisfied
						amountFound += realNeededAmount - amountFound;
					}

					itemsNeeded.Add(foundComponent);

					if (amountFound != realNeededAmount)
						continue;

					componentFound = true;
					break;
				}

				if (componentFound)
					continue;

				wasComponentMissing = true;

				var missingInfo = new Dictionary<string, object>
				{
					{k.definition, component.EntityDefault.Definition},
					{k.targetAmount, realNeededAmount},
					{k.amount, amountFound}
				};

				componentsMissing.Add("m" + missingCounter++, missingInfo);
			}

			wasComponentMissing.ThrowIfTrue(ErrorCodes.RequiredComponentNotFound,gex => gex.SetData(k.missing, componentsMissing));
			return itemsNeeded;
		}


		public static ErrorCodes ReserveComponents_noSQL(IEnumerable<ProductionLiveComponent> itemsNeeded, long storageEid, Container container, out long[] reservedItems)
		{
			var ec = ErrorCodes.NoError;
			var reservedList = new List<long>();
			reservedItems = null;

			foreach (var plc in itemsNeeded)
			{
				//load the component
				var component = container.GetItem(plc.eid, true);
				if (component == null)
				{
					ec = ErrorCodes.ItemNotFound;
					return ec;
				}

				if (plc.resultQuantity == 0)
				{
					//the component will be completely used for the manufacture

					//put it into the storage

					if (!container.RemoveItemFromTree(component))
						return ErrorCodes.ItemNotFound;

					component.Parent = storageEid;

					reservedList.Add(component.Eid); //this is the item we put into the storage

				    component.Save();
				}
				else
				{
					//create a portion of the component

					var componentPortion = component.Unstack(plc.quantity - plc.resultQuantity);

					componentPortion.Parent = storageEid;

					//save the component portion
				    componentPortion.Save();

				    reservedList.Add(componentPortion.Eid);
				}
			}

			reservedItems = reservedList.ToArray();

			//---------------------------------------------DEBUG OUTPUT-------------------------------------
			var outputDict = new Dictionary<string, int>();
			foreach (var comp in itemsNeeded)
			{
				var defname = EntityDefault.Get(comp.definition).Name;
				var quantityUsed = comp.resultQuantity == 0 ? comp.quantity : comp.quantity - comp.resultQuantity;

				if (outputDict.ContainsKey(defname))
				{
					outputDict[defname] += quantityUsed;
				}
				else
				{
					outputDict[defname] = quantityUsed;
				}
			}

			Logger.Info("items used for production: ----------------------------------");
			foreach (var pair in outputDict)
			{
				Logger.Info(pair.Key + " " + pair.Value);
			}
			Logger.Info("-------------------------------------------------------------");

			return ec;
		}

	}
}