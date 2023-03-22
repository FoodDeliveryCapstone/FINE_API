using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStaffService
    {
        Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(StaffResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StaffResponse>> GetStaffById(int staffId);
        Task<BaseResponseViewModel<dynamic>> Login(LoginRequest request);
        Task<BaseResponseViewModel<StaffResponse>> CreateAdminManager(CreateStaffRequest request);
        Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(int staffId, UpdateStaffRequest request);
    }

    public class StaffService : IStaffService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly string _fineSugar;


        public StaffService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config, IFcmTokenService customerFcmtokenService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
            _fineSugar = _config["FineSugar"];
            _customerFcmtokenService = customerFcmtokenService;
        }

        public async Task<BaseResponseViewModel<StaffResponse>> CreateAdminManager(CreateStaffRequest request)
        {
            var checkStaff = _unitOfWork.Repository<Staff>().Find(x => x.Username.Contains(request.Username));

            if (checkStaff != null)
                throw new ErrorResponse(404, (int)StaffErrorEnum.STAFF_EXSIST,
                                    StaffErrorEnum.STAFF_EXSIST.GetDisplayName());

            var staff = _mapper.Map<CreateStaffRequest,Staff>(request);

            staff.Password = Ultils.GetHash(request.Pass, _fineSugar);
            staff.IsAvailable = true;
            staff.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<Staff>().InsertAsync(staff);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<StaffResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffResponse>(staff)
            };
        }

        public async Task<BaseResponseViewModel<StaffResponse>> GetStaffById(int staffId)
        {
            var staff = _unitOfWork.Repository<Staff>().GetAll()
                                   .FirstOrDefault(x => x.Id == staffId);
            if (staff == null)
            {
                throw new ErrorResponse(404, (int)StaffErrorEnum.NOT_FOUND_ID,
                                    StaffErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }
            return new BaseResponseViewModel<StaffResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffResponse>(staff)
            };
        }

        public async Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(StaffResponse filter, PagingRequest paging)
        {
            var staff = _unitOfWork.Repository<Staff>().GetAll()
                                   .ProjectTo<StaffResponse>(_mapper.ConfigurationProvider)
                                   .DynamicFilter(filter)
                                   .DynamicSort(filter)
                                   .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<StaffResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = staff.Item1
                },
                Data = staff.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<dynamic>> Login(LoginRequest request)
        {
            var staff = _unitOfWork.Repository<Staff>().GetAll()
                                    .Where(x => x.Username.Equals(request.UserName) && x.IsAvailable == true)
                                    .FirstOrDefault();

            if (staff == null || !Ultils.CompareHash(request.Password, staff.Password, _fineSugar))
                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Success = false,
                        Message = StaffErrorEnum.LOGIN_FAIL.GetDisplayName(),
                        ErrorCode = (int)StaffErrorEnum.LOGIN_FAIL
                    }
                };

            if (staff.RoleType == (int)SystemRoleEnum.StoreManager)
            {
                if (staff.StoreId == null)
                {
                    return new BaseResponseViewModel<dynamic>()
                    {
                        Status = new StatusViewModel()
                        {
                            Success = false,
                            Message = CampusErrorEnums.NOT_FOUND_ID.GetDisplayName(),
                            ErrorCode = (int)CampusErrorEnums.NOT_FOUND_ID
                        }
                    };
                }
            }

            if (request.FcmToken != null && request.FcmToken.Trim().Length > 0)
                _customerFcmtokenService.AddStaffFcmToken(request.FcmToken, staff.Id);

            var token = AccessTokenManager.GenerateJwtToken(staff.Name, staff.RoleType, staff.Id, _config);
            return new BaseResponseViewModel<dynamic>()
            {
                Status = new StatusViewModel()
                {
                    Success = true,
                    Message = "Success",
                    ErrorCode = 0
                },
                Data = new
                {
                    AccessToken = token,
                    Name = staff.Name,
                    Roles = staff.RoleType
                }
            };
        }

        public async Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(int staffId, UpdateStaffRequest request)
        {
            var staff = _unitOfWork.Repository<Staff>().Find(x => x.Id == staffId);
            if (staff == null)
            {
                throw new ErrorResponse(404, (int)StaffErrorEnum.NOT_FOUND_ID,
                                    StaffErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }
            var staffMappingResult = _mapper.Map<UpdateStaffRequest, Staff>(request, staff);
            staffMappingResult.UpdateAt = DateTime.Now;
            await _unitOfWork.Repository<Staff>()
                            .UpdateDetached(staffMappingResult);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<StaffResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffResponse>(staffMappingResult)
            };
        }
    }
}
