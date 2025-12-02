using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Reflection;
using System.Security.Cryptography;

namespace Banking_Application
{
    public class AuditLogger
    {
        private static AuditLogger instance = new AuditLogger();
        private const string EVENT_LOG_SOURCE = "SSD Banking Application";
        private const string EVENT_LOG_NAME = "Application";
        private EventLog eventLog;

        private AuditLogger()
        {
            InitializeEventLog();
        }

        public static AuditLogger GetInstance()
        {
            return instance;
        }

        private void InitializeEventLog()
        {
            try
            {
                if (!EventLog.SourceExists(EVENT_LOG_SOURCE))
                {
                    EventLog.CreateEventSource(EVENT_LOG_SOURCE, EVENT_LOG_NAME);
                    Console.WriteLine("Event Log Source Created: " + EVENT_LOG_SOURCE);
                    Console.WriteLine("Please restart the application to use the event log.");
                }

                eventLog = new EventLog();
                eventLog.Source = EVENT_LOG_SOURCE;
                eventLog.Log = EVENT_LOG_NAME;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing Event Log: " + ex.Message);
                Console.WriteLine("Note: Administrator privileges required to create Event Log source.");
            }
        }

        public void LogAccountCreation(string tellerName, string accountNo, string accountHolderName)
        {
            string message = BuildLogMessage(
                tellerName,
                accountNo,
                accountHolderName,
                "Account Creation",
                null
            );

            WriteToEventLog(message, EventLogEntryType.Information);
        }

        public void LogAccountClosure(string tellerName, string accountNo, string accountHolderName)
        {
            string message = BuildLogMessage(
                tellerName,
                accountNo,
                accountHolderName,
                "Account Closure",
                null
            );

            WriteToEventLog(message, EventLogEntryType.Warning);
        }

        public void LogBalanceQuery(string tellerName, string accountNo, string accountHolderName)
        {
            string message = BuildLogMessage(
                tellerName,
                accountNo,
                accountHolderName,
                "Balance Query",
                null
            );

            WriteToEventLog(message, EventLogEntryType.Information);
        }

        public void LogLodgement(string tellerName, string accountNo, string accountHolderName, double amount, string reason = null)
        {
            string transactionType = $"Lodgement (Amount: €{amount:F2})";

            string message = BuildLogMessage(
                tellerName,
                accountNo,
                accountHolderName,
                transactionType,
                reason
            );

            WriteToEventLog(message, EventLogEntryType.Information);
        }

        public void LogWithdrawal(string tellerName, string accountNo, string accountHolderName, double amount, string reason = null)
        {
            string transactionType = $"Withdrawal (Amount: €{amount:F2})";

            string message = BuildLogMessage(
                tellerName,
                accountNo,
                accountHolderName,
                transactionType,
                reason
            );

            WriteToEventLog(message, EventLogEntryType.Information);
        }

        private string BuildLogMessage(string tellerName, string accountNo, string accountHolderName,
                                       string transactionType, string reason)
        {
            StringBuilder logMessage = new StringBuilder();

            logMessage.AppendLine($"Bank Teller: {tellerName}");

            logMessage.AppendLine($"Account Number: {accountNo}");
            logMessage.AppendLine($"Account Holder: {accountHolderName}");

            logMessage.AppendLine($"Transaction Type: {transactionType}");

            string macAddress = GetMACAddress();
            logMessage.AppendLine($"Device MAC Address: {macAddress}");

            string windowsSID = GetWindowsSID();
            logMessage.AppendLine($"Windows SID: {windowsSID}");

            logMessage.AppendLine($"Transaction DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            if (!string.IsNullOrEmpty(reason))
            {
                logMessage.AppendLine($"Reason: {reason}");
            }

            logMessage.AppendLine(GetApplicationMetadata());

            return logMessage.ToString();
        }

        private string GetMACAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        PhysicalAddress macAddress = nic.GetPhysicalAddress();
                        if (macAddress != null && macAddress.ToString() != "")
                        {
                            return macAddress.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "MAC Address Unavailable: " + ex.Message;
            }

            return "MAC Address Not Found";
        }

