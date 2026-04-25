using AutoMapper;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Models;

namespace MidgardAddressBook.BLL.Mapping;

/// <summary>
/// AutoMapper profile mapping between <see cref="AddressBookEntry"/> domain entities and
/// <see cref="AddressBookEntryDto"/> view models.
/// </summary>
public class AddressBookEntryProfile : Profile
{
    /// <summary>Initializes the mapping profile.</summary>
    public AddressBookEntryProfile()
    {
        CreateMap<AddressBookEntry, AddressBookEntryDto>();
        CreateMap<AddressBookEntryDto, AddressBookEntry>()
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.FileName, opt => opt.Ignore())
            // DateAdded is set explicitly in CreateAsync (DateTimeOffset.UtcNow) and must be
            // preserved in UpdateAsync — it is never present in a form POST, so mapping it
            // would overwrite the DB timestamp with the DTO default (0001-01-01).
            .ForMember(dest => dest.DateAdded, opt => opt.Ignore());
    }
}
