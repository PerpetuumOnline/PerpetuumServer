using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Services.Mail;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.Sessions;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Services.Channels
{
    public class GameAdminCommands
    {
        public GameAdminCommands()
        {

        }

        // obviously everything coming in from the in-game chat is a string.
        // we have to take that string and chop it up, work out what command is being executed
        // then parse/cast/convert arguments as necessary.
        public void ParseAdminCommand(Character sender, string text, IRequest request, Channel channel, ISessionManager sessionManager, ChannelManager channelmanager)
        {
            string[] command = text.Split(new char[] { ',' });

            // channel is not secured. must be secured first.
            if (channel.Type != ChannelType.Admin)
            {

                if (command[0] == "#secure")
                {
                    channel.SetAdmin(true);
                    channel.SendMessageToAll(sessionManager, sender, "Channel Secured.");
                    return;
                }

                channel.SendMessageToAll(sessionManager, sender, "Channel must be secured before sending commands.");
                return;
            }

            if (command[0] == "#shutdown")
            {
                DateTime shutdownin = DateTime.Now;
                int minutes = 1;
                if (!int.TryParse(command[2], out minutes))
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                shutdownin = shutdownin.AddMinutes(minutes);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "message", command[1] },
                    { "date", shutdownin }
                };

                string cmd = string.Format("serverShutDown:relay:{0}", GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#shutdowncancel")
            {
                string cmd = string.Format("serverShutDownCancel:relay:null");
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#jumpto")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int zone);
                err = !int.TryParse(command[2], out int x);
                err = !int.TryParse(command[3], out int y);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "zoneID" , zone },
                    { "x" , x },
                    { "y" , y }
                };

                string cmd = string.Format("jumpAnywhere:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#moveplayer")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int characterID);
                err = !int.TryParse(command[2], out int zoneID);
                err = !int.TryParse(command[3], out int x);
                err = !int.TryParse(command[4], out int y);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                // get the target character's session.
                var charactersession = sessionManager.GetByCharacter(characterID);

                if (charactersession.Character.ZoneId == null)
                {
                    channel.SendMessageToAll(sessionManager, sender, string.Format("ERR: Character with ID {0} does not have a zone. Are they docked?", characterID));
                    return;
                }

                // get destination zone.
                var zone = request.Session.ZoneMgr.GetZone(zoneID);

                if (charactersession.Character.ZoneId == null)
                {
                    channel.SendMessageToAll(sessionManager, sender, string.Format("ERR: Invalid Zone ID {0}", zoneID));
                    return;
                }

                // get a teleporter object to teleport the player.
                TeleportToAnotherZone tp = new TeleportToAnotherZone(zone);

                // we need the player (robot, etc) to teleport on the origin zone
                var player = request.Session.ZoneMgr.GetZone((int)charactersession.Character.ZoneId).GetPlayer(charactersession.Character.ActiveRobotEid);
                //var player = zone.GetPlayer(charactersession.Character.Eid);

                // set the position.
                tp.TargetPosition = new Position(x, y);

                // do it.
                tp.DoTeleportAsync(player);
                tp = null;

                channel.SendMessageToAll(sessionManager, sender, string.Format("Moved Character {0}-{1} to Zone {2} @ {3},{4}", characterID, charactersession.Character.Nick, zone.Id, x, y));
            }
