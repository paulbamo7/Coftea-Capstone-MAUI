using Coftea_Capstone;
using Coftea_Capstone.C_;
using SQLite;

namespace Coftea_Capstone.Pages;

public partial class LoginPage : ContentPage
{
    private readonly Database _database;
    public LoginPage()
    {
        InitializeComponent();
        _database = new Database(App.dbPath);
    }
}