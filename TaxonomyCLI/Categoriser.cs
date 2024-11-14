using McMaster.Extensions.CommandLineUtils;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("NationalArchives.Taxonomy.CLI.UnitTests")]

namespace NationalArchives.Taxonomy.CLI
{
    internal class Categoriser
    {
        private const string INVALID_IAID = "One or more information asset IDs supplied were in an invalid format.  Processing will now terminate.";
        private const string TEST_CATEGORISE_HELP_DESC = "Tests the categorisation of a single information asset without updating the database.  Multiple assets can be processed e.g. -t:C12345 -t:98765";
        private const string CATEGORISE_HELP_DESC = "Categorises a single information asset and updates the categorisation in the database. Multiple assets can be processed e.g. -c:C12345 -c:98765";
        private const string TEST_CATEGORISE_HELP_DESC_FILE = "Tests the categorisation of a list of information assets from a file without updating the database.  Each asset must be entered on a separate line in the file";
        private const string SHOW_CONFIG_INFO = "Shows application confirguration information.";
        private const string CATEGORISE_SUMMARY_TEXT = "Interim updates queue updated with categorisation results.";
        private const string TEST_CATEGORISE_SUMMARY_TEXT = "Test only - no updates made to interim updates queue.";
        private const string NO_INFORMATION_ASSETS_SUPPLIED = "No information assets supplied for categorisation or testing!";
        private const string USE_HELP_MSG = "Use the --help option for more information on usage.";

        // Information Asset IDs must start with the letter C or D and be followed by 2 to 8 digits.
        private static readonly Regex informationAssetRegex = new Regex(@"^(C\d{2,8}|D\d{2,8}|\w{32})$", RegexOptions.IgnoreCase);

        private readonly ICategoriserService <CategorisationResult> _categoriserService;
        private readonly IConsole _console;

        public Categoriser(ICategoriserService<CategorisationResult> categoriserService, IConsole console)
        {
            _categoriserService = categoriserService;
            _console = console;
        }

        public int OnExecute(CommandLineApplication app)
        {

            if (String.IsNullOrEmpty(FileTestCategoriseSingle) && ((TestCategoriseSingle ?? CategoriseSingle) == null) || (TestCategoriseSingle?.Length == 0 || CategoriseSingle?.Length == 0))
            {
                _console.WriteLine(NO_INFORMATION_ASSETS_SUPPLIED);
                _console.WriteLine(USE_HELP_MSG);
                return -1;
            }

            //Check the IAIDs supplied are in the correct format.  If any are invalid, no processing will take place:
            IEnumerable<string> invalidIaids = (CategoriseSingle ?? new string[] { }.Union(TestCategoriseSingle ?? new string[] { }))
                .Where(s => !informationAssetRegex.IsMatch(s));

            if (invalidIaids.Any())
            {
                invalidIaids.ToList().ForEach(s => Console.WriteLine($"{s} is not a vaild information asset ID"));
                _console.WriteLine(INVALID_IAID);
                return -1;
            }

            ProcessIaids(TestCategoriseSingle, _categoriserService.TestCategoriseSingle, TEST_CATEGORISE_SUMMARY_TEXT);

            ProcessIaids(CategoriseSingle, _categoriserService.CategoriseSingle, CATEGORISE_SUMMARY_TEXT);

            ProcessFile(FileTestCategoriseSingle);

            _console.WriteLine("Processing completed.");

            return 0;
        }

        private void ProcessIaids(string[] iaidsToProcess, Func<string, Task<IList<CategorisationResult>>> categoriserOperation, string consoleText)
        {
            if (iaidsToProcess == null || iaidsToProcess.Length == 0)
            {
                return;
            }

            // TODO: Possibly allow for parallel processing if there is a clear use case.
            // Though we would need to lock around the console output to ensure all lines for each IAID are grouped together.

            try
            {
                foreach (string iaid in iaidsToProcess)
                {
                    DateTime startTime = DateTime.Now;
                    IList<CategorisationResult> catResults = categoriserOperation(iaid).Result;
                    DateTime endTime = DateTime.Now;

                    TimeSpan categorisationTime = endTime - startTime;

                    int resultCount = catResults.Count;
                    string outputWording = resultCount == 1 ? "category." : resultCount == 0 ? "categories." : "categories:";

                    _console.WriteLine($"{iaid} matched {catResults.Count} {outputWording}");

                    foreach (CategorisationResult result in catResults)
                    {
                        _console.WriteLine(result.ToString().PadLeft(50, '*'));
                    }

                    _console.WriteLine($"Results took {startTime.Millisecond } milliseconds.");

                    if (!String.IsNullOrEmpty(consoleText))
                    {
                        _console.WriteLine(consoleText);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ProcessFile(string filename)
        {
            if(String.IsNullOrEmpty(filename))
            {
                return;
            }

            string[] files = File.ReadAllLines(filename);

            ProcessIaids(files, _categoriserService.TestCategoriseSingle, TEST_CATEGORISE_SUMMARY_TEXT);
        }

        [Option(Description = TEST_CATEGORISE_HELP_DESC)]
        public string[] TestCategoriseSingle { get; set; }

        [Option(Description = TEST_CATEGORISE_HELP_DESC_FILE)]
        public string FileTestCategoriseSingle { get; set; }

        [Option(Description = CATEGORISE_HELP_DESC)]
        public string[] CategoriseSingle { get; set; }
    }
}
