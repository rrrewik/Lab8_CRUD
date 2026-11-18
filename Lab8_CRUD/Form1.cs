using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Lab8_CRUD
{
    public partial class Form1 : Form
    {
        // Строка подключения к твоему серверу
        private string connectionString = @"Data Source=SALIKAEVPD07;Initial Catalog=StudentDB;Integrated Security=True;";

        // Переменная для хранения выбранного ID (для Update и Delete)
        private int selectedStudentId = -1;

        public Form1()
        {
            InitializeComponent();

            // Привязка событий
            this.Load += new EventHandler(Form1_Load);
            this.dataGridViewStudents.SelectionChanged += new EventHandler(dataGridViewStudents_SelectionChanged);
            this.buttonAdd.Click += new EventHandler(buttonAdd_Click);
            this.buttonUpdate.Click += new EventHandler(buttonUpdate_Click);
            this.buttonDelete.Click += new EventHandler(buttonDelete_Click);
            this.buttonClear.Click += new EventHandler(buttonClear_Click);
            this.buttonExport.Click += new EventHandler(buttonExport_Click);
            this.buttonCancel.Click += new EventHandler(buttonCancel_Click);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        // --- READ: Загрузка данных ---
        private void LoadData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT Id, FullName, Age, GroupName, AverageGrade FROM Students ORDER BY Id";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dataGridViewStudents.DataSource = dt;

                    if (dataGridViewStudents.Columns["Id"] != null)
                        dataGridViewStudents.Columns["Id"].Visible = false;

                    UpdateStatus($"Данные загружены.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        // Выбор строки в таблице
        private void dataGridViewStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewStudents.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dataGridViewStudents.SelectedRows[0];
                if (row.Cells["Id"].Value != null)
                {
                    selectedStudentId = Convert.ToInt32(row.Cells["Id"].Value);
                    textBoxName.Text = row.Cells["FullName"].Value.ToString();
                    numericAge.Value = Convert.ToDecimal(row.Cells["Age"].Value);
                    textBoxGroup.Text = row.Cells["GroupName"].Value.ToString();
                    numericGrade.Value = Convert.ToDecimal(row.Cells["AverageGrade"].Value);
                }
            }
        }

        // --- CREATE: Добавление ---
        private void buttonAdd_Click(object sender, EventArgs e)
        {
            // Улучшенная проверка: убираем пробелы по краям и смотрим, пусто ли поле
            string name = textBoxName.Text.Trim();
            string group = textBoxGroup.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(group))
            {
                MessageBox.Show($"Вы ввели:\nФИО: '{name}'\nГруппа: '{group}'\n\nПоля не должны быть пустыми!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка на дубликат
                    string checkQuery = "SELECT COUNT(*) FROM Students WHERE FullName = @name";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@name", name);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("Студент с таким ФИО уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Добавление
                    string query = "INSERT INTO Students (FullName, Age, GroupName, AverageGrade) VALUES (@name, @age, @group, @grade)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@age", (int)numericAge.Value);
                    command.Parameters.AddWithValue("@group", group);
                    command.Parameters.AddWithValue("@grade", numericGrade.Value);

                    command.ExecuteNonQuery();
                }

                LoadData();
                ClearFields();
                UpdateStatus("Студент успешно добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        // --- UPDATE: Обновление ---
        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (selectedStudentId == -1)
            {
                MessageBox.Show("Выберите студента из таблицы для изменения!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = textBoxName.Text.Trim();
            string group = textBoxGroup.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(group))
            {
                MessageBox.Show("Заполните ФИО и Группу!");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Students SET FullName = @name, Age = @age, GroupName = @group, AverageGrade = @grade WHERE Id = @id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@age", (int)numericAge.Value);
                    command.Parameters.AddWithValue("@group", group);
                    command.Parameters.AddWithValue("@grade", numericGrade.Value);
                    command.Parameters.AddWithValue("@id", selectedStudentId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                LoadData();
                ClearFields();
                UpdateStatus("Данные студента обновлены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }

        // --- DELETE: Удаление ---
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (selectedStudentId == -1)
            {
                MessageBox.Show("Выберите студента из таблицы для удаления!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить студента {textBoxName.Text}?",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Students WHERE Id = @id";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@id", selectedStudentId);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    LoadData();
                    ClearFields();
                    UpdateStatus("Студент удален.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message);
                }
            }
        }

        // Очистка полей
        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearFields();
            UpdateStatus("Поля очищены.");
        }

        private void ClearFields()
        {
            textBoxName.Clear();
            textBoxGroup.Clear();
            numericAge.Value = 15;
            numericGrade.Value = 0;
            selectedStudentId = -1;
            dataGridViewStudents.ClearSelection();
        }

        // --- ЭКСПОРТ В CSV ---
        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (dataGridViewStudents.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV файлы (*.csv)|*.csv";
            sfd.FileName = "Students.csv";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Id;FullName;Age;GroupName;AverageGrade");

                    foreach (DataGridViewRow row in dataGridViewStudents.Rows)
                    {
                        if (!row.IsNewRow)
                        {
                            sb.AppendLine($"{row.Cells["Id"].Value};{row.Cells["FullName"].Value};{row.Cells["Age"].Value};{row.Cells["GroupName"].Value};{row.Cells["AverageGrade"].Value}");
                        }
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    UpdateStatus("Данные экспортированы в CSV.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка экспорта: " + ex.Message);
                }
            }
        }

        // --- ОТМЕНА ---
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            LoadData();
            ClearFields();
            UpdateStatus("Изменения отменены, данные перезагружены.");
        }

        // Счетчик студентов
        private void UpdateStatus(string message)
        {
            int count = dataGridViewStudents.Rows.Count;
            if (dataGridViewStudents.AllowUserToAddRows) count--;

            labelStatus.Text = $"{message} | Всего студентов: {count}";
        }
    }
}