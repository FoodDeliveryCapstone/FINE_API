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
        //Task<BaseResponsePagingViewModel<CustomerResponse>> GetCustomers(CustomerResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<CustomerResponse>> GetCustomerById(string customerId);
        //Task<BaseResponseViewModel<CustomerResponse>> CreateCustomer(CreateCustomerRequest request);
        //Task<BaseResponseViewModel<CustomerResponse>> GetCustomerByEmail(string email);
        Task<BaseResponseViewModel<LoginResponse>> LoginByMail(ExternalAuthRequest data);
        //Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(string customerId, UpdateCustomerRequest request);
        Task Logout(string fcmToken);
    }
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IFcmTokenService _customerFcmtokenService;
        //private readonly IAccountService _accountService;

        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper, IFcmTokenService customerFcmtokenService,
                                IConfiguration configuration /*IAccountService accountService*/)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _customerFcmtokenService = customerFcmtokenService;
            //_accountService = accountService;
        }

        public async Task<BaseResponseViewModel<LoginResponse>> LoginByMail(ExternalAuthRequest data)
        {
            try
            {
                string newAccessToken = null;

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
                    var customerResult = CreateCustomer(newCustomer).Result.Data;
                    customer = _mapper.Map<Customer>(customerResult);
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
                        access_token = newAccessToken,
                        customer = _mapper.Map<CustomerResponse>(customer)
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {
                var customer = _mapper.Map<CreateCustomerRequest, Customer>(request);

                customer.Id = Guid.NewGuid();
                customer.CustomerCode = customer.Id.ToString() + '_' + DateTime.Now.Date.ToString("ddMMyyyy");
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

        //public async Task<BaseResponsePagingViewModel<CustomerResponse>> GetCustomers(CustomerResponse request, PagingRequest paging)
        //{
        //    try
        //    {
        //        var customers = _unitOfWork.Repository<Customer>().GetAll()
        //            .ProjectTo<CustomerResponse>(_mapper.ConfigurationProvider)
        //            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

        //        return new BaseResponsePagingViewModel<CustomerResponse>()
        //        {
        //            Metadata = new PagingsMetadata()
        //            {
        //                Page = paging.Page,
        //                Size = paging.PageSize,
        //                Total = customers.Item1
        //            },
        //            Data = customers.Item2.ToList()
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<BaseResponseViewModel<CustomerResponse>> UpdateCustomer(string id, UpdateCustomerRequest request)
        //{
        //    try
        //    {
        //        var customerId = Guid.Parse(id);
        //        var customer = _unitOfWork.Repository<Customer>()
        //                                .Find(c => c.Id == customerId);
        //        if (customer == null)
        //            throw new ErrorResponse(404, (int)CustomerErrorEnums.NOT_FOUND_ID,
        //                                 CustomerErrorEnums.NOT_FOUND_ID.GetDisplayName());

        //        if (request.Phone != null)
        //        {
        //            var checkPhone = Utils.CheckVNPhone(request.Phone);
        //            if (checkPhone == false)
        //                throw new ErrorResponse(404, (int)CustomerErrorEnums.INVALID_PHONENUMBER,
        //                                    CustomerErrorEnums.INVALID_PHONENUMBER.GetDisplayName());
        //        }

        //        customer = _mapper.Map<UpdateCustomerRequest, Customer>(request, customer);

        //        await _unitOfWork.Repository<Customer>().UpdateDetached(customer);
        //        await _unitOfWork.CommitAsync();

        //        return new BaseResponseViewModel<CustomerResponse>()
        //        {
        //            Status = new StatusViewModel()
        //            {
        //                Message = "Success",
        //                Success = true,
        //                ErrorCode = 0
        //            },
        //            Data = _mapper.Map<CustomerResponse>(customer)
        //        };
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void CreateNewMemberShipCard(int uniId, int customerId)
        //{
        //    try
        //    {
        //        string newCardCode = GenerateNewMembershipCardCode(uniId);
        //        var card = _membershipCardService.GetMembershipCardByCodeByBrandId(newCardCode, uniId);

        //        while (card != null)
        //        {
        //            newCardCode = GenerateNewMembershipCardCode(uniId);
        //        }

        //        var newMembershipCard = _membershipCardService.AddMembershipCard(newCardCode, uniId, customerId).Result;

        //        _accountService.CreateAccountByMemCard(newMembershipCard.CardCode, 0, uniId, newMembershipCard.Id, (int)AccountTypeEnum.CreditAccount);
        //        _accountService.CreateAccountByMemCard(newMembershipCard.CardCode, 0, uniId, newMembershipCard.Id, (int)AccountTypeEnum.PointAccount);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private void CheckMembershipCard(Customer customer, int uniId)
        //{
        //    try
        //    {
        //        var membershipCard = customer.MembershipCards.Where(q => (bool)q.Active).FirstOrDefault();
        //        //Check MemberShipCard exist
        //        if (membershipCard != null)
        //        {
        //            var accountList = membershipCard.Accounts.ToList();
        //            if (accountList.Count() > 0)
        //            {
        //                //Create CreditAccount
        //                if (accountList.Where(x => x.Type == (int)AccountTypeEnum.CreditAccount) == null)
        //                {
        //                    _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.CreditAccount);
        //                }
        //                //Create PointAccount
        //                if (accountList.Where(q => q.Type == (int)AccountTypeEnum.PointAccount) == null)
        //                {
        //                    _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.PointAccount);
        //                }
        //            }
        //            else
        //            {
        //                _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.CreditAccount);
        //                _accountService.CreateAccountByMemCard(membershipCard.CardCode, 0, uniId, membershipCard.Id, (int)AccountTypeEnum.PointAccount);
        //            }
        //        }
        //        else
        //        {
        //            //Create new MembershipCard Account
        //            CreateNewMemberShipCard(uniId, customer.Id);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        //public static string GenerateNewMembershipCardCode(int uniId)
        //{
        //    var randomCode = new Random();
        //    switch (uniId)
        //    {
        //        case (int)DestinationTypeEnum.FPT:
        //            string chars = "0123456789";
        //            int length = 10;
        //            return new string(Enumerable.Repeat(chars, length)
        //            .Select(s => s[randomCode.Next(s.Length)]).ToArray());
        //        default:
        //            return String.Empty;
        //    }
        //}

        public async Task Logout(string fcmToken)
        {
            if (fcmToken != null && !fcmToken.Trim().Equals("") && !await _customerFcmtokenService.ValidToken(fcmToken))
            {
                _customerFcmtokenService.RemoveFcmTokens(new List<string> { fcmToken });
            }
        }
    }
}
