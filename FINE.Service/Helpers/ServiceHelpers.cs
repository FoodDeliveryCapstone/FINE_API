using FINE.Data.Entity;
using FINE.Service.DTO.Response;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Helpers
{
    public class ServiceHelpers
    {
        public static IConfiguration config;
        public static void Initialize(IConfiguration Configuration)
        {
            config = Configuration;
        }

        public async static Task<dynamic> GetSetDataRedis(RedisSetUpType type, string key, object value = null)
        {
            try
            {
                CoOrderResponse rs = new CoOrderResponse();
                string connectRedisString = config.GetSection("Endpoint:RedisEndpoint").Value + "," + config.GetSection("Endpoint:Password").Value;
                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectRedisString);

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }
                switch (type)
                {
                    case RedisSetUpType.GET:
                        var redisValue = db.StringGet(key);
                        rs = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);
                        break;

                    case RedisSetUpType.SET:
                        var redisNewValue = JsonConvert.SerializeObject(value);
                        db.StringSet(key, redisNewValue);
                        break;

                    case RedisSetUpType.DELETE:
                        db.KeyDelete(key);
                        break;
                }

                redis.Close();
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static FillBoxResult FillTheBox(double volumeSpace, ProductAttribute product)
        {
            try
            {       
                FillBoxResult result = new FillBoxResult()
                {
                    Success = true,
                    VolumeRemainingSpace = volumeSpace,
                };
                if (volumeSpace == null)
                {
                    var box = new 
                    {
                        Height = double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                        Width = double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                        Length = double.Parse(config.GetSection("BoxSize:Length").Value.ToString())
                    };

                    var volumeBox = (box.Height * box.Width * box.Length) - 1000;

                    volumeSpace = volumeBox - (product.Height * product.Length * product.Width);                   
                }
                else
                {
                    volumeSpace = volumeSpace - (product.Height * product.Length * product.Width);
                }
                if (volumeSpace < 0)
                {
                    result.Success = false;
                }             
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
