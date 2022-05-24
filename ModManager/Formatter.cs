using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModManager.Models;

namespace ModManager
{
    internal class Formatter
    {
        private Config.Formating? config;

        public Formatter(Config.Formating config)
        {
            this.config = config;
        }

        public List<Item> Parse(List<string> list)
        {
            var result = new List<Item>();
            if (config != null && list != null && list.Count > 0)
            {
                var commentRegex = new Regex(config.Comment!.Pattern!, RegexOptions.IgnoreCase);
                var enableRegex = new Regex(config.Enable!.Pattern!, RegexOptions.IgnoreCase);
                var disableRegex = new Regex(config.Disable!.Pattern!, RegexOptions.IgnoreCase);
                
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
                    else if (!this.config!.RemoveOnDisable && !item.IsEnabled)
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
            var template = config!.Enable?.Format;
            if (!string.IsNullOrEmpty(template))
            {
                var dict = new Dictionary<string, string>();
                dict.Add(@"<fileName>", fileName);
                result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));
            }
            return result;
        }

        private string MakeDisable(string fileName)
        {
            var result = fileName;
            var template = config!.Disable?.Format;
            if (!string.IsNullOrEmpty(template))
            {
                var dict = new Dictionary<string, string>();
                dict.Add(@"<fileName>", fileName);
                result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));
            }
            return result;
        }

        private string MakeComment(string comment)
        {
            var result = comment;
            var template = config!.Comment?.Format;
            if (!string.IsNullOrEmpty(template))
            {
                var dict = new Dictionary<string, string>();
                dict.Add(@"<comment>", comment);
                result = dict.Aggregate(template, (s, kv) => s.Replace(kv.Key, kv.Value));
            }
            return result;
        }

        public class Item
        {
            public string? Data { get; set; }

            public string? Source { get; set; }

            public bool IsEnabled { get; set; } = false;

            public bool IsComment { get; set; } = false;
        }
    }
}
