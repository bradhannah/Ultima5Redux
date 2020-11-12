using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.DayNightMoon
{
    public class MoonPhaseReferences
    {
        /// <summary>
        ///     All available moon phases
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")] 
        public enum MoonPhases
        {
            NewMoon = 0, CrescentWaxing, FirstQuarter, GibbousWaxing, FullMoon, GibbousWaning, LastQuarter,
            CrescentWaning, NoMoon
        }

        public enum MoonsAndSun { Trammel = 4, Felucca = 8 + 12, Sun = 12 }

        public enum TimeOfDayPhases { Daytime, Nighttime, Sunrise, Sunset }

        /// <summary>
        ///     Standard offset for insetting moon and suns into plan
        /// </summary>
        private const int N_OFFSET_ADJUST = 0x30;

        /// <summary>
        ///     Angle of the Trammel moon
        /// </summary>
        private const double D_TRAMMEL_ANGLE = 16 / 24d * 360d - 90d;

        /// <summary>
        ///     Angle of the Felucca moon
        /// </summary>
        private const double D_FELUCCA_ANGLE = 8 / 24d * 360d - 90d;

        /// <summary>
        ///     Angle of the Sun
        /// </summary>
        private const double D_SUN_ANGLE = 270;

        /// <summary>
        ///     DataChunk that describes the moon phases and which moongate gets into town by town
        /// </summary>
        private readonly DataChunk _moonPhaseChunk;

        /// <summary>
        ///     Build references for moon phases and anything that they may affects
        /// </summary>
        /// <param name="dataOvlReference"></param>
        public MoonPhaseReferences(DataOvlReference dataOvlReference)
        {
            _moonPhaseChunk = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.MOON_PHASES);
        }

        /// <summary>
        ///     Gets the location represented by the moonphase
        /// </summary>
        /// <param name="moonPhase"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public SmallMapReferences.SingleMapReference.Location GetLocationByMoonPhase(MoonPhases moonPhase)
        {
            Debug.Assert(moonPhase >= 0 && (int) moonPhase < 8);
            return (SmallMapReferences.SingleMapReference.Location) (int) moonPhase;
        }

        /// <summary>
        ///     Based on time of day, indicates if ambient lights should be turned on
        /// </summary>
        /// <param name="tod"></param>
        /// <returns>true if lights should be on</returns>
        // ReSharper disable once UnusedMember.Global
        public bool AreAmbientLightsOn(TimeOfDay tod)
        {
            return GetTimeOfDayPhase(tod) != TimeOfDayPhases.Daytime;
        }

        public MoonPhases GetMoonPhasesByTimeOfDay(TimeOfDay timeOfDay, MoonsAndSun moonsAndSun)
        {
            // the value stored is an offset and needs to be adjusted to a zero based index
            int getAdjustedValue(int nValue)
            {
                return nValue - N_OFFSET_ADJUST;
            }

            switch (moonsAndSun)
            {
                case MoonsAndSun.Felucca:
                    return (MoonPhases) getAdjustedValue(_moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1) * 2]);
                case MoonsAndSun.Trammel:
                    return (MoonPhases) getAdjustedValue(_moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1) * 2 + 1]);
                case MoonsAndSun.Sun:
                    return MoonPhases.NoMoon;
                default:
                    throw new Ultima5ReduxException("We have asked for a moon phase but did not met the criteria. " +
                                                    timeOfDay);
            }
        }

        /// <summary>
        ///     Gets moonphase that i will be used by a moongate based on current time of day
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public MoonPhases GetMoonGateMoonPhase(TimeOfDay timeOfDay)
        {
            // the value stored is an offset and needs to be adjusted to a zero based index
            // int getAdjustedValue(int nValue)
            // {
            //     return nValue - N_OFFSET_ADJUST;
            // }

            // we don't have a moon phase in the day time
            if (timeOfDay.IsDayLight) return MoonPhases.NoMoon;

            if (timeOfDay.Hour <= 4)
                return
                    GetMoonPhasesByTimeOfDay(timeOfDay,
                        MoonsAndSun
                            .Felucca); //return (MoonPhases)getAdjustedValue(_moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1)*2]);
            if (timeOfDay.Hour >= 20 && timeOfDay.Hour <= 23)
                return
                    GetMoonPhasesByTimeOfDay(timeOfDay,
                        MoonsAndSun
                            .Trammel); //return (MoonPhases)getAdjustedValue(_moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1)*2 + 1]);

            throw new Ultima5ReduxException("We have asked for a moongate phase but did not met the criteria. " +
                                            timeOfDay);
        }

        /// <summary>
        ///     Returns the general "day phase"
        ///     Used for lighting
        /// </summary>
        /// <param name="tod">current time of day</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static TimeOfDayPhases GetTimeOfDayPhase(TimeOfDay tod)
        {
            const int nSunsetHour = 19;
            const int nSunriseHour = 6;
            if (tod.Hour == nSunsetHour && tod.Minute <= 50) return TimeOfDayPhases.Sunset;
            if (tod.Hour == nSunriseHour && tod.Minute >= 10 && tod.Minute <= 60) return TimeOfDayPhases.Sunrise;
            if (tod.Hour > nSunriseHour && tod.Hour < nSunsetHour) return TimeOfDayPhases.Daytime;
            return TimeOfDayPhases.Nighttime;
        }

        /// <summary>
        ///     With the sun at top (270degrees) this will return the number of degrees to rotate by based
        ///     on the time of day
        /// </summary>
        /// <param name="timeOfDay">current time of day</param>
        /// <returns></returns>
        public static float GetMoonAngle(TimeOfDay timeOfDay)
        {
            float moonPercentageOfDay = (timeOfDay.Hour * 60f + timeOfDay.Minute) / (60f * 24f);
            return moonPercentageOfDay * 360f;
        }

        /// <summary>
        ///     Gives an x,y coordinate of a moon or sun position on a cartesian plan
        /// </summary>
        /// <param name="dAngle">the angle of the entity</param>
        /// <param name="dDiameter">the diameter of the square space</param>
        /// <param name="dOffset">an offset to inset into the circle</param>
        /// <returns></returns>
        private static Point2DFloat GetSunMoonPosition(double dAngle, double dDiameter, double dOffset)
        {
            dAngle %= dDiameter;
            double radius = (dDiameter - dOffset) / 2d;
            double x = radius * Math.Cos(dAngle * (Math.PI / 180));
            double y = radius * Math.Sin(dAngle * (Math.PI / 180));
            return new Point2DFloat((float) x, (float) y);
        }

        /// <summary>
        ///     Based on the moon or sun provided it will provide the x,y coordinate on a plane
        /// </summary>
        /// <param name="moonAndSun"></param>
        /// <param name="dDiameter"></param>
        /// <param name="dOffset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        // ReSharper disable once UnusedMember.Global
        public Point2DFloat GetMoonSunPositionOnCircle(MoonsAndSun moonAndSun, double dDiameter, double dOffset)
        {
            switch (moonAndSun)
            {
                case MoonsAndSun.Trammel:
                    return GetSunMoonPosition(D_TRAMMEL_ANGLE, dDiameter, dOffset);
                case MoonsAndSun.Felucca:
                    return GetSunMoonPosition(D_FELUCCA_ANGLE, dDiameter, dOffset);
                case MoonsAndSun.Sun:
                    return GetSunMoonPosition(D_SUN_ANGLE, dDiameter, dOffset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(moonAndSun), moonAndSun, null);
            }
        }
    }
}