using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceRefactorToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Attendance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Attendance",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UserId",
                table: "Attendance",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_UserId",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Attendance");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
