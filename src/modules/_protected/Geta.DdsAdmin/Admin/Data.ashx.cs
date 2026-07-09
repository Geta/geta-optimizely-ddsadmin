using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Geta.DdsAdmin.Dds.Interfaces;
using Geta.DdsAdmin.Dds.Responses;
using Geta.DdsAdmin.Dds.Services;
using log4net;

namespace Geta.DdsAdmin.Admin
{
    public class Data : IHttpHandler
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DdsAdmin));

        private readonly ICrudService crudService = new CrudService(new StoreService(new ExcludedStoresService()));

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            logger.Info("Started request");
            try
            {
                if (!SecurityHelper.CheckAccess())
                {
                    context.Response.StatusCode = 401;
                    context.Response.End();
                    return;
                }

                WriteResponse(context);
            }
            catch (NotImplementedException ex)
            {
                context.Response.StatusCode = 501;
                context.Response.Write(new JavaScriptSerializer().Serialize(ex.Message));
                logger.Error(ex);
            }
            catch (Exception ex)
            {
                logger.Error("Exception occurred:", ex);
                throw;
            }

            logger.Info("Finished request");
        }

        private static BaseResponse SerializeResponse(HttpContext context, StringResponse stringResponse)
        {
            context.Response.Write(stringResponse.NotJson
                                       ? stringResponse.Response
                                       : new JavaScriptSerializer().Serialize(stringResponse.Response));
            return stringResponse;
        }

        private BaseResponse Create(HttpContext context, string storeName)
        {
            var values = context.Request.Form.AllKeys.ToDictionary(item => item.Substring(5), item => context.Request.Form[item]);

            var createResponse = this.crudService.Create(storeName, values);
            return SerializeResponse(context, createResponse);
        }

        private BaseResponse Delete(HttpContext context, string storeName)
        {
            string id = context.Request.Form["id"];

            var deleteResponse = this.crudService.Delete(storeName, id);
            return SerializeResponse(context, deleteResponse);
        }

        private BaseResponse Read(HttpContext context, string storeName, string filterColumnName, string filter)
        {
            int start = Convert.ToInt32(context.Request.QueryString["iDisplayStart"]);
            int pageSize = Convert.ToInt32(context.Request.QueryString["iDisplayLength"]);
            int echo = Convert.ToInt32(context.Request.QueryString["sEcho"]);
            string search = context.Request.QueryString["sSearch"];
            int sortByColumn = Convert.ToInt32(context.Request.QueryString["iSortCol_0"]);
            string sortDirection = context.Request.QueryString["sSortDir_0"];

            var readResponse = this.crudService.Read(storeName, start, pageSize, search, sortByColumn, sortDirection, filterColumnName, filter);

            var result = new
                             {
                                 sEcho = echo,
                                 iTotalRecords = readResponse.TotalCount,
                                 iTotalDisplayRecords = readResponse.TotalCount,
                                 aaData = readResponse.Data
                             };

            context.Response.Write(new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(result));
            return readResponse;
        }

        private BaseResponse Update(HttpContext context, string storeName)
        {
            int columnId = Convert.ToInt32(context.Request.Form["columnId"]);
            string columnName = context.Request.Form["columnName"];
            string id = context.Request.Form["id"];
            string value = context.Request.Form["value"];

            var updateResponse = this.crudService.Update(storeName, columnId, value, id, columnName);
            return SerializeResponse(context, updateResponse);
        }

        private void WriteResponse(HttpContext context)
        {
            string operation = context.Request.QueryString[Constants.OperationKey];
            logger.InfoFormat("Operation:{0}", operation);
            string storeName = context.Request.QueryString[Constants.StoreKey];
            logger.InfoFormat("Store:{0}", operation);
            string filterColumnName = context.Request.QueryString[Constants.FilterColumnNameKey];
            logger.InfoFormat("Filter column name:{0}", filterColumnName);
            string filter = context.Request.QueryString[Constants.FilterKey];
            logger.InfoFormat("Filter:{0}", filter);

            context.Response.Clear();
            context.Response.ClearContent();
            context.Response.ContentType = "application/json; charset=UTF-8";

            BaseResponse response;
            switch (operation)
            {
                case "read":
                    {
                        response = Read(context, storeName, filterColumnName, filter);
                    }
                    break;
                case "update":
                    {
                        response = Update(context, storeName);
                    }
                    break;
                case "delete":
                    {
                        response = Delete(context, storeName);
                    }
                    break;
                case "create":
                    {
                        response = Create(context, storeName);
                    }
                    break;
                default:
                    logger.ErrorFormat("Operation:{0} is not implemented!", operation);
                    throw new NotImplementedException(string.Format("Operation:{0} is not implemented!", operation));
            }

            if (!response.Success)
            {
                context.Response.StatusCode = response.StatusCode ?? 500;
            }

            context.Response.End();
        }
    }
}
