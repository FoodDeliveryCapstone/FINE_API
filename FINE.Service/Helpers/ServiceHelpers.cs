using Azure;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    QuantitySuccess = quantity
                };
                var spaceInBox = new PutIntoBoxRequest()
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
                        Length = 0
                    }
                };

                if (card.Count() > 0)
                {
                    foreach (var item in card)
                    {
                        var limitTurn1 = item.Quantity;
                        var failedTurn1 = 0;
                        for (int turn = 1; turn <= limitTurn1; turn++)
                        {
                            var rsIncard = PutProductIntoBox(spaceInBox, item.Product, item.Quantity);

                            if (rsIncard.Success is true)
                            {
                                spaceInBox = new PutIntoBoxRequest
                                {
                                    RemainingSpaceBox = rsIncard.RemainingSpace,
                                    VolumeOccupied = rsIncard.VolumeOccupiedN,
                                    RemainingLengthSpace = rsIncard.RemainingLengthSpaceN,
                                    VolumeLengthOccupied = rsIncard.VolumeLengthOccupiedN,
                                    RemainingWidthSpace = rsIncard.RemainingWidthSpaceN,
                                    VolumeWidthOccupied = rsIncard.VolumeWidthOccupiedN,
                                };
                                break;
                            }
                            else
                            {
                                item.Quantity--;
                                failedTurn1++;
                            }
                        }

                        if (failedTurn1 > 0)
                        {
                            for (int turn2 = 1; turn2 <= failedTurn1; turn2++)
                            {
                                var rs2 = PutProductIntoBox(spaceInBox, product, turn2);

                                if ((bool)rs2.Success is false)
                                {
                                    spaceInBox = new PutIntoBoxRequest
                                    {
                                        RemainingSpaceBox = rs2.RemainingSpace,
                                        VolumeOccupied = rs2.VolumeOccupiedN,
                                        RemainingLengthSpace = rs2.RemainingLengthSpaceN,
                                        VolumeLengthOccupied = rs2.VolumeLengthOccupiedN,
                                        RemainingWidthSpace = rs2.RemainingWidthSpaceN,
                                        VolumeWidthOccupied = rs2.VolumeWidthOccupiedN,
                                    };
                                    break;
                                }
                            }
                        }
                    }
                }
                var limitTurn = quantity;
                var failedTurn = 0;
                for (int turn = 1; turn <= limitTurn; turn++)
                {
                    var rs = PutProductIntoBox(spaceInBox, product, quantity);

                    if (rs.Success is true)
                    {
                        spaceInBox = new PutIntoBoxRequest
                        {
                            RemainingSpaceBox = rs.RemainingSpace,
                            VolumeOccupied = rs.VolumeOccupiedN,
                            RemainingLengthSpace = rs.RemainingLengthSpaceN,
                            VolumeLengthOccupied = rs.VolumeLengthOccupiedN,
                            RemainingWidthSpace = rs.RemainingWidthSpaceN,
                            VolumeWidthOccupied = rs.VolumeWidthOccupiedN,
                        };
                        break;
                    }
                    else
                    {
                        quantity--;
                        failedTurn++;
                        fillBoxResponse.QuantitySuccess -= 1;
                    }
                }

                if (failedTurn > 0)
                {
                    for (int turn2 = 1; turn2 <= failedTurn; turn2++)
                    {
                        var rs2 = PutProductIntoBox(spaceInBox, product, turn2);

                        if ((bool)rs2.Success)
                        {
                            fillBoxResponse.QuantitySuccess += 1;
                        }
                        else
                        {
                            fillBoxResponse.RemainingWidthSpace = rs2.RemainingWidthSpaceN;
                            fillBoxResponse.RemainingLengthSpace = rs2.RemainingLengthSpaceN;
                            break;
                        }
                    }
                }
                fillBoxResponse.RemainingWidthSpace = spaceInBox.RemainingWidthSpace;
                fillBoxResponse.RemainingLengthSpace = spaceInBox.RemainingLengthSpace;

                return fillBoxResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static PutIntoBoxResponse PutProductIntoBox(PutIntoBoxRequest space, ProductAttribute product, int quantity)
        {
            try
            {
                var box = new CubeModel()
                {
                    Height = double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                    Width = double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                    Length = double.Parse(config.GetSection("BoxSize:Length").Value.ToString())
                };

                var response = new PutIntoBoxResponse()
                {
                    Success = true,
                    RemainingSpace = space.RemainingSpaceBox,
                    RemainingLengthSpaceN = new CubeModel(),
                    RemainingWidthSpaceN = new CubeModel(),
                    VolumeOccupiedN = new CubeModel()
                };

                var productSizeAfterEdit = new CubeModel()
                {
                    Height = product.Height,
                    Width = product.Width,
                    Length = product.Length
                };
                var volumeProductOccupied = new CubeModel();

                //xoay + ghep
                switch (product.RotationType)
                {
                    // nếu product có thể xoay => ưu tiên dựng product nằm dọc => volume occpied: h = p.h; w,l = p.w,l * quantity 
                    case (int)ProductRotationTypeEnum.Both:
                        if (product.Width < product.Height && product.Height < product.Length)
                        {
                            productSizeAfterEdit = new CubeModel
                            {
                                Height = product.Length,
                                Width = product.Height,
                                Length = product.Width
                            };
                        }
                        else if (product.Width > product.Height)
                        {
                            productSizeAfterEdit = new CubeModel
                            {
                                Height = product.Length,
                                Width = product.Width,
                                Length = product.Height
                            };
                        }
                        volumeProductOccupied = new CubeModel()
                        {
                            Height = productSizeAfterEdit.Height,
                            Width = productSizeAfterEdit.Width * quantity,
                            Length = productSizeAfterEdit.Length * quantity
                        };
                        break;
                    // nếu product mặc định nằm ngang => product nằm chồng lên nhau => volume occpied: h = q.h * quantity; w,l = p.w,l
                    case (int)ProductRotationTypeEnum.Horizontal:
                        volumeProductOccupied = new CubeModel()
                        {
                            Height = productSizeAfterEdit.Height * quantity,
                            Width = productSizeAfterEdit.Width,
                            Length = productSizeAfterEdit.Length
                        };
                        break;
                    case (int)ProductRotationTypeEnum.Vertical:
                        volumeProductOccupied = new CubeModel()
                        {
                            Height = productSizeAfterEdit.Height,
                            Width = productSizeAfterEdit.Width,
                            Length = productSizeAfterEdit.Length * quantity
                        };
                        break;
                }

                if (volumeProductOccupied.Height <= box.Height
                    && volumeProductOccupied.Length <= box.Length
                    && volumeProductOccupied.Width <= box.Width)
                {
                    //chiếm
                    // trường hợp 1: chưa có bất kì phần chia box nào
                    if (space.RemainingSpaceBox is not null)
                    {
                        //trường hợp 1: ghép với thể tích đã có mà vượt qua giới hạn box
                        if (volumeProductOccupied.Length + space.VolumeOccupied.Length > space.RemainingSpaceBox.Length
                            && volumeProductOccupied.Width + space.VolumeOccupied.Width > space.RemainingSpaceBox.Width)
                        {
                            response.Success = false;
                        }
                        //trường hợp 2: ghép với thể tích đã có bên chiều dài mà vượt qua giới hạn box,
                        //nhưng ghép bên chiều rộng vẫn có thể => cắt box làm 2 phần và bắt đầu ghép bên phần mới cắt
                        else if (volumeProductOccupied.Length + space.VolumeOccupied.Length > space.RemainingSpaceBox.Length
                            && volumeProductOccupied.Width + space.VolumeOccupied.Width <= space.RemainingSpaceBox.Width)
                        {
                            //chia box
                            response.RemainingWidthSpaceN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = space.RemainingSpaceBox.Width - space.VolumeOccupied.Width,
                                Length = space.RemainingSpaceBox.Length
                            };
                            response.VolumeWidthOccupiedN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = volumeProductOccupied.Width,
                                Length = volumeProductOccupied.Length
                            };
                            response.RemainingLengthSpaceN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = space.VolumeOccupied.Width,
                                Length = space.RemainingSpaceBox.Length
                            };
                            response.VolumeLengthOccupiedN = space.VolumeOccupied;

                            //disable thể tích box
                            response.VolumeOccupiedN = null;
                            response.RemainingSpace = null;
                        }
                        //Trường hợp 3: ghép tiếp vào thể tích đã có bên chiều dài
                        else if (volumeProductOccupied.Length + space.VolumeOccupied.Length <= space.RemainingSpaceBox.Length)
                        {
                            if (space.VolumeOccupied.Height == 0)
                            {
                                response.VolumeOccupiedN = new CubeModel()
                                {
                                    Height = box.Height,
                                    Width = volumeProductOccupied.Width,
                                    Length = volumeProductOccupied.Length
                                };
                            }
                            else
                            {
                                response.VolumeOccupiedN.Length = space.VolumeOccupied.Length + volumeProductOccupied.Length;
                            }

                            //vẫn cần phát sinh RemainingWidthSpace and RemainingWidthSpace để recommend
                            response.RemainingWidthSpaceN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = space.RemainingSpaceBox.Width - response.VolumeOccupiedN.Width,
                                Length = space.RemainingSpaceBox.Length
                            };
                            response.RemainingLengthSpaceN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = space.VolumeOccupied.Width,
                                Length = space.RemainingSpaceBox.Length
                            };
                        }
                    }
                    else
                    {
                        if (volumeProductOccupied.Length + space.VolumeWidthOccupied.Length > space.RemainingWidthSpace.Length)
                        {
                            response.Success = false;
                        }
                        else
                        {
                            response.VolumeWidthOccupiedN = new CubeModel()
                            {
                                Height = box.Height,
                                Width = space.VolumeWidthOccupied.Width,
                                Length = volumeProductOccupied.Length + space.VolumeWidthOccupied.Length
                            };
                        }
                    }
                }
                else
                {
                    response.Success = false;
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}