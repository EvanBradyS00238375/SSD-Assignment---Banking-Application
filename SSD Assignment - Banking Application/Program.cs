using System;
using System.Collections.Generic;
using System.Linq;

namespace Banking_Application
{
    public class Program
    {
        private static AuthenticationManager authManager;

        public static void Main(string[] args)
        {
            Console.WriteLine("***SSD BANKING APPLICATION***");
            Console.WriteLine("Active Directory Authentication Required");
            Console.WriteLine("");

            authManager = new AuthenticationManager();

            bool authenticated = false;
            int loginAttempts = 0;
            const int MAX_LOGIN_ATTEMPTS = 3;

            while (!authenticated && loginAttempts < MAX_LOGIN_ATTEMPTS)
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();

                Console.Write("Password: ");
                string password = ReadPassword();
                Console.WriteLine();
                Console.WriteLine("");

                authenticated = authManager.AuthenticateUser(username, password);

                if (!authenticated)
                {
                    loginAttempts++;
                    int remainingAttempts = MAX_LOGIN_ATTEMPTS - loginAttempts;

                    if (remainingAttempts > 0)
                    {
                        Console.WriteLine($"Login attempts remaining: {remainingAttempts}");
                        Console.WriteLine("");
                    }
                    else
                    {
                        Console.WriteLine("Maximum login attempts exceeded. Application will now exit.");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        Environment.Exit(1);
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine($"Welcome, {authManager.CurrentUsername}!");
            Console.WriteLine("");

            Data_Access_Layer dal = Data_Access_Layer.getInstance();
            dal.loadBankAccounts();
            bool running = true;

            do
            {
                Console.WriteLine("");
                Console.WriteLine("***Banking Application Menu***");
                Console.WriteLine("1. Add Bank Account");
                Console.WriteLine("2. Close Bank Account");
                Console.WriteLine("3. View Account Information");
                Console.WriteLine("4. Make Lodgement");
                Console.WriteLine("5. Make Withdrawal");
                Console.WriteLine("6. Exit");
                Console.WriteLine("CHOOSE OPTION:");
                String option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        HandleAccountCreation(dal);
                        break;
                    case "2":
                        HandleAccountClosure(dal);
                        break;
                    case "3":
                        HandleBalanceQuery(dal);
                        break;
                    case "4":
                        HandleLodgement(dal);
                        break;
                    case "5":
                        HandleWithdrawal(dal);
                        break;
                    case "6":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                        break;
                }

            } while (running != false);

            Console.WriteLine("Thank you for using SSD Banking Application.");
        }
        private static string ReadPassword()
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

        private static void HandleAccountCreation(Data_Access_Layer dal)
        {
            String accountType = "";
            int loopCount = 0;

            do
            {
                if (loopCount > 0)
                    Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");

                Console.WriteLine("");
                Console.WriteLine("***Account Types***:");
                Console.WriteLine("1. Current Account.");
                Console.WriteLine("2. Savings Account.");
                Console.WriteLine("CHOOSE OPTION:");
                accountType = Console.ReadLine();

                loopCount++;

            } while (!(accountType.Equals("1") || accountType.Equals("2")));

            String name = "";
            loopCount = 0;

            do
            {
                if (loopCount > 0)
                    Console.WriteLine("INVALID NAME ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Name: ");
                name = Console.ReadLine();

                loopCount++;

            } while (name.Equals(""));

            String addressLine1 = "";
            loopCount = 0;

            do
            {
                if (loopCount > 0)
                    Console.WriteLine("INVALID ÀDDRESS LINE 1 ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Address Line 1: ");
                addressLine1 = Console.ReadLine();

                loopCount++;

            } while (addressLine1.Equals(""));

            Console.WriteLine("Enter Address Line 2: ");
            String addressLine2 = Console.ReadLine();

            Console.WriteLine("Enter Address Line 3: ");
            String addressLine3 = Console.ReadLine();

            String town = "";
            loopCount = 0;

            do
            {
                if (loopCount > 0)
                    Console.WriteLine("INVALID TOWN ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Town: ");
                town = Console.ReadLine();

                loopCount++;

            } while (town.Equals(""));

            double balance = -1;
            loopCount = 0;

            do
            {
                if (loopCount > 0)
                    Console.WriteLine("INVALID OPENING BALANCE ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Opening Balance: ");
                String balanceString = Console.ReadLine();

                try
                {
                    balance = Convert.ToDouble(balanceString);
                }
                catch
                {
                    loopCount++;
                }

            } while (balance < 0);

            Bank_Account ba;

            if (Convert.ToInt32(accountType) == Account_Type.Current_Account)
            {
                double overdraftAmount = -1;
                loopCount = 0;

                do
                {
                    if (loopCount > 0)
                        Console.WriteLine("INVALID OVERDRAFT AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Overdraft Amount: ");
                    String overdraftAmountString = Console.ReadLine();

                    try
                    {
                        overdraftAmount = Convert.ToDouble(overdraftAmountString);
                    }
                    catch
                    {
                        loopCount++;
                    }

                } while (overdraftAmount < 0);

                ba = new Current_Account(name, addressLine1, addressLine2, addressLine3, town, balance, overdraftAmount);
            }
            else
            {
                double interestRate = -1;
                loopCount = 0;

                do
                {
                    if (loopCount > 0)
                        Console.WriteLine("INVALID INTEREST RATE ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Interest Rate: ");
                    String interestRateString = Console.ReadLine();

                    try
                    {
                        interestRate = Convert.ToDouble(interestRateString);
                    }
                    catch
                    {
                        loopCount++;
                    }

                } while (interestRate < 0);

                ba = new Savings_Account(name, addressLine1, addressLine2, addressLine3, town, balance, interestRate);
            }

            String accNo = dal.addBankAccount(ba, authManager.CurrentUsername);
            Console.WriteLine("New Account Number Is: " + accNo);
        }

        private static void HandleAccountClosure(Data_Access_Layer dal)
        {
            Console.WriteLine("Enter Account Number: ");
            String accNo = Console.ReadLine();

            Bank_Account ba = dal.findBankAccountByAccNo(accNo, authManager.CurrentUsername, false);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                Console.WriteLine(ba.ToString());

                String ans = "";

                do
                {
                    Console.WriteLine("Proceed With Deletion (Y/N)?");
                    ans = Console.ReadLine();

                    if (ans.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        bool adminApproved = authManager.RequestAdministratorApproval(accNo, ba.name);

                        if (adminApproved)
                        {
                            dal.closeBankAccount(accNo, authManager.CurrentUsername);
                            Console.WriteLine("Account closed successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Account closure cancelled - Administrator approval denied.");
                        }
                        break;
                    }
                    else if (ans.Equals("N", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Account closure cancelled.");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                    }
                } while (true);
            }
        }

        private static void HandleBalanceQuery(Data_Access_Layer dal)
        {
            Console.WriteLine("Enter Account Number: ");
            String accNo = Console.ReadLine();

            Bank_Account ba = dal.findBankAccountByAccNo(accNo, authManager.CurrentUsername, true);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                Console.WriteLine(ba.ToString());
            }
        }

        private static void HandleLodgement(Data_Access_Layer dal)
        {
            Console.WriteLine("Enter Account Number: ");
            String accNo = Console.ReadLine();

            Bank_Account ba = dal.findBankAccountByAccNo(accNo, authManager.CurrentUsername, false);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                double amountToLodge = -1;
                int loopCount = 0;

                do
                {
                    if (loopCount > 0)
                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Amount To Lodge: ");
                    String amountToLodgeString = Console.ReadLine();

                    try
                    {
                        amountToLodge = Convert.ToDouble(amountToLodgeString);
                    }
                    catch
                    {
                        loopCount++;
                    }

                } while (amountToLodge < 0);

                string reason = null;
                if (amountToLodge > 10000.00)
                {
                    Console.WriteLine("LARGE TRANSACTION DETECTED (>€10,000)");
                    Console.WriteLine("Please provide a reason for this transaction:");
                    reason = Console.ReadLine();

                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = "No reason provided";
                    }
                }

                dal.lodge(accNo, amountToLodge, authManager.CurrentUsername, reason);
                Console.WriteLine($"Successfully lodged €{amountToLodge:F2}");
            }
        }

