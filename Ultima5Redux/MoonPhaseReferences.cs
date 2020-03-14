using System;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class MoonPhaseReferences
    {
        private readonly DataChunk moonPhaseChunk;
        private const int nOffsetAdjust = -30; 
        
        // private const int TRAMMEL_OFFSET = 4;
        // private const int FELUCCA_OFFSET = 8 + 12;
        // private const int SUN_OFFSET = 12;
        
        public enum MoonsAndSun { Trammel = 4, Felucca = 8 + 12, Sun = 12 }

        public enum TimeOfDayPhases { Daytime, Nighttime, Sunrise, Sunset }
        
        /// <summary>
        /// All available moon phases
        /// </summary>
        public enum MoonPhases { NewMoon = 0, CrescentWaxing, FirstQuarter, GibbousWaxing, FullMoon, GibbousWaning, LastQuarter, CrescentWaning, NoMoon }
        
        /// <summary>
        /// Build references for moon phases and anything that they may affects 
        /// </summary>
        /// <param name="dataOvlReference"></param>
        public MoonPhaseReferences(DataOvlReference dataOvlReference)
        {
            this.moonPhaseChunk = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.MOON_PHASES);
        }

        /// <summary>
        /// Gets the location represented by the moonphase
        /// </summary>
        /// <param name="moonPhase"></param>
        /// <returns></returns>
        public SmallMapReferences.SingleMapReference.Location GetLocationByMoonPhase(MoonPhases moonPhase)
        {
            Debug.Assert(moonPhase >= 0 && (int)moonPhase < 8);
            return ((SmallMapReferences.SingleMapReference.Location) (int) moonPhase);
        }

        /// <summary>
        /// Gets moonphase that i will be used by a moongate based on current time of day
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public MoonPhases GetMoonGateMoonPhase(TimeOfDay timeOfDay)
        {
            // the value stored is an offset and needs to be adjusted to a zero based index
            int getAdjustedValue(int nValue)
            {
                return nValue - nOffsetAdjust;
            }
            
            // we don't have a moon phase in the day time
            if (timeOfDay.IsDayLight) return MoonPhases.NoMoon;

            if (timeOfDay.Hour <= 4) return (MoonPhases)getAdjustedValue(moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1)*2]);
            if (timeOfDay.Hour >= 20 && timeOfDay.Hour <= 23) return (MoonPhases)getAdjustedValue(moonPhaseChunk.GetAsByteList()[(timeOfDay.Day - 1)*2 + 1]);
            
            throw new Ultima5ReduxException("We have asked for a moongate phase but did not met the criteria. "+timeOfDay);
        }

        public TimeOfDayPhases GetTimeOfDayPhase(TimeOfDay tod)
        {
            const int nSunsetHour = 19;
            const int nSunriseHour = 6;
            if (tod.Hour == nSunsetHour && (tod.Minute <= 50)) return TimeOfDayPhases.Sunrise;
            if (tod.Hour == nSunriseHour && (tod.Minute >= 10 && tod.Minute <= 60)) return TimeOfDayPhases.Sunrise;
            if (tod.Hour > nSunriseHour && tod.Hour < nSunsetHour) return TimeOfDayPhases.Daytime;
            return TimeOfDayPhases.Nighttime;
        }

        public float GetMoonAngle(TimeOfDay timeOfDay)
        {
            float moonPercentageOfDay = ((float)timeOfDay.Hour * 60f + (float)timeOfDay.Minute) / (60f * 24f);
            return moonPercentageOfDay * 360f;
        }
        
        public int GetMoonsOrSunOffset(MoonsAndSun moonAndSun)
        {
            return (int) moonAndSun;
        }
        
        private Point2DFloat getSunMoonPosition(double dAngle, double dDiameter, double dOffset)
        {
            //const double dOffset = 35;
            dAngle %= dDiameter;
            double radius = (dDiameter - dOffset) / 2d;
            double x = radius * Math.Cos(dAngle * (Math.PI / 180));
            double y = radius * Math.Sin(dAngle * (Math.PI / 180));
            return new Point2DFloat((float)x, (float)y);
        }

        private const double dTrammelAngle = (16 / 24d) * 360d - 90d;
        private const double dFeluccaAngle = (8 / 24d) * 360d - 90d;
        private const double dSunAngle = 270; 
        
        public Point2DFloat GetMoonSunPositionOnCircle(MoonsAndSun moonAndSun, double dDiameter, double dOffset)
        {
            switch (moonAndSun)
            {
                case MoonsAndSun.Trammel:
                    return (getSunMoonPosition(dTrammelAngle, dDiameter, dOffset));
                case MoonsAndSun.Felucca:
                    return (getSunMoonPosition(dFeluccaAngle, dDiameter, dOffset));
                case MoonsAndSun.Sun:
                    return (getSunMoonPosition(dSunAngle, dDiameter, dOffset));
                default:
                    throw new ArgumentOutOfRangeException(nameof(moonAndSun), moonAndSun, null);
            }
        }
    }
}