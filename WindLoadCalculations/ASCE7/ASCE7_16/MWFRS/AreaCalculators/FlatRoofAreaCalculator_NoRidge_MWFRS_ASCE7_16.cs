using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class FlatRoofAreaCalculator_NoRidge_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public FlatRoofAreaCalculator_NoRidge_MWFRS_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }



        public override void ComputeEffectiveWindAreas()
        {
            //if (optionalParams == null) return;

            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;

            // the divisions of the flat roof or parallel to ridge for theta <= 10 deg
            double offset1 = 0.5 * h;  
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;


            if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
            {
                // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
                // assumes wind is blowing from the left (west) to the right (east)
                // D---p21---p22---p23---C
                // | 4  | 3  |  2   |  1 |
                // E===p11===p12===p13===F  Ridge "="
                // | 4  | 3  |  2   |  1 |
                // A---p1----p2----p3----B
                Point A = new Point(0, 0);
                Point B = new Point(building_length, 0);
                Point C = new Point(building_length, building_width);
                Point D = new Point(0, building_width);
                Point E = new Point(0, 0.5 * building_width);
                Point F = new Point(building_length, 0.5 * building_width);

                Point p1, p2, p3, p11, p12, p13, p21, p22, p23;

                // Zone 4
                if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
                {
                    p1 = new Point(offset1, 0);
                    p11 = new Point(offset1, 0.5 * building_width);
                    p21 = new Point(offset1, building_width);

                    effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, E }, null));
                    effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, p11, p21, D }, null));
                }
                else
                {
                    // doesn't fit in building
                    effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, F, E }, null));
                    effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, F, C, D }, null));
                    return;
                }

                // Zone 3
                if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
                {
                    p2 = new Point(offset2, 0);
                    p12 = new Point(offset2, 0.5 * building_width);
                    p22 = new Point(offset2, building_width);

                    effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, p12, p22, p21 }, null));
                }
                else
                {
                    // doesn't fit in building -- extend end of zone to end of building
                    effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, B, F, p11 }, null));
                    effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, F, C, p21 }, null));
                    return;
                }

                // Zone 2 and Zone 1
                if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                {
                    p3 = new Point(offset3, 0);
                    p13 = new Point(offset3, 0.5 * building_width);
                    p23 = new Point(offset3, building_width);

                    // Zone 2
                    effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, p13, p23, p22 }, null));

                    // Zone 1
                    effWindAreas.Add(7, new EffectiveWindArea("Zone1", new List<Point> { p3, B, F, p13 }, null));
                    effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p13, F, C, p23 }, null));
                }
                else
                {
                    // Zone 2
                    effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, B, F, p12 }, null));
                    effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, F, C, p22 }, null));
                    return;
                }
            }
                
               
            // Note: Ridge can be in any of the four zones -- but Flat roofs don't care about the ridge
            else
            {
                // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
                // D---p11---p12-- R1-----p13----C
                // |    |    |     ||      |     |
                // |    |    |     ||      |     |
                // |    |    |     ||      |     |
                // | 4  | 3  |  2a ||  2b  |  1  |
                // A---p1----p2---R2------p3-----B
                //  
                //  Ridge = "="
                Point A = new Point(0, 0);
                Point B = new Point(building_length, 0);
                Point C = new Point(building_length, building_width);
                Point D = new Point(0, building_width);

                // set the ridge to the right most points on the roof -- so that its not within any of the possible roof regions.
                Point R1;
                Point R2;
                double ridge_position;

                if (buildingData.RoofTypeIsSloped())
                {
                    // and if we are hipped or sloped, move it back to the middle of the building
                    R1 = new Point(0.5 * building_length, 0);
                    R2 = new Point(0.5 * building_length, building_width);
                    ridge_position = 0.5 * building_length;
                } else
                {
                    R1 = B;
                    R2 = C;
                    ridge_position = building_length;
                }

                Point p1, p2, p3, p11, p12, p13;
                bool ridge_already_found = false;

                // Zone 4
                // is Zone4 fully fit in the building?
                if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
                {
                    // yes
                    p1 = new Point(offset1, 0);
                    p11 = new Point(offset1, building_width);

                    // now check if the ridge is in this region
                    if (IsValidRectangle_BuildingLength(ridge_position, building_width, offset1))
                    {
                        // no draw the whole region without splitting
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, D }, null));

                    } else
                    {
                        // yes, so split the whole area
                        ridge_already_found = true;

                        // check if the ridge points are the same as p1 and p11
                        if (R1 == p1 || R2 == p11)
                        {
                            // Ridge occurs at p1 or p11 so no need to split
                            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, R1, R2, D }, null));
                        }
                        else
                        {
                            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, R1, R2, D }, null));
                            effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { R1, p1, p11, R2 }, null));
                        }

                    }
                } else
                {
                    p1 = new Point(building_length, 0);
                    p11 = new Point(building_length, building_width);

                    // now check if the ridge is in this region
                    if (IsValidRectangle_BuildingLength(ridge_position, building_width, offset1))
                    {
                        // no draw the whole region without splitting
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, C, D }, null));
                    }
                    else
                    {
                        // yes, so split the whole area
                        ridge_already_found = true;

                        // check if the ridge points are the same as p1 and p11
                        if (R1 == p1 || R2 == p11)
                        {
                            // Ridge occurs at p1 or p11 so no need to split
                            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, C, D }, null));
                        }
                        else
                        {
                            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, R1, R2, D }, null));
                            effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { R1, B, C, R2 }, null));
                        }
                    }
                    return; // we are done
                }

                // Zone 3
                // is Zone4 fully fit in the building?
                if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
                {
                    // yes
                    p2 = new Point(offset2, 0);
                    p12 = new Point(offset2, building_width);

                    if (ridge_already_found)
                    {
                        effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                    }
                    // ridge hasn't been located yet
                    else
                    {
                        // now check if the ridge is in this region
                        if (IsValidRectangle_BuildingLength(ridge_position, building_width, offset2))
                        {
                            // yes, so split the whole area
                            ridge_already_found = true;
                            // no draw the whole region without splitting
                            effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                        }
                        else
                        {
                            // no draw the whole region without splitting
                            // check if the ridge points are the same as p1 and p11
                            if (R1 == p2 || R2 == p12)
                            {
                                // Ridge occurs at p1 or p11 so no need to split
                                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, R1, R2, p11 }, null));
                            }
                            else
                            {
                                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, R1, R2, p11 }, null));
                                effWindAreas.Add(11, new EffectiveWindArea("Zone3", new List<Point> { R1, p2, p12, R2 }, null));
                            }
                        }
                    }
                }
                // doesn't fit within the building
                else
                {
                    // yes
                    p2 = new Point(building_length, 0);
                    p12 = new Point(building_length, building_width);

                    if (ridge_already_found)
                    {
                        effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, B, C, p11 }, null));
                    }
                    // ridge hasn't been located yet
                    else
                    {
                        // now check if the ridge is in this region
                        if (IsValidRectangle_BuildingLength(ridge_position, building_width, offset2))
                        {
                            ridge_already_found = true;
                            // no draw the whole region without splitting
                            effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, B, C, p11 }, null));
                        }
                        else
                        {
                            // yes, so split the whole area
                            if (R1 == p2 || R2 == p12)
                            {
                                // Ridge occurs at p2 or p12 so no need to split
                                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, R1, R2, p11 }, null));
                            }
                            else
                            {
                                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p1, R1, R2, p11 }, null));
                                effWindAreas.Add(11, new EffectiveWindArea("Zone3", new List<Point> { R1, B, C, R2 }, null));
                            }

                        }
                    }
                    return;  // we are done
                }

                // Zone 2 and 1
                // is ridge in Zone2
                if (ridge_already_found)
                {
                    // Does 2 fit in the building?
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                    {
                        p3 = new Point(offset3, 0);
                        p13 = new Point(offset3, building_width);

                        effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                        effWindAreas.Add(30, new EffectiveWindArea("Zone1", new List<Point> { p3, B, C, p13 }, null));
                    }
                    else
                    {
                        // no 2 doesnt fit and so theres no zone 1 on this roof
                        effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, B, C, p12 }, null));
                        return;
                    }
                }
                else
                {
                    p3 = new Point(offset3, 0);
                    p13 = new Point(offset3, building_width);

                    // is ridge in 2?
                    if (IsValidRectangle_BuildingLength(ridge_position, building_width, offset3))
                    {
                        // no ridge is not in 2 -- and if we are here then it must be in 1 if it exists


                        if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                        {
                            effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));

                            // yes, so split the whole area
                            if (R1 == p3 || R2 == p13)
                            {
                                // Ridge occurs at p3 or p13 so no need to split
                                effWindAreas.Add(30, new EffectiveWindArea("Zone1", new List<Point> { p3, R1, R2, p13 }, null));
                            }
                            else
                            {
                                effWindAreas.Add(30, new EffectiveWindArea("Zone1", new List<Point> { p3, R1, R2, p13 }, null));
                                effWindAreas.Add(31, new EffectiveWindArea("Zone1", new List<Point> { R1, B, C, R2 }, null));
                            }

                        }
                        else
                        {
                            // 1 doesnt exist and 2 takes up remainder of building
                            effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, B, C, p12 }, null));
                            return;
                        }
                    }
                    else
                    {
                        // no its not in 2 -- and if we are here then it must be in 1 if it exists
                        // first does 2 fit in the building?
                        if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                        {
                            // yes it fits
                            if (R1 == p3 || R2 == p13)
                            {
                                // Ridge occurs at p3 or p13 so no need to split
                                effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, R1, R2, p12 }, null));
                            }
                            else
                            {
                                effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, R1, R2, p12 }, null));
                                effWindAreas.Add(30, new EffectiveWindArea("Zone2", new List<Point> { R1, p3, p13, R2 }, null));
                            }
                            
                            effWindAreas.Add(31, new EffectiveWindArea("Zone1", new List<Point> { p3, B, C, p13 }, null));
                        }
                        else
                        {
                            // 1 doesnt exist and 2 takes up remainder of building
                            effWindAreas.Add(20, new EffectiveWindArea("Zone2", new List<Point> { p2, B, C, p12 }, null));
                            return;
                        }
                    }
                }
            }
        }
    }
}
