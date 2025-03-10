﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogWebsite.Migrations
{
	public partial class AddIsActiveOnApplicationUser : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "IsActive",
				table: "AspNetUsers",
				type: "bit",
				nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "IsActive",
				table: "AspNetUsers");
		}
	}
}
