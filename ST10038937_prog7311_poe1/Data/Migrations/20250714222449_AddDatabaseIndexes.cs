using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST10038937_prog7311_poe1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_ProductionDate",
                table: "Products",
                columns: new[] { "Category", "ProductionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_FarmerId_Category",
                table: "Products",
                columns: new[] { "FarmerId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductionDate",
                table: "Products",
                column: "ProductionDate");

            migrationBuilder.CreateIndex(
                name: "IX_PostReplies_CreatedAt",
                table: "PostReplies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PostReplies_ForumPostId_CreatedAt",
                table: "PostReplies",
                columns: new[] { "ForumPostId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_CreatedAt",
                table: "ForumPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Farmers_Email",
                table: "Farmers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Farmers_Name",
                table: "Farmers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category_ProductionDate",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_FarmerId_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductionDate",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PostReplies_CreatedAt",
                table: "PostReplies");

            migrationBuilder.DropIndex(
                name: "IX_PostReplies_ForumPostId_CreatedAt",
                table: "PostReplies");

            migrationBuilder.DropIndex(
                name: "IX_ForumPosts_CreatedAt",
                table: "ForumPosts");

            migrationBuilder.DropIndex(
                name: "IX_Farmers_Email",
                table: "Farmers");

            migrationBuilder.DropIndex(
                name: "IX_Farmers_Name",
                table: "Farmers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs");
        }
    }
}