#if DEBUG  
            if (command[0] == "#currentzonecleanobstacleblocking")
            {
                string cmd = string.Format("zoneCleanObstacleBlocking:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzonedrawblockingbyeid")
            {
                bool err = false;
                err = !Int64.TryParse(command[1], out Int64 eid);

                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "eid", eid }
                };

                string cmd = string.Format("zoneDrawBlockingByEid:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneremoveobjectbyeid")
            {
                bool err = false;
                err = !Int64.TryParse(command[1], out Int64 eid);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "target", eid }
                };

                string cmd = string.Format("zoneRemoveObject:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonecreateisland")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int lvl);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "low", lvl }
                };

                string cmd = string.Format("zoneCreateIsland:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneplacewall")
            {
                string cmd = string.Format("zonePlaceWall:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneclearwalls")
            {
                string cmd = string.Format("zoneClearWalls:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneadddecor")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int definition);
                err = !int.TryParse(command[2], out int x);
                err = !int.TryParse(command[3], out int y);
                err = !int.TryParse(command[4], out int z);
                err = !double.TryParse(command[5], out double qx);
                err = !double.TryParse(command[6], out double qy);
                err = !double.TryParse(command[7], out double qz);
                err = !double.TryParse(command[8], out double qw);
                err = !double.TryParse(command[9], out double scale);
                err = !int.TryParse(command[10], out int cat);

                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", x*256 },
                    { "y", y*256 },
                    { "z", z*256 },
                    { "quaternionX", qx },
                    { "quaternionY", qy },
                    { "quaternionZ", qz },
                    { "quaternionW", qw },
                    { "scale", scale },
                    { "category", cat }
                };

                string cmd = string.Format("zoneDecorAdd:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#adddecortolockedtile")
            {

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var terrainLock = player.GetPrimaryLock() as TerrainLock;
                if (terrainLock == null)
                {
                    return;
                }

                double x = terrainLock.Location.X;
                double y = terrainLock.Location.Y;
                double z = terrainLock.Location.Z;

                bool err = !double.TryParse(command[2], out double scale);
                err = !int.TryParse(command[1], out int definition);

                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", (int)x*256 },
                    { "y", (int)y*256 },
                    { "z", (int)z*256 },
                    { "quaternionX", (double)0 },
                    { "quaternionY", (double)0 },
                    { "quaternionZ", (double)0 },
                    { "quaternionW", (double)0 },
                    { "scale", scale },
                    { "category", 1 }
                };

                string cmd = string.Format("zoneDecorAdd:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonedeletedecor")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int idno);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "ID", idno }
                };

                string cmd = string.Format("zoneDecorDelete:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zoneclearlayer")
            {

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "layerName", command[1] }
                };

                string cmd = string.Format("zoneClearLayer:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonesetplantspeed")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int speed);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "speed", speed }
                };

                string cmd = string.Format("zoneSetPlantsSpeed:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonesetplantmode")
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "mode", command[1] }
                };

                string cmd = string.Format("zoneSetPlantsMode:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzonerestoreoriginalgamma")
            {
                string cmd = string.Format("zoneRestoreOriginalGamma:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonedrawblockingbydefinition")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int def);
                int[] defs = new int[1];
                defs[0] = def;

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", defs }
                };

                string cmd = string.Format("zoneDrawBlockingByDefinition:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#addblockingtotiles")
            {
                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        zone.Terrain.Blocks.SetValue(pos, new BlockingInfo() { Obstacle = true });
                        item.Cancel(); // cancel this lock. we processed it.
                    }
                }

                channel.SendMessageToAll(sessionManager, sender, string.Format("Added Blocking To {0} Tiles.", lockedtiles.Count));
            }

            if (command[0] == "#removeblockingfromtiles")
            {
                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        zone.Terrain.Blocks.SetValue(pos, new BlockingInfo() { Obstacle = false });
                        item.Cancel(); // cancel this lock. we processed it.
                    }
                }

                channel.SendMessageToAll(sessionManager, sender, string.Format("Removed Blocking From {0} Tiles.", lockedtiles.Count));
            }

            if (command[0] == "#zonedecorlock")
            {

                bool err = false;
                err = !int.TryParse(command[1], out int id);
                err = !int.TryParse(command[2], out int locked);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "ID", id },
                    { "locked", locked }
                };

                string cmd = string.Format("zoneDecorLock:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));

            }

            if (command[0] == "#zonetileshighway")
            {
                bool.TryParse(command[1], out bool adddelete);
                bool.TryParse(command[2], out bool keeplock);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                        ti.Highway = adddelete;
                        zone.Terrain.Controls.SetValue(pos, ti);
                        if (!keeplock)
                        {
                            item.Cancel(); // cancel this lock. we processed it.
                        }
                    }
                }
                channel.SendMessageToAll(sessionManager, sender, string.Format("Altered state of control layer on {0} Tiles (Highway)", lockedtiles.Count));
            }

            if (command[0] == "#zonetilesconcretea")
            {
                bool.TryParse(command[1], out bool adddelete);
                bool.TryParse(command[2], out bool keeplock);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                        ti.ConcreteA = adddelete;
                        zone.Terrain.Controls.SetValue(pos, ti);
                        if (!keeplock)
                        {
                            item.Cancel(); // cancel this lock. we processed it.
                        }
                    }
                }
                channel.SendMessageToAll(sessionManager, sender, string.Format("Altered state of control layer on {0} Tiles (ConcreteA)", lockedtiles.Count));
            }

            if (command[0] == "#zonetilesconcreteb")
            {

                bool.TryParse(command[1], out bool adddelete);
                bool.TryParse(command[2], out bool keeplock);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                        ti.ConcreteB = adddelete;
                        zone.Terrain.Controls.SetValue(pos, ti);
                        if (!keeplock)
                        {
                            item.Cancel(); // cancel this lock. we processed it.
                        }
                    }
                }
                channel.SendMessageToAll(sessionManager, sender, string.Format("Altered state of control layer on {0} Tiles (ConcreteB)", lockedtiles.Count));
            }

            if (command[0] == "#zonetilesroaming")
            {

                bool.TryParse(command[1], out bool adddelete);
                bool.TryParse(command[2], out bool keeplock);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                        ti.Roaming = adddelete;
                        zone.Terrain.Controls.SetValue(pos, ti);
                        if (!keeplock)
                        {
                            item.Cancel(); // cancel this lock. we processed it.
                        }
                    }
                }
                channel.SendMessageToAll(sessionManager, sender, string.Format("Altered state of control layer on {0} Tiles (Roaming)", lockedtiles.Count));
            }

            if (command[0] == "#zonetilesPBSTerraformProtected")
            {

                bool.TryParse(command[1], out bool adddelete);
                bool.TryParse(command[2], out bool keeplock);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtiles = player.GetLocks();

                using (new TerrainUpdateMonitor(zone))
                {
                    foreach (Lock item in lockedtiles)
                    {
                        Position pos = (item as TerrainLock).Location;
                        TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                        ti.PBSTerraformProtected = adddelete;
                        zone.Terrain.Controls.SetValue(pos, ti);
                        if (!keeplock)
                        {
                            item.Cancel(); // cancel this lock. we processed it.
                        }
                    }
                }
                channel.SendMessageToAll(sessionManager, sender, string.Format("Altered state of control layer on {0} Tiles (PBSTerraformProtected)", lockedtiles.Count));
            }

            if (command[0] == "#zoneislandblock")
            {
                bool err = false;
                var dictionary = new Dictionary<string, object>();
                if (command.Length != 2)
                {
                    channel.SendMessageToAll(sessionManager, sender, "Missing or too many args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                err = !int.TryParse(command[1], out int low);
                if (err)
                {
                    channel.SendMessageToAll(sessionManager, sender, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                dictionary.Add(k.low, low);
                var cmd = string.Format("{0}:zone_{1}:{2}", Commands.ZoneCreateIsland.Text, sender.ZoneId, GenxyConverter.Serialize(dictionary));
                channel.SendMessageToAll(sessionManager, sender, $"Islandblocking command accepted: {dictionary.ToDebugString()} \r\nBlocking... ");
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
                channel.SendMessageToAll(sessionManager, sender, $"Zone water level blocked! ");
                return;
            }

            if (command[0] == "#zonecreategarden")
            {
                bool err = false;
                var dictionary = new Dictionary<string, object>();
                if (command.Length != 3)
                {
                    channel.SendMessageToAll(sessionManager, sender, "Missing or too many args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                err = !int.TryParse(command[1], out int x);
                err = !int.TryParse(command[2], out int y);
                if (err)
                {
                    channel.SendMessageToAll(sessionManager, sender, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                dictionary.Add(k.x, x);
                dictionary.Add(k.y, y);
                var cmd = string.Format("{0}:zone_{1}:{2}", Commands.ZoneCreateGarden.Text, sender.ZoneId, GenxyConverter.Serialize(dictionary));
                channel.SendMessageToAll(sessionManager, sender, $"Garden command accepted: {dictionary.ToDebugString()} \r\nPlanting... ");
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
                channel.SendMessageToAll(sessionManager, sender, $"Garden Created! ");
                return;
            }

            //MissionTestResolve - DEBUG ONLY
            if (command[0] == "#testmissions")
            {
                int.TryParse(command[1], out int charID);
                int.TryParse(command[2], out int zoneID);
                int.TryParse(command[3], out int level);
                int.TryParse(command[4], out int numAttempts);
                int.TryParse(command[5], out int displayFlag);
                int.TryParse(command[6], out int singleFlag);
                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.characterID, charID },
                    { k.zone, zoneID },
                    { k.level, level },
                    { "display", displayFlag },
                    { "attempts", numAttempts },
                    { "single", singleFlag },
                };

                string cmd = string.Format("{0}:relay:{1}", Commands.MissionResolveTest.Text, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));

                channel.SendMessageToAll(sessionManager, sender, string.Format("Running missionresolve test {0}", dictionary.ToDebugString()));
            }

#endif

            if (command[0] == "#giveitem")
            {

                int.TryParse(command[1], out int definition);
                int.TryParse(command[2], out int qty);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "quantity", qty }
                };


                string cmd = string.Format("createItem:relay:{0}", GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));

                channel.SendMessageToAll(sessionManager, sender, string.Format("Gave Item {0} ", definition));
            }


            if (command[0] == "#getlockedtileproperties")
            {

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var lockedtile = player.GetPrimaryLock();

                TerrainControlInfo ti = zone.Terrain.Controls.GetValue((lockedtile as TerrainLock).Location);

                channel.SendMessageToAll(sessionManager, sender, string.Format("Tile at {0},{1} has the following flags..", (lockedtile as TerrainLock).Location.X, (lockedtile as TerrainLock).Location.Y));
                channel.SendMessageToAll(sessionManager, sender, "TerrainControlFlags:");
                foreach (TerrainControlFlags f in Enum.GetValues(typeof(TerrainControlFlags)))
                {
                    if (ti.Flags.HasFlag(f) && f != TerrainControlFlags.Undefined)
                    {
                        channel.SendMessageToAll(sessionManager, sender, string.Format("{0}", f.ToString()));
                    }
                }

                BlockingInfo bi = zone.Terrain.Blocks.GetValue((lockedtile as TerrainLock).Location);

                channel.SendMessageToAll(sessionManager, sender, "BlockingFlags:");
                foreach (BlockingFlags f in Enum.GetValues(typeof(BlockingFlags)))
                {
                    if (bi.Flags.HasFlag(f) && f != BlockingFlags.Undefined)
                    {
                        channel.SendMessageToAll(sessionManager, sender, string.Format("{0}", f.ToString()));
                    }
                }

                PlantInfo pi = zone.Terrain.Plants.GetValue((lockedtile as TerrainLock).Location);

                channel.SendMessageToAll(sessionManager, sender, "PlantType:");
                foreach (PlantType f in Enum.GetValues(typeof(PlantType)))
                {
                    if (pi.type.HasFlag(f) && f != PlantType.NotDefined)
                    {
                        channel.SendMessageToAll(sessionManager, sender, string.Format("{0}", f.ToString()));
                    }
                }

                channel.SendMessageToAll(sessionManager, sender, "GroundType:");
                foreach (GroundType f in Enum.GetValues(typeof(GroundType)))
                {
                    if (pi.groundType.HasFlag(f))
                    {
                        channel.SendMessageToAll(sessionManager, sender, string.Format("{0}", f.ToString()));
                    }
                }

            }

            if (command[0] == "#setvisibility")
            {

                bool.TryParse(command[1], out bool visiblestate);

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                player.HasGMStealth = !visiblestate;

                channel.SendMessageToAll(sessionManager, sender, string.Format("Player {0} visibility is {1}", player.Character.Nick, visiblestate));
            }

            if (command[0] == "#zonedrawstatmap")
            {

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "type", command[1] }
                };

                string cmd = string.Format("zoneDrawStatMap:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }


            if (command[0] == "#listplayersinzone")
            {

                int.TryParse(command[1], out int zoneid);

                channel.SendMessageToAll(sessionManager, sender, string.Format("Players On Zone {0}", zoneid));
                channel.SendMessageToAll(sessionManager, sender, string.Format("  AccountId    CharacterId    Nick    Access Level    Docked?    DockedAt    Position"));
                foreach (Character c in sessionManager.SelectedCharacters.Where(x => x.ZoneId == zoneid))
                {
                    channel.SendMessageToAll(sessionManager, sender, string.Format("   {0}       {1}        {2}        {3}       {4}       {5}      {6}",
                        c.AccountId, c.Id, c.Nick, c.AccessLevel, c.IsDocked, c.GetCurrentDockingBase().Eid, c.GetPlayerRobotFromZone().CurrentPosition));
                }
            }

            if (command[0] == "#countofplayers")
            {
                foreach (IZone z in request.Session.ZoneMgr.Zones)
                {
                    channel.SendMessageToAll(sessionManager, sender, string.Format("Players On Zone {0}: {1}", z.Id, z.Players.ToList().Count));
                }
            }

            if (command[0] == "#unsecure")
            {
                channel.SetAdmin(false);
                channel.SendMessageToAll(sessionManager, sender, "Channel is now public.");
            }

            if (command[0] == "#addtochannel")
            {
                int.TryParse(command[1], out int characterid);

                var c = sessionManager.GetByCharacter(characterid);

                channelmanager.JoinChannel(channel.Name, c.Character, ChannelMemberRole.Operator, string.Empty);

                channel.SendMessageToAll(sessionManager, sender, string.Format("Added character {0} to channel ", c.Character.Nick));

            }

            if (command[0] == "#removefromchannel")
            {
                int.TryParse(command[1], out int characterid);

                var c = sessionManager.GetByCharacter(characterid);

                channelmanager.LeaveChannel(channel.Name, c.Character);

                channel.SendMessageToAll(sessionManager, sender, string.Format("Removed character {0} from channel ", c.Character.Nick));

            }

            if (command[0] == "#listrifts")
            {
                foreach (IZone z in request.Session.ZoneMgr.Zones)
                {
                    var rift = z.Units.OfType<Rift>();
                    foreach (Rift r in rift)
                    {
                        channel.SendMessageToAll(sessionManager, sender, string.Format("Rift - Zone: {0}, Position: ({1})", r.Zone, r.CurrentPosition));
                    }
                }
            }


            if (command[0] == "#flagplayernameoffensive")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int characterID);
                err = !bool.TryParse(command[2], out bool isoffensive);

                var charactersession = sessionManager.GetByCharacter(characterID);
                charactersession.Character.IsOffensiveNick = isoffensive;

                channel.SendMessageToAll(sessionManager, sender, string.Format("Player with nick {0} is offensive:{1}", charactersession.Character.Nick, charactersession.Character.IsOffensiveNick));

            }

            if (command[0] == "#renamecorp")
            {
                string currentCorpName = command[1];
                string desiredCorpName = command[2];
                string desiredCorpNick = command[3];

                Corporation.IsNameOrNickTaken(desiredCorpName, desiredCorpNick).ThrowIfTrue(ErrorCodes.NameTaken);
                var corp = Corporation.GetByName(currentCorpName);
                corp.SetName(desiredCorpName, desiredCorpNick);

                channel.SendMessageToAll(sessionManager, sender, string.Format("Corp with nick {0} changed to: {1} [{2}]", currentCorpName, desiredCorpName, desiredCorpNick));
            }


            //FreeAllLockedEP for account - by request of player
            if (command[0] == "#unlockallep")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int accountID);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.accountID, accountID }
                };

                string cmd = string.Format("{0}:relay:{1}", Commands.ExtensionFreeAllLockedEpCommand.Text, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
                channel.SendMessageToAll(sessionManager, sender, "unlockallep: " + dictionary.ToDebugString());
            }

            //EPBonusCommands
            if (command[0] == "#epbonusset")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int bonusBoost);
                err = !int.TryParse(command[2], out int hours);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.bonus, bonusBoost },
                    { k.duration, hours }
                };

                string cmd = string.Format("{0}:relay:{1}", Commands.EPBonusSet.Text, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
                channel.SendMessageToAll(sessionManager, sender, "EP Bonus Set with command: " + dictionary.ToDebugString());
            }


