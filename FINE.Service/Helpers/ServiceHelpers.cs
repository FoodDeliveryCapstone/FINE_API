using FINE.Data.Entity;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Index.HPRtree;
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

        public static FillBoxResponse FillTheBox(ProductAttribute product, int quantity, List<FillBoxRequest> card)
        {
            try
            {
                FillBoxResponse fillBoxResponse = new FillBoxResponse()
                {
                    Success = new List<bool>()
                };

                var rsFillBoxWithCard = new PutIntoBoxModel()
                {
                    RemainingSpaceBox = new CubeModel()
                    {
                        Height = double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                        Width = double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                        Length = double.Parse(config.GetSection("BoxSize:Length").Value.ToString())
                    },
                    VolumeOccupied = new CubeModel()
                    {
                        Height = 0,
                        Width = 0,
                        Length = 0,
                    }
                };

                if (card.Count() > 0)
                {
                    foreach (var item in card)
                    {
                        rsFillBoxWithCard = PutProductIntoBox(rsFillBoxWithCard, item.Product, item.Quantity);
                    }
                }
                for (int turn = 1; turn <= quantity; turn++)
                {
                    rsFillBoxWithCard = PutProductIntoBox(rsFillBoxWithCard, product, 1);
                    fillBoxResponse.Success.Add(rsFillBoxWithCard.Success);
                }
                fillBoxResponse.RemainingSpaceBox = rsFillBoxWithCard.RemainingSpaceBox;
                fillBoxResponse.RemainingWidthSpace = rsFillBoxWithCard.RemainingWidthSpace;
                fillBoxResponse.RemainingLengthSpace = rsFillBoxWithCard.RemainingLengthSpace;

                return fillBoxResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static PutIntoBoxModel PutProductIntoBox(PutIntoBoxModel space, ProductAttribute product, int quantity)
        {
            try
            {
                PutIntoBoxModel response = new PutIntoBoxModel()
                {
                    Success = true
                };

                var box = new CubeModel()
                {
                    Height = double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                    Width = double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                    Length = double.Parse(config.GetSection("BoxSize:Length").Value.ToString())
                };

                var productSize = new CubeModel();
                var volumeProductOccupied = new CubeModel();

                //xoay + ghep
                switch (product.RotationType)
                {
                    case (int)ProductRotationTypeEnum.Both:
                        if (product.Width < product.Height && product.Height < product.Length)
                        {
                            productSize = new CubeModel
                            {
                                Height = product.Length,
                                Width = product.Height,
                                Length = product.Width
                            };
                        }
                        else if (product.Width > product.Height)
                        {
                            productSize = new CubeModel
                            {
                                Height = product.Length,
                                Width = product.Width,
                                Length = product.Height
                            };
                        }
                        volumeProductOccupied = new CubeModel()
                        {
                            Height = productSize.Height,
                            Width = productSize.Width * quantity,
                            Length = productSize.Length * quantity
                        };
                        break;

                    case (int)ProductRotationTypeEnum.Horizontal:
                        volumeProductOccupied = new CubeModel()
                        {
                            Height = productSize.Height * quantity,
                            Width = productSize.Width,
                            Length = productSize.Length
                        };
                        break;
                }

                //chiếm
                if (space.VolumeOccupied.Height < volumeProductOccupied.Height)
                    space.VolumeOccupied.Height = volumeProductOccupied.Height;

                if (volumeProductOccupied.Length + space.VolumeOccupied.Length > box.Length
                    && volumeProductOccupied.Width + space.VolumeOccupied.Width > box.Width)
                {
                    response.Success = false;
                }
                else if (volumeProductOccupied.Length + space.VolumeOccupied.Length > box.Length
                    && volumeProductOccupied.Width + space.VolumeOccupied.Width < box.Width)
                {
                    space.RemainingSpaceBox = null;
                    if (space.RemainingWidthSpace == null)
                    {
                        space.RemainingWidthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = box.Width - space.VolumeOccupied.Width,
                            Length = box.Length
                        };
                        space.VolumeWidthOccupied = new CubeModel()
                        {
                            Height = volumeProductOccupied.Height,
                            Width = volumeProductOccupied.Width,
                            Length = volumeProductOccupied.Length
                        };
                        space.RemainingLengthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = space.VolumeOccupied.Width,
                            Length = box.Length - space.VolumeOccupied.Length
                        };
                    }
                    else
                    {
                        if (space.VolumeWidthOccupied.Height < volumeProductOccupied.Height)
                            space.VolumeWidthOccupied.Height = volumeProductOccupied.Height;

                        if (volumeProductOccupied.Length + space.VolumeWidthOccupied.Length > space.RemainingWidthSpace.Length)
                        {
                            response.Success = false;
                        }
                        else
                        {
                            space.VolumeWidthOccupied = new CubeModel()
                            {
                                Height = space.VolumeWidthOccupied.Height,
                                Width = space.VolumeWidthOccupied.Width,
                                Length = volumeProductOccupied.Length + space.VolumeWidthOccupied.Length
                            };
                        }
                    }
                }
                else if (volumeProductOccupied.Length + space.VolumeOccupied.Length < box.Length)
                {
                    response.RemainingSpaceBox = new CubeModel()
                    {
                        Height = box.Height,
                        Width = box.Width - space.VolumeOccupied.Width,
                        Length = box.Length - space.VolumeOccupied.Length
                    };
                    space.VolumeOccupied = new CubeModel()
                    {
                        Height = space.VolumeOccupied.Height,
                        Width = space.VolumeOccupied.Width,
                        Length = volumeProductOccupied.Length + space.VolumeOccupied.Length,
                    };
                }
                response.VolumeOccupied = space.VolumeOccupied;
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}