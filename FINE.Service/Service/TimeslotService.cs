using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ITimeslotService
    {
        Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging);
    }

    public class TimeslotService : ITimeslotService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TimeslotService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging)
        {
            try
            {
                #region check Destination exist
                var destination = _unitOfWork.Repository<Destination>().GetAll()
                              .FirstOrDefault(x => x.Id == Guid.Parse(destinationId));
                if (destination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());
                #endregion

                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                         .Where(x => x.DestinationId == destination.Id)
                                         .ProjectTo<TimeslotResponse>(_mapper.ConfigurationProvider)
                                         .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<TimeslotResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = timeslot.Item1
                    },
                    Data = timeslot.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
