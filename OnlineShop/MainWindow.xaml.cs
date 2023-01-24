using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OnlineShop;

public partial class MainWindow : Window
{
    SqlConnection? connection = null;
    SqlDataAdapter? adapter = null;
    DataSet? dataSet = null;

    public MainWindow()
    {
        InitializeComponent();
        Configuration();
    }

    private void Configuration()
    {
        var conStr = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build()
                    .GetConnectionString("OnlineShopConnectionSting");

        connection = new SqlConnection(conStr);
        adapter = new SqlDataAdapter("SELECT * FROM Products; SELECT * FROM Categories; SELECT * FROM Ratings", connection);
        dataSet = new DataSet();

        adapter.TableMappings.Add("Table", "Products");
        adapter.TableMappings.Add("Table1", "Categories");
        adapter.TableMappings.Add("Table2", "Ratings");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if(connection is not null && dataSet is not null) 
        {
            adapter?.Fill(dataSet);
            ProductsList.ItemsSource = dataSet.Tables["Products"]?.AsDataView();
        }
    }
}
