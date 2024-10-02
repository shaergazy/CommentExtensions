using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace CommentRemover
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class BrackerAdderCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("87a41c82-2390-4191-bd55-283cf5cb57c1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrackerAdderCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private BrackerAdderCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static BrackerAdderCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in BrackerAdderCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new BrackerAdderCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));

            var activeDocument = dte.ActiveDocument;
            if (activeDocument == null) return;

            var textDocument = (TextDocument)activeDocument.Object("TextDocument");
            var selection = textDocument.Selection;

            if (selection == null) return;

            // Determine if the user has made a selection
            if (selection.IsEmpty)
            {
                // If no selection, format the entire document
                var editPoint = textDocument.StartPoint.CreateEditPoint();
                var documentText = editPoint.GetText(textDocument.EndPoint);
                var formattedText = FormatIfStatements(documentText);

                editPoint.Delete(textDocument.EndPoint);
                editPoint.Insert(formattedText);
            }
            else
            {
                // Format only the selected text
                var selectedText = selection.Text;
                var formattedSelectedText = FormatIfStatements(selectedText);

                selection.Delete(); // Remove the old selection
                selection.Insert(formattedSelectedText); // Insert formatted text
            }
        }

        public static string FormatIfStatements(string input)
        {
            var pattern = @"(^\s*)(if\s*\([^\)]+\))\s*throw\s*([^\;]+);";

            var result = Regex.Replace(input, pattern, match =>
            {
                var indentation = match.Groups[1].Value;
                var condition = match.Groups[2].Value.Trim();
                var exception = match.Groups[3].Value.Trim();

                return $"{indentation}{condition}\n{indentation}{{\n{indentation}    throw {exception};\n{indentation}}}";
            }, RegexOptions.Multiline);

            result = Regex.Replace(result, @"\r?\n\s*\r?\n", "\n");

            return result;
        }

    }
}
