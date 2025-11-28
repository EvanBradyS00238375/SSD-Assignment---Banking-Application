using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Banking_Application
{
    public class Data_Access_Layer
    {
        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();
        private EncryptionHelper encryptionHelper;

        private Data_Access_Layer()
        {
            accounts = new List<Bank_Account>();
            encryptionHelper = EncryptionHelper.GetInstance();
        }

        public static Data_Access_Layer getInstance()
        {
            return instance;
        }

        private SqliteConnection getDatabaseConnection()
        {
            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);
        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Bank_Accounts(
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID
                ";
                command.ExecuteNonQuery();
            }
        }

        public void loadBankAccounts()
        {
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();
            else
            {
                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Bank_Accounts WHERE 1 = @alwaysTrue";
                    command.Parameters.AddWithValue("@alwaysTrue", 1);
                    SqliteDataReader dr = command.ExecuteReader();


                    while (dr.Read())
                    {
                        int accountType = dr.GetInt16(7);

                        if (accountType == Account_Type.Current_Account)
                        {
                            Current_Account ca = new Current_Account();
                            ca.accountNo = dr.GetString(0);

                            ca.name = encryptionHelper.DecryptString(dr.GetString(1));
                            ca.address_line_1 = encryptionHelper.DecryptString(dr.GetString(2));
                            ca.address_line_2 = encryptionHelper.DecryptString(dr.GetString(3));
                            ca.address_line_3 = encryptionHelper.DecryptString(dr.GetString(4));
                            ca.town = encryptionHelper.DecryptString(dr.GetString(5));

                            ca.balance = dr.GetDouble(6);
                            ca.overdraftAmount = dr.GetDouble(8);
                            accounts.Add(ca);
                        }
                        else
                        {
                            Savings_Account sa = new Savings_Account();
                            sa.accountNo = dr.GetString(0);

                            sa.name = encryptionHelper.DecryptString(dr.GetString(1));
                            sa.address_line_1 = encryptionHelper.DecryptString(dr.GetString(2));
                            sa.address_line_2 = encryptionHelper.DecryptString(dr.GetString(3));
                            sa.address_line_3 = encryptionHelper.DecryptString(dr.GetString(4));
                            sa.town = encryptionHelper.DecryptString(dr.GetString(5));

                            sa.balance = dr.GetDouble(6);
                            sa.interestRate = dr.GetDouble(9);
                            accounts.Add(sa);
                        }
                    }
                }
            }
        }

        public String addBankAccount(Bank_Account ba)
        {
            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                string encryptedName = encryptionHelper.EncryptString(ba.name);
                string encryptedAddressLine1 = encryptionHelper.EncryptString(ba.address_line_1);
                string encryptedAddressLine2 = encryptionHelper.EncryptString(ba.address_line_2);
                string encryptedAddressLine3 = encryptionHelper.EncryptString(ba.address_line_3);
                string encryptedTown = encryptionHelper.EncryptString(ba.town);

                command.CommandText = @"
                    INSERT INTO Bank_Accounts 
                    (accountNo, name, address_line_1, address_line_2, address_line_3, town, balance, accountType, overdraftAmount, interestRate)
                    VALUES (@accNo, @name, @addr1, @addr2, @addr3, @town, @balance, @type, @od, @ir)";

                command.Parameters.AddWithValue("@accNo", ba.accountNo);
                command.Parameters.AddWithValue("@name", encryptedName ?? string.Empty);
                command.Parameters.AddWithValue("@addr1", encryptedAddressLine1 ?? string.Empty);
                command.Parameters.AddWithValue("@addr2", encryptedAddressLine2 ?? string.Empty);
                command.Parameters.AddWithValue("@addr3", encryptedAddressLine3 ?? string.Empty);
                command.Parameters.AddWithValue("@town", encryptedTown ?? string.Empty);
                command.Parameters.AddWithValue("@balance", ba.balance);
                command.Parameters.AddWithValue("@type", ba.GetType() == typeof(Current_Account) ? 1 : 2);

                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.Parameters.AddWithValue("@od", ca.overdraftAmount);
                    command.Parameters.AddWithValue("@ir", DBNull.Value);
                }
                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.Parameters.AddWithValue("@od", DBNull.Value);
                    command.Parameters.AddWithValue("@ir", sa.interestRate);
                }

                command.ExecuteNonQuery();
            }

            return ba.accountNo;
        }

        public Bank_Account findBankAccountByAccNo(String accNo)
        {
            foreach (Bank_Account ba in accounts)
            {
                if (ba.accountNo.Equals(accNo))
                {
                    return ba;
                }
            }
            return null;
        }

        public bool closeBankAccount(String accNo)
        {
            Bank_Account toRemove = null;
            foreach (Bank_Account ba in accounts)
            {
                if (ba.accountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }
            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = @accNo";
                    command.Parameters.AddWithValue("@accNo", toRemove.accountNo);
                    command.ExecuteNonQuery();
                }

                return true;
            }
        }

        public bool lodge(String accNo, double amountToLodge)
        {
            Bank_Account toLodgeTo = null;
            foreach (Bank_Account ba in accounts)
            {
                if (ba.accountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }
            }

            if (toLodgeTo == null)
                return false;
            else
            {
                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accNo";
                    command.Parameters.AddWithValue("@balance", toLodgeTo.balance);
                    command.Parameters.AddWithValue("@accNo", toLodgeTo.accountNo);
                    command.ExecuteNonQuery();
                }

                return true;
            }
        }

        public bool withdraw(String accNo, double amountToWithdraw)
        {
            Bank_Account toWithdrawFrom = null;
            bool result = false;
            foreach (Bank_Account ba in accounts)
            {
                if (ba.accountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }
            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {
                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accNo";
                    command.Parameters.AddWithValue("@balance", toWithdrawFrom.balance);
                    command.Parameters.AddWithValue("@accNo", toWithdrawFrom.accountNo);
                    command.ExecuteNonQuery();
                }

                return true;
            }
        }
    }
}

