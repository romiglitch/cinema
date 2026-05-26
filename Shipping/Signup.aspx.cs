using DALLlilbrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    // עמוד הרשמה - מאפשר למשתמש חדש ליצור חשבון ולהתחבר מיד לאחר מכן
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BtnSign_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            try
            {
                string pass = TxtPassword.Text;
                string fullname = TxtName.Text.Trim();
                // Store/login by normalized email to avoid case/space duplicates.
                string email = EmailHelper.Normalize(TxtEmail.Text);
                string phone = TxtPhone.Text.Trim();
                bool isAdmin = false;

                string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand existsCmd = new SqlCommand(
                        "SELECT COUNT(1) FROM Users WHERE Email = @email", connection))
                    {
                        existsCmd.Parameters.AddWithValue("@email", email);
                        int existing = Convert.ToInt32(existsCmd.ExecuteScalar());
                        if (existing > 0)
                        {
                            // Email is the username, so it must be unique.
                            msg.Text = "כתובת האימייל כבר רשומה במערכת";
                            return;
                        }
                    }

                    string insertQuery = @"INSERT INTO Users (FullName, Password, Phone, Email, IsAdmin)
                        VALUES (@fullname, @pass, @phone, @email, @isAdmin);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int userId;
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                    {
                        // Parameterized INSERT prevents injection and handles special characters safely.
                        insertCmd.Parameters.AddWithValue("@fullname", fullname);
                        insertCmd.Parameters.AddWithValue("@pass", pass);
                        insertCmd.Parameters.AddWithValue("@phone", phone);
                        insertCmd.Parameters.AddWithValue("@email", email);
                        insertCmd.Parameters.AddWithValue("@isAdmin", isAdmin);

                        userId = Convert.ToInt32(insertCmd.ExecuteScalar());
                    }

                    Session["UserId"] = userId;
                    Session["UserEmail"] = email;
                    // DisplayName is used for header + receipts (full name), while email is the login identifier.
                    Session["displayName"] = fullname;
                    Session["category"] = "user";

                    Response.Redirect("HomePage.aspx");
                }
            }
            catch (Exception ex)
            {
                msg.Text = "שגיאה ברישום";
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
