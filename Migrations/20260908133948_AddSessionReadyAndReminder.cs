using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoChatingApp.WebRTC.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionReadyAndReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReminderSent",
                table: "TutoringSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSent",
                table: "TutoringSessions");
        }
    }
}
