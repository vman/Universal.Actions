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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universal.Actions.Models;

namespace Universal.Actions
{
    public class UniversalBot : TeamsActivityHandler
    {

        private UniversalDb _universalDb;

        public UniversalBot(UniversalDb universalDb)
        {
            _universalDb = universalDb;

        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.RemoveRecipientMention().ToLower() == "request")
            {
                //Capture this user as the owner of the asset and send the base approval request card with approve button
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
                //When a user clicks the approve button, update the message with the base adaptive card where the user is in in the userIds array.
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

                //For each user in the userIds array, get the relevant card depending on their role and actions.
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

            string[] users = asset.ApprovedBy.Select(user => user.Id).Append(asset.Owner).ToArray();

            return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", users);
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

                return GetCard(@".\AdaptiveCards\ApprovalOwner_AdaptiveCard.json", new string[] { userId }, text);
            }
            else if (asset != null && asset.ApprovedBy.FindIndex(u => u.Id == userId) != -1)
            {
                string[] users = asset.ApprovedBy.Select(user => user.Id).Append(asset.Owner).ToArray();

                return GetCard(@".\AdaptiveCards\ApprovalDone_AdaptiveCard.json", users);

            }
            else
            {
                return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", new string[] { userId });
            }
        }

        private async Task<string> GetApprovalRequestCard(string assetId, string ownerId)
        {
            var asset = new Asset() { Id = assetId, Owner = ownerId, ApprovedBy = new List<User>() };
            
            await _universalDb.UpsertAssetAsync(asset);

            return GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json", new string[] { ownerId });
        }

      

        private static string GetCard(string filePath, string[] userIds, string text = "")
        {
            string templateJson = File.ReadAllText(filePath);

            var template = new AdaptiveCardTemplate(templateJson);

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
