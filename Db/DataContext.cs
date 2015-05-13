using System.Data;
using Oracle.DataAccess.Client;
using System;

namespace MVC.Services.Db {

    /// <summary>
    /// интерфейс фабрики sql команд для
    /// </summary>
    public interface IDataContext {
        /// <summary>
        /// Создать sql комманду для подготовленного запроса
        /// </summary>
        /// <param name="query">Текс запроса</param>
        /// <returns>созданный Класс SQLCommand</returns>
        ISqlCommand CreateSqlCommand(string query);
        /// <summary>
        /// Создать sql комманду для хранимой процедуры
        /// </summary>
        /// <param name="command">Название хранимой процедуры</param>
        /// <returns>созданный Класс SQLCommand</returns>
        ISqlCommand CreateStoredProcedureCommand(string command);
        /// <summary>
        /// Создать sql комманду для хранимой процедуры
        /// </summary>
        /// <param name="command">Название хранимой процедуры</param>
        /// <param name="resultVarName">имя возвращаемого параметра</param>
        /// <param name="returnType">тип возвращаемого параметра, по умолчанию decimal</param>
        /// <returns>созданный Класс SQLCommand</returns>
        ISqlCommand CreateFunctionCommand(string command, Type returnType = null, string resultVarName = "result");
    }

    /// <summary>
    /// db контекст дял Oracle
    /// </summary>
    public class OracleDataContext : IDataContext {
        /// <summary>
        /// Connection к текущей базе
        /// </summary>
        private readonly OracleConnection _connection;

        private readonly ISqlCommandFactory _commandFactory;

        public OracleDataContext(string connectString, ISqlCommandFactory commandFactory) {
            _connection = new OracleConnection(connectString);
            _commandFactory = commandFactory;
        }

        public ISqlCommand CreateSqlCommand(string query) {
            return _commandFactory.Create(new Oracle.DataAccess.Client.OracleCommand(query, _connection)
            {
                CommandType = CommandType.Text
            });
        }

        public ISqlCommand CreateStoredProcedureCommand(string command)
        {
            return _commandFactory.Create(new Oracle.DataAccess.Client.OracleCommand(command, _connection)
            {
                CommandType = CommandType.StoredProcedure
            });
        }

        public ISqlCommand CreateFunctionCommand(string command, Type returnType = null, string resultVarName = "result")
        {
            var c = _commandFactory.Create(new Oracle.DataAccess.Client.OracleCommand(command, _connection)
            {
                CommandType = CommandType.StoredProcedure
            });
            c.AddParamReturn(resultVarName, (returnType ?? typeof(decimal)));
            return c;
        }
    }
}