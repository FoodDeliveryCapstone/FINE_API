using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ISimulateService
    {
        Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinish(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToDelivering(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToBoxStored(SimulateOrderStatusRequest request);
    }

    public class SimulateService : ISimulateService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly IStaffService _staffService;

        public SimulateService(IMapper mapper, IUnitOfWork unitOfWork, IOrderService orderService, IStaffService staffService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _staffService = staffService;
        }

        public async Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request)
        {
            try
            {
                var rand = new Random();
                if (!Guid.TryParse(request.TimeSlotId, out Guid checkGuid))
                {
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                      TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
                }
                var timeslot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == Guid.Parse(request.TimeSlotId));
                if (timeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                       TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                var timeslotResponse = new TimeSlotOrderResponse()
                {
                    Id = timeslot.Id.ToString(),
                    CloseTime = timeslot.CloseTime,
                    ArriveTime = timeslot.ArriveTime,
                    CheckoutTime = timeslot.CheckoutTime,
                };

                SimulateResponse response = new SimulateResponse()
                {
                    Timeslot = timeslotResponse,
                    SingleOrderResult = new SimulateSingleOrderResponse()
                    {
                        OrderSuccess = new List<OrderSuccess>(),
                        OrderFailed = new List<OrderFailed>()
                    },
                    CoOrderOrderResult = new SimulateCoOrderResponse()
                    {
                        OrderSuccess = new List<OrderSuccess>(),
                        OrderFailed = new List<OrderFailed>()
                    }
                };

                var productInMenu = await _unitOfWork.Repository<ProductInMenu>().GetAll()
                            .Include(x => x.Menu)
                            .Include(x => x.Product)
                            .Where(x => x.Menu.TimeSlotId == Guid.Parse(request.TimeSlotId))
                            .GroupBy(x => x.Product)
                            .Select(x => x.Key)
                            .ToListAsync();
                var station = await _unitOfWork.Repository<Station>().GetAll().ToListAsync();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .ToListAsync();
                getAllCustomer = getAllCustomer.OrderBy(x => rand.Next()).ToList();
                int customerIndex = 0;

                #region Single Order
                if (request.SingleOrder is not null)
                {

                    for (int quantity = 1; quantity <= request.SingleOrder.TotalOrder; quantity++)
                    {
                        customerIndex = quantity - 1;
                        var customer = getAllCustomer.ElementAt(customerIndex);

                        var payload = new CreatePreOrderRequest
                        {
                            OrderType = OrderTypeEnum.OrderToday,
                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                        };

                        int numberProductTake = rand.Next(1, 4);
                        int randomQuantity = rand.Next(1, 4);
                        var randomProduct = productInMenu.OrderBy(x => rand.Next());
                        payload.OrderDetails = randomProduct
                            .Take(numberProductTake)
                            .Select(x => new CreatePreOrderDetailRequest
                            {
                                ProductId = x.Id,
                                Quantity = randomQuantity
                            })
                            .ToList();
                        try
                        {
                            var rs = _orderService.CreatePreOrder(customer.Id.ToString(), payload).Result.Data;
                            //var rs = _orderService.CreatePreOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payload).Result.Data;
                            var stationId = station
                                                .OrderBy(x => rand.Next())
                                                .Select(x => x.Id)
                                                .FirstOrDefault();

                            var payloadCreateOrder = new CreateOrderRequest()
                            {
                                Id = rs.Id,
                                OrderCode = rs.OrderCode,
                                PartyCode = null,
                                TotalAmount = rs.TotalAmount,
                                FinalAmount = rs.FinalAmount,
                                TotalOtherAmount = rs.TotalOtherAmount,
                                OrderType = (OrderTypeEnum)rs.OrderType,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                StationId = stationId.ToString(),
                                //StationId = "CF42E4AF-F08F-4CC7-B83F-DA449857B2D3",
                                PaymentType = PaymentTypeEnum.FineWallet,
                                IsPartyMode = false,
                                ItemQuantity = rs.ItemQuantity,
                                Point = rs.Point,
                                OrderDetails = rs.OrderDetails.Select(detail => new CreateOrderDetail()
                                {
                                    Id = detail.Id,
                                    OrderId = detail.Id,
                                    ProductInMenuId = detail.ProductInMenuId,
                                    StoreId = detail.StoreId,
                                    ProductCode = detail.ProductCode,
                                    ProductName = detail.ProductName,
                                    UnitPrice = detail.UnitPrice,
                                    Quantity = detail.Quantity,
                                    TotalAmount = detail.TotalAmount,
                                    FinalAmount = detail.FinalAmount,
                                    Note = detail.Note

                                }).ToList(),
                                OtherAmounts = rs.OtherAmounts
                            };

                            try
                            {
                                var result =  _staffService.CreateOrderForSimulate(customer.Id.ToString(), payloadCreateOrder).Result.Data;
                                //var result = _orderService.CreateOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payloadCreateOrder).Result.Data;
                                var orderSuccess = new OrderSuccess
                                {
                                    Id = result.Id,
                                    OrderCode = result.OrderCode,
                                    ItemQuantity = result.ItemQuantity,
                                    Customer = result.Customer,
                                };
                                response.SingleOrderResult.OrderSuccess.Add(orderSuccess);
                            }
                            catch (Exception ex)
                            {
                                ErrorResponse err = (ErrorResponse)ex.InnerException;
                                var orderFail = new OrderFailed
                                {
                                    OrderCode = rs.OrderCode,
                                    Customer = rs.Customer,
                                    Status = new StatusViewModel
                                    {
                                        Message = err.Error.Message,
                                        Success = false,
                                        ErrorCode = err.Error.ErrorCode,
                                    }
                                };
                                response.SingleOrderResult.OrderFailed.Add(orderFail);
                            }
                        }
                        catch (Exception ex)
                        {
                            var randomCustomer = getAllCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            ErrorResponse err = (ErrorResponse)ex.InnerException;
                            var orderFail = new OrderFailed
                            {
                                OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                Customer = new CustomerOrderResponse
                                {
                                    Id = randomCustomer.Id,
                                    CustomerCode = randomCustomer.Name,
                                    Email = randomCustomer.Email,
                                    Name = randomCustomer.Name,
                                    Phone = randomCustomer.Phone,
                                },
                                Status = new StatusViewModel
                                {
                                    Message = err.Error.Message,
                                    Success = false,
                                    ErrorCode = err.Error.ErrorCode,
                                }
                            };
                            response.SingleOrderResult.OrderFailed.Add(orderFail);
                        }
                    }
                }
                #endregion

                #region CoOrder
                if (request.CoOrder is not null)
                {
                    var listCustomer = getAllCustomer
                                           .OrderBy(x => rand.Next())
                                           .Take((int)request.CoOrder.CustomerEach)
                                           .ToList();
                    var openCoOrderCustomer = listCustomer.OrderBy(x => rand.Next()).Take(1).FirstOrDefault();
                    var restCustomers = listCustomer.Skip(1).ToList();

                    var totalCoOrderSuccess = request.CoOrder.TotalOrderSuccess;
                    var totalCoOrderFailed = request.CoOrder.TotalOrder - request.CoOrder.TotalOrderSuccess;

                    for (int quantity = 1; quantity <= request.CoOrder.TotalOrder; quantity++)
                    {
                        if (totalCoOrderSuccess > 0 && totalCoOrderFailed > 0)
                        {
                            if (rand.Next() % 2 == 0)
                            {
                                var payloadPreOrder = new CreatePreOrderRequest
                                {
                                    OrderType = OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    PartyType = PartyOrderType.CoOrder
                                };

                                int numberProductTake = rand.Next(1, 4);
                                payloadPreOrder.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(numberProductTake)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 1
                                    })
                                    .ToList();

                                var openCoOrder = _orderService.OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                                try
                                {
                                    foreach (var cus in restCustomers)
                                    {
                                        var joinCoOrder = _orderService.JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                        var cusPayloadPreOrder = new CreatePreOrderRequest
                                        {
                                            OrderType = OrderTypeEnum.OrderToday,
                                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                                            PartyType = PartyOrderType.CoOrder
                                        };

                                        int newNumberProductTake = rand.Next(1, 4);
                                        cusPayloadPreOrder.OrderDetails = productInMenu
                                            .OrderBy(x => rand.Next())
                                            .Take(newNumberProductTake)
                                            .Select(x => new CreatePreOrderDetailRequest
                                            {
                                                ProductId = x.Id,
                                                Quantity = 1
                                            })
                                            .ToList();
                                        var addProduct = _orderService.AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                        var confirmCoOrder = _orderService.FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    }


                                    var preCoOrder = _orderService.CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (OrderTypeEnum)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;

                                    var stationId = station
                                                        .OrderBy(x => rand.Next())
                                                        .Select(x => x.Id)
                                                        .FirstOrDefault();

                                    var payloadCreateOrder = new CreateOrderRequest()
                                    {
                                        Id = preCoOrder.Id,
                                        OrderCode = preCoOrder.OrderCode,
                                        PartyCode = openCoOrder.Result.Data.PartyCode,
                                        TotalAmount = preCoOrder.TotalAmount,
                                        FinalAmount = preCoOrder.FinalAmount,
                                        TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                        OrderType = (OrderTypeEnum)preCoOrder.OrderType,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        StationId = stationId.ToString(),
                                        PaymentType = PaymentTypeEnum.FineWallet,
                                        IsPartyMode = true,
                                        ItemQuantity = preCoOrder.ItemQuantity,
                                        Point = preCoOrder.Point,
                                        OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
                                        {
                                            Id = detail.Id,
                                            OrderId = detail.Id,
                                            ProductInMenuId = detail.ProductInMenuId,
                                            StoreId = detail.StoreId,
                                            ProductCode = detail.ProductCode,
                                            ProductName = detail.ProductName,
                                            UnitPrice = detail.UnitPrice,
                                            Quantity = detail.Quantity,
                                            TotalAmount = detail.TotalAmount,
                                            FinalAmount = detail.FinalAmount,
                                            Note = detail.Note

                                        }).ToList(),
                                        OtherAmounts = preCoOrder.OtherAmounts
                                    };
                                    try
                                    {
                                        var result = _staffService.CreateOrderForSimulate(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;

                                        var orderSuccess = new OrderSuccess
                                        {
                                            Id = result.Id,
                                            OrderCode = result.OrderCode,
                                            Customer = result.Customer,
                                        };
                                        response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = preCoOrder.OrderCode,
                                            Customer = preCoOrder.Customer,
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                                    if (ex is ErrorResponse errorResponse)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = randomCustomer.Id,
                                                CustomerCode = randomCustomer.Name,
                                                Email = randomCustomer.Email,
                                                Name = randomCustomer.Name,
                                                Phone = randomCustomer.Phone,
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = "",
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = randomCustomer.Id,
                                                CustomerCode = randomCustomer.Name,
                                                Email = randomCustomer.Email,
                                                Name = randomCustomer.Name,
                                                Phone = randomCustomer.Phone,
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = ex.Message,
                                                Success = false,
                                                ErrorCode = 400
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                totalCoOrderSuccess--;
                            }
                            else
                            {
                                var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                                if (!Utils.CheckTimeSlot(timeslot))
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = randomCustomer.Id,
                                            CustomerCode = randomCustomer.Name,
                                            Email = randomCustomer.Email,
                                            Name = randomCustomer.Name,
                                            Phone = randomCustomer.Phone,
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = "Out of Time Slot!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else
                                {
                                    int randomError = rand.Next(1, 2);
                                    if (randomError == (int)SimulateOrderFailedType.Payment)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                                CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                                Email = "",
                                                Name = "Di Na Cuteeeee",
                                                Phone = "+84838073755",
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = "Balance is not enough",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = randomCustomer.Id,
                                                CustomerCode = randomCustomer.Name,
                                                Email = randomCustomer.Email,
                                                Name = randomCustomer.Name,
                                                Phone = randomCustomer.Phone,
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = "This product is not avaliable!",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                totalCoOrderFailed--;
                            }
                        }
                        else if (totalCoOrderSuccess > 0)
                        {

                            var payloadPreOrder = new CreatePreOrderRequest
                            {
                                OrderType = OrderTypeEnum.OrderToday,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                PartyType = PartyOrderType.CoOrder
                            };

                            int numberProductTake = rand.Next(1, 4);
                            payloadPreOrder.OrderDetails = productInMenu
                                .OrderBy(x => rand.Next())
                                .Take(numberProductTake)
                                .Select(x => new CreatePreOrderDetailRequest
                                {
                                    ProductId = x.Id,
                                    Quantity = 1
                                })
                                .ToList();

                            var openCoOrder = _orderService.OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                            try
                            {
                                foreach (var cus in restCustomers)
                                {
                                    var joinCoOrder = _orderService.JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    var cusPayloadPreOrder = new CreatePreOrderRequest
                                    {
                                        OrderType = OrderTypeEnum.OrderToday,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        PartyType = PartyOrderType.CoOrder
                                    };

                                    int newNumberProductTake = rand.Next(1, 4);
                                    cusPayloadPreOrder.OrderDetails = productInMenu
                                        .OrderBy(x => rand.Next())
                                        .Take(newNumberProductTake)
                                        .Select(x => new CreatePreOrderDetailRequest
                                        {
                                            ProductId = x.Id,
                                            Quantity = 1
                                        })
                                        .ToList();
                                    var addProduct = _orderService.AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                    var confirmCoOrder = _orderService.FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                }


                                var preCoOrder = _orderService.CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (OrderTypeEnum)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;

                                var stationId = station
                                                    .OrderBy(x => rand.Next())
                                                    .Select(x => x.Id)
                                                    .FirstOrDefault();

                                var payloadCreateOrder = new CreateOrderRequest()
                                {
                                    Id = preCoOrder.Id,
                                    OrderCode = preCoOrder.OrderCode,
                                    PartyCode = openCoOrder.Result.Data.PartyCode,
                                    TotalAmount = preCoOrder.TotalAmount,
                                    FinalAmount = preCoOrder.FinalAmount,
                                    TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                    OrderType = (OrderTypeEnum)preCoOrder.OrderType,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    StationId = stationId.ToString(),
                                    PaymentType = PaymentTypeEnum.FineWallet,
                                    IsPartyMode = true,
                                    ItemQuantity = preCoOrder.ItemQuantity,
                                    Point = preCoOrder.Point,
                                    OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
                                    {
                                        Id = detail.Id,
                                        OrderId = detail.Id,
                                        ProductInMenuId = detail.ProductInMenuId,
                                        StoreId = detail.StoreId,
                                        ProductCode = detail.ProductCode,
                                        ProductName = detail.ProductName,
                                        UnitPrice = detail.UnitPrice,
                                        Quantity = detail.Quantity,
                                        TotalAmount = detail.TotalAmount,
                                        FinalAmount = detail.FinalAmount,
                                        Note = detail.Note

                                    }).ToList(),
                                    OtherAmounts = preCoOrder.OtherAmounts
                                };
                                try
                                {
                                    var result = _staffService.CreateOrderForSimulate(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;

                                    var orderSuccess = new OrderSuccess
                                    {
                                        Id = result.Id,
                                        OrderCode = result.OrderCode,
                                        Customer = result.Customer,
                                    };
                                    response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                }
                                catch (Exception ex)
                                {
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = preCoOrder.OrderCode,
                                        Customer = preCoOrder.Customer,
                                        Status = new StatusViewModel
                                        {
                                            Message = err.Error.Message,
                                            Success = false,
                                            ErrorCode = err.Error.ErrorCode,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            catch (Exception ex)
                            {
                                var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                                if (ex is ErrorResponse errorResponse)
                                {
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = randomCustomer.Id,
                                            CustomerCode = randomCustomer.Name,
                                            Email = randomCustomer.Email,
                                            Name = randomCustomer.Name,
                                            Phone = randomCustomer.Phone,
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = err.Error.Message,
                                            Success = false,
                                            ErrorCode = err.Error.ErrorCode,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = "",
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = randomCustomer.Id,
                                            CustomerCode = randomCustomer.Name,
                                            Email = randomCustomer.Email,
                                            Name = randomCustomer.Name,
                                            Phone = randomCustomer.Phone,
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = ex.Message,
                                            Success = false,
                                            ErrorCode = 400
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            totalCoOrderSuccess--;
                        }
                        else if (totalCoOrderFailed > 0)
                        {
                            var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            if (!Utils.CheckTimeSlot(timeslot))
                            {
                                var orderFail = new OrderFailed
                                {
                                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                    Customer = new CustomerOrderResponse
                                    {
                                        Id = randomCustomer.Id,
                                        CustomerCode = randomCustomer.Name,
                                        Email = randomCustomer.Email,
                                        Name = randomCustomer.Name,
                                        Phone = randomCustomer.Phone,
                                    },
                                    Status = new StatusViewModel
                                    {
                                        Message = "Out of Time Slot!",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                            else
                            {
                                int randomError = rand.Next(1, 2);
                                if (randomError == (int)SimulateOrderFailedType.Payment)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                            CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                            Email = "",
                                            Name = "Di Na Cuteeeee",
                                            Phone = "+84838073755",
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = "Balance is not enough",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = randomCustomer.Id,
                                            CustomerCode = randomCustomer.Name,
                                            Email = randomCustomer.Email,
                                            Name = randomCustomer.Name,
                                            Phone = randomCustomer.Phone,
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = "This product is not avaliable!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            totalCoOrderFailed--;
                        }
                    }
                }
                #endregion
                return new BaseResponseViewModel<SimulateResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0,
                    },
                    Data = response
                };

            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinish(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.BoxStored)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.Finished,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.Processing)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.FinishPrepare,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToDelivering(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.FinishPrepare)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.Delivering,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToBoxStored(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.Delivering)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.BoxStored,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                    ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.DELETE, order.Id.ToString());
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
