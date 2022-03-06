using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1.ViewModel
{
    public class VehicleViewModel
    {
        public string ListingId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Mileage { get; set; }
        public string Price { get; set; }
        public string SellerAdDescription { get; set; }
        public List<Photos> Photos { get; set; }
        public DroppedPrice Drop { get; set; }
        public Dealer Delaer { get; set; }
        public List<string> Deals { get; set; }
        public List<DescriptionList> DescriptionList { get; set; }
    }

}
