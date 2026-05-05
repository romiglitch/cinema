using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALLlilbrary
{
    public class DAL
    {
        public string con { get; set; } // connection string
        public string com { get; set; } // שאילתא
        public string table { get; set; } // שם של טבלה
        public List<SqlParameter> Params = new List<SqlParameter>();

        // פעולה בונה המקבלת את שלושת המאפיינים של המחלקה
        public DAL(string connectionString, string sqlCommand, string tableName)
        {
            con = connectionString;
            com = sqlCommand;
            table = tableName;
        }
        public DAL(string connectionString, string sqlCommand, string tableName, List<SqlParameter> P)
        {
            con = connectionString;
            com = sqlCommand;
            table = tableName;
            Params = P;
        }
        public DataTable GetData()
        {
            SqlCommand cmd = new SqlCommand(com);
            using (SqlConnection connection = new SqlConnection(con))
            {
                cmd.Connection = connection;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        // מחזירה טבלה דאטא-טייבל עם ערכים
        public DataTable GetTable()
        {
            // משתמש בקשר עם השרת
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open(); // פותח את הקשר
                SqlDataAdapter ClientAdapter = new SqlDataAdapter(com, connection); // מתאם בין השאילתא לשרת
                DataSet dataSet = new DataSet(); // יוצר דאטא-סט חדשה
                ClientAdapter.Fill(dataSet, table); // ממלא את הטבלה - נותן לה ערכים
                return dataSet.Tables[table]; // מחזיר את הטבלה
            }
        }
        // מחזירה DataTable עם פרמטרים (ל־SELECT מותאם)
        public DataTable GetTableWithParams()
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                using (SqlCommand cmd = new SqlCommand(com, connection))
                {
                    foreach (var p in Params)
                        cmd.Parameters.Add(p);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }
        // מחזיר טבלה דאטא-סט עם ערכים
        public DataSet GetFilledDataSet()
        {
            DataSet dataSet = new DataSet();
            using (SqlConnection connection = new SqlConnection(con)) // אם הצליח לפתוח את הקשר למסד הנתונים תמשיך הלאה
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(com, connection); // פעולה שמקבלת את השאילתא והחיבור למסד הנתונים
                SqlDataAdapter adapter = new SqlDataAdapter(cmd); // מבטא את המתאם בין מסד הנתונים 
                adapter.Fill(dataSet); // ממלא את הטבלה - נותן לה ערכים
            }
            return dataSet;
        }

        // מבצע את השאילתא אשר לא מחזירה ערך
        public void ExecuteNonQueryDal()
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(com, connection);
                cmd.ExecuteNonQuery();//מחזירה את מספר השורות שהושפעו מהשאילתא, אבל הפונקציה לא מחזירה כלום ExecuteNonQuery
            }
        }
        public void ExecuteNonQueryDalPar()
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(com, connection);

                // הוספת כל הפרמטרים
                foreach (var p in Params)
                    cmd.Parameters.Add(p);

                cmd.ExecuteNonQuery();
            }
        }

        // מבצע את השאילתא אשר מחזירה ערך ומחזיר אובייקט ראשוני שהושפע
        public object ExecuteScalarDal()
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(com, connection);
                return cmd.ExecuteScalar();
            }
        }
        //SqlCommandל Paramsמבצע את השאילתא שמחזירה ערך יחיד ומחזיר אובייקט, כולל הוספת כל הפרמטרים מ
        public object ExecuteScalarDalPar()
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(com, connection);

                // הוספת כל הפרמטרים ל-SqlCommand לפני הריצה
                if (Params != null)
                {
                    foreach (SqlParameter p in Params)
                    {
                        cmd.Parameters.Add(p);
                    }
                }

                return cmd.ExecuteScalar();
            }
        }

    }
}

