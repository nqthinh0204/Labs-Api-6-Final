using AspLab06Final.Mvc.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspLab06Final.Mvc.Data;

// Lab06: kế thừa IdentityDbContext<ApplicationUser> thay vì DbContext thường
// để có sẵn các bảng AspNetUsers/AspNetRoles/AspNetUserRoles... phục vụ Identity + Role-based Authorization.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bắt buộc gọi trước tiên: cấu hình các bảng Identity (AspNetUsers, AspNetRoles, ...)
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genres");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.BookCode).IsRequired().HasMaxLength(20);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Author).IsRequired().HasMaxLength(150);
            entity.Property(b => b.Publisher).HasMaxLength(150);
            entity.Property(b => b.Price).HasColumnType("decimal(18,2)");

            // Mã nghiệp vụ duy nhất (tính cả bản ghi đã soft delete)
            entity.HasIndex(b => b.BookCode).IsUnique();

            // Concurrency token. SQLite không tự sinh rowversion nên cấu hình
            // ValueGeneratedNever và gán thủ công trong SaveChanges -> concurrency hoạt động thật.
            entity.Property(b => b.RowVersion)
                  .IsConcurrencyToken()
                  .ValueGeneratedNever();

            // Global query filter: mặc định mọi truy vấn bỏ qua bản ghi đã soft delete
            entity.HasQueryFilter(b => !b.IsDeleted);

            entity.HasOne(b => b.Genre)
                  .WithMany(g => g.Books)
                  .HasForeignKey(b => b.GenreId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Level).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(40);
            entity.Property(a => a.Entity).IsRequired().HasMaxLength(40);
            entity.Property(a => a.EntityKey).HasMaxLength(40);
            entity.Property(a => a.Message).IsRequired().HasMaxLength(500);
            entity.Property(a => a.UserName).HasMaxLength(256);
            entity.Property(a => a.IpAddress).HasMaxLength(64);
            entity.Property(a => a.Result).IsRequired().HasMaxLength(20);

            // Phục vụ Feature 3 - tìm kiếm/lọc theo user, action, result, khoảng thời gian
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.Result);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.SaleCode).IsRequired().HasMaxLength(30);
            entity.Property(s => s.CreatedByUserName).HasMaxLength(256);
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.SaleCode).IsUnique();
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable("SaleItems");
            entity.HasKey(si => si.Id);
            entity.Property(si => si.BookTitleSnapshot).IsRequired().HasMaxLength(200);
            entity.Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(si => si.Sale)
                  .WithMany(s => s.Items)
                  .HasForeignKey(si => si.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Không cho xoá 1 cuốn sách nếu đã có lịch sử bán hàng gắn với nó (giữ toàn vẹn lịch sử)
            entity.HasOne(si => si.Book)
                  .WithMany()
                  .HasForeignKey(si => si.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        byte[] V(int n) => new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)n };

        // Seed Genres
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = 1, Name = "Kỹ năng sống" },
            new Genre { Id = 2, Name = "Tiểu thuyết" },
            new Genre { Id = 3, Name = "Khoa học" },
            new Genre { Id = 4, Name = "Công nghệ" }
        );

        // Seed Books
        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, BookCode = "BK-SKL-101", Title = "7 Thói Quen Hiệu Quả", Author = "Stephen R. Covey", Publisher = "NXB Trẻ", Price = 145000, Quantity = 18, MinStock = 5, GenreId = 1, CreatedAt = seedDate, RowVersion = V(1) },
            new Book { Id = 2, BookCode = "BK-SKL-102", Title = "Sức Mạnh Của Thói Quen", Author = "Charles Duhigg", Publisher = "NXB Lao Động", Price = 132000, Quantity = 6, MinStock = 5, GenreId = 1, CreatedAt = seedDate, RowVersion = V(2) },
            new Book { Id = 3, BookCode = "BK-SKL-103", Title = "Atomic Habits", Author = "James Clear", Publisher = "NXB Thế Giới", Price = 168000, Quantity = 12, MinStock = 5, GenreId = 1, CreatedAt = seedDate, RowVersion = V(3) },
            new Book { Id = 4, BookCode = "BK-SKL-104", Title = "Dám Bị Ghét", Author = "Ichiro Kishimi", Publisher = "NXB Lao Động", Price = 98000, Quantity = 4, MinStock = 5, GenreId = 1, CreatedAt = seedDate, RowVersion = V(4) },

            new Book { Id = 5, BookCode = "BK-NOV-101", Title = "Không Gia Đình", Author = "Hector Malot", Publisher = "NXB Văn Học", Price = 89000, Quantity = 15, MinStock = 3, GenreId = 2, CreatedAt = seedDate, RowVersion = V(5) },
            new Book { Id = 6, BookCode = "BK-NOV-102", Title = "Rừng Na Uy", Author = "Haruki Murakami", Publisher = "NXB Hội Nhà Văn", Price = 125000, Quantity = 7, MinStock = 3, GenreId = 2, CreatedAt = seedDate, RowVersion = V(6) },
            new Book { Id = 7, BookCode = "BK-NOV-103", Title = "Kiêu Hãnh Và Định Kiến", Author = "Jane Austen", Publisher = "NXB Văn Học", Price = 119000, Quantity = 9, MinStock = 3, GenreId = 2, CreatedAt = seedDate, RowVersion = V(7) },
            new Book { Id = 8, BookCode = "BK-NOV-104", Title = "451 Độ F", Author = "Ray Bradbury", Publisher = "NXB Thế Giới", Price = 112000, Quantity = 5, MinStock = 3, GenreId = 2, CreatedAt = seedDate, RowVersion = V(8) },

            new Book { Id = 9, BookCode = "BK-SCI-101", Title = "Cosmos", Author = "Carl Sagan", Publisher = "NXB Khoa Học", Price = 178000, Quantity = 8, MinStock = 3, GenreId = 3, CreatedAt = seedDate, RowVersion = V(9) },
            new Book { Id = 10, BookCode = "BK-SCI-102", Title = "The Selfish Gene", Author = "Richard Dawkins", Publisher = "NXB Khoa Học", Price = 165000, Quantity = 3, MinStock = 3, GenreId = 3, CreatedAt = seedDate, RowVersion = V(10) },
            new Book { Id = 11, BookCode = "BK-SCI-103", Title = "Brief Answers to the Big Questions", Author = "Stephen Hawking", Publisher = "NXB Trẻ", Price = 156000, Quantity = 2, MinStock = 3, GenreId = 3, CreatedAt = seedDate, RowVersion = V(11) },

            new Book { Id = 12, BookCode = "BK-TECH-101", Title = "Refactoring", Author = "Martin Fowler", Publisher = "NXB Lao Động", Price = 245000, Quantity = 6, MinStock = 3, GenreId = 4, CreatedAt = seedDate, RowVersion = V(12) },
            new Book { Id = 13, BookCode = "BK-TECH-102", Title = "Design Patterns", Author = "Erich Gamma", Publisher = "NXB Lao Động", Price = 265000, Quantity = 4, MinStock = 3, GenreId = 4, CreatedAt = seedDate, RowVersion = V(13) },
            new Book { Id = 14, BookCode = "BK-TECH-103", Title = "Domain-Driven Design", Author = "Eric Evans", Publisher = "NXB Công Nghệ", Price = 285000, Quantity = 2, MinStock = 3, GenreId = 4, CreatedAt = seedDate, RowVersion = V(14) },
            new Book { Id = 15, BookCode = "BK-TECH-104", Title = "Code Complete", Author = "Steve McConnell", Publisher = "NXB Công Nghệ", Price = 238000, Quantity = 10, MinStock = 3, GenreId = 4, CreatedAt = seedDate, RowVersion = V(15) }
        );
    }

    // Tự động gán RowVersion mới cho Book khi thêm/sửa (vì SQLite không tự sinh).
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Book>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}