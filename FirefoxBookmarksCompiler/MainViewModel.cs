using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace FirefoxBookmarksCompiler
{
    public partial class Bookmark : ObservableObject
    {
        [ObservableProperty]
        private string? displayName;

        [ObservableProperty]
        private string? link;

        [ObservableProperty]
        private DateTime? createdAt;

        public Bookmark(string Name, string Link, string Epoch)
        {
            this.displayName = Name;
            this.link = Link;
            this.createdAt = string.IsNullOrEmpty(Epoch) ? null : DateTimeOffset.FromUnixTimeSeconds(int.Parse(Epoch)).LocalDateTime;
        }
    }

    public partial class MainViewModel : ObservableObject
    {
        public static string BookmarksFile = "Bookmarks.json";

        [ObservableProperty]
        private ObservableCollection<string> files = new ObservableCollection<string>();

        [ObservableProperty]
        public ObservableCollection<Bookmark> bookmarks = new ObservableCollection<Bookmark>();

        [ObservableProperty]
        public bool collectionFilled = false;

        [ObservableProperty]
        public Visibility hintVisibility = Visibility.Visible;

        [ObservableProperty]
        public Visibility dataGridVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private bool deleteAfter = false;

        [ObservableProperty]
        private bool removeDuplicates = false;

        public MainViewModel()
        {
            files.CollectionChanged += Files_CollectionChanged;
            bookmarks.CollectionChanged += Bookmarks_CollectionChanged;

            if (File.Exists(BookmarksFile))
            {
                foreach (var bookmark in JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(File.ReadAllText(BookmarksFile)))
                {
                    bookmarks.Add(bookmark);
                }
            }
        }

        private void Bookmarks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            collectionFilled = true;
            HintVisibility = Visibility.Collapsed;
            DataGridVisibility = Visibility.Visible;
        }

        private async void Files_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems)
                    {
                        if (newItem != null)
                        {
                            if (IsHtml(File.ReadAllText(newItem.ToString())))
                            {
                                var doc = new HtmlDocument();
                                doc.OptionFixNestedTags = true;
                                doc.Load(newItem.ToString());

                                var nodes = doc.DocumentNode.SelectNodes("//a");

                                foreach (var node in nodes)
                                {
                                    string href = node.Attributes["href"].Value;

                                    if (!href.Contains("https://support.mozilla.org") && bookmarks.Any(x => x.Link == href) == false)
                                    {
                                        string addDate = node.Attributes["add_date"].Value;
                                        string icon = node.Attributes["icon"] != null ? node.Attributes["icon"].Value : "";
                                        string title = node.InnerText;

                                        App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
                                        {
                                            bookmarks.Add(new Bookmark(title, href, addDate));
                                        });

                                    }
                                }

                                await Task.Delay(1024);
                                
                                SaveBookmarks();
                            }

                            else
                            {

                            }
                        }
                    }
                }
            });
        }

        private void SaveBookmarks()
        {
            File.WriteAllText(BookmarksFile, JsonConvert.SerializeObject(bookmarks));
        }

        public static bool IsHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Regular expression to check for HTML tags
            string pattern = @"<[^>]+>";
            return Regex.IsMatch(input, pattern);
        }

        [RelayCommand]
        public void Compile()
        {

        }
    }
}
