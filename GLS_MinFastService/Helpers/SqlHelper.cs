using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLS_MinFastService.Helpers
{
    using Microsoft.Extensions.Configuration;
    using System.Collections;
    using System.Data;
    using System.Data.SqlClient;

    public class SqlHelper
    {
        private readonly string? _connectionString;

        public SqlHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Connection");
        }

        public DataTable QueryNotParamAsDatatable(string proc, string? connectionString)
        {
            try
            {

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new(proc, conn)
                    {
                        CommandType = CommandType.Text
                    };
                    SqlDataAdapter dataAdapt = new()
                    {
                        SelectCommand = cmd
                    };
                    DataTable dataTable = new();
                    dataAdapt.Fill(dataTable);
                    return dataTable;
                }
            }
            catch
            {
                throw;
            }
        }
        public void ExecSqlNonQuery(string sql)
        {
            try
            {


                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn)
                    {
                        CommandType = CommandType.Text
                    };
                    cmd.ExecuteNonQuery();

                }
            }
            catch
            {
                throw;

            }
        }
        public DataTable QueryAsDataTable(Hashtable param, string sp_proc, string? connectionString)
        {
            try
            {
                DataTable dt = new DataTable();


                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sp_proc, conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    foreach (DictionaryEntry de in param)
                    {
                        cmd.Parameters.Add(new SqlParameter(de.Key.ToString(), de.Value));
                    }
                    SqlDataAdapter dataAdapt = new SqlDataAdapter
                    {
                        SelectCommand = cmd
                    };
                    dataAdapt.Fill(dt);
                    return dt;
                }
            }
            catch
            {
                throw;
            }
        }
        public DataSet QueryAsDataSet(Hashtable param, string sp_proc)
        {

            try
            {
                DataSet ds = new DataSet();


                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sp_proc, conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    foreach (DictionaryEntry de in param)
                    {
                        cmd.Parameters.Add(new SqlParameter(de.Key.ToString(), de.Value));
                    }
                    SqlDataAdapter dataAdapt = new SqlDataAdapter
                    {
                        SelectCommand = cmd
                    };
                    dataAdapt.Fill(ds);
                    return ds;
                }
            }
            catch
            {
                throw;
            }

        }
        public void ExecuteInsertStoredProc()
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using SqlCommand command = new("proc_MinFast_InsertMinCheck", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@BranchId", 2);
                command.Parameters.AddWithValue("@BranchName", "test");
                command.Parameters.AddWithValue("@PosData", "400");
                command.Parameters.AddWithValue("@CenterData", "600");
                command.Parameters.AddWithValue("@DifferenceStatusOfPosAndCenter", 0);
                command.Parameters.AddWithValue("@EodStatusYesterday", 0);
                command.Parameters.AddWithValue("@CheckStatus", 0);
                command.Parameters.AddWithValue("@Executed", 0);

                command.ExecuteNonQuery();
            }
        }
    }
}
