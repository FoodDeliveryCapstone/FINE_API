using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using NetTopologySuite.Mathematics;
using System;
using System.Linq.Dynamic.Core;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ICustomerService
    {
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(string customerId);
        Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(string customerId, UpdateCustomerRequest request);
        Task<BaseResponseViewModel<LoginResponse>> Login(ExternalAuthRequest data);
        Task<BaseResponseViewModel<CustomerResponse>> FindCustomer(string phoneNumber);
        Task Logout(string fcmToken);
    }
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly IAccountService _accountService;

        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper, IFcmTokenService customerFcmtokenService,
                                IConfiguration configuration, IAccountService accountService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _customerFcmtokenService = customerFcmtokenService;
            _accountService = accountService;
        }

        public async Task<BaseResponseViewModel<LoginResponse>> Login(ExternalAuthRequest data)
        {
            try
            {
                string newAccessToken = null;
                bool isFirstLogin = false;

                if (data.FcmToken != null && data.FcmToken.Trim().Length > 0)
                {
                    if (!await _customerFcmtokenService.ValidToken(data.FcmToken))
                        throw new ErrorResponse(400, (int)FcmTokenErrorEnums.INVALID_TOKEN,
                                             FcmTokenErrorEnums.INVALID_TOKEN.GetDisplayName());
                }

                //decode token -> user record
                var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                FirebaseToken decodeToken = await auth.VerifyIdTokenAsync(data.IdToken);
                UserRecord userRecord = await auth.GetUserAsync(decodeToken.Uid);

                //check exist customer 
                var customer = _unitOfWork.Repository<Customer>().GetAll()
                                .FirstOrDefault(x => x.Email.Contains(userRecord.Email) || x.Phone.Contains(userRecord.PhoneNumber));

                //new customer => add fcm map with Id
                if (customer is null)
                {
                    CreateCustomerRequest newCustomer = new CreateCustomerRequest()
                    {
                        Name = userRecord.DisplayName,
                        Email = userRecord.Email,
                        Phone = userRecord.PhoneNumber,
                        ImageUrl = userRecord.PhotoUrl
                    };

                    //create customer
                    var customerResult = CreateCustomer(newCustomer).Result.Data;
                    customer = _mapper.Map<Customer>(customerResult);
                    isFirstLogin = true;
                }

                _customerFcmtokenService.AddFcmToken(data.FcmToken, customer.Id);
                newAccessToken = AccessTokenManager.GenerateJwtToken(string.IsNullOrEmpty(customer.Name) ? "" : customer.Name, null, customer.Id, _configuration);
                
                return new BaseResponseViewModel<LoginResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = new LoginResponse()
                    {
                        Access_token = newAccessToken,
                        IsFirstLogin = isFirstLogin,
                        Customer = _mapper.Map<CustomerResponse>(customer)
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(string id)
        {
            try
            {
                var customerId = Guid.Parse(id);
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                    .Where(x => x.Id == customerId)
                    .FirstOrDefaultAsync();

                if (customer == null)
                    throw new ErrorResponse(404, (int)CustomerErrorEnums.NOT_FOUND,
                        CustomerErrorEnums.NOT_FOUND.GetDisplayName());

                return new BaseResponseViewModel<CustomerResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<CustomerResponse>(customer)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(string customerId, UpdateCustomerRequest request)
        {
            try
            {
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(c => c.Id == Guid.Parse(customerId));
                if (request.Phone is not null)
                {
                    if (request.Phone.Contains("+84"))
                    {
                        request.Phone = request.Phone.Replace("+84", "0");
                    }
                    var check = Utils.CheckVNPhone(request.Phone);

                    if (check == false)
                        throw new ErrorResponse(404, (int)CustomerErrorEnums.INVALID_PHONENUMBER,
                                            CustomerErrorEnums.INVALID_PHONENUMBER.GetDisplayName());
                }

                customer = _mapper.Map<UpdateCustomerRequest, Customer>(request, customer);

                await _unitOfWork.Repository<Customer>().UpdateDetached(customer);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CustomerResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<CustomerResponse>(customer)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> FindCustomer(string phoneNumber)
        {
            try
            {
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                            .Where(x => x.Phone.Contains(phoneNumber))
                            .ProjectTo<CustomerResponse>(_mapper.ConfigurationProvider)
                            .FirstOrDefaultAsync();

                return new BaseResponseViewModel<CustomerResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = customer
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<BaseResponseViewModel<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {
                var newCustomer = _mapper.Map<CreateCustomerRequest, Customer>(request);

                newCustomer.Id = Guid.NewGuid();
                newCustomer.CustomerCode = newCustomer.Id.ToString() + '_' + DateTime.Now.Date.ToString("ddMMyyyy");
                newCustomer.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Customer>().InsertAsync(newCustomer);

                _accountService.CreateAccount(newCustomer.Id);

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CustomerResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<CustomerResponse>(newCustomer)
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task Logout(string fcmToken)
        {
            if (fcmToken != null && !fcmToken.Trim().Equals("") && !await _customerFcmtokenService.ValidToken(fcmToken))
            {
                _customerFcmtokenService.RemoveFcmTokens(new List<string> { fcmToken });
            }
        }

    }
}
