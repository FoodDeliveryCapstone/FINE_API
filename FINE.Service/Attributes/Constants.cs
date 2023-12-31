﻿namespace FINE.Service.Attributes
{
    public class Constants
    {
        public const int DefaultPaging = 50;
        public const int LimitPaging = 500;

        public const string NOTIFICATION_TOPIC = "order_reminder";
        public const string NOTIFICATION_INVITATION_TITLE = "Bạn có lời mời gia nhập tổ đội ăn sập sàn";

        public const string SUC_ORDER_CREATED = "Chòi oi đặt thành công rồi nè !!!";
        public const string VNPAY_PAYMENT_FAIL = "Thanh toán VnPay không thành công";
        public const string VNPAY_PAYMENT_SUCC = "Thanh toán VnPay thành công";
        public const string PARTYORDER_LINKED = "LPO";
        public const string PARTYORDER_COLAB = "CPO";

        public const string CHANGE_ADMIN_PARTY = "Bạn lên chức tổ trưởng ròi nè!!!";
        public const string OUT_OF_LIMIT_ORDER = "Bạn đã hết lượt đặt đơn!!!";
        public const string REPORT_ERROR_PACK = "Không ổn rồi!!!";
    }
}