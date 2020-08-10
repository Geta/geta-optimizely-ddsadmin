using System.Collections.Generic;
using Geta.DdsAdmin.Dds.Responses;

namespace Geta.DdsAdmin.Dds.Interfaces
{
    public interface ICrudService
    {
        StringResponse Create(string storeName, Dictionary<string, string> values);
        StringResponse Delete(string storeName, string identityId);
        ReadResponse Read(string storeName, int start, int pageSize, string search, int sortByColumn, string sortDirection, string filterByColumn, string filterValue);
        StringResponse Update(string storeName, int columnId, string value, string id, string columnName);
    }
}
