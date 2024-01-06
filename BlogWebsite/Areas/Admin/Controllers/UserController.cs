﻿using AspNetCoreHero.ToastNotification.Abstractions;
using BlogWebsite.Data;
using BlogWebsite.Models;
using BlogWebsite.Utilites;
using BlogWebsite.ViewModels;
using EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogWebsite.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class UserController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly INotyfService _notification;
		private readonly IEmailSender _emailSender;
		private readonly EmailConfiguration _emailConfig;

		public UserController(UserManager<ApplicationUser> userManager,
							  SignInManager<ApplicationUser> signInManager,
							  INotyfService notyfService, IEmailSender emailSender,
							  EmailConfiguration emailConfig,
							  ApplicationDbContext context)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_notification = notyfService;
			_emailSender = emailSender;
			_emailConfig = emailConfig;
			_context = context;
		}

		[Authorize(Roles = "Admin")]
		[HttpGet("User")]
		public async Task<IActionResult> Index()
		{
			var users = await _userManager.Users.ToListAsync();
			var vm = users.Select(x => new UserVM()
			{
				Id = x.Id,
				FirstName = x.FirstName,
				LastName = x.LastName,
				UserName = x.UserName,
				Email = x.Email
			}).ToList();
			foreach (var user in vm)
			{
				var singleUser = await _userManager.FindByIdAsync(user.Id);
				var role = await _userManager.GetRolesAsync(singleUser);
				user.Role = role.FirstOrDefault();
			}

			return View(vm);
		}

		[HttpGet("ForgotPassword")]
		public IActionResult ForgotPassword()
		{
			return View(new ForgotPasswordVM());
		}

		[HttpPost("ForgotPassword")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordVM vm)
		{
			if (!ModelState.IsValid)
				return View(vm);
			var user = await _userManager.FindByEmailAsync(vm.Email);
			var checkUserByEmail = await _userManager.FindByEmailAsync(vm.Email);
			if (user == null)
			{
				return RedirectToAction(nameof(ForgotPasswordConfirmationError));
			}
			else
			{
				var token = await _userManager.GeneratePasswordResetTokenAsync(user);
				var callback = Url.Action("ResetPassword", "User", new { token, email = user.Email }, Request.Scheme);
				var message = new Message(_emailConfig, new string[] { user.Email }, "Reset password link", callback!, null!);
				await _emailSender.SendEmailAsync(message);
				return RedirectToAction(nameof(ForgotPasswordConfirmation));
			}

		}

		[HttpGet("ForgotPasswordConfirmation")]
		public IActionResult ForgotPasswordConfirmation()
		{
			return View();
		}

		[HttpGet("ForgotPasswordConfirmationError")]
		public IActionResult ForgotPasswordConfirmationError()
		{
			return View();
		}

		[HttpGet("Register")]
		public IActionResult Register()
		{
			return View(new RegisterVM());
		}

		[HttpPost("Register")]
		public async Task<IActionResult> Register(RegisterVM vm)
		{
			if (!ModelState.IsValid)
			{
				return View(vm);
			}

			var checkUserByEmail = await _userManager.FindByEmailAsync(vm.Email);
			if (checkUserByEmail != null)
			{
				_notification.Error("This Email Already Exist!");
				return View(vm);
			}
			var checkUserByUsername = await _userManager.FindByNameAsync(vm.UserName);
			if (checkUserByUsername != null)
			{
				_notification.Error("This UserName Already Exist!");
				return View(vm);
			}

			var applicationUser = new ApplicationUser()
			{
				FirstName = vm.FirstName,
				LastName = vm.LastName,
				Email = vm.Email,
				UserName = vm.UserName
			};

			var result = await _userManager.CreateAsync(applicationUser, vm.Password);

			if (result.Succeeded)
			{
				await _userManager.AddToRoleAsync(applicationUser, WebsiteRole.WebisteAuthor);
				_notification.Success("User Created Successfully!");
				return RedirectToAction("Login", "User", new { area = "Admin" });
			}
			return View(vm);

		}

		[HttpGet("ResetPassword")]
		public IActionResult ResetPassword(string token, string email)
		{
			var model = new ResetPasswordVM { Token = token, Email = email };
			return View(model);
		}

		[HttpPost("ResetPassword")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
		{
			if (!ModelState.IsValid)
				return View(vm);

			var user = await _userManager.FindByEmailAsync(vm.Email);
			if (user == null)
				RedirectToAction(nameof(ResetPasswordConfirmation));

			var resetPassResult = await _userManager.ResetPasswordAsync(user!, vm.Token, vm.Password);
			if (!resetPassResult.Succeeded)
			{
				foreach (var error in resetPassResult.Errors)
				{
					ModelState.TryAddModelError(error.Code, error.Description);
				}

				return View();
			}

			return RedirectToAction(nameof(ResetPasswordConfirmation));
		}

		[HttpGet("ResetPasswordConfirmation")]
		public IActionResult ResetPasswordConfirmation()
		{
			return View();
		}

		[HttpGet("Login")]
		public IActionResult Login()
		{
			if (!HttpContext.User.Identity!.IsAuthenticated)
			{
				return View(new LoginVM());
			}
			return RedirectToAction("Index", "Home", new { area = "Default" });
		}

		[HttpPost("Login")]
		public async Task<IActionResult> Login(LoginVM vm)
		{
			if (!ModelState.IsValid)
			{
				return View(vm);
			}

			var user = await _userManager.FindByEmailAsync(vm.Username);
			if (user == null)
			{
				user = await _userManager.FindByNameAsync(vm.Username);
				if (user == null)
				{
					_notification.Error("Username or Email does not exist");
					return View(vm);
				}
			}

			var signInResult = await _signInManager.PasswordSignInAsync(user.UserName, vm.Password, vm.RememberMe, true);
			if (signInResult.Succeeded)
			{
				_notification.Success("Logged In Successfully!");
				return RedirectToAction("Index", "Home", new { area = "Default" });
			}
			else
			{
				_notification.Error("Invalid password");
				return View(vm);
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteUser(string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);

			if (user == null)
			{
				_notification.Error("This User Is Not Exist!");
			}

			var post=await _context.posts!.Where(p=>p.ApplicationUserId == userId).Include(p=>p.Comments).ToListAsync();
			if (post.Count > 0)
			{
				_context.posts!.RemoveRange(post);
				await _context.SaveChangesAsync();
			}
			var fpost = await _context.forumPosts!.Where(p => p.ApplicationUserId == userId).Include(p=>p.Answer).ToListAsync();
			if (fpost.Count > 0)
			{
				_context.forumPosts!.RemoveRange(fpost);
				await _context.SaveChangesAsync();
			}

			var commment = await _context.comments!.Where(c=>c.ApplicationUserId == userId).ToListAsync();
			if(commment.Count > 0)
			{
				_context.comments!.RemoveRange(commment);
				await _context.SaveChangesAsync();
			}

			var result = await _userManager.DeleteAsync(user!);
			if (result.Succeeded)
			{
				_notification.Success("Delete User Successfully!");
				return RedirectToAction("Index", "User", new { area = "Admin" });

			}
			else
			{
				_notification.Error("Delete User Fail!");
				return RedirectToAction("Index", "User", new { area = "Admin" });


			}
		}

		[HttpPost]
		[Authorize]
		public IActionResult Logout()
		{
			_signInManager.SignOutAsync();
			_notification.Success("You logged out successfully!");
			return RedirectToAction("Index", "Home", new { area = "Default" });
		}
	}
}
