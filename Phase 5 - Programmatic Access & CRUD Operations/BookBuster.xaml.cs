/*
* FILE          : BookBuster.xaml.cs
* PROJECT       : Phase 5 - Programmatic Access & CRUD Operations
* PROGRAMMER    : Najaf Ali, Zemmat Hagos, Brad Kajganich
* FIRST VERSION : 2025-12-03
* DESCRIPTION   :
*      This file contains the code-behind logic for the BookBuster Library Management System.
*      It implements complete CRUD operations (Create, Read, Update, Delete) for a MySQL
*      database including Books, Members, Staff, Loans, Fines, and Payments with transaction
*      support, comprehensive data validation, and error handling. The application features
*      automatic data grid refreshing, combo box population from database tables, and
*      business logic enforcement (e.g., preventing deletion of rented books, checking
*      for active loans before member deletion). It uses MySqlConnection with parameterized
*      queries to prevent SQL injection and provides user-friendly feedback through
*      MessageBox dialogs.
*/

using MySql.Data.MySqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Phase_5___Programmatic_Access___CRUD_Operations
{
    public partial class BookBuster : Window
    {
        // Database connection string - connects to MySQL server with BookBuster database
        private string connectionString = "Server=localhost;Port=3306;Uid=root;Pwd=Friday!1;Database=BookBuster;ConnectionTimeout=30";

        //
        // CONSTRUCTOR   : BookBuster
        // DESCRIPTION   : Initializes the main window, loads combo box data, refreshes all data,
        //                 and sets up the window closing event handler
        //
        public BookBuster()
        {
            InitializeComponent();
            LoadComboBoxData();
            RefreshAllData();
            this.Closing += BookBuster_Closing;
        }

        //
        // EVENT HANDLER : BookBuster_Closing
        // DESCRIPTION   : Handles window closing event for cleanup operations
        //
        private void BookBuster_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        //
        // METHOD        : LoadComboBoxData
        // DESCRIPTION   : Loads all combo boxes with data from the database for dropdown selections
        //                 Handles six different combo boxes for various entities
        //
        private void LoadComboBoxData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Load available books (not rented) for loans
                    cmbLoanBook.Items.Clear();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "SELECT BookID, Title FROM Books WHERE RentalStatus = 'Available'", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbLoanBook.Items.Add(new ComboBoxItem
                            {
                                Content = $"{reader["BookID"]} - {reader["Title"]}",
                                Tag = reader["BookID"]
                            });
                        }
                    }

                    // Load all members for loans
                    cmbLoanMember.Items.Clear();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT MemberID, FirstName, LastName FROM Members", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbLoanMember.Items.Add(new ComboBoxItem
                            {
                                Content = $"{reader["MemberID"]} - {reader["FirstName"]} {reader["LastName"]}",
                                Tag = reader["MemberID"]
                            });
                        }
                    }

                    // Load all staff for loans
                    cmbLoanStaff.Items.Clear();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT StaffID, FirstName, LastName FROM Staff", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbLoanStaff.Items.Add(new ComboBoxItem
                            {
                                Content = $"{reader["StaffID"]} - {reader["FirstName"]} {reader["LastName"]}",
                                Tag = reader["StaffID"]
                            });
                        }
                    }

                    // Load loans eligible for fines (returned but not fined)
                    cmbFineLoan.Items.Clear();
                    using (MySqlCommand cmd = new MySqlCommand(
                        @"SELECT l.LoanID, b.Title, m.FirstName, m.LastName 
                          FROM Loans l 
                          JOIN Books b ON l.BookID = b.BookID 
                          JOIN Members m ON l.MemberID = m.MemberID 
                          WHERE l.ReturnDate IS NOT NULL 
                          AND l.LoanID NOT IN (SELECT LoanID FROM Fines)", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbFineLoan.Items.Add(new ComboBoxItem
                            {
                                Content = $"{reader["LoanID"]} - {reader["Title"]} ({reader["FirstName"]} {reader["LastName"]})",
                                Tag = reader["LoanID"]
                            });
                        }
                    }

                    // Load fines eligible for payments
                    cmbPaymentFine.Items.Clear();
                    using (MySqlCommand cmd = new MySqlCommand(
                        @"SELECT f.FineID, l.LoanID 
                          FROM Fines f 
                          JOIN Loans l ON f.LoanID = l.LoanID", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbPaymentFine.Items.Add(new ComboBoxItem
                            {
                                Content = $"Fine {reader["FineID"]} (Loan {reader["LoanID"]})",
                                Tag = reader["FineID"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading combo box data: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // METHOD        : RefreshAllData
        // DESCRIPTION   : Refreshes all data grids by calling individual refresh methods
        //                 Provides centralized error handling for refresh operations
        //
        private void RefreshAllData()
        {
            try
            {
                BookRefresh_Click(null, null);
                MemberRefresh_Click(null, null);
                StaffRefresh_Click(null, null);
                LoanRefresh_Click(null, null);
                FineRefresh_Click(null, null);
                PaymentRefresh_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Refresh Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Book CRUD Operations

        //
        // EVENT HANDLER : BookRefresh_Click
        // DESCRIPTION   : Refreshes the books data grid with current database data
        //                 Includes book genres concatenated for display
        //
        private void BookRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT b.*, GROUP_CONCAT(g.Genre SEPARATOR ', ') as Genres 
                                   FROM Books b 
                                   LEFT JOIN BookGenre bg ON b.BookID = bg.BookID 
                                   LEFT JOIN Genres g ON bg.GenreID = g.GenreID 
                                   GROUP BY b.BookID 
                                   ORDER BY b.BookID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgBooks.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading books: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : BookAdd_Click
        // DESCRIPTION   : Adds a new book to the database with validation and transaction support
        //                 BookID is auto-generated by the database
        //
        private void BookAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(txtBookTitle.Text))
                {
                    MessageBox.Show("Please enter a title", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtBookAuthor.Text))
                {
                    MessageBox.Show("Please enter an author", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate publishing year
                if (!int.TryParse(txtBookYear.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 1)
                {
                    MessageBox.Show($"Please enter a valid year between 1000 and {DateTime.Now.Year + 1}",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtBookLanguage.Text))
                {
                    MessageBox.Show("Please enter a language", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbBookStatus.SelectedItem == null)
                {
                    MessageBox.Show("Please select a rental status", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate at least one genre is selected
                if (lstBookGenres.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one genre", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert book record (BookID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Books (Title, Author, PublishingYear, BookLanguage, RentalStatus) 
                                  VALUES (@Title, @Author, @PublishingYear, @BookLanguage, @RentalStatus);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Title", txtBookTitle.Text.Trim());
                                cmd.Parameters.AddWithValue("@Author", txtBookAuthor.Text.Trim());
                                cmd.Parameters.AddWithValue("@PublishingYear", year);
                                cmd.Parameters.AddWithValue("@BookLanguage", txtBookLanguage.Text.Trim());

                                string status = (cmbBookStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                                cmd.Parameters.AddWithValue("@RentalStatus", status);

                                // Get the auto-generated BookID
                                int bookId = Convert.ToInt32(cmd.ExecuteScalar());

                                bool genreError = false;
                                // Insert book-genre relationships
                                foreach (ListBoxItem item in lstBookGenres.SelectedItems)
                                {
                                    string genreName = item.Content.ToString();
                                    int genreId = GetOrCreateGenreId(genreName, conn, trans);

                                    if (genreId > 0)
                                    {
                                        using (MySqlCommand genreCmd = new MySqlCommand(
                                            "INSERT INTO BookGenre (BookID, GenreID) VALUES (@BookID, @GenreID)", conn, trans))
                                        {
                                            genreCmd.Parameters.AddWithValue("@BookID", bookId);
                                            genreCmd.Parameters.AddWithValue("@GenreID", genreId);
                                            genreCmd.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        genreError = true;
                                        break;
                                    }
                                }

                                if (genreError)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Error adding genre relationships", "Database Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                trans.Commit();
                                MessageBox.Show($"Book added successfully with ID: {bookId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                BookClear_Click(null, null);
                                BookRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding book: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : BookUpdate_Click
        // DESCRIPTION   : Updates an existing book record with transaction support
        //                 Handles book details update and genre relationships recreation
        //
        private void BookUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtBookID.Text, out int bookId) || bookId <= 0)
                {
                    MessageBox.Show("Please select a valid book to update", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate at least one genre is selected
                if (lstBookGenres.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one genre", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update book details
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"UPDATE Books SET Title = @Title, Author = @Author, 
                                  PublishingYear = @PublishingYear, BookLanguage = @BookLanguage, 
                                  RentalStatus = @RentalStatus WHERE BookID = @BookID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@BookID", bookId);
                                cmd.Parameters.AddWithValue("@Title", txtBookTitle.Text.Trim());
                                cmd.Parameters.AddWithValue("@Author", txtBookAuthor.Text.Trim());
                                cmd.Parameters.AddWithValue("@PublishingYear", int.Parse(txtBookYear.Text));
                                cmd.Parameters.AddWithValue("@BookLanguage", txtBookLanguage.Text.Trim());

                                string status = (cmbBookStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                                cmd.Parameters.AddWithValue("@RentalStatus", status);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Delete existing genre relationships
                                    using (MySqlCommand deleteGenres = new MySqlCommand(
                                        "DELETE FROM BookGenre WHERE BookID = @BookID", conn, trans))
                                    {
                                        deleteGenres.Parameters.AddWithValue("@BookID", bookId);
                                        deleteGenres.ExecuteNonQuery();
                                    }

                                    bool genreError = false;
                                    // Insert new genre relationships
                                    foreach (ListBoxItem item in lstBookGenres.SelectedItems)
                                    {
                                        // FIXED: Get the actual content string from the ListBoxItem
                                        string genreName = item.Content.ToString();
                                        int genreId = GetOrCreateGenreId(genreName, conn, trans);

                                        if (genreId > 0)
                                        {
                                            using (MySqlCommand genreCmd = new MySqlCommand(
                                                "INSERT INTO BookGenre (BookID, GenreID) VALUES (@BookID, @GenreID)", conn, trans))
                                            {
                                                genreCmd.Parameters.AddWithValue("@BookID", bookId);
                                                genreCmd.Parameters.AddWithValue("@GenreID", genreId);
                                                genreCmd.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            genreError = true;
                                            break;
                                        }
                                    }

                                    if (genreError)
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Error updating genre relationships", "Database Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }

                                    trans.Commit();
                                    MessageBox.Show("Book updated successfully", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    BookRefresh_Click(null, null);
                                    LoadComboBoxData();
                                }
                                else
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Book not found", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error updating book: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : BookDelete_Click
        // DESCRIPTION   : Deletes a book record with confirmation and validation
        //                 Prevents deletion of rented books and handles foreign key constraints
        //
        private void BookDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtBookID.Text, out int bookId) || bookId <= 0)
                {
                    MessageBox.Show("Please select a valid book to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete book ID {bookId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Check if book is rented
                                using (MySqlCommand checkCmd = new MySqlCommand(
                                    "SELECT RentalStatus FROM Books WHERE BookID = @BookID", conn, trans))
                                {
                                    checkCmd.Parameters.AddWithValue("@BookID", bookId);
                                    object statusResult = checkCmd.ExecuteScalar();

                                    if (statusResult != null && statusResult.ToString() == "Rented")
                                    {
                                        MessageBox.Show("Cannot delete a rented book. Return it first.",
                                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        trans.Rollback();
                                        return;
                                    }
                                }

                                // Delete genre relationships first
                                using (MySqlCommand deleteGenres = new MySqlCommand(
                                    "DELETE FROM BookGenre WHERE BookID = @BookID", conn, trans))
                                {
                                    deleteGenres.Parameters.AddWithValue("@BookID", bookId);
                                    deleteGenres.ExecuteNonQuery();
                                }

                                // Delete book record
                                using (MySqlCommand deleteBook = new MySqlCommand(
                                    "DELETE FROM Books WHERE BookID = @BookID", conn, trans))
                                {
                                    deleteBook.Parameters.AddWithValue("@BookID", bookId);
                                    int rowsAffected = deleteBook.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        trans.Commit();
                                        MessageBox.Show("Book deleted successfully", "Success",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                        BookClear_Click(null, null);
                                        BookRefresh_Click(null, null);
                                        LoadComboBoxData();
                                    }
                                    else
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Book not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            catch (MySqlException mysqlEx) when (mysqlEx.Number == 1451)
                            {
                                MessageBox.Show("Cannot delete book. It has related loan records.",
                                    "Constraint Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show($"Error deleting book: {ex.Message}", "Database Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : BookClear_Click
        // DESCRIPTION   : Clears all book input fields and resets selections
        //                 Sets BookID to "Auto-generated" since it's auto-generated
        //
        private void BookClear_Click(object sender, RoutedEventArgs e)
        {
            txtBookID.Text = "Auto-generated";
            txtBookTitle.Clear();
            txtBookAuthor.Clear();
            txtBookYear.Clear();
            txtBookLanguage.Clear();
            cmbBookStatus.SelectedIndex = -1;
            lstBookGenres.SelectedItems.Clear();
        }

        //
        // EVENT HANDLER : dgBooks_SelectionChanged
        // DESCRIPTION   : Populates book input fields when a row is selected in the data grid
        //
        private void dgBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgBooks.SelectedItem != null && dgBooks.SelectedItem is DataRowView row)
            {
                txtBookID.Text = row["BookID"].ToString();
                txtBookTitle.Text = row["Title"].ToString();
                txtBookAuthor.Text = row["Author"].ToString();
                txtBookYear.Text = row["PublishingYear"].ToString();
                txtBookLanguage.Text = row["BookLanguage"].ToString();

                string status = row["RentalStatus"].ToString();
                foreach (ComboBoxItem item in cmbBookStatus.Items)
                {
                    if (item.Content.ToString() == status)
                    {
                        cmbBookStatus.SelectedItem = item;
                        break;
                    }
                }

                lstBookGenres.SelectedItems.Clear();
                if (row["Genres"] != DBNull.Value)
                {
                    string[] genres = row["Genres"].ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string genre in genres)
                    {
                        foreach (ListBoxItem item in lstBookGenres.Items)
                        {
                            // FIXED: Compare the actual content string
                            if (item.Content.ToString() == genre.Trim())
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        //
        // METHOD        : GetOrCreateGenreId
        // DESCRIPTION   : Retrieves the GenreID for a given genre name, creates it if it doesn't exist
        // RETURNS       : int - GenreID or -1 if error
        //
        private int GetOrCreateGenreId(string genreName, MySqlConnection conn, MySqlTransaction trans)
        {
            try
            {
                // First, try to get existing genre
                using (MySqlCommand cmd = new MySqlCommand("SELECT GenreID FROM Genres WHERE Genre = @Genre", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@Genre", genreName);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        // Genre doesn't exist, create it
                        using (MySqlCommand insertCmd = new MySqlCommand(
                            "INSERT INTO Genres (Genre) VALUES (@Genre); SELECT LAST_INSERT_ID();", conn, trans))
                        {
                            insertCmd.Parameters.AddWithValue("@Genre", genreName);
                            return Convert.ToInt32(insertCmd.ExecuteScalar());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting/creating genre '{genreName}': {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }
        #endregion

        #region Member CRUD Operations (Updated for Auto-generated IDs)

        //
        // EVENT HANDLER : MemberRefresh_Click
        // DESCRIPTION   : Refreshes the members data grid with current database data
        //                 Includes address, city, and province information from joined tables
        //
        private void MemberRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT m.MemberID, m.FirstName, m.LastName, m.JoinDate, 
                                           m.Email, m.PhoneNumber, a.AddressID, a.Street, c.City, p.Province
                                   FROM Members m 
                                   JOIN Addresses a ON m.AddressID = a.AddressID 
                                   JOIN Cities c ON a.CityID = c.CityID 
                                   JOIN Provinces p ON c.ProvinceID = p.ProvinceID 
                                   ORDER BY m.MemberID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgMembers.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // METHOD        : FindOrCreateAddress
        // DESCRIPTION   : Finds an existing address or creates a new one in the database
        //                 Returns the AddressID for the address (auto-generated)
        // RETURNS       : int - AddressID of found or created address
        //
        private int FindOrCreateAddress(string street, string city, string province)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // First, check if province exists
                    int provinceId;
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ProvinceID FROM Provinces WHERE Province = @Province", conn))
                    {
                        cmd.Parameters.AddWithValue("@Province", province);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            // Province doesn't exist, create it (auto-generated ID)
                            using (MySqlCommand insertProvince = new MySqlCommand(
                                "INSERT INTO Provinces (Province) VALUES (@Province); SELECT LAST_INSERT_ID();", conn))
                            {
                                insertProvince.Parameters.AddWithValue("@Province", province);
                                provinceId = Convert.ToInt32(insertProvince.ExecuteScalar());
                            }
                        }
                        else
                        {
                            provinceId = Convert.ToInt32(result);
                        }
                    }

                    // Check if city exists for this province
                    int cityId;
                    using (MySqlCommand cmd = new MySqlCommand(
                        "SELECT CityID FROM Cities WHERE City = @City AND ProvinceID = @ProvinceID", conn))
                    {
                        cmd.Parameters.AddWithValue("@City", city);
                        cmd.Parameters.AddWithValue("@ProvinceID", provinceId);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            // City doesn't exist, create it (auto-generated ID)
                            using (MySqlCommand insertCity = new MySqlCommand(
                                "INSERT INTO Cities (City, ProvinceID) VALUES (@City, @ProvinceID); SELECT LAST_INSERT_ID();", conn))
                            {
                                insertCity.Parameters.AddWithValue("@City", city);
                                insertCity.Parameters.AddWithValue("@ProvinceID", provinceId);
                                cityId = Convert.ToInt32(insertCity.ExecuteScalar());
                            }
                        }
                        else
                        {
                            cityId = Convert.ToInt32(result);
                        }
                    }

                    // Check if address exists for this city
                    int addressId;
                    using (MySqlCommand cmd = new MySqlCommand(
                        "SELECT AddressID FROM Addresses WHERE Street = @Street AND CityID = @CityID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Street", street);
                        cmd.Parameters.AddWithValue("@CityID", cityId);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            // Address doesn't exist, create it (auto-generated ID)
                            using (MySqlCommand insertAddress = new MySqlCommand(
                                "INSERT INTO Addresses (Street, CityID) VALUES (@Street, @CityID); SELECT LAST_INSERT_ID();", conn))
                            {
                                insertAddress.Parameters.AddWithValue("@Street", street);
                                insertAddress.Parameters.AddWithValue("@CityID", cityId);
                                addressId = Convert.ToInt32(insertAddress.ExecuteScalar());
                            }
                        }
                        else
                        {
                            addressId = Convert.ToInt32(result);
                        }
                    }

                    return addressId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error finding/creating address: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        //
        // EVENT HANDLER : MemberAdd_Click
        // DESCRIPTION   : Adds a new member to the database with validation and transaction support
        //                 MemberID and AddressID are auto-generated by the database
        //
        private void MemberAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(txtMemberFirstName.Text))
                {
                    MessageBox.Show("Please enter first name", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMemberLastName.Text))
                {
                    MessageBox.Show("Please enter last name", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpMemberJoinDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select a join date", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate address fields
                if (string.IsNullOrWhiteSpace(txtMemberStreet.Text))
                {
                    MessageBox.Show("Please enter street address", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMemberCity.Text))
                {
                    MessageBox.Show("Please enter city", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMemberProvince.Text))
                {
                    MessageBox.Show("Please enter province", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Find or create address (returns auto-generated AddressID)
                            int addressId = FindOrCreateAddress(
                                txtMemberStreet.Text.Trim(),
                                txtMemberCity.Text.Trim(),
                                txtMemberProvince.Text.Trim());

                            if (addressId == -1)
                            {
                                trans.Rollback();
                                return;
                            }

                            // Insert member record (MemberID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Members (FirstName, LastName, JoinDate, Email, PhoneNumber, AddressID) 
                                  VALUES (@FirstName, @LastName, @JoinDate, @Email, @PhoneNumber, @AddressID);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@FirstName", txtMemberFirstName.Text.Trim());
                                cmd.Parameters.AddWithValue("@LastName", txtMemberLastName.Text.Trim());
                                cmd.Parameters.AddWithValue("@JoinDate", dpMemberJoinDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtMemberEmail.Text) ? DBNull.Value : (object)txtMemberEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(txtMemberPhone.Text) ? DBNull.Value : (object)txtMemberPhone.Text.Trim());
                                cmd.Parameters.AddWithValue("@AddressID", addressId);

                                // Get the auto-generated MemberID
                                int memberId = Convert.ToInt32(cmd.ExecuteScalar());

                                trans.Commit();
                                MessageBox.Show($"Member added successfully with ID: {memberId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                MemberClear_Click(null, null);
                                MemberRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding member: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : MemberUpdate_Click
        // DESCRIPTION   : Updates an existing member record with transaction support
        //                 Validates input fields before updating, includes address updates
        //
        private void MemberUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtMemberID.Text, out int memberId) || memberId <= 0)
                {
                    MessageBox.Show("Please select a valid member to update", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate address fields
                if (string.IsNullOrWhiteSpace(txtMemberStreet.Text))
                {
                    MessageBox.Show("Please enter street address", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMemberCity.Text))
                {
                    MessageBox.Show("Please enter city", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMemberProvince.Text))
                {
                    MessageBox.Show("Please enter province", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Find or create address (returns auto-generated AddressID if new)
                            int addressId = FindOrCreateAddress(
                                txtMemberStreet.Text.Trim(),
                                txtMemberCity.Text.Trim(),
                                txtMemberProvince.Text.Trim());

                            if (addressId == -1)
                            {
                                trans.Rollback();
                                return;
                            }

                            // Update member details
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"UPDATE Members SET FirstName = @FirstName, LastName = @LastName, 
                                  JoinDate = @JoinDate, Email = @Email, PhoneNumber = @PhoneNumber, 
                                  AddressID = @AddressID WHERE MemberID = @MemberID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@MemberID", memberId);
                                cmd.Parameters.AddWithValue("@FirstName", txtMemberFirstName.Text.Trim());
                                cmd.Parameters.AddWithValue("@LastName", txtMemberLastName.Text.Trim());
                                cmd.Parameters.AddWithValue("@JoinDate", dpMemberJoinDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtMemberEmail.Text) ? DBNull.Value : (object)txtMemberEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(txtMemberPhone.Text) ? DBNull.Value : (object)txtMemberPhone.Text.Trim());
                                cmd.Parameters.AddWithValue("@AddressID", addressId);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    trans.Commit();
                                    MessageBox.Show("Member updated successfully", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    MemberRefresh_Click(null, null);
                                    LoadComboBoxData();
                                }
                                else
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Member not found", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error updating member: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : MemberDelete_Click
        // DESCRIPTION   : Deletes a member record with confirmation and validation
        //                 Prevents deletion of members with active loans
        //
        private void MemberDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtMemberID.Text, out int memberId) || memberId <= 0)
                {
                    MessageBox.Show("Please select a valid member to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete member ID {memberId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();

                        // Check for active loans
                        using (MySqlCommand checkCmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Loans WHERE MemberID = @MemberID AND ReturnDate IS NULL", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@MemberID", memberId);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                            {
                                MessageBox.Show("Cannot delete member with active loans", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        // Delete member record
                        using (MySqlCommand deleteCmd = new MySqlCommand(
                            "DELETE FROM Members WHERE MemberID = @MemberID", conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@MemberID", memberId);
                            int rowsAffected = deleteCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Member deleted successfully", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                MemberClear_Click(null, null);
                                MemberRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                            else
                            {
                                MessageBox.Show("Member not found", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx) when (mysqlEx.Number == 1451)
            {
                MessageBox.Show("Cannot delete member. They have related loan records.",
                    "Constraint Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting member: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : MemberClear_Click
        // DESCRIPTION   : Clears all member input fields and resets selections
        //                 Sets MemberID and AddressID to "Auto-generated"
        //
        private void MemberClear_Click(object sender, RoutedEventArgs e)
        {
            txtMemberID.Text = "Auto-generated";
            txtMemberFirstName.Clear();
            txtMemberLastName.Clear();
            txtMemberEmail.Clear();
            txtMemberPhone.Clear();
            txtMemberStreet.Clear();
            txtMemberCity.Clear();
            txtMemberProvince.Clear();
            txtMemberAddressID.Text = "Auto-generated";
            dpMemberJoinDate.SelectedDate = null;
        }

        //
        // EVENT HANDLER : dgMembers_SelectionChanged
        // DESCRIPTION   : Populates member input fields when a row is selected in the data grid
        //                 Now includes address fields for manual editing (street, city, province)
        //
        private void dgMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgMembers.SelectedItem != null && dgMembers.SelectedItem is DataRowView row)
            {
                txtMemberID.Text = row["MemberID"].ToString();
                txtMemberFirstName.Text = row["FirstName"].ToString();
                txtMemberLastName.Text = row["LastName"].ToString();

                if (DateTime.TryParse(row["JoinDate"].ToString(), out DateTime joinDate))
                    dpMemberJoinDate.SelectedDate = joinDate;

                txtMemberEmail.Text = row["Email"] is DBNull ? string.Empty : row["Email"].ToString();
                txtMemberPhone.Text = row["PhoneNumber"] is DBNull ? string.Empty : row["PhoneNumber"].ToString();

                // Populate address fields
                txtMemberStreet.Text = row["Street"].ToString();
                txtMemberCity.Text = row["City"].ToString();
                txtMemberProvince.Text = row["Province"].ToString();
                txtMemberAddressID.Text = row["AddressID"].ToString();
            }
        }
        #endregion

        #region Staff CRUD Operations (Updated for Auto-generated IDs)

        //
        // EVENT HANDLER : StaffRefresh_Click
        // DESCRIPTION   : Refreshes the staff data grid with current database data
        //                 Includes concatenated role names for display
        //
        private void StaffRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT s.*, GROUP_CONCAT(r.RoleName SEPARATOR ', ') as Roles 
                                   FROM Staff s 
                                   LEFT JOIN StaffRole sr ON s.StaffID = sr.StaffID 
                                   LEFT JOIN Roles r ON sr.RoleID = r.RoleID 
                                   GROUP BY s.StaffID 
                                   ORDER BY s.StaffID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgStaff.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading staff: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : StaffAdd_Click
        // DESCRIPTION   : Adds a new staff member to the database with validation and transaction support
        //                 StaffID is auto-generated by the database
        //                 FIXED: Role selection issue - now properly retrieves role IDs
        //
        private void StaffAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(txtStaffFirstName.Text))
                {
                    MessageBox.Show("Please enter first name", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtStaffLastName.Text))
                {
                    MessageBox.Show("Please enter last name", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate at least one role is selected
                if (lstStaffRoles.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one role", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert staff record (StaffID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Staff (FirstName, LastName, Email, PhoneNumber) 
                                  VALUES (@FirstName, @LastName, @Email, @PhoneNumber);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@FirstName", txtStaffFirstName.Text.Trim());
                                cmd.Parameters.AddWithValue("@LastName", txtStaffLastName.Text.Trim());
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtStaffEmail.Text) ? DBNull.Value : (object)txtStaffEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(txtStaffPhone.Text) ? DBNull.Value : (object)txtStaffPhone.Text.Trim());

                                // Get the auto-generated StaffID
                                int staffId = Convert.ToInt32(cmd.ExecuteScalar());

                                bool roleError = false;
                                // Insert role assignments
                                foreach (ListBoxItem item in lstStaffRoles.SelectedItems)
                                {
                                    // FIXED: Get the actual content string from the ListBoxItem
                                    string roleName = item.Content.ToString();
                                    int roleId = GetOrCreateRoleId(roleName, conn, trans);

                                    if (roleId > 0)
                                    {
                                        using (MySqlCommand roleCmd = new MySqlCommand(
                                            "INSERT INTO StaffRole (StaffID, RoleID) VALUES (@StaffID, @RoleID)", conn, trans))
                                        {
                                            roleCmd.Parameters.AddWithValue("@StaffID", staffId);
                                            roleCmd.Parameters.AddWithValue("@RoleID", roleId);
                                            roleCmd.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        roleError = true;
                                        break;
                                    }
                                }

                                if (roleError)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Error adding role relationships", "Database Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                trans.Commit();
                                MessageBox.Show($"Staff added successfully with ID: {staffId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                StaffClear_Click(null, null);
                                StaffRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding staff: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : StaffUpdate_Click
        // DESCRIPTION   : Updates an existing staff member with transaction support
        //                 Handles role assignments recreation
        //                 FIXED: Role selection issue - now properly retrieves role IDs
        //
        private void StaffUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtStaffID.Text, out int staffId) || staffId <= 0)
                {
                    MessageBox.Show("Please select a valid staff member to update", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate at least one role is selected
                if (lstStaffRoles.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one role", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update staff details
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"UPDATE Staff SET FirstName = @FirstName, LastName = @LastName, 
                                  Email = @Email, PhoneNumber = @PhoneNumber WHERE StaffID = @StaffID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@StaffID", staffId);
                                cmd.Parameters.AddWithValue("@FirstName", txtStaffFirstName.Text.Trim());
                                cmd.Parameters.AddWithValue("@LastName", txtStaffLastName.Text.Trim());
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtStaffEmail.Text) ? DBNull.Value : (object)txtStaffEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrWhiteSpace(txtStaffPhone.Text) ? DBNull.Value : (object)txtStaffPhone.Text.Trim());

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Delete existing role assignments
                                    using (MySqlCommand deleteRoles = new MySqlCommand(
                                        "DELETE FROM StaffRole WHERE StaffID = @StaffID", conn, trans))
                                    {
                                        deleteRoles.Parameters.AddWithValue("@StaffID", staffId);
                                        deleteRoles.ExecuteNonQuery();
                                    }

                                    bool roleError = false;
                                    // Insert new role assignments
                                    foreach (ListBoxItem item in lstStaffRoles.SelectedItems)
                                    {
                                        // FIXED: Get the actual content string from the ListBoxItem
                                        string roleName = item.Content.ToString();
                                        int roleId = GetOrCreateRoleId(roleName, conn, trans);

                                        if (roleId > 0)
                                        {
                                            using (MySqlCommand roleCmd = new MySqlCommand(
                                                "INSERT INTO StaffRole (StaffID, RoleID) VALUES (@StaffID, @RoleID)", conn, trans))
                                            {
                                                roleCmd.Parameters.AddWithValue("@StaffID", staffId);
                                                roleCmd.Parameters.AddWithValue("@RoleID", roleId);
                                                roleCmd.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            roleError = true;
                                            break;
                                        }
                                    }

                                    if (roleError)
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Error updating role relationships", "Database Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }

                                    trans.Commit();
                                    MessageBox.Show("Staff updated successfully", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    StaffRefresh_Click(null, null);
                                    LoadComboBoxData();
                                }
                                else
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Staff not found", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error updating staff: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : StaffDelete_Click
        // DESCRIPTION   : Deletes a staff member with confirmation and validation
        //                 Prevents deletion of staff who have processed loans
        //
        private void StaffDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtStaffID.Text, out int staffId) || staffId <= 0)
                {
                    MessageBox.Show("Please select a valid staff member to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete staff ID {staffId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();

                        // Check if staff has processed loans
                        using (MySqlCommand checkCmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Loans WHERE StaffID = @StaffID", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@StaffID", staffId);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                            {
                                MessageBox.Show("Cannot delete staff member who has processed loans", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        using (MySqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Delete role assignments first
                                using (MySqlCommand deleteRoles = new MySqlCommand(
                                    "DELETE FROM StaffRole WHERE StaffID = @StaffID", conn, trans))
                                {
                                    deleteRoles.Parameters.AddWithValue("@StaffID", staffId);
                                    deleteRoles.ExecuteNonQuery();
                                }

                                // Delete staff record
                                using (MySqlCommand deleteStaff = new MySqlCommand(
                                    "DELETE FROM Staff WHERE StaffID = @StaffID", conn, trans))
                                {
                                    deleteStaff.Parameters.AddWithValue("@StaffID", staffId);
                                    int rowsAffected = deleteStaff.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        trans.Commit();
                                        MessageBox.Show("Staff deleted successfully", "Success",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                        StaffClear_Click(null, null);
                                        StaffRefresh_Click(null, null);
                                        LoadComboBoxData();
                                    }
                                    else
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Staff not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show($"Error deleting staff: {ex.Message}", "Database Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx) when (mysqlEx.Number == 1451)
            {
                MessageBox.Show("Cannot delete staff member. They have related records.",
                    "Constraint Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : StaffClear_Click
        // DESCRIPTION   : Clears all staff input fields and resets selections
        //                 Sets StaffID to "Auto-generated"
        //
        private void StaffClear_Click(object sender, RoutedEventArgs e)
        {
            txtStaffID.Text = "Auto-generated";
            txtStaffFirstName.Clear();
            txtStaffLastName.Clear();
            txtStaffEmail.Clear();
            txtStaffPhone.Clear();
            lstStaffRoles.SelectedItems.Clear();
        }

        //
        // EVENT HANDLER : dgStaff_SelectionChanged
        // DESCRIPTION   : Populates staff input fields when a row is selected in the data grid
        //                 FIXED: Properly selects roles from the list
        //
        private void dgStaff_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStaff.SelectedItem != null && dgStaff.SelectedItem is DataRowView row)
            {
                txtStaffID.Text = row["StaffID"].ToString();
                txtStaffFirstName.Text = row["FirstName"].ToString();
                txtStaffLastName.Text = row["LastName"].ToString();

                txtStaffEmail.Text = row["Email"] is DBNull ? string.Empty : row["Email"].ToString();
                txtStaffPhone.Text = row["PhoneNumber"] is DBNull ? string.Empty : row["PhoneNumber"].ToString();

                lstStaffRoles.SelectedItems.Clear();
                if (row["Roles"] != DBNull.Value)
                {
                    string[] roles = row["Roles"].ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string role in roles)
                    {
                        foreach (ListBoxItem item in lstStaffRoles.Items)
                        {
                            // FIXED: Compare the actual content string
                            if (item.Content.ToString() == role.Trim())
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        //
        // METHOD        : GetOrCreateRoleId
        // DESCRIPTION   : Retrieves the RoleID for a given role name, creates it if it doesn't exist
        // RETURNS       : int - RoleID or -1 if error
        //
        private int GetOrCreateRoleId(string roleName, MySqlConnection conn, MySqlTransaction trans)
        {
            try
            {
                // First, try to get existing role
                using (MySqlCommand cmd = new MySqlCommand("SELECT RoleID FROM Roles WHERE RoleName = @RoleName", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        // Role doesn't exist, create it
                        using (MySqlCommand insertCmd = new MySqlCommand(
                            "INSERT INTO Roles (RoleName) VALUES (@RoleName); SELECT LAST_INSERT_ID();", conn, trans))
                        {
                            insertCmd.Parameters.AddWithValue("@RoleName", roleName);
                            return Convert.ToInt32(insertCmd.ExecuteScalar());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting/creating role '{roleName}': {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        //
        // METHOD        : GetRoleId
        // DESCRIPTION   : Retrieves the RoleID for a given role name (legacy method)
        // RETURNS       : int - RoleID or -1 if not found
        //
        private int GetRoleId(string roleName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT RoleID FROM Roles WHERE RoleName = @RoleName", conn))
                    {
                        cmd.Parameters.AddWithValue("@RoleName", roleName);
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : -1;
                    }
                }
            }
            catch
            {
                return -1;
            }
        }
        #endregion

        #region Loan CRUD Operations (Updated for Auto-generated IDs)

        //
        // EVENT HANDLER : LoanRefresh_Click
        // DESCRIPTION   : Refreshes the loans data grid with current database data
        //                 Includes book title, member name, and staff name
        //
        private void LoanRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT l.*, b.Title as BookTitle, 
                                   CONCAT(m.FirstName, ' ', m.LastName) as MemberName,
                                   CONCAT(s.FirstName, ' ', s.LastName) as StaffName
                                   FROM Loans l 
                                   JOIN Books b ON l.BookID = b.BookID 
                                   JOIN Members m ON l.MemberID = m.MemberID 
                                   JOIN Staff s ON l.StaffID = s.StaffID 
                                   ORDER BY l.LoanID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgLoans.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading loans: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : LoanAdd_Click
        // DESCRIPTION   : Adds a new loan with comprehensive validation and transaction support
        //                 LoanID is auto-generated by the database
        //
        private void LoanAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required selections
                if (cmbLoanBook.SelectedItem == null)
                {
                    MessageBox.Show("Please select a book", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbLoanMember.SelectedItem == null)
                {
                    MessageBox.Show("Please select a member", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbLoanStaff.SelectedItem == null)
                {
                    MessageBox.Show("Please select a staff member", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate dates
                if (dpCheckoutDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select a checkout date", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpDueDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select a due date", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpCheckoutDate.SelectedDate.Value > dpDueDate.SelectedDate.Value)
                {
                    MessageBox.Show("Checkout date cannot be after due date", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int bookId = Convert.ToInt32(((cmbLoanBook.SelectedItem as ComboBoxItem)?.Tag));
                            int memberId = Convert.ToInt32(((cmbLoanMember.SelectedItem as ComboBoxItem)?.Tag));
                            int staffId = Convert.ToInt32(((cmbLoanStaff.SelectedItem as ComboBoxItem)?.Tag));

                            // Check if book is available
                            using (MySqlCommand bookCmd = new MySqlCommand("SELECT RentalStatus FROM Books WHERE BookID = @BookID", conn, trans))
                            {
                                bookCmd.Parameters.AddWithValue("@BookID", bookId);
                                object statusResult = bookCmd.ExecuteScalar();

                                if (statusResult == null || statusResult.ToString() != "Available")
                                {
                                    MessageBox.Show("Selected book is not available for loan", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    trans.Rollback();
                                    return;
                                }
                            }

                            // Insert loan record (LoanID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Loans (MemberID, BookID, StaffID, CheckoutDate, DueDate, ReturnDate) 
                                  VALUES (@MemberID, @BookID, @StaffID, @CheckoutDate, @DueDate, @ReturnDate);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@MemberID", memberId);
                                cmd.Parameters.AddWithValue("@BookID", bookId);
                                cmd.Parameters.AddWithValue("@StaffID", staffId);
                                cmd.Parameters.AddWithValue("@CheckoutDate", dpCheckoutDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@DueDate", dpDueDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@ReturnDate", dpReturnDate.SelectedDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);

                                // Get the auto-generated LoanID
                                int loanId = Convert.ToInt32(cmd.ExecuteScalar());

                                // Update book status to 'Rented'
                                using (MySqlCommand updateBook = new MySqlCommand(
                                    "UPDATE Books SET RentalStatus = 'Rented' WHERE BookID = @BookID", conn, trans))
                                {
                                    updateBook.Parameters.AddWithValue("@BookID", bookId);
                                    updateBook.ExecuteNonQuery();
                                }

                                trans.Commit();
                                MessageBox.Show($"Loan added successfully with ID: {loanId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                LoanClear_Click(null, null);
                                LoanRefresh_Click(null, null);
                                BookRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding loan: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : LoanUpdate_Click
        // DESCRIPTION   : Updates an existing loan with transaction support
        //                 Handles book status updates when changing books
        //                 Also handles book return functionality (formerly Return Book button)
        //
        private void LoanUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtLoanID.Text, out int loanId) || loanId <= 0)
                {
                    MessageBox.Show("Please select a valid loan to update", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int currentBookId;
                            using (MySqlCommand getBookCmd = new MySqlCommand(
                                "SELECT BookID FROM Loans WHERE LoanID = @LoanID", conn, trans))
                            {
                                getBookCmd.Parameters.AddWithValue("@LoanID", loanId);
                                object currentBookResult = getBookCmd.ExecuteScalar();

                                if (currentBookResult == null)
                                {
                                    MessageBox.Show("Loan not found", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    trans.Rollback();
                                    return;
                                }

                                currentBookId = Convert.ToInt32(currentBookResult);
                            }

                            var bookItem = cmbLoanBook.SelectedItem as ComboBoxItem;
                            var memberItem = cmbLoanMember.SelectedItem as ComboBoxItem;
                            var staffItem = cmbLoanStaff.SelectedItem as ComboBoxItem;

                            if (bookItem == null || memberItem == null || staffItem == null)
                            {
                                MessageBox.Show("Please select all required fields", "Validation Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                trans.Rollback();
                                return;
                            }

                            int newBookId = Convert.ToInt32(bookItem.Tag);
                            int memberId = Convert.ToInt32(memberItem.Tag);
                            int staffId = Convert.ToInt32(staffItem.Tag);

                            // Handle book change
                            if (currentBookId != newBookId)
                            {
                                // Return current book to available
                                using (MySqlCommand returnBook = new MySqlCommand(
                                    "UPDATE Books SET RentalStatus = 'Available' WHERE BookID = @BookID", conn, trans))
                                {
                                    returnBook.Parameters.AddWithValue("@BookID", currentBookId);
                                    returnBook.ExecuteNonQuery();
                                }

                                // Check if new book is available
                                using (MySqlCommand checkBook = new MySqlCommand(
                                    "SELECT RentalStatus FROM Books WHERE BookID = @BookID", conn, trans))
                                {
                                    checkBook.Parameters.AddWithValue("@BookID", newBookId);
                                    object statusResult = checkBook.ExecuteScalar();

                                    if (statusResult == null || statusResult.ToString() != "Available")
                                    {
                                        MessageBox.Show("New book is not available for loan", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        trans.Rollback();
                                        return;
                                    }
                                }

                                // Rent new book
                                using (MySqlCommand rentBook = new MySqlCommand(
                                    "UPDATE Books SET RentalStatus = 'Rented' WHERE BookID = @BookID", conn, trans))
                                {
                                    rentBook.Parameters.AddWithValue("@BookID", newBookId);
                                    rentBook.ExecuteNonQuery();
                                }
                            }

                            // Update loan details
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"UPDATE Loans SET MemberID = @MemberID, BookID = @BookID, StaffID = @StaffID, 
                                  CheckoutDate = @CheckoutDate, DueDate = @DueDate, ReturnDate = @ReturnDate 
                                  WHERE LoanID = @LoanID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", loanId);
                                cmd.Parameters.AddWithValue("@MemberID", memberId);
                                cmd.Parameters.AddWithValue("@BookID", newBookId);
                                cmd.Parameters.AddWithValue("@StaffID", staffId);
                                cmd.Parameters.AddWithValue("@CheckoutDate", dpCheckoutDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@DueDate", dpDueDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@ReturnDate", dpReturnDate.SelectedDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // If return date is set, update book status to available
                                    if (dpReturnDate.SelectedDate != null && currentBookId == newBookId)
                                    {
                                        using (MySqlCommand updateBook = new MySqlCommand(
                                            "UPDATE Books SET RentalStatus = 'Available' WHERE BookID = @BookID", conn, trans))
                                        {
                                            updateBook.Parameters.AddWithValue("@BookID", newBookId);
                                            updateBook.ExecuteNonQuery();
                                        }
                                    }

                                    trans.Commit();
                                    MessageBox.Show("Loan updated successfully", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoanRefresh_Click(null, null);
                                    BookRefresh_Click(null, null);
                                    LoadComboBoxData();
                                }
                                else
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Loan not found", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error updating loan: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : LoanDelete_Click
        // DESCRIPTION   : Deletes a loan record with confirmation
        //                 Returns the book to 'Available' status
        //
        private void LoanDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtLoanID.Text, out int loanId) || loanId <= 0)
                {
                    MessageBox.Show("Please select a valid loan to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete loan ID {loanId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                int bookId;
                                using (MySqlCommand getBookCmd = new MySqlCommand(
                                    "SELECT BookID FROM Loans WHERE LoanID = @LoanID", conn, trans))
                                {
                                    getBookCmd.Parameters.AddWithValue("@LoanID", loanId);
                                    object bookResult = getBookCmd.ExecuteScalar();

                                    if (bookResult == null)
                                    {
                                        MessageBox.Show("Loan not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        trans.Rollback();
                                        return;
                                    }

                                    bookId = Convert.ToInt32(bookResult);
                                }

                                // Delete loan record
                                using (MySqlCommand deleteLoan = new MySqlCommand(
                                    "DELETE FROM Loans WHERE LoanID = @LoanID", conn, trans))
                                {
                                    deleteLoan.Parameters.AddWithValue("@LoanID", loanId);
                                    int rowsAffected = deleteLoan.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        // Return book to available
                                        using (MySqlCommand updateBook = new MySqlCommand(
                                            "UPDATE Books SET RentalStatus = 'Available' WHERE BookID = @BookID", conn, trans))
                                        {
                                            updateBook.Parameters.AddWithValue("@BookID", bookId);
                                            updateBook.ExecuteNonQuery();
                                        }

                                        trans.Commit();
                                        MessageBox.Show("Loan deleted successfully", "Success",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                        LoanClear_Click(null, null);
                                        LoanRefresh_Click(null, null);
                                        BookRefresh_Click(null, null);
                                        LoadComboBoxData();
                                    }
                                    else
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Loan not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            catch (MySqlException mysqlEx) when (mysqlEx.Number == 1451)
                            {
                                MessageBox.Show("Cannot delete loan. It has related fine records.",
                                    "Constraint Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show($"Error deleting loan: {ex.Message}", "Database Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : LoanClear_Click
        // DESCRIPTION   : Clears all loan input fields and resets selections
        //                 Sets LoanID to "Auto-generated"
        //
        private void LoanClear_Click(object sender, RoutedEventArgs e)
        {
            txtLoanID.Text = "Auto-generated";
            cmbLoanBook.SelectedIndex = -1;
            cmbLoanMember.SelectedIndex = -1;
            cmbLoanStaff.SelectedIndex = -1;
            dpCheckoutDate.SelectedDate = null;
            dpDueDate.SelectedDate = null;
            dpReturnDate.SelectedDate = null;
        }

        //
        // EVENT HANDLER : dgLoans_SelectionChanged
        // DESCRIPTION   : Populates loan input fields when a row is selected in the data grid
        //
        private void dgLoans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLoans.SelectedItem != null && dgLoans.SelectedItem is DataRowView row)
            {
                txtLoanID.Text = row["LoanID"].ToString();

                string bookInfo = $"{row["BookID"]} - {row["BookTitle"]}";
                foreach (ComboBoxItem item in cmbLoanBook.Items)
                {
                    if (item.Content.ToString().Contains(bookInfo))
                    {
                        cmbLoanBook.SelectedItem = item;
                        break;
                    }
                }

                string memberInfo = $"{row["MemberID"]} - {row["MemberName"]}";
                foreach (ComboBoxItem item in cmbLoanMember.Items)
                {
                    if (item.Content.ToString().Contains(memberInfo))
                    {
                        cmbLoanMember.SelectedItem = item;
                        break;
                    }
                }

                string staffInfo = $"{row["StaffID"]} - {row["StaffName"]}";
                foreach (ComboBoxItem item in cmbLoanStaff.Items)
                {
                    if (item.Content.ToString().Contains(staffInfo))
                    {
                        cmbLoanStaff.SelectedItem = item;
                        break;
                    }
                }

                if (DateTime.TryParse(row["CheckoutDate"].ToString(), out DateTime checkoutDate))
                    dpCheckoutDate.SelectedDate = checkoutDate;

                if (DateTime.TryParse(row["DueDate"].ToString(), out DateTime dueDate))
                    dpDueDate.SelectedDate = dueDate;

                if (row["ReturnDate"] != DBNull.Value && DateTime.TryParse(row["ReturnDate"].ToString(), out DateTime returnDate))
                    dpReturnDate.SelectedDate = returnDate;
                else
                    dpReturnDate.SelectedDate = null;
            }
        }
        #endregion

        #region Fine CRUD Operations (Updated for Auto-generated IDs)

        //
        // EVENT HANDLER : FineRefresh_Click
        // DESCRIPTION   : Refreshes the fines data grid with current database data
        //                 Includes loan, book, and member information
        //
        private void FineRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT f.FineID, f.LoanID, b.Title, m.FirstName, m.LastName 
                                   FROM Fines f 
                                   JOIN Loans l ON f.LoanID = l.LoanID 
                                   JOIN Books b ON l.BookID = b.BookID 
                                   JOIN Members m ON l.MemberID = m.MemberID 
                                   ORDER BY f.FineID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgFines.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading fines: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : FineAdd_Click
        // DESCRIPTION   : Adds a fine for a returned late book with validation
        //                 FineID is auto-generated by the database
        //
        private void FineAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbFineLoan.SelectedItem == null)
                {
                    MessageBox.Show("Please select a loan", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var loanItem = cmbFineLoan.SelectedItem as ComboBoxItem;
                int loanId = Convert.ToInt32(loanItem?.Tag);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Check if loan already has a fine
                            using (MySqlCommand checkCmd = new MySqlCommand(
                                "SELECT COUNT(*) FROM Fines WHERE LoanID = @LoanID", conn, trans))
                            {
                                checkCmd.Parameters.AddWithValue("@LoanID", loanId);
                                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                                {
                                    MessageBox.Show("This loan already has a fine", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    trans.Rollback();
                                    return;
                                }
                            }

                            // Check loan eligibility for fine
                            using (MySqlCommand loanCmd = new MySqlCommand(
                                "SELECT DueDate, ReturnDate FROM Loans WHERE LoanID = @LoanID", conn, trans))
                            {
                                loanCmd.Parameters.AddWithValue("@LoanID", loanId);
                                using (MySqlDataReader reader = loanCmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        DateTime dueDate = reader.GetDateTime("DueDate");

                                        if (reader.IsDBNull(reader.GetOrdinal("ReturnDate")))
                                        {
                                            MessageBox.Show("Cannot add fine. Book has not been returned yet.",
                                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            reader.Close();
                                            trans.Rollback();
                                            return;
                                        }

                                        DateTime returnDate = reader.GetDateTime("ReturnDate");
                                        reader.Close();

                                        if (returnDate <= dueDate)
                                        {
                                            MessageBox.Show("Cannot add fine. Book was returned on time.",
                                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            trans.Rollback();
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("Loan not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        trans.Rollback();
                                        return;
                                    }
                                }
                            }

                            // Insert fine record (FineID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Fines (LoanID) VALUES (@LoanID);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", loanId);

                                // Get the auto-generated FineID
                                int fineId = Convert.ToInt32(cmd.ExecuteScalar());

                                trans.Commit();
                                MessageBox.Show($"Fine added successfully with ID: {fineId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                FineClear_Click(null, null);
                                FineRefresh_Click(null, null);
                                PaymentRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding fine: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : FineDelete_Click
        // DESCRIPTION   : Deletes a fine record with confirmation
        //                 Also deletes related payments
        //
        private void FineDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtFineID.Text, out int fineId) || fineId <= 0)
                {
                    MessageBox.Show("Please select a valid fine to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete fine ID {fineId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Delete related payments first
                                using (MySqlCommand deletePayments = new MySqlCommand(
                                    "DELETE FROM Payments WHERE FineID = @FineID", conn, trans))
                                {
                                    deletePayments.Parameters.AddWithValue("@FineID", fineId);
                                    deletePayments.ExecuteNonQuery();
                                }

                                // Delete fine record
                                using (MySqlCommand deleteFine = new MySqlCommand(
                                    "DELETE FROM Fines WHERE FineID = @FineID", conn, trans))
                                {
                                    deleteFine.Parameters.AddWithValue("@FineID", fineId);
                                    int rowsAffected = deleteFine.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        trans.Commit();
                                        MessageBox.Show("Fine deleted successfully", "Success",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                        FineClear_Click(null, null);
                                        FineRefresh_Click(null, null);
                                        PaymentRefresh_Click(null, null);
                                        LoadComboBoxData();
                                    }
                                    else
                                    {
                                        trans.Rollback();
                                        MessageBox.Show("Fine not found", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show($"Error deleting fine: {ex.Message}", "Database Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : FineClear_Click
        // DESCRIPTION   : Clears all fine input fields and resets selections
        //                 Sets FineID to "Auto-generated"
        //
        private void FineClear_Click(object sender, RoutedEventArgs e)
        {
            txtFineID.Text = "Auto-generated";
            cmbFineLoan.SelectedIndex = -1;
        }

        //
        // EVENT HANDLER : dgFines_SelectionChanged
        // DESCRIPTION   : Populates fine input fields when a row is selected in the data grid
        //
        private void dgFines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFines.SelectedItem != null && dgFines.SelectedItem is DataRowView row)
            {
                txtFineID.Text = row["FineID"].ToString();

                string loanInfo = $"{row["LoanID"]} - {row["Title"]} ({row["FirstName"]} {row["LastName"]})";
                foreach (ComboBoxItem item in cmbFineLoan.Items)
                {
                    if (item.Content.ToString() == loanInfo)
                    {
                        cmbFineLoan.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        #endregion

        #region Payment CRUD Operations (Updated for Auto-generated IDs)

        //
        // EVENT HANDLER : PaymentRefresh_Click
        // DESCRIPTION   : Refreshes the payments data grid with current database data
        //                 Includes fine and loan information
        //
        private void PaymentRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT p.PaymentID, p.FineID, p.AmountPaid, p.PaymentDate, 
                                           p.PaymentMethod, l.LoanID
                                   FROM Payments p 
                                   JOIN Fines f ON p.FineID = f.FineID 
                                   JOIN Loans l ON f.LoanID = l.LoanID 
                                   ORDER BY p.PaymentID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgPayments.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : PaymentAdd_Click
        // DESCRIPTION   : Adds a new payment with validation and transaction support
        //                 PaymentID is auto-generated by the database
        //
        private void PaymentAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required selections
                if (cmbPaymentFine.SelectedItem == null)
                {
                    MessageBox.Show("Please select a fine", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate amount
                if (!decimal.TryParse(txtPaymentAmount.Text, out decimal amount) || amount <= 0 || amount > 9.99m)
                {
                    MessageBox.Show("Please enter a valid payment amount (0.01 to 9.99)", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpPaymentDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select a payment date", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbPaymentMethod.SelectedItem == null)
                {
                    MessageBox.Show("Please select a payment method", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fineItem = cmbPaymentFine.SelectedItem as ComboBoxItem;
                int fineId = Convert.ToInt32(fineItem?.Tag);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert payment record (PaymentID is auto-generated)
                            using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO Payments (FineID, AmountPaid, PaymentDate, PaymentMethod) 
                                  VALUES (@FineID, @AmountPaid, @PaymentDate, @PaymentMethod);
                                  SELECT LAST_INSERT_ID();", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@FineID", fineId);
                                cmd.Parameters.AddWithValue("@AmountPaid", amount);
                                cmd.Parameters.AddWithValue("@PaymentDate", dpPaymentDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@PaymentMethod", (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content.ToString());

                                // Get the auto-generated PaymentID
                                int paymentId = Convert.ToInt32(cmd.ExecuteScalar());

                                trans.Commit();
                                MessageBox.Show($"Payment added successfully with ID: {paymentId}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                PaymentClear_Click(null, null);
                                PaymentRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Error adding payment: {ex.Message}", "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : PaymentDelete_Click
        // DESCRIPTION   : Deletes a payment record with confirmation
        //
        private void PaymentDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPaymentID.Text, out int paymentId) || paymentId <= 0)
                {
                    MessageBox.Show("Please select a valid payment to delete", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete payment ID {paymentId}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(
                            "DELETE FROM Payments WHERE PaymentID = @PaymentID", conn))
                        {
                            cmd.Parameters.AddWithValue("@PaymentID", paymentId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Payment deleted successfully", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                PaymentClear_Click(null, null);
                                PaymentRefresh_Click(null, null);
                                LoadComboBoxData();
                            }
                            else
                            {
                                MessageBox.Show("Payment not found", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting payment: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : PaymentClear_Click
        // DESCRIPTION   : Clears all payment input fields and resets selections
        //                 Sets PaymentID to "Auto-generated"
        //
        private void PaymentClear_Click(object sender, RoutedEventArgs e)
        {
            txtPaymentID.Text = "Auto-generated";
            txtPaymentAmount.Clear();
            cmbPaymentFine.SelectedIndex = -1;
            cmbPaymentMethod.SelectedIndex = -1;
            dpPaymentDate.SelectedDate = null;
        }

        //
        // EVENT HANDLER : dgPayments_SelectionChanged
        // DESCRIPTION   : Populates payment input fields when a row is selected in the data grid
        //
        private void dgPayments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPayments.SelectedItem != null && dgPayments.SelectedItem is DataRowView row)
            {
                txtPaymentID.Text = row["PaymentID"].ToString();
                txtPaymentAmount.Text = row["AmountPaid"].ToString();

                string fineInfo = $"Fine {row["FineID"]} (Loan {row["LoanID"]})";
                foreach (ComboBoxItem item in cmbPaymentFine.Items)
                {
                    if (item.Content.ToString() == fineInfo)
                    {
                        cmbPaymentFine.SelectedItem = item;
                        break;
                    }
                }

                string method = row["PaymentMethod"].ToString();
                foreach (ComboBoxItem item in cmbPaymentMethod.Items)
                {
                    if (item.Content.ToString() == method)
                    {
                        cmbPaymentMethod.SelectedItem = item;
                        break;
                    }
                }

                if (DateTime.TryParse(row["PaymentDate"].ToString(), out DateTime paymentDate))
                    dpPaymentDate.SelectedDate = paymentDate;
            }
        }
        #endregion

        #region Menu and View Operations

        //
        // EVENT HANDLER : Exit_Click
        // DESCRIPTION   : Handles exit menu item click - shuts down the application
        //
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        //
        // EVENT HANDLER : ViewLoanFineDetails_Click
        // DESCRIPTION   : Opens a window displaying the LoanFineDetails database view
        //                 Shows comprehensive loan and fine information
        //
        private void ViewLoanFineDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM LoanFineDetails ORDER BY LoanID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        Window viewWindow = new Window
                        {
                            Title = "Loan Fine Details View",
                            Width = 1000,
                            Height = 600,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this
                        };

                        DataGrid dg = new DataGrid
                        {
                            ItemsSource = dt.DefaultView,
                            AutoGenerateColumns = true,
                            CanUserAddRows = false,
                            IsReadOnly = true,
                            Margin = new Thickness(10)
                        };

                        viewWindow.Content = dg;
                        viewWindow.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading LoanFineDetails view: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : ViewMemberBalances_Click
        // DESCRIPTION   : Opens a window displaying the MemberBalances database view
        //                 Shows member financial information
        //
        private void ViewMemberBalances_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM MemberBalances ORDER BY MemberID";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        Window viewWindow = new Window
                        {
                            Title = "Member Balances View",
                            Width = 800,
                            Height = 400,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this
                        };

                        DataGrid dg = new DataGrid
                        {
                            ItemsSource = dt.DefaultView,
                            AutoGenerateColumns = true,
                            CanUserAddRows = false,
                            IsReadOnly = true,
                            Margin = new Thickness(10)
                        };

                        viewWindow.Content = dg;
                        viewWindow.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading MemberBalances view: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //
        // EVENT HANDLER : About_Click
        // DESCRIPTION   : Displays the about dialog with application information and team details
        //
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("BookBuster Library Management System\n\n" +
                "Developed for PROG2111 Final Project\n\n" +
                "Group Members:\n" +
                "• Najaf Ali – 9060484\n" +
                "• Zemmat Hagos – 84884339\n" +
                "• Brad Kajganich – 9036321",
                "About BookBuster", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion
    }
}