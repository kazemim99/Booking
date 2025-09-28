


//using Microsoft.EntityFrameworkCore.ChangeTracking;

//namespace Booksy.ServiceCatalog.Domain.ValueObjects
//{
//    public sealed class ContactInfo : ValueObject
//    {
//        private ContactInfo()
//        {
            
//        }
//        public Email Email { get; }
//        public PhoneNumber PrimaryPhone { get; }
//        public PhoneNumber? SecondaryPhone { get; }
//        public string? Website { get; }
//        public string? FacebookPage { get; }
//        public string? InstagramHandle { get; }

//        private ContactInfo(Email email, PhoneNumber primaryPhone, PhoneNumber? secondaryPhone = null, string? website = null, string? facebookPage = null, string? instagramHandle = null)
//        {
//            Email = email;
//            PrimaryPhone = primaryPhone;
//            SecondaryPhone = secondaryPhone;
//            Website = website;
//            FacebookPage = facebookPage;
//            InstagramHandle = instagramHandle;
//        }

//        public static ContactInfo Create(Email email, PhoneNumber primaryPhone, PhoneNumber? secondaryPhone = null, string? website = null, string? facebookPage = null, string? instagramHandle = null)
//            => new(email, primaryPhone, secondaryPhone, website, facebookPage, instagramHandle);

//        public ContactInfo UpdateWebsite(string website)
//            => new(Email, PrimaryPhone, SecondaryPhone, website, FacebookPage, InstagramHandle);

//        public ContactInfo UpdateSocialMedia(string? facebookPage, string? instagramHandle)
//            => new(Email, PrimaryPhone, SecondaryPhone, Website, facebookPage, instagramHandle);

//        public ContactInfo UpdateSecondaryPhone(PhoneNumber? secondaryPhone)
//            => new(Email, PrimaryPhone, secondaryPhone, Website, FacebookPage, InstagramHandle);

//        protected override IEnumerable<object> GetAtomicValues()
//        {
//            yield return Email;
//            yield return PrimaryPhone;
//            if (SecondaryPhone != null) yield return SecondaryPhone;
//            if (Website != null) yield return Website;
//            if (FacebookPage != null) yield return FacebookPage;
//            if (InstagramHandle != null) yield return InstagramHandle;
//        }
//    }
//}