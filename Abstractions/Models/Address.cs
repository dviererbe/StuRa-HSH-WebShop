using System;
using System.ComponentModel.DataAnnotations;

namespace StuRaHsHarz.WebShop.Models
{
    public class Address
    {
        [Required]
        [DataType(DataType.Text)]
        public string AddressLine1 { get; set; }

        [DataType(DataType.Text)]
        public string AddressLine2 { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.PostalCode)]
        public uint PostalCode { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string CityName { get; set; }

        public override string ToString()
        {
            return AddressLine1 + Environment.NewLine +
                   (AddressLine2.Length > 0 ? AddressLine2 + Environment.NewLine : String.Empty) + 
                   PostalCode + " " + CityName;


        }
    }
}
