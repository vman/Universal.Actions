using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Universal.Actions
{
    public class GraphClient
    {
        protected readonly IConfiguration Configuration;

        public GraphClient(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal async Task<GraphServiceClient> GetApplicationAuthenticatedClient()
        {
            var scopes = new string[] { "https://graph.microsoft.com/.default" };

            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                            .Create(Configuration["MicrosoftAppId"])
                                            .WithClientSecret(Configuration["MicrosoftAppPassword"])
                                            .WithTenantId(Configuration["TenantId"])
                                            .Build();

            AuthenticationResult authResult = await clientApp.AcquireTokenForClient(scopes).ExecuteAsync();
            string accessToken = authResult.AccessToken;

            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        return Task.CompletedTask;
                    }));

            return graphClient;
        }

        internal async Task SendMessageAsync()
        {
            var graphClient = await GetApplicationAuthenticatedClient();


            var toRecip = new Recipient()
            {
                EmailAddress = new EmailAddress()
                {
                    // If recipient provided as an argument, use that
                    // If not, use the logged in user
                    Address = "vrd@augmentechdev.onmicrosoft.com"
                }
            };

            // Create the message
            var actionableMessage = new Message()
            {
                Subject = "Actionable message sent from code",
                ToRecipients = new List<Recipient>() { toRecip },
                Body = new ItemBody()
                {
                    ContentType = BodyType.Html,
                    Content = LoadActionableMessageBody()
                }
            };

            // Send the message
            await graphClient.Users["vrd@augmentechdev.onmicrosoft.com"].SendMail(actionableMessage, true).Request().PostAsync();


        }

        static string LoadActionableMessageBody()
        {
            // Load the card JSON
            var cardJson = System.IO.File.ReadAllText(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json");

            // Insert originator if one is configured
            //string originatorId = "";

            //// Add value
            //cardJson.Add(new JProperty("originator", originatorId));

            // Insert the JSON into the HTML
            return string.Format(System.IO.File.ReadAllText(@".\EmailBody.html"), "application/adaptivecard+json", cardJson);
        }
    }
}
