using EFCoreReadWriteSeparate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EFCoreReadWriteSeparate.DBModel
{
    public class MyDBContext : DbContext
    {
        private DBConnectionOption _masterSlaveOption;
        public MyDBContext(IOptionsMonitor<DBConnectionOption> options)
        {
            _masterSlaveOption = options.CurrentValue;
        }

        public DbContext Master()
        {
            //把链接字符串设为读写（主库）
            this.Database.GetDbConnection().ConnectionString = this._masterSlaveOption.MasterConnection;
            return this;
        }
        public DbContext Slave()
        {
            //把链接字符串设为之读（从库）
            this.Database.GetDbConnection().ConnectionString = this._masterSlaveOption.SlaveConnection;
            return this;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(this._masterSlaveOption.MasterConnection); //默认主库
        }
        public DbSet<SysUser> SysUser { get; set; }
    }
}

//針對這篇再練習，之後寫日記發佈、並上github
//https://blog.csdn.net/KingCruel/article/details/123451714

//https://www.cnblogs.com/wei325/p/16516014.html#autoid-2-2-0

//https://www.zendei.com/article/77238.html

//https://juejin.cn/post/7199131771668234296


//1、分表：时间分表、自定义分表、多表查询更新删除。

//2、分库：支持自定义分库、分库查询更新删除。

//3、分表分库：支持部分表格分表、部分表格分库。

//4、读写分离：支持一主多从的读写分离的方案。

//5、其他：支持动态分表、分库，支持高性能查询，支持事务等。



//https://blog.csdn.net/sD7O95O/article/details/94364149

