using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class PaginatorCommand
{
    public class Admin : CommandHandler, IAuthorizationRequirementsProvider
    {
        public Admin(
            CommandParser parser,
            TelegramBot bot,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Admin> logger)
            : base(parser, bot, httpContextAccessor, logger)
            {
            }

        public IEnumerable<IAuthorizationRequirement> Requirements { get; } 
            = IAuthorizationRequirementsProvider.Provide(
                MPlusAuthenticationRequirement.Instance,
                AccessLevelRequirement.Admin
                );

        internal override Command Target => "adminpaginator";

        protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
        {
            await Bot.SendMessageAsync_(Update.Message!.Chat.Id, @"UN Climate Change News, 27 September 2022 -  Momentum is building for a strong political outcome relating to oceans at the UN Climate Change Conference COP27 in Sharm el-Sheikh, Egypt, in November, including greater ambition to cut greenhouse gas emissions, strengthened measures to build the resilience of coastal communities to climate change impacts and the financing needed for this.

Oceans cover around 70% of the surface of the planet, and people in coastal regions continue to be at the forefront of the fight against climate change and are disproportionately affected. At the same time, oceans have a vast potential to store carbon, and coastal waters are, for example, a prime location for renewable energy projects and ecosystem protection and restoration.

As a key outcome of an Ocean and Climate Change Dialogue held in June, the UN Climate Change Secretariat has published a new report highlighting the vital importance of the ocean for livelihoods and biodiversity and as a fundamental component of the climate system, underscoring the need for greater ocean-related climate action at COP27.

The report delves into the need for governments to promote clear and long-horizon policy frameworks to cut greenhouse gas emission, including through offshore renewable energy and emission reductions from shipping.

The Ocean and Climate Change Dialogue Report lays out 10 key messages for governments to consider in the lead up to COP27 which set out opportunities for greater ambition and action for ocean climate action at the national and international level. These key messages are:

We must protect our ocean AND value its potential as a place for sustainable climate solutions and action.
Ocean-based measures offer significant mitigation (cutting greenhouse gas emissions) and adaptation (building resilience to climate change) options.
The ocean offers a space for integrated solutions that can be reflected in national climate policies and strategies.
Marine technology and marine and coastal nature-based solutions should be integrated to ensure that action is more robust, comprehensive and cost-effective than when using either solution alone.
We must use, improve and integrate the latest available ocean science and other knowledge systems.
A whole of society approach is needed for ocean - climate action, including to address governance aspects.
Funding for ocean-climate action needs to increase and access to funding must be supported
Strengthened finance and other support, including capacity building, must embrace complexity to provide innovative and multidisciplinary solutions.
A framework for collaborative efforts across UN Processes would increase institutional support for ocean-climate action.
Future ocean and climate change dialogues should focus on distinct topics to deep-dive into specific solutions that strategically support and strengthen ocean-climate action at national and international level and under the UNFCCC process.
2022 is 'the Super Year of the Ocean'

2022 has been named 'the Super Year of the Ocean', with a key number of breakthroughs on ocean-climate action: in March, the UN Environment Assembly agreed to begin negotiations for a binding global treaty to end plastic pollution, and at the UN-Ocean Conference in Lisbon in June, governments collectively agreed to scale up science-based and innovative actions to address the ocean emergency.

The Ocean and Climate Change Dialogue held in June was mandated by governments at last year’s UN Climate Change Conference COP26 in Glasgow. The Chair of the UNFCCC’s Subsidiary Body for Scientific and Technological Advice, Tosi Mpanu-Mpanu, described the dialogue as the start of a ""new mandate and a new opportunity for strengthening ocean and climate change action under the UNFCCC”.

A clear message that emerged from the Ocean and Climate Change Dialogue is of the importance of 'blueing the Paris Agreement' - making the ocean a place for real climate innovation and action.

“We need to close the funding gap notably for adaptation to climate change in the context of oceans. Official recognition of the outcomes of the Oceans Dialogue is one clear next step in the UNFCCC process that can inform future funding, funding priorities and implementation plans for ocean climate action at the national and local level"", said Lisa Schindler Murray, of the UNFCCC Nairobi Work Programme Expert Group.

Recent work by the UNFCCC Technology Executive Committee (TEC), Nairobi work programme Expert Group on Oceans, The International Union for Conservation of Nature (IUCN), and Friends of Ecosystem-based Adaptation (FEBA) shows that integrated technology- and nature-based solutions offer the potential to be more robust, comprehensive, and cost-effective than either solution alone, and ocean-based measures offer significant options for resilience to climate change impacts and risks.

These solutions include early warning systems for extreme events and approaches such as restoration of coastal vegetation alongside engineered seawalls to reduce the impacts of storm surges and sea level rise.

More information on the ocean action workstream under UN Climate Change can be found here.

Character Counter is a 100% free online character count calculator that's simple to use. Sometimes users prefer simplicity over all of the detailed writing information Word Counter provides, and this is exactly what this tool offers. It displays character count and word count which is often the only information a person needs to know about their writing. Best of all, you receive the needed information at a lightning fast speed.

To find out the word and character count of your writing, simply copy and paste text into the tool or write directly into the text area. Once done, the free online tool will display both counts for the text that's been inserted. This can be useful in many instances, but it can be especially helpful when you are writing for something that has a character minimum or limit.

Character and word limits are quite common these days on the Internet. The one that most people are likely aware of is the 140 character limit for tweets on Twitter, but character limits aren't restricted to Twitter. There are limits for text messages (SMS), Yelp reviews, Facebook posts, Pinterest pins, Reddit titles and comments, eBay titles and descriptions as well as many others. Knowing these limits, as well as being able to see as you approach them, will enable you to better express yourself within the imposed limits.

For students, there are usually limits or minimums for homework assignments. The same is often true for college applications. Abiding by these can have a major impact on how this writing is graded and reviewed, and it shows whether or not you're able to follow basic directions. Character counter can make sure you don't accidentally go over limits or fail to meet minimums that can be detrimental to these assignments.

This information can also be quite helpful for writers. Knowing the number of words and characters can help writers better understand the length of their writing, and work to display the pages of their writing in a specific way. For those who write for magazines and newspapers where there is limited space, knowing these counts can help the writer get the most information into that limited space.

For job seekers, knowing the number of characters of your resume can be essential to get all the information you want onto a single page. You can fool around with different fonts, their sizes and spacing to adjust the number of characters you can fit on a single page, but it's important to know the number you're attempting to fit on the page.

Character Counter isn't only for English. The tool can be helpful for those writing in non-English languages where character count is important. This can be the case for languages Japanese, Korean, Chinese and many others where characters are the basis of the written language. Even for those who aren't writing in English, knowing the character count of the writing is often beneficial to the writing.

Bonn, 28 September 2022 – From pizza night to a wedding to a corporate event or even the Olympic Games, the new Green Events Tool (GET) can improve the sustainability of these gatherings. Launched at the United Nations Climate Change Conference (COP 26) in Glasgow last year, the online tool is a user-friendly, end-to-end solution that can be used to lower greenhouse gas emissions and help reach reduction targets. GET is the outcome of close collaboration between the Gulf Organisation for Research & Development (GORD), UN Environment (UNEP) and UN Climate Change (UNFCCC).

Conor Barry, Manager at UNFCCC said: “Born as a project to help the UN system plan and deliver more sustainable events, the Green Events Tool has evolved to become an instrument available to all interested stakeholders globally. We hope that it will contribute to reducing the impact of the events we all deliver and look forward to users’ feedback to keep improving and expanding it.”

GET is a free web-based platform that helps users evaluate the sustainability performance of events and improve their environmental footprint. Whether an event is personal, corporate or public, the tool does not require any specific expertise to be used, ensuring broad accessibility to it. To this end, it includes features such as a sustainability checklist for users to select various “greening” actions relevant to their specific event within the available resources they have.

As the tool was unveiled earlier in September, Dr. Alhorr, Founding Chairperson, GORD, said: “GET’s intrinsic value lies in simplifying the arduous processes needed to evaluate and improve events’ sustainability profiles.”

Greening an event may involve looking at categories such as transport, accommodation, catering, venue or energy use: the tool encompasses all these fields, and more. Not only does the system provide suggestions to support improving sustainability on all these aspects, but it also (optionally) allows for an event’s green performance to be verified by a third party, to ensure higher credibility, if needed.

What’s more, GET integrates a carbon footprint calculator, so that the greenhouse gas emissions created by an event can be evaluated. After implementing the tools’ suggestions to reduce the footprint and calculating the remaining emissions, event organizers can go a step further and choose to make the event carbon neutral (by compensating the remaining emissions). A certificate is issued to help organizers communicate this.

Isabel Marras of UNEP added: “Events are a key part of how the UN operates and delivers on its mandates. For this reason, the GET is a vital tool to assist the UN system in meeting its commitments and reporting responsibilities”, confirming the organization’s trust in the new tool.

Through a final report generated by GET, event sustainability can be transparently and reliably communicated. GET is designed to be used throughout a project: it informs the planning stage of an event, the design of activities to improve sustainability, and adds credibility to communication activities linked to the event.");
        }
    }
}
