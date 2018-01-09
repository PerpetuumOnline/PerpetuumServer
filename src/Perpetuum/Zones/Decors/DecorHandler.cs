using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Zones.Environments;

namespace Perpetuum.Zones.Decors
{
    public class DecorHandler : IDecorHandler
    {
        private readonly IZone _zone;

        private readonly ConcurrentDictionary<int, DecorDescription> _decors = new ConcurrentDictionary<int, DecorDescription>();

        public DecorHandler(IZone zone)
        {
            _zone = zone;
        }

        public void Initialize()
        {
            try
            {
                var records = Db.Query().CommandText("select * from decor where zoneid=@zoneId")
                                     .SetParameter("@zoneId",_zone.Id)
                                     .Execute();

                foreach (var record in records)
                {
                    var x = record.GetValue<int>("x");
                    var y = record.GetValue<int>("y");
                    var z = record.GetValue<int>("z");

                    var decorDescription = new DecorDescription 
                    {
                        id = record.GetValue<int>("id"),
                        definition = record.GetValue<int>("definition"),
                        quaternionX = record.GetValue<double>("quaternionx"),
                        quaternionY = record.GetValue<double>("quaterniony"),
                        quaternionZ = record.GetValue<double>("quaternionz"),
                        quaternionW = record.GetValue<double>("quaternionw"),
                        position = new Position(x,y,z),
                        scale = record.GetValue<double>("scale"),
                        changed = record.GetValue<bool>("changed"),
                        zoneId = _zone.Id,
                        fadeDistance = record.GetValue<double>("fadedistance"),
                        category = record.GetValue<int>("category"),
                        locked = record.GetValue<bool>("locked")
                    };

                    SetDecor(decorDescription);
                }

                Logger.Info("decor loaded for zone " + _zone.Id);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        public void SetDecor(DecorDescription decorDescription)
        {
            _decors[decorDescription.id] = decorDescription;
        }

        public void DeleteDecor(int decorDescriptionId)
        {
            _decors.Remove(decorDescriptionId);
        }

        public IDictionary<string, object> DecorObjectsToDictionary()
        {
            var counter = 0;
            return (_decors.Values.Where(d => d.changed).Select(d => (object) d.ToDictionary())).ToDictionary(d => "d" + counter++);
        }

        public ErrorCodes InsertDecorSql(DecorDescription decorDescription, out int newDecorId)
        {
            var ec = ErrorCodes.NoError;
            newDecorId = -1;
            try
            {
                newDecorId = Db.Query().CommandText(@"insert decor (definition, quaternionx,quaterniony,quaternionz,quaternionw,zoneid,x,y,z,scale,fadedistance,category) values
                                           (@definition,@quaternionX,@quaternionY,@quaternionZ,@quaternionW,@ZoneID,@x,@y,@z,@scale,@fadeDistance,@category);  select cast(scope_identity() as int)").SetParameter("@definition", decorDescription.definition).SetParameter("@quaternionX", decorDescription.quaternionX).SetParameter("@quaternionY", decorDescription.quaternionY).SetParameter("@quaternionZ", decorDescription.quaternionZ).SetParameter("@quaternionW", decorDescription.quaternionW).SetParameter("@ZoneID", decorDescription.zoneId).SetParameter("@x", decorDescription.position.intX).SetParameter("@y", decorDescription.position.intY).SetParameter("@z", decorDescription.position.intZ).SetParameter("@scale", decorDescription.scale).SetParameter("@fadeDistance", decorDescription.fadeDistance).SetParameter("@category", decorDescription.category)
                        .ExecuteScalar<int>();
            }
            catch (Exception ex)
            {
                Logger.Error("error occured inserting a decor object: " + ex.Message);
                ec = ErrorCodes.SQLInsertError;
            }

            return ec;
        }

        public ErrorCodes UpdateDecorSql(DecorDescription decorDescription)
        {
            var res = Db.Query().CommandText(@"update decor set definition=@definition,
                                        quaternionx=@quaternionX,
                                        quaterniony=@quaternionY,
                                        quaternionz=@quaternionZ,
                                        quaternionw=@quaternionW,
                                        zoneid=@ZoneID,
                                        x=@x,
                                        y=@y,
                                        z=@z,
                                        scale=@scale,
                                        fadedistance=@fadeDistance,
                                        category=@category
                                        where id=@ID").SetParameter("@definition", decorDescription.definition).SetParameter("@quaternionX", decorDescription.quaternionX).SetParameter("@quaternionY", decorDescription.quaternionY).SetParameter("@quaternionZ", decorDescription.quaternionZ).SetParameter("@quaternionW", decorDescription.quaternionW).SetParameter("@ZoneID", decorDescription.zoneId).SetParameter("@x", decorDescription.position.intX).SetParameter("@y", decorDescription.position.intY).SetParameter("@z", decorDescription.position.intZ).SetParameter("@scale", decorDescription.scale).SetParameter("@fadeDistance", decorDescription.fadeDistance).SetParameter("@ID", decorDescription.id).SetParameter("@category", decorDescription.category)
                    .ExecuteNonQuery();

            return (res != 1) ? ErrorCodes.SQLInsertError : ErrorCodes.NoError;
        }

        public ErrorCodes DeleteDecorSql(int decorId)
        {
            var res = Db.Query().CommandText("delete decor where id=@decorId").SetParameter("@decorId",decorId).ExecuteNonQuery();
            return (res != 1) ? ErrorCodes.SQLDeleteError : ErrorCodes.NoError;
        }

        public void SpreadDecorChanges(DecorDescription decorDescription)
        {
            Message.Builder.SetCommand(Commands.DecorUpdate).WithData(new Dictionary<string, object> { { k.description, decorDescription.ToDictionary() } }).ToCharacters(_zone.GetCharacters()).Send();
        }

        public IDictionary<int, DecorDescription> Decors => _decors;

        public void SpreadDecorDelete(int deleteId)
        {
            Message.Builder.SetCommand(Commands.DecorDelete).WithData(new Dictionary<string, object> { { k.ID, deleteId } }).ToCharacters(_zone.GetCharacters()).Send();
        }

        public ErrorCodes SampleDecorEnvironment(int decorId, int range, out Dictionary<string, object> verboseResult)
        {
            ErrorCodes ec;
            EntityEnvironmentDescription environmentDescription;
            verboseResult = new Dictionary<string, object>();

            DecorDescription decorDescription;
            if (!_decors.TryGetValue(decorId, out decorDescription))
                return ErrorCodes.ItemNotFound;

            var turns = decorDescription.FindQuaternionRotation();
            if (turns == -1)
                return ErrorCodes.DecorIsNot90DegreeRotated;

            if (decorDescription.scale != 1.0)
                return ErrorCodes.DecorScaled;

            if ((ec = _zone.Environment.CollectEnvironmentFromPosition(decorDescription.GetServerPosition(), range, turns, out environmentDescription)) != ErrorCodes.NoError)
                return ec;

            if (environmentDescription.blocksTiles == null || environmentDescription.blocksTiles.Count == 0)
            {
                return ErrorCodes.NoError;
            }

            EntityEnvironment.WriteEnvironmentToSql(decorDescription.definition, environmentDescription);
            verboseResult.Add(k.blocks, environmentDescription.blocksTiles.Count);
            return ec;
        }

        public ErrorCodes DrawDecorEnvironment(int decorId)
        {
            DecorDescription decorDescription;
            if (!_decors.TryGetValue(decorId, out decorDescription))
                return ErrorCodes.ItemNotFound;

            var environmentDescription = EntityEnvironment.LoadEnvironmentSql(decorDescription.definition);

            if (environmentDescription.Equals(default(EntityEnvironmentDescription)))
                return ErrorCodes.DefinitionHasNoEnvironment;

            ErrorCodes ec;
            var newTurns = -1;
            var flipX = false;
            var flipY = false;

            if ((ec = decorDescription.FindQuaternionRotationAndMirror(ref newTurns, ref flipX, ref flipY)) != ErrorCodes.NoError)
            {
                return ec;
            }

            _zone.DrawEnvironmentForDecor(decorDescription.GetServerPosition(), environmentDescription, newTurns,flipX, flipY);
            return ErrorCodes.NoError;
        }
    }
}