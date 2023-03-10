using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FINE.Data.Entity;

public partial class FineStgDbContext : DbContext
{
    public FineStgDbContext()
    {
    }

    public FineStgDbContext(DbContextOptions<FineStgDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Campus> Campuses { get; set; }

    public virtual DbSet<CategoryStoreItem> CategoryStoreItems { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Fcmtoken> Fcmtokens { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Floor> Floors { get; set; }

    public virtual DbSet<MembershipCard> MembershipCards { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Notify> Notifies { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderFeedback> OrderFeedbacks { get; set; }

    public virtual DbSet<ParticipationOrder> ParticipationOrders { get; set; }

    public virtual DbSet<ParticipationOrderDetail> ParticipationOrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCollection> ProductCollections { get; set; }

    public virtual DbSet<ProductCollectionTimeSlot> ProductCollectionTimeSlots { get; set; }

    public virtual DbSet<ProductCombo> ProductCombos { get; set; }

    public virtual DbSet<ProductComboItem> ProductComboItems { get; set; }

    public virtual DbSet<ProductInMenu> ProductInMenus { get; set; }

    public virtual DbSet<ProductionCollectionItem> ProductionCollectionItems { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<StaffReport> StaffReports { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<StoreCategory> StoreCategories { get; set; }

    public virtual DbSet<SystemCategory> SystemCategories { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<University> Universities { get; set; }

    public virtual DbSet<UniversityInfo> UniversityInfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=18.140.30.255;Database=FINE_stg_db;User ID=finedb;Password=5HazQl5%82M7aj@b2AC5Q;MultipleActiveResultSets=true;Integrated Security=true;Trusted_Connection=False;Encrypt=True;TrustServerCertificate=True", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");

            entity.Property(e => e.AccountCode).HasMaxLength(50);
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.Balance).HasColumnType("money");
            entity.Property(e => e.FinishDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.MembershipCard).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.MembershipCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Account_MembershipCard");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("Area");

            entity.Property(e => e.AreaCode).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Campus).WithMany(p => p.Areas)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Area_Campus");
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.ToTable("BlogPost");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.Metadata).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Store).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPost_Store");
        });

        modelBuilder.Entity<Campus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Destination");

            entity.ToTable("Campus");

            entity.Property(e => e.Address).HasMaxLength(225);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(225);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.University).WithMany(p => p.Campuses)
                .HasForeignKey(d => d.UniversityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Campus_University");
        });

        modelBuilder.Entity<CategoryStoreItem>(entity =>
        {
            entity.ToTable("CategoryStoreItem");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.CategoryStoreItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryStoreItem_Product");

            entity.HasOne(d => d.StoreCategory).WithMany(p => p.CategoryStoreItems)
                .HasForeignKey(d => d.StoreCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryStoreItem_Collection");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");

            entity.HasIndex(e => e.CustomerCode, "IX_Customer").IsUnique();

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.UniInfo).WithMany(p => p.Customers)
                .HasForeignKey(d => d.UniInfoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_UniversityInfo");
        });

        modelBuilder.Entity<Fcmtoken>(entity =>
        {
            entity.ToTable("FCMToken");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Fcmtokens)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_FCMToken_Customer");

            entity.HasOne(d => d.Staff).WithMany(p => p.Fcmtokens)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_FCMToken_Staff");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("Floor");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Campus).WithMany(p => p.Floors)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Floor_Campus1");
        });

        modelBuilder.Entity<MembershipCard>(entity =>
        {
            entity.ToTable("MembershipCard");

            entity.Property(e => e.CardCode).HasMaxLength(50);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.PhysicalCardCode).HasMaxLength(50);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.MembershipCards)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MembershipCard_Customer");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menu");

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

            entity.Property(e => e.CheckInDate).HasColumnType("datetime");
            entity.Property(e => e.DeliveryPhone).HasMaxLength(50);
            entity.Property(e => e.OrderCode).HasMaxLength(30);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Order_Customer");

            entity.HasOne(d => d.GeneralOrder).WithMany(p => p.InverseGeneralOrder)
                .HasForeignKey(d => d.GeneralOrderId)
                .HasConstraintName("FK_Order_Order");

            entity.HasOne(d => d.Room).WithMany(p => p.Orders)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Room");

            entity.HasOne(d => d.Shipper).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShipperId)
                .HasConstraintName("FK_Order_Staff");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetail");

            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(225);

            entity.HasOne(d => d.Combo).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("FK_OrderDetail_ProductCombo");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_Order");

            entity.HasOne(d => d.ProductInMenu).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductInMenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_ProductInMenu");
        });

        modelBuilder.Entity<OrderFeedback>(entity =>
        {
            entity.ToTable("OrderFeedback");

            entity.Property(e => e.OrderFbContent).HasMaxLength(256);

            entity.HasOne(d => d.Customer).WithMany(p => p.OrderFeedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderFeedback_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderFeedbacks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderFeedback_Order");
        });

        modelBuilder.Entity<ParticipationOrder>(entity =>
        {
            entity.ToTable("ParticipationOrder");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.GeneralOrderCode).HasMaxLength(225);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.ParticipationOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipationOrder_Customer");

            entity.HasOne(d => d.GeneralOrder).WithMany(p => p.ParticipationOrders)
                .HasForeignKey(d => d.GeneralOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipationOrder_Order");
        });

        modelBuilder.Entity<ParticipationOrderDetail>(entity =>
        {
            entity.ToTable("ParticipationOrderDetail");

            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(225);

            entity.HasOne(d => d.Combo).WithMany(p => p.ParticipationOrderDetails)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("FK_ParticipationOrderDetail_ProductCombo");

            entity.HasOne(d => d.Customer).WithMany(p => p.ParticipationOrderDetails)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipationOrderDetail_Customer");

            entity.HasOne(d => d.ParticipationOrder).WithMany(p => p.ParticipationOrderDetails)
                .HasForeignKey(d => d.ParticipationOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipationOrderDetail_ParticipationOrder");

            entity.HasOne(d => d.ProductInMenu).WithMany(p => p.ParticipationOrderDetails)
                .HasForeignKey(d => d.ProductInMenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipationOrderDetail_ProductInMenu");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

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
            entity.ToTable("Product");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(225);
            entity.Property(e => e.Size).HasMaxLength(10);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_SystemCategory");

            entity.HasOne(d => d.GeneralProduct).WithMany(p => p.InverseGeneralProduct)
                .HasForeignKey(d => d.GeneralProductId)
                .HasConstraintName("FK_Product_Product");

            entity.HasOne(d => d.Store).WithMany(p => p.Products)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Store1");
        });

        modelBuilder.Entity<ProductCollection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCollection_1");

            entity.ToTable("ProductCollection");

            entity.Property(e => e.BannerUrl)
                .HasMaxLength(256)
                .HasColumnName("BannerURL");
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ProductCollectionTimeSlot>(entity =>
        {
            entity.ToTable("ProductCollectionTimeSlot");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ProductCollection).WithMany(p => p.ProductCollectionTimeSlots)
                .HasForeignKey(d => d.ProductCollectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductCollectionTimeSlot_ProductCollection1");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.ProductCollectionTimeSlots)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductCollectionTimeSlot_TimeSlot1");
        });

        modelBuilder.Entity<ProductCombo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCombination");

            entity.ToTable("ProductCombo");

            entity.Property(e => e.CombineName).HasMaxLength(225);
            entity.Property(e => e.ComboCode)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ProductComboItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCombinationItem");

            entity.ToTable("ProductComboItem");

            entity.HasOne(d => d.Combo).WithMany(p => p.ProductComboItems)
                .HasForeignKey(d => d.ComboId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductComboItem_ProductCombination");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductComboItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductComboItem_Product");
        });

        modelBuilder.Entity<ProductInMenu>(entity =>
        {
            entity.ToTable("ProductInMenu");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Menu).WithMany(p => p.ProductInMenus)
                .HasForeignKey(d => d.MenuId)
                .HasConstraintName("FK_ProductInMenu_Menu");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductInMenus)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductInMenu_Product");
        });

        modelBuilder.Entity<ProductionCollectionItem>(entity =>
        {
            entity.ToTable("ProductionCollectionItem");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.ProductCollection).WithMany(p => p.ProductionCollectionItems)
                .HasForeignKey(d => d.ProductCollectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionCollectionItem_ProductCollection1");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductionCollectionItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionCollectionItem_Product");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Area).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Area");

            entity.HasOne(d => d.Floor).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Floor");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Campus).WithMany(p => p.Staff)
                .HasForeignKey(d => d.CampusId)
                .HasConstraintName("FK_Staff_Campus");

            entity.HasOne(d => d.Customer).WithMany(p => p.Staff)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Staff_Customer");
        });

        modelBuilder.Entity<StaffReport>(entity =>
        {
            entity.ToTable("StaffReport");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffReports)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffReport_Staff");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Store");

            entity.Property(e => e.ContactPerson).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.StoreName).HasMaxLength(225);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Campus).WithMany(p => p.Stores)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Store_Destination");
        });

        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCollection");

            entity.ToTable("StoreCategory");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(225);
            entity.Property(e => e.Name).HasMaxLength(225);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Store).WithMany(p => p.StoreCategories)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Collection_Store");
        });

        modelBuilder.Entity<SystemCategory>(entity =>
        {
            entity.ToTable("SystemCategory");

            entity.Property(e => e.CategoryCode).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(225);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.ToTable("TimeSlot");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Campus).WithMany(p => p.TimeSlots)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TimeSlot_Destination");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transaction");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transaction_Account");
        });

        modelBuilder.Entity<University>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Brand");

            entity.ToTable("University");

            entity.Property(e => e.ContactEmail).HasMaxLength(225);
            entity.Property(e => e.ContactName).HasMaxLength(225);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UniCode).HasMaxLength(50);
            entity.Property(e => e.UniName).HasMaxLength(225);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<UniversityInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CampusInfo");

            entity.ToTable("UniversityInfo");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Domain).HasMaxLength(225);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.University).WithMany(p => p.UniversityInfos)
                .HasForeignKey(d => d.UniversityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UniversityInfo_University");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
