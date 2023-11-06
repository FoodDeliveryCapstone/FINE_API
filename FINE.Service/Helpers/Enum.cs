using System.ComponentModel.DataAnnotations;

namespace FINE.Service.Helpers
{
    public class Enum
    {
        public enum RedisDbEnum
        {
            [Display(Name = "CoOrder")]
            CoOrder = 1,

            [Display(Name = "Staff")]
            Staff = 2,

            [Display(Name = "Order")]
            OrderOperation = 3,

            [Display(Name = "Shipper")]
            Shipper = 4,

            [Display(Name = "Station")]
            Station = 5,

            [Display(Name = "Box")]
            Box = 6,
        }

        public enum QRCodeRole
        {
            Shipper = 1,
            User = 2,
            StaffStation = 3
        }

        public enum RedisSetUpType
        {
            GET = 1,
            SET = 2,
            DELETE = 3
        }

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

        public enum AccountTypeEnum
        {
            CreditAccount = 1,
            PointAccount = 2
        }

        public enum OrderTypeEnum
        {
            OrderToday = 1,
            OrderLater = 2
        }

        public enum OtherAmountTypeEnum
        {
            ShippingFee = 1,
            Discount = 2,
            Refund = 3
        }

        public enum ProductRotationTypeEnum
        {
            Vertical = 1,
            Horizontal = 2,
            Both = 3
        }

        public enum ProductInMenuStatusEnum
        {
            Wait = 1,
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
            FinishPrepare = 6,
            ShipperAssigned = 7,
            Delivering = 9,
            BoxStored = 10,
            Finished = 11,

            UserCancel = 12,
        }

        public enum OrderBoxStatusEnum
        {
            NotPicked = 1,
            Picked = 2,
            AboutToExpire = 3,

            StaffPicked = 4,
            LockBox = 5
        }

        public enum PartyOrderStatus
        {
            NotConfirm = 1,
            Confirm = 2,

            CloseParty = 3,

            NotRefund = 4,
            FinishRefund = 5,

            OutOfTimeslot = 6
        }

        public enum PartyOrderType
        {
            CoOrder = 1,
            LinkedOrder = 2
        }

        public enum PaymentTypeEnum
        {
            FineWallet = 1,
            VnPay = 2,
        }

        public enum TransactionStatusEnum
        {
            Processing = 1,
            Finish = 2,
            Fail = 3
        }

        public enum TransactionTypeEnum
        {
            Recharge = 1,
            Payment = 2
        }

        public enum NotifyTypeEnum
        {
            ForUsual = 1,
            ForInvitation = 2,
            ForPopup = 3,
            ForRefund = 4,
            ForFinishOrder = 5
        }

        public enum PackageUpdateTypeEnum
        {
            Confirm = 1,
            Error = 2,

            ReConfirm = 3
        }

        public enum LockBoxUpdateTypeEnum
        {
            Delete = 1,
            Change = 2
        }
    }
}
