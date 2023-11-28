using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Service
{
    public interface ILogService
    {
        void CatchLog(AppCatchLog appType, string message);
    }
    public class LogService : ILogService
    {
        private readonly IConfiguration _configuration;
        public LogService(IConfiguration configuration) 
        {
            _configuration = configuration;
        }

        public void CatchLog(AppCatchLog appType, string message)
        {
            RedisValue rs = new RedisValue();
            string connectRedisString = _configuration["Endpoint:RedisEndpoint"] + "," + _configuration["Endpoint:Password"];
            // Tạo kết nối
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectRedisString);

            // Lấy DB
            IDatabase db = redis.GetDatabase(2);

            // Ping thử
            if (db.Ping().TotalSeconds > 5)
            {
                throw new TimeoutException("Server Redis không hoạt động");
            }

            var key = appType.GetDisplayName() +":"+  DateTime.Now.ToString("dd/mm-hh.mm.ss");

            var value = message;
            db.StringSet(key, value);
        }
    }
}
