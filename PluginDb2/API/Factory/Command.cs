using System.Threading.Tasks;
using IBM.Data.DB2.Core;

namespace PluginDb2.API.Factory
{
    public class Command : ICommand
    {
        private readonly DB2Command _cmd;

        public Command()
        {
            _cmd = new DB2Command() {CommandTimeout = 0};
        }

        public Command(string commandText)
        {
            _cmd = new DB2Command(commandText) {CommandTimeout = 0};
        }

        public Command(string commandText, IConnection conn)
        {
            _cmd = new DB2Command(commandText, (DB2Connection) conn.GetConnection()) {CommandTimeout = 0};
        }

        public void SetConnection(IConnection conn)
        {
            _cmd.Connection = (DB2Connection) conn.GetConnection();
        }

        public void SetCommandText(string commandText)
        {
            _cmd.CommandText = commandText;
        }

        public void AddParameter(string name, object value)
        {
            _cmd.Parameters.Add(name, value);
        }

        public async Task<IReader> ExecuteReaderAsync()
        {
            return new Reader(await _cmd.ExecuteReaderAsync());
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            return await _cmd.ExecuteNonQueryAsync();
        }
    }
}