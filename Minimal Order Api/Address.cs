namespace Minimal_Order_Api
{
    public class Address
    {
        /// <summary>The id of the address in the external system</summary>
        public string Id { get; set; }
        
        /// <summary>The company name</summary>
        public string Company { get; set; }

        /// <summary>The street excl. the housenumber</summary>
        public string Street { get; set; }

        /// <summary>The house number</summary>
        public string HouseNumber { get; set; }

        /// <summary>The second address line</summary>
        public string Line2 { get; set; }

        /// <summary>The third address line</summary>
        public string Line3 { get; set; }

        /// <summary>The city</summary>
        public string City { get; set; }

        /// <summary>The postal code</summary>
        public string Zip { get; set; }
        
        /// <summary>The name of the state</summary>
        public string State { get; set; }

        /// <summary>The name of the country</summary>
        public string Country { get; set; }

        /// <summary>The 2-letter country code</summary>
        /// <remarks>de = Germany, fr = france</remarks>
        public string CountryISO2 { get; set; }
        
        /// <summary>The first name</summary>
        public string FirstName { get; set; }

        /// <summary>The last name</summary>
        public string LastName { get; set; }

        /// <summary>The phone number</summary>
        public string Phone { get; set; }

        /// <summary>The email address</summary>
        public string Email { get; set; }

        /// <summary>A addition to the address (e.g. Apartment 2, Back door)</summary>
        public string NameAddition { get; set; }
    }
}