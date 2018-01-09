using System;
using System.Collections.Generic;

namespace Perpetuum.Zones.Decors
{
    public class DecorDescription
    {
        public int id;
        public int definition;
        public int zoneId;
        public Position position;   //shifted
        public double quaternionX;
        public double quaternionY;
        public double quaternionZ;
        public double quaternionW;
        public double scale;
        public bool changed;
        public double fadeDistance;
        public int category;
        public bool locked;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID, id},
                {k.definition, definition},
                {k.zoneID, zoneId},
                {k.position, position},
                {k.scale, scale},
                {k.quaternionX, quaternionX},
                {k.quaternionY, quaternionY},
                {k.quaternionZ, quaternionZ},
                {k.quaternionW, quaternionW},
                {k.fadeDistance, fadeDistance},
                {k.category, category},
                {k.locked, locked}
            };
        }

        //z => z*4
        public Position GetServerPosition()
        {
            return new Position(position.X / 256.0, position.Y / 256.0, position.Z / 64.0);
        }

        public Position GetHomogeneousPosition()
        {
            return new Position(position.X / 256.0, position.Y / 256.0, position.Z / 256.0);
        }

        public int FindQuaternionRotation()
        {
            if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - 1.0) < double.Epsilon)
            {
                return 0;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
            {
                return 1;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - 1.0) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
            {
                return 2;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
            {
                return 3;
            }

            if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - (-1.0)) < double.Epsilon)
            {
                return 4;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
            {
                return 5;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - (-1.0)) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
            {
                return 6;
            }

            if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
            {
                return 7;
            }

            return -1;
        }

        public ErrorCodes FindQuaternionRotationAndMirror( ref int turns, ref bool flipX, ref bool flipY)
        {
            if (!(Math.Abs(scale - 1) < double.Epsilon || Math.Abs(scale - (-1)) < double.Epsilon)) return ErrorCodes.DecorScaled;

            turns = -1;
            if (Math.Abs(scale - 1) < double.Epsilon)
            {

                // no flip

                if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - 1.0) < double.Epsilon)
                {
                    turns = 0;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;
                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
                {
                    turns = 1;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 1.0) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 2;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
                {
                    turns = 3;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - (-1.0)) < double.Epsilon)
                {
                    turns = 4;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
                {
                    turns = 5;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;



                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-1.0)) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 6;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
                {
                    turns = 7;
                    flipX = false;
                    flipY = false;
                    return ErrorCodes.NoError;

                }


                //-------------------- flipXY

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 1.0) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 0;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;
                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
                {
                    turns = 1;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - (-1.0)) < double.Epsilon)
                {
                    turns = 2;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - (-0.707107)) < double.Epsilon)
                {
                    turns = 3;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-1.0)) < double.Epsilon && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 4;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - (-0.707107)) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
                {
                    turns = 5;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;



                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && quaternionZ.IsZero() && Math.Abs(quaternionW - 1.0) < double.Epsilon)
                {
                    turns = 6;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && Math.Abs(quaternionY - 0.707107) < double.Epsilon && quaternionZ.IsZero() && Math.Abs(quaternionW - 0.707107) < double.Epsilon)
                {
                    turns = 7;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;

                }



            }


            if (Math.Abs(scale - (-1)) < double.Epsilon)
            {

                //-------------------- flipX

                if (Math.Abs(quaternionX - 1.0) < double.Epsilon && quaternionY.IsZero() && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 0;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;
                }

                if (Math.Abs(quaternionX - 0.707107) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - (-0.707107)) < double.Epsilon && Math.Abs(quaternionW) < double.Epsilon)
                {
                    turns = 1;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && Math.Abs(quaternionZ - (-1.0)) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 2;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - (-0.707107)) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - (-0.707107)) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 3;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - (-1.0)) < double.Epsilon && quaternionY.IsZero() && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 4;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - (-0.707107)) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - 0.707107) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 5;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;



                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && Math.Abs(quaternionZ - 1.0) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 6;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - 0.707107) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - 0.707107) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 7;
                    flipX = true;
                    flipY = false;
                    return ErrorCodes.NoError;

                }

                //------- FlipY


                if (quaternionX.IsZero() && quaternionY.IsZero() && Math.Abs(quaternionZ - 1.0) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 0;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;
                }

                if (Math.Abs(quaternionX - 0.707107) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - 0.707107) < double.Epsilon && Math.Abs(quaternionW) < double.Epsilon)
                {
                    turns = 1;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - 1.0) < double.Epsilon && quaternionY.IsZero() && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 2;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - 0.707107) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - (-0.707107)) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 3;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;


                }

                if (quaternionX.IsZero() && quaternionY.IsZero() && Math.Abs(quaternionZ - (-1.0)) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 4;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - (-0.707107)) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - (-0.707107)) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 5;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;



                }

                if (Math.Abs(quaternionX - (-1.0)) < double.Epsilon && quaternionY.IsZero() && quaternionZ.IsZero() && quaternionW.IsZero())
                {
                    turns = 6;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;


                }

                if (Math.Abs(quaternionX - (-0.707107)) < double.Epsilon && quaternionY.IsZero() && Math.Abs(quaternionZ - 0.707107) < double.Epsilon && quaternionW.IsZero())
                {
                    turns = 7;
                    flipX = false;
                    flipY = true;
                    return ErrorCodes.NoError;

                }

            }



            return ErrorCodes.DecorIsRotated;
        }
    }
}