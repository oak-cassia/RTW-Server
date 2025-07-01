using Microsoft.EntityFrameworkCore;
using RTWWebServer.Entity;

namespace RTWWebServer.Database;

public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Guest> Guests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Salt).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("Guest");
        });

        // base.OnModelCreating를 마지막에 호출하는 이유:
        // 1. 자식 클래스에서 정의한 구성이 우선 적용되도록 하기 위함
        // 2. 부모 클래스의 구성이 자식 클래스의 구성을 덮어쓰지 않도록 함
        // 3. 충돌이 있는 경우 부모 클래스(DbContext)의 구성보다 현재 컨텍스트의 구성을 우선시하기 위함
        base.OnModelCreating(modelBuilder);
    }
}