#if DEBUG
            //SpawnRelics by command
            if (command[0] == "#spawnrelic")
            {
                bool err = false;

                var character = request.Session.Character;
                var zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                var player = zone.GetPlayer(character.ActiveRobotEid);

                var terrainLock = player.GetPrimaryLock() as TerrainLock;

                int x, y, zoneid;

                if (terrainLock == null)
                {
                    if (command.Length != 4)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Bad args");
                        throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                    }
                    err = !int.TryParse(command[1], out int xCommand);
                    err = !int.TryParse(command[2], out int yCommand);
                    err = !int.TryParse(command[3], out int zoneCommand);
                    if (err)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Bad args");
                        throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                    }
                    x = xCommand;
                    y = yCommand;
                    zoneid = zoneCommand;
                    zone = request.Session.ZoneMgr.GetZone((int)zoneid);
                    if (zone == null)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Bad zone id!");
                    }
                }
                else
                {
                    x = terrainLock.Location.intX;
                    y = terrainLock.Location.intY;
                    zoneid = zone.Id;
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "x", x },
                    { "y", y },
                    {"zoneid", zoneid }
                };
                if (zone.RelicManager != null)
                {
                    bool success = zone.RelicManager.ForceSpawnRelicAt(x, y);
                    if (success)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Spawned relic at: " + dictionary.ToDebugString());
                    }
                    else
                    {
                        channel.SendMessageToAll(sessionManager, sender, "FAILED to spawn relic at: " + dictionary.ToDebugString());
                    }
                }
                else
                {
                    channel.SendMessageToAll(sessionManager, sender, "This zone does NOT support relics!");
                }

            }
