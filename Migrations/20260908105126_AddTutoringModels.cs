using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VideoChatingApp.WebRTC.Migrations
{
    /// <inheritdoc />
    public partial class AddTutoringModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SubjectTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    HourlyRate = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExperienceYears = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAcceptingStudents = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutoringSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeacherId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectTagId = table.Column<int>(type: "INTEGER", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TeacherNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutoringSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutoringSessions_SubjectTags_SubjectTagId",
                        column: x => x.SubjectTagId,
                        principalTable: "SubjectTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TutoringSessions_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutoringSessions_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeacherProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_TeacherProfiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "TeacherProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeacherProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_SubjectTags_SubjectTagId",
                        column: x => x.SubjectTagId,
                        principalTable: "SubjectTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_TeacherProfiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "TeacherProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutoringReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutoringReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutoringReviews_TutoringSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TutoringSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TutoringReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SubjectTags",
                columns: new[] { "Id", "Description", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, "Algebra, Calculus, Statistics, Geometry", "📐", "Mathematics" },
                    { 2, "Mechanics, Thermodynamics, Electromagnetism", "⚛️", "Physics" },
                    { 3, "Organic, Inorganic, Physical Chemistry", "🧪", "Chemistry" },
                    { 4, "Cell Biology, Genetics, Ecology", "🧬", "Biology" },
                    { 5, "Programming, Algorithms, Data Structures", "💻", "Computer Science" },
                    { 6, "Grammar, Writing, Literature", "📝", "English" },
                    { 7, "Conversational, Grammar, Writing", "🗣️", "Spanish" },
                    { 8, "Conversational, Grammar, Writing", "🥐", "French" },
                    { 9, "World History, US History, European History", "📜", "History" },
                    { 10, "Micro, Macroeconomics, Finance", "📊", "Economics" },
                    { 11, "Piano, Guitar, Theory, Vocal", "🎵", "Music" },
                    { 12, "Drawing, Painting, Digital Art", "🎨", "Art" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTags_Name",
                table: "SubjectTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilities_TeacherProfileId_DayOfWeek",
                table: "TeacherAvailabilities",
                columns: new[] { "TeacherProfileId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfiles_IsAcceptingStudents",
                table: "TeacherProfiles",
                column: "IsAcceptingStudents");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfiles_UserId",
                table: "TeacherProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_SubjectTagId",
                table: "TeacherSubjects",
                column: "SubjectTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherProfileId_SubjectTagId",
                table: "TeacherSubjects",
                columns: new[] { "TeacherProfileId", "SubjectTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TutoringReviews_Rating",
                table: "TutoringReviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringReviews_ReviewerId",
                table: "TutoringReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringReviews_SessionId",
                table: "TutoringReviews",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TutoringSessions_ScheduledAt",
                table: "TutoringSessions",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringSessions_Status",
                table: "TutoringSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringSessions_StudentId",
                table: "TutoringSessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringSessions_SubjectTagId",
                table: "TutoringSessions",
                column: "SubjectTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TutoringSessions_TeacherId",
                table: "TutoringSessions",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherAvailabilities");

            migrationBuilder.DropTable(
                name: "TeacherSubjects");

            migrationBuilder.DropTable(
                name: "TutoringReviews");

            migrationBuilder.DropTable(
                name: "TeacherProfiles");

            migrationBuilder.DropTable(
                name: "TutoringSessions");

            migrationBuilder.DropTable(
                name: "SubjectTags");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
