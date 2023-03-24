using System.ComponentModel.DataAnnotations;

namespace FINE.Service.Helpers
{
    public class ErrorEnum
    {
        //400 Bad Request
        //404 Not Found

        public enum FcmTokenErrorEnums
        {
            //404
            [Display(Name = "Invalid fcm token!")]
            INVALID_TOKEN = 4001
        }

        public enum OrderErrorEnums
        {
            //400 
            [Display(Name = "Invalid phone number")]
            INVALID_PHONE_NUMBER = 4001
            //404 
        }

        public enum CustomerErrorEnums
        {
            //400
            [Display(Name = "Invalid phone number!")]
            INVALID_PHONENUMBER = 4001,
            //404
            [Display(Name = "Not found this customer id!")]
            NOT_FOUND_ID = 4041
        }

        public enum CampusErrorEnums
        {
            //400
            [Display(Name = "This campus code already exsist!")]
            CAMPUS_CODE_EXSIST = 4001,
            //404
            [Display(Name = "Not found this campus id!")]
            NOT_FOUND_ID = 4041
        }

        public enum ProductErrorEnums
        {
            //404
            [Display(Name = "Not found this product id!")]
            NOT_FOUND_ID = 4041,

            [Display(Name = "Not found this product code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This product code already exsist!")]
            PRODUCT_CODE_EXSIST = 4001,
        }

        public enum StoreErrorEnums
        {
            //404
            [Display(Name = "Not found this store id!")]
            NOT_FOUND_ID = 4041,

            [Display(Name = "Not found this store code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This store code already exsist!")]
            CODE_EXSIST = 4001,
        }

        public enum AreaErrorEnums
        {
            //404
            [Display(Name = "Not found this area Id!")]
            NOT_FOUND_ID = 4041,

            [Display(Name = "Not found this area code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This area code already exsist!")]
            CODE_EXSIST = 4001,
        }

        public enum UnversityInfoErrorEnums
        {
            //404
            [Display(Name = "University Infor Id not found")]
            NOT_FOUND_ID = 4041,

            //400
            [Display(Name = "This University Infor already exsist!")]
            UNIVERSITYINFO_EXSIST = 4001,
        }
        public enum TimeSlotErrorEnums
        {
            //400
            [Display(Name = "Out of Time Slot!")]
            OUT_OF_TIMESLOT = 4001,
            //404
            [Display(Name = "Not found this time slot Id!")]
            NOT_FOUND_ID = 4041,
        }

        public enum ProductCollectionTimeSlotErrorEnums
        {
            //404
            [Display(Name = "Not found this product collection time slot Id!")]
            NOT_FOUND_ID = 4041,
        }

        public enum SystemCategoryErrorEnums
        {
            //404
            [Display(Name = "Not found this system category Id!")]
            NOT_FOUND_ID = 4041,

            [Display(Name = "Not found this system category code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This system category code already exsist!")]
            CODE_EXSIST = 4001,
        }

        public enum NotifyErrorEnum
        {
            //404
            [Display(Name = "Not found this notify Id!")]
            NOT_FOUND_ID = 4041,

            //400
            [Display(Name = "This notify already exsist!")]
            NOTIFY_EXSIST = 4001,
        }

        public enum StaffReportErrorEnum
        {
            //404
            [Display(Name = "Not found Staff report base on this staff Id!")]
            NOT_FOUND_ID = 4041,

            //400
            [Display(Name = "This Staff Report already exsist!")]
            STAFF_REPORT_EXSIST = 4001,
        }


        public enum StoreCategoryErrorEnums
        {
            //404
            [Display(Name = "Not found this store category Id!")]
            NOT_FOUND_ID = 4041,
        }

        public enum ProductCollectionErrorEnums
        {
            //404
            [Display(Name = "Not found this product collection Id!")]
            NOT_FOUND_ID = 4041,

        }

        public enum ProductCollectionItemErrorEnums
        {
            //404
            [Display(Name = "Not found this product collection item Id!")]
            NOT_FOUND_ID = 4041,

        }


        public enum StaffErrorEnum
        {
            //404
            [Display(Name = "Staff ID not found!")]
            NOT_FOUND_ID = 4041,
            [Display(Name = "Staff need to have customer account first!")]
            NOT_FOUND_CUSTOMER_ID = 4042,

            //400
            [Display(Name = "This staff already exsist!")]
            STAFF_EXSIST = 4001,
            [Display(Name = "Username or password is not correct")]
            LOGIN_FAIL = 4002,
        }

        public enum BlogPostErrorEnums
        {
            //404
            [Display(Name = "Not found this Blog Post Id!")]
            NOT_FOUND_ID = 4041,


            //400
            [Display(Name = "This Blog Post already exsist!")]
            BLOGPOST_EXSIST = 4001,
        }

        public enum UniversityErrorenums
        {
            //404
            [Display(Name = "Not found this university Id!")]
            NOT_FOUND_ID = 4041,

            [Display(Name = "Not found this university code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This university code already exsist!")]
            CODE_EXSIST = 4001,
        }

        public enum MenuErrorEnums
        {
            //404
            [Display(Name = "Not found this menu id!")]
            NOT_FOUND_ID = 4041,
            [Display(Name = "Not found this menu !")]
            NOT_FOUND = 4042
        }

        public enum FloorErrorEnums
        {
            //404
            [Display(Name = "Not found this floor Id!")]
            NOT_FOUND_ID = 4041,
        }

        public enum ProductInMenuErrorEnums
        {
            //404
            [Display(Name = "Not found this product in menu Id!")]
            NOT_FOUND_ID = 4041,
        }

        public enum ProductBestSellerErrorEnums
        {
            //404
            [Display(Name = "Not found any Orders!")]
            NOT_FOUND_ORDER = 4041,

            [Display(Name = "Not found any Orders have similar products!")]
            SIMILAR_PRODUCT_NOT_FOUND = 4042,
        }
    }
}
