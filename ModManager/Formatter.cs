using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ModManager.Models;

namespace ModManager
{
    internal class Formatter
    {
        private static readonly Regex PATTERN_COMMENT = new Regex(@"^#[\s]{0,}(.*?[\w\W])$", RegexOptions.IgnoreCase);
        private static readonly Regex PATTERN_ENABLE = new Regex(@"^\*(.*?[\w\W])$", RegexOptions.IgnoreCase);
        private static readonly Regex PATTERN_DISABLE = new Regex(@"^([\w].*?[\w\W])$", RegexOptions.IgnoreCase);

        private static readonly string FORMAT_COMMENT = @"# <comment>";
        private static readonly string FORMAT_ENABLE = @"*<fileName>";
        private static readonly string FORMAT_DISABLE = @"<fileName>";

        public bool RemoveOnDisable { get; set; }

        public Formatter()
        {
        }

        public Formatter(bool removeOnDisable)
        {
            this.RemoveOnDisable = removeOnDisable;
        }

        public List<Item> Parse(List<string> list)
        {
            var result = new List<Item>();
            if (list != null && list.Count > 0)
            {
                var commentRegex = PATTERN_COMMENT;
                var enableRegex = PATTERN_ENABLE;
                var disableRegex = PATTERN_DISABLE;
                
                foreach(var s in list)
                {
                    var match = commentRegex.Match(s);
                    if (match.Success)
                    {
                        var data = match.Groups[1].Value;
                        result.Add(new Item 
                        {
                            Data = data,
                            Source = s,
                            IsComment = true
                        });
                        continue;
                    }

                    match = enableRegex.Match(s);
                    if (match.Success)
                    {
                        var data = match.Groups[1].Value;
                        result.Add(new Item
                        {
                            Data = data,
                            Source = s,
                            IsEnabled = true
                        });
                        continue;
                    }

                    match = disableRegex.Match(s);
                    if (match.Success)
                    {
                        var data = match.Groups[1].Value;
                        result.Add(new Item
                        {
                            Data = data,
                            Source = s
                        });
                        continue;
                    }
                }
            }
            return result;
        }

        public List<string> Make(List<string> comments, List<ListItemModel> lists)
        {
            var result = new List<string>();

            foreach (var c in comments)
            {
                result.Add(this.MakeComment(c));
            }

            if (lists.Count > 0)
            {
                foreach(var item in lists)
                {
                    if (item.IsSystem) continue;
                    if (!item.IsFound)
                    {
                        result.Add(this.MakeDisable(item.Name));
                    } 
                    else if (item.IsEnabled)
                    {
                        result.Add(this.MakeEnabled(item.Name));
                    }
                    else if (!this.RemoveOnDisable && !item.IsEnabled)
                    {
                        result.Add(this.MakeDisable(item.Name));
                    }
                }
            }
            return result;
        }

        private string MakeEnabled(string fileName)
        {
            var result = fileName;
            var template = FORMAT_ENABLE;
            
            var dict = new Dictionary<string, string>();
            dict.Add(@"<fileName>", fileName);
            result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));

            return result;
        }

        private string MakeDisable(string fileName)
        {
            var result = fileName;
            var template = FORMAT_DISABLE;
            
            var dict = new Dictionary<string, string>();
            dict.Add(@"<fileName>", fileName);
            result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));
            return result;
        }

        private string MakeComment(string comment)
        {
            var result = comment;
            var template = FORMAT_COMMENT;
            
            var dict = new Dictionary<string, string>();
            dict.Add(@"<comment>", comment);
            result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));
            return result;
        }

        public class Item
        {
            public string? Data { get; set; }

            public string? Source { get; set; }

            public bool IsEnabled { get; set; } = false;

            public bool IsComment { get; set; } = false;

            public override string ToString()
            {
                return this.Data ?? base.ToString()!;
            }
        }
    }
}
