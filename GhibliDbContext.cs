using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;  // Añadir esta línea

public class GhibliDbContext : DbContext
{
    public GhibliDbContext(DbContextOptions<GhibliDbContext> options) : base(options) {}

    public DbSet<Comment> Comments { get; set; }
}

public class Comment
{
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;  // Inicialización segura

    [Required]
    [StringLength(500)]
    public string Content { get; set; } = string.Empty;  // Inicialización segura

    [Required]
    public string IPAddress { get; set; } = string.Empty;  // Inicialización segura

    [Required]
    public string BrowserInfo { get; set; } = string.Empty;  // Inicialización segura

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

