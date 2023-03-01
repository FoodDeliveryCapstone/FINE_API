using System.ComponentModel.DataAnnotations;

namespace FINE.Service.Helpers
{
    public class Enum
    {
        //public enum AreaEnum
        //{
        // "Khu A - Khu vực trống đồng" = 1
        // "Khu B - Khu vực tiện lợi 7Element" = 2
        // "Khu C - Khu vực thư viện" = 3
        // "Khu D - Khu vực Passio" = 4
        //}

        //6h30 - 7h 
        //9h15-9h45
        //12h 12h30
        //2h45- 3h15
        public enum FcmTokenType
        {
            Customer = 1,
            Staff = 2
        }
        public enum SystemRoleEnum
        {
            [Display(Name = "Những con cáo")]
            SystemAdmin = 1,
            [Display(Name = "Quản lý cửa hàng")]
            StoreManager = 2,
            [Display(Name = "Người giao hàng")]
            Shipper = 3
        }

        public enum CampusEnum
        {
            FPT = 1,
        }
        public enum AccountTypeEnum
        {
            CreditAccount = 0,
            PointAccount = 1
        }
        public enum MembershipCardTypeEnum
        {
            Free = 0,
            Green = 1,
            Siver = 2,
            Gold = 3,
            Platinum = 4,
        }

        public enum OrderTypeEnum
        {
            AtStore = 1,
            Delivery = 2
        }

        public enum ProductStatusEnum
        {
            New = 0,
            Avaliable = 1,
            OutOfStock = 2
        }

        public enum OrderStatusEnum
        {
            //Order
            PreOrder = 1,
            PrePartyOrder = 2,
            
            PaymentPending = 3,
            Processing = 4,
            ShipperAssigned = 7,
            Delivering = 9,
            Finished = 10,

            UserCancel = 11,
        }

        public enum ShippingStatusEnum
        {
            NewOrder = 5,   
            OrderPreparing = 6,
            ReadyPickUp = 8,            
        }

        public enum StoreStatusEnum
        {
            OrderArrive = 6,
            StoreFinished = 7,

            StoreCancel = 12
        }

        public enum NotifyStatusEnum
        {
            IsNotRead = 0,
            IsNotActive = 0,
            IsAvtive = 1,
            IsRead = 1
        }

        public enum UniversityStatusEnum
        { 
            IsStudent = 1,
            IsTeacher = 2
        }

        public enum BlogPostStatusEnum
        { 
            IsNotActive = 0,
            IsActive = 1,
            IsNotDialog = 0,
            IsDialog = 1
        }
    }
}
