// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Flow.Plugin.VSCodeWorkspaces.SshConfigParser
{
    public class SshConfig
    {
        public static IEnumerable<SshHost> ParseFile(string path)
        {
            return Parse(File.ReadAllText(path));
        }

        public static IEnumerable<SshHost> Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new List<SshHost>();

            var list = new List<SshHost>();
            using var reader = new StringReader(str);
            SshHost currentHost = null;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                trimmed = StripInlineComment(trimmed);
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (!TryParseKeyValue(trimmed, out var key, out var value))
                    continue;

                if (key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    currentHost = new SshHost();
                    list.Add(currentHost);
                }

                if (currentHost == null)
                    continue;

                currentHost.Properties[key] = value;
            }

            return list;
        }

        private static string StripInlineComment(string line)
        {
            var inQuotes = false;
            var quoteChar = '\0';
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"' || ch == '\'')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = ch;
                    }
                    else if (quoteChar == ch)
                    {
                        inQuotes = false;
                    }
                }
                else if (ch == '#' && !inQuotes)
                {
                    return line.Substring(0, i).TrimEnd();
                }
            }

            return line;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            var index = 0;
            while (index < line.Length && char.IsWhiteSpace(line[index]))
                index++;

            if (index >= line.Length)
                return false;

            var keyStart = index;
            while (index < line.Length && !char.IsWhiteSpace(line[index]))
                index++;

            if (index == keyStart)
                return false;

            key = line.Substring(keyStart, index - keyStart);

            while (index < line.Length && char.IsWhiteSpace(line[index]))
                index++;

            if (index >= line.Length)
                return false;

            if (line[index] == '"' || line[index] == '\'')
            {
                var quote = line[index];
                index++;
                var valueStart = index;
                while (index < line.Length && line[index] != quote)
                    index++;
                value = line.Substring(valueStart, index - valueStart);
                return true;
            }

            var unquotedStart = index;
            while (index < line.Length && !char.IsWhiteSpace(line[index]))
                index++;

            value = line.Substring(unquotedStart, index - unquotedStart);
            return true;
        }
    }
}
