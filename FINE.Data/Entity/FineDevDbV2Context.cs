using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FINE.Data.Entity;

public partial class FineDevDbV2Context : DbContext
{
    public FineDevDbV2Context()
    {
    }

    public FineDevDbV2Context(DbContextOptions<FineDevDbV2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AggregatedCounter> AggregatedCounters { get; set; }

    public virtual DbSet<Box> Boxes { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Counter> Counters { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<Fcmtoken> Fcmtokens { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Floor> Floors { get; set; }

    public virtual DbSet<Hash> Hashes { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobParameter> JobParameters { get; set; }

    public virtual DbSet<JobQueue> JobQueues { get; set; }

    public virtual DbSet<List> Lists { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Notify> Notifies { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderBox> OrderBoxes { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OtherAmount> OtherAmounts { get; set; }

    public virtual DbSet<Party> Parties { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }

    public virtual DbSet<ProductInMenu> ProductInMenus { get; set; }

    public virtual DbSet<Schema> Schemas { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<Set> Sets { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<Station> Stations { get; set; }

    public virtual DbSet<StationReport> StationReports { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=52.221.192.64;Database=FINE_dev_db_v2;User ID=finedb;Password=5HazQl5%82M7aj@b2AC5Q;MultipleActiveResultSets=true;Integrated Security=true;Trusted_Connection=False;Encrypt=True;TrustServerCertificate=True", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AccountCode).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Account_Customer");
        });

        modelBuilder.Entity<AggregatedCounter>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("PK_HangFire_CounterAggregated");

            entity.ToTable("AggregatedCounter", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_AggregatedCounter_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Box>(entity =>
        {
            entity.ToTable("Box");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Station).WithMany(p => p.Boxes)
                .HasForeignKey(d => d.StationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Box_Station");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCollection");

            entity.ToTable("Category");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(225);
            entity.Property(e => e.Name).HasMaxLength(225);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Counter>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Counter", "HangFire");

            entity.HasIndex(e => e.Key, "CX_HangFire_Counter").IsClustered();

            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.Key).HasMaxLength(100);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");

            entity.HasIndex(e => e.CustomerCode, "IX_Customer").IsUnique();

            entity.HasIndex(e => e.CustomerCode, "IX_Customer_1").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("Destination");

            entity.HasIndex(e => e.Code, "IX_Destination").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Lat).HasMaxLength(20);
            entity.Property(e => e.Long).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(225);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Fcmtoken>(entity =>
        {
            entity.ToTable("FCMToken");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Order");
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("Floor");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Destionation).WithMany(p => p.Floors)
                .HasForeignKey(d => d.DestionationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Floor_Destination");
        });

        modelBuilder.Entity<Hash>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Field }).HasName("PK_HangFire_Hash");

            entity.ToTable("Hash", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Hash_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Field).HasMaxLength(100);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Job");

            entity.ToTable("Job", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Job_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => e.StateName, "IX_HangFire_Job_StateName").HasFilter("([StateName] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.StateName).HasMaxLength(20);
        });

        modelBuilder.Entity<JobParameter>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Name }).HasName("PK_HangFire_JobParameter");

            entity.ToTable("JobParameter", "HangFire");

            entity.Property(e => e.Name).HasMaxLength(40);

            entity.HasOne(d => d.Job).WithMany(p => p.JobParameters)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_JobParameter_Job");
        });

        modelBuilder.Entity<JobQueue>(entity =>
        {
            entity.HasKey(e => new { e.Queue, e.Id }).HasName("PK_HangFire_JobQueue");

            entity.ToTable("JobQueue", "HangFire");

            entity.Property(e => e.Queue).HasMaxLength(50);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FetchedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_List");

            entity.ToTable("List", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_List_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menu");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.MenuName).HasMaxLength(100);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Menus)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Menu_TimeSlot");
        });

        modelBuilder.Entity<Notify>(entity =>
        {
            entity.ToTable("Notify");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Notifies)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notify_Customer");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order");

            entity.HasIndex(e => e.OrderCode, "IX_Order").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CheckInDate).HasColumnType("datetime");
            entity.Property(e => e.OrderCode).HasMaxLength(100);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Orders)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_TimeSlot");
        });

        modelBuilder.Entity<OrderBox>(entity =>
        {
            entity.ToTable("OrderBox");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Key).HasMaxLength(10);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Box).WithMany(p => p.OrderBoxes)
                .HasForeignKey(d => d.BoxId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderBox_Box");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderBoxes)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderBox_Order");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetail");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(225);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_Order");

            entity.HasOne(d => d.ProductInMenu).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductInMenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_ProductInMenu");

            entity.HasOne(d => d.Store).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_Store");
        });

        modelBuilder.Entity<OtherAmount>(entity =>
        {
            entity.ToTable("OtherAmount");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Order).WithMany(p => p.OtherAmounts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OtherAmount_Order");
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.ToTable("Party");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.PartyCode).HasMaxLength(10);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Parties)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Party_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.Parties)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Party_Order");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Order");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Products");

            entity.ToTable("Product");

            entity.HasIndex(e => e.ProductCode, "IX_Products").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(225);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Category");

            entity.HasOne(d => d.Store).WithMany(p => p.Products)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Store");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("ProductAttribute");

            entity.HasIndex(e => e.Code, "IX_ProductAttribute").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(225);
            entity.Property(e => e.Size).HasMaxLength(10);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttributes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAttribute_Product");
        });

        modelBuilder.Entity<ProductInMenu>(entity =>
        {
            entity.ToTable("ProductInMenu");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Menu).WithMany(p => p.ProductInMenus)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductInMenu_Menu");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductInMenus)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductInMenu_ProductAttribute");
        });

        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("PK_HangFire_Schema");

            entity.ToTable("Schema", "HangFire");

            entity.Property(e => e.Version).ValueGeneratedNever();
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Server");

            entity.ToTable("Server", "HangFire");

            entity.HasIndex(e => e.LastHeartbeat, "IX_HangFire_Server_LastHeartbeat");

            entity.Property(e => e.Id).HasMaxLength(200);
            entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Value }).HasName("PK_HangFire_Set");

            entity.ToTable("Set", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Set_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => new { e.Key, e.Score }, "IX_HangFire_Set_Score");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(256);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Station).WithMany(p => p.Staff)
                .HasForeignKey(d => d.StationId)
                .HasConstraintName("FK_Staff_Station");

            entity.HasOne(d => d.Store).WithMany(p => p.Staff)
                .HasForeignKey(d => d.StoreId)
                .HasConstraintName("FK_Staff_Store");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Id }).HasName("PK_HangFire_State");

            entity.ToTable("State", "HangFire");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(100);

            entity.HasOne(d => d.Job).WithMany(p => p.States)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_State_Job");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("Station");

            entity.HasIndex(e => e.Code, "IX_Station_1").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AreaCode).HasMaxLength(50);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Floor).WithMany(p => p.Stations)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Station_Floor");
        });

        modelBuilder.Entity<StationReport>(entity =>
        {
            entity.ToTable("StationReport");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Box).WithMany(p => p.StationReports)
                .HasForeignKey(d => d.BoxId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StationReport_Box");

            entity.HasOne(d => d.Staff).WithMany(p => p.StationReports)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StationReport_Staff");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Store");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ContactPerson).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.StoreName).HasMaxLength(225);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Destination).WithMany(p => p.Stores)
                .HasForeignKey(d => d.DestinationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Store_Destination");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.ToTable("TimeSlot");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Destination).WithMany(p => p.TimeSlots)
                .HasForeignKey(d => d.DestinationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TimeSlot_Destination");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transaction");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transaction_Account");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
