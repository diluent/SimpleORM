using System;
using System.Collections.Generic;
using System.Data;
using MVC.Services.Db;

namespace MVC.Repository.User {

    /// <summary>
    /// Данные пользователя
    /// </summary>
    public class UserModel {
        public int Id;
        public string Login;
        public string Name;
        public string SureName;
        public string PatronomicName;
        public string Phone;
    }

    /// <summary>
    /// Репозиторий пользователя
    /// </summary>
    public interface IUserRepository {
        UserModel Get(string login);
        List<UserModel> Select();
        bool Insert(UserModel data);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IDataContext _dataContext;

        public UserRepository(IDataContext dataContext) {
            _dataContext = dataContext;
        }

        private readonly Func<IDataReader, IdentData> _adapterUserData = reader => new UserModel {
            Id = reader["id"].ToString(),
            Login = reader["Login"].ToString(),
            Name = reader["Name"].ToString(),
            SureName = reader["SureName"].ToString(),
            PatronomicName = reader["PatronomicName"].ToString(),
            Phone = reader["Phone"].ToString(),
        }

        public UserModel Get(string login) {
            const string query = "Package.pUserGet";
            var cmd = _dataContext.CreateStoredProcedureCommand(query)
                                  .AddParamIn("p_user_login", login);

            return cmd.Get(_adapterUserData);
        }

        List<UserModel> Select() {
            const string query = "Package.pUserSelect";
            var cmd = _dataContext.CreateStoredProcedureCommand(query);
            return cmd.GetList(_adapterUserData).ToList();
        }

        bool Insert(UserModel data) {
            const string query = "Package.pUserInsert";
            var cmd = _dataContext.CreateStoredProcedureCommand(query)
                                 .AddParamIn("p_login", data.Login)
                                 .AddParamIn("p_name", data.Name)
                                 .AddParamIn("p_surename", data.SureName)
                                 .AddParamIn("p_patronomicname", data.PatronomicName)
                                 .AddParamIn("p_phone", data.Phone);
            return cmd.SimpleExecute();
        }
    }
}
