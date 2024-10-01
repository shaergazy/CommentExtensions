using CommentExteansions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace CommentExtensions
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")] // Обрабатываем код
    [TagType(typeof(ClassificationTag))]
    internal class CommentHighlighterProvider : ITaggerProvider
    {
        [Export]
        [Name("CommentHighlighter")]
        [BaseDefinition("code")]
        internal static ClassificationTypeDefinition CommentClassificationType = null;

        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null;

        [Import]
        internal IClassificationFormatMapService FormatMapService = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new CommentHighlighter(buffer, ClassificationRegistry) as ITagger<T>;
        }
    }
}
