﻿using System.ComponentModel.DataAnnotations;

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
            INVALID_PHONE_NUMBER = 4001,
            [Display(Name = "Cannot cancel order")]
            CANNOT_CANCEL_ORDER = 4002,
            [Display(Name = "Cannot finish order right now")]
            CANNOT_FINISH_ORDER = 4003,
            [Display(Name = "Cannot update order right now")]
            CANNOT_UPDATE_ORDER = 4004,
            [Display(Name = "This product cannot add into the box")]
            CANNOT_ADD_TO_CARD = 4005,
            [Display(Name = "Out of limit order")]
            OUT_OF_LIMIT_ORDER = 4006,
            //404 
            [Display(Name = "Not found this order!")]
            NOT_FOUND = 4041,
            [Display(Name = "Not found this coOrder!")]
            NOT_FOUND_COORDER = 4042
        }

        public enum BoxErrorEnums
        {
            //400 
            [Display(Name = "Order already taken!")]
            ORDER_TAKEN = 4001,
            [Display(Name = "Order already taken by staff!")]
            STAFF_TAKEN = 4002,
            [Display(Name = "Box is not available!")]
            BOX_NOT_AVAILABLE = 4003,
            [Display(Name = "This box code already exsist!")]
            CODE_EXIST = 4004,

            //404
            [Display(Name = "Not found this box!")]
            NOT_FOUND = 4041,
            [Display(Name = "Not found order in this box!")]
            NOT_FOUND_ORDERBOX = 4042,
            [Display(Name = "Error!")]
            ORDER_BOX_ERROR = 4003,
        }

        public enum PartyErrorEnums
        {
            //400 
            [Display(Name = "Invalid party code")]
            INVALID_CODE = 4001,
            [Display(Name = "Party is closed")]
            PARTY_CLOSED = 4002,
            [Display(Name = "You already in another CoOrder party")]
            COORDER_PARTY_JOINED = 4003,
            [Display(Name = "You already in another Linked party")]
            LINKED_PARTY_JOINED = 4004,
            [Display(Name = "Party order is out of timeslot!")]
            OUT_OF_TIMESLOT = 4005,
            [Display(Name = "Party is deleted")]
            PARTY_DELETE = 4006,
            [Display(Name = "Join wrong timeslot")]
            WRONG_TIMESLOT = 4007,
            [Display(Name = "You already in this party")]
            PARTY_JOINED = 4008,
            [Display(Name = "You are out this party")]
            OUT_OF_PARTY = 4009,
        }

        public enum PaymentErrorsEnum
        {
            //400 
            [Display(Name = "Invalid payment type")]
            INVALID_PAYMENT_TYPE = 4001,
            [Display(Name = "Balance is not enough")]
            ERROR_BALANCE = 4002,
        }

        public enum TransactionErrorEnum
        {
            //400 
            [Display(Name = "Create transaction fail")]
            CREATE_TRANS_FAIL = 4001,
        }

        public enum CustomerErrorEnums
        {
            //400
            [Display(Name = "Invalid phone number!")]
            INVALID_PHONENUMBER = 4001,
            [Display(Name = "Customer needs to update phone number!")]
            MISSING_PHONENUMBER = 4002,
            //404
            [Display(Name = "Not found this customer!")]
            NOT_FOUND = 4041
        }

        public enum DestinationErrorEnums
        {
            //400
            [Display(Name = "This Destination code already exsist!")]
            Destination_CODE_EXIST = 4001,
            //404
            [Display(Name = "Not found this destination!")]
            NOT_FOUND = 4041
        }

        public enum ProductErrorEnums
        {
            //404
            [Display(Name = "Not found this product!")]
            NOT_FOUND = 4041,

            [Display(Name = "Not found this product code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This product code already exsist!")]
            PRODUCT_CODE_EXIST = 4001,
        }

        public enum StoreErrorEnums
        {
            //404
            [Display(Name = "Not found this store!")]
            NOT_FOUND = 4041,

            [Display(Name = "Not found this store code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This store code already exsist!")]
            CODE_EXIST = 4001,
        }

        public enum AreaErrorEnums
        {
            //404
            [Display(Name = "Not found this area!")]
            NOT_FOUND = 4041,

            [Display(Name = "Not found this area code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This area code already exsist!")]
            CODE_EXIST = 4001,
        }

        public enum StationErrorEnums
        {
            //404
            [Display(Name = "Not found this station!")]
            NOT_FOUND = 4041,

            //400
            [Display(Name = "Station is unavailable!")]
            UNAVAILABLE = 4001,
            [Display(Name = "This station code already exsist!")]
            CODE_EXIST = 4002,
        }

        public enum TimeSlotErrorEnums
        {
            //400
            [Display(Name = "Out of Time Slot!")]
            OUT_OF_TIMESLOT = 4001,
            [Display(Name = "Time Slot is unavailiable!")]
            TIMESLOT_UNAVAILIABLE = 4001,
            //404
            [Display(Name = "Not found this time slot!")]
            NOT_FOUND = 4041,
        }

        public enum NotifyErrorEnum
        {
            //404
            [Display(Name = "Not found this notify!")]
            NOT_FOUND = 4041,

            //400
            [Display(Name = "This notify already exsist!")]
            NOTIFY_EXSIST = 4001,
            [Display(Name = "Data is null!")]
            DATA_NULL = 4002,

        }

        public enum StaffErrorEnum
        {
            //404
            [Display(Name = "Staff not found!")]
            NOT_FOUND = 4041,

            //400
            [Display(Name = "This staff already exsist!")]
            STAFF_EXSIST = 4001,
            [Display(Name = "Username or password is not correct")]
            LOGIN_FAIL = 4002,
        }

        public enum MenuErrorEnums
        {
            //404
            [Display(Name = "Not found this menu!")]
            NOT_FOUND = 4041,
            [Display(Name = "Not found menu in this timeslot!")]
            NOT_FOUND_MENU_IN_TIMESLOT = 4042
        }

        public enum FloorErrorEnums
        {
            //404
            [Display(Name = "Not found this floor!")]
            NOT_FOUND = 4041,
        }

        public enum ProductInMenuErrorEnums
        {
            //404
            [Display(Name = "Not found this product in menu!")]
            NOT_FOUND = 4041,

            //400
            [Display(Name = "This product already in this menu!")]
            PRODUCT_ALREADY_IN_MENU = 4001,
            [Display(Name = "This product is not avaliable!")]
            PRODUCT_NOT_AVALIABLE = 4002
        }

        public enum OrderBoxErrorEnums
        {
            //400
            [Display(Name = "This order already in box!")]
            ORDER_BOX_EXISTED = 4001,
        }

        public enum ProductAttributeErrorEnums
        {
            //404
            [Display(Name = "Not found this product attribute!")]
            NOT_FOUND = 4041,

            [Display(Name = "Not found this product attribute code!")]
            NOT_FOUND_CODE = 4042,

            //400
            [Display(Name = "This product attribute code already exsist!")]
            PRODUCT_CODE_EXIST = 4001,
        }
    }
}
