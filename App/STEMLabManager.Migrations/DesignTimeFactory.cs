using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using STEMLabManager.Infrastructure;
namespace STEMLabManager.Migrations;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<LabDbContext>
{
    public LabDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<LabDbContext>().UseSqlite("Data Source=:memory:").Options);
}
