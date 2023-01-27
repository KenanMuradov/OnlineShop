using MaterialDesignThemes.Wpf;
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
using System.Windows.Input;
using System.Windows.Shapes;

namespace OnlineShop;

public partial class MainWindow : Window
{
    SqlConnection? connection = null;
    SqlDataAdapter? adapter = null;
    DataViewManager? dataView = null;
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
        dataView = new DataViewManager(dataSet);

        adapter.TableMappings.Add("Table", "Products");
        adapter.TableMappings.Add("Table1", "Categories");
        adapter.TableMappings.Add("Table2", "Ratings");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (connection is not null && dataSet is not null)
        {
            adapter?.Fill(dataSet);
            ProductsList.ItemsSource = dataSet.Tables["Products"]?.AsDataView();

            CBoxCategories.DataContext = dataSet.Tables["Categories"];
            CBoxCategories.DisplayMemberPath = dataSet.Tables["Categories"]?.Columns["Name"]?.ColumnName;
        }
    }

    private void CBoxCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CBoxCategories.SelectedItem is DataRowView rowView)
        {
            var row = rowView.Row;

            var id = row["Id"];

            var table = dataSet?.Tables["Products"];
            if (table != null && dataView != null)
            {
                var view = dataView.CreateDataView(table);

                view.RowFilter = $"CategoryId = {id}";

                ProductsList.ItemsSource = view;
            }

        }
    }

    private void SearchTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchTxt.Text))
        {
            ProductsList.ItemsSource = dataSet?.Tables["Products"]?.AsDataView();
            return;
        }

        var view = dataView.CreateDataView(dataSet?.Tables["Products"]);

        view.RowFilter = $"Name LIKE '%{SearchTxt.Text}%'";


        ProductsList.ItemsSource = view;

    }

    private void SelectCurrentItem(object sender, MouseEventArgs e)
    {
        ListViewItem item = (ListViewItem)sender;
        item.IsSelected = true;

    }

    private void BasicRatingBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(sender is RatingBar ratingBar)
        {
            try
            {
                connection?.Open();

                var command = connection?.CreateCommand();

                if (command is null)
                    return;

                var tran = connection?.BeginTransaction();

                command.Transaction = tran;

                command.CommandText = "usp_AddRating";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("productId", SqlDbType.Int);
                command.Parameters["productId"].Value = ProductsList.SelectedIndex+1;

                command.Parameters.Add("rating", SqlDbType.Int);
                command.Parameters["rating"].Value = ratingBar.Value;

                command.ExecuteNonQuery();

                tran?.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection?.Close();
            }
        }
    }
}
