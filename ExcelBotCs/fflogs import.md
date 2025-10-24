# FFLogs Import Feature

## Authorisation
The application needs to authenticate before performing any requests against the API.

**Clarifying Questions:**
- What type of authentication does FFLogs use? (OAuth 2.0, API keys, etc.) -> All of them, here is the official documentation: https://www.archon.gg/ffxiv/articles/help/api-documentation
- Where should the FFLogs credentials be stored? (Environment variables, configuration file?) -> The credentials are being provided through environment variables or the appsettings.json files
- What scopes/permissions are needed from the FFLogs API?
- Should we create a dedicated service class for FFLogs authentication, similar to the Discord integration? -> The service is going to authenticate against the FFLogs website, so we probably don't need anything similar to the discord integration.

## Importing Fights
I want to periodically import a list of all fights that exist on the Site FFLogs.com and import them into the database. For this my Fight.cs Entity and FightDto.cs DTO classes need to get updated to accomodate the ids and keys used by the FFLogs API.

**Clarifying Questions:**
- What is the import frequency? (Hourly, daily, weekly?) -> The current interval is 5mins. This should be kept for now
- Which FFLogs API endpoint(s) should we call to get the fight list? -> The documentation can be found here: https://www.fflogs.com/v2-api-docs/ff/query.doc.html 
- What specific FFLogs IDs/keys need to be added to `Fight.cs` and `FightDto.cs`? -> Figure this one out by comparing the Fight.cs class with the API specification.
  - Fight ID from FFLogs?
  - Report ID/Code?
  - Encounter ID?
  - Any other identifiers?
- Should we import ALL fights from FFLogs, or filter by specific criteria (e.g., specific game, expansion, difficulty)? -> I only want fights from Final Fantasy XIV, but all of them
- How do we handle duplicates? If a fight already exists in our database, should we update it or skip it? -> If a fight already exists in the database it should get skipped
- Should we track import history/logs to know when the last successful import occurred? -> Yes, this info might be useful later on to refine the synchronization interval

## Checking character activity
I want to periodically check the recent activity of a specific character to check which fights have been cleared and update the Member object in the database to include the fight in the 'Experience' attribute. This Attribute links to the individual fights in the fights collection.

**Clarifying Questions:**
- What identifies a "specific character"? -> It is either the 'Member.LodestoneId' or the region + server + character name, not sure what exactly the API requires
  - Is it the `Member.LodestoneId`?
  - Character name + server?
  - FFLogs character ID?
- What is the check frequency? (Hourly, daily, after each import?) -> Use the interval of the WorkerService
- What FFLogs API endpoint provides character activity/cleared fights? -> Check the official API documentation for this
- What does "cleared" mean? -> The first completion regardless of performance.
  - Any completion?
  - Specific performance threshold (parse percentage, kill time)?
  - First clear only, or every clear?
- How is the `Experience` attribute structured? -> It is a list of 'Fight.cs' references
  - Is it a list of Fight IDs?
  - Does it include clear dates/times?
  - Does it track multiple clears of the same fight?
- Should we check all members or only specific ones? (e.g., only active members, only those with LodestoneIds set) -> The end goal is to check all members, but in order to save API credits the sync should happen in waves

## Background Service Implementation
**Clarifying Questions:**
- Should this use the existing `WorkerService.cs` mentioned in the git log? -> Yes
- Should fight imports and character activity checks run as separate background tasks or combined? -> Combined
- What error handling is expected? (Retry logic, notification on failure?) -> Only logging on failure
- Should there be admin endpoints to manually trigger imports/checks? -> No

## Data Model Questions
**Clarifying Questions:**
- Does `Member.Experience` already exist, or does it need to be added? -> The 'Experience' Property already exists within the 'Member' entity
- Should we create a join/relationship entity to track Member-Fight relationships with additional metadata (clear date, performance metrics)? -> The database used is a MongoDb, so we should only save the ids to reference between collections
- Do we need to store FFLogs-specific metadata like parse percentages, DPS rankings, etc.? -> Not yet