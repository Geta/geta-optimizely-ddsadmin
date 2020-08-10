using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using ClosedXML.Excel;
using EPiServer;
using EPiServer.Data.Dynamic;
using EPiServer.Shell;
using EPiServer.UI;
using EPiServer.Web.Internal;
using Geta.DdsAdmin.Dds;
using Geta.DdsAdmin.Dds.Services;

namespace Geta.DdsAdmin.Admin
{
    public partial class DdsAdmin : SystemPageBase
    {
        private readonly CrudService _crudService;
        private readonly StoreService _storeService;

        public DdsAdmin()
        {
            _storeService = new StoreService(new ExcludedStoresService());
            _crudService = new CrudService(_storeService);
        }

        private int[] HiddenColumns { get; set; }
        protected string CurrentStoreName { get; set; }
        protected string CurrentFilterColumnName { get; set; }
        protected string CurrentFilter { get; set; }
        protected bool ExportToXlsx { get; set; }
        protected string CustomHeading { get; set; }
        protected string CustomMessage { get; set; }
        protected string CurrentFilterMessage { get; set; }
        protected PropertyMap Item { get; set; }
        protected StoreMetadata Store { get; set; }

        protected string GetColumnsScript()
        {
            return DdsAdminScriptHelper.GetColumns(Store.Columns.ToList(), HiddenColumns);
        }

        protected string GetInvisibleColumnsScript()
        {
            return DdsAdminScriptHelper.GetInvisibleColumns(HiddenColumns);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            Page.Header.Controls.Add(new Literal
            {
                Text = "<link type=\"text/css\" rel=\"stylesheet\" href=\"" +
                       Paths.ToClientResource(typeof(MenuProvider), "content/themes/DDSAdmin/custom/minified.css") +
                       "\" />"
            });

            Page.Header.Controls.Add(new Literal
            {
                Text = "<script src=\"" + Paths.ToClientResource(typeof(MenuProvider),
                    "scripts/datatables-1.9.4/media/js/jquery.dataTables.min.js") + "\"></script>"
            });

            Page.Header.Controls.Add(new Literal
            {
                Text = "<script src=\"" +
                       Paths.ToClientResource(typeof(MenuProvider), "scripts/dataTables.jeditable.min.js") +
                       "\"></script>"
            });
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!SecurityHelper.CheckAccess())
            {
                AccessDenied();
            }

            if (IsPostBack)
            {
                return;
            }

            GetQueryStringParameters();
            if (ExportToXlsx)
            {
                RenderXlsx(CurrentStoreName, CurrentFilterColumnName, CurrentFilter);
                return;
            }

            Store = _storeService.GetMetadata(CurrentStoreName);

            LoadAndDisplayData();
        }

        protected string SetItem(RepeaterItem repeaterItem)
        {
            Item = repeaterItem.DataItem as PropertyMap;
            return string.Empty;
        }

        protected string GetParameters()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var builder = new UrlBuilder("http://localhost");
            builder.QueryCollection[Constants.StoreKey] = CurrentStoreName;
            if (CurrentFilterMessage != null)
            {
                builder.QueryCollection[Constants.FilterColumnNameKey] = CurrentFilterColumnName;
                builder.QueryCollection[Constants.FilterKey] = CurrentFilter;
            }