        private static void HandleWithdrawal(Data_Access_Layer dal)
        {
            Console.WriteLine("Enter Account Number: ");
            String accNo = Console.ReadLine();

            Bank_Account ba = dal.findBankAccountByAccNo(accNo, authManager.CurrentUsername, false);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                double amountToWithdraw = -1;
                int loopCount = 0;

                do
                {
                    if (loopCount > 0)
                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Amount To Withdraw (€" + ba.getAvailableFunds() + " Available): ");
                    String amountToWithdrawString = Console.ReadLine();

                    try
                    {
                        amountToWithdraw = Convert.ToDouble(amountToWithdrawString);
                    }
                    catch
                    {
                        loopCount++;
                    }

                } while (amountToWithdraw < 0);

                string reason = null;
                if (amountToWithdraw > 10000.00)
                {
                    Console.WriteLine("LARGE TRANSACTION DETECTED (>€10,000)");
                    Console.WriteLine("Please provide a reason for this transaction:");
                    reason = Console.ReadLine();

                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = "No reason provided";
                    }
                }

                bool withdrawalOK = dal.withdraw(accNo, amountToWithdraw, authManager.CurrentUsername, reason);

                if (withdrawalOK == false)
                {
                    Console.WriteLine("Insufficient Funds Available.");
                }
                else
                {
                    Console.WriteLine($"Successfully withdrew €{amountToWithdraw:F2}");
                }
            }
        }
    }
}