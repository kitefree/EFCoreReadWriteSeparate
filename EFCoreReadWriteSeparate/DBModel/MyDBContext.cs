using EFCoreReadWriteSeparate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EFCoreReadWriteSeparate.DBModel
{
    public class MyDBContext : DbContext
    {
        private DBConnectionOption _readAndWrite = null;

        private string conn = string.Empty;

        public MyDBContext(IOptionsMonitor<DBConnectionOption> options)
        {
            this._readAndWrite = options.CurrentValue;
        }

        private static int _iSeed = 0;
        public DbContext ToRead()
        {
            //隨機
            //int num = new Random(_iSeed++).Next(0, this._readAndWrite.ReadConnectionList.Count);

            //輪詢
            this.Database.GetDbConnection().ConnectionString =
                this._readAndWrite.ReadConnectionList[_iSeed++ %
                this._readAndWrite.ReadConnectionList.Count];

            return this;
        }

        public DbContext ToWrite()
        {
            this.Database.GetDbConnection().ConnectionString =
                this._readAndWrite.WriteConnection;

            return this;
        }

        public DbSet<SysUser> SysUser { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {               
            optionsBuilder.UseSqlServer(this._readAndWrite.WriteConnection);
         
            #region Print SQL語句
            optionsBuilder.UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            }));
            #endregion
            
        }       
    }
}
