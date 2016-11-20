using System;
using System.Net.Mail;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public static class eMail
    {
        /// <summary>
        /// Class that contains the options to send an eMail including registering callback handlers for asynchronous emails
        /// </summary>
        public class SendEmailOptions
        {
            /// <summary>
            /// Email options
            /// </summary>
            public EmailBasicSettings eMailSettings;
            
            /// <summary>
            /// eMail Subject
            /// </summary>
            public string subject;
            public string message;
            public Log jobLog;
            public SendCompletedEventHandler asyncCallBackHandler;
            public bool forceAysncCallBack;
        }

        /// <summary>
        /// Send an eMail synchronously or asynchronously (depending upon options passed)
        /// </summary>
        /// <param name="eMailOptions">eMail User State options</param>
        /// <returns>True if email is sent successfuly</returns>
        public static bool SendEMail(SendEmailOptions eMailOptions)
        {
            return SendEMail(eMailOptions.eMailSettings, eMailOptions.subject, eMailOptions.message, eMailOptions.jobLog, eMailOptions.asyncCallBackHandler, eMailOptions.forceAysncCallBack);
        }

        /// <summary>
        /// Send email immediately (synchronously)
        /// </summary>
        /// <returns>True if eMail is sent successfully</returns>
        public static bool SendEMail(EmailBasicSettings emailSettings, string subject, string message, Log jobLog)
        {
            return SendEMail(emailSettings, subject, message, jobLog, null);
        }

        /// <summary>
        /// Send email in the background and callback handler when completed (synchronous or asynchronous)
        /// Check the error in the callback handler for eMail status (success or error or cancelled)
        /// </summary>
        /// <param name="asyncCallback">User handler to call back and will send eMail asynchronously. The callback handler will receive an object of type EmailUserState</param>
        /// <param name="forceAsyncNoCallback">If set to true it will send eMail asynchronously without any user callback handler</param>
        /// <returns>False if an error is encountered crafting the eMail message</returns>
        public static bool SendEMail(EmailBasicSettings emailSettings, string subject, string message, Log jobLog, SendCompletedEventHandler asyncCallback, bool forceAsyncNoCallback = false)
        {
            string smtpServer = emailSettings.smtpServer;
            int portNo = emailSettings.port; // default port is 25
            bool ssl = emailSettings.ssl;
            string fromAddress = emailSettings.fromAddress;
            string toAddresses = emailSettings.toAddresses;
            string bccAddresses = emailSettings.bccAddress;
            string username = emailSettings.userName;
            string password = emailSettings.password;

            jobLog.WriteEntry(Localise.GetPhrase("Request to send eMail"), Log.LogEntryType.Information, true);
            jobLog.WriteEntry("Server -> " + smtpServer, Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("Port -> " + portNo.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("SSL -> " + ssl.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("Username -> " + username, Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("From -> " + fromAddress, Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("To -> " + toAddresses, Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("Subject -> " + subject, Log.LogEntryType.Debug, true);
            jobLog.WriteEntry("Message -> " + message, Log.LogEntryType.Debug, true);

            try
            {
                // Create the eMail message
                MailMessage eMailMessage = new MailMessage();
                eMailMessage.Subject = subject;
                eMailMessage.Body = message;
                eMailMessage.From = new MailAddress(fromAddress);
                if (!String.IsNullOrWhiteSpace(toAddresses)) // Avoid an exception, since to is not mandatory
                {
                    string[] addresses = toAddresses.Split(';');
                    for (int i = 0; i < addresses.Length; i++)
                        eMailMessage.To.Add(addresses[i]); // Add the To recipients
                }
                if (!String.IsNullOrWhiteSpace(bccAddresses)) // Avoid an exception, since bcc is not mandatory
                {
                    string[] bccToAddresses = bccAddresses.Split(';');
                    for (int i = 0; i < bccToAddresses.Length; i++)
                        eMailMessage.Bcc.Add(bccToAddresses[i]); // Add the Bcc recipients
                }
                eMailMessage.BodyEncoding = System.Text.Encoding.UTF8;
                eMailMessage.SubjectEncoding = System.Text.Encoding.UTF8;

                // Create the client to send the message
                SmtpClient eMailClient = new SmtpClient(smtpServer, portNo);
                if (username != "")
                    eMailClient.Credentials = new System.Net.NetworkCredential(username, password); // add the authentication details
                if (ssl)
                    eMailClient.EnableSsl = true;// Set the SSL if required
                eMailClient.Timeout = GlobalDefs.SMTP_TIMEOUT; // Set the timeout

                // Send the eMail - check for Async or Sync email sending
                if (asyncCallback == null && !forceAsyncNoCallback)
                {
                    eMailClient.Send(eMailMessage);
                    jobLog.WriteEntry(Localise.GetPhrase("Successfully send eMail"), Log.LogEntryType.Information, true);
                }
                else
                {
                    if (forceAsyncNoCallback)
                        eMailClient.SendCompleted += eMailClient_SendCompleted; // use default call back
                    else
                        eMailClient.SendCompleted += eMailClient_SendCompleted + asyncCallback; // Register call back

                    eMailClient.SendAsync(eMailMessage, new SendEmailOptions { eMailSettings = emailSettings, message = message, subject = subject, jobLog = jobLog, asyncCallBackHandler = asyncCallback, forceAysncCallBack = forceAsyncNoCallback });
                }
                
                return true;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry(Localise.GetPhrase("Error sending eMail") + " -> " + e.ToString(), Log.LogEntryType.Error, true);
                return false;
            }
        }

        /// <summary>
        /// Call back handler for aSynchronous eMail, checks for success or failure of email sending
        /// </summary>
        private static void eMailClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            SendEmailOptions userState = (SendEmailOptions)e.UserState;

            if (userState == null)
                return;

            if (e.Error == null)
                userState.jobLog.WriteEntry(Localise.GetPhrase("Successfully send eMail"), Log.LogEntryType.Information, true);
            else
                userState.jobLog.WriteEntry(Localise.GetPhrase("Error sending eMail") + " -> " + e.ToString(), Log.LogEntryType.Error, true);

            return;
        }
    }
}
