using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace CommentExtensions
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CommentRemoverCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("87a41c82-2390-4191-bd55-283cf5cb57c1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentRemoverCommand"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CommentRemoverCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static CommentRemoverCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CommentRemoverCommand(package, commandService);
        }

        /// <summary>
        /// Execute the command: removes comments from the selected code or entire file if no selection.
        /// </summary>
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
                // If no selection, remove comments from the entire document
                var editPoint = textDocument.StartPoint.CreateEditPoint();
                var documentText = editPoint.GetText(textDocument.EndPoint);
                var uncommentedText = RemoveComments(documentText);

                editPoint.Delete(textDocument.EndPoint);
                editPoint.Insert(uncommentedText);
            }
            else
            {
                // Remove comments only from the selected text
                var selectedText = selection.Text;
                var uncommentedSelectedText = RemoveComments(selectedText);

                selection.Delete(); // Remove the old selection
                selection.Insert(uncommentedSelectedText); // Insert uncommented text
            }
        }

        /// <summary>
        /// Removes comments from the provided text (supports single-line and multi-line comments),
        /// while preserving formatting (indentation and whitespace).
        /// </summary>
        /// <param name="input">The text from which comments should be removed.</param>
        /// <returns>The text with comments removed while keeping the formatting intact.</returns>
        private string RemoveComments(string input)
        {
            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool insideMultiLineComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Handle multi-line comments that span across multiple lines
                if (insideMultiLineComment)
                {
                    lines[i] = RemoveMultiLineCommentEnd(line, ref insideMultiLineComment);
                    continue;
                }

                // Handle single-line comments (//) and multi-line comment starts (/*)
                if (line.Contains("//"))
                {
                    lines[i] = RemoveSingleLineComment(line);
                }

                if (line.Contains("/*"))
                {
                    lines[i] = RemoveMultiLineCommentStart(line, ref insideMultiLineComment);
                }
            }

            // Return lines with non-whitespace content only
            return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        /// <summary>
        /// Removes the content of a single-line comment from a line of code.
        /// </summary>
        /// <param name="line">The line of code.</param>
        /// <returns>The line with the comment removed.</returns>
        private string RemoveSingleLineComment(string line)
        {
            int commentIndex = line.IndexOf("//");

            // If the comment is after some code, remove the comment only
            if (commentIndex > 0)
            {
                return line.Substring(0, commentIndex).TrimEnd();
            }

            // If the entire line is a comment, return an empty string
            return string.Empty;
        }

        /// <summary>
        /// Removes the start of a multi-line comment and handles cases where the comment spans multiple lines.
        /// </summary>
        /// <param name="line">The current line of code.</param>
        /// <param name="insideMultiLineComment">Boolean flag to indicate whether we are inside a multi-line comment.</param>
        /// <returns>The line with the multi-line comment start removed.</returns>
        private string RemoveMultiLineCommentStart(string line, ref bool insideMultiLineComment)
        {
            int startCommentIndex = line.IndexOf("/*");

            // If the multi-line comment ends on the same line
            if (line.Contains("*/"))
            {
                int endCommentIndex = line.IndexOf("*/", startCommentIndex);
                return line.Substring(0, startCommentIndex) + line.Substring(endCommentIndex + 2).TrimStart();
            }

            // Otherwise, we are inside a multi-line comment that spans multiple lines
            insideMultiLineComment = true;
            return line.Substring(0, startCommentIndex).TrimEnd();
        }

        /// <summary>
        /// Removes the content of a multi-line comment until the end of the comment is found.
        /// </summary>
        /// <param name="line">The current line of code.</param>
        /// <param name="insideMultiLineComment">Boolean flag to indicate whether we are inside a multi-line comment.</param>
        /// <returns>The line after the end of the multi-line comment or an empty string if still inside the comment.</returns>
        private string RemoveMultiLineCommentEnd(string line, ref bool insideMultiLineComment)
        {
            if (line.Contains("*/"))
            {
                int endCommentIndex = line.IndexOf("*/");
                insideMultiLineComment = false;
                return line.Substring(endCommentIndex + 2).TrimStart();
            }

            // If still inside a multi-line comment, return an empty string
            return string.Empty;
        }

    }
}
