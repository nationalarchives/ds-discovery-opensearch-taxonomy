# Discovery taxonomy updater for Opensearch

## Prerequisite

Download and install:

- Visual Studio 2022 Professional
- .NET Core 8.0

Setup

Detailed setup information is included in the MS Word documentation available at https://national-archives.atlassian.net/browse/DPS-978.  Also:
- For setting environment variables, please see https://github.com/nationalarchives/ds-discovery-opensearch-taxonomy/blob/main/EnvironmentVariables.txt
- For Taxonomy Generator installation using Powershell,  please see https://github.com/nationalarchives/ds-discovery-opensearch-taxonomy/blob/main/TaxonomyGenerator.txt
- For Taxonomy OpenSearch update installation using Powershell, please see https://github.com/nationalarchives/ds-discovery-opensearch-taxonomy/blob/main/TaxonomyUpdate.txt

There are 4 related applications in this repo:

1. Taxonomy Generator
2. Taxonomy Open Search Update
3. Taxonomy Command Line application
4. Taxonomy API

These are described below.  For further information, please see the MS Word document in this repo.  There are also installation scriots and information about environment variables.

## 1. Taxonomy Generator

1.1	The taxonomy Batch processor, also known as the Taxonomy Generator, is used for categorising multiple information assets.  It can be run in either or two modes Full Re-Index or Daily Updates.  Which mode applies is determined by the OperationMode setting in the appsettings.json file.  The main difference between the two modes is that the Full Re-index mode uses bulk queries and batching to process large numbers of records as efficiently as possible.  With the daily updates, the numbers to be processed vary considerably and (at least with the current architecture) cannot be known prior to the start of processing.  Therefore, it is not feasible to predict whether batch processing would be more efficient for a given round of daily updates and, if so, what batch size to use.

1.2	In Full Re-index mode, the application:

-	Fetches a list of information asset IDs.  With the latest version, the list can be fetched from either:
o	 Open Search (this can either be the same database used for updates, or a separate instance).
o	An Amazon SQS input queue (this could be a dedicated queue for full re-indexing, or the same queue used for daily updates)
-	Adds these asset IDs to an internal queue.
-	Dequeues the asset IDs from the internal queue.  The dequeued IAIDs are organised into one or more batches based on the configured batch size and the number of batches to process concurrently. When the number of IAIDs dequeued meet these thresholds (or the internal queue has finished being populated as all the asset IDs have been fetched), the application submits a bulk query to Open Search to obtain the information asset data for these asset IDs.
-	The application then batches the information asset data and indexes all the information asset details into Lucene running in process.
-	The application next submits a bulk query to Lucene running in process to determine which categories match which information assets.
-	Lucene returns information about which information assets match which categories.
-	The application adds these results to an internal categorisation results queue.
-	Each message on this queue represents an information asset ID and an array of zero or more matching category IDs.  
-	A background worker process dequeues results from this internal results queue, and forwards them to an AWS SQS instance.  This data can then be processed by the Taxonomy Open Search Update Service to add or update Open Search.
-	If the IAID source is OpenSearch, the process continues until the cursor has fetched all the IAIDs, and the internal asset ID queue has been marked as complete and has no more assets.  If the IAID source is set to SQS, there is no way to know when the fetch is complete.
-	As categorisation progresses, the application writes processing, performance and exception data to log files configured using NLog. This can include individual categorisation results, if configured to do so (see 2.4). Key information is also written to the Windows event log.

1.6	In Daily Update mode, the application:
	
-	Fetches messages from an AWS SQS instance.  These messages contain the Information Asset IDs (IAIDs) of the assets to be categorised.
-	Obtains a list of information asset identifiers (IAIDs) from each message.  Each message may contain one or more IAIDs, separated by semi colons. The following steps are then processed for each IAID in turn:
o	Queries Open Search to obtain the information asset data for the IAID. i.e. the fields we want to use for categorisation.
o	Indexes the asset into an in-process Lucene index
o	Submits a query to the in-process Lucene index to determine which categories (if any) match the asset.
o	Updates a further Active MQ message queue with the results.  Each message on this queue represents an information asset and an array of zero or more matching categories.  This data can then be processed by the Taxonomy Open Update Service to add or update Open Search.
-	As categorisation progresses, the application writes processing and performance data to the Windows event log.  This can include individual categorisation results, if configured to do so (see 2.5).


## 2. Taxonomy Open Search Update

2.1	The Taxonomy Open Search Update retrieves categorisation results (generated via either daily updates or a full re-indexing) from an AWS SQS instance.  Each result obtained from the queue consists of an information asset identifier (IAID) and a list of zero or more matching category identifiers.  The service batches these results and updates the corresponding information asset entries in Open Search using bulk queries.

2.2	For each information asset updated, the service adds a TAXONOMY_ID field, or replaces the existing TAXONOMY_ID field if it already exists.  This field consists of an array of zero or more category identifiers, representing the categories found to have matched the information asset. E.g. {TAXONOMY_ID: [“C10005”; C10006”; “C10007”]}


## 3. Taxonomy Command Line Application

3.1	The Taxonomy Command Line application can be used to categorise small numbers of information assets, and also obtain the scores for each category.  The results are displayed in the console, and can optionally also be submitted to the AWS SQS instance used to update Open Search. The usage format is as follows:
```
	dotnet .\NationalArchives.Taxonomy.CLI.dll [arguments]
```
To obtain help in formation, run :
```
	dotnet .\NationalArchives.Taxonomy.CLI.-h (OR –help)
```
3.2	In the main usage scenario, the application can be used for test categorisations (i.e. no submission to the ActiveMQ update queue), or live categorisations.  You can even mix test and live requests in a single command.  The syntax required is one or more occurrences of -c (or --categorise-single) and/or -t (or --test-categorise-single).  Each occurrence must then be followed by an information asset ID, separated from its flag by either a space or a colon.  The information asset ID can optionally be surrounded by single or double quotes, though this is unnecessary as asset IDs never have spaces.

3.3	Examples:
```
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t C12345
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t:C12345
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t:“C12345”
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t “C12345”
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t:’C12345’
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t ‘C12345’
```
All of these display matching categories with scores in the console for the information asset whose identifier is C12345. 
```
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t C12345 -t C23456 
	dotnet .\NationalArchives.Taxonomy.CLI.dll -t:C12345 -t:C23456
```
Displays matching categories with scores in the console for the information assets with the identifiers C12345 and C23456
```
	dotnet .\NationalArchives.Taxonomy.CLI.dll -c C12345
```
Displays matching categories (if any) with scores in the console for the information asset whose identifier is C12345, and submits an update request to the configured update queue to update the configured Open search database with these categorisation results (i.e. only the matching categories – the scores are not stored).
```
	dotnet .\NationalArchives.Taxonomy.CLI.dll -c:C12345 -t:C23456
```
Displays matching categories (if any) with scores in the console for the information asset whose identifier is C12345, and submits an update request to the configured update queue to update the configured Open search database with these categorisation results.  For the information asset whose ID is C23456, only the console results are displayed and no update request is submitted.


 N.B. All of the above examples will also work with –test-categorise-single in place of -t, or –categorise-single in place of -c.

## 4. Taxonomy API

   The API is used by the Discovery Classifier application.  This is an application written in WPF that allows end users to query and manage the categories.  The category definitions are stored in MongoDB.
