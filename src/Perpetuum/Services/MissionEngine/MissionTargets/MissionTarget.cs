using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Zones;
using Perpetuum.Zones.Scanning;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
   

    [Serializable]
    public abstract class MissionTarget 
    {
        public readonly int id;
        private readonly string _name;
        private readonly string _description;
        public virtual MissionTargetType Type { get; private set ; }

        protected int? definition;
        protected int? quantity;

        protected int? targetPositionX;
        protected int? targetPositionY;
        protected int? targetPositionZone;

        protected int? targetPositionRange;
        public Position targetPosition;

        protected int? probeType;

        public readonly string completedMessage;
        public readonly string activatedMessage;
        protected int? artifactType;
        public int? teleportChannel;
        private readonly int? _npcPresenceId;

        private readonly int? _missionId;
        public int targetOrder; //same number means they appear together
        public int displayOrder; //sequential 
        public readonly bool isOptional;
        private readonly bool _isHidden; //client display
        private readonly int? _branchMissionId;

        protected long? missionStructureEid;
        protected int? secondaryDefinition;
        protected int? secondaryQuantity;
        private readonly int? _primaryDefinitionFromIndex;
        private readonly int? _secondaryDefinitionFromIndex;
        private readonly int? _findRadius; //at target searching matched with the searchOrigin/searchRadius
        private readonly bool _spawnNpcs; //used at find artifact
        public readonly bool isSnapToNextStructure;
        protected readonly bool generateSecondaryDefinition;
        public readonly bool targetSecondaryAsMyPrimary;
        protected readonly bool targetPrimaryAsMySecondary;
        public bool useQuantityOnly;
        public readonly bool deliverAtAnyLocation;
        protected readonly bool generateResearchKit;
        protected readonly bool generateCalibrationProgram;
        private readonly long? _primaryCategory;
        private readonly long? _secondaryCategory;
        public readonly bool scalePrimaryQuantityWithLevel;
        protected readonly bool scaleSecondaryQuantityWithLevel;
        private readonly double? _primaryscalemult;
        private readonly double? _secondaryscalemult;
        
        private bool _isResolved;

        public static MissionDataCache missionDataCache { get; set; }
        public static IProductionDataAccess ProductionDataAccess { get; set; }
        public static IRobotTemplateRelations RobotTemplateRelations { get; set; }
        public static MissionTargetInProgress.Factory MissionTargetInProgressFactory { get; set; }

        public virtual void Scale(MissionInProgress missionInProgress)
        {
            IsScaled = true;
        }

        private bool _isScaled;

        public  bool IsScaled
        {
            get { return _isScaled; }
            private set { _isScaled = value; }
        }

        public  int Reward
        {
            get { return missionDataCache.GetRewardByType(Type); }
        }
     


        public virtual void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget(this);
        }


        public EntityDefault PrimaryEntityDefault
        {
            get
            {
                if (ValidDefinitionSet)
                {
                    return EntityDefault.Get(Definition);
                }

                return EntityDefault.None;
            }
        }


        public EntityDefault SecondaryEntityDefault
        {
            get
            {
                if (ValidSecondaryDefinitionSet)
                {
                    return EntityDefault.Get(SecondaryDefinition);
                }

                return EntityDefault.None;
            }
        }


        public int MissionId
        {
            get { return _missionId ?? 0; }
        }

        public bool ValidMissionIdSet
        {
            get { return _missionId != 0 && _missionId > 0; }
        }


        public int TargetPositionRange
        {
            get { return targetPositionRange ?? 0; }
            set { targetPositionRange = value; }
        }

        public bool ValidRangeSet
        {
            get { return targetPositionRange != null && targetPositionRange > 0; }
        }

        public int Definition
        {
            get { return definition ?? EntityDefault.None.Definition; }
            set { definition = value; }
        }

        public bool ValidDefinitionSet
        {
            get { return definition != null && definition != EntityDefault.None.Definition; }
        }

        public int Quantity
        {
            get { return quantity ?? 0; }
        }

        public bool ValidQuantitySet
        {
            get { return quantity != null && quantity > 0; }
        }

        public bool IsBranching
        {
            get { return _branchMissionId != null && _branchMissionId > 0; }
        }

        public int BranchMissionId
        {
            get { return _branchMissionId ?? 0; }
        }

        public long MissionStructureEid
        {
            get { return missionStructureEid ?? 0; }
        }

        public bool ValidMissionStructureEidSet
        {
            get { return missionStructureEid != null && missionStructureEid > 0; }
        }

        public bool IsSecondaryItemSet
        {
            get { return secondaryDefinition != null && secondaryQuantity != null && secondaryDefinition > 0 && secondaryQuantity > 0; }
        }

        public int SecondaryDefinition
        {
            get { return secondaryDefinition ?? 0; }
            set { secondaryDefinition = value; }
        }

        public int SecondaryQuantity
        {
            get { return secondaryQuantity ?? 0; }
            set { secondaryQuantity = value; }
        }

        public bool IsResolved
        {
            get { return _isResolved; }
            set { _isResolved = value; }
        }

        public int NpcPresenceId
        {
            get { return _npcPresenceId ?? 0; }
        }

        public bool ValidPresenceSet
        {
            get { return _npcPresenceId != null && _npcPresenceId > 0; }
        }

        public bool ValidPrimaryLinkSet
        {
            get { return _primaryDefinitionFromIndex != null && _primaryDefinitionFromIndex > 0; }
        }

        public bool ValidSecondaryLinkSet
        {
            get { return _secondaryDefinitionFromIndex != null && _secondaryDefinitionFromIndex > 0; }
        }

        public int PrimaryDefinitionLinkId
        {
            get { return _primaryDefinitionFromIndex ?? 0; }
        }

        public int SecondaryDefinitionLinkId
        {
            get { return _secondaryDefinitionFromIndex ?? 0; }
        }

        public int ZoneId
        {
            get { return targetPositionZone ?? -1; }
        }

        public bool ValidZoneSet
        {
            get { return targetPositionZone != null && targetPositionZone >= 0; }
        }

        public bool ValidPositionSet
        {
            get { return targetPositionX != null && targetPositionY != null; }
        }

        public bool CheckPosition
        {
            get { return ValidZoneSet && ValidPositionSet && ValidRangeSet; }
        }


        public bool ValidArtifactTypeSet
        {
            get { return artifactType != null && artifactType > 0; }
        }

        public ArtifactType TargetArtifactType
        {
            get { return artifactType == null ? ArtifactType.undefined : (ArtifactType) artifactType; }
        }

        public bool FindArtifactSpawnsNpcs
        {
            get { return _spawnNpcs; }
        }

        public int FindRadius
        {
            get { return _findRadius ?? 0; }
        }

        public bool ValidFindRadiusSet
        {
            get { return _findRadius != null && _findRadius > 0; }
        }

        public bool ValidSecondaryDefinitionSet
        {
            get { return secondaryDefinition != null && secondaryDefinition > 0; }
        }

        public bool ValidSecondaryQuantitySet
        {
            get { return secondaryQuantity != null && secondaryQuantity > 0; }
        }


        public bool ValidItemInfo
        {
            get { return definition != null && definition > 0 && quantity != null && quantity > 0; }
        }

        public ItemInfo GetItemInfoFromPrimaryDefinition
        {
            get { return new ItemInfo(Definition, Quantity); }
        }

        public double PrimaryScaleMultiplier
        {
            get { return _primaryscalemult ?? 1.0; }
        }

        public double SecondaryScaleMultiplier
        {
            get { return _secondaryscalemult ?? 1.0; }
        }

        public bool IsPrimaryMineral
        {
            get
            {
                if (!ValidDefinitionSet)
                {
                    return false;
                }
                
                return PrimaryEntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_raw_material);
            }
        }


        protected bool ValidPrimaryCategorySet { get { return _primaryCategory != null && _primaryCategory > 0; } }
        protected CategoryFlags PrimaryCategoryFlags { get { return (CategoryFlags) (_primaryCategory ?? 0); } }

        protected bool ValidSecondaryCategorySet { get { return _secondaryCategory != null && _secondaryCategory > 0; } }

        protected CategoryFlags SecondaryCategoryFlags
        {
            get { return (CategoryFlags) (_secondaryCategory ?? 0); }
        }

        protected MissionTarget (IDataRecord record)
        {
            id = record.GetValue<int>(k.ID.ToLower());
            _description = record.GetValue<string>(k.description);
            _name = record.GetValue<string>(k.name);
            Type = (MissionTargetType) record.GetValue<int>(k.targetType.ToLower());
            definition = record.GetValue<int?>(k.definition);
            quantity = record.GetValue<int?>(k.quantity);
            targetPositionX = record.GetValue<int?>(k.targetPositionX.ToLower());
            targetPositionY = record.GetValue<int?>(k.targetPositionY.ToLower());

            targetPositionRange = record.GetValue<int?>(k.targetPositionRange.ToLower());
            targetPositionZone = record.GetValue<int?>(k.targetPositionZone.ToLower());

            probeType = record.GetValue<int?>(k.scanType.ToLower());

            completedMessage = record.GetValue<string>("completedmessage");
            activatedMessage = record.GetValue<string>("activatedmessage");
            artifactType = record.GetValue<int?>("artifacttype");
            teleportChannel = record.GetValue<int?>("teleportchannel");
            _npcPresenceId = record.GetValue<int?>("npcpresenceid");

            _missionId = record.GetValue<int>(k.missionID.ToLower());
            targetOrder = record.GetValue<int>(k.targetOrder.ToLower());
            displayOrder = record.GetValue<int>(k.displayOrder.ToLower());
            _branchMissionId = record.GetValue<int?>(k.branchMissionId.ToLower());
            isOptional = record.GetValue<bool>(k.optional);
            _isHidden = record.GetValue<bool>(k.hidden);
            missionStructureEid = record.GetValue<long?>("structureeid");
            _primaryDefinitionFromIndex = record.GetValue<int?>("primarydefinitionfromindex");
            _secondaryDefinitionFromIndex = record.GetValue<int?>("secondarydefinitionfromindex");
            _findRadius = record.GetValue<int?>("findradius");
            artifactType = record.GetValue<int?>("artifacttype");
            _spawnNpcs = record.GetValue<bool>("spawnnpcs");
            isSnapToNextStructure = record.GetValue<bool>("snaptonextstructure");
            generateSecondaryDefinition = record.GetValue<bool>("generatesecondarydefinition");
            targetSecondaryAsMyPrimary = record.GetValue<bool>("targetsecondaryasmyprimary");
            targetPrimaryAsMySecondary = record.GetValue<bool>("targetprimaryasmysecondary");
            deliverAtAnyLocation = record.GetValue<bool>("anylocation");
            useQuantityOnly = record.GetValue<bool>("usequantityonly");
            generateCalibrationProgram = record.GetValue<bool>("generatecprg");
            generateResearchKit = record.GetValue<bool>("generateresearchkit");
            _primaryCategory = record.GetValue<long?>("primarycategory");
            _secondaryCategory = record.GetValue<long?>("secondarycategory");
            secondaryQuantity = record.GetValue<int?>("secondaryquantity");
            scalePrimaryQuantityWithLevel = record.GetValue<bool>("scaleprimaryqwithlevel");
            scaleSecondaryQuantityWithLevel = record.GetValue<bool>("scalesecondaryqwithlevel");
            _primaryscalemult = record.GetValue<double?>("primaryscalemult");
            _secondaryscalemult = record.GetValue<double?>("secondaryscalemult");

            ResetMyDictionary();
        }

       


        public MissionTarget GetClone()
        {
            return this.Clone();
        }

        private Dictionary<string, object> GenerateMyDictionary()
        {
            var result = new Dictionary<string, object>
            {
                {k.ID, id},
                {k.description, _description},
                {k.type, (int) Type},
                {k.definition, definition},
                {k.quantity, quantity},
                {k.scanType, probeType},
                {k.artifactType, artifactType},
                {k.teleportChannelID, teleportChannel},
                {k.missionID, _missionId},
                {k.targetOrder, targetOrder},
                {k.displayOrder, displayOrder},
                {k.branchMissionId, _branchMissionId},
                {k.optional, isOptional},
                {k.hidden, _isHidden},
                {k.structureEid, missionStructureEid},
            };

            if (ValidZoneSet)
            {
                result.Add(k.targetPositionZone, targetPositionZone);
            }

            if (ValidPositionSet)
            {
                result.Add(k.targetPosition, targetPosition);
                result.Add(k.targetPositionRange, targetPositionRange);
            }

            return result;
        }


        private Lazy<Dictionary<string, object>> _myDictionary;
        


        public  virtual Dictionary<string, object> ToDictionary()
        {
            return _myDictionary.Value;
        }

        public void ResetMyDictionary()
        {
            _myDictionary = new Lazy<Dictionary<string, object>>(GenerateMyDictionary);
        }

        public MaterialProbeType GetProbeType
        {
            get { return (probeType == null) ? MaterialProbeType.Undefined : (MaterialProbeType) probeType; }
        }

        public int MineralDefinition
        {
            get { return definition ?? 0; }
        }

        

            /// <summary>
        /// ez a recordbol osszerakja megegyszer, csak az ellenorzes kedveert, ... nem kene de ez van %%%
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public static bool Filter(IDataRecord record)
        {
            var target = MissionTargetFactory.GenerateMissionTargetFromConfigRecord(record);

            if (!target.FilterWork())
            {
                Logger.Error("mission target consistency error! ID: " + target.id);
                return false;
            }

            return true;
        }

        public virtual void PostLoadedAsConfigTarget()
        {
            SetTargetPosition_ConfigTarget();
        }

        protected virtual bool FilterWork()
        {
            return CheckTargetConsistency();
        }


        private void SetTargetPosition_ConfigTarget()
        {
            if (ValidPositionSet)
            {
                targetPosition = new Position(targetPositionX ?? 0, targetPositionY ?? 0);
            }
            else
            {
                targetPosition = default(Position);
            }
        }

        public void SetPositionComponents(int x, int y)
        {
            targetPositionX = x;
            targetPositionY = y;
            targetPosition = new Position(targetPositionX ?? 0, targetPositionY ?? 0);
        }


        private bool CheckTargetConsistency()
        {
            EntityDefault ed;

            if (ValidMissionIdSet)
            {
                //handmade content

                //definition and quantity needed
                switch (Type)
                {
                    case MissionTargetType.fetch_item:
                    case MissionTargetType.kill_definition:
                    case MissionTargetType.loot_item:
                    case MissionTargetType.scan_container:
                    case MissionTargetType.scan_unit:
                    case MissionTargetType.drill_mineral:
                    case MissionTargetType.submit_item:
                        if (definition == null)
                        {
                            Logger.Error("consistency error mission target ID:" + id + " definition is NULL");
                            return false;
                        }
                        if (quantity == null || quantity == 0)
                        {
                            Logger.Error("consistency error mission target ID:" + id + " quantity is NULL or 0");
                            return false;
                        }

                        ed = EntityDefault.Get((int) definition);
                        if (ed == null)
                        {
                            Logger.Error("consistency error mission target ID:" + id + " definition not exists or not enabled. definition: " + definition);
                            return false;
                        }

                        break;
                }
            }

            if (CheckPosition)
            {
                //target position, zone, range needed
                switch (Type)
                {
                    case MissionTargetType.reach_position:
                    case MissionTargetType.scan_container:
                    case MissionTargetType.scan_mineral:
                    case MissionTargetType.scan_unit:
                    case MissionTargetType.loot_item:
                    case MissionTargetType.kill_definition:
                    case MissionTargetType.drill_mineral:
                    case MissionTargetType.submit_item:
                    case MissionTargetType.use_switch:
                    case MissionTargetType.find_artifact:
                    case MissionTargetType.dock_in:
                    case MissionTargetType.summon_npc_egg:

                        //zoneid missing, but position ok
                        if ((targetPositionX != null || targetPositionY != null) && targetPositionZone == null)
                        {
                            Logger.Error("consistency error mission target ID:" + id + " zone missing, but position is defined");
                            return false;
                        }

                        if (targetPositionZone == null)
                        {
                            Logger.Error("consistency error target zone is null in missiontarget:" + id);
                            return false;
                        }
                        if (targetPositionRange == null || targetPositionRange == 0)
                        {
                            Logger.Error("consistency error range is null in missiontarget:" + id);
                            return false;
                        }
                        if (targetPositionX == null || targetPositionY == null)
                        {
                            Logger.Error("consistency error target position is null in missiontarget:" + id);
                            return false;
                        }

                        break;
                }
            }

            if (Type == MissionTargetType.scan_mineral)
            {
                if (probeType == null || probeType == 0)
                {
                    Logger.Error("consistency error scantype is null in missiontarget:" + id);
                    return false;
                }

                switch ((MaterialProbeType) probeType)
                {
                    case MaterialProbeType.Area:
                    case MaterialProbeType.Directional:
                    case MaterialProbeType.Tile:

                        if (definition == null)
                        {
                            Logger.Error("consistency error definition is null in missiontarget:" + id);
                            return false;
                        }
                        if (quantity == null || quantity == 0)
                        {
                            Logger.Error("consistency error quantity is null in missiontarget:" + id);
                            return false;
                        }

                        ed = EntityDefault.Get((int) definition);

                        if (ed == null) return false;
                        if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_organic))
                        {
                            Logger.Error("consistency error non organic definition is set in missiontarget:" + id);
                            return false;
                        }
                        break;

                    case MaterialProbeType.OneTile:
                        definition = null;
                        quantity = null;
                        break;

                    case MaterialProbeType.Undefined:
                        return false;
                }
            }

            if (Type == MissionTargetType.find_artifact)
            {
                if (artifactType == null)
                {
                    Logger.Error("no artifact type was set for " + this);
                    return false;
                }
            }

            //check enum and code consistency
            if (!((int) Type).IsValueInEnum<MissionTargetType>())
            {
                Logger.Error("error occured caching the mission targets. Target type exists in database but in MissionTargetType not found: ID:" + (int) Type);
                return false;
            }

            if (definition != null && quantity == null)
            {
                Logger.Error("consistency error in missiontarget. definition is not null and quantity is null. id:" + id);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Basic operation: use the sql data into the running target
        /// </summary>
        /// <param name="missionInProgress"></param>
        /// <returns></returns>
        public virtual MissionTargetInProgress CreateTargetInProgress(MissionInProgress missionInProgress)
        {
            //use the object from the mission data cache
            //technically by reference no runtime duplication needed
            return MissionTargetInProgressFactory(missionInProgress, this);
        }

        public void ModifyWithRecord(IDataRecord record)
        {
            targetOrder = record.GetValue<int>("targetorder");
            displayOrder = record.GetValue<int>("displayorder");

            definition = record.GetValue<int?>("definition");
            quantity = record.GetValue<int?>("quantity");
            missionStructureEid = record.GetValue<long?>("structureeid");
            secondaryDefinition = record.GetValue<int?>("secondarydefinition");
            secondaryQuantity = record.GetValue<int?>("secondaryquantity");
            targetPositionZone = record.GetValue<int?>("zoneid");
            targetPositionX = record.GetValue<int?>("x");
            targetPositionY = record.GetValue<int?>("y");
            artifactType = record.GetValue<int?>("artifacttype");
            Type = (MissionTargetType) record.GetValue<int>("targettype");
            probeType = record.GetValue<int?>("scantype");
            targetPositionRange = record.GetValue<int?>("targetrange");

            //...
            SetTargetPosition_ConfigTarget();
            ResetMyDictionary();
        }

        [Conditional("DEBUG")]
        public void Log(string message)
        {
            if (MissionResolveTester.skipLog) return;
            Logger.Info(Type + " *-> " + message);
        }


        public virtual void ResolveLinks(MissionInProgress missionInProgress)
        {
            Log("Links resolved " + this);

            _isResolved = true;
        }

        public void AddParametersToArchive(DbQuery dbQuery)
        {
            dbQuery
                .SetParameter("@targetOrder", targetOrder)
                .SetParameter("@displayOrder", displayOrder)
                .SetParameter("@definition", definition)
                .SetParameter("@quantity", quantity)
                .SetParameter("@structureEid", missionStructureEid)
                .SetParameter("@secondaryDefinition", secondaryDefinition)
                .SetParameter("@secondaryQuantity", secondaryQuantity)
                .SetParameter("@zoneId", targetPositionZone)
                .SetParameter("@x", targetPositionX)
                .SetParameter("@y", targetPositionY)
                .SetParameter("@artifactType", artifactType)
                .SetParameter("@targetType", (int) Type)
                .SetParameter("@scanType", probeType)
                .SetParameter("@targetRange", targetPositionRange);
                

        }

        public virtual bool ResolveLocation(MissionInProgress missionInProgress)
        {
            return true;
        }

        /// <summary>
        /// Inserts a mission target that will be used as a possible target spot
        /// </summary>
        public static void InsertMissionTargetSpot(string name, MissionTargetType targetType, double x, double y, int zoneId, int findRadius)
        {
            var query = @"
INSERT dbo.missiontargets
        ( name ,
          description ,
          targettype ,
          targetpositionx ,
          targetpositiony ,
          targetpositionzone ,
          findradius 
        )
VALUES  ( @name ,
          @name,
          @targetType,
          @x,
          @y,
          @zoneId,
          @findRadius
        )";

            var res = Db.Query().CommandText(query)
                .SetParameter("@name", name)
                .SetParameter("@targetType", (int) targetType)
                .SetParameter("@x", x)
                .SetParameter("@y", y)
                .SetParameter("@zoneId", zoneId)
                .SetParameter("@findRadius", findRadius)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);
        }

        public void UpdatePositionById(Position position)
        {
            var qs = "update missiontargets set targetpositionx=@x,targetpositiony=@y where id=@id";
            var res =
            Db.Query().CommandText(qs)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@id", id)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLUpdateError);
        }

        public static void UpdatePositionByStructureEid(long eid, Position position)
        {
            var qs = "update missiontargets set targetpositionx=@x,targetpositiony=@y where structureeid=@eid";
            var res =
            Db.Query().CommandText(qs)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@eid", eid)
                .ExecuteNonQuery();

           // (res == 1).ThrowIfFalse(ErrorCodes.SQLUpdateError); %%% csak melora
        }

        public static bool IsTargetNameTaken(string targetName)
        {
            return Db.Query().CommandText("select count(*) from missiontargets where name=@name")
                .SetParameter("@name", targetName)
                .ExecuteScalar<int>() > 0;
        }

        public static int CountTypeOnZone(MissionTargetType missionTargetType, int zoneId)
        {
            return
                Db.Query().CommandText("SELECT COUNT(DISTINCT name) FROM dbo.missiontargets WHERE targetpositionzone=@zoneId AND targettype=@targetType")
                    .SetParameter("@zoneId", zoneId)
                    .SetParameter("@targetType", (int) missionTargetType)
                    .ExecuteScalar<int>();
        }


        public static void UpdatePosition(int targetId, int x, int y)
        {
            var res =
                Db.Query().CommandText("UPDATE dbo.missiontargets SET targetpositionx=@x,targetpositiony=@y WHERE id=@targetId")
                    .SetParameter("@targetId", targetId)
                    .SetParameter("@x", x)
                    .SetParameter("@y", y)
                    .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLUpdateError);
        }

        public static void InsertMissionTargetForStructure(MissionStructure missionStructure, IZone zone, int findRadius, int? useRange = null)
        {
            var targetType = missionStructure.TargetType;

            var name = GenerateName(targetType, zone.Id, "mstruct");
            var x = missionStructure.CurrentPosition.X;
            var y = missionStructure.CurrentPosition.Y;
            var zoneId = zone.Id;

            var query = @"
INSERT dbo.missiontargets
        ( name ,
          description ,
          targettype ,
          targetpositionx ,
          targetpositiony ,
          targetpositionzone ,
          targetpositionrange,
          findradius ,
          structureeid
        )
VALUES  ( @name ,
          @name,
          @targetType,
          @x,
          @y,
          @zoneId,
          @useRange,
          @findRadius,
          @eid
        )";

            var res = Db.Query().CommandText(query)
                .SetParameter("@name", name)
                .SetParameter("@targetType", (int) targetType)
                .SetParameter("@x", x)
                .SetParameter("@y", y)
                .SetParameter("@zoneId", zoneId)
                .SetParameter("@findRadius", findRadius)
                .SetParameter("@eid", missionStructure.Eid)
                .SetParameter("@useRange", useRange)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);
        }

        public static string GenerateName(MissionTargetType targetType, int zoneId, string prefix)
        {
            var countOnZone = CountTypeOnZone(targetType, zoneId);
            countOnZone++;

            var name = MakeName(prefix,targetType,zoneId,countOnZone);
            while (IsTargetNameTaken(name))
            {
                countOnZone += 1;
                name = MakeName(prefix, targetType, zoneId, countOnZone);
            }

            return name;
        }

        private static string MakeName(string prefix, MissionTargetType targetType, int zoneId, int countOnZone)
        {
            return prefix + "_" + targetType + "_z" + zoneId + "_n" + countOnZone;
        }


        public override string ToString()
        {
            var displayStr =  id + " " + _name + " " + Type + " T:" + targetOrder + " D:" + displayOrder;

            if (ValidDefinitionSet)
            {
                displayStr += " def:" + Definition + " " + PrimaryEntityDefault.Name;
            }

            if (ValidQuantitySet)
            {
                displayStr += " qty:" + Quantity;
            }

            if (ValidSecondaryDefinitionSet)
            {
                displayStr += " secDef:" + SecondaryDefinition + " " + SecondaryEntityDefault.Name;
            }

            if (ValidSecondaryQuantitySet)
            {
                displayStr += " secQty:" + SecondaryQuantity;
            }

            if (ValidMissionStructureEidSet)
            {
                displayStr += " mstrEid:" + MissionStructureEid;
            }
            if (generateResearchKit)
            {
                displayStr += " genResKit";
            }
            if (generateCalibrationProgram)
            {
                displayStr += " genCPRG";
            }
            if (deliverAtAnyLocation)
            {
                displayStr += " anyLocation";
            }
            if (useQuantityOnly)
            {
                displayStr += " useQtyOnly";
            }

            if (generateSecondaryDefinition)
            {
                displayStr += " genSecDef";
            }

            if (isSnapToNextStructure)
            {
                displayStr += " snap";
            }

            if (ValidZoneSet)
            {
                displayStr += " zone:" + ZoneId;
            }

            if (ValidPositionSet)
            {
                displayStr += " x:" + targetPositionX + " y:" + targetPositionY;
            }

            if (ValidRangeSet)
            {
                displayStr += " rng:" + TargetPositionRange;
            }

            if (isOptional)
            {
                displayStr += " Optional";
            }

            if (ValidPrimaryCategorySet)
            {
                displayStr += " priCat:" + PrimaryCategoryFlags;
            }

            if (ValidSecondaryCategorySet)
            {
                displayStr += " secCat:" + SecondaryCategoryFlags;
            }

            if (scalePrimaryQuantityWithLevel)
            {
                displayStr += " priScales";
            }

            if (scaleSecondaryQuantityWithLevel)
            {
                displayStr += " secScales";
            }

            if (_primaryscalemult != null)
            {
                displayStr += " priScMult:" + _primaryscalemult;
            }

            if (_secondaryscalemult != null)
            {
                displayStr += " secScMult:" + _secondaryscalemult;
            }

            return displayStr;
        }


        public void CopyMyPrimaryDefinitionToTarget(MissionTarget destinationTarget)
        {
            //copy the parameters to the current target -> resolve the link
            if (destinationTarget.targetSecondaryAsMyPrimary)
            {
                //sanityCheck
                if (!ValidSecondaryDefinitionSet)
                {
                    Logger.Error("secondary definition is not set in linked target. " + destinationTarget + " linking as primary to " + this);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                Log("copy def secondary->primary definition:" + SecondaryDefinition + " " + SecondaryEntityDefault.Name + " to " + destinationTarget);

                //nondefault behaviour
                destinationTarget.Definition = SecondaryDefinition;
            }
            else
            {
                if (useQuantityOnly)
                {
                    //this target does not specify definition
                    return;
                }

                //sanity check
                if (!ValidDefinitionSet)
                {
                    Logger.Error("primary definition is not set in linked target. " + destinationTarget + " linking as primary to " + this);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                Log("copy def primary->primary definition:" + Definition + " " + PrimaryEntityDefault.Name + " to " + destinationTarget);

                //default
                destinationTarget.Definition = Definition;
            }
        }





        public void CopyMySecondaryDefinitionToTarget(MissionTarget destinationTarget)
        {
            if (destinationTarget.targetPrimaryAsMySecondary)
            {
                if (useQuantityOnly)
                {
                    return;
                }

                //sanity check
                if (!ValidDefinitionSet)
                {
                    Logger.Error("primary definition is not set in linked target. " + destinationTarget + " linking as secondary to " + this );
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                Log("copy def primary->secondary definition:" + Definition + " " + PrimaryEntityDefault.Name +" to " + destinationTarget);
                
                //switched
                destinationTarget.SecondaryDefinition = Definition;
                
            }
            else
            {
                //sanity check
                if (!ValidSecondaryDefinitionSet)
                {
                    Logger.Error("secondary definition is not set in linked target. " + destinationTarget + " linking as secondary to " + this );
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                Log("copy secondary->secondary definition:" + SecondaryDefinition + " " + SecondaryEntityDefault.Name  +" to " + destinationTarget);

                //default
                destinationTarget.SecondaryDefinition = SecondaryDefinition;
            }
        }


        public static void DeleteByStrucureEid(long eid)
        {
            var res =
            Db.Query().CommandText("delete missiontargets where structureeid=@eid")
                .SetParameter("@eid", eid)
                .ExecuteNonQuery();

            Logger.Info(res + " target deleted with eid:" + eid);


        }
    }
}
