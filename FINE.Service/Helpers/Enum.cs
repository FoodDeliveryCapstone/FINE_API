using System.ComponentModel.DataAnnotations;

namespace FINE.Service.Helpers
{
    public class Enum
    {
        public enum FcmTokenType
        {
            Customer = 1,
            Staff = 2
        }

        public enum SystemRoleTypeEnum
        {
            [Display(Name = "Những con cáo")]
            SystemAdmin = 1,
            [Display(Name = "Quản lý cửa hàng")]
            StoreManager = 2,
            [Display(Name = "Người giao hàng")]
            Shipper = 3
        }

        public enum DestinationTypeEnum
        {
            FPT = 1,
        }

        public enum AccountTypeEnum
        {
            CreditAccount = 1,
            PointAccount = 2
        }

        public enum OrderTypeEnum
        {
            Delivery = 1
        }

        public enum OtherAmountTypeEnum
        {
            ShippingFee = 1,
            Discount = 2
        }

        public enum UpdateOrderTypeEnum
        {
            UserCancel = 1,
            FinishOrder = 2,
            UpdateDetails = 3
        }

        public enum ProductInMenuStatusEnum
        {
            Wait =1,
            Avaliable = 2,
            OutOfStock = 3
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

        public enum PartyOrderStatus
        {
            NotConfirm = 1,
            Confirm = 2
        }

        public enum PaymentTypeEnum
        {
            FineWallet = 1,
            VnPay = 2,
        }

        public enum PaymentStatusEnum
        {
            Finish = 1,
            Processing = 2,
            Fail = 3
        }

        public enum TransactionStatusEnum
        {
            Processing = 1,
            Finish = 2
        }

        public enum TransactionTypeEnum
        {
            Recharge = 1,
            Payment = 2
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
            IsNotRead = 1,
            IsNotActive = 1,
            IsAvtive = 2,
            IsRead = 2
        }
        public enum NotifyTypeEnum
        {
            ForOrder = 1,
            ForGift = 2,
        }
    }
}