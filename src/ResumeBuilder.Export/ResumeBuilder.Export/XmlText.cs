using System.Text;
using System.Xml;

namespace ResumeBuilder.Export;

/// <summary>
/// Strips the characters XML 1.0 cannot carry, so a pasted resume never produces a DOCX that
/// Word refuses to open.
///
/// Control characters below U+0020 other than tab, newline and carriage return, lone surrogates,
/// and U+FFFE/U+FFFF are all illegal in a document, and OpenXml writes them through unchecked. A
/// form feed pasted from a PDF was enough to break every DOCX export of that resume.
/// </summary>
public static class XmlText
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder? cleaned = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var keep = XmlConvert.IsXmlChar(c);

            if (!keep && char.IsHighSurrogate(c) && i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c))
            {
                cleaned?.Append(c).Append(text[i + 1]);
                i++;
                continue;
            }

            if (keep)
            {
                cleaned?.Append(c);
                continue;
            }

            if (cleaned is null)
            {
                cleaned = new StringBuilder(text.Length);
                cleaned.Append(text, 0, i);
            }
        }

        return cleaned?.ToString() ?? text;
    }
}
