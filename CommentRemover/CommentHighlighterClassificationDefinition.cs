using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace CommentExtensions
{
    /// <summary>
    /// Classification type definition export for CommentHighlighter
    /// </summary>
    internal static class CommentHighlighterClassificationDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

        /// <summary>
        /// Defines the "CommentHighlighter" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CommentHighlighter")]
        private static ClassificationTypeDefinition typeDefinition;

#pragma warning restore 169
    }
}
