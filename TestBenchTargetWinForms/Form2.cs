using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics.Eventing.Reader;

namespace TestBenchTargetWinForms
{
    public partial class Form2 : Form
    {
        private BindingSource bindingSource;
        private CustomBindingList<DataItem> dataList;
        private readonly Timer timer;
        private bool isHandlingDataError = false;
        private bool isUpdatingUI = false;
        private bool isDeleting = false;
        private bool isClearing = false;
        private static bool isAdding = false;
        private Timer updateTimer;
        private float _dpiScaleFactor;

        public Form2()
        {
            InitializeComponent();

            // Initialize time and date
            LabelTime.Text = DateTime.Now.ToString("HH:mm:ss");

            // Setup timer
            timer = new Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += (s, e) =>
            {
                LabelTime.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();


            ColumnPoints.DefaultCellStyle.NullValue = 0; // Set default value for empty cells - before binding DataSource yet
            ColumnDate.DefaultCellStyle.NullValue = DateTime.Now.Date; // Set default value for empty cells - before binding DataSource yet
            DataGridView1.DataError += DataGridView1_DataError; // empty cells are not allowed 
            DataGridView1.CellParsing += DataGridView1_CellParsing; // empty cells are not allowed - custom validation

            // Initialize data source
            dataList = new CustomBindingList<DataItem>();
            bindingSource = new BindingSource();
            bindingSource.DataSource = dataList;

            // Setup column mapping
            DataGridView1.AutoGenerateColumns = false;
            ColumnDate.DataPropertyName = "DateColumnValue";
            ColumnDate.DefaultCellStyle.Format = "dd.MM.yyyy";

            ColumnProcedure.DataPropertyName = "ProcedureColumnValue";
            ColumnPoints.DataPropertyName = "PointsColumnValue";
            ColumnDelegate.DataPropertyName = "DelegateColumnValue";

            // Connect DataGridView to data source
            DataGridView1.DataSource = bindingSource;

            // Setup row for adding new records
            DataGridView1.AllowUserToAddRows = true;
            DataGridView1.AllowUserToDeleteRows = true;
            DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            DataGridView1.MultiSelect = false;
            DataGridView1.DataError += DataGridView1_DataError;

            // Setup event for showing empty row at the top
            DataGridView1.RowsAdded += DataGridView1_RowsAdded;

            // Initialize DomainUpDownDate with dates from the last 7 days
            InitializeDomainUpDown();

            // Assign events
            DomainUpDownDate.KeyDown += DomainUpDownDate_KeyDown;

            // Assign event for SelectionChanged
            DataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

            // Setup timer for delayed update
            updateTimer = new Timer();
            updateTimer.Interval = 500; // 500 ms delay
            updateTimer.Tick += UpdateTimer_Tick;

            // Modify text changed event handlers
            TextBoxProcedure.TextChanged += TextBox_TextChanged;
            TextBoxPoints.TextChanged += TextBoxPoints_TextChanged_New;
            TextBoxDelegate.TextChanged += TextBox_TextChanged;
            // Assign events for textboxes  
            TextBoxPoints.KeyPress += TextBoxPoints_KeyPress;
            TextBoxPoints.LostFocus += TextBoxPoints_LostFocus;

            DataGridView1.CellValidating += DataGridView1_CellValidating;
            DataGridView1.CellEndEdit += DataGridView1_CellEndEdit;

            // Nastavenie DPI škálovania
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // Získanie aktuálneho DPI faktora
            _dpiScaleFactor = this.DeviceDpi / 96.0f;

            // Registrácia event handlera pre Load udalosť
            this.Load += Form2_Load;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }

            base.OnFormClosing(e);
        }

