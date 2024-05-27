using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StatusNotifier.Pages;

public class IndexModel : PageModel
{
    public string? CurrentFilter { get; set; }
    public string SortOrder { get; set; } = "time";
    public bool SortDesc { get; set; } = true;

    public PaginatedList<Notification> Notifications { get; set; } = new([], 0, 0, 0);
    readonly NotificationDbContext context;

    public IndexModel(NotificationDbContext context) => this.context = context;


    public async Task OnGetAsync(string? searchString, string? sortOrder, bool? sortDesc, int? pageNumber)
    {
        if (searchString is not null) CurrentFilter = searchString;
        if (sortOrder is not null) SortOrder = sortOrder;
        if (sortDesc is not null) SortDesc = sortDesc ?? true;

        var notificationsIQ = context.Notifications.AsQueryable();
        if (!string.IsNullOrEmpty(searchString))
        {
            // ToLower() calls because EF doesn't understand StrignComparison.OrdinalIgnoreCase
            notificationsIQ = notificationsIQ.Where(n =>
                n.Content.ToLower().Contains(searchString.ToLower())
                || n.Username.ToLower().Contains(searchString.ToLower())
                || n.Nickname.ToLower().Contains(searchString.ToLower())
                || n.NodeVersion.ToLower().Contains(searchString.ToLower())
                || n.Ip.ToLower().Contains(searchString.ToLower())
                || n.Host.ToLower().Contains(searchString.ToLower())
                || n.Username.ToLower().Contains(searchString.ToLower())
                || n.MachineName.ToLower().Contains(searchString.ToLower())
                || n.AuthInfo.ToLower().Contains(searchString.ToLower())
            );
        }

        notificationsIQ = (SortOrder, SortDesc) switch
        {
            (null, _) => notificationsIQ,
            ("time", false) => notificationsIQ.OrderBy(n => n.Time),
            ("time", true) => notificationsIQ.OrderByDescending(n => n.Time),
            ("nickname", false) => notificationsIQ.OrderBy(n => n.Nickname),
            ("nickname", true) => notificationsIQ.OrderByDescending(n => n.Nickname),
            ("nodeversion", false) => notificationsIQ.OrderBy(n => n.NodeVersion),
            ("nodeversion", true) => notificationsIQ.OrderByDescending(n => n.NodeVersion),

            ("ip", false) => notificationsIQ.OrderBy(n => n.Ip),
            ("ip", true) => notificationsIQ.OrderByDescending(n => n.Ip),
            ("publicport", false) => notificationsIQ.OrderBy(n => n.PublicPort),
            ("publicport", true) => notificationsIQ.OrderByDescending(n => n.PublicPort),
            ("host", false) => notificationsIQ.OrderBy(n => n.Host),
            ("host", true) => notificationsIQ.OrderByDescending(n => n.Host),

            ("username", false) => notificationsIQ.OrderBy(n => n.Username),
            ("username", true) => notificationsIQ.OrderByDescending(n => n.Username),
            ("machinename", false) => notificationsIQ.OrderBy(n => n.MachineName),
            ("machinename", true) => notificationsIQ.OrderByDescending(n => n.MachineName),
            _ => notificationsIQ.OrderBy(n => n.Username),
        };

        var pageSize = 200;
        Notifications = await PaginatedList<Notification>.CreateAsync(notificationsIQ.AsNoTracking(), pageNumber ?? 1, pageSize);
    }


    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int) Math.Ceiling(count / (double) pageSize);

            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip(
                (pageIndex - 1) * pageSize)
                .Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
