using System.Diagnostics;

namespace Ultima5Redux
{
    public class MoonPhaseReferences
    {
        private DataChunk moonPhaseChunk;
        private const int nOffsetAdjust = -30; 
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
        
    }
}