        // Initialize DomainUpDown with dates
        private void InitializeDomainUpDown()
        {
            // days into DomainUpDownDate
            for (int i = -367; i <= 367; i++)
            {
                DateTime date = DateTime.Now.AddDays(-i);
                DomainUpDownDate.Items.Add(date.ToString("dd.MM.yyyy"));
            }
            // Set current date as default (it's in the middle of the list)
            DomainUpDownDate.SelectedIndex = 367; // Index of current day (at position in the middle)

            // Prevent direct editing by user to avoid invalid formats
            DomainUpDownDate.ReadOnly = true;
        }

        private void TextBoxPoints_KeyPress(object sender, KeyPressEventArgs e)
        {
            //  to LostFocus()
            // allow all characters but only digits and control keys (backspace, delete, etc.) will be used for conversion
        }

        private void TextBoxPoints_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxPoints.Text) ||
                !int.TryParse(TextBoxPoints.Text, out _))
            {
                if (TextBoxPoints.Text != "0") 
                {
                    TextBoxPoints.Text = "0";
                }
            }
        }

        private void ButtonAddDate_Click(object sender, EventArgs e)
        {
            Console.WriteLine("ButtonAddDate_Click called");
            if (isAdding) return;
            isAdding = true;

            try
            {
                Console.WriteLine("Adding date...");
                // Parse date with validation
                string dateString = DomainUpDownDate.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(dateString))
                {
                    dateString = DateTime.Now.ToString("dd.MM.yyyy");
                    DomainUpDownDate.SelectedItem = dateString;
                }

                DateTime selectedColumnDate = DateTime.Now.Date;  
                IsValidDate(dateString, out selectedColumnDate);

                // Get values from textboxes
                string procedureColumnValue = TextBoxProcedure.Text;

                int pointsColumnValue = 0;
                if (!string.IsNullOrEmpty(TextBoxPoints.Text))
                {
                    if (!int.TryParse(TextBoxPoints.Text, out pointsColumnValue))
                    {
                        Console.WriteLine($"Invalid points value: {TextBoxPoints.Text}");
                        pointsColumnValue = 0;
                    }
                }
                Console.WriteLine($"Parsed points value: {pointsColumnValue}");
                string delegateColumnValue = TextBoxDelegate.Text;

                // Create and add new record
                DataItem newItem = new DataItem
                {
                    DateColumnValue = selectedColumnDate,
                    ProcedureColumnValue = procedureColumnValue,
                    PointsColumnValue = pointsColumnValue,
                    DelegateColumnValue = delegateColumnValue
                };
                Console.WriteLine($"New item created: {newItem.DateColumnValue}, {newItem.ProcedureColumnValue}, {newItem.PointsColumnValue}, {newItem.DelegateColumnValue}");
                // Add new record
                dataList.Add(newItem);

                // Set focus back to DomainUpDownDate
                DomainUpDownDate.Focus();

                //  After adding 
                if (DataGridView1.Rows.Count > 0)
                {
                    DataGridView1.ClearSelection();
                    DataGridView1.Rows[0].Selected = true;
                    DataGridView1.CurrentCell = DataGridView1.Rows[0].Cells[0];
                    DataGridView1.FirstDisplayedScrollingRowIndex = 0;
                    if (DataGridView1.CurrentRow != null)
                    {
                        DataGridView1.CurrentRow.Selected = true;
                        if (DataGridView1.CurrentRow.Cells.Count > 0)
                        {
                            DataGridView1.CurrentRow.Cells[0].Selected = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding date: {ex.Message}");
                MessageBox.Show($"Add date error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isAdding = false;
            }
        }

        private void ClearTextBoxes()
        {
            isClearing = true; //  flag
            try
            {
                TextBoxProcedure.Text = string.Empty;
                TextBoxPoints.Text = string.Empty;
                TextBoxDelegate.Text = string.Empty;
            }
            finally
            {
                isClearing = false;
            }
        }

        // Method for handling key press in DomainUpDownDate
        private void DomainUpDownDate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true; // Prevent further processing of the event
                e.SuppressKeyPress = true; // Prevent beep
                ButtonAddDate_Click(this, EventArgs.Empty);
            }
        }

        // Main method for adding date to DataGridView
        private void AddDateToDataGrid()
        {
            string dateString = DomainUpDownDate.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(dateString))
            {
                MessageBox.Show("Please select a valid date.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Parse date with validation
                if (!IsValidDate(dateString, out DateTime selectedDate))
                {
                    MessageBox.Show($"Invalid date format: {dateString}. Please use dd.MM.yyyy format.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create and add new record
                DataItem newItem = new DataItem
                {
                    DateColumnValue = selectedDate,
                    // Leave other fields empty or with default values
                };

                // Add new record to the beginning of the list
                dataList.Add(newItem);

                // Set focus back to DomainUpDownDate for next input
                DomainUpDownDate.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Add date error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            // Ensure the first row is always visible
            if (DataGridView1.Rows.Count > 0)
            {
                DataGridView1.FirstDisplayedScrollingRowIndex = 0;
            }
        }

        public class DataItem : INotifyPropertyChanged
        {
            private DateTime dateColumnValue = DateTime.Now;
            private string procedureColumnValue = string.Empty;
            private int pointsColumnValue;
            private string delegateColumnValue = string.Empty; 

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public DateTime DateColumnValue
            {
                get => dateColumnValue;
                set
                {
                    if (dateColumnValue != value.Date)
                    {
                        dateColumnValue = value.Date;
                        OnPropertyChanged(nameof(DateColumnValue));
                    }
                }
            }

            public string ProcedureColumnValue
            {
                get => procedureColumnValue;
                set
                {
                    if (procedureColumnValue != value)
                    {
                        procedureColumnValue = value;
                        OnPropertyChanged(nameof(ProcedureColumnValue));
                    }
                }
            }

            public int PointsColumnValue
            {
                get => pointsColumnValue;
                set
                {
                    if (pointsColumnValue != value)
                    {
                        pointsColumnValue = value;
                        OnPropertyChanged(nameof(PointsColumnValue));
                    }
                }
            }

            public string DelegateColumnValue // Updated property name to match the renamed field
            {
                get => delegateColumnValue;
                set
                {
                    if (delegateColumnValue != value)
                    {
                        delegateColumnValue = value;
                        OnPropertyChanged(nameof(DelegateColumnValue));
                    }
                }
            }
        }

        // Custom implementation of BindingList
        public class CustomBindingList<T> : BindingList<T>
        {
            protected override void InsertItem(int index, T item)
            {
                // Always insert at the beginning of the list
                base.InsertItem(0, item);
            }
        }

        // Method for removing selected record
        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            Console.WriteLine("ButtonRemove_Click called");
            // Prevent duplicate calls
            if (isDeleting) return;
            isDeleting = true;

            try
            {
                Console.WriteLine("Removing selected rows...");
                if (DataGridView1.SelectedRows.Count > 0)
                {
                    // Disable UI during deletion
                    DataGridView1.Enabled = false;

                    // First get CurrencyManager for bindingSource
                    CurrencyManager currencyManager =
                        (CurrencyManager)BindingContext[bindingSource];

                    // Temporarily suspend binding to prevent errors
                    currencyManager.SuspendBinding();

                    // Collect indexes of selected rows
                    List<int> selectedIndexes = new List<int>();
                    foreach (DataGridViewRow row in DataGridView1.SelectedRows)
                    {
                        if (!row.IsNewRow)
                        {
                            int rowIndex = row.Index;
                            if (rowIndex >= 0 && rowIndex < dataList.Count)
                            {
                                Console.WriteLine($"Row index: {rowIndex}");
                                selectedIndexes.Add(rowIndex);
                            }
                        }
                    }

                    // If no valid rows are selected, end
                    if (selectedIndexes.Count == 0)
                    {
                        Console.WriteLine("No valid rows selected.");
                        MessageBox.Show("No valid rows selected.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        currencyManager.ResumeBinding();
                        DataGridView1.Enabled = true;
                        return;
                    }

                    Console.WriteLine($"Selected indexes: {string.Join(", ", selectedIndexes)}");

                    // Disconnect DataSource before changing data
                    DataGridView1.DataSource = null;

                    // Sort indexes in descending order to remove from end first
                    selectedIndexes.Sort();
                    selectedIndexes.Reverse();

                    // Remove items from end to keep indexes consistent
                    foreach (int index in selectedIndexes)
                    {
                        if (index >= 0 && index < dataList.Count)
                        {
                            dataList.RemoveAt(index);
                        }
                    }

                    // Reconnect data source
                    bindingSource.DataSource = dataList;
                    DataGridView1.DataSource = bindingSource;

                    Console.WriteLine("Rebinding DataGridView...");
                    // Re-setup column mapping
                    DataGridView1.AutoGenerateColumns = false;
                    ColumnDate.DataPropertyName = "DateColumnValue";
                    ColumnDate.DefaultCellStyle.Format = "dd.MM.yyyy";
                    ColumnProcedure.DataPropertyName = "ProcedureColumnValue";
                    ColumnPoints.DataPropertyName = "PointsColumnValue";
                    ColumnDelegate.DataPropertyName = "DelegateColumnValue";

                    // Restore binding functionality
                    currencyManager.ResumeBinding();

                    Console.WriteLine("Resetting bindings...");
                    // Apply items
                    bindingSource.ResetBindings(false);
                    DataGridView1.Refresh();

                    // Re-enable UI
                    DataGridView1.Enabled = true;

                    // Clear selection and verify we have valid selection
                    if (DataGridView1.Rows.Count > 0)
                    {
                        DataGridView1.ClearSelection();
                        DataGridView1.Rows[0].Selected = true;
                        DataGridView1.FirstDisplayedScrollingRowIndex = 0;
                    }
                }
                else
                {
                    Console.WriteLine("No row selected to delete.");
                    MessageBox.Show("No row selected to delete.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing rows: {ex.Message}");
                MessageBox.Show($"Error removing rows: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Console.WriteLine("Finished removing rows.");
                isDeleting = false;
                DataGridView1.Enabled = true;
            }
        }

        // Method for adding/updating data from textboxes to DataGridView
        private void LabelsToDataGridView(object sender, EventArgs e)
        {
            // If UI changes are already in progress, do nothing
            if (isClearing || isUpdatingUI) return;
            isUpdatingUI = true;

            try
            {
                // Store sender as TextBox and remember cursor position
                TextBox textBox = sender as TextBox;
                int cursorPosition = -1;
                if (textBox != null)
                {
                    cursorPosition = textBox.SelectionStart;
                }

                // Get values from textboxes
                string procedureColumnValue = TextBoxProcedure.Text ?? string.Empty;
                string pointsText = TextBoxPoints.Text ?? string.Empty;
                string delegateColumnValue = TextBoxDelegate.Text ?? string.Empty;

                // Convert points to number, always defaulting to 0 for invalid/empty values
                int pointsColumnValue = 0;
                if (!string.IsNullOrWhiteSpace(pointsText))
                {
                    if (!int.TryParse(pointsText, out pointsColumnValue))
                    {
                        pointsColumnValue = 0;
                    }
                }

                // If we have a selected row in DataGridView, update it
                if (DataGridView1.CurrentRow != null && !DataGridView1.CurrentRow.IsNewRow)
                {
                    int rowIndex = DataGridView1.CurrentRow.Index;
                    if (rowIndex >= 0 && rowIndex < dataList.Count)
                    {
                        // Update existing record
                        dataList[rowIndex].ProcedureColumnValue = procedureColumnValue;
                        dataList[rowIndex].PointsColumnValue = pointsColumnValue;
                        dataList[rowIndex].DelegateColumnValue = delegateColumnValue;

                        // Refresh binding source to reflect changes in UI
                        bindingSource.ResetBindings(false);
                    }
                }

                // Restore cursor position
                if (textBox != null && cursorPosition != -1)
                {
                    textBox.SelectionStart = cursorPosition;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LabelsToDataGridView: {ex.Message}");
            }
            finally
            {
                isUpdatingUI = false;
            }
        }

        // Method for displaying selected record in textboxes
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (DataGridView1.CurrentRow != null && !DataGridView1.CurrentRow.IsNewRow)
            {
                int rowIndex = DataGridView1.CurrentRow.Index;
                if (rowIndex >= 0 && rowIndex < dataList.Count)
                {
                }
            }
        }

        private void ButtonOpenFolder_Click(object sender, EventArgs e)
        {
            // Open Windows Explorer general folder
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }

        // Save data to JSON file - Newtonsoft.Json
        private void SaveDataToJson(string filePath)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(dataList, Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
                MessageBox.Show("Data saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDataFromJson(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    var loadedData = JsonConvert.DeserializeObject<List<DataItem>>(jsonData);

                    // Clear existing data
                    dataList.Clear();

                    // Add loaded data
                    foreach (var item in loadedData)
                    {
                        dataList.Add(item);
                    }

                    // Refresh UI
                    bindingSource.ResetBindings(false);
                    MessageBox.Show("Data loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true; // stop of standard behavior
                ButtonRemove_Click(this, EventArgs.Empty); 
            }
        }

        private void ButtonSaveData_Click(object sender, EventArgs e)
        {
            SaveDataWithDialog();
        }

        private void ButtonLoadData_Click(object sender, EventArgs e)
        {
            QuickLoadData();
        }

        // Default name and path to file
        private string GetDefaultJsonFilePath()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsFolder, "TestBenchTargetWinForms.json");
        }

        // Method for quick save to default file
        private void QuickSaveData()
        {
            string defaultPath = GetDefaultJsonFilePath();
            SaveDataToJson(defaultPath);
        }

        private void SaveDataWithDialog()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Data As";
                saveFileDialog.DefaultExt = "json";
                saveFileDialog.FileName = "TestBenchTargetWinForms.json";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveDataToJson(saveFileDialog.FileName);
                }
            }
        }

        private void QuickLoadData()
        {
            string defaultPath = GetDefaultJsonFilePath();
            if (File.Exists(defaultPath))
            {
                LoadDataFromJson(defaultPath);
            }
            else
            {
                MessageBox.Show("No saved data found. Please save data first.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private bool IsValidDate(string dateString, out DateTime result)
        {
            // Trim the string to remove any leading or trailing whitespace
            dateString = dateString?.Trim();

            bool success = DateTime.TryParseExact(dateString, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out result);

            if (!success)
            {
                result = DateTime.Now.Date;
                return false;
            }

            return true;
        }

        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (isHandlingDataError) return;
            isHandlingDataError = true;

            try
            {                
                e.ThrowException = false;  // stop showing error message
                e.Cancel = true;  // mark error as handled

                if (e.ColumnIndex >= 0 && e.ColumnIndex < DataGridView1.Columns.Count &&
                    e.RowIndex >= 0)
                {
                    if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnPoints")
                    {
                        DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;

                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].PointsColumnValue = 0;
                        }

                        this.BeginInvoke(new Action(() => {
                            DataGridView1.Refresh();
                        }));
                    }
                    else if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnDate")
                    {
                        if (e.RowIndex < dataList.Count)
                        {
                            dataList[e.RowIndex].DateColumnValue = DateTime.Now.Date;
                            this.BeginInvoke(new Action(() => {
                                DataGridView1.Refresh();
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DataGridView1_DataError: {ex.Message}");
            }
            finally
            {
                isHandlingDataError = false;
            }
        }

        // Method for parsing cell values
        private void RefreshDataGridView()
        {
            try
            {
                // Disable and again connect DataSource UI during refresh
                DataGridView1.DataSource = null;
                bindingSource.DataSource = dataList;
                DataGridView1.DataSource = bindingSource;

                // reset mapping the columns
                ColumnDate.DataPropertyName = "DateColumnValue";
                ColumnDate.DefaultCellStyle.Format = "dd.MM.yyyy";
                ColumnProcedure.DataPropertyName = "ProcedureColumnValue";
                ColumnPoints.DataPropertyName = "PointsColumnValue";
                ColumnDelegate.DataPropertyName = "DelegateColumnValue";

                bindingSource.ResetBindings(false);
                DataGridView1.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing DataGridView: {ex.Message}");
                MessageBox.Show($"Error refreshing DataGridView: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Reset and restart timer on each text change
            updateTimer.Stop();
            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {          
            updateTimer.Stop();   // Stop timer
            LabelsToDataGridView(null, EventArgs.Empty); // Now update data
        }

        private void TextBoxPoints_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI) return;
            isUpdatingUI = true;

            try
            {
                updateTimer.Stop();

                if (string.IsNullOrWhiteSpace(TextBoxPoints.Text))
                {
                    TextBoxPoints.Text = "0";
                    TextBoxPoints.SelectionStart = 1; // cursor after number
                    updateTimer.Start();
                    return;
                }

                string cleanedText = string.Empty;
                foreach (char c in TextBoxPoints.Text)
                {
                    if (char.IsDigit(c))
                    {
                        cleanedText += c;
                    }
                }

                if (string.IsNullOrEmpty(cleanedText))
                {
                    TextBoxPoints.Text = "0";
                    TextBoxPoints.SelectionStart = 1;
                }

                else if (cleanedText != TextBoxPoints.Text)
                {
                    int cursorPosition = TextBoxPoints.SelectionStart;
                    TextBoxPoints.Text = cleanedText;

                    if (cursorPosition <= TextBoxPoints.Text.Length)
                    {
                        TextBoxPoints.SelectionStart = cursorPosition;
                    }
                    else
                    {
                        TextBoxPoints.SelectionStart = TextBoxPoints.Text.Length;
                    }
                }
                updateTimer.Start();  //  starts timer for update data 
            }
            finally
            {
                isUpdatingUI = false;
            }
        }

        private void TextBoxPoints_TextChanged_New(object sender, EventArgs e)
        {           
            if (isUpdatingUI) return;   //  recursive call prevention
            isUpdatingUI = true;

            try
            {
                updateTimer.Stop();  // stop timer for update

                int cursorPosition = TextBoxPoints.SelectionStart;  // remember cursor position

                if (string.IsNullOrWhiteSpace(TextBoxPoints.Text))  // control empty input 
                {
                    TextBoxPoints.Text = "0";
                    TextBoxPoints.SelectionStart = 1; // cursor after number
                }
                else
                {
                    // remove all not digit characters (numbers)
                    string cleanedText = string.Empty;
                    foreach (char c in TextBoxPoints.Text)
                    {
                        if (char.IsDigit(c))
                        {
                            cleanedText += c;
                        }
                    }

                    if (string.IsNullOrEmpty(cleanedText))  // if is value empty - set on 0
                    {
                        TextBoxPoints.Text = "0";
                        TextBoxPoints.SelectionStart = 1;
                    }
                    // if changed, update text
                    else if (cleanedText != TextBoxPoints.Text)
                    {
                        TextBoxPoints.Text = cleanedText;

                        // set position of cursor
                        if (cursorPosition <= TextBoxPoints.Text.Length)
                        {
                            TextBoxPoints.SelectionStart = cursorPosition;
                        }
                        else
                        {
                            TextBoxPoints.SelectionStart = TextBoxPoints.Text.Length;
                        }
                    }
                }                
                updateTimer.Start();  // refresh DataGridView with delay
            }
            finally
            {
                isUpdatingUI = false;
            }
        }


        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnPoints" ||
                DataGridView1.Columns[e.ColumnIndex].Name == "ColumnDate")
            {
                // allow all values ​​to pass validation - process empty values ​​in CellParsing and CellEndEdit
                e.Cancel = false;
                return;
            }
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // recursive call prevention
            if (isHandlingDataError) return;
            isHandlingDataError = true;

            try
            {
                // Skontrolujeme, ktorý stĺpec sa upravuje
                if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnPoints")
                {
                    //  PointsColumnValue
                    var cellValue = DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    bool setToZero = false;
                    int numericValue = 0;

                    if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        setToZero = true;
                    }
                    else if (!int.TryParse(cellValue.ToString(), out numericValue))
                    {
                        setToZero = true;  // if is not number, set to 0
                    }

                    if (setToZero)
                    {
                        // set cells to 0
                        DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;

                        // if is row binded to dataList, refresh source data 
                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].PointsColumnValue = 0;
                        }
                    }
                    else
                    {
                        // if is value valid number, update model
                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].PointsColumnValue = numericValue;
                        }
                    }
                }
                else if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnDate")
                {
                    var cellValue = DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    bool isValidDate = false;
                    DateTime dateColumnValue = DateTime.Now.Date; // default value if parsing fails is today's date

                    if (cellValue != null)
                    {
                        // try value converting to the date
                        isValidDate = DateTime.TryParseExact(
                            cellValue.ToString(),
                            "dd.MM.yyyy",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out dateColumnValue);
                    }

                    if (!isValidDate)
                    {
                        dateColumnValue = DateTime.Now.Date;
                        DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = dateColumnValue;

                        // refresh binding source if row is in dataList
                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].DateColumnValue = dateColumnValue; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DataGridView1_CellEndEdit: {ex.Message}");

                try
                {
                    if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnPoints")
                    {
                        DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0; // for points set to 0
                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].PointsColumnValue = 0;
                        }
                    }
                    else if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnDate")
                    {
                        DateTime today = DateTime.Now.Date;
                        DataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = today;

                        if (e.RowIndex < dataList.Count && !DataGridView1.Rows[e.RowIndex].IsNewRow)
                        {
                            dataList[e.RowIndex].DateColumnValue = today;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine($"Error setting default value: {ex.Message}");
                }
            }
            finally
            {
                isHandlingDataError = false;
            }
        }

        private void DataGridView1_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnPoints")
            {
                try
                {                   
                    if (e.Value == null || string.IsNullOrWhiteSpace(e.Value.ToString()))
                    {
                        e.Value = 0;
                        e.ParsingApplied = true; // Označíme, že sme hodnotu spracovali
                        return;
                    }

                    // if value is not null or empty, try to parse it
                    if (!int.TryParse(e.Value.ToString(), out int result))
                    {
                        e.Value = 0;
                        e.ParsingApplied = true;
                    }
                }
                catch
                {
                    e.Value = 0;
                    e.ParsingApplied = true;
                }
            }

            else if (DataGridView1.Columns[e.ColumnIndex].Name == "ColumnDate")
            {
                try
                {
                    // if is value null or empty replace with today's date
                    if (e.Value == null || string.IsNullOrWhiteSpace(e.Value.ToString()))
                    {
                        e.Value = DateTime.Now.Date;
                        e.ParsingApplied = true;
                        return;
                    }

                    // converting value to DateTime
                    if (!DateTime.TryParseExact(
                        e.Value.ToString(),
                        "dd.MM.yyyy",
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dateValue))
                    {
                        // if conversion fails, set to today's date
                        e.Value = DateTime.Now.Date;
                        e.ParsingApplied = true;
                    }
                }
                catch
                {
                    e.Value = DateTime.Now.Date;
                    e.ParsingApplied = true;
                }
            }
        }


        // *****************************************

        private void Form2_Load(object sender, EventArgs e)
        {
            // Úprava UI pri načítaní formulára
            AdjustUIForHighDpi();
        }

        // Pridajte túto novú metódu
        private void AdjustUIForHighDpi()
        {
            // Aplikujte úpravy len ak je DPI faktor väčší ako 1.5 (150%)
            if (_dpiScaleFactor > 1.5f)
            {
                // 1. Zväčšenie šírky formulára pre väčšie rozostupy
                this.Width = (int)(this.Width * 1.1f);

                // 2. Úprava veľkosti a umiestnenia vstupných polí a popiskov

                // Zväčšenie rozostupov medzi labelmi a kontrolkami
                LabelDate.Top = DomainUpDownDate.Top - (int)(22 * _dpiScaleFactor / 1.5f);
                LabelProcedure.Top = TextBoxProcedure.Top - (int)(22 * _dpiScaleFactor / 1.5f);
                LabelPoints.Top = TextBoxPoints.Top - (int)(22 * _dpiScaleFactor / 1.5f);
                LabelDelegate.Top = TextBoxDelegate.Top - (int)(22 * _dpiScaleFactor / 1.5f);

                // 3. Úprava rozostupu medzi tlačidlami v pravej časti
                int buttonSpacing = (int)(15 * _dpiScaleFactor);
                ButtonLoadData.Top = ButtonAddDate.Bottom + buttonSpacing;
                ButtonSaveData.Top = ButtonLoadData.Bottom + buttonSpacing;
                ButtonRemove.Top = ButtonSaveData.Bottom + buttonSpacing;
                ButtonOpenFolder.Top = ButtonRemove.Bottom + buttonSpacing;

                // 4. Zväčšenie výšky textových polí
                int textBoxHeight = (int)(TextBoxProcedure.Height * 1.1f);
                TextBoxProcedure.Height = textBoxHeight;
                TextBoxPoints.Height = textBoxHeight;
                TextBoxDelegate.Height = textBoxHeight;
                DomainUpDownDate.Height = textBoxHeight;

                // 5. Zväčšenie výšky tlačidiel
                int buttonHeight = (int)(ButtonAddDate.Height * 1.1f);
                ButtonAddDate.Height = buttonHeight;
                ButtonLoadData.Height = buttonHeight;
                ButtonSaveData.Height = buttonHeight;
                ButtonRemove.Height = buttonHeight;
                ButtonOpenFolder.Height = buttonHeight;

                // 6. Úprava DataGridView pre lepšie zobrazenie
                DataGridView1.Width = (int)(DataGridView1.Width * 0.95f);
                foreach (DataGridViewColumn column in DataGridView1.Columns)
                {
                    // Zväčšenie šírky hlavičiek stĺpcov
                    column.HeaderCell.Style.Font = new Font(column.HeaderCell.Style.Font.FontFamily,
                                                           column.HeaderCell.Style.Font.Size * (_dpiScaleFactor > 2.0f ? 0.8f : 0.9f),
                                                           column.HeaderCell.Style.Font.Style);
                }
            }
        }

        // Pridajte túto novú metódu pre podporu dynamickej zmeny DPI
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);

            // Výpočet zmeny DPI faktora
            float oldDpiScaleFactor = _dpiScaleFactor;
            _dpiScaleFactor = e.DeviceDpiNew / 96.0f;

            // Výpočet zmeny škálovania
            float scaleChange = _dpiScaleFactor / oldDpiScaleFactor;

            // Dočasné pozastavenie layoutu
            this.SuspendLayout();

            // Škálovanie formulára
            this.Scale(new SizeF(scaleChange, scaleChange));

            // Opätovná úprava rozloženia UI pre nové DPI
            AdjustUIForHighDpi();

            // Obnovenie layoutu
            this.ResumeLayout(true);
        }
    }
}
