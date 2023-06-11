using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.XPath;

namespace AzureSearchCrawler
{
    /// <summary>
    /// Extracts text content from a web page. The default implementation is very simple: it removes all script, style,
    /// svg, and path tags, and then returns the InnerText of the page body, with cleaned up whitespace.
    /// <para/>You can implement your own custom text extraction by overriding the ExtractText method. The protected
    /// helper methods in this class might be useful. GetCleanedUpTextForXpath is the easiest way to get started.
    /// </summary>
    public class TextExtractor
    {
        private readonly Regex newlines = new Regex(@"(\r\n|\n)+");
        private readonly Regex spaces = new Regex(@"[ \t]+");

        public virtual string ExtractText(IDocument doc)
        {
            return GetCleanedUpTextForXpath(doc, "//body");
        }

        public string GetCleanedUpTextForXpath(IDocument doc, string xpath)
        {
            if (doc == null || doc.Body == null)
            {
                return null;
            }

            RemoveNodesOfType(doc, "script", "style", "svg", "path");

            string content = ExtractTextFromFirstMatchingElement(doc, xpath);
            return NormalizeWhitespace(content);
        }

        protected string NormalizeWhitespace(string content)
        {
            if (content == null)
            {
                return null;
            }

            content = newlines.Replace(content, "\n");
            return spaces.Replace(content, " ");
        }

        protected void RemoveNodesOfType(IDocument doc, params string[] types)
        {
            string xpath = String.Join(" | ", types.Select(t => "//" + t));
            RemoveNodes(doc, xpath);
        }

        protected void RemoveNodes(IDocument doc, string xpath)
        {
            var nodes = SafeSelectNodes(doc, xpath).ToList();
            // Console.WriteLine("Removing {0} nodes matching {1}.", nodes.Count, xpath);
            foreach (var node in nodes)
            {
                node.Parent.RemoveChild(node);
            }
        }

        /// <summary>
        /// Returns TextContent of the first element matching the xpath expression, or null if no elements match.
        /// </summary>
        protected string ExtractTextFromFirstMatchingElement(IDocument doc, string xpath)
        {
            return SafeSelectNodes(doc, xpath).FirstOrDefault()?.TextContent;
        }

        /// <summary>
        /// Null-safe DocumentNode.SelectNodes
        /// </summary>
        protected IEnumerable<INode> SafeSelectNodes(IDocument doc, string xpath)
        {
            return doc.Body.SelectNodes(xpath) ?? Enumerable.Empty<INode>();
        }
    }
}
