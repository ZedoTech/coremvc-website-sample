using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using website.Models;

namespace website.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private UserManager<AppUser> userManager;
        private IUserValidator<AppUser> userValidator;
        private IPasswordValidator<AppUser> passwordValidator;
        private IPasswordHasher<AppUser> passwordHasher;

        public AdminController(UserManager<AppUser> usermgr, IUserValidator<AppUser> userValid, IPasswordValidator<AppUser> passValid, IPasswordHasher<AppUser> passwordHash)
        {
            userManager = usermgr;
            userValidator = userValid;
            passwordValidator = passValid;
            passwordHasher = passwordHash;
        }

        /// <summary>
        /// 顯示所有帳號
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View("UserList", userManager.Users);
        }

        /// <summary>
        /// 註冊帳號頁面
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ViewResult Create() => View();

        /// <summary>
        /// 接收用戶註冊資訊
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create(CreateModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new AppUser
                {
                    UserName = model.Name,
                    Email = model.Email
                };
                IdentityResult result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded) {
                    return RedirectToAction("Index");
                }
                else {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }

        /// <summary>
        /// 編輯帳號頁面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(string id) {
            AppUser user = await userManager.FindByIdAsync(id);
            if (user != null) {
                return View(user);
            }
            else {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 接收編輯帳號資訊
        /// </summary>
        /// <param name="id"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string username, string email, string password) {
            AppUser user = await userManager.FindByIdAsync(id);
            if (user != null) {
                user.UserName = username;
                user.Email = email;
                IdentityResult validEmail = await userValidator.ValidateAsync(userManager, user);
                if (!validEmail.Succeeded) {
                    AddErrorsFromResult(validEmail);
                }
                IdentityResult validPass = null;
                if (!string.IsNullOrEmpty(password)) {
                    validPass = await passwordValidator.ValidateAsync(userManager, user, password);
                    if (validPass.Succeeded) {
                        user.PasswordHash = passwordHasher.HashPassword(user, password);
                    }
                    else {
                        AddErrorsFromResult(validPass);
                    }
                }
                if ((validEmail.Succeeded && validPass == null) || (validEmail.Succeeded && password != string.Empty && validPass.Succeeded)) {
                    IdentityResult result = await userManager.UpdateAsync(user);
                    if (result.Succeeded) {
                        return RedirectToAction("Index");
                    }
                    else {
                        AddErrorsFromResult(result);
                    }
                }
            }
            else {
                ModelState.AddModelError("", "User Not Found");
            }
            return View(user);
        }


        /// <summary>
        /// 接收刪除帳號
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Delete(string id) {
            AppUser user = await userManager.FindByIdAsync(id);
            if (user != null) {
                IdentityResult result = await userManager.DeleteAsync(user);
                if (result.Succeeded) {
                    return RedirectToAction("Index");
                }
                else {
                    AddErrorsFromResult(result);
                }
            }
            else {
                ModelState.AddModelError("", "User Not Found");
            }
            return View("Index", userManager.Users); }

        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        /// <param name="result"></param>
        private void AddErrorsFromResult(IdentityResult result) {
            foreach (IdentityError error in result.Errors) {
                ModelState.AddModelError("", error.Description);
            }
        }
    }
}