        private string GetWindowsSID()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User?.Value ?? "SID Unavailable";
            }
            catch (Exception ex)
            {
                return "SID Unavailable: " + ex.Message;
            }
        }

        private string GetApplicationMetadata()
        {
            StringBuilder metadata = new StringBuilder();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                AssemblyName assemblyName = assembly.GetName();

                metadata.AppendLine($"Application Name: {assemblyName.Name}");

                metadata.AppendLine($"Application Version: {assemblyName.Version}");

                string location = assembly.Location;
                metadata.AppendLine($"Application Path: {location}");

                if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
                {
                    string hash = CalculateFileHash(location);
                    metadata.AppendLine($"Application Hash (SHA256): {hash}");
                }
            }
            catch (Exception ex)
            {
                metadata.AppendLine($"Application Metadata Error: {ex.Message}");
            }

            return metadata.ToString();
        }

        private string CalculateFileHash(string filePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] hashBytes = sha256.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "");
                    }
                }
            }
            catch (Exception ex)
            {
                return "Hash Calculation Failed: " + ex.Message;
            }
        }
        private void WriteToEventLog(string message, EventLogEntryType entryType)
        {
            try
            {
                if (eventLog != null)
                {
                    eventLog.WriteEntry(message, entryType);
                }
                else
                {
                    Console.WriteLine("Event Log not initialized. Message would be:");
                    Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to Event Log: " + ex.Message);
                Console.WriteLine("Attempted to log: " + message);
            }
        }

        public void LogSuccessfulLogin(string username, bool isBankTeller, bool isAdministrator)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"Login Attempt: SUCCESS");
            logMessage.AppendLine($"Username: {username}");
            logMessage.AppendLine($"Bank Teller: {isBankTeller}");
            logMessage.AppendLine($"Administrator: {isAdministrator}");
            logMessage.AppendLine($"Login DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            string macAddress = GetMACAddress();
            logMessage.AppendLine($"Device MAC Address: {macAddress}");

            string windowsSID = GetWindowsSID();
            logMessage.AppendLine($"Windows SID: {windowsSID}");

            logMessage.AppendLine(GetApplicationMetadata());

            WriteToEventLog(logMessage.ToString(), EventLogEntryType.SuccessAudit);
        }

        public void LogFailedLogin(string username, string reason)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"Login Attempt: FAILED");
            logMessage.AppendLine($"Username: {username}");
            logMessage.AppendLine($"Failure Reason: {reason}");
            logMessage.AppendLine($"Login DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            string macAddress = GetMACAddress();
            logMessage.AppendLine($"Device MAC Address: {macAddress}");

            string windowsSID = GetWindowsSID();
            logMessage.AppendLine($"Windows SID: {windowsSID}");

            logMessage.AppendLine(GetApplicationMetadata());

            WriteToEventLog(logMessage.ToString(), EventLogEntryType.FailureAudit);
        }

        public void LogSuccessfulAdminApproval(string tellerUsername, string adminUsername, string accountNo, string accountHolderName)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"Administrator Approval: GRANTED");
            logMessage.AppendLine($"Requesting Teller: {tellerUsername}");
            logMessage.AppendLine($"Approving Administrator: {adminUsername}");
            logMessage.AppendLine($"Account Number: {accountNo}");
            logMessage.AppendLine($"Account Holder: {accountHolderName}");
            logMessage.AppendLine($"Action: Account Deletion");
            logMessage.AppendLine($"Approval DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            string macAddress = GetMACAddress();
            logMessage.AppendLine($"Device MAC Address: {macAddress}");

            string windowsSID = GetWindowsSID();
            logMessage.AppendLine($"Windows SID: {windowsSID}");

            logMessage.AppendLine(GetApplicationMetadata());

            WriteToEventLog(logMessage.ToString(), EventLogEntryType.SuccessAudit);
        }

        public void LogFailedAdminApproval(string tellerUsername, string adminUsername, string accountNo, string reason)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"Administrator Approval: DENIED");
            logMessage.AppendLine($"Requesting Teller: {tellerUsername}");
            logMessage.AppendLine($"Attempted Administrator: {adminUsername}");
            logMessage.AppendLine($"Account Number: {accountNo}");
            logMessage.AppendLine($"Failure Reason: {reason}");
            logMessage.AppendLine($"Action: Account Deletion");
            logMessage.AppendLine($"Approval DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            string macAddress = GetMACAddress();
            logMessage.AppendLine($"Device MAC Address: {macAddress}");

            string windowsSID = GetWindowsSID();
            logMessage.AppendLine($"Windows SID: {windowsSID}");

            logMessage.AppendLine(GetApplicationMetadata());

            WriteToEventLog(logMessage.ToString(), EventLogEntryType.FailureAudit);
        }
    }
}