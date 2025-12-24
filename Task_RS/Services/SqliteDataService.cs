using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.Sqlite;
using Task_RS.DTOs;
using Task_RS.Interfaces;

namespace Task_RS.Services
{
    public class SqliteDataService : IDataService
    {
        private readonly string _connectionString;

        public SqliteDataService()
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "data.db");
            _connectionString = $"Data Source={dbPath}";

            Task.Run(() => InitializeDatabase()).GetAwaiter().GetResult();
        }


        private async Task InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var tableCmd = @"
                CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Unit TEXT NOT NULL,
                PriceEur REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending'
                );
                ";






            var createGroupsTable = @"
                CREATE TABLE IF NOT EXISTS Groups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PriceEur REAL NOT NULL
                );
            ";


            var createProductsByGroupTable = @"
                CREATE TABLE IF NOT EXISTS ProductsByGroup (
                    Id INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    PriceEur REAL NOT NULL,
                    Quantity INTEGER NOT NULL,

                    FOREIGN KEY (Id) REFERENCES Groups(Id)
                );
            ";


            using var cmd1 = new SqliteCommand(tableCmd, conn);
            using var cmd2 = new SqliteCommand(createGroupsTable, conn);
            using var cmd3 = new SqliteCommand(createProductsByGroupTable, conn);

            await cmd1.ExecuteNonQueryAsync();
            await cmd2.ExecuteNonQueryAsync();
            await cmd3.ExecuteNonQueryAsync();
        }

        public async Task AddProductsAsync(IEnumerable<ProductDto> products)
        {

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            const string query = @"
            INSERT INTO Products (Name, Unit, PriceEur, Quantity)
            VALUES (@Name, @Unit, @PriceEur, @Quantity);
            ";

            foreach (var item in products)
            {
                using var cmd = new SqliteCommand(query, conn);

                cmd.Parameters.AddWithValue("@Name", item.Name);
                cmd.Parameters.AddWithValue("@Unit", item.Unit);
                cmd.Parameters.AddWithValue("@PriceEur", item.PriceEur);
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                await cmd.ExecuteNonQueryAsync();
            }

        }

        public async Task<List<Product>> GetProductsSortedByPrice()
        {
            var result = new List<Product>();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            const string query = @"
            SELECT Id, Name, Unit, PriceEur, Quantity
            FROM Products
            WHERE Status != 'Processed'
            ORDER BY PriceEur DESC;
            ";

            using var command = new SqliteCommand(query, conn);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var dto = new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Unit = reader.GetString(2),
                    PriceEur = reader.GetDecimal(3),
                    Quantity = reader.GetInt32(4)
                };

                result.Add(dto);
            }

            //if (result.Count > 0)
            //{
            //    var ids = string.Join(",", result.Select(p => p.Id));
            //    var updateQuery = $"UPDATE Products SET Status = 'Processed' WHERE Id IN ({ids})";

            //    using var updateCommand = new SqliteCommand(updateQuery, conn);
            //    await updateCommand.ExecuteNonQueryAsync();
            //}








            return result;
        }

        public async Task<decimal> GetSum()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand(
                "SELECT SUM(Quantity * PriceEur) FROM Products WHERE Status != 'Processed'",
                connection);

            decimal totalSum = command.ExecuteScalar() is DBNull
                ? 0m
                : Convert.ToDecimal(command.ExecuteScalar());

            return totalSum;
        }


        public async Task AddGroupAsync(List<List<Product>> products, List<decimal> prices)
        {

            if (products.Count != prices.Count)
                throw new ArgumentException("Количество групп и цен должно совпадать");

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();


            using var transaction = conn.BeginTransaction();

            try
            {
                var insertGroupCmd = new SqliteCommand(
                    "INSERT INTO Groups (PriceEur) VALUES (@price); SELECT last_insert_rowid();",
                    conn,
                    transaction);

                var insertProductCmd = new SqliteCommand(
                    @"INSERT INTO ProductsByGroup
              (Id, Name, Unit, PriceEur, Quantity)
              VALUES (@groupId, @name, @unit, @price, @qty)",
                    conn,
                    transaction);

                for (int i = 0; i < products.Count; i++)
                {
                    insertGroupCmd.Parameters.Clear();
                    insertGroupCmd.Parameters.AddWithValue("@price", prices[i]);


                    long groupId = Convert.ToInt64(insertGroupCmd.ExecuteScalar() ?? throw new Exception("Failed to get Id"));


                    
                    foreach (var p in products[i])
                    {
                        insertProductCmd.Parameters.Clear();

                        insertProductCmd.Parameters.AddWithValue("@groupId", groupId);
                        insertProductCmd.Parameters.AddWithValue("@name", p.Name);
                        insertProductCmd.Parameters.AddWithValue("@unit", p.Unit);
                        insertProductCmd.Parameters.AddWithValue("@price", p.PriceEur);
                        insertProductCmd.Parameters.AddWithValue("@qty", p.Quantity);

                        insertProductCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<GroupDto>> GetGroupsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT Id, PriceEur FROM Groups";
            var groups = await connection.QueryAsync<GroupDto>(query);
            return groups;
        }

        public async Task<IEnumerable<ProductByGroupDto>> GetProductsByGroupAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT Id, Name, Unit, PriceEur, Quantity FROM ProductsByGroup WHERE Id = @Id";
            var productsByGroup = await connection.QueryAsync<ProductByGroupDto>(query, new { Id = id });
            return productsByGroup;
        }


        public async Task SetStatusAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            var parameters = new List<string>();
            var cmd = new SqliteCommand();
            cmd.Connection = conn;
            cmd.Transaction = transaction;

            for (int i = 0; i < ids.Count; i++)
            {
                string paramName = $"@id{i}";
                parameters.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, ids[i]);
            }

            cmd.CommandText = $"UPDATE Products SET Status = 'Processed' WHERE Id IN ({string.Join(",", parameters)})";

            await cmd.ExecuteNonQueryAsync();
            transaction.Commit();
        }

    }
}

