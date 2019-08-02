using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure;
using FlwDatabase;
using InstagramModels;
using NLog;
using InstagramSelenium;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace InstaWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {   
        private static Profile profile = InstagramLogins.GetProfile(ProfileId);
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private Random probability = new Random();


        private static int ProfileId
        {
            get
            {
                var id = 0;
                int.TryParse(CloudConfigurationManager.GetSetting("ProfileId"), out id);
                return id;
            }
        }

        private static string UploadBlob(string blobContainerName, string key, Stream sourceStrem, string contentType)
        {
            //getting the storage account
            string uri = null;
            try
            {
                blobContainerName = blobContainerName.ToLowerInvariant();
                string azureStorageAccountConnection = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(azureStorageAccountConnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = cloudBlobClient.GetContainerReference(blobContainerName);
                container.CreateIfNotExists();

                CloudBlockBlob blob = container.GetBlockBlobReference(key);
                blob.Properties.ContentType = contentType;

                blob.UploadFromStream(sourceStrem);
                uri = blob.Uri.ToString();
            }
            catch (Exception exception)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error(exception.Message, exception);
            }
            return uri;
        }

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));

            // Create a CloudFileClient object for credentialed access to File storage.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            CloudFileShare share = fileClient.GetShareReference("Log");

            // Ensure that the share exists.
            if (share.Exists())
            {
                //do something    
                
            }
            
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            config.UseTimers();

            _logger.Log(LogLevel.Info, "Service started");
            try
            {
                if (profile == null)
                {
                    Console.WriteLine(string.Format("No Profile id : {0}", ProfileId));
                    _logger.Log(LogLevel.Info, string.Format("No Profile id : {0}", ProfileId));
                    return;
                }

                Instagram.ProfileId = profile.Id;
                //Instagram.InitPhantomDriver();
                Instagram.InitFirefoxDriver();
                Instagram.Open("https://www.instagram.com/");
                Instagram.InitCookiesAndRefreshPage(profile.ProfileName);


                if (Instagram.IsNeedToLogin())
                    Instagram.Login(profile.Login, profile.Password, profile.ProfileName);
                else
                    _logger.Log(LogLevel.Info, "Service already logged!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Service exception: {0}", ex));
                _logger.Log(LogLevel.Error, string.Format("Service exception: {0}", ex));
            }
            
            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
