﻿using AspNetCoreHero.ToastNotification.Abstractions;
using BlogWebsite.Data;
using BlogWebsite.Models;
using BlogWebsite.Utilites;
using BlogWebsite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class TagController : Controller
    {
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        public TagController(ApplicationDbContext context,
                              INotyfService notyfService,
                              IWebHostEnvironment webHostEnvironment,
                              UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _notification = notyfService;
        }

        [HttpGet("Tag")]
        public async Task<IActionResult> Index()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser);

            var listOfTag=await _context.tags!
                .ToListAsync();

            var listOfTagVM = listOfTag.Select(x => new TagVM
            {
                Id = x.Id,
                Name = x.Name,
            }).ToList();
            return View(listOfTagVM);
        }

        [HttpGet("CreateTag")]
        public IActionResult CreateTag()
        {
            return View(new CreateTagVM());
        }

		[HttpPost("CreateTag")]
		public async Task<IActionResult> CreateTag(CreateTagVM vm)
		{
			if (!ModelState.IsValid)
			{
				return View(vm);
			}

			var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);

			var tagExist = await _context.tags!.AnyAsync(x => x.Name == vm.Name);

			if (tagExist)
			{
				_notification.Error("This Tag Has Already Exist!");
				return RedirectToAction("Index");
			}
			else
			{
				var tag = new Tag
				{
					Name = vm.Name,
				};

				_context.tags!.Add(tag);
				await _context.SaveChangesAsync();
				_notification.Success("Tag Created Successfully!");
				return RedirectToAction("Index");
			}
		}
		[HttpPost]
		public async Task<IActionResult> DeleteTag(int id)
		{
			var tag = await _context.tags!.SingleOrDefaultAsync(x => x.Id == id);
			_context.tags!.Remove(tag!);
			await _context.SaveChangesAsync();
			_notification.Success("Tag Deleted Successfully!");
			return RedirectToAction("Index", "Tag", new { area = "Admin" });
		}
	}
}
