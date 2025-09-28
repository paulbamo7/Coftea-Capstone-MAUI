using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
