// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Universal.Actions
{
    public class UniversalBot : TeamsActivityHandler
    {

        private UniversalDb _universalDb;
        private GraphClient _graphClient;

        public UniversalBot(UniversalDb universalDb, GraphClient graphClient)
        {
            _universalDb = universalDb;
            _graphClient = graphClient;

        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.RemoveRecipientMention().ToLower() == "request")
            {

                string cardJson = GetApprovalRequestCard();

                var attachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JsonConvert.DeserializeObject(cardJson),
                };

                var messageActivity = MessageFactory.Attachment(attachment);

                await turnContext.SendActivityAsync(messageActivity);
            }

        }



        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var activityValue = ((JObject)turnContext.Activity.Value).ToObject<AdaptiveCardInvokeValue>();

            string cardJson;

            switch (activityValue.Action.Verb)
            {
                case "stage1ApproveClicked":
                    cardJson = await ApproveAsset(turnContext.Activity.From.Id, "stage1");
                    break;
                case "stage2ApproveClicked":
                    cardJson = await ApproveAsset(turnContext.Activity.From.Id, "stage2");
                    break;
                case "refreshCard":
                    cardJson = await GetApprovalStatusCard(turnContext.Activity.From.Id);
                    break;
                default:
                    cardJson = GetApprovalRequestCard();
                    break;
            }

            var adaptiveCardResponse = new AdaptiveCardInvokeResponse()
            {
                StatusCode = 200,
                Type = AdaptiveCard.ContentType,
                Value = JsonConvert.DeserializeObject(cardJson)
            };

            return CreateInvokeResponse(adaptiveCardResponse);
        }

        private async Task<string> ApproveAsset(string userId, string stage)
        {
            var user = new Models.User() { Id = userId, Approved = stage };
            await _universalDb.UpsertApprovalAsync(user);

            return await GetApprovalStatusCard(userId);
        }

        private async Task<string> GetApprovalStatusCard(string userId)
        {
            var user = await _universalDb.GetApprovalAsync(userId);

            if (user != null)
            {
                switch (user.Approved)
                {
                    case "stage1":
                        return GetCard(@".\AdaptiveCards\Stage1ApprovalDone_AdaptiveCard.json", userId);
                    case "stage2":
                        return GetCard(@".\AdaptiveCards\Stage2ApprovalDone_AdaptiveCard.json", userId);
                    default:
                        return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", userId);
                }

            }
            else
            {
                return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", userId);
            }
        }

        private string GetApprovalRequestCard()
        {
            return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", string.Empty);
        }

        private static string GetCard(string filePath, string userId)
        {
            string templateJson = File.ReadAllText(filePath);

            var template = new AdaptiveCardTemplate(templateJson);

            string[] userIds;

            if (string.IsNullOrWhiteSpace(userId))
            {
                userIds = new string[] { };
            }
            else 
            {
                userIds = new string[] { userId };
            }

            var adaptiveCardData = new
            {
                userIds
            };

            string cardJson = template.Expand(adaptiveCardData);
            return cardJson;
        }
    }
}
