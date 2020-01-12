using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class TimeOfDay
    {
        //dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Current Year", 0x2CE, 0x02, 0x00, DataChunkName.CURRENT_YEAR);
        //    dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Month", 0x2D7, 0x01, 0x00, DataChunkName.CURRENT_MONTH);
        //    dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Day", 0x2D8, 0x01, 0x00, DataChunkName.CURRENT_DAY);
        //    dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Hour", 0x2D9, 0x01, 0x00, DataChunkName.CURRENT_HOUR);
        //    // 0x2DA is copy of 2D9 for some reason
        //    dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Hour", 0x2DB, 0x01, 0x00, DataChunkName.CURRENT_MINUTE);

        private DataChunk CurrentYearDataChunk;
        private DataChunk CurrentMonthDataChunk;
        private DataChunk CurrentDayDataChunk;
        private DataChunk CurrentHourDataChunk;
        private DataChunk CurrentMinuteDataChunk;

        public TimeOfDay(DataChunk currentYearDataChunk, DataChunk currentMonthDataChunk, DataChunk currentDayDataChunk, DataChunk currentHourDataChunk,
            DataChunk currentMinuteDataChunk)
        {
            CurrentYearDataChunk = currentYearDataChunk;
            CurrentMonthDataChunk = currentMonthDataChunk;
            CurrentDayDataChunk = currentDayDataChunk;
            CurrentHourDataChunk = currentHourDataChunk;
            CurrentMinuteDataChunk = currentMinuteDataChunk;
        }

        public bool IsDayLight
        {
            get
            {
                return (Hour >= 5 && Hour < (8 + 12));
            }
        }

        public string FormattedDate
        {
            get
            {
                return Month + "-" + Day + "-" + Year;
            }
        }

        public string FormattedTime
        {
            get
            {
                string suffix;
                if (Hour < 12) suffix = "AM"; else suffix = "PM";
                return Hour % 12 + ":" + String.Format("{0:D2}", Minute) + " " + suffix;
            }
        }


        public UInt16 Year
        {
            get
            {
                return CurrentYearDataChunk.GetChunkAsUINT16();
            }
            set
            {
                CurrentYearDataChunk.SetChunkAsUINT16(value);
            }
        }

        public byte Month
        {
            get
            {
                return CurrentMonthDataChunk.GetChunkAsByte();
            }
            set
            {
                CurrentMonthDataChunk.SetChunkAsByte(value);
            }
        }

        public byte Day
        {
            get
            {
                return CurrentDayDataChunk.GetChunkAsByte();
            }
            set
            {
                CurrentDayDataChunk.SetChunkAsByte(value);
            }
        }

        public byte Hour
        {
            get
            {
                return CurrentHourDataChunk.GetChunkAsByte();
            }
            set
            {
                Debug.Assert(value >= 0 && value <= 23);
                CurrentHourDataChunk.SetChunkAsByte(value);
            }
        }

        public byte Minute
        {
            get
            {
                return CurrentMinuteDataChunk.GetChunkAsByte();
            }
            set
            {
                Debug.Assert(value >= 0 && value <= 59);
                CurrentMinuteDataChunk.SetChunkAsByte(value);
            }
        }

        public void AdvanceClock(int nMinutes)
        {
            // ensuring that you can't advance more than a day ensures that we can make some time saving assumptions
            if (nMinutes > (60 * 9)) throw new Exception("You can not advance more than 9 hours at a time");

            // if we add the time, and it enters the next hour then we have some work to do
            if (Minute + nMinutes > 59)
            {
                int nExtraMinutes;
                byte nHours = (byte)Math.DivRem(nMinutes, 60, out nExtraMinutes);

                byte newHour = (byte)((Hour + nHours + 1));
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
                    if (nDay > 28)
                    {
                        Day = 1;
                        int nMonth = (byte)(Month + 1);
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
                        Day = (byte)(Day + 1);
                    }
                }
            }
            else
            {
                Minute += (byte)nMinutes;
            }
        }
    }
}
