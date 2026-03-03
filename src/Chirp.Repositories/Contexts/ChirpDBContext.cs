using Chirp.Core.Classes;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Chirp.Repositories.Repositories;
public class ChirpDBContext(DbContextOptions<ChirpDBContext> options) : IdentityDbContext<Author>(options)
{
	public DbSet<Cheep> Cheeps { get; set; }
	public DbSet<Author> Authors { get; set; }
	public DbSet<Follow> Follows { get; set; }
	public DbSet<Comment> Comments { get; set; }


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Configure entity relationships and other configurations here
		modelBuilder.Entity<Cheep>()
			.HasOne(c => c.Author)
			.WithMany(a => a.Cheeps)
			.HasForeignKey(c => c.AuthorId);

		// Enforce string length constraint on the Text property
		modelBuilder.Entity<Cheep>()
			.Property(c => c.Text)
			.HasMaxLength(160)
			.IsRequired();

		// Setting the composite key for the follows table
		modelBuilder.Entity<Follow>()
			.HasKey(f => new { f.FollowerId, f.FollowedId });

		modelBuilder.Entity<Follow>()
			.HasOne(f => f.Follower)
			.WithMany(a => a.Following)
			.HasForeignKey(f => f.FollowerId);

		modelBuilder.Entity<Follow>()
			.HasOne(f => f.Followed)
			.WithMany(a => a.Followers)
			.HasForeignKey(f => f.FollowedId);

		modelBuilder.Entity<Comment>()
			.HasOne(c => c.Author)
			.WithMany(a => a.Comments)
			.HasForeignKey(c => c.AuthorId);

		// Value converters for SQLite-originated migrations (text columns in PostgreSQL)
		var dateTimeConverter = new ValueConverter<DateTime, string>(
			v => v.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
			v => DateTime.Parse(v));

		var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, string?>(
			v => v == null ? null : v.Value.ToString("o"),
			v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v));

		modelBuilder.Entity<Cheep>()
			.Property(c => c.TimeStamp)
			.HasConversion(dateTimeConverter);

		modelBuilder.Entity<Comment>()
			.Property(c => c.TimeStamp)
			.HasConversion(dateTimeConverter);

		modelBuilder.Entity<Author>()
			.Property(a => a.LockoutEnd)
			.HasConversion(dateTimeOffsetConverter);

		// Boolean columns stored as integer (SQLite-originated migrations)
		var boolToIntConverter = new ValueConverter<bool, int>(
			v => v ? 1 : 0,
			v => v != 0);

		modelBuilder.Entity<Author>()
			.Property(a => a.EmailConfirmed)
			.HasConversion(boolToIntConverter);
		modelBuilder.Entity<Author>()
			.Property(a => a.PhoneNumberConfirmed)
			.HasConversion(boolToIntConverter);
		modelBuilder.Entity<Author>()
			.Property(a => a.TwoFactorEnabled)
			.HasConversion(boolToIntConverter);
		modelBuilder.Entity<Author>()
			.Property(a => a.LockoutEnabled)
			.HasConversion(boolToIntConverter);

		var imagesConverter = new ValueConverter<List<string>?, string>(
			static v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
			static v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>());

		var imagesComparer = new ValueComparer<List<string>?>(
			(l, r) => (l == null && r == null) || (l != null && r != null && l.SequenceEqual(r)),
			v => v == null ? 0 : v.Aggregate(0, (a, s) => HashCode.Combine(a, s == null ? 0 : s.GetHashCode())),
			v => v == null ? null : v.ToList());

		var imagesProperty = modelBuilder.Entity<Cheep>()
			.Property(c => c.Images)
			.HasConversion(imagesConverter);
		imagesProperty.Metadata.SetValueComparer(imagesComparer);
	}
}