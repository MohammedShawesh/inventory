using System;
using System.Data;
using System.Data.SqlClient;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
namespace inventory
{
    public class Class1
    {
        private string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\shawish\Downloads\inventory\inventory\bin\Debug\inventory.mdf;Integrated Security=True";

        public DataTable GetAllItems()
        {
            DataTable itemsTable = new DataTable();
            string query = "SELECT Name FROM Items";

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    itemsTable.Load(reader);
                }
                con.Close();
            }

            return itemsTable;
        }

        public void AddItem(string name, int quantity, decimal price)
        {
            string selectQuery = "SELECT ItemId, Quantity FROM Items WHERE Name = @name";
            int existingItemId = 0;
            int existingQuantity = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
            {
                selectCmd.Parameters.AddWithValue("@name", name);
                con.Open();
                using (SqlDataReader reader = selectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        existingItemId = Convert.ToInt32(reader["ItemId"]);
                        existingQuantity = Convert.ToInt32(reader["Quantity"]);
                    }
                }
                con.Close();
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                if (existingItemId > 0)
                {
                    int newQuantity = existingQuantity + quantity;
                    string updateQuery = "UPDATE Items SET Quantity = @newQuantity WHERE ItemId = @itemId";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@newQuantity", newQuantity);
                        updateCmd.Parameters.AddWithValue("@itemId", existingItemId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string insertQuery = "INSERT INTO Items (Name, Quantity, Price) VALUES (@name, @quantity, @price)";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@name", name);
                        insertCmd.Parameters.AddWithValue("@quantity", quantity);
                        insertCmd.Parameters.AddWithValue("@price", price);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void GenerateReport(string filePath)
        {
            DataTable itemsTable = new DataTable();
            string query = "SELECT Name, Quantity, Price FROM Items";

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    itemsTable.Load(reader);
                }
            }

            try
            {
                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    Document document = new Document(pdf);

                    document.Add(new Paragraph("Inventory Report")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(20));
                    document.Add(new Paragraph("Generated on: " + DateTime.Now.ToString())
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(12));
                    document.Add(new Paragraph("\n"));

                    Table table = new Table(3);
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Name")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Quantity")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Price")));

                    foreach (DataRow row in itemsTable.Rows)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(row["Name"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Quantity"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Price"].ToString())));
                    }

                    document.Add(table);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating PDF report: " + ex.Message);
            }
        }

        public void ArchiveItem(string name)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

               
                int itemId = 0;
                int quantity = 0;
                decimal price = 0;

                string selectQuery = "SELECT ItemId, Quantity, Price FROM Items WHERE Name = @Name";
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
                {
                    selectCmd.Parameters.AddWithValue("@Name", name);
                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            itemId = reader.GetInt32(reader.GetOrdinal("ItemId"));
                            quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                            price = reader.GetDecimal(reader.GetOrdinal("Price"));
                        }
                    }
                }

                if (itemId > 0)
                {
                   
                    string insertArchiveQuery = "INSERT INTO ArchivedItems (ItemId, Name, Quantity, Price, ArchivedDate) VALUES (@ItemId, @Name, @Quantity, @Price, @ArchivedDate)";
                    using (SqlCommand insertArchiveCmd = new SqlCommand(insertArchiveQuery, con))
                    {
                        insertArchiveCmd.Parameters.AddWithValue("@ItemId", itemId);
                        insertArchiveCmd.Parameters.AddWithValue("@Name", name);
                        insertArchiveCmd.Parameters.AddWithValue("@Quantity", quantity);
                        insertArchiveCmd.Parameters.AddWithValue("@Price", price);
                        insertArchiveCmd.Parameters.AddWithValue("@ArchivedDate", DateTime.Now);
                        insertArchiveCmd.ExecuteNonQuery();
                    }

                  
                    string deleteQuery = "DELETE FROM Items WHERE ItemId = @ItemId";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@ItemId", itemId);
                        deleteCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    throw new InvalidOperationException("Item not found.");
                }
            }
        }


        public void RestoreItem(string name)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

               
                string selectQuery = "SELECT ArchivedItemId, ItemId, Quantity, Price FROM ArchivedItems WHERE Name = @Name";
                int archivedItemId;
                int itemId;
                int quantity;
                decimal price;

                using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
                {
                    selectCmd.Parameters.AddWithValue("@Name", name);
                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            archivedItemId = reader.GetInt32(reader.GetOrdinal("ArchivedItemId"));
                            itemId = reader.GetInt32(reader.GetOrdinal("ItemId"));
                            quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                            price = reader.GetDecimal(reader.GetOrdinal("Price"));
                        }
                        else
                        {
                            throw new InvalidOperationException("Archived item not found.");
                        }
                    }
                }

                string insertItemQuery = "INSERT INTO Items (Name, Quantity, Price) VALUES (@Name, @Quantity, @Price)";

                using (SqlCommand insertItemCmd = new SqlCommand(insertItemQuery, con))
                {
                    insertItemCmd.Parameters.AddWithValue("@Name", name);
                    insertItemCmd.Parameters.AddWithValue("@Quantity", quantity);
                    insertItemCmd.Parameters.AddWithValue("@Price", price);
                    insertItemCmd.ExecuteNonQuery();
                }

                string deleteArchiveQuery = "DELETE FROM ArchivedItems WHERE ArchivedItemId = @ArchivedItemId";

                using (SqlCommand deleteArchiveCmd = new SqlCommand(deleteArchiveQuery, con))
                {
                    deleteArchiveCmd.Parameters.AddWithValue("@ArchivedItemId", archivedItemId);
                    deleteArchiveCmd.ExecuteNonQuery();
                }
            }
        }


        public void GenerateArchivedItemsReport(string filePath)
        {
            DataTable archivedItemsTable = new DataTable();
            string query = "SELECT Name, Quantity, Price, ArchivedDate FROM ArchivedItems";

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    archivedItemsTable.Load(reader);
                }
            }

            try
            {
                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    Document document = new Document(pdf);

                    document.Add(new Paragraph("Archived Items Report")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(20));
                    document.Add(new Paragraph("Generated on: " + DateTime.Now.ToString())
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(12));
                    document.Add(new Paragraph("\n"));

                    Table table = new Table(4);
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Name")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Quantity")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Price")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Archived Date")));

                    foreach (DataRow row in archivedItemsTable.Rows)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(row["Name"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Quantity"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Price"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(((DateTime)row["ArchivedDate"]).ToString("g"))));
                    }

                    document.Add(table);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating PDF report: " + ex.Message);
            }
        }

        public void RemoveItem(string name, int quantity)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                int itemId = 0;
                int currentQuantity = 0;
                decimal price = 0;

                string selectQuery = "SELECT ItemId, Quantity, Price FROM Items WHERE Name = @Name";
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
                {
                    selectCmd.Parameters.AddWithValue("@Name", name);
                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            itemId = reader.GetInt32(reader.GetOrdinal("ItemId"));
                            currentQuantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                            price = reader.GetDecimal(reader.GetOrdinal("Price"));
                        }
                    }
                }

                if (currentQuantity < quantity)
                {
                    throw new InvalidOperationException("Not enough quantity to remove.");
                }

                // Update quantity in Items
                string updateQuery = "UPDATE Items SET Quantity = Quantity - @Quantity WHERE ItemId = @ItemId";
                using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                {
                    updateCmd.Parameters.AddWithValue("@ItemId", itemId);
                    updateCmd.Parameters.AddWithValue("@Quantity", quantity);
                    updateCmd.ExecuteNonQuery();
                }

                // Insert into RemovedItems
                string insertRemovedQuery = "INSERT INTO RemovedItems (ItemId, Name, Quantity, Price, RemovedDate) VALUES (@ItemId, @Name, @Quantity, @Price, @RemovedDate)";
                using (SqlCommand insertRemovedCmd = new SqlCommand(insertRemovedQuery, con))
                {
                    insertRemovedCmd.Parameters.AddWithValue("@ItemId", itemId);
                    insertRemovedCmd.Parameters.AddWithValue("@Name", name);
                    insertRemovedCmd.Parameters.AddWithValue("@Quantity", quantity);
                    insertRemovedCmd.Parameters.AddWithValue("@Price", price);
                    insertRemovedCmd.Parameters.AddWithValue("@RemovedDate", DateTime.Now);
                    insertRemovedCmd.ExecuteNonQuery();
                }
            }
        }

        public void GenerateRemovedItemsReport(string filePath)
        {
            DataTable removedItemsTable = new DataTable();
            string query = "SELECT Name, Quantity, Price, RemovedDate FROM RemovedItems";

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    removedItemsTable.Load(reader);
                }
            }

            try
            {
                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    Document document = new Document(pdf);

                    document.Add(new Paragraph("Removed Items Report")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(20));
                    document.Add(new Paragraph("Generated on: " + DateTime.Now.ToString())
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(12));
                    document.Add(new Paragraph("\n"));

                    Table table = new Table(4);
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Name")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Quantity")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Price")));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Removed Date")));

                    foreach (DataRow row in removedItemsTable.Rows)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(row["Name"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Quantity"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(row["Price"].ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(((DateTime)row["RemovedDate"]).ToString("g"))));
                    }

                    document.Add(table);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating PDF report: " + ex.Message);
            }
        }
    }

    } 

