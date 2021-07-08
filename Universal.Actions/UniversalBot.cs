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
using Universal.Actions.Models;

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
                //1) Capture this user as the owner of the asset
                string cardJson = await GetApprovalRequestCard(turnContext.Activity.Conversation.Id, turnContext.Activity.From.Id);

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

            string cardJson = string.Empty;

            switch (activityValue.Action.Verb)
            {
                //2) When a user clicks the approve button, update their card but also update the owners card.
                case "approveClicked":
                    cardJson = await ApproveAsset(turnContext.Activity.Conversation.Id, turnContext.Activity.From);
                    var attachment = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = JsonConvert.DeserializeObject(cardJson),
                    };

                    var messageActivity = MessageFactory.Attachment(attachment);
                    messageActivity.Id = turnContext.Activity.ReplyToId;
                    await turnContext.UpdateActivityAsync(messageActivity);

                    break;
                case "refreshCard":
                    cardJson = await GetApprovalStatusCard(turnContext.Activity.Conversation.Id, turnContext.Activity.From.Id);
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

        private async Task<string> ApproveAsset(string assetId, ChannelAccount user)
        {
            var asset = await _universalDb.GetAssetAsync(assetId);

            asset.ApprovedBy.Add(new User { Id = user.Id, Name = user.Name });

            await _universalDb.UpsertAssetAsync(asset);

            return await GetApprovalStatusCard(assetId, user.Id);
        }

        private async Task<string> GetApprovalStatusCard(string assetId, string userId)
        {
            var asset = await _universalDb.GetAssetAsync(assetId);


            if (asset != null && asset.Owner == userId) 
            {
                string text = "Approved by: \n";

                foreach (var user in asset.ApprovedBy) 
                {
                    text += $"{user.Name} \n";
                }

                return GetCard(@".\AdaptiveCards\ApprovalOwner_AdaptiveCard.json", userId, text);
            }
            else if (asset != null && asset.ApprovedBy.FindIndex(u => u.Id == userId) != -1)
            {
                return GetCard(@".\AdaptiveCards\ApprovalDone_AdaptiveCard.json", userId);

            }
            else
            {
                return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", userId);
            }
        }

        private async Task<string> GetApprovalRequestCard(string assetId, string ownerId)
        {
            var asset = new Asset() { Id = assetId, Owner = ownerId, ApprovedBy = new List<User>() };
            
            await _universalDb.UpsertAssetAsync(asset);

            return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", ownerId);
        }

        private static string GetCard(string filePath, string userId, string text = "")
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
                userIds,
                text
            };

            string cardJson = template.Expand(adaptiveCardData);
            return cardJson;
        }
    }
}