            return builder.Query.TrimStart('?');
        }

        private void GetQueryStringParameters()
        {
            CurrentStoreName = HttpUtility.HtmlEncode(Request.QueryString[Constants.StoreKey]);
            CurrentFilterColumnName = HttpUtility.HtmlEncode(Request.QueryString[Constants.FilterColumnNameKey]);
            CurrentFilter = HttpUtility.HtmlEncode(Request.QueryString[Constants.FilterKey]);
            ExportToXlsx = HttpUtility.HtmlEncode(Request.QueryString[Constants.ExportToXlsxKey]) != null;
            CurrentFilterMessage =
                !string.IsNullOrWhiteSpace(CurrentFilterColumnName) && !string.IsNullOrWhiteSpace(CurrentFilter)
                    ? $"filtered by {CurrentFilterColumnName} = {CurrentFilter}"
                    : null;
            CustomHeading = HttpUtility.HtmlEncode(Request.QueryString[Constants.HeadingKey]);
            CustomMessage = HttpUtility.HtmlEncode(Request.QueryString[Constants.MessageKey]);

            var hiddenColumns = HttpUtility.HtmlEncode(Request.QueryString[Constants.HiddenColumnsKey]);
            if (string.IsNullOrEmpty(hiddenColumns))
            {
                HiddenColumns = new int[0];
                return;
            }

            HiddenColumns = hiddenColumns.Split(new[]
                {
                    ","
                },
                StringSplitOptions.RemoveEmptyEntries).Select(item => Convert.ToInt32(item)).ToArray();
        }

        private void LoadAndDisplayData()
        {
            if (string.IsNullOrEmpty(CurrentStoreName))
            {
                hdivNoStoreTypeSelected.Visible = true;
                return;
            }

            if (Store == null)
            {
                hdivStoreTypeDoesntExist.Visible = true;
                return;
            }

            hdivStoreTypeSelected.Visible = true;

            repColumnsHeader.DataSource = Store.Columns;
            repForm.DataSource = Store.Columns;
            repColumnsHeader.DataBind();
            repForm.DataBind();

            Flush.Text = string.IsNullOrWhiteSpace(CurrentFilterMessage) ? "Delete all data" : "Delete filtered data";
            Flush.OnClientClick = string.IsNullOrWhiteSpace(CurrentFilterMessage)
                ? "return confirm('Do you really want to delete all data from this table?')"
                : "return confirm('Do you really want to delete filtered data from this table?')";
            Export.Text = string.IsNullOrWhiteSpace(CurrentFilterMessage)
                ? "Export to Excel"
                : "Export filtered data to Excel";
        }

        protected void FlushStoreClick(object sender, EventArgs e)
        {
            var storeName = Request.Form["CurrentStoreName"];
            var filterColumnName = Request.Form["CurrentFilterColumnName"];
            var filter = Request.Form["CurrentFilter"];

            _storeService.Flush(storeName, filterColumnName, filter);

            Response.Redirect(Request.RawUrl);
        }

        protected void FilterClick(object sender, EventArgs e)
        {
            var filterColumnName = Request.Form["CurrentFilterColumnName"];
            var filter = Request.Form["CurrentFilter"];

            var builder = new UrlBuilder(Request.Url);
            if (string.IsNullOrWhiteSpace(filterColumnName))
            {
                builder.QueryCollection.Remove(Constants.FilterColumnNameKey);
                builder.QueryCollection.Remove(Constants.FilterKey);
            }
            else
            {
                builder.QueryCollection[Constants.FilterColumnNameKey] = filterColumnName;
                builder.QueryCollection[Constants.FilterKey] = filter;
            }

            Response.Redirect(builder.ToString());
        }

        protected void ExportStoreClick(object sender, EventArgs e)
        {
            var storeName = Request.Form["CurrentStoreName"];
            var filterColumnName = Request.Form["CurrentFilterColumnName"];
            var filter = Request.Form["CurrentFilter"];

            RenderXlsx(storeName, filterColumnName, filter);
        }

        private void RenderXlsx(string storeName, string filterColumnName, string filter)
        {
            var ddsDataSet = GetDdsStoreAsDataSet(storeName, filterColumnName, filter);

            using (var wb = new XLWorkbook())
            {
                wb.Worksheets.Add(ddsDataSet.Tables[0], "Sheet1");

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                if (!string.IsNullOrWhiteSpace(filterColumnName))
                {
                    var fileName = $"{storeName}.{filterColumnName}.{filter}.xlsx";
                    Path.GetInvalidFileNameChars()
                        .Aggregate(fileName, (current, c) => current.Replace(c, '_'));
                    Response.AddHeader("content-disposition",
                        $"attachment;filename={fileName.Substring(0, Math.Min(fileName.Length, 200))}");
                }
                else
                {
                    Response.AddHeader("content-disposition", $"attachment;filename={storeName}.xlsx");
                }

                using (var MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
        }

        private DataSet GetDdsStoreAsDataSet(string storeName, string filterColumnName, string filter)
        {
            var dataTable = new DataTable("record");

            var columns = _storeService.GetMetadata(storeName);

            foreach (var column in columns.Columns)
            {
                dataTable.Columns.Add(column.PropertyName, typeof(string));
            }

            var allRecords = _crudService.Read(storeName, 0, int.MaxValue, null, 0, null, filterColumnName, filter);

            if (allRecords == null || !allRecords.Success)
            {
                return null;
            }

            foreach (var record in allRecords.Data)
            {
                var row = dataTable.NewRow();

                var columMap = columns.Columns.ToArray();

                for (var i = 0; i < columMap.Length; i++)
                {
                    var column = columMap[i];
                    row[column.PropertyName] = record[i + 1];
                }

                dataTable.Rows.Add(row);
            }

            return new DataSet
            {
                Tables =
                {
                    dataTable
                }
            };
        }
    }
}