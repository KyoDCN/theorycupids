using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheoryCupids.Helpers
{
    public class PagedList<T> : List<T>
    {
        /// <summary>
        /// Current page
        /// </summary>
        public int CurrentPage { get; set; }
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }
        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// Total count of items
        /// </summary>
        public int TotalCount { get; set; }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            int count = await source.CountAsync();
            List<T> items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
