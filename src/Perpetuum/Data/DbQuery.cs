using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Data
{
    public delegate IDbConnection DbConnectionFactory();

    public class DbQuery
    {
        private readonly DbConnectionFactory _connectionFactory;

        private string _commandText = string.Empty;
        private Dictionary<string, object> _parameters;

        public DbQuery(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DbQuery CommandText(string cmdText)
        {
            _commandText = cmdText;
            return this;
        }

        public DbQuery SetParameters(IEnumerable<KeyValuePair<string,object>> parameters)
        {
            if (parameters == null)
                return this;

            foreach (var kvp in parameters)
            {
                SetParameter(kvp.Key,kvp.Value);
            }

            return this;
        }

        public DbQuery SetParameter(string name,object value)
        {
            if (_parameters == null)
                _parameters = new Dictionary<string, object>();

            _parameters[name] = value;
            return this;
        }

        private T ExecuteHelper<T>(Func<IDbCommand,T> execute)
        {
            using (var connection = _connectionFactory())
            {
                connection.Open();

                if (Transaction.Current != null && connection is DbConnection dbConnection)
                    dbConnection.EnlistTransaction(Transaction.Current);

                var command = connection.CreateCommand();
                command.CommandText = _commandText;
                command.CommandType = _commandText.Contains(" ") ? CommandType.Text : CommandType.StoredProcedure;

                if (_parameters != null)
                {
                    foreach (var kvp in _parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = kvp.Key;
                        parameter.Value = kvp.Value ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }
                }

                using (command)
                {
                    return execute(command);
                }
            }
        }

        public List<IDataRecord> Execute()
        {
            return ExecuteHelper((cmd) =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.ToEnumerable().ToList();
                }
            });
        }

        public IDataRecord ExecuteSingleRow()
        {
            return ExecuteHelper((cmd) =>
            {
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return reader.ToEnumerable().FirstOrDefault();
                }
            });
        }

        public int ExecuteNonQuery()
        {
            return ExecuteHelper((cmd) => cmd.ExecuteNonQuery());
        }

        public T ExecuteScalar<T>()
        {
            return (T) ExecuteHelper((cmd) =>
            {
                var value = cmd.ExecuteScalar();
                if (value == DBNull.Value)
                    return default(T);

                return value ?? default(T);
            });
        }
    }
}