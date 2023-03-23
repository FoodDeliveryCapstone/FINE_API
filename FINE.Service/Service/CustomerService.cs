using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
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
using NTQ.Sdk.Core.Utilities;
using System;
using System.Linq.Dynamic.Core;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ICustomerService
    {
        Task<BaseResponsePagingViewModel<CustomerResponse>> GetCustomers(CustomerResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(int customerId);
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerByFCM(string accessToken);
        Task<BaseResponseViewModel<CustomerResponse>> CreateCustomer(CreateCustomerRequest request);
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerByEmail(string email);
        Task<BaseResponseViewModel<LoginResponse>> Login(ExternalAuthRequest data);
        Task Logout(string fcmToken);
        Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(int customerId, UpdateCustomerRequest request);
    }
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly IMembershipCardService _membershipCardService;
        private readonly IAccountService _accountService;

        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper, IFcmTokenService customerFcmtokenService,
                                IConfiguration configuration, IMembershipCardService membershipCardService, IAccountService accountService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _customerFcmtokenService = customerFcmtokenService;
            _membershipCardService = membershipCardService;
            _accountService = accountService;
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> GetCustomerByEmail(string email)
        {
            try
            {
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                     .Where(x => x.Email.Contains(email)).FirstOrDefaultAsync();

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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(int customerId)
        {
            try
            {
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                    .Where(x => x.Id == customerId)
                    .FirstOrDefaultAsync();

                if (customer == null)
                    throw new ErrorResponse(404, (int)CustomerErrorEnums.NOT_FOUND_ID,
                        CustomerErrorEnums.NOT_FOUND_ID.GetDisplayName());

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

        public async Task<BaseResponseViewModel<CustomerResponse>> GetCustomerByFCM(string accessToken)
        {
            try
            {
                var token = await _unitOfWork.Repository<Fcmtoken>().GetAll()
                            .FirstOrDefaultAsync(x => x.Token.Contains(accessToken));

                return new BaseResponseViewModel<CustomerResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<CustomerResponse>(token.Customer)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<CustomerResponse>> GetCustomers(CustomerResponse request, PagingRequest paging)
        {
            try
            {
                var customers = _unitOfWork.Repository<Customer>().GetAll()
                    .ProjectTo<CustomerResponse>(_mapper.ConfigurationProvider)
                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<CustomerResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = customers.Item1
                    },
                    Data = customers.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {

                var customer = _mapper.Map<CreateCustomerRequest, Customer>(request);

                #region recognize school by email root
                string[] splitEmail = customer.Email.Split('@');
                var rootEmail = splitEmail[1];
                var uniInfo = _unitOfWork.Repository<UniversityInfo>().GetAll()
                                            .FirstOrDefault(x => x.Domain.Contains(rootEmail));
                if (uniInfo == null)
                    throw new ErrorResponse(404, (int)CampusErrorEnums.NOT_FOUND_ID, CampusErrorEnums.NOT_FOUND_ID.GetDisplayName());
                #endregion

                var code = Ultils.GenerateRandomCode();

                customer.CustomerCode = uniInfo.University.UniCode + "-" + code;
                customer.UniversityId = uniInfo.University.Id;
                customer.UniInfoId = uniInfo.Id;
                customer.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Customer>().InsertAsync(customer);
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(int customerId, UpdateCustomerRequest request)
        {
            try
            {
                Customer customer = null;
                customer = _unitOfWork.Repository<Customer>()
                                        .Find(c => c.Id == customerId);

                if (customer == null)
                    throw new ErrorResponse(404, (int)CustomerErrorEnums.NOT_FOUND_ID,
                                         CustomerErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var checkPhone = Ultils.CheckVNPhone(request.Phone);

                if (checkPhone == false)
                    throw new ErrorResponse(404, (int)CustomerErrorEnums.INVALID_PHONENUMBER,
                                        CustomerErrorEnums.INVALID_PHONENUMBER.GetDisplayName());

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

        public async Task<BaseResponseViewModel<LoginResponse>> Login(ExternalAuthRequest data)
        {
            try
            {
                // check fcm token 
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
                                .FirstOrDefault(x => x.Email.Contains(userRecord.Email));

                //new customer => add fcm map with Id
                if (customer == null)
                {
                    CreateCustomerRequest newCustomer = new CreateCustomerRequest()
                    {
                        Name = userRecord.DisplayName,
                        Email = userRecord.Email,
                        Phone = userRecord.PhoneNumber,
                        ImageUrl = userRecord.PhotoUrl
                    };

                    //create customer
                    await CreateCustomer(newCustomer);

                    //create account (wallet) for customer
                    customer = _unitOfWork.Repository<Customer>().GetAll()
                                .FirstOrDefault(x => x.Email.Contains(userRecord.Email));

                    CreateNewMemberShipCard(customer.UniversityId, customer.Id);

                    var membership = _membershipCardService.GetMembershipCardActiveByCustomerIdAndBrandId(customer.Id, customer.UniversityId);

                    //generate token
                    var newToken = AccessTokenManager.GenerateJwtToken(string.IsNullOrEmpty(customer.Name) ? "" : customer.Name, 0, customer.Id, _configuration);

                    decimal balance = 0;
                    decimal point = 0;
                    var account = _accountService.GetAccountByCustomerId(customer.Id).Result;

                    //Add fcm token 
                    if (data.FcmToken != null && data.FcmToken.Trim().Length > 0)
                        _customerFcmtokenService.AddFcmToken(data.FcmToken, customer.Id);

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
                            access_token = newToken,
                            customer = _mapper.Map<CustomerResponse>(customer)
                        }
                    };
                }
                else
                {
                    CheckMembershipCard(customer, customer.UniversityId);

                    var newToken = AccessTokenManager.GenerateJwtToken(string.IsNullOrEmpty(customer.Name) ? "" : customer.Name, 0, customer.Id, _configuration);

                    if (data.FcmToken != null && !data.FcmToken.Trim().Equals(""))
                         _customerFcmtokenService.AddFcmToken(data.FcmToken, customer.Id);

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
                            access_token = newToken,
                            customer = _mapper.Map<CustomerResponse>(customer)
                        }
                    };
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public void CreateNewMemberShipCard(int uniId, int customerId)
        {
            try
            {
                string newCardCode = GenerateNewMembershipCardCode(uniId);
                var card = _membershipCardService.GetMembershipCardByCodeByBrandId(newCardCode, uniId);

                while (card != null)
                {
                    newCardCode = GenerateNewMembershipCardCode(uniId);
                }

                var newMembershipCard = _membershipCardService.AddMembershipCard(newCardCode, uniId, customerId).Result;

                _accountService.CreateAccountByMemCard(newMembershipCard.CardCode, 0, uniId, newMembershipCard.Id, (int)AccountTypeEnum.CreditAccount);
                _accountService.CreateAccountByMemCard(newMembershipCard.CardCode, 0, uniId, newMembershipCard.Id, (int)AccountTypeEnum.PointAccount);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CheckMembershipCard(Customer customer, int uniId)
        {
            try
            {
                var membershipCard = customer.MembershipCards.Where(q => (bool)q.Active).FirstOrDefault();
                //Check MemberShipCard exist
                if (membershipCard != null)
                {
                    var accountList = membershipCard.Accounts.ToList();
                    if (accountList.Count() > 0)
                    {
                        //Create CreditAccount
                        if (accountList.Where(x => x.Type == (int)AccountTypeEnum.CreditAccount) == null)
                        {
                            _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.CreditAccount);
                        }
                        //Create PointAccount
                        if (accountList.Where(q => q.Type == (int)AccountTypeEnum.PointAccount) == null)
                        {
                            _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.PointAccount);
                        }
                    }
                    else
                    {
                        _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.CreditAccount);
                        _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.PointAccount);
                    }
                }
                else
                {
                    //Create new MembershipCard Account
                    CreateNewMemberShipCard(uniId, customer.Id);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string GenerateNewMembershipCardCode(int uniId)
        {
            var randomCode = new Random();
            switch (uniId)
            {
                case (int)CampusEnum.FPT:
                    string chars = "0123456789";
                    int length = 10;
                    return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[randomCode.Next(s.Length)]).ToArray());
                default:
                    return String.Empty;
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
