using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using Geta.DdsAdmin.Dds.Interfaces;
using log4net;

namespace Geta.DdsAdmin.Dds.Services
{
    public class StoreService : IStoreService
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof (StoreService));
        private readonly IExcludedStoresService excludedStoresService;

        public StoreService(IExcludedStoresService excludedStoresService)
        {
            this.excludedStoresService = excludedStoresService;
        }

        public PropertyBag Create(string storeName, Dictionary<string, string> values)
        {
            try
            {
                var store = DynamicDataStoreFactory.Instance.GetStore(storeName);
                var item = new PropertyBag {Id = Identity.NewIdentity()};
                foreach (var value in values)
                {
                    if (value.Key == "Id")
                    {
                        // skip id when creating new record
                        continue;
                    }

                    var meta = store.Metadata().GetColumnMetadata(value.Key);

                    if (!(meta is InlinePropertyMap))
                    {
                        // or is it better to skip such column?
                        throw new NotImplementedException(
                            string.Format("Saving {0} field name = {1} is not yet implemented!",
                                          meta.GetType().Name,
                                          value.Key));
                    }

                    item[value.Key] = GetValue(value.Value, meta.PropertyType);
                }

                store.Save(item);
                return item;
            }
            catch (NotImplementedException ex)
            {
                logger.Error(ex);
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("Create row failed", ex);
                return null;
            }
        }

        public bool Delete(string storeName, Identity id)
        {
            try
            {
                var store = DynamicDataStoreFactory.Instance.GetStore(storeName);
                var item = store.ItemsAsPropertyBag().First(items => items.Id.Equals(id));

                store.Delete(item);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Delete failed", ex);
                return false;
            }
        }

        public bool Flush(string storeName, string filterColumnName, string filter)
        {
            try
            {
                var store = DynamicDataStoreFactory.Instance.GetStore(storeName);
                if (!string.IsNullOrWhiteSpace(filterColumnName))
                {
                    var itemsToBeDeleted = store.FindAsPropertyBag(filterColumnName, filter).ToList();
                    foreach (var itemToBeDeleted in itemsToBeDeleted)
                    {
                        store.Delete(itemToBeDeleted.Id);
                    }
                }
                else
                {
                    store.DeleteAll();
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Delete failed", ex);
                return false;
            }
        }

        public IEnumerable<StoreMetadata> GetAllMetadata(bool filterEnabled)
        {
            IEnumerable<StoreMetadata> visibleStores;

            if (filterEnabled)
            {
                var invisibleStoreFilters = excludedStoresService.GetAll();
                visibleStores = GetFilteredMetadata(invisibleStoreFilters);
            }
            else
            {
                visibleStores = GetFilteredMetadata();
            }

            foreach (var info in visibleStores)
            {
                info.Rows = DynamicDataStoreFactory.Instance.GetStore(info.Name).Items().Count();
                yield return info;
            }
        }

        public StoreMetadata GetMetadata(string storeName)
        {
            return (from store in StoreDefinition.GetAll()
                    where store.StoreName == storeName
                    select new StoreMetadata {Name = store.StoreName, Columns = store.ActiveMappings}).FirstOrDefault();
        }

        public bool UpdateCell(string storeName, Identity id, int columnId, string columnName, object value)
        {
            try
            {
                var store = DynamicDataStoreFactory.Instance.GetStore(storeName);
                var item = store.ItemsAsPropertyBag().First(items => items.Id.Equals(id));
                var meta = store.Metadata().GetColumnMetadata(columnName);

                item[columnName] = GetValue(value.ToString(), meta.PropertyType);
                store.Save(item);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Update cell failed", ex);
                return false;
            }
        }

        private object GetValue(string stringValue, Type type)
        {
            if(type == typeof(string))
            {
                return stringValue;
            }
            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter is DoubleConverter)
            {
                double value;
                if (double.TryParse(stringValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out value))
                {
                    return value;
                }
            }
            return typeConverter.ConvertFromInvariantString(stringValue);
        }

        private IEnumerable<StoreMetadata> GetFilteredMetadata(IEnumerable<ExcludedStore> invisibleStores = null)
        {
            return from store in StoreDefinition.GetAll()
                   where invisibleStores == null || !invisibleStores.Any(i => store.StoreName.Contains(i.Filter))
                   select new StoreMetadata {Name = store.StoreName, Columns = store.ActiveMappings};
        }
    }
}