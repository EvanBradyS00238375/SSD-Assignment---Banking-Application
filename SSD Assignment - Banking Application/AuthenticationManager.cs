using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.DirectoryServices.AccountManagement;

namespace Banking_Application
{
    public class AuthenticationManager
    {
        private const string DOMAIN_NAME = "ITSLIGO.LAN";
        private const string BANK_TELLER_GROUP = "Bank Teller";
        private const string BANK_TELLER_ADMIN_GROUP = "Bank Teller Administrator";

        private PrincipalContext domainContext;
        private AuditLogger auditLogger;

        public string CurrentUsername { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsBankTeller { get; private set; }
        public bool IsAdministrator { get; private set; }

        public AuthenticationManager()
        {
            auditLogger = AuditLogger.GetInstance();
            IsAuthenticated = false;
            IsBankTeller = false;
            IsAdministrator = false;
        }

        public bool AuthenticateUser(string username, string password)
        {
            try
            {
                domainContext = new PrincipalContext(ContextType.Domain, DOMAIN_NAME);

                bool validCredentials = domainContext.ValidateCredentials(username, password);

                if (!validCredentials)
                {
                    auditLogger.LogFailedLogin(username, "Invalid credentials");

                    Console.WriteLine("Authentication Failed: Invalid username or password.");
                    IsAuthenticated = false;
                    return false;
                }

                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(
                    domainContext,
                    IdentityType.SamAccountName,
                    username
                );

                if (userPrincipal == null)
                {
                    auditLogger.LogFailedLogin(username, "User principal not found");
                    Console.WriteLine("Authentication Failed: User account not found.");
                    IsAuthenticated = false;
                    return false;
                }

                IsBankTeller = userPrincipal.IsMemberOf(
                    domainContext,
                    IdentityType.SamAccountName,
                    BANK_TELLER_GROUP
                );

                IsAdministrator = userPrincipal.IsMemberOf(
                    domainContext,
                    IdentityType.SamAccountName,
                    BANK_TELLER_ADMIN_GROUP
                );

                if (!IsBankTeller)
                {
                    auditLogger.LogFailedLogin(username, $"User not member of '{BANK_TELLER_GROUP}' group");

                    Console.WriteLine($"Access Denied: User '{username}' is not a member of the '{BANK_TELLER_GROUP}' group.");
                    Console.WriteLine("Only authorized Bank Tellers can access this application.");

                    IsAuthenticated = false;
                    return false;
                }

                CurrentUsername = username;
                IsAuthenticated = true;

                auditLogger.LogSuccessfulLogin(username, IsBankTeller, IsAdministrator);

                Console.WriteLine($"Authentication Successful: Welcome, {username}!");
                Console.WriteLine($"Role: Bank Teller" + (IsAdministrator ? " Administrator" : ""));

                return true;
            }
            catch (Exception ex)
            {
                auditLogger.LogFailedLogin(username, $"Exception: {ex.Message}");
                Console.WriteLine($"Authentication Error: {ex.Message}");
                Console.WriteLine("Please ensure you are connected to the ITSLIGO.LAN domain.");

                IsAuthenticated = false;
                return false;
            }
        }

        public bool RequestAdministratorApproval(string accountNo, string accountHolderName)
        {
            Console.WriteLine("");
            Console.WriteLine("ADMINISTRATOR APPROVAL REQUIRED");
            Console.WriteLine($"Account to be deleted: {accountNo}");
            Console.WriteLine($"Account holder: {accountHolderName}");
            Console.WriteLine("");
            Console.WriteLine("This action requires administrator credentials.");
            Console.WriteLine("Please enter administrator username and password:");
            Console.WriteLine("");

            Console.Write("Administrator Username: ");
            string adminUsername = Console.ReadLine();

            Console.Write("Administrator Password: ");
            string adminPassword = ReadPassword();
            Console.WriteLine(); 

            try
            {
                PrincipalContext adminContext = new PrincipalContext(ContextType.Domain, DOMAIN_NAME);
                bool validAdminCreds = adminContext.ValidateCredentials(adminUsername, adminPassword);

                if (!validAdminCreds)
                {
                    auditLogger.LogFailedAdminApproval(CurrentUsername, adminUsername, accountNo, "Invalid admin credentials");
                    Console.WriteLine("Administrator approval DENIED: Invalid credentials.");
                    return false;
                }

                UserPrincipal adminPrincipal = UserPrincipal.FindByIdentity(
                    adminContext,
                    IdentityType.SamAccountName,
                    adminUsername
                );

                if (adminPrincipal == null)
                {
                    auditLogger.LogFailedAdminApproval(CurrentUsername, adminUsername, accountNo, "Admin principal not found");
                    Console.WriteLine("Administrator approval DENIED: User not found.");
                    return false;
                }

                bool isAdmin = adminPrincipal.IsMemberOf(
                    adminContext,
                    IdentityType.SamAccountName,
                    BANK_TELLER_ADMIN_GROUP
                );

                if (!isAdmin)
                {
                    auditLogger.LogFailedAdminApproval(CurrentUsername, adminUsername, accountNo,
                        $"User not member of '{BANK_TELLER_ADMIN_GROUP}' group");

                    Console.WriteLine($"Administrator approval DENIED: User '{adminUsername}' is not a member of the '{BANK_TELLER_ADMIN_GROUP}' group.");
                    return false;
                }

                auditLogger.LogSuccessfulAdminApproval(CurrentUsername, adminUsername, accountNo, accountHolderName);
                Console.WriteLine($"Administrator approval GRANTED by {adminUsername}.");
                Console.WriteLine("Account deletion authorized.");

                return true;
            }
            catch (Exception ex)
            {
                auditLogger.LogFailedAdminApproval(CurrentUsername, adminUsername, accountNo, $"Exception: {ex.Message}");
                Console.WriteLine($"Administrator approval error: {ex.Message}");
                return false;
            }
        }

        private string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\b \b"); 
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*"); 
                }
            } while (key.Key != ConsoleKey.Enter);

            return password;
        }
    }
}