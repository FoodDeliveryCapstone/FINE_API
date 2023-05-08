using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Castle.Core.Resource;
using System.Net.Mail;
using IronBarCode;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrderByCustomerId(int customerId, PagingRequest paging);
        Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(int customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(int customerId, CreateGenOrderRequest request);
        Task<BaseResponseViewModel<GenOrderResponse>> CancelOrder(int orderId);
        Task<BaseResponseViewModel<GenOrderResponse>> UpdateOrder(int orderId);
    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async static Task<string> getHtml(Order genOrder)
        {
            try
            {
                string head = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\">\r\n\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta content=\"width=device-width, initial-scale=1\" name=\"viewport\">\r\n    <meta name=\"x-apple-disable-message-reformatting\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta content=\"telephone=no\" name=\"format-detection\">\r\n    <title></title>\r\n    <!--[if (mso 16)]>\r\n    <style type=\"text/css\">\r\n    a {text-decoration: none;}\r\n    </style>\r\n    <![endif]-->\r\n    <!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]-->\r\n    <!--[if gte mso 9]>\r\n<xml>\r\n    <o:OfficeDocumentSettings>\r\n    <o:AllowPNG></o:AllowPNG>\r\n    <o:PixelsPerInch>96</o:PixelsPerInch>\r\n    </o:OfficeDocumentSettings>\r\n</xml>\r\n<![endif]-->\r\n    <!--[if !mso]><!-- -->\r\n    <link href=\"https://fonts.googleapis.com/css?family=Lora:400,400i,700,700i\" rel=\"stylesheet\">\r\n    <link href=\"https://fonts.googleapis.com/css?family=Playfair+Display:400,400i,700,700i\" rel=\"stylesheet\">\r\n    <!--<![endif]-->\r\n</head>\r\n\r\n<body>\r\n    <div class=\"es-wrapper-color\">\r\n        <!--[if gte mso 9]>\r\n\t\t\t<v:background xmlns:v=\"urn:schemas-microsoft-com:vml\" fill=\"t\">\r\n\t\t\t\t<v:fill type=\"tile\" color=\"#ffffff\"></v:fill>\r\n\t\t\t</v:background>\r\n\t\t<![endif]-->\r\n        <table class=\"es-wrapper\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">\r\n            <tbody>\r\n                <tr>\r\n                    <td class=\"esd-email-paddings\" valign=\"top\">\r\n                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"esd-header-popover es-header\" align=\"center\">\r\n                            <tbody>\r\n                                <tr>\r\n                                    <td class=\"esd-stripe\" align=\"center\">\r\n                                        <table class=\"es-header-body\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"560\" style=\"background-color: transparent;\">\r\n                                            <tbody>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p20t es-p5b es-p20r es-p20l\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"520\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" bgcolor=\"#ffffff\" style=\"background-color: #ffffff;\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-m-txt-c es-p20\">\r\n                                                                                        <p style=\"color: #238e9c; font-size: 40px; font-family: 'comic sans ms', 'marker felt-thin', arial, sans-serif;\"><strong>F.I.N.E</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p10b es-p20r es-p20l\" align=\"left\">\r\n                                                        <table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td class=\"es-m-p0r es-m-p20b esd-container-frame\" width=\"520\" valign=\"top\" align=\"center\">\r\n                                                                        <table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td class=\"esd-block-menu\" esd-tmp-menu-color=\"#333333\" esd-tmp-divider=\"0|solid|#333333\" esd-tmp-menu-font-size=\"12px\" esd-tmp-menu-padding=\"15|15\">\r\n                                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" class=\"es-menu\">\r\n                                                                                            <tbody>\r\n                                                                                                <tr class=\"links\">\r\n                                                                                                    <td align=\"center\" valign=\"top\" width=\"25%\" class=\"es-p10t es-p10b es-p5r es-p5l\" style=\"padding-top: 15px; padding-bottom: 15px;\"><a target=\"_blank\" href=\"https://viewstripo.email\" style=\"color: #238e9c; font-size: 12px;\">Home</a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\" width=\"25%\" class=\"es-p10t es-p10b es-p5r es-p5l\" style=\"padding-top: 15px; padding-bottom: 15px;\"><a target=\"_blank\" href=\"https://viewstripo.email\" style=\"color: #238e9c; font-size: 12px;\">Blog</a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\" width=\"25%\" class=\"es-p10t es-p10b es-p5r es-p5l\" style=\"padding-top: 15px; padding-bottom: 15px;\"><a target=\"_blank\" href=\"https://viewstripo.email\" style=\"color: #238e9c; font-size: 12px;\">Download</a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\" width=\"25%\" class=\"es-p10t es-p10b es-p5r es-p5l\" style=\"padding-top: 15px; padding-bottom: 15px;\"><a target=\"_blank\" href=\"https://viewstripo.email\" style=\"color: #238e9c; font-size: 12px;\">About Us</a></td>\r\n                                                                                                </tr>\r\n                                                                                            </tbody>\r\n                                                                                        </table>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p20\" align=\"left\">\r\n                                                        <table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td class=\"es-m-p0r es-m-p20b esd-container-frame\" width=\"520\" valign=\"top\" align=\"center\">\r\n                                                                        <table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p5b\">\r\n                                                                                        <h1 style=\"color: #238e9c; font-family: 'comic sans ms', 'marker felt-thin', arial, sans-serif;\">Thank you for your&nbsp;order&nbsp;!</h1>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-spacer es-p10t es-p10b es-p20r es-p20l\" style=\"font-size:0\">\r\n                                                                                        <table border=\"0\" width=\"30%\" height=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"display: inline-table; width: 30% !important;\">\r\n                                                                                            <tbody>\r\n                                                                                                <tr>\r\n                                                                                                    <td style=\"border-bottom: 1px solid #238e9c; background: none; height: 1px; width: 100%; margin: 0px;\"></td>\r\n                                                                                                </tr>\r\n                                                                                            </tbody>\r\n                                                                                        </table>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text\">\r\n                                                                                        <p style=\"font-size: 20px; font-family: 'comic sans ms', 'marker felt-thin', arial, sans-serif;\">FINE chúc bạn ngon miệng</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                            </tbody>\r\n                                        </table>\r\n                                    </td>\r\n                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-content\" align=\"center\">\r\n                            <tbody>\r\n                                <tr>\r\n                                    <td class=\"esd-stripe\" align=\"center\">\r\n                                        <table bgcolor=\"#ffffff\" class=\"es-content-body\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"560\">\r\n                                            <tbody>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p20r es-p20l\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"520\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-spacer\" height=\"0\"></td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                            </tbody>\r\n                                        </table>\r\n                                    </td>\r\n                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-content\" align=\"center\">\r\n                            <tbody>\r\n                                <tr>\r\n                                    <td class=\"esd-stripe\" align=\"center\" style=\"background-image: url(https://firebasestorage.googleapis.com/v0/b/finedelivery-880b6.appspot.com/o/bgLandingPage.jpg?alt=media&token=b5bc6b0e-eed6-4a7e-8a3f-df0ae4edef7f); background-repeat: repeat; background-position: left top;\" background=\"https://firebasestorage.googleapis.com/v0/b/finedelivery-880b6.appspot.com/o/bgLandingPage.jpg?alt=media&token=b5bc6b0e-eed6-4a7e-8a3f-df0ae4edef7f\">\r\n                                        <table bgcolor=\"#ffffff\" class=\"es-content-body\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"560\" style=\"border-width: 1px; border-style: solid; border-color: #999999;\">\r\n                                            <tbody>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p10t\" bgcolor=\"#FFFFFF\">\r\n                                                                                        <p style=\"font-size: 20px; color: #333333; font-family: lora, georgia, 'times new roman', serif;\"><b>Đưa mã cho Staff&nbsp;để nhận món nha&nbsp;!!!</b></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>"
                            + "                  < tr >\r\n                                                    < td class=\"esd-structure es-p20\" align=\"left\" bgcolor=\"#FFFFFF\" style=\"background-color: #ffffff;\">\r\n                                                        <table cellpadding = \"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width = \"518\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding = \"0\" cellspacing=\"0\" width=\"100%\" style=\"border-left:3px solid #157380;border-right:3px solid #157380;border-top:3px solid #157380;border-bottom:3px solid #157380;border-radius: 15px; border-collapse: separate;\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align = \"center\" class=\"esd-block-image es-p10\" style=\"font-size: 0px;\"><a target = \"_blank\"><img class=\"adapt-img\" src=cid:MyPic alt style = \"display: block;\" width=\"222\"></a></td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>"
                            + "<tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\" bgcolor=\"#157380\" style=\"background-color: #157380;\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p5\">\r\n                                                                                        <p style=\"font-family: lora, georgia, 'times new roman', serif; font-size: 24px; color: #ffffff;\"><strong>Chi tiết đơn hàng</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>";

                string content = "";
                foreach (var order in genOrder.InverseGeneralOrder)
                {
                    content += $"<tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text\">\r\n                                                                                        <h1 style=\"font-family: lora, georgia, &quot;times new roman&quot;, serif; font-size: 20px; color: #333333;\">{order.Store.StoreName}</h1>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p10t es-p10b es-p15r es-p15l\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"528\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"255\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"255\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 17px;\"><strong>Mã đơn hàng: {order.OrderCode}</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"253\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"253\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 17px;\"><strong>Ngày đặt hàng: {order.CheckInDate}</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>";
                    foreach (var orderDetail in order.OrderDetails)
                    {
                        content += $"<tr>\r\n                                                    <td class=\"esd-structure es-p20\" align=\"left\" bgcolor=\"#FFFFFF\" style=\"background-color: #ffffff;\">\r\n                                                        <!--[if mso]><table width=\"518\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"204\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"184\" class=\"es-m-p0r es-m-p20b esd-container-frame\" align=\"center\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-image\" style=\"font-size:0\"><a target=\"_blank\"><img class=\"adapt-img esdev-empty-img\" src=\"{orderDetail.ProductInMenu.Product.ImageUrl}\" alt width=\"100%\" height=\"100\" style=\"display: none;\"></a></td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                    <td class=\"es-hidden\" width=\"20\"></td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"118\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"98\" class=\"es-m-p20b esd-container-frame\" align=\"center\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p35t\">\r\n                                                                                        <p style=\"line-height: 150%; color: #333333; font-family: lora, georgia, 'times new roman', serif;\">{orderDetail.ProductName}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                    <td class=\"es-hidden\" width=\"20\"></td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"65\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"65\" class=\"es-m-p20b esd-container-frame\" align=\"center\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p35t\">\r\n                                                                                        <p style=\"color: #333333; font-family: lora, georgia, 'times new roman', serif;\">{orderDetail.Quantity}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"111\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"111\" align=\"center\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p35t\">\r\n                                                                                        <p style=\"color: #333333; font-family: lora, georgia, 'times new roman', serif;\">{orderDetail.TotalAmount}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>";               
                    }
                    content += $" <tr>\r\n                                                    <td class=\"esd-structure es-p5t es-p5b\" align=\"left\" style=\"border-radius: 5px 5px 0px 0px;\">\r\n                                                        <!--[if mso]><table width=\"558\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p10l\">\r\n                                                                                        <p style=\"color: #333333; font-size: 18px; font-family: lora, georgia, 'times new roman', serif;\">Tổng:</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text\">\r\n                                                                                        <p style=\"font-size: 18px; font-family: lora, georgia, 'times new roman', serif;\"><strong>{order.TotalAmount}</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>";
                }
                content += $"<tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text\" bgcolor=\"#238e9c\">\r\n                                                                                        <p style=\"line-height: 100%;\"><br></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"558\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b es-p10l\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Tổng:</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b\">\r\n                                                                                        <p style=\"font-size: 18px; font-family: lora, georgia, 'times new roman', serif;\">{genOrder.TotalAmount}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"558\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b es-p10l\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Khuyến mãi:</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b\">\r\n                                                                                        <p style=\"font-size: 18px; font-family: lora, georgia, 'times new roman', serif;\">{genOrder.Discount}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"558\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b es-p10l\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Tổng phí ship:</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b\">\r\n                                                                                        <p style=\"font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">{genOrder.ShippingFee}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"558\" cellpadding=\"0\" cellspacing=\"0\"><tr><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" class=\"es-m-p20b esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b es-p10l\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\"><strong>Tổng cộng:</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"269\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"269\" align=\"left\" class=\"esd-container-frame\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"left\" class=\"esd-block-text es-p5b\">\r\n                                                                                        <p style=\"font-size: 18px; font-family: lora, georgia, 'times new roman', serif;\"><strong>{genOrder.FinalAmount}</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"border-left:2px solid #157380;border-right:2px solid #157380;border-top:2px solid #157380;border-bottom:2px solid #157380;border-radius: 5px; border-collapse: separate;\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td class=\"esd-block-text es-p10t es-p10b es-p10l\" align=\"left\">\r\n                                                                                        <p style=\"line-height: 150%; color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Khách hàng: {genOrder.Customer.Name}</p>\r\n                                                                                        <p style=\"line-height: 150%; color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Số điện thoại: {genOrder.DeliveryPhone}</p>\r\n                                                                                        <p style=\"line-height: 150%; color: #238e9c; font-family: lora, georgia, 'times new roman', serif; font-size: 18px;\">Địa chỉ: {genOrder.Room.RoomNumber}</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"558\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-image\" style=\"font-size: 0px;\"><a target=\"_blank\" href><img class=\"adapt-img\" src=\"https://dfrchf.stripocdn.email/content/guids/CABINET_f49208be225fae6d848f28d5c4491021059b98c7cb60539ceba8be58e3ba9c61/images/freebie__printable_thank_you_card.jpg\" alt style=\"display: block;\" height=\"361\"></a></td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                            </tbody>\r\n                                        </table>\r\n                                    </td>\r\n                                </tr>\r\n                            </tbody>\r\n                        </table>";
                string foot = "<!--[if !mso]><!-- -->\r\n                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-footer es-desk-hidden\" align=\"center\">\r\n                            <tbody>\r\n                                <tr>\r\n                                    <td class=\"esd-stripe\" align=\"center\">\r\n                                        <table class=\"es-footer-body\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"560\" style=\"background-color: transparent;\">\r\n                                            <tbody>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"560\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p15t\">\r\n                                                                                        <p style=\"color: #238e9c; font-family: 'comic sans ms', 'marker felt-thin', arial, sans-serif; font-size: 25px;\"><strong>Contact with us</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p30t es-p30b es-p20r es-p20l\" align=\"left\">\r\n                                                        <!--[if mso]><table width=\"520\" cellpadding=\"0\" \r\n                        cellspacing=\"0\"><tr><td width=\"250\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-left\" align=\"left\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"250\" align=\"left\" class=\"esd-container-frame es-m-p20b\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-image\" style=\"font-size: 0px;\"><a target=\"_blank\" href=\"https://viewstripo.email\"><img class=\"adapt-img\" src=\"https://firebasestorage.googleapis.com/v0/b/finedelivery-880b6.appspot.com/o/logo%20final-01%201.png?alt=media&token=e949eaa9-b1f2-45c9-bafe-8ed8c40e3271\" alt=\"Eid Mubarak\" style=\"display: block;\" width=\"190\" title=\"Eid Mubarak\"></a></td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td><td width=\"20\"></td><td width=\"250\" valign=\"top\"><![endif]-->\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-right\" align=\"right\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"250\" class=\"esd-container-frame\" align=\"left\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text\">\r\n                                                                                        <p style=\"font-size: 24px; color: #157380;\"><strong>Smjle Team</strong></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-p10t es-p10b\" esd-links-color=\"#157380\">\r\n                                                                                        <p style=\"line-height: 150%; color: #157380;\">Home</p>\r\n                                                                                        <p style=\"line-height: 150%; color: #157380;\"><a href=\"https://viewstripo.email\" style=\"color: #157380;\" target=\"_blank\">About Us</a></p>\r\n                                                                                        <p style=\"line-height: 150%; color: #157380;\"><a href=\"https://viewstripo.email\" style=\"color: #157380;\" target=\"_blank\">Contact Us</a></p>\r\n                                                                                        <p style=\"line-height: 150%; color: #157380;\">Download App</p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-social\" style=\"font-size:0\">\r\n                                                                                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-table-not-adapt es-social\">\r\n                                                                                            <tbody>\r\n                                                                                                <tr>\r\n                                                                                                    <td align=\"center\" valign=\"top\" class=\"es-p10r\"><a target=\"_blank\" href=\"https://viewstripo.email\"><img title=\"Facebook\" src=\"https://dfrchf.stripocdn.email/content/assets/img/social-icons/logo-black/facebook-logo-black.png\" alt=\"Fb\" width=\"24\" height=\"24\"></a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\" class=\"es-p10r\"><a target=\"_blank\" href=\"https://viewstripo.email\"><img title=\"Twitter\" src=\"https://dfrchf.stripocdn.email/content/assets/img/social-icons/logo-black/twitter-logo-black.png\" alt=\"Tw\" width=\"24\" height=\"24\"></a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\" class=\"es-p10r\"><a target=\"_blank\" href=\"https://viewstripo.email\"><img title=\"Instagram\" src=\"https://dfrchf.stripocdn.email/content/assets/img/social-icons/logo-black/instagram-logo-black.png\" alt=\"Inst\" width=\"24\" height=\"24\"></a></td>\r\n                                                                                                    <td align=\"center\" valign=\"top\"><a target=\"_blank\" href=\"https://viewstripo.email\"><img title=\"Youtube\" src=\"https://dfrchf.stripocdn.email/content/assets/img/social-icons/logo-black/youtube-logo-black.png\" alt=\"Yt\" width=\"24\" height=\"24\"></a></td>\r\n                                                                                                </tr>\r\n                                                                                            </tbody>\r\n                                                                                        </table>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                        <!--[if mso]></td></tr></table><![endif]-->\r\n                                                    </td>\r\n                                                </tr>\r\n                                            </tbody>\r\n                                        </table>\r\n                                    </td>\r\n                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                        <!--<![endif]-->\r\n                        <table cellpadding=\"0\" cellspacing=\"0\" class=\"es-content esd-footer-popover\" align=\"center\">\r\n                            <tbody>\r\n                                <tr>\r\n                                    <td class=\"esd-stripe\" align=\"center\">\r\n                                        <table class=\"es-content-body\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"560\" style=\"background-color: transparent;\">\r\n                                            <tbody>\r\n                                                <tr>\r\n                                                    <td class=\"esd-structure es-p20\" align=\"left\">\r\n                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                            <tbody>\r\n                                                                <tr>\r\n                                                                    <td width=\"520\" class=\"esd-container-frame\" align=\"center\" valign=\"top\">\r\n                                                                        <table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">\r\n                                                                            <tbody>\r\n                                                                                <tr>\r\n                                                                                    <td align=\"center\" class=\"esd-block-text es-infoblock\" esd-links-color=\"#999999\">\r\n                                                                                        <p style=\"line-height: 150%; color: #999999;\">You are receiving this email because you have visited our site or asked us about the regular newsletter. Make sure our messages get to your Inbox (and not your bulk or junk folders).<br><a target=\"_blank\" style=\"line-height: 150%; color: #999999;\" href=\"https://viewstripo.email\">Privacy police</a> | <a target=\"_blank\" style=\"line-height: 150%; color: #999999;\">Unsubscribe</a></p>\r\n                                                                                    </td>\r\n                                                                                </tr>\r\n                                                                            </tbody>\r\n                                                                        </table>\r\n                                                                    </td>\r\n                                                                </tr>\r\n                                                            </tbody>\r\n                                                        </table>\r\n                                                    </td>\r\n                                                </tr>\r\n                                            </tbody>\r\n                                        </table>\r\n                                    </td>\r\n                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                    </td>\r\n                </tr>\r\n            </tbody>\r\n        </table>\r\n    </div>\r\n</body>\r\n\r\n</html>";
                string message = head + content + foot;
                return message; // return HTML Table as string from this function  
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<bool> CreateMailMessage(Order order)
        {
            GeneratedBarcode Qrcode = IronBarCode.QRCodeWriter.CreateQrCode($"https://dev.fine-api.smjle.vn/api/order/orderStatus?orderId={order.Id}");
            Qrcode.AddAnnotationTextAboveBarcode("Scan me o((>ω< ))o");
            Qrcode.SaveAsPng("MyBarCode.png");

            var path = Path.Combine(Environment.CurrentDirectory + @"\MyBarCode.png");
            LinkedResource LinkedImage = new LinkedResource(path);
            LinkedImage.ContentId = "MyPic";
            var mess = await getHtml(order);
            bool success = false;
            string to = order.Customer.Email;
            string from = "mytdvse151417@fpt.edu.vn";
            MailMessage message = new MailMessage(from, to);
            message.Subject = "Đơn hàng đây, đơn hàng đây! Fine mang niềm vui đến cho bạn nè!!!!";
            message.Body = mess;

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(mess, null, "text/html");
            htmlView.LinkedResources.Add(LinkedImage);
            message.AlternateViews.Add(htmlView);

            message.IsBodyHtml = true;
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            SmtpServer.UseDefaultCredentials = false;

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("mytdvse151417@fpt.edu.vn", "070301000119");
            SmtpServer.EnableSsl = true;

            try
            {
                SmtpServer.Send(message);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                throw new Exception(ex.Message);
            }
            return success;
        }

        public async Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrderByCustomerId(int customerId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
                                        .Where(x => x.CustomerId == customerId)
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<GenOrderResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<GenOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(int customerId, CreatePreOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Phân store trong order detail
                List<ListDetailByStore> listPreOD = new List<ListDetailByStore>();
                foreach (var orderDetail in request.OrderDetails)
                {
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Include(x => x.Product)
                        .Where(x => x.Id == orderDetail.ProductInMenuId)
                        .FirstOrDefault();

                    if (productInMenu.Menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                           MenuErrorEnums.NOT_FOUND.GetDisplayName());

                    var detail = new PreOrderDetailRequest();
                    detail.ProductInMenuId = orderDetail.ProductInMenuId;
                    detail.ProductCode = productInMenu.Product.ProductCode;
                    detail.ProductName = productInMenu.Product.ProductName;
                    detail.UnitPrice = productInMenu.Price;
                    detail.Quantity = orderDetail.Quantity;
                    detail.TotalAmount = (double)(detail.UnitPrice * detail.Quantity);
                    detail.FinalAmount = detail.TotalAmount;
                    detail.ComboId = orderDetail.ComboId;

                    //phan store
                    if (!listPreOD.Any(x => x.StoreId == productInMenu.StoreId))
                    {
                        ListDetailByStore preOD = new ListDetailByStore();
                        preOD.Details = new List<PreOrderDetailRequest>();
                        preOD.StoreId = (int)productInMenu.StoreId;
                        preOD.StoreName = productInMenu.Product.Store.StoreName;
                        preOD.TotalAmount = detail.TotalAmount;
                        preOD.TotalProduct = detail.Quantity;
                        preOD.Details.Add(detail);
                        listPreOD.Add(preOD);
                    }
                    else
                    {
                        var preOD = listPreOD.Where(x => x.StoreId == productInMenu.StoreId)
                            .FirstOrDefault();
                        preOD.TotalAmount += detail.TotalAmount;
                        preOD.TotalProduct += detail.Quantity;
                        preOD.Details.Add(detail);
                    }
                }
                #endregion

                var genOrder = _mapper.Map<GenOrderResponse>(request);

                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                var orderCount = _unitOfWork.Repository<Order>().GetAll().Count() + 1;
                genOrder.OrderCode = customer.UniInfo.University.UniCode + "-" +
                                        orderCount.ToString().PadLeft(3, '0') + "." + Ultils.GenerateRandomCode();

                #region Gen customer + delivery phone

                //check phone number
                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Ultils.CheckVNPhone(request.DeliveryPhone))
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }

                genOrder.Customer = _mapper.Map<OrderCustomerResponse>(customer);
                genOrder.DeliveryPhone = request.DeliveryPhone;
                #endregion

                genOrder.CheckInDate = DateTime.Now;

                #region FinalAmount / TotalAmount
                genOrder.InverseGeneralOrder = new List<OrderResponse>();

                foreach (var store in listPreOD)
                {
                    var order = _mapper.Map<OrderResponse>(store);
                    order.TotalAmount = store.TotalAmount;
                    order.OrderCode = genOrder.OrderCode + "-" + store.StoreId;
                    order.FinalAmount = order.TotalAmount;
                    order.OrderStatus = (int)OrderStatusEnum.PreOrder;
                    order.ItemQuantity = store.TotalProduct;
                    order.OrderDetails = _mapper.Map<List<OrderDetailResponse>>(store.Details);
                    genOrder.InverseGeneralOrder.Add(order);

                    genOrder.ItemQuantity += store.TotalProduct;
                    genOrder.TotalAmount += order.TotalAmount;
                    if (genOrder.ShippingFee == 0)
                    {
                        genOrder.ShippingFee = 5000;
                    }
                    else
                    {
                        genOrder.ShippingFee += 2000;
                    }
                }
                #endregion

                var room = _unitOfWork.Repository<Room>().GetAll().FirstOrDefault(x => x.Id == request.RoomId);
                genOrder.Room = _mapper.Map<OrderRoomResponse>(room);

                genOrder.FinalAmount = (double)(genOrder.TotalAmount + genOrder.ShippingFee);
                genOrder.OrderStatus = (int)OrderStatusEnum.PreOrder;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

                genOrder.CheckInDate = DateTime.Now;
                genOrder.TimeSlot = _mapper.Map<OrderTimeSlotResponse>(timeSlot);

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(genOrder)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(int customerId, CreateGenOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Customer
                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                //check phone number
                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Ultils.CheckVNPhone(request.DeliveryPhone))
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }
                #endregion


                var genOrder = _mapper.Map<Order>(request);
                genOrder.CheckInDate = DateTime.Now;
                genOrder.CustomerId = customerId;
                genOrder.OrderStatus = (int)OrderStatusEnum.Processing;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

                foreach (var order in request.InverseGeneralOrder)
                {
                    var inverseOrder = new Order();
                    inverseOrder = _mapper.Map<Order>(genOrder);
                    inverseOrder = _mapper.Map<CreateOrderRequest, Order>(order, inverseOrder);
                    genOrder.InverseGeneralOrder.Add(inverseOrder);
                }

                await _unitOfWork.Repository<Order>().InsertAsync(genOrder);
                await _unitOfWork.CommitAsync();

                CreateMailMessage(genOrder);


                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(genOrder)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);
                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(order)
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<GenOrderResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<GenOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> CancelOrder(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
                          OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

                if (order.GeneralOrderId != null)
                {
                    var genOrder = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == order.GeneralOrderId);

                    if (genOrder.InverseGeneralOrder.Count() > 1)
                    {
                        genOrder.TotalAmount -= order.TotalAmount;
                        genOrder.Discount -= order.Discount;
                        genOrder.FinalAmount -= order.FinalAmount;
                        genOrder.ShippingFee -= 2000;
                    }
                    else
                    {
                        genOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
                    }
                    await _unitOfWork.Repository<Order>().UpdateDetached(genOrder);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    foreach (var inverseOrder in order.InverseGeneralOrder)
                    {
                        inverseOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
                    }
                }
                order.OrderStatus = (int)OrderStatusEnum.UserCancel;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> UpdateOrder(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
                              OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

                order.OrderStatus = (int)OrderStatusEnum.Finished;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}

