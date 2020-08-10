using System.Collections.Generic;
using System.Linq;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using Geta.DdsAdmin.Dds.Interfaces;
using Geta.DdsAdmin.Dds.Responses;
using log4net;

namespace Geta.DdsAdmin.Dds.Services
{
    public class CrudService : ICrudService
    {
        private const int MaxLength = 50000;

        private static readonly ILog logger = LogManager.GetLogger(typeof(CrudService));

        private readonly IStoreService storeService;

        public CrudService(IStoreService storeService)
        {
            this.storeService = storeService;
        }

        public StringResponse Create(string storeName, Dictionary<string, string> values)
        {
            var response = new StringResponse { Success = false };
            logger.Debug("Create started");

            var newItem = this.storeService.Create(storeName, values);

            if (newItem != null)
            {
                response.Response = newItem.Id.ToString();
                response.Success = true;
                response.NotJson = true;
            }
            else
            {
                response.StatusCode = 500;
                response.Response = "Could not create row!";
                logger.Debug("Create failed - Could not create row!");
            }

            logger.Debug("Create finished");
            return response;
        }

        public StringResponse Delete(string storeName, string identityId)
        {
            var response = new StringResponse { Success = false };
            logger.Debug("Delete started");
            Identity identity;
            if (Identity.TryParse(identityId, out identity) && identity != null)
            {
                if (this.storeService.Delete(storeName, identity))
                {
                    response.Response = "ok";
                    response.Success = true;
                    response.NotJson = true;
                }
                else
                {
                    response.StatusCode = 500;
                    response.Response = "Could not delete row!";
                }
            }
            else
            {
                response.StatusCode = 500;
                response.Response = "Could not interpret Id!";
            }

            logger.Debug("Delete finished");
            return response;
        }

        public ReadResponse Read(string storeName, int start, int pageSize, string search, int sortByColumn, string sortDirection, string filterByColumn, string filterValue)
        {
            var response = new ReadResponse { Success = false };
            logger.Debug("Read started");
            int totalCount;

            var store = DynamicDataStoreFactory.Instance.GetStore(storeName);
            var storeMetadata = store.Metadata();

            List<PropertyBag> data;
            int count;

            var preSorted = false;

            if (string.IsNullOrWhiteSpace(filterByColumn))
            {
                // TODO: we cannot order here due to fact that this is PropertyBag, if we could it would be great performance boost
                // var orderBy = sortByColumn == 0 ? "Id" : StoreMetadata.Columns.ToList()[sortByColumn - 1].PropertyName;
                var query = store.ItemsAsPropertyBag(); // .OrderBy(orderBy);

                preSorted = sortByColumn == 0 && string.IsNullOrEmpty(search);

                data = preSorted
                                   ? (sortDirection == "asc"
                                              ? query.OrderBy(r => r.Id).Skip(start).Take(pageSize).ToList()
                                              : query.OrderByDescending(r => r.Id).Skip(start).Take(pageSize).ToList())
                                   : query.ToList();
                count = query.Count();
            }
            else
            {
                data = store.FindAsPropertyBag(filterByColumn, filterValue).ToList();
                count = data.Count;
            }

            List<List<string>> stringData;
            if (preSorted)
            {
                // no sorting and no filtering, use fast code then
                stringData = FormatData(storeMetadata, data);
                totalCount = count;
            }
            else
            {
                stringData = FilterAndFormatData(storeMetadata, data, search);
                totalCount = stringData.Count;
                stringData = GetSortedPagedData(stringData, start, pageSize, sortByColumn, sortDirection == "asc");
            }

            response.TotalCount = totalCount;
            response.Data = stringData;
            response.Success = true;
            logger.Debug("Read finished");
            return response;
        }

        public StringResponse Update(string storeName, int columnId, string value, string id, string columnName)
        {
            var response = new StringResponse { Success = false };
            logger.DebugFormat("Update started");

            Identity identity;
            if (Identity.TryParse(id, out identity) && identity != null)
            {
                if (this.storeService.UpdateCell(storeName, identity, columnId, columnName, value))
                {
                    response.Response = value;
                    response.Success = true;
                }
                else
                {
                    response.StatusCode = 500;
                    response.Response = "Could not save cell!";
                    logger.Error("Update failed - Could not save cell!");
                }
            }
            else
            {
                response.StatusCode = 500;
                response.Response = "Could not interpret Id!";
                logger.Error("Update failed - Could not interpret Id!");
            }

            logger.Debug("Update finished");

            return response;
        }

        private static List<List<string>> FilterAndFormatData(StoreMetadata storeMetadata, IEnumerable<PropertyBag> data, string search)
        {
            var stringData = new List<List<string>>();
            foreach (var row in data)
            {
                bool containsSearchCriteria = string.IsNullOrEmpty(search);

                var item = new List<string> { row.Id.ToString() };
                if (!containsSearchCriteria && row.Id.ToString().Contains(search))
                {
                    containsSearchCriteria = true;
                }

                foreach (var column in storeMetadata.Columns)
                {
                    if (row.Keys.Any(s => s == column.PropertyName) && row[column.PropertyName] != null)
                    {
                        var value = row[column.PropertyName].ToString();
                        item.Add(Truncate(value));

                        if (!containsSearchCriteria && value.Contains(search))
                        {
                            containsSearchCriteria = true;
                        }
                    }
                    else
                    {
                        item.Add(null);
                    }
                }

                if (containsSearchCriteria)
                {
                    stringData.Add(item);
                }
            }

            return stringData;
        }

        private static List<List<string>> FormatData(StoreMetadata storeMetadata, IEnumerable<PropertyBag> data)
        {
            var stringData = new List<List<string>>();
            foreach (var row in data)
            {
                var item = new List<string> { row.Id.ToString() };

                item.AddRange(
                        storeMetadata.Columns.Select(
                                column =>
                                    {
                                        if (row.Keys.Any(s => s == column.PropertyName) && row[column.PropertyName] != null)
                                        {
                                            var s = row[column.PropertyName].ToString();
                                            return Truncate(s);
                                        }

                                        return null;
                                    }));

                stringData.Add(item);
            }

            return stringData;
        }

        private static string Truncate(string s)
        {
            var result = s.Substring(0, s.Length > MaxLength ? MaxLength : s.Length);
            return s.Length > MaxLength ? result + "*** The rest of the content is truncated. ***" : result;
        }

        private static List<List<string>> GetSortedPagedData(List<List<string>> stringData, int skip, int take, int sortColumn, bool ascending)
        {
            var query = ascending ? stringData.OrderBy(sd => sd[sortColumn]) : stringData.OrderByDescending(sd => sd[sortColumn]);
            return query.Skip(skip).Take(take).ToList();
        }
    }
}
