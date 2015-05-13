using System.Collections.Concurrent;
using System.Data;

namespace MVC.Services.Db {
    public interface ISqlCommandFactory {
        ISqlCommand Create(IDbCommand comm);
    }

    public class OracleCommandFactory : ISqlCommandFactory {

        public ISqlCommand Create(IDbCommand comm) {
            return new OracleCommand(comm);
        }
    }
}