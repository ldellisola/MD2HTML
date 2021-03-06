﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MDParser.Utils;

namespace MDParser
{
    public static class DirectoryStructure
    {
        private static string[] excludedExtensions = {".avi"};
        private static string[] excludedFolders = {"docs"};
        public static void Copy(DirectoryInfo src, DirectoryInfo dest,bool isRoot, bool overwriteFiles = true)
        {
            if (src.Exists)
            {

                if (!dest.Exists)
                {
                    dest.Create();
                }

                int srcLenght = src.FullName.Length;

                var files = src.GetFiles().Where(t=>!t.Attributes.HasFlag(FileAttributes.Hidden)).ToList();


                files.ForEach((t) =>
                {
                    try
                    {
                        File.Copy(t.FullName, (dest.FullName + @"/" + t.Name).FormatAsPath(), overwriteFiles);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("The File {0} couldn't be copied. Error MSG: {1}",t.FullName,e.Message);
                    }
                });


                var directories = src.GetDirectories().Where(t => !excludedFolders.Contains(t.Name) && !t.Attributes.HasFlag(FileAttributes.Hidden)).ToList();

                directories.ForEach(t =>
                {
                    var newDest = dest.CreateSubdirectory(t.Name);
                    Copy(t,newDest,false, overwriteFiles);
                });


            }
        }

        public  static string CreateIndex(List<FileInfo> mdFiles,string title)
        {
            List<TitleNode> fileTrees = new List<TitleNode>();

            foreach (var file in mdFiles)
            {
                var lines = ( File.ReadAllLines(file.FullName))
                    .Where(t =>
                    {
                        var reg = Regex.Match(t, "^(#+).+$");
                        return reg.Success && reg.Groups[1].Value.Length > 0;
                    }).ToList();

                string text = file.Name.Replace(".md", "");
                int ind = text.IndexOf('-');
                if (ind != -1)
                {
                    text = text.Substring(ind+1);
                }

                TitleNode root = new TitleNode
                {
                    Text = text.Trim(),
                    ParentNode = null,
                    Level = 0,
                    FileName = file.Name,
                    RealLevel = 0
                };

                TitleNode curr = root;

                lines.ForEach(t =>
                {
                    var reg = Regex.Match(t, "^(#+).+$");
                    int level = reg.Groups[1].Value.Length;

                    while (level <= curr.Level)
                    {
                        curr = curr.ParentNode;
                    }

                    TitleNode child = new TitleNode
                    {
                        ParentNode = curr,
                        Level = level,
                        Text = t.TrimStart('#').Trim(),
                        FileName = file.Name,
                        RealLevel = level
                    };
                    curr.ChildNodes.Add(child);

                    curr = child;
                });

                if (root.ChildNodes.Count == 1)
                {
                    var removedChild = root.ChildNodes.First();
                    root.ChildNodes = removedChild.ChildNodes;
                    root.RearrangeTree();
                }

                fileTrees.Add(root);
            }


            StringBuilder bld = new StringBuilder($"# {title}\n");

            int index = 1;
            foreach (var tree in fileTrees)
            {
                tree.WriteMD(bld,index++);
            }

            return bld.ToString();
        }

        public static void RunInEveryDirectory(Action<DirectoryInfo> convert, string dir)
        {

            convert(new DirectoryInfo(dir));

            Directory.GetDirectories(dir).ToList().ForEach((t) =>
            {
                RunInEveryDirectory(convert,t);
            });

        }

        public static async Task RunInAllFiles(Func<string, Task> convert, string dir,string searchPattern = "*.md" )
        {

            var files = Directory.GetFiles(dir,searchPattern);
            
            
            Task[]tasks = new Task[files.Length];

            for(int i = 0; i < files.Length; i++)
            {
                var i1 = i;
                //convert(files[i1]);
                tasks[i] = convert(files[i1]); 
                //tasks[i] = (Task.Factory.StartNew(() =>
                //{
                //    convert(files[i1]); 
                //}));
            }
            
            await Task.WhenAll(tasks);
            var directories = Directory.GetDirectories(dir).ToList();


            await Task.WhenAll(directories.Select(item => RunInAllFiles(convert, item)));

            //foreach (var directory in directories)
            //{
            //    await RunInAllFiles(convert,directory);    
            //}


        }
    }
}
