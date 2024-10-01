using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace CommentExtensions
{
    /// <summary>
    /// Defines an editor format for the CommentHighlighter type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CommentHighlighter")]
    [Name("CommentHighlighter")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class CommentHighlighterFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="
        /// 
        /// 
        /// CommentHighlighterFormat"/> class.
        /// </summary>
        public CommentHighlighterFormat()
        {
            this.DisplayName = "Highlighted Comment (TODO, FIXME, NOTE)";
            this.ForegroundColor = Colors.OrangeRed; // Вы можете изменить цвет на любой другой
        }
    }
}
