using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Service;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.CLI.UnitTests
{
    [TestClass]
    public class TestCommandLineCategoriser
    {
        ICategoriserService<CategorisationResult> _categoriser;
        IConsole _console;
        CommandLineApplication _app;
        TextWriter _textWriter;

        List<string> _consoleOutput;

        IList<CategorisationResult> _categorisationResults;

        private const int ADDITIONAL_INFO_MESSAGES_PER_IAID_PROCESSED = 3;

        [TestInitialize]       
        public void Init()
        {
            _categoriser = Substitute.For<ICategoriserService<CategorisationResult>>();
            
            _categorisationResults = SubstituteCategorisationResults();

            _categoriser.TestCategoriseSingle(Arg.Any<string>())
                .Returns(s =>
                    {
                        var tcs = new TaskCompletionSource<IList<CategorisationResult>>();
                        tcs.SetResult(_categorisationResults);
                        return tcs.Task;
                    }
                );

            _categoriser.CategoriseSingle(Arg.Any<string>())
                .Returns(s =>
                    {
                        var tcs = new TaskCompletionSource<IList<CategorisationResult>>();
                        tcs.SetResult(_categorisationResults);
                        return tcs.Task;
                    }
                );

            _app = new CommandLineApplication();

            _console = new TestConsole(this);
            _textWriter = Substitute.For<TextWriter>();
        }

        [TestMethod]
        public void RunTestCategorisation_WhenAllIaidsAreValid_ReturnsZero()  
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            string[] iaidsToProcess = new string[] { "C10101", Guid.NewGuid().ToString("N") };

            program.TestCategoriseSingle = iaidsToProcess; // Simulates supplying one or more -t option flags with Asset IDs e.g -t:C12345
            int result = program.OnExecute(_app);

            // Each IAID should result in 3 lines of fixed output, and one line for each categorisation result.
            Assert.AreEqual(_consoleOutput.Count, ExpectedConsoleOutputLineCountForValidProcessing(iaidsToProcess.Length));
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void RunTestCategorisation_WhenOneIaidIsInvalid_ReturnsMinusOne()
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            program.TestCategoriseSingle = new string[] { "K10101", Guid.NewGuid().ToString("N") };
            int result = program.OnExecute(_app);

            Assert.AreEqual(-1, result);
            Assert.AreEqual(1, _consoleOutput.Count);
        }

        [TestMethod]
        public void RunApp_WhenNoOptionsSet_ReturnsMinusOne()
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            string[] iaidsToProcess = new string[] { "C10101", Guid.NewGuid().ToString("N") };

            int result = program.OnExecute(_app);
            Assert.AreEqual(-1, result);
            Assert.AreEqual(2, _consoleOutput.Count);
        }

        [TestMethod]
        public void RunCategorisation_WhenAllIaidsAreValid_ReturnsZero()
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            string[] iaidsToProcess = new string[] { "C10101", Guid.NewGuid().ToString("N") };

            program.CategoriseSingle = iaidsToProcess; // Simulates supplying one or more -t option flags with Asset IDs e.g -t:C12345
            int result = program.OnExecute(_app);

            // Each IAID should result in 3 lines of fixed output, and one line for each categorisation result.
            Assert.AreEqual(_consoleOutput.Count, ExpectedConsoleOutputLineCountForValidProcessing(iaidsToProcess.Length));
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void RunCategorisation_WhenOneIaidIsInvalid_ReturnsMinusOne()
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            program.TestCategoriseSingle = new string[] { "K10101", Guid.NewGuid().ToString("N") };
            int result = program.OnExecute(_app);

            Assert.AreEqual(-1, result);
            Assert.AreEqual(1, _consoleOutput.Count);
        }

        [TestMethod]
        public void RunBothCategorisationCommands_WhenAllIaidsAreValid_ReturnsZero()
        {
            var console = new TestConsole(this);
            _consoleOutput = new List<string>();

            var program = new Categoriser(categoriserService: _categoriser, console: _console);
            string[] iaidsToProcess = new string[] { "C10101", Guid.NewGuid().ToString("N") };

            program.TestCategoriseSingle = iaidsToProcess; // Simulates supplying one or more -t option flags with Asset IDs e.g -t:C12345
            program.TestCategoriseSingle = iaidsToProcess; // Simulates supplying one or more -2 option flags with Asset IDs e.g -t:C12345
            int result = program.OnExecute(_app);

            // Each IAID should result in 3 lines of fixed output, and one line for each categorisation result.
            // Multiplied by 2 as we're runnig  both live and test categorisation.
            Assert.AreEqual(_consoleOutput.Count * 2 - 1, ExpectedConsoleOutputLineCountForValidProcessing(iaidsToProcess.Length * 2));
            Assert.AreEqual(0, result);
        }


        internal List<String> ConsoleOutput
        {
            get => _consoleOutput;
            set => _consoleOutput = value;
        }

        private IList<CategorisationResult> SubstituteCategorisationResults()
        {
            Category category1 = new Category() { Id = "C12345", Query = "cheese", Score = 1.0 };
            Category category2 = new Category() { Id = "C23456", Query = "wine", Score = 1.1 };
            Category category3 = new Category() { Id = "C34567", Query = "roses", Score = 1.2 };
            Category category4 = new Category() { Id = "C45678", Query = "chocolate", Score = 1.4 };

            CategorisationResult categorisationResult1 = new CategorisationResult(category1, 2.1);
            CategorisationResult categorisationResult2 = new CategorisationResult(category2, 2.1);
            CategorisationResult categorisationResult3 = new CategorisationResult(category3, 2.1);
            CategorisationResult categorisationResult4 = new CategorisationResult(category4, 2.1);

            var results = new List<CategorisationResult>()
            {
                categorisationResult1,
                categorisationResult2,
                categorisationResult3,
                categorisationResult4
            };

            return results;
        }

        private int ExpectedConsoleOutputLineCountForValidProcessing(int iaidCount)
        {
            return ((_categorisationResults.Count + ADDITIONAL_INFO_MESSAGES_PER_IAID_PROCESSED) * iaidCount) + 1;
        }

        /// <summary>
        /// Test friendly implementationn of IConsole so we can abstract the Console.Writeline etc. calls.
        /// </summary>
        class TestConsole : IConsole
        {
            private TextWriter _writer;
            private TestCommandLineCategoriser _parent;

            public TestConsole(TestCommandLineCategoriser parent)
            {
                _parent = parent;
                _writer = Substitute.For<TextWriter>();
                _writer.When(w => w.WriteLine(Arg.Any<string>())).Do(x => { Console.WriteLine(x); _parent.ConsoleOutput.Add(x.Arg<string>()); });
            }


            public TextWriter Out => _writer;

            public TextWriter Error => throw new NotImplementedException();

            public TextReader In => throw new NotImplementedException();

            public bool IsInputRedirected => throw new NotImplementedException();

            public bool IsOutputRedirected => throw new NotImplementedException();

            public bool IsErrorRedirected => throw new NotImplementedException();

            public ConsoleColor ForegroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public ConsoleColor BackgroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public event ConsoleCancelEventHandler CancelKeyPress;

            public void ResetColor()
            {
                throw new NotImplementedException();
            }
        }
    }
}
