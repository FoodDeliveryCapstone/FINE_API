using AutoMapper;
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
using System.Security.Cryptography;
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

        //note: key for coOrder is party code
        //      key for staff is storeId.Value + "-" + order.TimeSlot.ArriveTime; (xem thêm ở SplitOrder line 1509)
        //      keyShipper is station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
        public async static Task<RedisValue> GetSetDataRedis(RedisSetUpType type, string key, object value)
        {
            try
            {
                RedisValue rs = new RedisValue();
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
                        rs = db.StringGet(key);
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

        public async static Task<dynamic> GetSetDataIntoKafka(RedisSetUpType type, string key, object value = null)
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

        public static FixBoxResponse CheckProductFixTheBox(List<CheckFixBoxRequest> card, ProductAttribute? product = null, int? quantity = 0)
        {
            try
            {
                FixBoxResponse fillBoxResponse = new FixBoxResponse()
                {
                    QuantitySuccess = 0
                };

                var boxSize = new CubeModel()
                {
                    Height = double.Parse(config.GetSection("BoxSize:Height").Value.ToString()),
                    Width = double.Parse(config.GetSection("BoxSize:Width").Value.ToString()),
                    Length = double.Parse(config.GetSection("BoxSize:Depth").Value.ToString())
                };

                var spaceInBox = new SpaceInBoxMode()
                {
                    RemainingSpaceBox = boxSize,
                    RemainingLengthSpace = boxSize,
                    RemainingWidthSpace = boxSize,
                    VolumeOccupied = null
                };

                // tính thể tích bị chiếm bởi các product trong card trước
                card.OrderByDescending(x => (x.Product.Length * x.Product.Width));
                foreach (var item in card)
                {
                    // ghép product lên cho vừa họp dựa trên quantity yêu cầu
                    var pairingCardResult = ProductPairing(item.Quantity, item.Product, boxSize);
                    var turnCard = Math.Ceiling((decimal)((double)item.Quantity / pairingCardResult.QuantityCanAdd));

                    for (var successCase = 1; successCase <= turnCard; successCase++)
                    {
                        // đem đi bỏ vào tủ 
                        var calculateCardResult = CalculateRemainingSpace(boxSize, spaceInBox, pairingCardResult.ProductOccupied); 

                        if (calculateCardResult.Success == true)
                        {
                            spaceInBox = new SpaceInBoxMode
                            {
                                RemainingSpaceBox = calculateCardResult.RemainingSpaceBox,
                                VolumeOccupied = calculateCardResult.VolumeOccupied,
                                RemainingLengthSpace = calculateCardResult.RemainingLengthSpace,
                                RemainingWidthSpace = calculateCardResult.RemainingWidthSpace,
                                VolumeLengthOccupied = calculateCardResult.VolumeLengthOccupied,
                                VolumeWidthOccupied = calculateCardResult.VolumeWidthOccupied
                            };
                            if (product is not null)
                            {
                                if (item.Product.Id == product.Id)
                                {
                                    if (successCase == turnCard)
                                    {
                                        fillBoxResponse.QuantitySuccess = (int)quantity;
                                    }
                                    else
                                    {
                                        fillBoxResponse.QuantitySuccess = pairingCardResult.QuantityCanAdd * successCase;
                                    }
                                }
                            }
                        }
                        else
                        {
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

        public static ProductParingResponse ProductPairing(int quantity, ProductAttribute product, CubeModel boxSize)
        {
            try
            {
                ProductParingResponse paringResponse = new ProductParingResponse()
                {
                    ProductOccupied = null,
                    QuantityCanAdd = 1
                };

                CubeModel productSizeAfterEdit = new CubeModel()
                {
                    Height = product.Height,
                    Width = product.Width,
                    Length = product.Length
                };

                //xoay + ghep
                switch (product.RotationType)
                {
                    // nếu product có thể xoay => ưu tiên dựng product nằm dọc => số lượng có thể add * product.w < box.w
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
                        paringResponse.ProductOccupied = new CubeModel()
                        {
                            Height = productSizeAfterEdit.Height,
                            Width = productSizeAfterEdit.Width,
                            Length = productSizeAfterEdit.Length
                        };
                        break;

                    // nếu product mặc định nằm ngang => product nằm chồng lên nhau => số lượng có thể add * product.h < product.h (tùy thuộc có thể chồng hay ko)
                    case (int)ProductRotationTypeEnum.Horizontal:
                        if (product.Product.IsStackable is true)
                        {
                            paringResponse.ProductOccupied = new CubeModel()
                            {
                                Height = productSizeAfterEdit.Height * quantity,
                                Width = productSizeAfterEdit.Width,
                                Length = productSizeAfterEdit.Length
                            };

                            if (paringResponse.ProductOccupied.Height > boxSize.Height)
                            {
                                paringResponse.QuantityCanAdd = (int)Math.Floor(boxSize.Height / productSizeAfterEdit.Height);
                                paringResponse.ProductOccupied = new CubeModel()
                                {
                                    Height = productSizeAfterEdit.Height * paringResponse.QuantityCanAdd,
                                    Width = productSizeAfterEdit.Width,
                                    Length = productSizeAfterEdit.Length
                                };
                            }
                        }
                        else
                        {
                            paringResponse.ProductOccupied = new CubeModel()
                            {
                                Height = productSizeAfterEdit.Height,
                                Width = productSizeAfterEdit.Width,
                                Length = productSizeAfterEdit.Length
                            };
                        }
                        break;

                    // nếu product mặc định nằm đứng => số lượng có thể add * product.w < box.w
                    case (int)ProductRotationTypeEnum.Vertical:
                        paringResponse.ProductOccupied = new CubeModel()
                        {
                            Height = productSizeAfterEdit.Height,
                            Width = productSizeAfterEdit.Width,
                            Length = productSizeAfterEdit.Length
                        };
                        break;
                }

                var temp = paringResponse.ProductOccupied.Length;
                paringResponse.ProductOccupied.Length = paringResponse.ProductOccupied.Length > paringResponse.ProductOccupied.Width ? paringResponse.ProductOccupied.Length : paringResponse.ProductOccupied.Width;
                paringResponse.ProductOccupied.Width = paringResponse.ProductOccupied.Length > paringResponse.ProductOccupied.Width ? paringResponse.ProductOccupied.Width : temp;

                return paringResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //qui định: giả định đặt sp vào vị trí phải trên cùng của hộp
        //luồn : ưu tiên RemainingWidthSpace
        //RemainingLengthSpace : khoảng trống bên trái sp (cắt dọc xuống)
        //RemainingWidthSpace : khoảng trống bên dưới sp (cắt ngang qua)
        public static SpaceInBoxMode CalculateRemainingSpace(CubeModel box, SpaceInBoxMode space, CubeModel productOccupied)
        {
            try
            {
                var response = new SpaceInBoxMode()
                {
                    Success = true,
                    RemainingSpaceBox = space.RemainingSpaceBox,
                    RemainingLengthSpace = space.RemainingLengthSpace,
                    RemainingWidthSpace = space.RemainingWidthSpace,
                    VolumeOccupied = space.VolumeOccupied
                };

                //chiếm
                // trường hợp 1: product đầu tiên được bỏ vào box
                if (space.VolumeOccupied is null)
                {
                    response.VolumeOccupied = new CubeModel()
                    {
                        Height = productOccupied.Height,
                        Width = productOccupied.Width,
                        Length = productOccupied.Length
                    };
                    //vẫn cần phát sinh RemainingWidthSpace and RemainingWidthSpace để recommend
                    response.RemainingLengthSpace = new CubeModel()
                    {
                        Height = box.Height,
                        Width = box.Width - response.VolumeOccupied.Length,
                        Length = box.Length
                    };
                    response.RemainingWidthSpace = new CubeModel()
                    {
                        Height = box.Height,
                        Width = box.Width,
                        Length = box.Length - response.VolumeOccupied.Width
                    };
                }
                // trường hợp 2: product thứ n trở đi và chưa có bất kì phần chia box nào
                else if (space.RemainingSpaceBox is not null)
                {
                    //Trường hợp 2.1: ghép tiếp vào thể tích đã có bên dưới sp mà vẫn ok
                    if (space.VolumeOccupied.Width + productOccupied.Width <= box.Length)
                    {
                        response.VolumeOccupied = new CubeModel()
                        {
                            Height = space.VolumeOccupied.Height < productOccupied.Height ? productOccupied.Height : space.VolumeOccupied.Height,
                            Width = space.VolumeOccupied.Width + productOccupied.Width,
                            Length = space.VolumeOccupied.Length < productOccupied.Length ? productOccupied.Length : space.VolumeOccupied.Length
                        };

                        //vẫn cần phát sinh RemainingWidthSpace and RemainingWidthSpace để recommend
                        response.RemainingLengthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = box.Width - response.VolumeOccupied.Length,
                            Length = box.Length
                        };
                        response.RemainingWidthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = box.Width,
                            Length = box.Length - response.VolumeOccupied.Width
                        };
                    }
                    //Trường hợp 2.2: ghép với thể tích đã có bên dưới sp mà vượt qua giới hạn chều sâu box,
                    //but ghép bên chiều rộng vẫn có thể => cắt box làm 2 phần và bắt đầu ghép bên phần mới cắt
                    else if (space.VolumeOccupied.Width + productOccupied.Width > box.Length
                        && space.VolumeOccupied.Length + productOccupied.Length <= box.Width)
                    {
                        //chia box làm 2 phần khoản trống
                        response.RemainingLengthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = box.Width - space.VolumeOccupied.Length,
                            Length = box.Length
                        };
                        response.RemainingWidthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = box.Width,
                            Length = box.Length - space.VolumeOccupied.Width
                        };
                        //ưu tiên RemainingWidthSpace
                        if (productOccupied.Width < response.RemainingWidthSpace.Length)
                        {
                            response.VolumeWidthOccupied = new CubeModel()
                            {
                                Height = productOccupied.Height,
                                Width = productOccupied.Width,
                                Length = productOccupied.Length
                            };

                            response.RemainingWidthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = response.RemainingWidthSpace.Width,
                                Length = response.RemainingWidthSpace.Length - response.VolumeWidthOccupied.Width
                            };
                            response.VolumeLengthOccupied = new CubeModel();
                        }
                        else if (productOccupied.Width < response.RemainingLengthSpace.Width)
                        {
                            response.VolumeLengthOccupied = new CubeModel()
                            {
                                Height = productOccupied.Height,
                                Width = productOccupied.Width,
                                Length = productOccupied.Length
                            };

                            response.RemainingLengthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = response.RemainingLengthSpace.Width - response.VolumeLengthOccupied.Width,
                                Length = response.RemainingLengthSpace.Length
                            };
                            response.VolumeWidthOccupied = new CubeModel();
                        }
                        else
                        {
                            response.Success = false;
                        }

                        //disable thể tích box
                        response.RemainingSpaceBox = null;
                    }
                    else
                    {
                        response.Success = false;
                    }
                }// trường hợp 3: product thứ n trở đi và đã chia box làm 2 phần
                else if (space.RemainingSpaceBox is null)
                {
                    var recoveryRemainingLengthSpace = new CubeModel();
                    var recoveryRemainingWidthSpace = new CubeModel();
                    //khôi phục lại remainingWidth và length space
                    //ưu tiên đặt phần remainingWidth trước  
                    if (space.VolumeWidthOccupied is not null && space.VolumeWidthOccupied.Length != 0)
                    {
                        recoveryRemainingWidthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = space.RemainingWidthSpace.Width,
                            Length = space.RemainingWidthSpace.Length + space.VolumeWidthOccupied.Width
                        };                  
                        if (space.VolumeWidthOccupied.Length + productOccupied.Length <= response.RemainingWidthSpace.Width)
                        {
                            response.VolumeWidthOccupied = new CubeModel()
                            {
                                Height = space.VolumeWidthOccupied.Height < productOccupied.Height ? productOccupied.Height : space.VolumeWidthOccupied.Height,
                                Width = space.VolumeWidthOccupied.Width < productOccupied.Width ? productOccupied.Width : space.VolumeWidthOccupied.Width,
                                Length = space.VolumeWidthOccupied.Length + productOccupied.Length
                            };
                            response.RemainingWidthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = recoveryRemainingWidthSpace.Width,
                                Length = recoveryRemainingWidthSpace.Length - response.VolumeWidthOccupied.Width
                            };
                        }
                        else if (space.VolumeWidthOccupied.Width + productOccupied.Width <= response.RemainingWidthSpace.Length)
                        {
                            response.VolumeWidthOccupied = new CubeModel()
                            {
                                Height = space.VolumeWidthOccupied.Height < productOccupied.Height ? productOccupied.Height : space.VolumeWidthOccupied.Height,
                                Width = space.VolumeWidthOccupied.Width + productOccupied.Width,
                                Length = space.VolumeWidthOccupied.Length < productOccupied.Length ? productOccupied.Length : space.VolumeWidthOccupied.Length
                            };
                            response.RemainingWidthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = recoveryRemainingWidthSpace.Width,
                                Length = recoveryRemainingWidthSpace.Length - response.VolumeWidthOccupied.Width
                            };
                        }
                    }
                    if (space.VolumeLengthOccupied is not null && space.VolumeLengthOccupied.Length != 0)
                    {
                        recoveryRemainingLengthSpace = new CubeModel()
                        {
                            Height = box.Height,
                            Width = space.RemainingLengthSpace.Width + space.VolumeLengthOccupied.Length,
                            Length = space.RemainingLengthSpace.Length
                        };
                        if (space.VolumeLengthOccupied is not null && (space.VolumeLengthOccupied.Width + productOccupied.Width <= response.RemainingLengthSpace.Width))
                        {
                            response.VolumeLengthOccupied = new CubeModel()
                            {
                                Height = space.VolumeLengthOccupied.Height < productOccupied.Height ? productOccupied.Height : space.VolumeLengthOccupied.Height,
                                Width = space.VolumeLengthOccupied.Width + productOccupied.Width,
                                Length = space.VolumeLengthOccupied.Length < productOccupied.Length ? productOccupied.Length : space.VolumeLengthOccupied.Length
                            };
                            response.RemainingLengthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = recoveryRemainingLengthSpace.Width,
                                Length = recoveryRemainingLengthSpace.Length - response.VolumeLengthOccupied.Length
                            };
                        }
                        else if (space.VolumeLengthOccupied is not null && (space.VolumeLengthOccupied.Length + productOccupied.Length <= response.RemainingLengthSpace.Length))
                        {
                            response.VolumeLengthOccupied = new CubeModel()
                            {
                                Height = space.VolumeLengthOccupied.Height < productOccupied.Height ? productOccupied.Height : space.VolumeLengthOccupied.Height,
                                Width = space.VolumeLengthOccupied.Width < productOccupied.Width ? productOccupied.Width : space.VolumeLengthOccupied.Width,
                                Length = space.VolumeLengthOccupied.Length + productOccupied.Length
                            };
                            recoveryRemainingLengthSpace = new CubeModel()
                            {
                                Height = box.Height,
                                Width = recoveryRemainingLengthSpace.Width,
                                Length = recoveryRemainingLengthSpace.Length - response.VolumeLengthOccupied.Length
                            };
                        }
                        else
                        {
                            response.Success = false;
                        }
                    }
                    else
                    {
                        response.Success = false;
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

        public async static Task<dynamic> GetSetDataRedisOrder(RedisSetUpType type, string key = null, object value = null)
        {
            try
            {
                List<OrderByStoreResponse> rs = new List<OrderByStoreResponse>();
                string connectRedisString = config.GetSection("Endpoint:RedisEndpoint").Value + "," + config.GetSection("Endpoint:Password").Value;
                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectRedisString);

                // Lấy DB
                IDatabase db = redis.GetDatabase(2);
                var allKeys = redis.GetServer("localhost", 6379).Keys(database: 2);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }
                switch (type)
                {
                    case RedisSetUpType.GET:
                        if (key == null)
                        {
                            foreach (var eachKey in allKeys)
                            {
                                RedisValue redisValue = db.StringGet(eachKey);
                                if (redisValue.HasValue)
                                {

                                    List<OrderByStoreResponse> orderResponses = JsonConvert.DeserializeObject<List<OrderByStoreResponse>>(redisValue);
                                    rs.AddRange(orderResponses);
                                }
                            }
                        }
                        else
                        {
                            RedisValue redisValue = db.StringGet(key);
                            if (redisValue.HasValue)
                            {

                                List<OrderByStoreResponse> orderResponses = JsonConvert.DeserializeObject<List<OrderByStoreResponse>>(redisValue);
                                rs.AddRange(orderResponses);
                            }
                        }
                        //rs = JsonConvert.DeserializeObject<OrderByStoreResponse>(redisValue);
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
        public async static Task<dynamic> GetSetDataRedisReportMissingProduct(RedisSetUpType type, string key = null, object value = null)
        {
            try
            {
                ReportMissingProductResponse rs = new ReportMissingProductResponse();
                List<ReportMissingProductResponse> rsList = new List<ReportMissingProductResponse>();
                string connectRedisString = config.GetSection("Endpoint:RedisEndpoint").Value + "," + config.GetSection("Endpoint:Password").Value;
                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectRedisString);

                // Lấy DB
                IDatabase db = redis.GetDatabase(3);
                var allKeys = redis.GetServer("localhost", 6379).Keys(database: 3);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }
                switch (type)
                {
                    case RedisSetUpType.GET:
                        if (key == null)
                        {
                            foreach (var eachKey in allKeys)
                            {
                                RedisValue redisValue = db.StringGet(eachKey);
                                if (redisValue.HasValue)
                                {
                                    //List<ReportMissingProductResponse> reportResponse = JsonConvert.DeserializeObject<List<ReportMissingProductResponse>>(redisValue);
                                    //rs.AddRange(reportResponse);
                                    ReportMissingProductResponse reportResponse = JsonConvert.DeserializeObject<ReportMissingProductResponse>(redisValue);
                                    rsList.Add(reportResponse);
                                }
                            }
                        }
                        else
                        {
                            RedisValue redisValue = db.StringGet(key);
                            if (redisValue.HasValue)
                            {

                                //List<ReportMissingProductResponse> reportResponse = JsonConvert.DeserializeObject<List<ReportMissingProductResponse>>(redisValue);
                                //rs.AddRange(reportResponse);
                                ReportMissingProductResponse reportResponse = JsonConvert.DeserializeObject<ReportMissingProductResponse>(redisValue);
                                rsList.Add(reportResponse);
                            }
                        }
                        //rs = JsonConvert.DeserializeObject<OrderByStoreResponse>(redisValue);
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
                return rsList;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}