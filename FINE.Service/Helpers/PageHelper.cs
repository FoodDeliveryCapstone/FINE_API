using FINE.Service.DTO.Response;

namespace FINE.Service.Helpers
{
    public static class PageHelper<T> where T : class
    {
        public static PagedResults<T> Paging(List<T> list, int? page, int? pageSize)
        {
            try
            {
                if (page == null && pageSize == null)
                {
                    pageSize = list.Count;
                    page = 1;
                }
                else
                if (page < 1 || pageSize < 1)
                {
                    return null;
                }
                var skipAmount = pageSize * (page - 1);
                var totalNumberOfRecords = list.Count;
                var results = list.Skip((int)skipAmount).Take((int)pageSize).ToList();
                var mod = totalNumberOfRecords % pageSize;
                var totalPageCount = totalNumberOfRecords / pageSize + (mod == 0 ? 0 : 1);
                return new PagedResults<T>
                {
                    Results = results,
                    PageNumber = (int)page,
                    PageSize = (int)pageSize,
                    TotalNumberOfPages = (int)totalPageCount,
                    TotalNumberOfRecords = totalNumberOfRecords,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<T> Sorting(SortType.SortOrder sortType, IEnumerable<T> searchResult, string colName)
        {
            if (sortType == SortType.SortOrder.Ascending)
            {
                return searchResult.OrderBy(item => typeof(T).GetProperties().First(x => x.Name.Contains(colName, StringComparison.CurrentCultureIgnoreCase)).GetValue(item)).ToList();
            }
            else if (sortType == SortType.SortOrder.Descending)
            {
                return searchResult.OrderByDescending(item => typeof(T).GetProperties().First(x => x.Name.Contains(colName, StringComparison.CurrentCultureIgnoreCase)).GetValue(item)).ToList();
            }
            else
            {
                return searchResult.ToList();
            }
        }
    }
}