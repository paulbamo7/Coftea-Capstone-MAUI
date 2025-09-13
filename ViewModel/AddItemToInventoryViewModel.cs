using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class AddItemToInventoryViewModel : ObservableObject
    {
        private readonly Database _database;

        // Form fields
     

        public AddItemToInventoryViewModel()
        {
            _database = new Database(
                host: "localhost",
                database: "coftea_db",
                user: "root",
                password: ""
            );
        }
    }
}