#endif

            //List all Relics
            if (command[0] == "#listrelics")
            {
                bool err = false;

                var character = request.Session.Character;
                IZone zone = null;

                if (command.Length == 2)
                {
                    err = !int.TryParse(command[1], out int zoneCommand);
                    if (err)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Bad args");
                        throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                    }
                    var zoneid = zoneCommand;
                    zone = request.Session.ZoneMgr.GetZone((int)zoneid);
                }
                else if (character.ZoneId != null)
                {
                    zone = request.Session.ZoneMgr.GetZone((int)character.ZoneId);
                }

                if (zone == null)
                {
                    channel.SendMessageToAll(sessionManager, sender, "Zone not provided or not found");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    {"zoneid", zone.Id }
                };
                if (zone.RelicManager != null)
                {
                    var relicDictList = zone.RelicManager.GetRelicListDictionary();
                    foreach (var dict in relicDictList)
                    {
                        channel.SendMessageToAll(sessionManager, sender, dict.ToDebugString());
                    }
                }
                else
                {
                    channel.SendMessageToAll(sessionManager, sender, "This zone does NOT support relics!");
                }

            }

            if (command[0] == "#savelayers")
            {
                bool err = false;
                var dictionary = new Dictionary<string, object>();
                if (command.Length == 2)
                {
                    err = !int.TryParse(command[1], out int zoneId);
                    if (err)
                    {
                        channel.SendMessageToAll(sessionManager, sender, "Bad args");
                        throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                    }
                    dictionary.Add(k.zoneID, zoneId);
                }
                var cmd = string.Format("{0}:relay:{1}", Commands.ZoneSaveLayer.Text, GenxyConverter.Serialize(dictionary));
                channel.SendMessageToAll(sessionManager, sender, $"SaveLayers command accepted: {dictionary.ToDebugString()} \r\nSaving... ");
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
                channel.SendMessageToAll(sessionManager, sender, $"Layer(s) Saved! ");
            }
        }
    }
}
