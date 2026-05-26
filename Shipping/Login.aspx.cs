using DALLlilbrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    public partial class Login1 : System.Web.UI.Page
    {
        // Admin login is email-based like users, but uses a fixed credential.
        private const string AdminEmail = "admin@cinema.edu";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            // Email is the login identifier; normalize to match DB/index rules.
            string email = EmailHelper.Normalize(TxtEmail.Text);
            string password = TxtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                LblMsg.Text = "יש להזין אימייל וסיסמא";
                return;
            }

            if (email == AdminEmail && password == "1234")
            {
                Session["category"] = "admin";
                // UI displays a friendly name; login identity remains email.
                Session["displayName"] = "מנהל מערכת";
                Response.Redirect("AdminPage.aspx");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string strQuery = "SELECT UserId, Email, FullName FROM Users WHERE Email=@email AND Password=@pass";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(strQuery, con);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pass", password);

                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    // Persist both identity (UserId/Email) and display name for consistent UI and checkout.
                    Session["UserId"] = rdr["UserId"];
                    Session["UserEmail"] = rdr["Email"];
                    Session["displayName"] = rdr["FullName"];
                    Session["category"] = "user";

                    string returnUrl = Request.QueryString["returnUrl"];

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        Response.Redirect(returnUrl);
                    }
                    else
                    {
                        Response.Redirect("Movies.aspx");
                    }
                }
                else
                {
                    LblMsg.Text = "אימייל או סיסמא לא נכונים";
                }
            }
        }

        protected void BtnForgotPassword_Click(object sender, EventArgs e)
        {
            Response.Redirect("ForgotPassword.aspx");
        }
    }
}
