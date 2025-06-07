using System;
using System.Collections.Generic;

namespace CheckoutService.Domain.ValueObjects
{
    public class AddressData
    {
        public string Street { get; }
        public string City { get; }
        public string ZipCode { get; }

        public AddressData(string street, string city, string zipCode)
        {
            Street = street ?? throw new ArgumentNullException(nameof(street));
            City = city ?? throw new ArgumentNullException(nameof(city));
            ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
        }

        protected bool Equals(AddressData other)
        {
            return Street == other.Street && City == other.City && ZipCode == other.ZipCode;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AddressData)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Street, City, ZipCode);
        }
    }
}
