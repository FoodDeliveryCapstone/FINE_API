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

        public static FillBoxListResult FillTheBox(BoxModel boxRemainingSize, ProductAttribute product, int quantity)
        {
            try
            {
                var newBoxRemainingSize = new BoxModel();
                FillBoxListResult result = new FillBoxListResult()
                {
                    listCheck = new List<bool>()
                };
                for (int increaseUnit = 1; increaseUnit <= quantity; increaseUnit++)
                {
                    if (boxRemainingSize == null)
                    {
                        BoxModel box = new BoxModel()
                        {
                            Height = Double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                            Width = Double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                            Length = Double.Parse(config.GetSection("BoxSize:Length").Value.ToString())
                        };

                        newBoxRemainingSize = new BoxModel()
                        {
                            Height = box.Height - product.Height,
                            Width = box.Width - product.Width,
                            Length = box.Length - product.Length
                        };
                    }
                    else
                    {
                        if (boxRemainingSize.Height < product.Height)
                        {
                            var temp = product.Height;
                            product.Height = product.Width;
                            product.Width = temp;
                            if(product.Length < product.Width)
                            {
                                temp = product.Width;
                                product.Width = product.Length;
                                product.Length = temp;
                            }
                        }
                        newBoxRemainingSize = new BoxModel()
                        {
                            Height = boxRemainingSize.Height - product.Height,
                            Width = boxRemainingSize.Width - product.Width,
                            Length = boxRemainingSize.Length - product.Length
                        };
                    }
                    if (newBoxRemainingSize.Length < newBoxRemainingSize.Width)
                    {
                        newBoxRemainingSize.Temp = newBoxRemainingSize.Width;
                        newBoxRemainingSize.Width = newBoxRemainingSize.Length;
                        newBoxRemainingSize.Length = newBoxRemainingSize.Temp;
                    }
                    if (newBoxRemainingSize.Height < 0 || newBoxRemainingSize.Width < 0 || newBoxRemainingSize.Length < 0)
                        result.listCheck.Add(false);
                    else
                        result.listCheck.Add(true);

                    boxRemainingSize = newBoxRemainingSize;
                }
                result.Box = newBoxRemainingSize;
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
