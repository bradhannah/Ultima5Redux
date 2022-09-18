using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.DayNightMoon
{
    public class TimeOfDay
    {
        public const int N_DAYS_IN_MONTH = 28;
        public const int N_MONTHS_PER_YEAR = 13;

        /// <summary>
        ///     Dictionary of all change trackers and if time has changed since last check
        /// </summary>
        [DataMember(Name = "TimeHasChangedDictionary")]
        private readonly Dictionary<Guid, bool> _timeHasChangedDictionary = new();

        /// <summary>
        ///     tracks the total number of registered change trackers
        /// </summary>
        [DataMember(Name = "TotalChangeTrackers")]
        private int _nTotalChangeTrackers;

        [DataMember] public byte Day { get; set; }

        [DataMember]
        public byte Hour
        {
            get => _nHour;
            set
            {
                Debug.Assert(value <= 23);
                _nHour = value;
            }
        }

        [DataMember]
        public byte Minute
        {
            get => _nMinute;
            set
            {
                Debug.Assert(value <= 59);
                _nMinute = value;
            }
        }

        [DataMember] public byte Month { get; set; }

        /// <summary>
        ///     Tick represents a number that increases every time the clock is incremented
        ///     but it does not represent the time of day or number of turns necessarily
        /// </summary>
        [DataMember]
        public int Tick { get; set; }

        [DataMember] public ushort Year { get; set; }

        [IgnoreDataMember] private byte _nHour;
        [IgnoreDataMember] private byte _nMinute;

        // ReSharper disable once UnusedMember.Global
        [IgnoreDataMember] public string FormattedDate => $"{Month} - {Day} - {Year}";

        [IgnoreDataMember]
        public string FormattedTime
        {
            get
            {
                string suffix = Hour < 12 ? "AM" : "PM";
                int nHour = Hour % 12 == 0 ? 12 : Hour % 12;
                return $"{nHour}:{Minute:D2} {suffix}";
            }
        }

        [IgnoreDataMember] public bool IsDayLight => Hour >= 5 && Hour < 8 + 12;

        [IgnoreDataMember]
        public int MinutesSinceBeginning => Minute + Hour * 60 + Day * N_DAYS_IN_MONTH +
                                            Month * N_DAYS_IN_MONTH * 24 * 60
                                            + Year * N_MONTHS_PER_YEAR * N_DAYS_IN_MONTH * 24 * 60;

        /// <summary>
        ///     Gets a string describing the current time of day
        /// </summary>
        /// <returns></returns>
        [IgnoreDataMember]
        public string TimeOfDayName
        {
            get
            {
                if (Hour is > 5 and < 12) return "morning";
                if (Hour is >= 12 and < 17) return "afternoon";
                return "evening";
            }
        }

        [JsonConstructor] private TimeOfDay()
        {
        }

        /// <summary>
        ///     Constructor. Builds with datachunks from save game file
        /// </summary>
        /// <param name="currentYearDataChunk"></param>
        /// <param name="currentMonthDataChunk"></param>
        /// <param name="currentDayDataChunk"></param>
        /// <param name="currentHourDataChunk"></param>
        /// <param name="currentMinuteDataChunk"></param>
        public TimeOfDay(DataChunk currentYearDataChunk, DataChunk currentMonthDataChunk, DataChunk currentDayDataChunk,
            DataChunk currentHourDataChunk, DataChunk currentMinuteDataChunk) : this(
            currentYearDataChunk.GetChunkAsUint16(),
            currentMonthDataChunk.GetChunkAsByte(),
            currentDayDataChunk.GetChunkAsByte(),
            currentHourDataChunk.GetChunkAsByte(),
            currentMinuteDataChunk.GetChunkAsByte())
        {
        }

        public TimeOfDay(ushort year, ushort month, ushort day, ushort hour, ushort minute)
        {
            if (month > N_MONTHS_PER_YEAR)
                throw new Ultima5ReduxException($"Month value: {month} higher than {N_MONTHS_PER_YEAR}");
            if (day > N_DAYS_IN_MONTH)
                throw new Ultima5ReduxException($"Day value: {day} higher than {N_DAYS_IN_MONTH}");
            if (hour > 23)
                throw new Ultima5ReduxException($"Hour value: {hour} higher than 23");
            if (minute > 59)
                throw new Ultima5ReduxException($"Minute value: {hour} higher than 59");

            Year = year;
            Month = (byte)month;
            Day = (byte)day;
            Hour = (byte)hour;
            Minute = (byte)minute;
        }

        /// <summary>
        ///     Advances the clock by certain number of minutes
        /// </summary>
        /// <param name="nMinutes"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void AdvanceClock(int nMinutes)
        {
            Tick++;

            // ensuring that you can't advance more than a day ensures that we can make some time saving assumptions
            if (nMinutes > 60 * 9) throw new Ultima5ReduxException("You can not advance more than 9 hours at a time");

            // if we add the time, and it enters the next hour then we have some work to do
            if (Minute + nMinutes > 59)
            {
                byte nHours = (byte)Math.DivRem(nMinutes, 60, out int nExtraMinutes);

                byte newHour = (byte)(Hour + nHours + 1);
                Minute = (byte)((Minute + (byte)nExtraMinutes) % 60);

                // if it puts us into a new day
                if (newHour <= 23)
                {
                    Hour = newHour;
                }
                else
                {
                    Hour = (byte)(newHour % 24);
                    // if the day + 1 is more days then we are allow in the month, then restart the days, and go to next month
                    int nDay = (byte)(Day + 1);
                    if (nDay > N_DAYS_IN_MONTH)
                    {
                        Day = 1;
                        int nMonth = (byte)(Month + 1);
                        // if the next month goes beyond 13, then we reset and advance the year
                        if (nMonth > N_MONTHS_PER_YEAR)
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
                        Day = (byte)(Day + 1);
                    }
                }
            }
            else
            {
                Minute += (byte)nMinutes;
            }

            // time has changed, so we reset all our change tracking flags
            SetAllChangeTrackers();
        }

        public TimeOfDay Copy()
        {
            var tod = new TimeOfDay
            {
                Year = Year,
                Month = Month,
                Day = Day,
                Hour = Hour,
                Minute = Minute
            };
            return tod;
        }

        public void DeRegisterChangeTracker(Guid changeTrackerId)
        {
            if (!IsTimeChangeTrackerIdValid(changeTrackerId)) return;

            _timeHasChangedDictionary.Remove(changeTrackerId);
            _nTotalChangeTrackers--;
        }

        /// <summary>
        ///     Has the time changed since the last time we checked with the given change tracker id
        /// </summary>
        /// <param name="changeTrackerId">the change tracker id (registered with RegisterChangeTracker)</param>
        /// <returns>true if change has occured, otherwise false</returns>
        // ReSharper disable once UnusedMember.Global
        public bool HasTimeChanged(Guid changeTrackerId)
        {
            if (!IsTimeChangeTrackerIdValid(changeTrackerId))
                throw new Ultima5ReduxException(
                    "Looked up time change tracking but it didn't exist! " + changeTrackerId);
            bool bTimeChangeOccured = _timeHasChangedDictionary[changeTrackerId];
            // we reset it to false to say we saw it until the next change
            _timeHasChangedDictionary[changeTrackerId] = false;
            return bTimeChangeOccured;
        }

        public bool IsSameTime(TimeOfDay tod)
        {
            if (tod == null) return false;
            return Year == tod.Year && Month == tod.Month && Day == tod.Day && Hour == tod.Hour &&
                   Minute == tod.Minute;
        }

        public bool IsTimeChangeTrackerIdValid(Guid changeTrackerId) =>
            _timeHasChangedDictionary.ContainsKey(changeTrackerId);

        /// <summary>
        ///     Registers a change tracker, returning the int handle to it that will need to be stored
        /// </summary>
        /// <returns>the int handler of the change tracker</returns>
        // ReSharper disable once UnusedMember.Global
        public Guid RegisterChangeTracker()
        {
            var changeTrackerId = Guid.NewGuid();
            _timeHasChangedDictionary[changeTrackerId] = true;
            return changeTrackerId;
        }

        /// <summary>
        ///     Sets all the flags to indicate if time has or hasn't changed for all "RegisterChangeTracker" registered
        ///     change tracker id
        /// </summary>
        /// <param name="bTimeChangeHappened">has the time changed? (almost always true)</param>
        public void SetAllChangeTrackers(bool bTimeChangeHappened = true)
        {
            Guid[] guids = new Guid[_timeHasChangedDictionary.Keys.Count];

            _timeHasChangedDictionary.Keys.CopyTo(guids, 0);
            foreach (Guid changeTrackerId in guids)
            {
                _timeHasChangedDictionary[changeTrackerId] = bTimeChangeHappened;
            }
        }
    }
}