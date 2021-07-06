using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
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

        internal async Task<GraphServiceClient> GetAuthenticatedClient()
        {

            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", "eyJ0eXAiOiJKV1QiLCJub25jZSI6IjJQQ2g3NmZwWGNvS1JpWl9VaGdyanBUUGM4WFZSWjNfaTd0cGRIZFU1dWMiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC82NGM5MmJmZC1kNjhjLTQ3ODEtOTA3YS0yZmFkNzQwYTc5NDQvIiwiaWF0IjoxNjI1MjcxODcwLCJuYmYiOjE2MjUyNzE4NzAsImV4cCI6MTYyNTI3NTc3MCwiYWNjdCI6MCwiYWNyIjoiMSIsImFjcnMiOlsidXJuOnVzZXI6cmVnaXN0ZXJzZWN1cml0eWluZm8iLCJ1cm46bWljcm9zb2Z0OnJlcTEiLCJ1cm46bWljcm9zb2Z0OnJlcTIiLCJ1cm46bWljcm9zb2Z0OnJlcTMiLCJjMSIsImMyIiwiYzMiLCJjNCIsImM1IiwiYzYiLCJjNyIsImM4IiwiYzkiLCJjMTAiLCJjMTEiLCJjMTIiLCJjMTMiLCJjMTQiLCJjMTUiLCJjMTYiLCJjMTciLCJjMTgiLCJjMTkiLCJjMjAiLCJjMjEiLCJjMjIiLCJjMjMiLCJjMjQiLCJjMjUiXSwiYWlvIjoiRTJaZ1lEZ1ZGTExMN1hiWi83bnF6NDI4WGRRUFQ2eGpWMWg4VEkxdGc5L1B0aE8vWW1VQiIsImFtciI6WyJwd2QiXSwiYXBwX2Rpc3BsYXluYW1lIjoiR3JhcGggZXhwbG9yZXIgKG9mZmljaWFsIHNpdGUpIiwiYXBwaWQiOiJkZThiYzhiNS1kOWY5LTQ4YjEtYThhZC1iNzQ4ZGE3MjUwNjQiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IkRlc2hwYW5kZSIsImdpdmVuX25hbWUiOiJWYXJkaGFtYW4iLCJpZHR5cCI6InVzZXIiLCJpcGFkZHIiOiI4Mi4yMy45MS45MSIsIm5hbWUiOiJWYXJkaGFtYW4gRGVzaHBhbmRlIiwib2lkIjoiMTc1OWMxZmQtMGMxNS00NDI4LTk5ZDktNDVhZTVlNTQ0NzQ1IiwicGxhdGYiOiIzIiwicHVpZCI6IjEwMDMyMDAxM0UwOUNGREEiLCJyaCI6IjAuQVlJQV9TdkpaSXpXZ1VlUWVpLXRkQXA1UkxYSWk5NzUyYkZJcUsyM1NOcHlVR1NDQUNVLiIsInNjcCI6Ikdyb3VwLlJlYWQuQWxsIE1haWwuU2VuZCBvcGVuaWQgcHJvZmlsZSBVc2VyLlJlYWQgZW1haWwiLCJzaWduaW5fc3RhdGUiOlsia21zaSJdLCJzdWIiOiJUQnN5b1VOUU9YRTVKcktZb2Mzb3dJTGZsU3FvTjZYbmNTLXVWRlp6TnNjIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6IkVVIiwidGlkIjoiNjRjOTJiZmQtZDY4Yy00NzgxLTkwN2EtMmZhZDc0MGE3OTQ0IiwidW5pcXVlX25hbWUiOiJ2cmRAYXVnbWVudGVjaGRldi5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJ2cmRAYXVnbWVudGVjaGRldi5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiJUM0hrYVQ1VDAwNng2d0h2b09TdkFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyI2MmU5MDM5NC02OWY1LTQyMzctOTE5MC0wMTIxNzcxNDVlMTAiLCJiNzlmYmY0ZC0zZWY5LTQ2ODktODE0My03NmIxOTRlODU1MDkiXSwieG1zX3N0Ijp7InN1YiI6Ikk1azVadFVyWXVIcmpDNzREZzhMQ0dVY3YzYUY1WXh3elpmLV8zeXYtX1UifSwieG1zX3RjZHQiOjE2MjA0NzkyNDR9.Jh6vr4kXsDdGXbpehTXIDYcA4UFe2HBCa2qsfEbf1M7yv_ygzWqcEM1tPmLuuMx8mU0zC1R1diiP0_QYE08r5vdmuxrCF9B-lFDHnz_hqXdeKnnsL6VYKf1943jpyIa4il4v27J3ia1tfWpTOZqRhSknbZCKfwhBpn7IGWwRJKEOAZDx-B7-NSGGsHhd_43kv4ZV8lsuJlddD9Sn7I6ZABtFvbDFIQLH5rTsetCZtvVbFhyBwPWX79q2TsDSK1Vfs03AINskCaF4d8tZHccFE90CP9_8lekLEl08biO9A3vLfRC0eMYxip6ekdSOc_UTu5o4KEUDPjJuVRmostuv8g");

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
            //await graphClient.Me.SendMail(actionableMessage, true).Request().PostAsync();


        }

        static string LoadActionableMessageBody()
        {
            // Load the card JSON
            var cardJson = System.IO.File.ReadAllText(@".\AdaptiveCards\ApprovalRequest_Outlook_AdaptiveCard.json");

            //Insert originator if one is configured
            //string originatorId = "";

            //// Add value
            //cardJson.Add(new JProperty("originator", originatorId));

            // Insert the JSON into the HTML
            return string.Format(System.IO.File.ReadAllText(@".\EmailBody.html"), "application/adaptivecard+json", cardJson);
        }
    }
}
