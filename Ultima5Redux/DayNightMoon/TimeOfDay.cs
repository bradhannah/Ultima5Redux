using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.Data;

namespace Ultima5Redux.DayNightMoon
{
    public class TimeOfDay
    {
        private readonly DataChunk _currentDayDataChunk;
        private readonly DataChunk _currentHourDataChunk;
        private readonly DataChunk _currentMinuteDataChunk;
        private readonly DataChunk _currentMonthDataChunk;
        private readonly DataChunk _currentYearDataChunk;

        /// <summary>
        ///     Dictionary of all change trackers and if time has changed since last check
        /// </summary>
        private readonly Dictionary<int, bool> _timeHasChangedDictionary = new Dictionary<int, bool>();

        /// <summary>
        ///     tracks the total number of registered change trackers
        /// </summary>
        private int _nTotalChangeTrackers;

        /// <summary>
        ///     Constructor. Builds with datachunks from save game file
        /// </summary>
        /// <param name="currentYearDataChunk"></param>
        /// <param name="currentMonthDataChunk"></param>
        /// <param name="currentDayDataChunk"></param>
        /// <param name="currentHourDataChunk"></param>
        /// <param name="currentMinuteDataChunk"></param>
        public TimeOfDay(DataChunk currentYearDataChunk, DataChunk currentMonthDataChunk, DataChunk currentDayDataChunk,
            DataChunk currentHourDataChunk,
            DataChunk currentMinuteDataChunk)
        {
            _currentYearDataChunk = currentYearDataChunk;
            _currentMonthDataChunk = currentMonthDataChunk;
            _currentDayDataChunk = currentDayDataChunk;
            _currentHourDataChunk = currentHourDataChunk;
            _currentMinuteDataChunk = currentMinuteDataChunk;
        }

        /// <summary>
        ///     Gets a string describing the current time of day
        /// </summary>
        /// <returns></returns>
        public string TimeOfDayName
        {
            get
            {
                if (Hour > 5 && Hour < 12) return "morning";
                if (Hour >= 12 && Hour < 17) return "afternoon";
                return "evening";
            }
        }

        public bool IsDayLight => Hour >= 5 && Hour < 8 + 12;
        // ReSharper disable once UnusedMember.Global
        public string FormattedDate => Month + "-" + Day + "-" + Year;

        public string FormattedTime
        {
            get
            {
                string suffix = Hour < 12 ? "AM" : "PM";
                return (Hour % 12 == 0 ? 12 : Hour % 12) + ":" + $"{Minute:D2}" + " " + suffix;
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public ushort Year
        {
            get => _currentYearDataChunk.GetChunkAsUint16();
            set => _currentYearDataChunk.SetChunkAsUint16(value);
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public byte Month
        {
            get => _currentMonthDataChunk.GetChunkAsByte();
            set => _currentMonthDataChunk.SetChunkAsByte(value);
        }

        public byte Day
        {
            get => _currentDayDataChunk.GetChunkAsByte();
            set => _currentDayDataChunk.SetChunkAsByte(value);
        }

        public byte Hour
        {
            get => _currentHourDataChunk.GetChunkAsByte();
            set
            {
                Debug.Assert(value <= 23);
                _currentHourDataChunk.SetChunkAsByte(value);
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public byte Minute
        {
            get => _currentMinuteDataChunk.GetChunkAsByte();
            set
            {
                Debug.Assert(value <= 59);
                _currentMinuteDataChunk.SetChunkAsByte(value);
            }
        }

        /// <summary>
        ///     Registers a change tracker, returning the int handle to it that will need to be stored
        /// </summary>
        /// <returns>the int handler of the change tracker</returns>
        // ReSharper disable once UnusedMember.Global
        public int RegisterChangeTracker()
        {
            int nChangeTracker = _nTotalChangeTrackers++;
            _timeHasChangedDictionary[nChangeTracker] = true;
            return nChangeTracker;
        }

        /// <summary>
        ///     Sets all the flags to indicate if time has or hasn't changed for all "RegisterChangeTracker" registered
        ///     change tracker id
        /// </summary>
        /// <param name="bTimeChangeHappened">has the time changed? (almost always true)</param>
        public void SetAllChangeTrackers(bool bTimeChangeHappened = true)
        {
            for (int i = 0; i < _nTotalChangeTrackers; i++)
            {
                _timeHasChangedDictionary[i] = bTimeChangeHappened;
            }
        }

        /// <summary>
        ///     Has the time changed since the last time we checked with the given change tracker id
        /// </summary>
        /// <param name="nChangeTrackerId">the change tracker id (registered with RegisterChangeTracker)</param>
        /// <returns>true if change has occured, otherwise false</returns>
        // ReSharper disable once UnusedMember.Global
        public bool HasTimeChanged(int nChangeTrackerId)
        {
            bool bTimeChangeOccured = _timeHasChangedDictionary[nChangeTrackerId];
            // we reset it to false to say we saw it until the next change
            _timeHasChangedDictionary[nChangeTrackerId] = false;
            return bTimeChangeOccured;
        }

        /// <summary>
        ///     Advances the clock by certain number of minutes
        /// </summary>
        /// <param name="nMinutes"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void AdvanceClock(int nMinutes)
        {
            const int nDaysInMonth = 28;

            // ensuring that you can't advance more than a day ensures that we can make some time saving assumptions
            if (nMinutes > 60 * 9) throw new Ultima5ReduxException("You can not advance more than 9 hours at a time");

            // if we add the time, and it enters the next hour then we have some work to do
            if (Minute + nMinutes > 59)
            {
                byte nHours = (byte) Math.DivRem(nMinutes, 60, out int nExtraMinutes);

                byte newHour = (byte) (Hour + nHours + 1);
                Minute = (byte) ((Minute + (byte) nExtraMinutes) % 60);

                // if it puts us into a new day
                if (newHour <= 23)
                {
                    Hour = newHour;
                }
                else
                {
                    Hour = (byte) (newHour % 24);
                    // if the day + 1 is more days then we are allow in the month, then restart the days, and go to next month
                    int nDay = (byte) (Day + 1);
                    if (nDay > nDaysInMonth)
                    {
                        Day = 1;
                        int nMonth = (byte) (Month + 1);
                        // if the next month goes beyond 13, then we reset and advance the year
                        if (nMonth > 13)
                        {
                            Month = 1;
                            Year += 1;
                        }
                        else
                        {
                            Month += 1;
                        }
                    }
                    else
                    {
                        Day = (byte) (Day + 1);
                    }
                }
            }
            else
            {
                Minute += (byte) nMinutes;
            }

            // time has changed, so we reset all our change tracking flags
            SetAllChangeTrackers();
        }
    }
}