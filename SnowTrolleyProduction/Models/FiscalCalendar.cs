namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class FiscalCalendar
    {
        public System.DateTime Date { get; set; }
        public int FiscalDay { get; set; }
        public int FiscalWeek { get; set; }
        public int FiscalMonth { get; set; }
        public int FiscalQuarter { get; set; }
        public int CalendarDayOfYear { get; set; }
        public int CalendarWeekOfYear { get; set; }
        public int CalendarMonth { get; set; }
        public int CalendarQuarter { get; set; }
        public Nullable<int> FiscalYear { get; set; }
    }
}
