using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Core.Modules.Utility.Services
{
    public class SqlCommandsService : INService
    {
        
        private readonly DbReadOnlyService _dbro;

        public SqlCommandsService(DbReadOnlyService dbro)
        {
            _dbro = dbro;
        }


        public class SelectResult
        {
            public List<string> ColumnNames { get; set; }
            public List<string[]> Results { get; set; }
        }

        public SelectResult SelectSql(string sql)
        {
            var result = new SelectResult()
            {
                ColumnNames = new List<string>(),
                Results = new List<string[]>(),
            };

            using (var uow = _dbro.GetDbContext())
            {
                var conn = uow._context.Database.GetDbConnection();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                result.ColumnNames.Add(reader.GetName(i));
                            }
                            while (reader.Read())
                            {
                                var obj = new object[reader.FieldCount];
                                reader.GetValues(obj);
                                result.Results.Add(obj.Select(x => x.ToString()).ToArray());
                            }
                        }
                    }
                }
            }
            return result;
        }

        
    }
}
