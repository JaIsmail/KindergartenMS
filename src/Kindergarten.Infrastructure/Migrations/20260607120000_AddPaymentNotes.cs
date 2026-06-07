using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Kindergarten.Infrastructure.Migrations
{
    public partial class AddPaymentNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'Notes')
                    ALTER TABLE Payments ADD Notes nvarchar(max) NOT NULL DEFAULT ''
            ");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'Notes')
                    ALTER TABLE Payments DROP COLUMN Notes
            ");
        }
    }
}
