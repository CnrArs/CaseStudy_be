using System;
using CefSharp;
using CefSharp.OffScreen;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ConsoleApp1.ViewModel;
using ConsoleApp1.Helper;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace ClassLibrary1
{
    public static class Program
    {
        private static ChromiumWebBrowser _browser;
        private static bool _isLoggedIn = false;
        private static bool _scrapCompleted = false;
        private static List<string> _vehicleItems;
        private static List<VehicleViewModel> _vehicleList;

        public static void Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            InitializeCef();
        }

        public static void InitializeCef()
        {
            _vehicleItems = new List<string>();
            _vehicleList = new List<VehicleViewModel>();


            Cef.Initialize(new CefSettings()
            {
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            }, performDependencyCheck: true, browserProcessHandler: null);

            _browser = new ChromiumWebBrowser(address: "https://www.cars.com/signin");

            _browser.LoadingStateChanged += async (object sender, LoadingStateChangedEventArgs e) =>
            {
                if (!e.IsLoading && !_isLoggedIn)
                {
                    await LoginToAppAsync();
                }
            };

            Console.WriteLine("═════════════════STARTED═══════════════");
            Console.ReadKey();
        }

        /// <summary>
        /// Login to app the user information
        /// </summary>
        /// <returns></returns>
        private static async Task LoginToAppAsync()
        {
            string _loginScript = @"
                               document.getElementById('email').value = 'johngerson808@gmail.com';
                               document.getElementById('password').value = 'test8008';
                               document.getElementsByClassName('session-form')[0].submit();";

            await _browser.EvaluateScriptAsync(_loginScript).ContinueWith(async u =>
            {
                _isLoggedIn = true;
                await SearchCarsAsync();
            });
        }

        /// <summary>
        /// Search cars for specific keys.
        /// Default keys are available
        /// </summary>
        /// <returns></returns>
        private static async Task SearchCarsAsync(
            string make_model_search_stocktype = "used",
            string makes = "tesla",
            string models = "tesla-model_s",
            string make_model_max_price = "100000",
            string make_model_maximum_distance = "all",
            string make_model_zip = "94596")
        {

            string _searchForm =
                $"document.getElementById('make-model-search-stocktype').value ='{make_model_search_stocktype}'; " +
                $"document.getElementById('makes').value ='{makes}'; " +
                $"document.getElementById('models').value ='{models}';  " +
                $"document.getElementById('make-model-max-price').value = '{make_model_max_price}';" +
                $"document.getElementById('make-model-maximum-distance').value = '{make_model_maximum_distance}';" +
                $"document.getElementById('make-model-zip').value = '{make_model_zip}';" +
                $"document.getElementsByClassName('search-form')[0].submit()";

            await _browser.EvaluateScriptAsync(_searchForm).ContinueWith(x =>
            {
                _browser.FrameLoadEnd += async (sender, args) =>
                {
                    if (args.Frame.IsMain)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (!_scrapCompleted)
                            {
                                await ScrapVehicleList();
                            }

                            HomeDelivery().Wait();
                            GotoNextPage().Wait();

                            if (_vehicleItems.Any())
                            {
                                GotoOtherSceneriosAsync().Wait();
                            }
                        }

                        HtmlParser(_vehicleItems.ToArray());
                        if (_vehicleList.Any())
                            File.WriteAllText($"d:/result.json", JsonConvert.SerializeObject(_vehicleList).ToString());
                    }
                };
            });
        }


        /// <summary>
        /// Pass query
        /// Get list by "vehicle-card"
        /// </summary>
        /// <returns></returns>
        private static async Task ScrapVehicleList()
        {
            string _scrapItems = @"(function() { 
                                    var links=document.getElementsByClassName('vehicle-card'); 
                                    var linksArray = new Array(); 
                                    for (var i = 0; i < links.length; i++) { 
                                        linksArray[i] = String(links[i].outerHTML); 
                                    } 
                                    return linksArray; 
                                })(); ";

            await _browser.EvaluateScriptAsync(_scrapItems).ContinueWith(listingItems =>
            {
                if (listingItems.Result.Success && listingItems.Result.Result != null)
                {
                    var vehicleItems = ((List<dynamic>)listingItems.Result.Result);
                    if (vehicleItems.Count > 0)
                    {
                        _scrapCompleted = true;
                    }

                    foreach (var item in vehicleItems)
                    {
                        _vehicleItems.Add(item);
                    }
                }
            });
        }

        /// <summary>
        /// Goto newt page and parse it.
        /// </summary>
        /// <returns></returns>
        private static async Task GotoNextPage()
        {
            await _browser.EvaluateScriptAsync(@"document.getElementById('next_paginate').click()").ContinueWith(x =>
            {
                if (x.Result != null)
                {
                    ScrapVehicleList().Wait();
                }
            });
        }


        /// <summary>
        /// Click to home delivery
        /// </summary>
        /// <returns></returns>
        private static async Task HomeDelivery()
        {
            await _browser.EvaluateScriptAsync(@"document.getElementById('home_delivery_true').click();").ContinueWith(x =>
            {
                if (x.Result != null)
                {
                    Thread.Sleep(2000);
                }
            });
        }

        private static async Task GotoOtherSceneriosAsync()
        {
            await _browser.EvaluateScriptAsync(@"(function() { 
                            document.getElementById('model_tesla-model_x').click();
                            document.getElementById('model_tesla-model_s').click();
                            return linksArray; 
                        })();").ContinueWith(listingItems =>
            {
                if (listingItems.Result.Success && listingItems.Result.Result != null)
                {
                    var vehicleItems = ((List<dynamic>)listingItems.Result.Result);
                    if (vehicleItems.Count > 0)
                    {
                        _scrapCompleted = true;
                    }

                    foreach (var item in vehicleItems)
                    {
                        _vehicleItems.Add(item);
                    }
                }
            });
        }


        /// <summary>
        /// Parce vehicle items from Html text.
        /// </summary>
        /// <param name="items"></param>
        private static void HtmlParser(string[] items)
        {
            object aa = new object();
            try
            {
                foreach (var item in items)
                {
                    aa = item;

                    bool _isValid = true;
                    var _HtmlWebDocument = new HtmlAgilityPack.HtmlDocument();
                    _HtmlWebDocument.LoadHtml(item);

                    if (_HtmlWebDocument == null)
                    {
                        _isValid = false;
                    }

                    if (_isValid)
                    {
                        var _vehicleCard = _HtmlWebDocument?.DocumentNode.Descendants().Where(n => n.HasClass("vehicle-card"));

                        if (!_vehicleCard.Any())
                        {
                            _isValid = false;
                        }

                        if (_isValid)
                        {
                            foreach (var _vehicleCardItem in _vehicleCard)
                            {
                                var dealerTitle =
                                    _vehicleCardItem.Descendants("h2")?.FirstOrDefault(n => n.HasClass("title"));

                                var vehicleId =
                                    _vehicleCardItem.Descendants("button")?.FirstOrDefault(n => n.HasClass("heart"));

                                var millageStatus =
                                    _vehicleCardItem.Descendants("div")?.FirstOrDefault(n => n.HasClass("mileage"));

                                var primaryPrice =
                                    _vehicleCardItem.Descendants("span")?.FirstOrDefault(n => n.HasClass("primary-price"));

                                var IsDropped =
                                    _vehicleCardItem.Descendants("span")?.FirstOrDefault(n => n.HasClass("price-drop"));

                                var dealerName =
                                    _vehicleCardItem.Descendants("div")?.FirstOrDefault(n => n.HasClass("dealer-name"));

                                var dealerContact =
                                    _vehicleCardItem.Descendants("div")?.FirstOrDefault(n => n.HasClass("contact-buttons"));

                                var milesFrom =
                                    _vehicleCardItem.Descendants("div")?.FirstOrDefault(n => n.HasClass("miles-from"));

                                var galleryItems =
                                    _vehicleCardItem.Descendants("div")?.Where(c => c.HasClass("image-wrap"));

                                var dealItems =
                                    _vehicleCardItem.Descendants("span")?.Where(c => c.HasClass("sds-badge__label"));


                                List<Photos> photoItems = new List<Photos>();

                                if (galleryItems != null && galleryItems.Any())
                                {
                                    foreach (var galleryItem in galleryItems)
                                    {
                                        if (galleryItem.SelectSingleNode(".//div") == null)
                                        {
                                            var ImageSrc = string.Empty;

                                            if (galleryItem.SelectSingleNode(".//img")?.Attributes.FirstOrDefault(x => x.Name == "data-src") == null)
                                            {
                                                ImageSrc = galleryItem.SelectSingleNode(".//img")?.Attributes.FirstOrDefault(x => x.Name == "src")?.Value;
                                            }
                                            else
                                            {
                                                ImageSrc = galleryItem.SelectSingleNode(".//img")?.Attributes.FirstOrDefault(x => x.Name == "data-src")?.Value;
                                            }

                                            photoItems.Add(new Photos()
                                            {
                                                Url = ImageSrc
                                            });
                                        }
                                    }
                                }

                                if (
                                    vehicleId?.Attributes["data-listing-id"] != null && !_vehicleList.Any(l => l.ListingId == vehicleId?.Attributes["data-listing-id"].Value))
                                {
                                    _vehicleList.Add(new VehicleViewModel()
                                    {
                                        ListingId = vehicleId.Attributes["data-listing-id"]?.Value,
                                        Url = $"{vehicleId.Attributes["data-listing-id"]?.Value}",
                                        Title = dealerTitle?.InnerText,
                                        Mileage = millageStatus?.InnerText,
                                        Price = primaryPrice?.InnerText,
                                        Photos = photoItems,
                                        Drop = new DroppedPrice()
                                        {
                                            Drop = IsDropped?.InnerText
                                        },
                                        Delaer = new Dealer()
                                        {
                                            Title = dealerName.Descendants("strong").FirstOrDefault()?.InnerText,
                                            MilesFrom = milesFrom.InnerText,
                                            Contact = new Contact()
                                            {
                                                ContactPhone = dealerContact.Descendants("a")?.FirstOrDefault().Attributes.FirstOrDefault(x => x.Name == "data-phone-number")?.Value,
                                            }
                                        },
                                        Deals = (dealItems.Any() ? dealItems.Select(x => x.InnerText)?.ToList() : null)
                                    });
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                var aaa = aa;
                throw;
            }
        }


        #region Unused

        /// UNUSED YET
        /// <summary>
        /// If paging available, get all items.
        /// </summary>
        /// <returns></returns>
        //private static async Task getOtherPages()
        //{
        //    string _getAllPageList = @"(function() { 
        //                            var links=document.getElementsByClassName('sds-pagination__item'); 
        //                            var linksArray = new Array(); 
        //                            for (var i = 0; i < links.length; i++) { 
        //                                let text = links[i].innerText
        //                                if(text != '...')
        //                                    linksArray[i] = links[i].innerText;
        //                            } 
        //                            return linksArray; 
        //                        })();";

        //    await _browser.EvaluateScriptAsync(_getAllPageList).ContinueWith(async pageList =>
        //    {
        //        if (pageList.Result.Success && pageList.Result.Result != null)
        //        {
        //            var pageNumbers = ((List<dynamic>)pageList.Result.Result);
        //            if (!pageNumbers.Any())
        //                _scrapCompleted = false;

        //            foreach (var item in pageNumbers)
        //            {
        //                if (!string.IsNullOrEmpty(item))
        //                {
        //                    htmlParser(_vehicleItems.ToArray());
        //                    GotoNextPage().Wait();
        //                }
        //            }

        //            if (_vehicleList.Any())
        //                File.WriteAllText("d:/test.json", JsonConvert.SerializeObject(_vehicleList).ToString());
        //        }
        //    });
        //}
        #endregion

    }
}