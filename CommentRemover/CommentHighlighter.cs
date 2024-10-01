using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace CommentExteansions
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "CommentHighlighter" classification type.
    /// </summary>
    internal class CommentHighlighter : ITagger<ClassificationTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly IClassificationType _todoType;
        private static readonly Regex _regex = new Regex(@"\s*//\s*(TODO|FIXME|NOTE)", RegexOptions.Compiled);

        internal CommentHighlighter(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _buffer = buffer;
            _todoType = registry.GetClassificationType("CommentHighlighter");
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var text = span.GetText();

                foreach (Match match in _regex.Matches(text))
                {
                    var keywordSpan = new SnapshotSpan(span.Snapshot, new Span(span.Start + match.Index, match.Length));
                    yield return new TagSpan<ClassificationTag>(keywordSpan, new ClassificationTag(_todoType));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void RaiseTagsChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }
    }

    internal static class CommentHighlighterClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CommentHighlighter")]
        private static ClassificationTypeDefinition typeDefinition;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CommentHighlighter")]
    [Name("CommentHighlighter")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CommentHighlighterFormat : ClassificationFormatDefinition
    {
        public CommentHighlighterFormat()
        {
            this.DisplayName = "Highlighted Comment (TODO, FIXME, NOTE)";
            this.ForegroundColor = Colors.OrangeRed;
            this.IsBold = true;  // Optional: makes the text bold
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ClassificationTag))]
    internal class CommentHighlighterProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagger = new CommentHighlighter(buffer, ClassificationRegistry) as ITagger<T>;
            if (tagger != null)
                return tagger;
            throw new InvalidOperationException("Failed to create ITagger<T> for buffer.");
        }
    }

}
