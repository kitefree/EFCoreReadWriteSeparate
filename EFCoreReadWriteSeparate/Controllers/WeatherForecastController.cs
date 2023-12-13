using EFCoreReadWriteSeparate.DBModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EFCoreReadWriteSeparate.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private DbContext _dbContext;

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }


        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost(Name = "PostWeatherForecast")]
        public IEnumerable<SysUser> Post()
        {
            //新增-------------------
            SysUser user = new SysUser()
            {
                UserName = "李二狗",
                Account = "liergou",
                Password = Guid.NewGuid().ToString(),
                Phone = "13345435554",
                CreateTime = DateTime.Now
            };

            Console.WriteLine($"新增,目前連結字串為:{_dbContext.Database.GetDbConnection().ConnectionString}");
            _dbContext.Master().Add(user);
            _dbContext.SaveChanges();

            //只讀--------------------------------

            var users = _dbContext.Slave().Set<SysUser>().ToList();
            Console.WriteLine($"讀取SysUser,數量為:{users.Count},目前連結字串為:{_dbContext.Database.GetDbConnection().ConnectionString}");

            return users;
        }



    }
}