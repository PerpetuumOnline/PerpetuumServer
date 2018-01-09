using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.Services.ProductionEngine
{
   
    public class ProductionLine
    {
        private readonly IProductionDataAccess _productionDataAccess;
        private readonly ItemHelper _itemHelper;
        public int Id;
        public int CharacterId;
        public long FacilityEid;
        public int TargetDefinition;
        public double MaterialEfficiency;
        public double TimeEfficiency;
        public int Cycles;
        public int Rounds;
        public int? RunningProductionId;
        public long? CPRGEid;


        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.ID, Id},
                           {k.facility, FacilityEid},
                           {k.targetDefinition, TargetDefinition},
                           {k.materialEfficiency,(int) MaterialEfficiency},
                           {k.timeEfficiency,(int) TimeEfficiency},
                           {k.cycle, Cycles},
                           {k.production, RunningProductionId},
                           {k.rounds, Rounds},
                           {k.maxDistortion, Decalibration.distorsionMax},
                           {k.dead, IsAtZero()}
                           
                       };

        }

        public override string ToString()
        {
            return $"productionLineId:{Id}, CharacterId:{CharacterId}, Facility:{FacilityEid}, Definition:{TargetDefinition} {EntityDefault.Get(TargetDefinition).Name}";
        }

        public delegate ProductionLine Factory();

        public static Factory ProductionLineFactory { get; set; }

        public ProductionLine(IProductionDataAccess productionDataAccess,ItemHelper itemHelper)
        {
            _productionDataAccess = productionDataAccess;
            _itemHelper = itemHelper;
        }

        /*
         id
characterid
facilityeid
runningproductionid
targetdefinition
materialefficiency
timeefficiency
cycles
rounds
cprgeid

         */

        private const string SELECT_LINE = "select * from productionlines";

        public static ProductionLine CreateFromRecord(IDataRecord record)
        {
            var line = ProductionLineFactory();
            line.Id = record.GetValue<int>("id");
            line.CharacterId = record.GetValue<int>("characterid");
            line.FacilityEid = record.GetValue<long>("facilityeid");
            line.TargetDefinition = record.GetValue<int>("targetdefinition");
            line.MaterialEfficiency = record.GetValue<double>("materialefficiency");
            line.TimeEfficiency = record.GetValue<double>("timeefficiency");
            line.Cycles = record.GetValue<int>("cycles");
            line.RunningProductionId = record.GetValue<int?>("runningproductionid");
            line.Rounds = record.GetValue<int>("rounds");
            line.CPRGEid = record.GetValue<long?>("cprgeid");
            return line;
        }



        public bool IsAtZero()
        {
            return (MaterialEfficiency <= 0 && TimeEfficiency <= 0);
        }

        public ErrorCodes IsDeletable()
        {
            return IsActive() ? ErrorCodes.ProductionIsRunningOnThisLine : ErrorCodes.NoError;
        }

        public bool IsActive()
        {
            return !(RunningProductionId == null || RunningProductionId == 0);
        }

        public double GetMaterialAmountMultiplier()
        {
            if (Math.Abs(MaterialEfficiency - 0) < double.Epsilon) return 1.0;

            return 1 / MaterialEfficiency;
        }

        public int GetMaterialPoints()
        {
            return (int) MaterialEfficiency;
        }


        public double GetTimeMultiplier()
        {
            return 1 / TimeEfficiency;
        }

        public int GetTimePoints()
        {
            return (int) TimeEfficiency;
        }

        private int GetMaterialEfficiencyPercentage()
        {
            return (int)(MaterialEfficiency * 100);
        }

        private int GetTimeEfficiencyPercentage()
        {
            return (int)(TimeEfficiency * 100);
        }

        public Character GetOwnerCharacter
        {
            get
            {
                var character = Character.Get(CharacterId);
                if (character == Character.None)
                {
                    Logger.Error("character not found related to productionline " + this);
                    throw new PerpetuumException(ErrorCodes.CharacterNotFound);
                }

                return character;

            }
        }

        public void GetDecalibratedEfficiencies(ref double newMaterialEfficiency, ref double newTimeEfficiency)
        {
            var productionDecalibration = Decalibration;

            GetDecalibratedEfficiencies(MaterialEfficiency, TimeEfficiency, productionDecalibration.decrease, ref newMaterialEfficiency, ref newTimeEfficiency);

        }

        public static void GetDecalibratedEfficiencies(double oldMaterialEfficiency, double oldTimeEfficiency, double decrease, ref double newMaterialEfficiency, ref double newTimeEfficiency)
        {
            newMaterialEfficiency = oldMaterialEfficiency - decrease;
            newTimeEfficiency = oldTimeEfficiency - decrease;

            newMaterialEfficiency = newMaterialEfficiency.Clamp(0, oldMaterialEfficiency);
            newTimeEfficiency = newTimeEfficiency.Clamp(0, oldTimeEfficiency);
        }

        public ProductionDecalibration Decalibration => _productionDataAccess.GetDecalibration(TargetDefinition);

        //------ static stuff

        public static Dictionary<string, object> ListCalibratedLines(int characterId, long facilityEid)
        {
            var counter = 0;
            return
                (GetLinesByCharacter(characterId, facilityEid)
                    .Select(pl => (object)pl.ToDictionary()))
                    .ToDictionary(d => "c" + counter++);
        }

        public static IEnumerable<ProductionLine> GetLinesByCharacter(int characterId, long facilityEid)
        {
            return
                (from r in
                     Db.Query().CommandText(SELECT_LINE + " where characterid=@characterID and facilityeid=@facilityEID").SetParameter("@characterID", characterId).SetParameter("@facilityEID", facilityEid)
                     .Execute()
                 select CreateFromRecord(r));

        }

        public static IEnumerable<ProductionLine> GetLinesByFacilityEid(long facilityEid)
        {
            return 
            Db.Query().CommandText(SELECT_LINE + " where facilityeid=@facilityEID")
                   .SetParameter("@facilityEID", facilityEid)
                   .Execute()
                   .Select(CreateFromRecord);

        }


        public static int CountLinesForCharacter(Character character, long facilityEid)
        {
            return Db.Query().CommandText("select count(*) from productionlines where characterid=@characterID and facilityeid=@facilityEID")
                           .SetParameter("@characterID", character.Id)
                           .SetParameter("@facilityEID", facilityEid)
                           .ExecuteScalar<int>();
        }

        public static int DeleteAllByFacilityEid(long facilityEid)
        {
            return Db.Query().CommandText("delete productionlines where facilityeid=@facilityEid").SetParameter("@facilityEid", facilityEid).ExecuteNonQuery();
        }


        public static ErrorCodes LoadById(int id, out ProductionLine productionLine)
        {
            productionLine = null;
            var record =
            Db.Query().CommandText(SELECT_LINE + " where id=@ID").SetParameter("@ID", id)
                .ExecuteSingleRow();

            if (record == null)
            {
                return ErrorCodes.ItemNotFound;
            }

            productionLine = CreateFromRecord(record);
            return ErrorCodes.NoError;

        }

        [NotNull]
        public static ProductionLine LoadByIdAndCharacterAndFacility(Character character, int lineId, long facilityEid)
        {
            var record = Db.Query().CommandText(SELECT_LINE + " where id=@lineID and characterId=@characterID and facilityeid=@facility")
                .SetParameter("@lineID", lineId)
                .SetParameter("@characterId", character.Id)
                .SetParameter("@facility", facilityEid)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.ItemNotFound);

            return CreateFromRecord(record);
        }

        public static ErrorCodes SetRounds(int targetRounds, int lineId)
        {
            var res =
            Db.Query().CommandText("update productionlines set rounds=@rounds where id=@lineID").SetParameter("@lineID", lineId).SetParameter("@rounds", targetRounds)
               .ExecuteNonQuery();


            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;

        }


        public void DecreaseRounds()
        {

            Rounds--;

            if (Rounds < 0) Rounds = 0;

            SetRounds(Rounds, Id);

        }



        public static ErrorCodes SetRunningProductionId(int lineId, int? runningProductionId)
        {
            object rpid = DBNull.Value;

            if (runningProductionId != null) rpid = (int)runningProductionId;

            var res =
            Db.Query().CommandText("update productionlines set runningproductionid=@rpid where id=@lineID").SetParameter("@lineID", lineId).SetParameter("@rpid", rpid)
                .ExecuteNonQuery();

            return (res == 1) ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;

        }

        public static ErrorCodes CheckOwner(int characterId, int lineId)
        {
            var res =
                Db.Query().CommandText("select targetdefinition from productionlines where characterid=@characterID and id=@ID").SetParameter("@characterID", characterId).SetParameter("@ID", lineId)
                    .ExecuteScalar<int>();

            return (res == 0) ? ErrorCodes.AccessDenied : ErrorCodes.NoError;
        }

        public static void DeleteById(int lineId)
        {
            Db.Query().CommandText("delete productionlines where id=@ID")
                .SetParameter("@ID", lineId)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLDeleteError);
        }

        public static void CreateCalibratedLine(Character character, long facilityEid,CalibrationProgram program)
        {
            const string insertSqlCommand = @"INSERT dbo.productionlines (characterid,facilityeid,targetdefinition,materialefficiency,timeefficiency,cprgeid) 
                                              VALUES (@characterID, @facility ,@definition,@materialEfficiency,@timeEfficiency,@cprgEid)";

            Db.Query().CommandText(insertSqlCommand)
                .SetParameter("@characterID", character.Id)
                .SetParameter("@facility", facilityEid)
                .SetParameter("@definition",program.TargetDefinition)
                .SetParameter("@materialEfficiency",program.MaterialEfficiencyPoints)
                .SetParameter("@timeEfficiency",program.TimeEfficiencyPoints)
                .SetParameter("@cprgEid", program.Eid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }


        public static void PostMassProduction(Character character, int lineId, double newTimeEfficiency, double newMaterialEfficiency)
        {
            Db.Query().CommandText("update productionlines set runningproductionid=NULL, cycles=cycles+1,materialefficiency=@materialEfficiency,timeefficiency=@timeEfficiency where characterid=@characterID and id=@lineID")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@lineID", lineId)
                .SetParameter("@timeEfficiency", newTimeEfficiency)
                .SetParameter("@materialEfficiency", newMaterialEfficiency)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public static ErrorCodes LoadByProductionId(Character character, int productionInProgressId, out ProductionLine productionLine)
        {
            productionLine = null;

            var record = Db.Query().CommandText(SELECT_LINE + " where characterid=@characterID and runningproductionid=@rpid")
                                 .SetParameter("@characterID", character.Id)
                                 .SetParameter("@rpid", productionInProgressId)
                                 .ExecuteSingleRow();

            if (record == null) 
                return ErrorCodes.ItemNotFound;

            productionLine = CreateFromRecord(record);

            return ErrorCodes.NoError;
        }

        [CanBeNull]
        public static ProductionLine LoadByProductionId(Character character, int productionInProgressId)
        {
            var record = Db.Query().CommandText(SELECT_LINE + " where characterid=@characterID and runningproductionid=@rpid")
                                 .SetParameter("@characterID", character.Id)
                                 .SetParameter("@rpid", productionInProgressId)
                                 .ExecuteSingleRow();

            if (record == null) 
                return null;

            return CreateFromRecord(record);
        }


        public void CalculateDecalibrationPenalty(Character character, out int newMaterialEfficiencyPoints, out int newTimeEfficiencyPoints)
        {
            var extensionBonus = character.GetExtensionBonusByName(ExtensionNames.PRODUCTION_DECALIBRATION_EFFICIENCY).Clamp(0, 10);
            var penaltyMultiplier = 0.9 + (extensionBonus * 0.01);

            newMaterialEfficiencyPoints =(int)( MaterialEfficiency * penaltyMultiplier);
            newTimeEfficiencyPoints = (int) (TimeEfficiency * penaltyMultiplier);
        }

        public int GetCalibrationTemplateDefinition()
        {
            if (DynamicCalibrationProgram.IsDefinitionDynamic(TargetDefinition))
            {
                return DynamicCalibrationProgram.GetDynamicTemplateDefinition(TargetDefinition);
            }

            var prototypePairDefinition = _productionDataAccess.GetPrototypePair(TargetDefinition);
            var researchLevel = _productionDataAccess.ResearchLevels.GetOrDefault(prototypePairDefinition).ThrowIfNull(ErrorCodes.ItemNotResearchable);

            return (int)researchLevel.calibrationProgramDefinition.ThrowIfNull(ErrorCodes.ServerError);
        }

        [NotNull]
        public CalibrationProgram GetOrCreateCalibrationProgram(Mill mill)
        {
            
                CalibrationProgram calibrationProgram;


                if (CPRGEid == null)
                {
                    Logger.Info("cprg eid is creating one for " + this);

                    var character = GetOwnerCharacter;
                    
                    //create item to ram
                    var calibrationProgramDefinition = GetCalibrationTemplateDefinition();
                    calibrationProgram = (CalibrationProgram) Entity.Factory.CreateWithRandomEID(calibrationProgramDefinition);
                    calibrationProgram.Owner = character.Eid;
                    mill.GetStorage().AddChild(calibrationProgram);
                    
                    // db-be kell csinalni mert a dinamikus felulirja save-nel
                    calibrationProgram.Save();

                    Logger.Info("cprg created " + calibrationProgram);

                }
                else
                {
                    //load from sql
                    calibrationProgram =  (CalibrationProgram)_itemHelper.LoadItemOrThrow((long) CPRGEid);
                    Logger.Info("found and cprg loaded " + calibrationProgram);
                }

                return calibrationProgram;
            
        }

        /// <summary>
        /// Data from the line is transferred to CPRG
        /// </summary>
        /// <returns></returns>
        public CalibrationProgram ExtractCalibrationProgram(Mill mill)
        {
            var character = GetOwnerCharacter;

            var calibrationProgram = GetOrCreateCalibrationProgram(mill);

            int materialEfficiency;
            int timeEfficiency;
            CalculateDecalibrationPenalty(character, out materialEfficiency, out timeEfficiency);

            calibrationProgram.MaterialEfficiencyPoints = materialEfficiency;
            calibrationProgram.TimeEfficiencyPoints = timeEfficiency;

            return calibrationProgram;
        }



    }

}
