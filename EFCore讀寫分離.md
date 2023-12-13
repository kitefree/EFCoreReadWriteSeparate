

## 讀寫分離示意圖

![image-20231213101632763](https://i.imgur.com/AswBDyN.png)

`發佈伺服器`的實作可以透過`SQLSERVER`的`複寫`功能，分別設定`發佈`與`訂閱`，資料庫複寫設定有興趣可以參考相關連結。以下針對程式碼如何實踐讀寫分離，做個紀錄筆記。



## 資料準備

### DB

分別建立`MyDB_Master`、`MyDB_Slave01`、`MyDB_Slave02`，共三個資料庫。

- `MyDB_Master`扮演`寫入`

- 、`MyDB_Slave01`、`MyDB_Slave02`扮演`讀取`。

完成結果如下：

![image-20231213102554874](https://i.imgur.com/cQzoEfS.png)

### SQL

1. 三個資料庫建立以下資料表

    ```SQL
    CREATE TABLE [dbo].[SysUser](
        [Id] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
        [UserName] [varchar](50) NOT NULL,
        [Account] [varchar](20) NOT NULL,
        [Password] [varchar](100) NOT NULL,
        [Phone] [varchar](50) NOT NULL,
        [CreateTime] [datetime] NOT NULL,
     CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    ```

2. 新增資料

    使用`編輯前200列資料`功能，再COPY PASTE以下資料即可。

    ```SQL
    #MyDB_Master
    1	主-狗狗01	gougou	eae8da4d-5cf2-4bbc-ab2b-217a0be96e59	13345435554	2023-12-13 09:08:22.030
    2	主-狗狗02	gougou	2d989e51-961a-440b-9528-90362ed79d0a	13345435554	2023-12-13 09:08:31.793
    #MyDB_Slave01
    1	從01-狗狗01	gougou	efae76c8-271a-4bbe-b0aa-512163d1ccaf	13345435554	2023-12-13 09:10:02.887
    #MyDB_Slave02
    1	從02-狗狗01	gougou	efae76c8-271a-4bbe-b0aa-512163d1ccaf	13345435554	2023-12-13 09:10:02.887
    ```

## 專案實作

### 專案套件

`EFCoreReadWriteSeparate.csproj`

```XML
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.25" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.7" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```

#### 設定 DB  Config 

`appsettings.json`

```C#
  "ConnectionStrings": {
    //寫
    "WriteConnection": "Server=KITE;Database=MyDB_Master;Trusted_Connection=True;MultipleActiveResultSets=true; TrustServerCertificate=true",
    //讀
    "ReadConnectionList": [
      "Server=KITE;Database=MyDB_Slave01;Trusted_Connection=True;MultipleActiveResultSets=true; TrustServerCertificate=true",
      "Server=KITE;Database=MyDB_Slave02;Trusted_Connection=True;MultipleActiveResultSets=true; TrustServerCertificate=true"
    ]
  }
```

#### 讀取 DB Config

`Program.cs`

```c#
builder.Services.Configure<DBConnectionOption>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddTransient<DbContext, MyDBContext>();
```

#### 擴充DbContext方法

`DBModel\DbContextExtend.cs`

```C#
using Microsoft.EntityFrameworkCore;

namespace EFCoreReadWriteSeparate.DBModel
{
    public static class DbContextExtend
    {
        public static DbContext ToRead(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
                return ((MyDBContext)dbContext).ToRead();
            else
                throw new Exception();
        }

        public static DbContext ToWrite(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
                return ((MyDBContext)dbContext).ToWrite();
            else
                throw new Exception();

        }
    }
}
```

#### 實作DbContext方法

`DBModel\MyDBContext.cs`

```C#
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
```

#### 建立Model Class

`DBModel\SysUser.cs`

```C#
namespace EFCoreReadWriteSeparate.DBModel
{
    public class SysUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
```

#### API測試

`WeatherForecastController.cs`

```C#
        [HttpPost(Name = "PostWeatherForecast")]
        public IEnumerable<SysUser> Post()
        {
            //新增-------------------
            SysUser user = new SysUser()
            {
                UserName = "主-狗狗02",
                Account = "gougou",
                Password = Guid.NewGuid().ToString(),
                Phone = "13345435554",
                CreateTime = DateTime.Now
            };

            Console.WriteLine($"新增,目前連結字串為:{_dbContext.Database.GetDbConnection().ConnectionString}");
            _dbContext.ToWrite().Add(user);
            _dbContext.SaveChanges();

            //只讀--------------------------------
            var users = _dbContext.ToRead().Set<SysUser>().ToList();
            Console.WriteLine($"讀取SysUser,數量為:{users.Count},目前連結字串為:{_dbContext.Database.GetDbConnection().ConnectionString}");

            return users;
        }
```

#### 測試結果

第一次執行，讀取時，取得Slave

![image-20231213131525781](https://i.imgur.com/zTNzoxP.png)

觀查command line

![image-20231213131825336](https://i.imgur.com/RKOC4AG.png)

第二次執行

![image-20231213131939848](https://i.imgur.com/K3pds2U.png)

觀查command line

![image-20231213132046360](https://i.imgur.com/Nj0D80Q.png)

## 參考資料

- [SQL Server、MySQL主从搭建，EF Core读写分离代码实现](https://www.cnblogs.com/wei325/p/16516014.html)
- [EFCore 读写分离](https://blog.csdn.net/KingCruel/article/details/123451714)
  - 這篇實作輪詢作法
- [EF core 實現讀寫分離解決方案](https://www.zendei.com/article/77238.html)
- [一款针对EF Core轻量级分表分库、读写分离的开源项目](https://juejin.cn/post/7199131771668234296)
  - 這篇很值得之後研究，其中有實踐分表、分庫、讀寫分離等作法

