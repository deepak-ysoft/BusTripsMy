using Azure;
using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace BusTrips.Web.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _users;
        private readonly SignInManager<AppUser> _signIn;
        private readonly AppDbContext _db;
        private readonly UrlEncoder _urlEncoder;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;

        public AccountService(UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, UrlEncoder urlEncoder, IEmailSender emailSender, IConfiguration config)
        {
            _users = users;
            _signIn = signIn;
            _db = db;
            _urlEncoder = urlEncoder;
            _emailSender = emailSender;
            _config = config;
        }

        // Registers a new user with the specified role (default is "User").
        public async Task<ResponseVM<AppUser>> RegisterAsync(RegisterVm vm, string role = "User") 
        {
            if (vm.AcceptedUserTerms == false)
            {
                return new ResponseVM<AppUser> { IsSuccess = false, Message = "You must accept the terms and conditions to register." };
            }

            if (role == "Driver")
            {
                if (string.IsNullOrWhiteSpace(vm.LicenseNumber))
                    return new ResponseVM<AppUser> { IsSuccess = false, Message = "License Number is required for drivers." };
                if (string.IsNullOrWhiteSpace(vm.LicenseProvince))
                    return new ResponseVM<AppUser> { IsSuccess = false, Message = "License Province is required for drivers." };
            }

            var existingUser = await _users.Users
                .Where(u => u.Email.ToLower() == vm.Email.ToLower() || u.PhoneNumber == vm.PhoneNumber)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                if (existingUser.Email.ToLower() == vm.Email.ToLower())
                    return new ResponseVM<AppUser> { IsSuccess = false, Message = "This email address is already in use. Please use a different email." };
                else
                    return new ResponseVM<AppUser> { IsSuccess = false, Message = "This phone number is already linked to another account. Please use a different number." };
            }

            var user = new AppUser 
            {
                UserName = vm.Email,
                Email = vm.Email,
                SecondaryEmail = vm.SecondaryEmail,
                PhoneNumber = vm.PhoneNumber,
                PhoneNumber2 = vm.PhoneNumber2,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                AcceptedUserTerms = vm.AcceptedUserTerms
            };

            var res = await _users.CreateAsync(user, vm.Password); // Create user with hashed password

            if (!res.Succeeded)
            {
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = string.Join(", ", res.Errors.Select(e => e.Description))
                };
            }

            await _users.AddToRoleAsync(user, role);

            if (role == "Driver")
            {
                _db.BusDrivers.Add(new BusDriver
                {
                    Id = user.Id,
                    AppUserId = user.Id,
                    LicenseNumber = vm.LicenseNumber ?? "",
                    LicenseProvince = vm.LicenseProvince ?? ""
                });
                await _db.SaveChangesAsync();
            }

            return new ResponseVM<AppUser>
            {
                IsSuccess = true,
                Message = "Registration successful! Please check your email to confirm your account.",
                Data = user
            };
        }

        public async Task<ResponseVM<AppUser>> AddUserAsync(AddUserVm vm, Guid userId) // Inviting userId is the one who is adding new user
        {
            if (await _users.Users.AnyAsync(u => u.PhoneNumber == vm.PhoneNumber))
            {
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = "The phone number you entered is already associated with another account.",
                };
            }

            var userMembership = await _db.OrganizationMemberships.FirstOrDefaultAsync(m => m.OrganizationId == vm.OrgId && !m.IsDeleted); // Check if the inviting user is part of the organization

            if (userMembership == null)
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = "Organization not found."
                };

            if (userMembership.MemberType != MemberTypeEnum.Creator && (vm.MemberType == MemberTypeEnum.Creator || vm.MemberType == MemberTypeEnum.Admin))
            {
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = "You can not create Creator"
                };
            }

            if (userMembership.MemberType != MemberTypeEnum.Admin
                && userMembership.MemberType != MemberTypeEnum.Creator
                && vm.MemberType == MemberTypeEnum.Admin)
            {
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = "You can not create Admin"
                };
            }


            var user = new AppUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
            };

            var res = await _users.CreateAsync(user, vm.Password); // Create user with hashed password
            if (!res.Succeeded)
            {
                foreach (var error in res.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Description.Contains("username", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ResponseVM<AppUser>
                        {
                            IsSuccess = false,
                            Message = "This user already has an account. Please send them an invitation to join your organization via email."
                        };
                    }
                }
                return new ResponseVM<AppUser>
                {
                    IsSuccess = false,
                    Message = string.Join(", ", res.Errors.Select(e => e.Description))
                };
            }

            await _users.AddToRoleAsync(user, "User"); // New users added by admin are always "User"

            if (!await _db.OrganizationMemberships.AnyAsync(m => m.OrganizationId == vm.OrgId && m.AppUserId == user.Id))
            {
                _db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    OrganizationId = vm.OrgId,
                    AppUserId = user.Id,
                    MemberType = vm.MemberType?? MemberTypeEnum.ReadOnly,
                    IsInvited = true,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                });
                await _db.SaveChangesAsync();
            }
            return new ResponseVM<AppUser> { IsSuccess = true, Message = "User Created", Data = user };
        }

        public async Task<ResponseVM<string>> LoginAsync(LoginVm vm, string pathRole) // pathRole is the role required to access the requested path
        {
            // Find user by email
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == vm.Email); 

            if (user is null)
            {
                return new ResponseVM<string>
                {
                    IsSuccess = false,
                    Message = "No account found with this email address."
                };
            }

            // Email confirmation check
            if (!user.EmailConfirmed)
            {
                return new ResponseVM<string>
                {
                    IsSuccess = false,
                    Message = "Confirm Email"
                };
            }

            // Check user role against the requested path
            var roles = await _users.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            if (pathRole != userRole)
            {
                return new ResponseVM<string>
                {
                    IsSuccess = false,
                    Message = $"You do not have permission to access this section. Required role: {pathRole}, your role: {userRole}."
                };
            }

            // Check password
            var result = await _signIn.PasswordSignInAsync(user, vm.Password, isPersistent: true, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return new ResponseVM<string>
                {
                    IsSuccess = false,
                    Message = "The password you entered is incorrect. Please try again or reset your password if you forgot it."
                };
            }

            // Successful login
            return new ResponseVM<string>
            {
                IsSuccess = true,
                Message = "You have successfully logged in!",
                Data = userRole
            };
        }

        public async Task LogoutAsync() // Logs out the current user
        {
            await _signIn.SignOutAsync();
        }

        public async Task<ResponseVM<List<TermsAndConditionResponseVM>>> GetTermsAndConditionsAsync(string role) // role can be "User" or "Driver"
        {
            var terms = await _db.TermsAndConditions.OrderByDescending(x=>x.CreatedAt) 
                .Where(t => !t.IsDeleted && t.TermsFor == role)
                .Select(t => new TermsAndConditionResponseVM
                {
                    Id = t.Id,
                    TermsFor = t.TermsFor,
                    Title = t.Title,
                    Content = t.Content,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
            return new ResponseVM<List<TermsAndConditionResponseVM>>
            {
                IsSuccess = true,
                Data = terms
            };
        }

        public async Task<ResponseVM<string>> AddEditTermsAndConditionAsync(TermsAndConditionRequestVM vm, Guid userId) // userId is the admin/creator making the change
        {
            if (string.IsNullOrWhiteSpace(vm.Title) || string.IsNullOrWhiteSpace(vm.Content) || string.IsNullOrWhiteSpace(vm.TermsFor))
            {
                return new ResponseVM<string> { IsSuccess = false, Message = "Title, Content, and TermsFor are required." };
            }
            TermsAndConditions? terms;
            if (vm.Id != null && vm.Id != Guid.Empty)
            {
                terms = await _db.TermsAndConditions.FirstOrDefaultAsync(t => t.Id == vm.Id && !t.IsDeleted);
                if (terms == null)
                {
                    return new ResponseVM<string> { IsSuccess = false, Message = "Terms and Conditions not found." };
                }
                terms.Title = vm.Title;
                terms.Content = vm.Content;
                terms.TermsFor = vm.TermsFor;
                terms.UpdatedBy = userId;
                terms.UpdatedAt = DateTime.Now;
            }
            else
            {
                terms = new TermsAndConditions
                {
                    Id = Guid.NewGuid(),
                    Title = vm.Title,
                    Content = vm.Content,
                    TermsFor = vm.TermsFor,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                };
                await _db.TermsAndConditions.AddAsync(terms);
            }
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Terms and Conditions saved successfully." }; 
        }

        public async Task<ResponseVM<string>> DeleteTermsAndConditionAsync(Guid id, Guid userId) // userId is the admin/creator making the change
        {
            var terms = await _db.TermsAndConditions.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (terms == null)
            {
                return new ResponseVM<string> { IsSuccess = false, Message = "Terms and Conditions not found." };
            }
            terms.IsDeleted = true;
            terms.UpdatedBy = userId;
            terms.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Terms and Conditions deleted successfully." };
        }

    }
}
