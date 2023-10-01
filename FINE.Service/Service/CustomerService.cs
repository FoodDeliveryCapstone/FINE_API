using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Core;
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
using FirebaseAdmin.Messaging;
using Hangfire.MemoryStorage.Database;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Mathematics;
using StackExchange.Redis;
using System;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ICustomerService
    {
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(string customerId);
        Task<BaseResponsePagingViewModel<CustomerTransactionResponse>> GetTransactionByCustomerId(string customerId, CustomerTransactionResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(string customerId, UpdateCustomerRequest request);
        Task<BaseResponseViewModel<LoginResponse>> Login(ExternalAuthRequest data);
        Task<BaseResponseViewModel<CustomerResponse>> FindCustomer(string phoneNumber);
        Task<List<CustomerResponse>> SimulateCreateCustomer(int quantity);
        Task SendInvitation(string customerId, string adminId, string partyCode);
        Task Logout(string fcmToken);
    }
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly IAccountService _accountService;
        private readonly IFirebaseMessagingService _fm;
        private readonly INotifyService _notifyService;


        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper, IFcmTokenService customerFcmtokenService,
                                IConfiguration configuration, IAccountService accountService, INotifyService notifyService, IFirebaseMessagingService fm)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _customerFcmtokenService = customerFcmtokenService;
            _accountService = accountService;
            _notifyService = notifyService;
            _fm = fm;
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
                string customerPhone = userRecord.PhoneNumber;

                if (userRecord.PhoneNumber is not null && userRecord.PhoneNumber.Contains("+84"))
                {
                    customerPhone = userRecord.PhoneNumber.Replace("+84", "0");
                }
                //check exist customer 
                var customer = _unitOfWork.Repository<Customer>().GetAll()
                                .FirstOrDefault(x => x.Phone == customerPhone);

                //new customer => add fcm map with Id
                if (customer is null)
                {
                    CreateCustomerRequest newCustomer = new CreateCustomerRequest()
                    {
                        Name = userRecord.DisplayName,
                        Email = userRecord.Email,
                        Phone = customerPhone,
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

        public async Task<List<CustomerResponse>> SimulateCreateCustomer(int quantity)
        {
            try
            {
                var result = new List<CustomerResponse>();
                for (int i = 0; i <= quantity; i++)
                {
                    const string chars = "0123456789";
                    Random random = new Random();

                    char[] resultRnd = new char[7];
                    for (int x = 0; x < 7; x++)
                    {
                        resultRnd[x] = chars[random.Next(chars.Length)];
                    }
                    var strings = new string(resultRnd);
                    CreateCustomerRequest customer = new CreateCustomerRequest()
                    {
                        Name = "Test" + i.ToString(),
                        Email = "Test" + i.ToString() + "@gmail.com",
                        Phone = "096" + strings,
                    };
                    var customerResult = CreateCustomer(customer).Result.Data;
                    result.Add(customerResult);
                    Task.Delay(100).Wait();
                }
                return result;
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

                var result = _mapper.Map<CustomerResponse>(customer);
                result.Balance = customer.Accounts.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.CreditAccount).Balance;
                result.Point = customer.Accounts.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.PointAccount).Balance;

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
                    Data = result
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
                            .Where(x => x.Phone.Equals(phoneNumber))
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

        public async Task SendInvitation(string customerId, string adminId, string partyCode)
        {
            try
            {
                var party = await _unitOfWork.Repository<Party>().GetAll()
                                                    .FirstOrDefaultAsync(x => x.PartyCode == partyCode);

                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                var admin = coOrder.PartyOrder.Where(x => x.Customer.Id == Guid.Parse(adminId)).FirstOrDefault();

                var customerToken = await _unitOfWork.Repository<Fcmtoken>().GetAll()
                                                    .Where(x => x.UserId == Guid.Parse(customerId))
                                                    .Select(x => x.Token)
                                                    .FirstOrDefaultAsync();

                Notification notification = new Notification
                {
                    Title = Constants.NOTIFICATION_INVITATION_TITLE,
                    Body = String.Format("Bạn {0} gửi đến bạn lời mời gia nhập party được giao vào lúc {1}"
                                        , admin.Customer.Name, coOrder.TimeSlot.ArriveTime)
                };

                var data = new Dictionary<string, string>()
                    {
                        { "key", partyCode },
                        { "type", NotifyTypeEnum.ForInvitation.ToString()}
                    };

                _fm.SendToToken(customerToken, notification, data);
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

                if (newCustomer.Phone is not null)
                {
                    if (newCustomer.Phone.Contains("+84"))
                    {
                        newCustomer.Phone = newCustomer.Phone.Replace("+84", "0");
                    }
                }

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

        public async Task<BaseResponsePagingViewModel<CustomerTransactionResponse>> GetTransactionByCustomerId(string customerId, CustomerTransactionResponse filter, PagingRequest paging)
        {
            try
            {
                var transaction = _unitOfWork.Repository<Transaction>().GetAll()
                                    .Where(x => x.Account.CustomerId == Guid.Parse(customerId)
                                            && x.Account.Type == (int)AccountTypeEnum.CreditAccount)
                                    .OrderByDescending(x => x.CreatedAt)
                                    .ProjectTo<CustomerTransactionResponse>(_mapper.ConfigurationProvider)
                                    .DynamicFilter(filter)
                                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);


                return new BaseResponsePagingViewModel<CustomerTransactionResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = transaction.Item1
                    },
                    Data = transaction.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
