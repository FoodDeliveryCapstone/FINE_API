using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using FINE.Service.Helpers;
using Microsoft.EntityFrameworkCore;
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
    public interface IPackageService
    {
        Task<BaseResponseViewModel<PackageResponse>> GetPackage(string staffId, string timeSlotId);
    }
    public class PackageService : IPackageService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PackageService(IMapper mapper, UnitOfWork unitOfWork) 
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<PackageResponse>> GetPackage(string storeId, string timeSlotId)
        {
            try
            {
                var store = await _unitOfWork.Repository<Store>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(storeId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = store.StoreName + "-" + timeSlot.ArriveTime;

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);
                PackageResponse staffOrderResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);

                return new BaseResponseViewModel<PackageResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = staffOrderResponse
                };
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
