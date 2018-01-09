using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{


    /// <summary>
    /// Running instance of a target
    /// Technically a wrapper for the config stuff.
    /// </summary>
    public class MissionTargetInProgress
    {
        //these values changing as the mission progresses
        public int progressCount;
        public bool completed;

        private readonly MissionDataCache _missionDataCache;
        public readonly MissionInProgress myMissionInProgress;
        public readonly MissionTarget myTarget;
        private readonly DeliveryHelper.Factory _deliveryHelperFactory;

        private int? _successZoneId;
        private int? _successX;
        private int? _successY;
        private DateTime? _successTime;

        public delegate MissionTargetInProgress Factory(MissionInProgress missionInProgress,MissionTarget missionTarget);

        public MissionTargetInProgress(MissionDataCache missionDataCache,DeliveryHelper.Factory deliveryHelperFactory,MissionInProgress missionInProgress, MissionTarget missionTarget)
        {
            _missionDataCache = missionDataCache;
            _deliveryHelperFactory = deliveryHelperFactory;

            myMissionInProgress = missionInProgress;
            myTarget = missionTarget;
        }

        public MissionTargetType TargetType
        {
            get { return myTarget.Type; }
        }

        public int MissionTargetId
        {
            get { return myTarget.id; }
        }

        public int TargetOrder
        {
            get { return myTarget.targetOrder; }
        }

        public int DisplayOrder
        {
            get { return myTarget.displayOrder; }
        }

        public bool IsMyTurn
        {
            get { return TargetOrder == myMissionInProgress.currentTargetOrder; }
        }

        public void ForceComplete()
        {
            completed = true;
            progressCount = 1;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.target, MissionTargetId}, //helper id
                {k.progressCount, progressCount}, //current progress
                {k.completed, completed},
                {k.missionID, myMissionInProgress.MissionId},
                {k.guid, myMissionInProgress.missionGuid.ToString()},
                {k.type, (int) TargetType}, //targetInfo
                {k.targetOrder, TargetOrder}, //targetInfo
                {k.display, DisplayOrder}, //targetInfo
                {"targetInfo", myTarget.ToDictionary()}
            };
            
        }


        /// <summary>
        /// Digs out the success info from a dictionary
        /// This is the place where this target got succeeded
        /// </summary>
        /// <param name="info"></param>
        public void GetSuccessInfo(IDictionary<string, object> info)
        {

            if (info.ContainsKey(k.locationID))
            {
                var locationId = info.GetValue<int>(k.locationID);
                MissionLocation location;
                if (_missionDataCache.GetLocationById(locationId, out location))
                {
                    _successZoneId = location.ZoneConfig.Id;
                    _successX = location.MyPosition.intX;
                    _successY = location.MyPosition.intY;
                    _successTime = DateTime.Now;
                }
            }
            else
            {
                int zoneId;
                Position position;

                if (info.ContainsKey(k.zoneID))
                {
                    zoneId = info.GetValue<int>(k.zoneID);
                }
                else
                {
                    Logger.Error("no zoneId was found in success info " + this);
                    throw new PerpetuumException(ErrorCodes.ServerError);
                }


                if (info.ContainsKey(k.position))
                {
                    position = info.GetValue<Position>(k.position);
                }
                else
                {
                    Logger.Error("no position was found in success info " + this);
                    throw new PerpetuumException(ErrorCodes.ServerError);
                }
                
                _successZoneId = zoneId;
                _successX = position.intX;
                _successY = position.intY;
                _successTime = DateTime.Now;
            }

            Logger.Info("success info obtained zoneId:" + _successZoneId + " x:" + _successX + " y:" + _successY  + " " + this);
        }


        public override string ToString()
        {
            return $"progress:{progressCount} completed:{completed} -- {myTarget}";
        }

        public void PrepareInfoDictionary(Dictionary<string,object> info,  string message = null, int sourceCharacter = 0)
        {
            info[k.targetID] = MissionTargetId;

            if (message != null)
            {
                info[k.message] = message;
            }

            if (sourceCharacter != 0)
            {
                //assisting info
                info[k.sourceAgent] = sourceCharacter;
            }
        }




        public void SendTargetStatusToGangOnCommitted(Command command,  Dictionary<string,object> info, string message = null )
        {
            if (message != null)
            {
                info[k.message] = message;
            }
            Transaction.Current.OnCommited(() => SendTargetStatusToGang(command, info));
        }


        public Task SendTargetStatusToGangAsync(Command command, Dictionary<string,object> info )
        {
            return Task.Run(() => SendTargetStatusToGang(command, info));
        }



        private void SendTargetStatusToGang(Command command, Dictionary<string,object> info )
        {
            var affectedCharacters = myMissionInProgress.GetAffectedCharacters();

            Message.Builder.SetCommand(command)
                .WithData(info)
                .ToCharacters(affectedCharacters)
                .Send();

        }


        public bool IsTargetBranching
        {
            get { return myTarget.IsBranching; }
        }

        public DateTime SuccessTime { get { return _successTime ?? DateTime.Now; } }

        public Position SuccessPosition { get {  return new Position( _successX ?? 0, _successY ?? 0);} }


        public bool ValidSuccessInfoSet
        {
            get { return _successTime != null && _successX != null && _successY != null && _successZoneId != null; }
        }


        public bool IsBranchNeeded()
        {
            return IsTargetBranching && completed;
        }


        public ErrorCodes WriteMissionTargetToSql()
        {
            var dbQuery = Db.Query().CommandText("mission_writetargetinprogress")
                .SetParameter("@missionID", myMissionInProgress.MissionId)
                .SetParameter("@characterID", myMissionInProgress.character.Id)
                .SetParameter("@targetID", MissionTargetId)
                .SetParameter("@progressCount", progressCount)
                .SetParameter("@completed", completed)
                .SetParameter("@guid", myMissionInProgress.missionGuid);

            //add target specific sql parameters
            myTarget.AddParametersToArchive(dbQuery);
            
            var res = dbQuery.ExecuteScalar<int>();

            return (res != 1) ? ErrorCodes.SQLExecutionError : ErrorCodes.NoError;
        }

        #region Advancing target

        private ErrorCodes Advance_SimpleTarget()
        {
            var ec = ErrorCodes.NoError;

            //safety
            if (completed) return ErrorCodes.NothingToDo;

            completed = true;
            progressCount = 1;

            return ec;
        }

        private ErrorCodes AdvanceTarget_WithQuantity(bool isComplete, int currentProgress)
        {
            var ec = ErrorCodes.NoError;

            completed = isComplete;
            progressCount = currentProgress;

#if DEBUG
            if (isComplete)
            {
                if (myTarget.Quantity != currentProgress)
                {
                    Logger.Error("consistency error! currentProgress:" + currentProgress + " target description count:" +
                                 myTarget.Quantity + " targetType:" + TargetType + " id:" + MissionTargetId);
                }
            }
#endif
            return ec;
        }

        public ErrorCodes AdvanceTarget_KillDefinition_IncreaseOnly()
        {
            progressCount ++;

            completed = myTarget.Quantity <= progressCount;

            return ErrorCodes.NoError;
        }


        public ErrorCodes AdvanceTarget_Teleport(int teleportChannelId)
        {
           
            if (myTarget.teleportChannel == teleportChannelId)
            {
                completed = true;
                return ErrorCodes.NoError;
            }

            return ErrorCodes.NothingToDo;
        }


        public ErrorCodes AdvanceTarget_MassProduce(int definition, int quantity, IDictionary<string, object> data)
        {
            if (myTarget.Definition == definition)
            {
                progressCount += quantity;
                
                if (progressCount >= myTarget.Quantity)
                {
                    completed = true;
                    
                }

                return ErrorCodes.NoError;
            }

            return ErrorCodes.NothingToDo;
        }

        public ErrorCodes AdvanceTarget_Prototype(int definition)
        {
            if (myTarget.Definition == definition)
            {
                completed = true;
                return ErrorCodes.NoError;
            }

            return ErrorCodes.NothingToDo;
        }

        public ErrorCodes AdvanceTarget_Research(int definition, IDictionary<string, object> data )
        {
            if (myTarget.Definition == definition)
            {
                completed = true;
                return ErrorCodes.NoError;
            }

            return ErrorCodes.NothingToDo;
        }


        public ErrorCodes AdvanceTarget_LootItem(bool isComplete, int lootItemsCount)
        {
            return AdvanceTarget_WithQuantity(isComplete, lootItemsCount);
        }

        public ErrorCodes AdvanceTarget_KillDefinition(bool isComplete, int killDefinitionCount)
        {
            return AdvanceTarget_WithQuantity(isComplete, killDefinitionCount);
        }

        
        public ErrorCodes AdvanceTarget_LockUnit(bool isComplete, int lockedUnitCount, long[] lockedNpcEids)
        {
            var lockUnitTarget = myTarget as LockUnitRandomTarget;
            lockUnitTarget?.SetLockedNpcEids(lockedNpcEids);

            return AdvanceTarget_WithQuantity(isComplete, lockedUnitCount);
        }


        public ErrorCodes AdvanceTarget_ReachPosition()
        {
            return Advance_SimpleTarget();
        }

        public ErrorCodes AdvanceTarget_PopNpc()
        {
            return Advance_SimpleTarget();
        }


        public ErrorCodes AdvanceTarget_Alarm()
        {
            return Advance_SimpleTarget();
        }

        
        public ErrorCodes AdvanceTarget_ItemSupply(bool isComplete, int itemCount)
        {
            return AdvanceTarget_WithQuantity(isComplete, itemCount);
            
        }


        public ErrorCodes AdvanceTarget_FindArtifact()
        {
            return Advance_SimpleTarget();
        }


        public ErrorCodes AdvanceTarget_ScanMineral()
        {
            return Advance_SimpleTarget();
        }


        public ErrorCodes AdvanceTarget_ScanUnit(bool isComplete, int scanCount)
        {
            return AdvanceTarget_WithQuantity(isComplete, scanCount);
        }


        public ErrorCodes AdvanceTarget_ScanContainer(bool isComplete, int scannedDefinitionCount)
        {
            return AdvanceTarget_WithQuantity(isComplete, scannedDefinitionCount);
        }


      

        public ErrorCodes AdvanceTarget_MineralDrilled(bool isComplete, int drilledAmount)
        {
            return AdvanceTarget_WithQuantity(isComplete, drilledAmount);
        }

        public ErrorCodes AdvanceTarget_MineralHarvested(bool isComplete, int harvestedAmount)
        {
            return AdvanceTarget_WithQuantity(isComplete, harvestedAmount);
        }

        public ErrorCodes AdvanceTarget_FetchItem(bool isComplete, int fetched, int locationId)
        {
            MissionLocation location;
            if (!_missionDataCache.GetLocationById(locationId, out location))
            {
                return ErrorCodes.ConsistencyError;
            }

            myMissionInProgress.deliveryWorkLocation = location; //set the location to work with later in spawn reward

            return AdvanceTarget_WithQuantity(isComplete, fetched);
        }


        public ErrorCodes AdvanceTarget_SubmitItem(bool isComplete, int submittedAmount)
        {
            return AdvanceTarget_WithQuantity(isComplete, submittedAmount);
        }

        public ErrorCodes AdvanceTarget_DockIn(Position position, int zoneId)
        {
            var ec = ErrorCodes.NoError;

            //ez meg range-re megy, ellenorizd le, h ez mi %%%
            if (myTarget.ZoneId == zoneId &&
                myTarget.targetPosition.IsInRangeOf2D(position, myTarget.TargetPositionRange))
            {
                completed = true;
                progressCount = 1;

                Logger.Info("dock in target completed. ");
            }
            else
            {
                Logger.Info("nothing to do with dock in target ");
                return ErrorCodes.NothingToDo;
            }

            return ec;
        }


        public ErrorCodes AdvanceTarget_SummonNPCEgg(bool isComplete, int currentProgress)
        {
            return AdvanceTarget_WithQuantity(isComplete, currentProgress);
        }

        #endregion

        public DeliveryHelper CreateDeliveryHelper(int locationId, Character character, int missionId, int targetId, Character assisting, Guid missionGuid)
        {
            var helper = _deliveryHelperFactory();

            helper.definition = myTarget.Definition;
            helper.ProgressCount = progressCount;
            helper.quantity = myTarget.Quantity;
            helper.locationId = locationId;
            helper.missionOwnerCharacter = character;
            helper.missionId = missionId;
            helper.targetId = targetId;
            helper.assisting = assisting;
            helper.missionGuid = missionGuid;

            return helper;
        }

    

      

        public void WriteSuccessInfo()
        {
            var res =
            Db.Query().CommandText("UPDATE dbo.missiontargetsarchive SET successx=@x,successy=@y,successzoneid=@zoneId,successtime=getdate() WHERE missionguid=@guid AND missionid=@missionId AND characterid=@characterId and targetid=@targetId")
                .SetParameter("@x", _successX)
                .SetParameter("@y", _successY)
                .SetParameter("@zoneId", _successZoneId)
                .SetParameter("@guid", myMissionInProgress.missionGuid)
                .SetParameter("@missionId", myMissionInProgress.MissionId)
                .SetParameter("@characterId", myMissionInProgress.character.Id)
                .SetParameter("@targetId", myTarget.id)
                .ExecuteNonQuery();

            if (res != 1)
            {
                Logger.Error("error occured writing the success info in " + this);
            }

            res.ThrowIfNotEqual(1, () => new PerpetuumException(ErrorCodes.SQLUpdateError));

        }

        public void ReadSuccessInfo(IDataRecord record)
        {
            _successX = record.GetValue<int?>("successx");
            _successY = record.GetValue<int?>("successy");
            _successZoneId = record.GetValue<int?>("successzoneid");
            _successTime = record.GetValue<DateTime?>("successtime");
        }

        public double GetRewardFee(bool estimation)
        {
            var trc = new MissionTargetRewardCalculator(myMissionInProgress, this, estimation);
            return trc.CalculateReward(myTarget);

        }

       



        public void WriteSuccessLog()
        {
            if (_successZoneId == null || _successX == null || _successY == null)
            {
                Logger.Error("invalid success info in " + myTarget + " " + myMissionInProgress);
                return;
            }

            if (myMissionInProgress.isTestMode)
            {
                if (_successTime == null) 
                    _successTime = DateTime.Now;

                var si = new SuccessLogInfo()
                {
                    guid = myMissionInProgress.missionGuid,
                    locationEid = myMissionInProgress.myLocation.LocationEid,
                    missionCategory = myMissionInProgress.myMission.missionCategory,
                    targetType = TargetType,
                    x = (int) _successX,
                    y = (int) _successY,
                    zoneId = (int) _successZoneId,
                    eventTime = ((DateTime)_successTime).AddSeconds(myTarget.displayOrder +5) //fake time
                };


                MissionResolveTester.EnqueSuccesLogInfo(si);
                return;
            }


            WriteSuccessLog( (int) _successZoneId, (int) _successX, (int) _successY, myMissionInProgress.missionGuid, TargetType, myMissionInProgress.myLocation.LocationEid, myMissionInProgress.myMission.missionCategory);

        }

        public  static void WriteSuccessLog(int zoneId, int x, int y, Guid guid, MissionTargetType targetType, long locationEid, MissionCategory missionCategory)
        {
            var res = Db.Query().CommandText("INSERT dbo.missiontargetslog ( zoneid, x, y, targettype, guid, locationeid, missioncategory ) VALUES ( @zoneId, @x, @y, @targetType, @guid, @locationeid, @category )")
                .SetParameter("@x", x)
                .SetParameter("@y", y)
                .SetParameter("@zoneId", zoneId)
                .SetParameter("@guid", guid)
                .SetParameter("@targetType", (int) targetType)
                .SetParameter("@locationeid", locationEid)
                .SetParameter("@category", (int)missionCategory)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);

        }

        public void SetSuccessInfo(int zoneId, int x, int y)
        {
            _successX = x;
            _successY = y;
            _successZoneId = zoneId;
            _successTime = DateTime.Now;

        }
    }

    public class SuccessLogInfo
    {
        public int zoneId;
        public int x;
        public int y;
        public Guid guid;
        public MissionTargetType targetType;
        public long locationEid;
        public MissionCategory missionCategory;
        public DateTime? eventTime;

        
        public void AddToInsertList(List<string> insertList)
        {


            var oneLine = string.Format("INSERT dbo.missiontargetslog ( zoneid, x, y, targettype, guid, locationeid, missioncategory {7} ) VALUES ( {0}, {1}, {2},{3}, '{4}', {5},{6} {8} )", zoneId, x, y, (int)targetType, guid.ToString(), locationEid, (int)missionCategory, (eventTime == null) ? "" : ",eventtime", (eventTime == null) ? "" : ",'" + ((DateTime)eventTime).ToString("O").Substring(0, 19) + "'");
            insertList.Add(oneLine);
        }

    }
    
}





