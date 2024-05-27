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


    public async Task OnGetAsync(string searchString, string? sortOrder, bool sortDesc, int? pageNumber)
    {
        CurrentFilter = searchString;

        SortOrder = sortOrder ?? "time";
        SortDesc = sortDesc;

        var notificationsIQ = context.Notifications.AsQueryable();
        if (!string.IsNullOrEmpty(searchString))
        {
            notificationsIQ = notificationsIQ.Where(n =>
                n.Username.Contains(searchString)
                || n.Nickname.Contains(searchString)
                || n.NodeVersion.Contains(searchString)
                || n.Ip.Contains(searchString)
                || n.Host.Contains(searchString)
                || n.Username.Contains(searchString)
                || n.MachineName.Contains(searchString)
                || n.AuthInfo.Contains(searchString)
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

        var pageSize = 100;
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
