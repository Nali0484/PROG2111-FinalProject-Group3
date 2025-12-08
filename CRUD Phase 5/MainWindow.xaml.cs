using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;

namespace CRUD_Phase_5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=localhost;Port=3306;Uid=root;Pwd=Test1234;Database=BookBuster;";
        private MySqlDataAdapter adapter;
        private DataSet ds;

        public MainWindow()
        {
            InitializeComponent();
            ReadBooks();
        }

        private void ReadBooks()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM Books";
                    adapter = new MySqlDataAdapter(query, connection);
                    ds = new DataSet();
                    adapter.Fill(ds, "Books");
                    dataGridBooks.ItemsSource = ds.Tables["Books"].DefaultView;
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("ERROR Can't read it" + ex.Message);
            }
                
        }

        private void BtnCreateBook_Click(object sender, RoutedEventArgs e)
        {
            int bookID = int.Parse(txtBookID.Text); string getTitle = txtTitle.Text; string getAuthor = txtAuthor.Text; int checkYear = int.Parse(txtYear.Text); string checkLanguage = txtLanguage.Text; 

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Books VALUES (BookID, Title, Author, PublishingYear, BookLanguage, RentalStatus) VALUES (@BookID, @Title, @Author, @PublishingYear, @BookLanguage, @RentalStatus)";

                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataRow newRow = ds.Tables["Books"].NewRow();

                    newRow["BookID"] = bookID;
                    newRow["Title"] = "getTitle";
                    newRow["Author"] = "getAuthor";
                    newRow["PublishingYear"] = checkYear;
                    newRow["BookLanguage"] = "checkLanguage";
                    
                    
                    ds.Tables["Books"].Rows.Add(newRow);

                    adapter.Update(ds, "Books");
                    
                   
                }

                ReadBooks();
                MessageBox.Show("Inserting book sucessfully");

            }
            catch (Exception ex)
            {
                MessageBox.Show("Insert ERROR" + ex.Message);
            }
        }

        private void BtnReadBook_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnUpdateBook_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnDeleteBook_Click(object sender, RoutedEventArgs e)
        {

        }
        
    }
}