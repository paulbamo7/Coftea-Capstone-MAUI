using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;

namespace Coftea_Capstone.Models
{
    public class SalesReportPageModel : ObservableObject
    {
        [PrimaryKey, NotNull]
        public int ReportID { get; set; }

        [NotNull]
        public int OrderItemID { get; set; }
        [NotNull]
        public DateTime ReportDate { get; set; }
        [NotNull]
        public int TotalOrder { get; set; }
    }
}
