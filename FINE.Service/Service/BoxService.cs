//using AutoMapper;
//using FINE.Data.Entity;
//using FINE.Data.UnitOfWork;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Box;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static FINE.Service.Helpers.ErrorEnum;

//namespace FINE.Service.Service
//{
//    public interface IBoxService
//    {
//        Task<BaseResponseViewModel<BoxResponse>> CheckBoxCode(CheckBoxCodeRequest request);

//    }

//    public class BoxService : IBoxService
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IMapper _mapper;
//        public BoxService(IUnitOfWork unitOfWork, IMapper mapper)
//        {
//            _mapper = mapper;
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<BaseResponseViewModel<BoxResponse>> CheckBoxCode(CheckBoxCodeRequest request)
//        {
//            try
//            {
//                var box = _unitOfWork.Repository<Box>().GetAll()
//                                .FirstOrDefault(x => x.Code == request.Code);
//                if (box == null)
//                    throw new ErrorResponse(404, (int)BoxErrorEnums.NOT_FOUND,
//                       BoxErrorEnums.NOT_FOUND.GetDisplayName());

//                return new BaseResponseViewModel<BoxResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    },
//                    Data = _mapper.Map<BoxResponse>(box)
//                };
//            }
//            catch (ErrorResponse ex)
//            {
//                throw ex;
//            }
//        }
//    }
//}
