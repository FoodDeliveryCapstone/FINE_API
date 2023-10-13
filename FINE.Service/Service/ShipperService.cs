using AutoMapper;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FINE.Data.UnitOfWork;
using FINE.Service.Helpers;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using FINE.Service.DTO.Request.Shipper;
using Azure;
using FINE.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace FINE.Service.Service
{
    public interface IShipperService
    {

    }

    public class ShipperService : IShipperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ShipperService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

    }
}
