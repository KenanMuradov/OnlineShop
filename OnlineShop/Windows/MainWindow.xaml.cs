using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;
using OnlineShop.Windows;
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
using WPFCustomMessageBox;

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

        var view = dataView?.CreateDataView(dataSet?.Tables?["Products"]);

        view.RowFilter = $"Name LIKE '%{SearchTxt.Text}%'";

        ProductsList.ItemsSource = view;
    }

    private void BasicRatingBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is RatingBar ratingBar)
        {
            var index = ProductsList.SelectedIndex;
            var row = (ProductsList.Items[index] as DataRowView)?.Row;

            var productId = Convert.ToInt32(row?["Id"]);
            try
            {
                connection?.Open();

                var command = connection?.CreateCommand();

                ArgumentNullException.ThrowIfNull(command);

                var tran = connection?.BeginTransaction();

                command.Transaction = tran;

                command.CommandText = "usp_AddRating";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("productId", SqlDbType.Int);
                command.Parameters["productId"].Value = productId;

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

    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        AddWindow addWindow = new(connection, dataSet?.Tables["Categories"]);

        addWindow.ShowDialog();

        if (addWindow.DialogResult is true)
        {
            dataSet?.Clear();

            if (dataSet is not null)
                adapter?.Fill(dataSet);

            ProductsList.ItemsSource = dataSet?.Tables["Products"]?.AsDataView();
        }

    }

    private void ButtonReset_Click(object sender, RoutedEventArgs e)
    {
        CBoxCategories.SelectedIndex = -1;
        SearchTxt.Text = string.Empty;
    }

    

    private async void ProductsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var index = ProductsList.SelectedIndex;

        if (index == -1)
            return;


        var result = CustomMessageBox.ShowYesNoCancel("What You Want To Do?", "Next Step", "Update Product", "Delete", "Cancel");

        if (result == MessageBoxResult.Cancel)
            return;


        if (result == MessageBoxResult.Yes)
        {

            var row = (ProductsList.Items[index] as DataRowView)?.Row;

            var productId = Convert.ToInt32(row?["Id"]);
            var name = row?["Name"].ToString();
            var price = Convert.ToDecimal(row?["Price"]);
            var quantity = Convert.ToInt32(row?["Quantity"]);
            var categoryId = Convert.ToInt32(row?["CategoryId"]);

            UpdateWindow updateWindow = new(connection, dataSet?.Tables["Categories"], name, quantity, price, categoryId, productId);

            updateWindow.ShowDialog();

            if (updateWindow.DialogResult is true)
            {
                dataSet?.Clear();
                if (dataSet is not null)
                    adapter?.Fill(dataSet);

                ProductsList.ItemsSource = dataSet?.Tables["Products"]?.AsDataView();
            }
        }
        else if (result == MessageBoxResult.No)
        {
            
            var row = (ProductsList.Items[index] as DataRowView)?.Row;

            var productId = Convert.ToInt32(row?["Id"]);

            try
            {
                connection?.Open();

                var command = connection?.CreateCommand();

                ArgumentNullException.ThrowIfNull(command);

                command.CommandText = "usp_DeleteProduct";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("productId", SqlDbType.Int);
                command.Parameters["productId"].Value = productId;
                await command.ExecuteNonQueryAsync();

                dataSet?.Clear();
                if (dataSet is not null)
                    adapter?.Fill(dataSet);

                ProductsList.ItemsSource = dataSet?.Tables["Products"]?.AsDataView();
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
