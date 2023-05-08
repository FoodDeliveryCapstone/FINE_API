//using ServiceStack.Redis;
//using System;
//using System.Collections.Generic;

//namespace FINE.Service.Helpers
//{
//    public class RedisHelper
//    {



//        public static AssetRedis SetAssestPoint(IRedisClientsManager redisClientManager, Guid Id, AssetRedis asset)
//        {
//            var cacheKey = Id.ToString();
//            var redis = redisClientManager.GetClient();
//            var timeInServer = TimeZoneInfo.ConvertTime(DateTime.Now,
//                     TimeZoneInfo.Local);

//            var currentAsset = redis.Get<AssetRedis>(cacheKey);

//            if (currentAsset is null)
//            {
//                asset.StartTime = timeInServer;
//                asset.CurrentLocation = asset.Location;
//                redis.Set<AssetRedis>(cacheKey, asset);
//                // redis.ExpireEntryIn(cacheKey, TimeSpan.FromMinutes(30));
//            }
//            else
//            {
//                currentAsset.CurrentLocation = asset.Location;
//                currentAsset.Location += ", " + asset.Location;
//                // var timeToLive = redis.GetTimeToLive(cacheKey);
//                redis.Set(cacheKey, currentAsset);
//                // redis.ExpireEntryIn(cacheKey, (TimeSpan)timeToLive);
//            }
//            // return redis.Get<AssetRedis>(cacheKey);
//            return null;
//        }

//        public static List<AssetRedisResponse> GetAssetRedis(int brandId, IRedisClientsManager redisClientManager)
//        {
//            List<AssetRedisResponse> result = new List<AssetRedisResponse>();

//            var redis = redisClientManager.GetClient();
//            var assets = redis.GetAllKeys();
//            foreach (var item in assets)
//            {
//                var currentAsset = redis.Get<AssetRedis>(item);
//                AssetRedisResponse response = new AssetRedisResponse
//                {
//                    CurrentLocationGeometry = GeoJsonHelper.ParseStringToPoint(currentAsset.CurrentLocation),
//                    BrandId = currentAsset.BrandId,
//                    BrandName = currentAsset.BrandName,
//                    Id = currentAsset.Id,
//                    Name = currentAsset.Name,
//                    StoreId = currentAsset.StoreId,
//                    StoreName = currentAsset.StoreName,
//                    Type = currentAsset.Type,
//                    StartTime = currentAsset.StartTime
//                };
//                if (currentAsset.BrandId == brandId)
//                {
//                    if (currentAsset.Location.Contains(","))
//                    {
//                        response.Geometry = GeoJsonHelper.ParseStringToLineString(currentAsset.Location);
//                    }
//                    else
//                    {
//                        response.Geometry = GeoJsonHelper.ParseStringToLineString(currentAsset.Location);
//                    }
//                    result.Add(response);
//                }
//            }
//            return result;
//        }


//    }
//}