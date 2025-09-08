using G2.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace G2.Infrastructure.Repository.Database.Base
{
    public class G2DbContext: DbContext
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public G2DbContext(DbContextOptions<G2DbContext> options, IConfiguration configuration) : base(options)
        {
            _connectionString = configuration.GetSection("Database")["ConnectionString"];
            _schema = configuration.GetSection("Database")["Schema"];
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
               optionsBuilder.UseMySQL(_connectionString);
            //    .Use(async (context, _, cancellationToken) =>
            //    {
            //         await PlansSeeder(context, cancellationToken);    
            //         await RolesSeeder(context, cancellationToken);
            //    });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                entity.SetSchema(_schema); // Set schema
            }
            modelBuilder.Entity<Model.Plan>(builder =>
            {
                builder.Property(p => p.RowVersion)
                    .IsRowVersion()
                    .ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Model.Role>(builder =>
            {
                builder.Property(p => p.RowVersion)
                    .IsRowVersion()
                    .ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Model.KnownHeader>(builder =>
            {
                builder.Property(p => p.RowVersion)
                    .IsRowVersion()
                    .ValueGeneratedOnAddOrUpdate();
            });


            modelBuilder.Entity<Model.Plan>().HasData(
                new Model.Plan
                {
                    Id = 1,
                    Name = "Free",
                    Concurrency = 1,
                    Quota = 1,
                    Price = 0.00,
                    Description = "$0.00/mo",
                    Discount = 0.00,
                    Visualisation = true,
                    Screenshot = false,
                    AIReport = false,
                    ConsoleApp = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1)
                },
                new Model.Plan
                {
                    Id = 2,
                    Name = "Starter",
                    Concurrency = 3,
                    Quota = 10,
                    Price = 20.00,
                    Description = "$20.00/mo",
                    Discount = 0.00,
                    Visualisation = true,
                    Screenshot = true,
                    AIReport = true,
                    ConsoleApp = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1)
                },
                new Model.Plan
                {
                    Id = 3,
                    Name = "Professional",
                    Concurrency = 10,
                    Quota = 50,
                    Price = 50.00,
                    Description = "$50.00/mo",
                    Discount = 0.00,
                    Visualisation = true,
                    Screenshot = true,
                    AIReport = true,
                    ConsoleApp = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1)
                }
            );

            modelBuilder.Entity<Model.Role>().HasData(
                new Model.Role
                {
                    Id = 1,
                    Name = "User",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1),
                },
                new Model.Role
                {
                    Id = 2,
                    Name = "Admin",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1),
                },
                new Model.Role
                {
                    Id = 3,
                    Name = "Super-Admin",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    UpdatedAt = DateTime.UtcNow.AddHours(1),
                }
            );

            var headers = KnownRequestHeader.Select((name, index) => new Model.KnownHeader
            {
                Id = index + 1,
                Name = name,
                Type = "request",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddHours(1),
            }).ToList();
            headers.AddRange(KnownResponseHeader.Select((name, index) => new Model.KnownHeader
            {
                Id = KnownRequestHeader.Count + index + 1,
                Name = name,
                Type = "response",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddHours(1),
            }).ToList());
            modelBuilder.Entity<Model.KnownHeader>().HasData(headers);
        }

        public DbSet<Model.User> Users { get; set; }
        public DbSet<Model.AccountVerification> AccountVerifications { get; set; }
        public DbSet<Model.Job> Jobs { get; set; }
        public DbSet<Model.KnownHeader> KnownHeaders { get; set; }
        public DbSet<Model.Message> Messages { get; set; }
        public DbSet<Model.Plan> Plans { get; set; }
        public DbSet<Model.PromoCode> PromoCodes { get; set; }
        public DbSet<Model.Referral> Referrals { get; set; }
        public DbSet<Model.Role> Role { get; set; }
        public DbSet<Model.Transaction> Transactions { get; set; }

        private readonly List<string> KnownRequestHeader =
        [
            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Access-Control-Request-Method",
            "Access-Control-Request-Headers",
            "Authorization",
            "Cache-Control",
            "Connection",
            "Content-Encoding",
            "Content-Length",
            "Content-MD5",
            "Content-Type",
            "Date",
            "Expect",
            "From",
            "Host",
            "HTTP2-Settings",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Max-Forwards",
            "Origin",
            "Pragma",
            "Prefer",
            "Proxy-Authorization",
            "Range",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "User-Agent",
            "Upgrade",
            "Via",
            "Warning",
            "Upgrade-Insecure-Requests",
            "X-Requested-With",
            "DNT",
            "X-Forwarded-For",
            "X-Forwarded-Host",
            "X-Forwarded-Proto",
            "Front-End-Https",
            "X-Http-Method-Override",
            "X-ATT-DeviceId",
            "X-Wap-Profile",
            "Proxy-Connection",
            "X-UIDH",
            "Save-Data",
            "Sec-GPC",
            "Sec-ch-ua",
            "Sec-ch-ua-mobile",
            "Sec-ch-ua-platform",
        ];

        private readonly List<string> KnownResponseHeader =
        [
            "Accept-CH",
            "Access-Control-Allow-Origin",
            "Access-Control-Allow-Credentials",
            "Access-Control-Expose-Headers",
            "Access-Control-Max-Age",
            "Access-Control-Allow-Methods",
            "Access-Control-Allow-Headers",
            "Accept-Patch",
            "Accept-Ranges",
            "Age",
            "Allow",
            "Alt-Svc",
            "Cache-Control",
            "Connection",
            "Content-Disposition",
            "Content-Encoding",
            "Content-Language",
            "Content-Length",
            "Content-Location",
            "Content-MD5",
            "Content-Range",
            "Content-Type",
            "Date",
            "Delta-Base",
            "ETag",
            "Expires",
            "IM",
            "Last-Modified",
            "Link",
            "Location",
            "P3P",
            "Pragma",
            "Preference-Applied",
            "Proxy-Authenticate",
            "Public-Key-Pins",
            "Retry-After",
            "Strict-Transport-Security",
            "Trailer",
            "Transfer-Encoding",
            "Tk",
            "Upgrade",
            "Vary",
            "Via",
            "Warning",
            "X-Frame-Options",
            "Expect-CT",
            "NEL",
            "Permissions-Policy",
            "Refresh",
            "Report-To",
            "Status",
            "Timing-Allow-Origin",
            "X-Content-Duration",
            "X-Content-Type-Options",
            "X-UA-Compatible",
            "cross-origin-resource-policy",
            "cf-ray",
            "cf-cache-status",
            "referrer-policy",
            "content-security-policy",
            "referer",
            "content-security-policy-report-only",
            "cross-origin-opener-policy",
            "x-jsd-version-type",
            "x-cache",
            "x-served-by",
            "x-jsd-version",
        ];
    }
}
