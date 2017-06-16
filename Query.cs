using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Security.Principal;

namespace Database
{
    public class Query
    {
        public string sql { get; set; }
        public List<Parameter> parameters = new List<Parameter>();
        public CommandType command_type = CommandType.Text;

        private string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

        public Query()
        {

        }

        public Query(string SQL)
        {
            sql = SQL;
        }


        public void AddParameter(string parameterName, object parameterValue, SqlDbType parameterDbType)
        {
            Parameter parameter = new Parameter();
            parameter.name = parameterName;
            if (parameterValue != null)
            {
                if (parameterDbType == SqlDbType.Structured)
                    parameter.table = (DataTable)parameterValue;
                else
                    parameter.value = parameterValue.ToString();
            }
            else
                parameter.value = null;

            parameter.type = parameterDbType;

            this.parameters.Add(parameter);
        }


        public string GetScalar()
        {
            string sResult = "";
            SqlConnection conn = new SqlConnection(connectionString);

            conn.Open();

            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandType = command_type;

            foreach (Parameter parameter in parameters)
            {
                var val = parameter.value;
                command.Parameters.Add(parameter.name, parameter.type).Value = val;
            }

            try
            {
                sResult = command.ExecuteScalar().ToString();
            }
            finally
            {
                conn.Close();
            }

            return sResult;
        }

        public DataRow GetDataRow()
        {
            try
            {
                DataRow dr = GetDataTable().Rows[0];
                return dr;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public DataRow GetDataRow(int row)
        {
            return GetDataTable().Rows[row];
        }

        public DataTable GetDataTable()
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(connectionString);

            conn.Open();

            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandType = command_type;

            foreach (Parameter parameter in parameters)
            {
                var val = parameter.value;

                if (parameter.type == SqlDbType.Bit)
                    command.Parameters.Add(parameter.name, parameter.type).Value = Convert.ToInt16(Convert.ToBoolean(val));
                else if (parameter.type != SqlDbType.Structured)
                    command.Parameters.Add(parameter.name, parameter.type).Value = val;
                else
                {
                    var table = parameter.table;
                    command.Parameters.AddWithValue(parameter.name, table);
                }
            }

            try
            {
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
            }
            finally
            {
                conn.Close();
            }


            return dt;
        }

        public DataSet GetDataSet()
        {
            DataSet ds = new DataSet();

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandType = command_type;
            command.CommandTimeout = 0;

            foreach (Parameter parameter in parameters)
            {
                if (parameter.type != SqlDbType.Structured)
                {
                    var val = parameter.value;
                    command.Parameters.Add(parameter.name, parameter.type).Value = val;
                }
                else
                {
                    var table = parameter.table;
                    command.Parameters.AddWithValue(parameter.name, table);
                }
            }

            try
            {
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(ds);
            }
            finally
            {
                conn.Close();
            }

            return ds;
        }


        public bool ExecuteNonQuery()
        {
            int iResult = 0;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandType = command_type;

            foreach (Parameter parameter in parameters)
            {
                var val = parameter.value;
                if (parameter.type == SqlDbType.Bit)
                    command.Parameters.Add(parameter.name, parameter.type).Value = Convert.ToInt16(Convert.ToBoolean(val));
                else if (parameter.type == SqlDbType.Structured)
                {
                    var table = parameter.table;
                    command.Parameters.AddWithValue(parameter.name, table);
                }
                else
                    command.Parameters.Add(parameter.name, parameter.type).Value = (object)val ?? DBNull.Value;
            }

            try
            {
                iResult = command.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }

            return (iResult > 0);
        }

        public dynamic ExecuteNonQueryWithReturnValue(SqlDbType sSqlDbType)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            object result = "";

            conn.Open();

            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandType = command_type;

            foreach (Parameter parameter in parameters)
            {
                var val = parameter.value;
                if (parameter.type == SqlDbType.Bit)
                    command.Parameters.Add(parameter.name, parameter.type).Value = Convert.ToInt16(Convert.ToBoolean(val));
                else if (parameter.type == SqlDbType.Structured)
                {
                    var table = parameter.table;
                    command.Parameters.AddWithValue(parameter.name, table);
                }
                else
                    command.Parameters.Add(parameter.name, parameter.type).Value = val;
            }

            var returnParameter = command.Parameters.Add("@ReturnVal", sSqlDbType);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            try
            {
                command.ExecuteNonQuery();
                result = returnParameter.Value;
            }
            finally
            {
                conn.Close();
            }

            return result;
        }


        public string QueryAsText()
        {
            string tmp = "";

            if (sql != null)
                tmp = sql;

            if (command_type == CommandType.StoredProcedure)
            {
                tmp = "exec " + tmp + " ";
                foreach (Parameter param in parameters)
                    tmp += ParamFormat(param) + ", ";

                if (parameters.Count > 0)
                    tmp = tmp.Substring(0, tmp.Length - 2);
            }
            else
            {
                foreach (Parameter param in parameters)
                    tmp = tmp.Replace(param.name, ParamFormat(param));
            }

            return tmp;
        }

        // Used by QueryAsText to determine whether or nto parameter values will be wrapped in ' 
        private static string ParamFormat(Parameter param)
        {
            string tmp = param.value;

            if (tmp != "null")
            {
                switch (param.type)
                {
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Char:
                    case SqlDbType.DateTime:
                    case SqlDbType.Date:
                        tmp = "'" + param.value + "'";
                        break;
                }
            }

            return tmp;
        }
    }
}
