// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Universal.Actions
{
    public class UniversalBot : TeamsActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string cardJson = GetCard(turnContext, "initial load");

            var attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            var messageActivity = MessageFactory.Attachment(attachment);

            await turnContext.SendActivityAsync(messageActivity);
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var activityValue = ((JObject)turnContext.Activity.Value).ToObject<AdaptiveCardInvokeValue>();

            string cardJson;// = GetCard(turnContext, "initial load");

            switch (activityValue.Action.Verb) 
            {
                case "actionClicked":
                    cardJson = GetCard(turnContext, "button clicked");
                    break;
                case "refreshCard":
                    cardJson = GetCard(turnContext, "card refreshed");
                    break;
                default:
                    cardJson = GetCard(turnContext, "initial load");
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

        private static string GetCard(ITurnContext turnContext, string title)
        {
            string templateJson = File.ReadAllText(@".\AdaptiveCards\SampleAdaptiveCard.json");

            var template = new AdaptiveCardTemplate(templateJson);

            // You can use any serializable object as your data
            var myData = new
            {
                title,
                userIds = new string[] { turnContext.Activity.From.Id }
            };

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(myData);
            return cardJson;
        }
    }
}
