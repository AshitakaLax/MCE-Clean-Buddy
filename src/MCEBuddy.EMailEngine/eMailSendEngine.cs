using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.EMailEngine
{
    public static class eMailSendEngine
    {
        private static Thread _eMailMonitorThread = null;
        private static ConcurrentQueue<eMail.SendEmailOptions> _eMailQueue = null;
        private static object _newEmail = new object(); // indicator for a new eMail
        
        /// <summary>
        /// Start the eMail sending and monitoring engine
        /// Can add emails to be sent, the engine will keep trying to send the email until the configured timeout occurs
        /// </summary>
        static eMailSendEngine()
        {
            // Create a new stack to store eMail operations
            _eMailQueue = new ConcurrentQueue<eMail.SendEmailOptions>();

            // LAST - Create a new thread to send eMails
            _eMailMonitorThread = new Thread(eMailMonitorThread);
            _eMailMonitorThread.IsBackground = true; // Kill thread when process terminates
            _eMailMonitorThread.Start();
        }

        /// <summary>
        /// Adds an eMail to be send to the queue. This eMail will be retried for a the configured time until it is sent
        /// It uses the Globally defined basic email settings
        /// </summary>
        public static void AddEmailToSendQueue(string subject, string message)
        {
            _eMailQueue.Enqueue(new eMail.SendEmailOptions { eMailSettings = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.eMailBasicSettings, subject = subject, message = message, jobLog = Log.AppLog, asyncCallBackHandler = null, forceAysncCallBack = false });
            
            // Let the engine know a new message is ready to send
            lock (_newEmail)
            {
                Monitor.Pulse(_newEmail);
            }
        }

        /// <summary>
        /// Thread to continue trying to send eMails in the background
        /// </summary>
        private static void eMailMonitorThread()
        {
            try
            {
                while (true)
                {
                    eMail.SendEmailOptions eMailOptions;
                    Queue<eMail.SendEmailOptions> retryQueue = new Queue<eMail.SendEmailOptions>();

                    while (_eMailQueue.TryDequeue(out eMailOptions)) // Get all email pending one at a time
                    {
                        eMailOptions.eMailSettings = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.eMailBasicSettings; // Get the latest settings (it may have changed)
                        if (!eMail.SendEMail(eMailOptions))
                            retryQueue.Enqueue(eMailOptions); // if it failed to send it then save it to retry later
                    }

                    // Add the unsent items back to the retry queue to try later
                    foreach (eMail.SendEmailOptions retryEmailOption in retryQueue)
                        _eMailQueue.Enqueue(retryEmailOption);

                    // Wait to retry sending again or until a new message comes in for sending
                    lock (_newEmail)
                    {
                        Monitor.Wait(_newEmail, GlobalDefs.EMAIL_SEND_ENGINE_RETRY_PERIOD);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
