using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Oracle.DataAccess.Client;

namespace MVC.Services.Db {

    public interface ISqlCommand {
        ISqlCommand AddParamIn(string name, string value);
        ISqlCommand AddParamIn(string name, int value);
        ISqlCommand AddParamIn(string name, char value);
        ISqlCommand AddParamIn(string name, long value);
        ISqlCommand AddParamIn(string name, double value);
        ISqlCommand AddParamIn(string name, DateTime value);
        ISqlCommand AddParamOutCursor(string name);
        ISqlCommand AddParamOut(string name, Type type, int size = 0);
        ISqlCommand AddParamReturn(string name, Type type, int size = 0);
        object GetParam(string name);
        object Execute();
        bool SimpleExecute();
        TModel Get<TModel>(Func<IDataReader, TModel> adapter);
        IEnumerable<TModel> GetList<TModel>(Func<IDataReader, TModel> adapter);
    }

    /// <summary>
    /// Summary description for SqlCommand
    /// </summary>
    public class OracleCommand : ISqlCommand {
        private readonly IDbCommand _dbCommand;

        public OracleCommand(IDbCommand dbCommand) {
            _dbCommand = dbCommand;
        }

        public ISqlCommand AddParamIn(string name, string value) {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Varchar2, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamIn(string name, int value) {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Int32, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamIn(string name, char value) {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Char, 1, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamIn(string name, long value)
        {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Long, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamIn(string name, double value)
        {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Double, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamIn(string name, DateTime value)
        {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.Date, value, ParameterDirection.Input));
            return this;
        }

        public ISqlCommand AddParamOutCursor(string name) {
            _dbCommand.Parameters.Add(new OracleParameter(name, OracleDbType.RefCursor, ParameterDirection.ReturnValue));
            return this;
        }

        public ISqlCommand AddParamOut(string name, Type type, int size = 0) {
            _dbCommand.Parameters.Add(size > 0
                                          ? new OracleParameter(name, GetDbType(type), size, null, ParameterDirection.Output)
                                          : new OracleParameter(name, GetDbType(type), ParameterDirection.Output));
            return this;
        }

        public ISqlCommand AddParamReturn(string name, Type type, int size = 0) {
            _dbCommand.Parameters.Add(size > 0
                                          ? new OracleParameter(name, GetDbType(type), size, null, ParameterDirection.ReturnValue)
                                          : new OracleParameter(name, GetDbType(type), ParameterDirection.ReturnValue));
            return this;
        }

        public object GetParam(string name) {
            return _dbCommand.Parameters[name] == null ? null : ((IDataParameter)_dbCommand.Parameters[name]).Value;
        }

        public object Execute() {
            try {
                OnExecuteCommand();
                _dbCommand.Connection.Open();
                return _dbCommand.ExecuteScalar();
            }
            catch (Exception e) {
                OnError(e);
            }
            finally {
                _dbCommand.Connection.Close();
            }
            return null;
        }

        public bool SimpleExecute() {
            try {
                OnExecuteCommand();
                _dbCommand.Connection.Open();
                _dbCommand.ExecuteNonQuery();
                OnReturnParameters();
            }
            catch (Exception e) {
                OnError(e);
                return false;
            }
            finally {
                _dbCommand.Connection.Close();
            }
            return true;
        }

        public TModel Get<TModel>(Func<IDataReader, TModel> adapter) {
            var list = GetList(adapter);
            return list == null
                       ? default(TModel)
                       : list.FirstOrDefault();
        }

        public IEnumerable<TModel> GetList<TModel>(Func<IDataReader, TModel> adapter) {
            var list = new List<TModel>();
            try {
                OnExecuteCommand();
                _dbCommand.Connection.Open();
                var reader = _dbCommand.ExecuteReader();
                if (reader != null)
                    while (reader.Read())
                        list.Add(adapter(reader));
                OnResultCountLines(list.Count);
            }
            catch (Exception e) {
                OnError(e);
            }
            finally {
                _dbCommand.Connection.Close();
            }
            return list;
        }

        private static OracleDbType GetDbType(Type type)
        {
            if (type == typeof(int)) return OracleDbType.Int32;
            if (type == typeof(string)) return OracleDbType.Varchar2;
            if (type == typeof(decimal)) return OracleDbType.Decimal;
            if (type == typeof(double)) return OracleDbType.Double;
            if (type == typeof(int)) return OracleDbType.Int32;
            throw new Exception(string.Format("Не удалось сопоставить тип {0} с типом БД", type));
        }

        private void OnExecuteCommand() {
            var par = string.Join(", ", _dbCommand
                                      .Parameters
                                      .Cast<IDataParameter>()
                                      .Where(x => x.Value != null)
                                      .Select(x => string.Format("{0}={1}",x.ParameterName, x.Value)));
            LogFile.Log.InfoFormat("Вызов {0} '{1}' с параметрами [{2}]",
                                   _dbCommand.CommandType,
                                   _dbCommand.CommandText,
                                   par);
        }

        private void OnReturnParameters() {
            const byte maxLen = 50;
            var par = string.Join(", ", _dbCommand
                                            .Parameters
                                            .Cast<IDataParameter>()
                                            .Where(x =>
                                                x.Value != null &&
                                                (x.Direction == ParameterDirection.Output ||
                                                x.Direction == ParameterDirection.ReturnValue))
                                            .Select(x => {
                                                var str = x.Value.ToString();
                                                return string.Format("{0}={1}", x.ParameterName,
                                                              str.Substring(0, maxLen < str.Length ? maxLen : str.Length));
                                            }));

                                                
            LogFile.Log.InfoFormat("Получены значения [{0}]", par);
        }

        private static void OnResultCountLines(int i) {
            LogFile.Log.InfoFormat("Получено [{0}] строк", i);
        }

        private static void OnError(Exception e) {
            LogFile.Log.ErrorFormat("Исключение при обращении к базе [{0}]", e.Message);
        }
    }
}